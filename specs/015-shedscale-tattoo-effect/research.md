# Phase 0 Research: Shedscale Tattoo Effect

All findings below are grounded against the real, locally installed RimWorld 1.6 game code
(`Assembly-CSharp.dll` under `RimWorldWin64_Data\Managed\`, decompiled read-only via `ilspycmd`, no game
code executed) and the real, locally installed `CombatExtended.dll` (Steam Workshop item `2890901044`) —
mirroring features 007/010/011/012/013/014's own decompile-to-verify approach, not assumed. This is the
first tattoo whose research required decompiling `Verse.HediffSet`'s missing/added-part machinery,
`RimWorld.Recipe_InstallArtificialBodyPart`/`Recipe_RemoveBodyPart`, `Verse.Pawn_HealthTracker.RestorePart`,
and `Verse.Need_Food`'s hunger-rate pipeline — none of the prior 14 features touched missing body parts,
prosthetics, or the hunger Need at all.

## R1: A body part can never be simultaneously "missing" and "prosthetic-blocked" — the proposal's two-state framing collapses to one engine-level check

**Decision**: Eligibility for regrowth is a single, always-fresh check — "is this `BodyPartRecord` currently in
`pawn.health.hediffSet.GetMissingPartsCommonAncestors()`?" — with no separate "blocked" sub-state to track,
persist, or bank progress against.

**Rationale**: The design proposal describes two facts about a part ("it's missing" and "it has a prosthetic
installed") as if they can coexist, with the tattoo needing to check both and block on the second. Decompiling
the actual install/remove pipeline shows they cannot coexist: `Recipe_InstallArtificialBodyPart.ApplyOnPawn`
calls `MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts` (which wraps `Pawn_HealthTracker.RestorePart`,
R2) *before* adding the new `Hediff_AddedPart` — this removes the part's `Hediff_MissingPart` outright, so an
installed part is never also "missing" from `HediffSet`'s own point of view. Symmetrically, uninstalling a
prosthetic/bionic (`Recipe_RemoveBodyPart` on a part where `HasDirectlyAddedPartFor(part)` is true, the
in-game "Remove: [prosthetic]" bill) calls `DamagePart` → `pawn.TakeDamage(new DamageInfo(DamageDefOf
.SurgicalCut, 99999f, ...))`, i.e. it re-amputates the part through the normal damage pipeline, which is what
actually creates a fresh `Hediff_MissingPart` (`ageTicks` reset to 0) — not a "resume" of anything. FR-008/
FR-009/FR-010/FR-011's entire "blocks the timer, doesn't bank progress, uninstalling restarts fresh" behavior
falls out for free from this one fact: our own eligibility scan (R3) simply never sees an installed part as
missing in the first place, and never needs to notice removal either — the next scan just finds a fresh
`Hediff_MissingPart` with no history, exactly matching "restart from zero." The FR-009 alert (R9) is the *only*
piece of UI that needs to actively notice "there's a prosthetic where a regrowth could otherwise be happening"
— everything else is a non-event.

**Alternatives considered**: Tracking an explicit `blockedParts` set alongside `GetMissingPartsCommonAncestors`
and cross-referencing `HasDirectlyAddedPartFor` — rejected once decompiling confirmed the two conditions are
mutually exclusive by construction; the extra state would just be perpetually empty dead code.

## R2: Regrowth completion — one vanilla call restores the part *and* clears old permanent injuries (FR-007)

**Decision**: Complete a regrowth by calling `pawn.health.RestorePart(part)` directly — no custom cleanup code
needed for FR-007's "clears old permanent injuries" upside.

**Rationale**: Decompiling `Pawn_HealthTracker.RestorePart(BodyPartRecord part, Hediff diffException = null,
bool checkStateChange = true)` shows it recursively removes *every* hediff on that part and its sub-parts
(missing-part hediff, injuries, scars — anything without `def.keepOnBodyPartRestoration`) via
`RestorePartRecursiveInt`, then calls `hediffSet.DirtyCache()` and `CheckForStateChange`. This is the exact
same call `Hediff_AddedPart.PostAdd` itself uses before installing a prosthetic — reused here for natural
regrowth instead. One call both "grows the part back" (removes the `Hediff_MissingPart`) and "returns a clean
part" (wipes any old scar/permanent-injury hediffs riding on it) — FR-007 is satisfied with no separate
injury-clearing pass.

**Alternatives considered**: Manually enumerating and removing hediffs on the part before/after regrowth —
rejected as strictly redundant with what `RestorePart` already does, and riskier (easy to miss a hediff type
vanilla's own recursive sweep already accounts for, e.g. descendant sub-parts of a multi-level-missing limb).

## R3: Body-part eligibility — `GetMissingPartsCommonAncestors()` plus an explicit tier-scoped allow-list, not raw `BodyPartDepth`

**Decision**: Enumerate candidates via `pawn.health.hediffSet.GetMissingPartsCommonAncestors()` (a
`List<Hediff_MissingPart>`, already deduplicated to the single topmost missing ancestor per lost region — a
missing whole arm reports once as "Arm," not also separately as its Hand/Fingers). Filter each candidate's
`.Part.def` against two explicit, XML-authored `BodyPartDef` allow-lists: a Tier 1 list (Finger, Toe, Hand,
Foot, Arm, Leg, Ear, Nose, Jaw, Eye) and a Tier-2-only additional list (Kidney, Lung), plus a hardcoded
never-eligible denylist (Brain, via `BodyPartTagDefOf.ConsciousnessSource` same as `HediffSet.GetBrain()`'s own
check; Spine, Neck, Torso, Pelvis; and the pawn's own `RaceProps.body.corePart` as a generic catch-all).

**Rationale**: Decompiling `Bodies_Humanlike.xml` shows `BodyPartRecord.depth` alone doesn't cleanly separate
"Tier 1 external limb" from "everything else" — `Torso` and `Neck` are themselves `Outside`-depth (same as
Arm/Hand/Eye), and `Arm`'s own skeletal sub-parts (`Humerus`, `Radius`) are `Inside`-depth but are never
independently "missing" anyway (they're always collapsed under their `Outside` parent by
`GetMissingPartsCommonAncestors`, R3 above). A pure depth check would need the same explicit exclusion list
anyway to keep Torso/Neck/Pelvis/Spine out, so an explicit allow-list is simpler and more honest about intent
than "depth, except for these five exceptions." The Tier 2 organ list is deliberately scoped to Kidney/Lung —
the proposal's own named examples — rather than every `Inside`-depth part in the body (Heart/Liver/Stomach/
Ribcage/Sternum/Spine/Pelvis are also `Inside`); losing a Heart or Stomach is generally not survivable long
enough for "missing and regrowing" to be a meaningful state the way a missing kidney or lung already is in
vanilla, so widening the list is a balance-pass call, not something this spec's approved proposal asks for.
Both lists live on `HediffCompProperties_ShedscaleEffect` as configurable `List<BodyPartDef>` fields
(Constitution Principle III), not hardcoded in C#, so a future balance pass can add Heart/Liver without a code
change.

**Alternatives considered**: Filtering purely on `BodyPartDepth` — rejected, R3's own decompile shows it
under- and over-includes without the same exclusion list anyway. Filtering on `BodyPartTagDef` groups (e.g. a
hypothetical "Regrowable" tag) — rejected; no such tag exists in vanilla and inventing one would require
patching every race's body def rather than just reading an explicit list.

## R4: Stacking hunger/pain — a companion `Hediff` whose `Severity` is the live regrowth count, not the generic `StatPart_TattooEffectOffset` pipeline

**Decision**: A new support `HediffDef` (`TattooMagic_Hediff_ShedscaleStrain`) with discrete `stages` keyed by
`minSeverity` (1, 2, 3, 4, 5+), each setting `hungerRateFactorOffset` and `painOffset`. `HediffComp_
ShedscaleEffect` adds it via `pawn.health.AddHediff(...)` the moment the tracked-regrowth count goes from 0 to
1, sets its `Severity` directly to the current count on every change, and removes it via `pawn.health
.RemoveHediff(...)` when the count returns to 0.

**Rationale**: Decompiling `Need_Food.FoodFallPerTickAssumingCategory` shows hunger rate is computed via
`pawn.health.hediffSet.GetHungerRateFactor(...)`, which sums/multiplies `HediffStage.hungerRateFactor`/
`hungerRateFactorOffset` across every hediff's `CurStage` directly — an entirely separate code path from the
generic `StatDef`/`StatPart` pipeline this mod's existing `StatPart_TattooEffectOffset` +
`IProvidesTattooStatOffset` (feature 002's Frost Sigil precedent) plugs into. There is no `StatDef` for
"hunger rate" a `StatPart` could intercept at all — `HungerRateMultiplier` doesn't exist in vanilla (confirmed
by grepping every Core XML def for it: zero hits; only `BedHungerRateFactor`, an unrelated bed-comfort stat,
exists). A real stage-based `Hediff` is therefore the only vanilla-native way to move this Need, and `painOffset`
being a sibling field on the same `HediffStage` means one companion hediff cleanly carries both costs at once,
scaled together per stack count. `Hediff.ShouldRemove => Severity <= 0f` means a stray `Severity = 0` would
self-clean even if the explicit `RemoveHediff` call were ever skipped, but the explicit call is used for
immediate, deterministic removal rather than waiting a tick.

**Alternatives considered**: Routing hunger through `IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset`
like Frost Sigil's move-speed slow — rejected once decompiling `Need_Food` showed hunger rate never reads any
`StatDef` this generic pipeline could intercept. A single non-staged hediff with severity-proportional values
computed in C# — rejected in favor of ordinary XML `stages`, keeping the actual per-stack numbers in
data (Constitution Principle III) rather than a formula buried in comp code.

## R5: Mood debuff — `Thought_Situational` + `ThoughtWorker`, mirroring Starlight Ward Tier 2 exactly (FR-015)

**Decision**: A new `ThoughtDef` (`TattooMagic_Thought_ShedscaleRegrowthActive`) with `thoughtClass
=TattooMagic.Thought_ShedscaleRegrowthActive` (overriding `BaseMoodOffset` via `TattooEffectValues`, negative)
and `workerClass=TattooMagic.ThoughtWorker_ShedscaleRegrowthActive` (`ActiveDefault` whenever the pawn's
Shedscale comp reports `ActiveRegrowthCount > 0`, else `Inactive`).

**Rationale**: This mod already has the exact shape needed — `ThoughtWorker_StarlightWardTier2Active` /
`Thought_StarlightWardTier2Active` (feature 013) implement a flat, non-stacking situational mood modifier
driven by live comp state with zero persisted mood-hediff bookkeeping, which is precisely FR-015's
requirement ("exactly one instance... regardless of how many parts are regrowing," auto-present/absent). No
new mechanism needed — only a second implementation of an already-proven pattern, with a negative
`baseMoodEffect` instead of Starlight Ward's positive one.

**Alternatives considered**: A companion mood-affecting `Hediff` (e.g. via `HediffStage.overrideMoodBase` or
`makesSickThought`) — rejected; `overrideMoodBase` *replaces* the pawn's entire mood baseline rather than
applying a bounded offset, and `makesSickThought` frames it as an illness-flavored thought this mod doesn't
otherwise use, where the existing `ThoughtWorker_Situational` pattern already fits perfectly and keeps mood
handling consistent with feature 013.

## R6: The Tier 1 reduced-efficiency condition — a per-part companion `Hediff` using `capMods`, mirroring `ParalyticAbasia`'s own shape

**Decision**: A new support `HediffDef` (`TattooMagic_Hediff_ShedscaleImperfectRegrowth`) carrying `stages[0]
.capMods` (`PawnCapacityDefOf.Manipulation`/`Moving` reduced toward the placeholder 75-85% range, scoped by
being attached to a specific `BodyPartRecord`). Added via `pawn.health.AddHediff(def, part)` immediately after
`RestorePart` completes a Tier 1 regrowth; removed via `pawn.health.RemoveHediff(...)` for every existing
instance the moment the wearer reaches Tier 2 (FR-018's retroactive clear).

**Rationale**: `Defs/HediffDefs/ParalyticAbasia.xml` (feature 014) already demonstrates this exact mechanism —
a hediff whose `stages[].capMods` reduces specific `PawnCapacityDef`s, standard vanilla modding for "this
hediff makes one body region work worse" (the same mechanism vanilla itself uses for old scars/wounds). Scoping
it per-`BodyPartRecord` (rather than a single flat whole-pawn stat) means multiple independently-regrown parts
each carry their own penalty and can each be individually cleared if that *specific* part is later lost and
regrown again post-Tier-2 (spec Edge Cases), without touching any other part's own instance.

**Alternatives considered**: A single whole-pawn severity-scaled hediff (like the Strain hediff, R4) covering
"how many parts are running under Tier 1 efficiency" as one number — rejected; FR-018's requirement to clear
the penalty from specific already-regrown parts, and the edge case of a re-lost-and-re-grown individual part,
both need per-part identity that one whole-pawn severity number can't carry.

## R7: Zero Harmony patches — a fully polling-based design, a first for a mechanically-rich tattoo in this mod

**Decision**: `HediffComp_ShedscaleEffect.CompPostTick` self-gates to once per in-game day (mirroring feature
014's `nextDaysAliveCheckTick` convention) and does everything by direct polling: re-scan `GetMissingPartsCommonAncestors()`
for newly-eligible parts to start tracking, advance each tracked part's elapsed-days counter (unless
malnourished, FR-014), complete any regrowth whose elapsed days reaches the tier duration via `RestorePart`
(R2), and maintain the Strain hediff (R4) and Tier 2 counters. No Harmony patch is introduced anywhere.

**Rationale**: Every other tattoo needing to react to an external event (surgery completing, a pawn dying, a
faction change) has needed a Harmony patch on that event (features 001, 007, 014). Shedscale's entire
"prosthetic blocks/unblocks regrowth" mechanic turned out to need no such hook at all (R1) — the daily poll of
`GetMissingPartsCommonAncestors()` already sees the *result* of an install or uninstall on its very next pass,
with no need to intercept the surgery bill itself mid-flight. This makes Shedscale the first mechanically rich
(non-trivial state machine, multiple companion hediffs, dual tier-2 conditions) tattoo in the mod's roster to
ship with zero new Harmony patches.

**Alternatives considered**: A Harmony postfix on `Recipe_InstallArtificialBodyPart.ApplyOnPawn`/`Recipe_RemoveBodyPart
.ApplyOnPawn` to react immediately rather than up to a day late — rejected as unnecessary complexity; nothing
in the spec requires same-tick reaction to a surgery bill (the regrowth timer itself is already measured in
days, so a same-day polling lag is imperceptible), and it would reintroduce exactly the kind of state R1 shows
is unnecessary.

## R8: Combat Extended — decompiled, confirmed clean

**Decision**: No CE-specific branch needed anywhere in this feature.

**Rationale**: Decompiling the real, locally installed `CombatExtended.dll` for every class touching
`BodyPart`/`MissingPart`/`HungerRate` finds exactly one overlap: `Harmony_Hediff_MissingPart_IsFresh_Patch`, a
prefix refining one `Hediff_MissingPart` boolean property (freshness-gated bleed nuance) — it does not touch
`HediffSet.GetMissingPartsCommonAncestors`, `HasDirectlyAddedPartFor`, or any of the structural queries this
feature relies on (R1/R3). CE has no hunger-rate override at all. This feature also introduces no targeting,
armor, or accuracy logic of any kind, so Constitution Principle IV (Two-Path Combat Patching) is not
applicable, same as every other non-Guardian's-Call tattoo.

**Alternatives considered**: N/A — this is a verification finding, not a design choice.

## R9: The prosthetic-block warning — a persistent `Alert`, scanning colonists directly (per clarification)

**Decision**: A new `Alert_ShedscaleRegrowthBlocked : Alert`, scanning
`PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended` each time it's
queried (vanilla re-queries active alerts on its own cadence, no extra polling needed) for any pawn with a
Shedscale tattoo who has at least one `Hediff_AddedPart` on a `BodyPartRecord` whose `def` is in either of the
Tier-scoped eligible lists (R3) — i.e., a part that would otherwise be eligible for regrowth if it were
missing instead of replaced. Returns `AlertReport.CulpritsAre(...)` over the matching pawns.

**Rationale**: Matches the spec's clarified answer (a persistent top-right alert, active every day the block
continues) and this mod's own existing `Alert_ReimplantationAvailable`/vanilla `Alert_Hypothermia` pattern —
`GetReport()`/`GetExplanation()` plus a cached scan list, no persisted state at all since the alert simply
re-evaluates live pawn/hediff state on every query, consistent with vanilla's own alert design.

**Alternatives considered**: A one-time `Messages.Message` at the moment a prosthetic is installed — rejected
per the clarification decision; a one-time message can't stay "active every day the block continues."

## R10: Removing the tattoo resets everything — free, by construction (per clarification)

**Decision**: No explicit "on tattoo removed" cleanup code is needed. All of Shedscale's own state (the
tracked-regrowth dictionary, the Tier 2 day-worn/parts-regrown counters) lives as plain fields on `HediffComp_
ShedscaleEffect` itself.

**Rationale**: `TattooHediffRemovalGuard.RemoveTattooHediff` (feature 001) — the sole sanctioned way this
mod's own code removes an applied tattoo hediff — calls ordinary `pawn.health.RemoveHediff(hediff)`, which
disposes the `Hediff` and every `HediffComp` on it, including all of its own instance fields. A colonist whose
Shedscale tattoo is removed and later re-applied receives a brand-new `HediffComp_ShedscaleEffect` with all
fields at their defaults — exactly the clarified behavior ("regrowth halts... a new tattoo restarts fresh,"
"Tier 2 progress clock resets... belongs to the current tattoo instance") with zero code written specifically
to produce it. The companion hediffs (Strain, per-part efficiency) are *not* themselves removed automatically
by the tattoo's own removal — they're separate `HediffDef`s on the pawn, not comps on the tattoo hediff — so
`HediffComp_ShedscaleEffect` needs one small explicit step on its own `CompPostPostRemoved` to clean those up
(remove the Strain hediff if present; leave any already-completed per-part efficiency hediffs alone, since
those represent already-finished regrowths the spec's Assumptions never says should be undone by losing the
tattoo later).

**Alternatives considered**: A dedicated Harmony patch or registry (like Phoenix's `GameComponent`) to detect
tattoo removal and explicitly reset state — rejected; nothing here needs cross-pawn or cross-session
coordination the way Phoenix's faction-wide cap did, so the ordinary `HediffComp` lifecycle already provides
exactly the reset semantics the clarification asked for.

## R11: Reporting dual-condition Tier 2 progress through `IProvidesTattooTierProgress`'s single-pair contract

**Decision**: Implement `IProvidesTattooTierProgress` directly on `HediffComp_ShedscaleEffect` (not via the
shared, composed `TattooTierProgress`, same reasoning as Phoenix, feature 014). `ProgressionCounter`/
`NextTierThreshold` report whichever of the two independent counters (days worn vs. parts regrown) is
proportionally closer to its own threshold at the moment queried — i.e., the leading "race."

**Rationale**: `TattooTierProgressUtility.DescribeTier` renders a single `X/Y` pair; Shedscale's two
thresholds (a day count and a part count) are different units racing each other, so summing or picking one
arbitrarily would misrepresent progress. Reporting the leading counter's own ratio gives an honest "how close
to Tier 2" readout matching the actual "whichever comes first" rule (FR-016), recomputed fresh on every read
rather than cached.

**Alternatives considered**: Always reporting the days-worn counter (ignoring parts-regrown) — rejected, would
misreport a colonist who is much closer via parts regrown. Composing `TattooTierProgress` and manually driving
its single `progressionCounter` from whichever race is leading — rejected; `TattooTierProgress
.TryRegisterQualifyingEvent` assumes one monotonic counter incrementing by exactly one per event, which
doesn't fit two differently-paced, independently-advancing counters cleanly (same shape mismatch that led
Phoenix to implement the interface directly rather than compose it).
