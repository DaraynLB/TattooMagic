# Implementation Plan: Starlight Ward Tattoo Effect (Ninth Passive Tattoo, Mental Resilience Slice)

**Branch**: `013-starlight-ward-tattoo-effect` | **Date**: 2026-08-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/013-starlight-ward-tattoo-effect/spec.md`

## Summary

Turn Starlight Ward from an inert stub Hediff (shipped by feature 001) into a working PRD §6 tattoo — the **last**
of the 11-tattoo v1 roster to get its effect implemented (spec SC-008). Tier 1 grants a permanent negative offset
to `StatDefOf.MentalBreakThreshold` and `StatDefOf.PsychicSensitivity` (research.md R1/R2 — lower is the resistant
direction for both, the inverse of every prior stat-offset tattoo), upgrading in place to stronger offsets plus a
Tier 2-exclusive passive mood buff, via a "mental-break risk events resisted" progression counter (PRD §5.4)
tracked entirely by self-contained polling inside the tattoo's own `HediffComp` — no Harmony patch, since every
piece of vanilla state needed (`Verse.AI.MentalBreaker`'s `Break*IsImminent` properties, `Pawn.InMentalState`) is
already `public` (research.md R4). Reuses feature 002's value accessor (`TattooEffectValues`), tier tracker
(`TattooTierProgress`), and stat-offset contract (`IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset`)
entirely unmodified, needing only two new `TattooEffectStatPartInstaller` registrations (research.md R3). This is
the ninth passive tattoo and the first whose qualifying event isn't damage-based and the first to touch mood at
all — its Tier 2 mood buff is delivered through a small new situational `ThoughtDef`/`ThoughtWorker`/
`Thought_Situational` trio, grounded in how vanilla's own environment thoughts (e.g. "EnvironmentDark") already
work, kept intentionally private to this feature rather than a new shared contract since nothing else in the
roster needs it (research.md R5). Unlike every prior combat-stat tattoo, this feature needs **zero** Combat
Extended-specific code — confirmed by fully decompiling the real, locally installed `CombatExtended.dll` and
finding no reference to either stat anywhere in CE's own code (research.md R6) — and needs no DLC-conditional
logic, since both stats are Core-only (research.md R7).

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–012 and
this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (zero new patches — this feature's event detection is self-contained
`HediffComp` polling of already-public vanilla state, research.md R4, and its mood buff is a vanilla-native
`ThoughtDef` mechanism, research.md R5), `Krafs.Rimworld.Ref` 1.6.* — same as features 001–012, no new external
dependency, no DLC dependency. Combat Extended interaction confirmed **absent** by fully decompiling the real,
locally installed `CombatExtended.dll` (Steam Workshop item `2890901044`) and finding zero references to
`MentalBreakThreshold` or `PsychicSensitivity` anywhere in CE's own code (research.md R6) — the first tattoo in the
roster where this is true.

**Storage**: RimWorld's built-in Scribe save/load system — Starlight Ward's tier, progression counter, and
risk-window tracking state (`inRiskWindow`/`riskWindowHadBreak`/`nextRiskCheckTick`) persist as `HediffComp` state
on the existing `TattooMagic_Hediff_StarlightWard` hediff, the same shape every prior tattoo's own comp already
uses, extended with three small additional `Scribe_Values` fields for the window-tracking state this feature's
event-detection approach needs (data-model.md).

**Testing**: No automated unit-test harness (unchanged from features 001–012 — RimWorld's `Verse`/`RimWorld` game
types aren't practically runnable outside the game process); validated via manual in-game verification in Dev Mode
per Constitution Principle II, using `quickstart.md`. Unlike prior features, no CE-loaded pass is expected to
surface any CE-specific behavior difference (research.md R6) — Scenario 8's CE check is a load-cleanliness/
no-regression pass, not a CE-specific-fix verification the way Ironskin Glyph's or Ember Ward's own CE scenarios
were.

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime). `Verse.AI.
MentalBreaker` (its `Break*IsImminent` properties and their dependence on `pawn.GetStatValue(StatDefOf.
MentalBreakThreshold)`), `RimWorld.StatDefOf.MentalBreakThreshold`/`PsychicSensitivity` (confirmed Core-only via
`Data/Core/Defs/Stats/Stats_Pawns_General.xml`, research.md R7), `RimWorld.ThoughtUtility.situationalNonSocial
ThoughtDefs`'s auto-enumeration (research.md R5), and Combat Extended's own compiled code (confirmed to never
reference either stat) were all grounded against the real, installed `Assembly-CSharp.dll` and
`CombatExtended.dll` (decompiled read-only via `ilspycmd`, no game code executed) rather than assumed, mirroring
features 007/010/011/012's own decompile-to-verify approach.

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–012; no new project/scaffold needed.

**Performance Goals**: `HediffComp_StarlightWardEffect.CompPostTick` self-gates its own risk-window check to
roughly once per second (a small internal tick-counter constant, mirroring `HediffComp_VampiricThornEffect`'s own
existing self-gated interval-check pattern for its post-kill heal), not every tick — cheap relative to the
existing per-tick `CompPostTick` dispatch every `HediffWithComps` pawn already receives. The two new
`MentalBreakThreshold`/`PsychicSensitivity` stat offsets add no new per-tick cost, queried by the existing
`StatPart` machinery only when something reads those stats, same as every other tattoo's stat offset. The new
`ThoughtDef`'s situational evaluation runs through vanilla's own existing `SituationalThoughtHandler` cadence
(already invoked for every pawn regardless of this feature) — this feature adds one more `ThoughtDef` to that
existing enumeration, not a new dispatch mechanism.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony + CE
(Constitution Principle I) — trivially satisfied here since nothing this feature touches is CE-aware content
(research.md R6), unlike every prior combat-stat tattoo that needed a CE-gated fix. All tunable numbers (Tier 1/2
resistance amounts for both stats, the Tier 2 mood buff, the Tier 1→2 threshold) remain placeholders pending PRD
§9's balance pass, routed through `TattooEffectValues` per FR-011 — including the mood buff, via a small
`Thought_Situational.MoodOffset()` override rather than leaving it as a bare XML value, to avoid an unexplained
exception to that convention (research.md R5) — using deliberately small/fast-to-test values rather than
"plausible-looking" ones (feature 006 research.md R8 precedent, reused by every tattoo since).

**Scale/Scope**: One existing inert `HediffDef` (`TattooMagic_Hediff_StarlightWard`) gains a `<comps>` block; one
new `HediffComp` (the effect itself — tier progression, two stat offsets, self-contained risk-window detection);
one new small `ThoughtDef` + `ThoughtWorker` + `Thought_Situational` pair (the mood buff — the first mood-touching
code in this mod); two new lines in `TattooEffectStatPartInstaller` (registering the two new stats) — no changes
to Frost Sigil's, Serpent's Eye's, Guardian's Call's, Stormlash's, Bloodrune's, Wraithstep's, Berserker's Mark's,
Vampiric Thorn's, Ember Ward's, or Ironskin Glyph's own files, no changes to `IOnIncomingDamageTattooEffect` or
`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` (this feature doesn't implement or need that interface at all), and
no new Harmony patch anywhere.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Only the general "loads/behaves cleanly" bar — this feature touches no combat stat CE replaces or wraps | **PASS** — research.md R6 independently confirms (by fully decompiling `CombatExtended.dll`, not by assuming a prior feature's CE finding transfers) that CE never references `MentalBreakThreshold` or `PsychicSensitivity`; the vanilla stat-offset pipeline alone is the complete, correct implementation in both configurations, needing no `CombatExtendedInterop.IsLoaded` branch |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines the required in-game Dev Mode scenarios covering Tier 1 resistance (both stats), fewer actual breaks at equal mood, the progression counter and Tier 2 upgrade (including a break correctly *not* counting as resisted), the Tier 2 mood buff's presence/absence, the no-tattoo/removal no-ops, persistence (including mid-risk-window), the removal guard, and a regression/CE-load/DLC-absence check |
| III. Data-Driven, Tunable Balance | Yes — Tier 1/Tier 2 resistance amounts for both stats, the Tier 2 mood buff, the Tier 2 threshold | **PASS** — every value lives on `HediffCompProperties_StarlightWardEffect` XML fields or the new `ThoughtDef`'s `stages[0].baseMoodEffect` (data-model.md) and is read exclusively through `TattooEffectValues` (FR-011) — including the mood buff, via a small `MoodOffset()` override (research.md R5) rather than leaving it as a bare, unrouted XML value; placeholders trace to PRD §9 via the same XML comment convention every prior tattoo carries |
| IV. Two-Path Combat Patching | Only required for targeting/AI-decision logic (Guardian's Call is the PRD example); this feature adds no targeting or attack-target-selection interception of any kind | **PASS (N/A)** — same non-applicability as every passive/triggered tattoo except Guardian's Call |
| V. Progression Integrity Across Saves | Yes — Starlight Ward's own tier/progress counter, plus its risk-window tracking state | **PASS** — persists via `CompExposeData()`'s `Scribe_Values.Look` calls, both through the composed `TattooTierProgress` (feature 002 precedent, unmodified) and three new small fields for `inRiskWindow`/`riskWindowHadBreak`/`nextRiskCheckTick` (data-model.md); a risk window in progress at save time resumes being tracked correctly on load rather than erroring, double-counting, or silently resetting — deterministic and idempotent, satisfying the same bar `TattooTierProgress.TryRegisterQualifyingEvent` already meets |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` confirms the design adds exactly one new `HediffComp` (+ its properties
class) implementing two existing, unmodified interfaces (`IProvidesTattooStatOffset`, `IProvidesTattooTierProgress`
— deliberately *not* `IOnIncomingDamageTattooEffect`, since this tattoo's event isn't damage-based), plus one new,
intentionally private `ThoughtWorker`/`Thought_Situational` pair consumed by nothing else in the roster. Zero new
Harmony patches, zero new shared interfaces/contracts, two new `TattooEffectStatPartInstaller` registrations. Table
above still holds; no new violations. This is the first tattoo whose own event-detection needed genuinely new
technique (self-contained polling instead of reusing an existing dispatch point) precisely because none of the
existing damage-based reuse points apply to a mood-driven event — User Story 3's own scope note anticipated this.

