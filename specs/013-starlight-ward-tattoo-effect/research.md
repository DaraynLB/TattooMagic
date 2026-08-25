# Phase 0 Research: Starlight Ward Tattoo Effect

All findings below are grounded against the real, locally installed game code — `Assembly-CSharp.dll` and
`Data/Core/Defs/**` (RimWorld 1.6, decompiled read-only via `ilspycmd`, no game code executed) and the real,
locally installed `CombatExtended.dll` (Steam Workshop item `2890901044`) — mirroring features 007/010/011/012's
own decompile-to-verify approach, not assumed.

## R1: How vanilla's mental-break threshold actually works, and how to grant resistance to it

**Decision**: Starlight Ward's mental-break resistance is a negative offset to the existing vanilla StatDef
`StatDefOf.MentalBreakThreshold`, delivered through this mod's existing `IProvidesTattooStatOffset` /
`StatPart_TattooEffectOffset` pipeline (feature 002) — no new interface, no new patch.

**Rationale**: Decompiling `Verse.AI.MentalBreaker` shows `BreakThresholdMinor`/`BreakThresholdMajor`/
`BreakThresholdExtreme` are all derived, every time they're read, from a single call:
`pawn.GetStatValue(StatDefOf.MentalBreakThreshold, applyPostProcess: true, 5)` (scaled by a fixed fraction per
intensity — 1x/0.571x/0.143x). A pawn is "at risk" of a break of a given intensity once `CurMood` (0–1) drops
below that intensity's threshold. **Lower is better**: this is the mood value *below* which a break becomes
possible, so reducing it makes a pawn harder to break, not easier — the opposite direction from every other
stat-offset tattoo shipped so far (armor, speed, accuracy), where higher is better. Confirmed in
`Data/Core/Defs/Stats/Stats_Pawns_General.xml`: `MentalBreakThreshold` is **Core**-only (not Royalty/Ideology-
gated), `defaultBaseValue=0.35`, `minValue=0.01`, `maxValue=0.50`. Because vanilla's own `StatWorker` clamps to
`[minValue, maxValue]` after all `StatDef.parts` (including ours) run, no manual clamping is needed in this
feature's own code — a placeholder-sized negative offset (well under the 0.01–0.50 range's width) cannot push the
stat out of bounds even stacked with other traits/hediffs that also offset it (e.g. vanilla's own "Nerves of
Steel"/"Very Nervous" traits).

**Alternatives considered**: A dedicated Harmony patch on `MentalBreaker`'s threshold properties — rejected as
pure unneeded surface area; the existing generic stat-offset pipeline already reaches any `StatDef` once
registered (R3), identical in shape to how Ember Ward/Ironskin Glyph deliver their own bonuses.

## R2: Psychic-sensitivity resistance

**Decision**: A second negative offset, to the existing vanilla StatDef `StatDefOf.PsychicSensitivity`, through the
same pipeline.

**Rationale**: Confirmed in the same Core XML file: `PsychicSensitivity` (`defaultBaseValue=1`, `minValue=0`, no
`maxValue`) is also Core-only, not gated behind Royalty or Ideology — it already has vanilla `<parts>` entries
(`StatPart_GearStatOffset`/`StatPart_GearStatFactor` for apparel), which our own registered
`StatPart_TattooEffectOffset` composes with additively (each `StatPart` in the list runs in sequence; nothing
about registering a new one disturbs the existing ones). Lower sensitivity means the pawn suffers less from
negative psychic effects — the resistance direction PRD §6 asks for.

**Alternatives considered**: None seriously entertained — this is the same established, generic mechanism as R1,
just a second `StatDef`.

## R3: Wiring the two new stats into the existing stat-offset pipeline

**Decision**: Add exactly two lines to `TattooEffectStatPartInstaller`'s static constructor —
`Register(StatDefOf.MentalBreakThreshold)` and `Register(StatDefOf.PsychicSensitivity)` — and nothing else.
`StatPart_TattooEffectOffset` itself needs zero changes; it already sums any comp implementing
`IProvidesTattooStatOffset` for whichever stat it's registered against, generic since feature 002.

**Rationale**: Confirmed no existing `HediffComp` implements `IGrantsStatOffsetImmunity` for either stat — only
`HediffComp_StormlashEffect` implements that interface at all, and per its own contract
(`specs/005-stormlash-tattoo-effect/contracts/stat-offset-immunity-contract.md`) it only vetoes negative
contributions to `MoveSpeed`, scoped per-stat by design. Starlight Ward's own negative contributions to
`MentalBreakThreshold`/`PsychicSensitivity` are therefore never at risk of being clamped away by that mechanism.

## R4: Detecting "a mental-break risk event resisted" (the progression counter)

**Decision**: Self-contained polling inside `HediffComp_StarlightWardEffect.CompPostTick`, gated to check roughly
once per second (mirroring the existing self-gated-interval pattern `HediffComp_VampiricThornEffect` already uses
for its post-kill heal ticking — no new tick-dispatch mechanism). Each check reads three already-`public`
properties on `Pawn.mindState.mentalBreaker` (`Verse.AI.MentalBreaker`, decompiled): `BreakMinorIsImminent`,
`BreakMajorIsImminent`, `BreakExtremeIsImminent` — plus `Pawn.InMentalState`. Define a **risk window** as a
contiguous span of checks where any of the three `*IsImminent` properties is true. A risk window counts as a
**resisted risk event** — and registers one `TryRegisterQualifyingEvent` call — only if the window closes (all
three go false again) *without* `pawn.InMentalState` ever having been observed true during that window. If a break
did occur mid-window, the window is not counted (it wasn't resisted — it happened).

**Rationale**: This needs no Harmony patch at all — every piece of state involved is already `public` on vanilla
types. Critically, `BreakThresholdMinor`/`Major`/`Extreme` (R1) already read `pawn.GetStatValue
(StatDefOf.MentalBreakThreshold, ...)` fresh on every access, so they automatically reflect Starlight Ward's own
stat offset with no extra work — whether and how long a risk window even opens already has this tattoo's own
effect baked in, without needing to separately reconstruct a "what would the threshold have been without this
tattoo" counterfactual (which would be fragile against whatever *other* traits/hediffs/mods also offset the same
stat on that pawn). Counting per-*window* rather than per-check avoids the counter inflating every ~second a pawn's
mood merely sits below threshold — directly satisfying spec FR-009 ("no risk, no count") and keeping this
counter's accrual rate in the same rough order of magnitude as every other passive tattoo's per-instance counter,
rather than a runaway per-tick one. This also settles spec User Story 3's open question: because no other tattoo
in the roster needs to observe mental-break state, this detection logic is written as private, self-contained
polling inside Starlight Ward's own comp — not a new shared interface/contract — consistent with how Vampiric
Thorn's and Ember Ward's own tattoo-specific mechanics (post-kill window, CE-gated reduction) stayed private to
their own comps without forcing a premature shared abstraction nothing else consumes yet.

**Alternatives considered**:
- A Harmony patch on `MentalBreaker.MentalBreakerTickInterval` (or `TestMoodMentalBreak`) — rejected; all needed
  state is already `public`, so a patch would only add fragility against future game updates for no behavioral
  gain over direct polling.
- Counting every ~150-tick vanilla check interval while imminent, rather than per-window — rejected: produces
  runaway accrual for a pawn sitting unhappy for an extended time, and doesn't match "an event" (a singular
  occurrence) as PRD §6 phrases it.
