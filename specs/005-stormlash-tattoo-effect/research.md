# Phase 0 Research: Stormlash Tattoo Effect (Second Triggered Ability + Movement-Slow Immunity Slice)

Grounded against the actual installed game and mods rather than assumption: RimWorld 1.6's own `Core/Defs/Stats/*.xml`
and the reference assembly `Krafs.Rimworld.Ref` 1.6.4871 (`Assembly-CSharp.dll`), and a real, installed Combat
Extended copy (Steam Workshop item `2890901044`, `CETeam.CombatExtended`, v16.7.3.0, RimWorld 1.6 — the same
installed copy feature 003's research was grounded against). CE's `Defs/Stats/*.xml` were checked directly, and
`CombatExtended.dll` was inspected read-only via `System.Reflection.MetadataLoadContext` plus decompilation
(`ilspycmd`) of the specific two classes this feature's Tier 2 immunity depends on — no game code executed.

## R1: Reusing feature 004's gizmo/cooldown reuse point for a second, unrelated consumer

**Decision**: `HediffComp_StormlashEffect` implements `IProvidesTattooGizmo` exactly as
`HediffComp_GuardiansCallEffect` does — a plain `Command_Action` gizmo whose `action` calls the comp's own
`TryActivate()`, and whose `disabled`/`disabledReason` are computed live from a stored `cooldownEndTick` field
each time `GetGizmo()` is called. No change to `IProvidesTattooGizmo` or `Patch_Pawn_GetGizmos_TattooGizmos`
(feature 004) is needed or made — this feature's entire contribution to that reuse point is a second
implementing `HediffComp`, added the same way a second `IOnMeleeHitTattooEffect` implementer could be added to
feature 002's dispatch patch without touching it.

**Rationale**: This is precisely the validation spec User Story 3 asks for — proving the interface+shared-patch
pattern generalizes to "a self-buff with no targeting or taunt behavior at all," a genuinely different shape of
ability than Guardian's Call's taunt. A pawn with both tattoos gets two independent `Command_Action` gizmos from
two independent comps; the shared patch's per-comp iteration (feature 004 R1) already guarantees no interaction
between them.

**Alternatives considered**: n/a — this is confirmed, unmodified reuse, not a design choice requiring
alternatives.

## R2: Which StatDefs represent "move speed" and "attack speed," and how are they offset?

**Decision**:

- **Move speed** → `StatDefOf.MoveSpeed`, already registered with `StatPart_TattooEffectOffset` since feature
  002 (Frost Sigil's slow proc already offsets it negatively on an attacker). Stormlash's boost is a **positive**
  offset via the same, already-registered StatPart — no new registration needed.
- **Attack speed** → confirmed via `Krafs.Rimworld.Ref`'s `RimWorld.StatDefOf` (reflected directly, not assumed)
  and RimWorld Core's `Defs/Stats/Stats_Pawns_Combat.xml`: `StatDefOf.MeleeCooldownFactor` ("A multiplier on the
  time this creature takes to recover after making a melee attack", default `1`, `minValue 0.05`) and
  `StatDefOf.RangedCooldownFactor` ("A multiplier on the cooldown between bursts when using a ranged weapon",
  default `1`, `minValue 0.01`) — both pawn-level multipliers, not per-weapon stats. A **negative** offset (e.g.
  `-0.15`) lowers the multiplier below `1.0`, i.e. faster attacks; RimWorld's own post-parts clamp to
  `minValue`/`maxValue` prevents this from ever going non-positive regardless of how many tattoos/effects stack
  onto it. `StatDefOf.AimingDelayFactor` ("how long it takes to shoot after choosing a target") is deliberately
  **excluded** — it governs pre-first-shot aim delay, not the "time between attacks" cadence PRD §6's "attack
  speed" describes (spec Assumptions).

`TattooEffectStatPartInstaller` (feature 002, shared/reusable file) gains two new `Register(...)` calls for
`MeleeCooldownFactor` and `RangedCooldownFactor` — the same kind of addition Guardian's Call's R6 made for the
three armor StatDefs, not a modification to any other tattoo's own effect file.

**Rationale**: Matches the codebase's established "offset a plain multiplier/additive StatDef via the shared
generic StatPart" pattern exactly; XML fields carry the positive user-facing magnitude (e.g. "15% faster"),
negated internally at the `GetStatOffset` call site, mirroring `HediffComp_FrostSigilEffect`'s
`ComfyTemperatureMin` offset convention.

**Alternatives considered**: Offsetting `RangedWeapon_Cooldown`/a per-weapon stat instead (rejected — that's a
`ThingDef`-scoped weapon stat, not a pawn stat; offsetting it would require per-weapon-instance bookkeeping for
no benefit over the existing pawn-level multiplier); including `AimingDelayFactor` (rejected per Rationale
above — conflates aim-acquisition delay with attack cadence, which PRD's "attack speed" doesn't ask for).

## R3: Does Combat Extended replace or relabel `MeleeCooldownFactor`/`RangedCooldownFactor`, the way it relabels `ShootingAccuracyPawn` (feature 003 R4)?

**Decision**: No. Confirmed by grepping the installed CE copy's entire `Defs/` and `Patches/` trees for either
StatDef's `defName` — zero matches for both. Unlike `ShootingAccuracyPawn` (which CE explicitly relabels to
"weapon handling" and repurposes for sway/recoil, feature 003 R4), CE leaves `MeleeCooldownFactor` and
`RangedCooldownFactor` completely untouched — no patch, no replacement `<workerClass>`, no reinterpreted
description. Therefore Stormlash's attack-speed boost needs **zero CE-specific branching**: the same offset on
the same two StatDefs is correct in both configurations.

**Rationale**: Same finding shape as Guardian's Call's R6 (armor StatDefs also untouched by CE) — not every
combat stat this mod touches turns out to need CE branching; confirming that concretely (rather than assuming
either way) is exactly what Constitution Principle I requires before shipping a "works with CE" claim.

**Alternatives considered**: n/a — this is a confirmed negative finding (no CE-specific behavior exists to
branch on), not a design choice.

## R4: What Combat Extended's "suppression-slow" actually is, and why Tier 2 immunity can't just be a bigger `MoveSpeed` offset

**Decision**: Grounded via `MetadataLoadContext` reflection plus `ilspycmd` decompilation of the two specific CE
classes involved, in the installed v16.7.3.0 assembly:

- CE installs its own `CombatExtended.StatWorker_MoveSpeed : RimWorld.StatWorker` as the **worker** for vanilla's
  `MoveSpeed` StatDef (the StatDef itself is unchanged — CE replaces how its *unfinalized* value is computed,
  not the StatDef identity, so `StatDefOf.MoveSpeed` remains the correct target either way). Its
  `GetValueUnfinalized` calls the vanilla base computation, then multiplies the result by a private
  `GetStatFactor(Thing)` helper.
- `GetStatFactor` includes `num *= 0.67f` whenever `CompSuppressable.IsCrouchWalking` is `true` for that pawn.
  Decompiled source: `IsCrouchWalking => CanReactToSuppression && isCrouchWalking`, where `isCrouchWalking` is
  set `true` inside `CompSuppressable`'s suppression-reaction logic specifically when the pawn is suppressed
  above threshold **and** neither fleeing to cover (`RunForCover` job) nor hunkering down — i.e. exactly the
  "holding position/fighting under fire" scenario a Stormlash-boosted pawn charging through enemy fire is most
  likely to hit.
- **Why a bigger positive `MoveSpeed` offset alone cannot produce true immunity**: RimWorld's stat pipeline
  computes `GetValueUnfinalized` (where CE's `*0.67f` lives) **before** applying `StatDef.parts` (where this
  mod's `StatPart_TattooEffectOffset` additive offset lives). A positive offset therefore stacks on top of an
  *already-reduced* base value — it mitigates the slow's effect but never cancels it, and cannot satisfy
  FR-007/SC-007's "unaffected by it," only "less affected."

Decision: a new CE-only, imperatively-applied Harmony patch —
`Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity` — following
`Patch_CE_ProjectileImpact_TattooAmmoBonus`'s established convention (`AccessTools.TypeByName`/
`PropertyGetter`, gated behind `CombatExtendedInterop.IsLoaded`, applied from `TattooMagicMain`'s static
constructor since `CompSuppressable` isn't a compile-time reference) — postfixes `CompSuppressable`'s
`IsCrouchWalking` **property getter**, forcing `__result = false` whenever the comp's owning pawn currently has
an active Tier 2 Stormlash boost. This neutralizes the `0.67f` multiplier at its source instead of trying to
out-add it, so the pawn's `MoveSpeed` comes out exactly as if never suppression-slowed for that component.

**Rationale**: Targeting the narrow `IsCrouchWalking` getter (rather than `AddSuppression`, `isSuppressed`, or
reimplementing `GetStatFactor`'s multiplier logic) neutralizes only the movement-speed consequence of
suppression — leaving mental-break risk, cover-seeking AI, hunkering, sway, and suppression motes/UI completely
untouched, matching spec FR-007's explicit scope ("without affecting the pawn's intrinsic movement-speed
factors" / immunity is about movement, not suppression as a whole, per the spec's own Assumptions). It's also
the most stable target available: a single public property getter is less likely to change shape across CE
point releases than pinning to a private helper method's exact multiplier formula.

**Alternatives considered**: Patching `AddSuppression`/`isSuppressed` to block suppression outright (rejected —
would also suppress mental-break risk and cover-seeking behavior, which the spec explicitly excludes from
"movement-slow immunity"); patching `StatWorker_MoveSpeed.GetStatFactor` or `GetValueUnfinalized` directly
(rejected — a private, more implementation-specific method than a public property, and duplicating/patching CE's
broader stat-factor formula risks also touching the unrelated carried-weight/encumbrance factor that same
method computes); computing a compensating multiplicative offset instead of a flat additive one (rejected — a
flat `StatPart` offset cannot correctly cancel a multiplicative `0.67f` factor across pawns with differing
baseline `MoveSpeed` values, so it would only be exactly correct for one specific baseline).

**Assumption requiring in-game confirmation (Principle II)**: Like feature 004's R5, this is grounded in static
decompilation of one specific installed CE version, not live behavior tracing. `quickstart.md`'s CE-loaded pass
must confirm in-game that a Tier 2 Stormlash pawn actively under suppression fire shows a genuinely unreduced
`MoveSpeed` (not merely a higher one), not just that the patch applies without a Harmony error.

## R5: Does Tier 2 immunity also need to cover this mod's own in-mod movement-slow source (Frost Sigil), and how, without modifying Frost Sigil's file?

**Decision**: Yes. Frost Sigil's attacker-side slow (`HediffComp_FrostSigilSlow`, feature 002) is the only
concretely identifiable, testable "externally-inflicted movement-slow effect" available in a **non-CE** game —
and spec Edge Cases require genuine immunity ("the wearer's movement speed is unaffected by it"), not merely a
big enough boost to still net positive. A new interface, `IGrantsStatOffsetImmunity`
(`bool BlocksNegativeOffsets(StatDef stat)`), is added to `Source/Effects/`, implemented by
`HediffComp_StormlashEffect` — returning `true` for `StatDefOf.MoveSpeed` only while `tierState.tier >= 2` and a
boost window is currently active, `false` for every other stat and every other state.

`StatPart_TattooEffectOffset.GetOffset` (feature 002, shared/tattoo-agnostic file) is extended with a small,
still-generic two-pass shape: first check whether *any* comp on the pawn currently reports
`BlocksNegativeOffsets(parentStat) == true`; if so, clamp each individual contributing comp's own
`GetStatOffset(parentStat)` to `Mathf.Max(0f, offset)` before summing, instead of summing raw values. The
`StatPart` itself still carries no knowledge of Stormlash or Frost Sigil by name — it only knows "some comp is
currently vetoing negative contributions to this stat," exactly the same shape `IProvidesTattooStatOffset`
itself already uses ("some comp contributes to this stat").

**Rationale**: Satisfies genuine immunity for the one concrete in-mod source without either tattoo needing
special-case knowledge of the other (mirrors the stacking-without-coupling principle Guardian's Call's FR-007
already established for *positive* contributions; this is the same idea applied to *blocking* negative ones).
Keeping the veto stat-scoped (`MoveSpeed` only, not "block all negative offsets to any stat") preserves normal
stacking everywhere immunity wasn't specifically granted — e.g. a future tattoo's accuracy penalty on the same
pawn is untouched.

**Alternatives considered**: Stormlash's `GetStatOffset` directly inspecting the pawn's hediffs for a
`HediffComp_FrostSigilSlow` and neutralizing it inline (rejected — couples Stormlash to Frost Sigil's concrete
implementation type, breaking the "no tattoo needs special-case knowledge of any other specific tattoo"
principle); modifying `HediffComp_FrostSigilSlow` itself to check for a Stormlash-granted flag (rejected —
FR-011 forbids modifying any prior feature's own tattoo-specific file for this feature, and it's the wrong
direction of coupling regardless — an earlier tattoo should never need to know about one shipped after it).

**Scope note**: This mechanism cannot, and does not attempt to, guarantee immunity to some unrelated
third-party mod's own movement-slow implementation that never routes through `IProvidesTattooStatOffset`/
`StatPart_TattooEffectOffset` at all — only to (a) this mod's own in-mod slow source (Frost Sigil, via the
interface above) and (b) Combat Extended's specific, identified suppression-slow mechanism (R4). This matches
spec's Assumptions ("externally-inflicted... including CE's suppression-slow") without promising more than this
codebase can concretely detect and counteract, consistent with Constitution Principle II. `quickstart.md`'s
Scenario 4 exercises exactly this Frost-Sigil-vs-Stormlash-Tier-2 interaction as the non-CE immunity proof.

## R6: How is boost/cooldown state tracked, given it must survive save/reload?

**Decision**: Identical shape to Guardian's Call's R2 — `HediffComp_StormlashEffect` holds two plain `int` tick
fields, `cooldownEndTick` and `boostEndTick`, both `Scribe_Values.Look`'d from `CompExposeData()`, plus a
composed `TattooTierProgress tierState` (feature 002, unmodified) handling tier/progression-counter persistence.
`TryActivate()` sets both fields from `Find.TickManager.TicksGame` plus accessor-resolved durations for the
comp's *current* tier at the moment of activation (so an activation just before a Tier 1→2 upgrade uses Tier
1's numbers, matching Guardian's Call's precedent); every consumer (`GetGizmo()`'s disabled check,
`GetStatOffset`'s active-window check, `BlocksNegativeOffsets`'s active-window check) compares against the
stored tick on demand rather than polling per-tick.

**Rationale**: Directly reuses a pattern already proven correct and Scribe-safe by feature 004; no new
persistence mechanism or `CompPostTick` polling is needed since nothing about Stormlash's effect requires
per-tick side effects (unlike Guardian's Call's taunt, which had to actively re-assert itself against AI
drift — Stormlash only ever needs to answer "is my boost currently active" on demand).

**Alternatives considered**: A transient companion hediff for the boost window, mirroring Frost Sigil's
attacker-side slow hediff (rejected — the boost lives on the *wearer's own* comp, not granted to a separate
target pawn, so a second hediff would be pure indirection around the same two ticks with no benefit, identical
reasoning to Guardian's Call's R2).

## R7: Testing strategy

**Decision**: No automated test project, unchanged from features 001–004 (RimWorld's `Verse`/`RimWorld` types,
and specifically `CompSuppressable`'s live suppression state, aren't practically runnable outside the game
process). Verification is manual, in Dev Mode, per `quickstart.md`, with two mandatory (not optional) passes:
a non-CE pass exercising the Frost-Sigil-vs-Stormlash-Tier-2 interaction (R5), and a CE-loaded pass exercising
genuine suppression-slow immunity under live suppression fire (R4) — both are load-bearing for this feature's
own defining Tier 2 claim, not just compatibility checks on stats CE happens to leave alone.

**Rationale**: Unchanged reasoning from prior features' own R7/R6 testing-strategy sections; Constitution
Principle II sets in-game verification as the actual bar for "done," and both of this feature's immunity claims
(R4, R5) are exactly the kind of live game-state interaction a standalone unit test cannot exercise.

**Alternatives considered**: n/a — same reasoning as features 001–004.

## R8 (discovered during live verification, not anticipated at planning time): `StatDef` immutability caching silently defeats a `StatPart`-based offset unless explicitly cleared

**Decision**: Manual Scenario 1 testing found that `RangedCooldownFactor`'s Tier 1 offset silently never applied
in-game, while the identically-implemented `MeleeCooldownFactor` offset worked correctly — despite both being
registered the same way in `TattooEffectStatPartInstaller`. Root-caused by decompiling the actual shipped
`Assembly-CSharp.dll` (not the stripped `Krafs.Rimworld.Ref` reference assembly, which has no method bodies):
`RimWorld.StatDef.SetImmutability()` runs during game startup, before any mod's `[StaticConstructorOnStartup]`
registrations execute, and marks any `StatDef` with no existing `<parts>` in XML — and no other loaded Def
content (a `HediffStage`'s `statOffsets`, an `IdeoRole` effect, a quality `StatModifier`, etc.) referencing it —
as `immutable = true`. Once marked, `StatWorker.GetValue` takes a different path: it checks a per-pawn
`immutableStatCache` dictionary first and, if already populated, returns that cached value **forever**, with no
TTL and no invalidation trigger — `stat.parts` (and therefore `StatPart_TattooEffectOffset`, and therefore any
tattoo's `IProvidesTattooStatOffset` contribution) is never consulted again for that pawn/stat pair.
`RangedCooldownFactor` has no vanilla `<parts>` and, checked explicitly, nothing else in Core, the one active
DLC (Anomaly), or the customer's other installed mods (Dogs Don't Die, Simple Crafting Quality, Dubs Performance
Analyzer, TrueTerrainColors) references it anywhere — so it got locked into the immutable/cached state before
Stormlash's registration ever ran. `MeleeCooldownFactor` only escaped this by coincidence (something else in the
loaded content already references it, independently of anything this mod does), and `MoveSpeed`/`ArmorRating_*`/
`ShootingAccuracyPawn` from features 002–004 were never at risk for the identical reason — this is the first
`StatDef` this mod has registered against with no such lucky exemption already in place from other content.

Fix: `TattooEffectStatPartInstaller.Register()` now explicitly does `stat.immutable = false;` and
`stat.Worker.DeleteStatCache();` immediately after adding the `StatPart`, for **every** stat it registers, not
only the two new attack-speed ones — this makes every past and future registration through this shared
installer correct regardless of whether the target stat happens to have some other coincidental dynamic
reference elsewhere, rather than continuing to depend on that coincidence holding.

**Rationale**: This is exactly the kind of runtime-only, non-guessable-from-documentation behavior Constitution
Principle II's "verify before claiming done" exists to catch — static analysis of this feature's own code could
never have surfaced it, since the bug lives entirely in how `StatDef`'s *own* framework-level optimization
interacts with a C#-runtime-added (not XML-declared) `StatPart`, a combination none of features 002–004 happened
to exercise. Fixing it in the shared `Register()` helper (rather than special-casing `RangedCooldownFactor`
alone) protects every tattoo this mod has already shipped and every one it ships in the future, since any of
them could have silently hit the same bug on any StatDef that doesn't happen to have other coincidental dynamic
content already referencing it.

**Alternatives considered**: Special-casing only `RangedCooldownFactor` in `HediffComp_StormlashEffect` itself
(rejected — would fix only the one symptom this session happened to catch, leaving every other registered stat,
past and future, still silently exposed to the same class of bug on any RimWorld/mod-load-order combination
where its coincidental exemption doesn't hold); relying on `cacheStaleAfterTicks` at each call site instead of
clearing `immutable` at the source (rejected — `immutable`-marked stats route through `immutableStatCache`
*before* the `cacheStaleAfterTicks` branch is ever reached, so a caller-side TTL parameter has no effect on an
immutable stat at all; the fix has to happen at the `StatDef`/`StatWorker` level, not the call site).