## Project Structure

### Documentation (this feature)

```text
specs/013-starlight-ward-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

No `contracts/` directory — this feature introduces no new interface or reusable infrastructure for a future
tattoo to consume (research.md R4/R5's private-to-this-feature decisions); mirrors feature 012's own precedent of
skipping `contracts/` when a passive tattoo needed none.

### Source Code (repository root)

```text
Defs/
├── HediffDefs/
│   └── Tattoos/
│       └── StarlightWard.xml                     # UPDATED — add HediffCompProperties_StarlightWardEffect
│                                                     #   entry with tier1/tier2 numeric fields
│                                                     #   (data-model.md), replacing the "effect not yet
│                                                     #   implemented" stub description; hediffClass stays
│                                                     #   HediffWithComps
└── ThoughtDefs/
    └── StarlightWard.xml                          # NEW — the Tier 2 mood-buff ThoughtDef (data-model.md)

Source/
└── Hediffs/
    ├── HediffComp_StarlightWardEffect.cs          # NEW — Starlight Ward-specific: composes
    │                                                 #   TattooTierProgress, implements
    │                                                 #   IProvidesTattooStatOffset (MentalBreakThreshold +
    │                                                 #   PsychicSensitivity, research.md R1-R2) and
    │                                                 #   IProvidesTattooTierProgress; self-contained
    │                                                 #   risk-window polling in CompPostTick (research.md
    │                                                 #   R4) — no IOnIncomingDamageTattooEffect
    ├── ThoughtWorker_StarlightWardTier2Active.cs   # NEW — small, private lookup of
    │                                                 #   HediffComp_StarlightWardEffect.Tier off the
    │                                                 #   pawn's own hediff comps (research.md R5)
    └── Thought_StarlightWardTier2Active.cs         # NEW — Thought_Situational subclass routing its
                                                        #   MoodOffset() through TattooEffectValues
                                                        #   (research.md R5, FR-011)