- Reconstructing "would this pawn have broken under the *unbuffered* vanilla threshold" — rejected as needlessly
  complex and fragile (would require subtracting this tattoo's own offset back out of a stat multiple other things
  can also offset) when the window-based approach already ties directly and simply to genuine risk exposure.

## R5: The Tier 2 passive mood buff

**Decision**: A new situational `ThoughtDef` (`workerClass` pointing at a small new `ThoughtWorker` subclass,
`thoughtClass` pointing at a small new `Thought_Situational` subclass) that is active only while the wearer's
Starlight Ward tattoo is at Tier 2. The `Thought_Situational` subclass overrides `MoodOffset()` to read the bonus
through `TattooEffectValues` (FR-011), with the `ThoughtDef`'s own `stages[0].baseMoodEffect` XML value used only
as that accessor's default fallback — the same "XML value is the default, `TattooEffectValues.Get` is the actual
read point" shape every other tattoo's numeric fields already use.

**Rationale**: Decompiling `RimWorld.ThoughtUtility.Reset()` confirms `situationalNonSocialThoughtDefs` is built
once from `DefDatabase<ThoughtDef>.AllDefs.Where(x.IsSituational && !x.IsSocial)` — i.e. *any* loaded `ThoughtDef`
with a situational `workerClass` is automatically evaluated for every pawn each mood recompute
(`SituationalThoughtHandler.UpdateAllMoodThoughts`), with zero per-pawn registration needed. This is exactly how
vanilla's own "Outside"/"EnvironmentDark"/"ApparelDamaged" thoughts work
(`Data/Core/Defs/ThoughtDefs/Thoughts_Situation_General.xml`) — confirmed by inspecting their XML, which sets only
`workerClass` + `stages`, no `thoughtClass` override (defaulting to `Thought_Situational`), no explicit
per-pawn wiring anywhere.

Vanilla's own `RimWorld.ThoughtWorker_Hediff` was considered and rejected as a direct fit: decompiling it shows it
keys its active stage off the parent hediff's own `CurStageIndex` (i.e. `HediffDef.stages` + the hediff's own
`Severity`). This mod's tier state has deliberately lived *only* inside the owning `HediffComp`'s composed
`TattooTierProgress.tier` field since feature 002, never mirrored onto the hediff's own `Severity`/stage for any
tattoo shipped so far — introducing `<stages>` on `TattooMagic_Hediff_StarlightWard` purely to make
`ThoughtWorker_Hediff` fit would create a second, parallel "what tier is this tattoo" representation that could
drift out of sync with the comp's own `tierState`, and every other feature's tier logic, UI reads
(`ITab_Pawn_Tattoos` via `IProvidesTattooTierProgress`), and save data all already assume the comp is the single
source of truth. A tiny custom `ThoughtWorker` that reads `HediffComp_StarlightWardEffect.Tier` directly off the
pawn's own hediof comps (a private, in-feature lookup — mirroring how `StatPart_TattooEffectOffset` already scans
a pawn's comps by interface/type) keeps that single-source-of-truth property intact at the cost of a handful of
lines.

**Alternatives considered**:
- Reusing `ThoughtWorker_Hediff` as-is by adding `<stages>` to the `HediffDef` and mirroring tier onto `Severity`
  — rejected for the single-source-of-truth reason above.
- Leaving the mood buff as a fixed XML `baseMoodEffect` with no `TattooEffectValues` routing — rejected as an
  unnecessary, easily-avoided exception to FR-011/Constitution Principle III; the override needed to route it
  through the accessor is trivial (one method).

## R6: Combat Extended has no interaction with either stat

**Decision**: No CE-specific code path is needed anywhere in this feature — Starlight Ward is the first tattoo in
the roster where this is true.

**Rationale**: PRD §7's CE-integration requirements are scoped to combat stats (accuracy, armor, speed,
suppression, ammo/loadout) — mental-break threshold and psychic sensitivity aren't among them. Confirmed by fully
decompiling the real, locally installed `CombatExtended.dll` (Steam Workshop item `2890901044`) and searching
every resulting file: zero references to `MentalBreakThreshold` or `PsychicSensitivity` anywhere in CE's own code.
Unlike every prior passive combat-stat tattoo (Frost Sigil, Serpent's Eye, Ember Ward, Ironskin Glyph), this
feature's `GetStatOffset` implementation needs no `CombatExtendedInterop.IsLoaded` branch and no direct
`dinfo.Amount` reduction path — the vanilla stat-offset alone is the complete, correct implementation in both
configurations. The mod must still load cleanly with CE present (Constitution Principle I's general regression
bar), which requires no special handling here since nothing this feature touches is CE-aware content.

## R7: DLC independence

**Decision**: No DLC-conditional logic anywhere in this feature.

**Rationale**: Both `StatDefOf.MentalBreakThreshold` and `StatDefOf.PsychicSensitivity` are declared as static
fields directly on `RimWorld.StatDefOf` (not inside any DLC-gated `DefOf` class), and confirmed to be defined by
XML that lives in `Data/Core/Defs/Stats/Stats_Pawns_General.xml` — Core, not `Data/Royalty` or `Data/Ideology`.
Both fields are therefore always non-null regardless of which DLCs are active, matching spec FR-014.