Source/
└── Effects/
    └── TattooEffectStatPartInstaller.cs            # UPDATED — two new Register() calls
                                                        #   (StatDefOf.MentalBreakThreshold,
                                                        #   StatDefOf.PsychicSensitivity); no other change
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–012 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) — no new project or scaffold. Starlight Ward's own comp lives in
`Source/Hediffs/`, mirroring every other tattoo-effect comp's placement exactly. The new `ThoughtWorker`/
`Thought_Situational` pair lives alongside it in `Source/Hediffs/` rather than a new `Source/Thoughts/` directory,
since it's small, single-purpose, and tightly coupled to this one tattoo's comp — not the start of a general
"tattoo mood thoughts" subsystem other tattoos are expected to need. `Source/Effects/TattooEffectStatPartInstaller.
cs` is the only shared-infrastructure file touched at all, and only by two additive `Register()` calls; every
other piece of shared infrastructure this feature needs (`TattooEffectValues`, `TattooTierProgress`,
`IProvidesTattooStatOffset`, `StatPart_TattooEffectOffset`, `IProvidesTattooTierProgress`,
`TattooHediffRemovalGuard`) already exists exactly as feature 012 left it, satisfying FR-010 and User Story 3's own
partial-reuse expectation — the genuinely new work this feature's planning phase produced is the risk-window
detection technique (research.md R4) and the mood-thought delivery mechanism (research.md R5), both deliberately
scoped private to this one tattoo rather than forced into premature shared contracts.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
