# Implementation Plan: Shedscale Tattoo Effect

**Branch**: `015-shedscale-tattoo-effect` | **Date**: 2026-09-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/015-shedscale-tattoo-effect/spec.md`

## Summary

The 15th tattoo, and the first built with **zero new Harmony patches** (research.md R7). A colonist wearing
Shedscale automatically regrows any genuinely missing eligible body part over a tiered duration (7 days Tier 1
/ 4 days Tier 2), with every eligible missing part regrowing simultaneously (no one-at-a-time limit). The
proposal's "an installed prosthetic/bionic blocks the timer without banking progress" turned out to need no
dedicated blocking state at all: decompiling vanilla's own install/remove surgery pipeline shows a body part
can never simultaneously be "missing" and "replaced" (research.md R1), so eligibility collapses to a single
always-fresh check — is the part currently in `GetMissingPartsCommonAncestors()`? — with a persistent `Alert`
(research.md R9) as the only piece of new surface actually needed for the "blocked" half of that mechanic.
Regrowth completion is a single vanilla `RestorePart` call that both regrows the part and clears its old
permanent injuries for free (research.md R2, FR-007's upside). Tier 1 regrowths carry a permanent
reduced-efficiency condition (a per-part `Hediff_Injury` reducing that part's own reported health, which
vanilla's own capacity system already weights correctly for whatever capacity that part serves — research.md
R6) that Tier 2 both prevents going forward and clears retroactively from every already-regrown part
(FR-018). Active regrowths carry a stacking hunger/pain cost (one companion hediff whose `Severity` tracks
concurrent-regrowth count, research.md R4) and a flat, non-stacking mood debuff (a `Thought_Situational` +
`ThoughtWorker` pair mirroring Starlight Ward's Tier 2 mood buff exactly, research.md R5) — both stalled by
malnutrition. Tier 2 is reached permanently on whichever of two independent conditions (30 days worn, or 3
parts regrown) comes first, reported to the shared tier-progress UI as whichever race is currently leading
(research.md R11, since `IProvidesTattooTierProgress`'s single `X/Y` contract doesn't fit two racing counters
directly — same shape mismatch Phoenix hit). Unlike Phoenix, Shedscale carries no rarity cap of any kind
(FR-019) — `Dialog_ChooseTattoo` needs no new condition at all for this feature, the single largest structural
difference from feature 014's own plan. All of the feature's own state — the tracked-regrowth dictionary and
both Tier 2 counters — lives as plain fields on the tattoo's own `HediffComp`, which is destroyed along with
the `Hediff` the moment the tattoo is removed, meaning "removing the tattoo resets everything" (the spec's own
Clarifications) is free by construction rather than requiring any dedicated removal-handling code
(research.md R10).

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–014
and this project's `rimworld-modding` toolchain).

**Primary Dependencies**: `Lib.Harmony` 2.3.* — **no new patches** this feature (research.md R7), the first
mechanically-rich tattoo to need none; reuses, unmodified, the existing generic
`Patch_HealthTracker_RemoveHediff_ProtectTattoos`/`TattooHediffRemovalGuard` (feature 001) via the same
`appliedHediff` registry every tattoo already participates in. `Krafs.Rimworld.Ref` 1.6.* — same as features
001–014, no new external dependency, no DLC dependency. Combat Extended interaction confirmed **absent** for
every mechanic this feature touches by fully decompiling the real, locally installed `CombatExtended.dll`: its
only overlap with anything this feature reads (`Verse.HediffSet`'s missing/added-part queries) is one
freshness-gated bleed-property patch on `Hediff_MissingPart` unrelated to the structural queries this feature
relies on, and CE has no hunger-rate override of any kind (research.md R8) — the third tattoo, after Starlight
Ward and Phoenix, confirmed CE-clean by full decompile rather than assumed.

**Storage**: RimWorld's built-in Scribe save/load system, entirely per-pawn (unlike Phoenix, this feature
introduces no faction-wide/`GameComponent` state at all): `HediffComp_ShedscaleEffect`'s tier/counters/
tracked-regrowth dictionary via `CompExposeData()`, the same mechanism every prior tattoo already uses, plus
one new pattern for this mod — `Scribe_Collections.Look` with `LookMode.BodyPart` for the `Dictionary
<BodyPartRecord, int>` tracking table (research.md's decompile of `Verse.Scribe_BodyParts`/`Verse.LookMode`
confirms `BodyPart` is a first-class, vanilla-supported look-mode, not a workaround).

**Testing**: No automated unit-test harness (unchanged from features 001–014). Validated via manual in-game
verification in Dev Mode per Constitution Principle II, using `quickstart.md`. This feature's real-world
multi-day timers (7/4-day regrowths, a days-long Tier-2 progression clock) make waiting them out for real
impractical, so `quickstart.md` leans on dedicated Dev Mode debug-action tooling (force a daily pass on
demand, jump straight to regrowth completion, set either Tier-2 counter directly) mirroring feature 013/014's
own precedent for a mechanic too slow/organic to reliably hand-test — simpler here than Phoenix's own tooling
since nothing in this feature spans multiple pawns or needs a registry sweep forced.

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime).
`Verse.HediffSet.GetMissingPartsCommonAncestors`/`HasDirectlyAddedPartFor`/`PartIsMissing`,
`Verse.Pawn_HealthTracker.RestorePart`, `RimWorld.Recipe_InstallArtificialBodyPart`/`Recipe_RemoveBodyPart`
(read-only, to understand the install/uninstall pipeline this feature relies on rather than patches),
`Verse.Need_Food.FoodFallPerTickAssumingCategory`/`HediffSet.GetHungerRateFactor`, `Verse.HediffStage`'s
`hungerRateFactorOffset`/`painOffset`/`capMods` fields, `Verse.Hediff_Injury`/`HediffComp_GetsPermanent`
(reused for the permanent efficiency-penalty hediff, same technique feature 014's `PhoenixRevivalUtility
.ApplyBurn` already established), `RimWorld.Alert`/`AlertReport`/`PawnsFinder` (this mod's first custom
`Alert` subclass), and `TattooMagic`'s own `Thought_Situational`/`ThoughtWorker` pattern (feature 013) — all
grounded against the real, installed `Assembly-CSharp.dll` (decompiled read-only via `ilspycmd`, no game code
executed) rather than assumed, including one place where the design proposal's own two-state framing
("missing" and "prosthetic-blocked" as separate, coexisting facts) turned out not to match how vanilla's
hediff model actually works (research.md R1).

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–014; no new project/scaffold needed.

**Performance Goals**: `HediffComp_ShedscaleEffect.CompPostTick` runs one self-gated once-per-in-game-day check
(matching feature 014's own `nextDaysAliveCheckTick` interval-gating convention), bounded by however many
parts one pawn can plausibly have missing at once (small, single digits) — cheap relative to the existing
per-tick `CompPostTick` dispatch every `HediffWithComps` pawn already receives. `Alert_ShedscaleRegrowthBlocked`
scans only `PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended`
(bounded by colony population), and only when the alert UI actually queries it (vanilla's own alert-refresh
cadence, not a separate tick hook) — identical cost shape to vanilla's own `Alert_Hypothermia`.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony +
CE (Constitution Principle I) — satisfied with no CE-specific code anywhere in this feature (research.md R8).
All tunable numbers (regrowth days per tier, the Tier 1 efficiency factor, both Tier 2 thresholds, the eligible
body-part-def lists themselves, the Strain hediff's per-stack hunger/pain values, the mood debuff's magnitude)
remain placeholders pending this spec's own Assumptions/balance pass, routed through `TattooEffectValues`
(Constitution Principle III) — including the two eligible-part-def lists, authored as `List<BodyPartDef>` XML
fields rather than hardcoded in C#, so a future balance pass can widen Tier 2's organ coverage (e.g. adding
Heart/Liver) without a code change (research.md R3).

**Scale/Scope**: One new tattoo `HediffDef`/`TattooMagicDef` pair (plain `HediffWithComps`, no custom `Hediff`
subclass needed); one new `HediffComp` (tier/day-worn-counter/parts-regrown-counter/tracked-regrowth
dictionary, implementing `IProvidesTattooTierProgress` directly rather than composing the shared
`TattooTierProgress`, since Shedscale's own progression races two independently-paced counters unlike every
other tattoo's single monotonic one, research.md R11); two new support `HediffDef`s with no dedicated `HediffComp`
of their own beyond a vanilla one (`ShedscaleStrain` — plain severity-driven stages; `ShedscaleImperfectRegrowth`
— `Hediff_Injury` + vanilla `HediffCompProperties_GetsPermanent`); one new `ThoughtDef` + `Thought_Situational`/
`ThoughtWorker` pair (mirroring feature 013's Starlight Ward Tier 2 mood buff exactly); one new `Alert` subclass
(this mod's first); **zero** new Harmony patches; **zero** new `GameComponent`s; **zero** new `TattooMagicDefOf`
entries beyond the three `HediffDef`s themselves (no `TattooMagicDef` entry needed, unlike Phoenix, since
nothing gates availability on a cap); no changes to `Dialog_ChooseTattoo` at all (no cap to check) — no changes
to any of the other 14 tattoos' own files, `IOnIncomingDamageTattooEffect`, `IOnMeleeHit*TattooEffect`, or
`TattooTierProgress` itself.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Only the general "loads/behaves cleanly" bar — nothing in this feature touches a combat stat, targeting, or armor pipeline CE replaces | **PASS** — research.md R8 confirms, by fully decompiling `CombatExtended.dll` for every class touching body parts/missing parts/hunger rate, that CE's only overlap is one narrow, freshness-gated `Hediff_MissingPart` bleed-property patch that never touches the structural `HediffSet` queries (`GetMissingPartsCommonAncestors`, `HasDirectlyAddedPartFor`) this feature relies on, and CE has no hunger-rate override of any kind |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines the required in-game Dev Mode scenarios covering all four user stories (natural regrowth at both tiers including organs, the prosthetic-block/unblock cycle and its alert, stacking hunger/pain plus the flat mood debuff plus the malnutrition stall, and dual-path Tier 2 progression with retroactive efficiency clearing), plus the no-cap requirement, removal resetting all state, persistence, and a CE-load/regression pass — leaning on new dedicated debug-action tooling since this feature's timers are real-world days long, simpler than Phoenix's own tooling since nothing here spans multiple pawns |
| III. Data-Driven, Tunable Balance | Yes — every numeric this feature introduces (see Scale/Scope), including the two eligible-body-part-def lists themselves | **PASS** — every value lives on `HediffCompProperties_ShedscaleEffect` XML fields (or the two support hediffs' own `<stages>`) and is read exclusively through `TattooEffectValues.Get(...)` at point of use, matching every tattoo shipped so far; placeholders trace to this spec's own Assumptions section via the standard XML top-of-file comment convention |
| IV. Two-Path Combat Patching | Only required for targeting/AI-decision logic; this feature adds no targeting or attack-target-selection interception of any kind | **PASS (N/A)** — same non-applicability as every non-Guardian's-Call tattoo |
| V. Progression Integrity Across Saves | Yes — Shedscale's own per-pawn tier/counters/tracked-regrowth state, entirely per-pawn (no faction-wide state at all, unlike Phoenix) | **PASS** — state persists via `CompExposeData()` exactly like every prior tattoo (data-model.md), using one new but fully vanilla-supported look-mode (`Scribe_Collections.Look(..., LookMode.BodyPart, LookMode.Value)` for the tracked-regrowth dictionary, confirmed via decompile of `Verse.Scribe_BodyParts`). Deterministic and idempotent across save/reload mid-regrowth: whole relative elapsed-day counts simply resume advancing on the next in-game day after load, with no absolute-tick re-evaluation needed the way Phoenix's own `reviveAttemptTick` required |

No violations requiring justification — **Complexity Tracking is not needed** for this feature. This is
mechanically comparable in richness to Phoenix (a multi-piece state machine, two new support hediffs, a new
mood-thought pair, a new alert) but with a smaller and less invasive footprint than any prior mechanically-rich
tattoo — zero new Harmony patches, zero new `GameComponent`s, zero changes to `Dialog_ChooseTattoo` — because
research.md's own findings (R1, R7, R10) repeatedly showed that state or patching the design proposal's
narrative framing seemed to call for was already handled for free by how vanilla's own hediff model and this
mod's own existing removal-guard infrastructure work. Every piece of new surface that *does* exist is required
directly by this feature's own FRs (per-part regrowth tracking, the two racing Tier 2 conditions, the stacking
cost, the flat mood debuff, the prosthetic-block alert) — not speculative infrastructure for a future tattoo —
and every existing shared contract that fits is reused unmodified (`IProvidesTattooTierProgress`,
`TattooEffectValues`, `TattooHediffRemovalGuard`, the `Thought_Situational`/`ThoughtWorker` pattern), per the
same "no premature abstraction, no unjustified exception either" bar every prior feature's plan has held
itself to.

*Post-Phase 1 re-check*: `data-model.md` confirms the design above holds with no new violations — three new
production pieces (`HediffComp_ShedscaleEffect` + properties, `Alert_ShedscaleRegrowthBlocked`,
`Thought_ShedscaleRegrowthActive`/`ThoughtWorker_ShedscaleRegrowthActive`), two new support `HediffDef`s with
no dedicated comps beyond a vanilla one, zero changes to any existing tattoo's own files or to any of
`IOnIncomingDamageTattooEffect`/`IOnMeleeHit*TattooEffect`/`TattooTierProgress`. The one deliberate deviation
from the shared, composed `TattooTierProgress` (implementing `IProvidesTattooTierProgress` directly instead,
research.md R11) is necessary, not incidental — the exact same shape mismatch that already justified Phoenix's
own identical deviation in feature 014, now confirmed to recur rather than being a one-off exception.

## Project Structure

### Documentation (this feature)

```text
specs/015-shedscale-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

No `contracts/` directory — this feature introduces no new interface or reusable infrastructure a *future*
tattoo is expected to consume (data-model.md's header note); it reuses `IProvidesTattooTierProgress`,
`TattooEffectValues`, `TattooHediffRemovalGuard`, and the `Thought_Situational`/`ThoughtWorker` pattern exactly
as they stand, mirroring feature 012/013/014's own precedent of skipping `contracts/` when nothing generic is
introduced.

### Source Code (repository root)

```text
Defs/
├── TattooDefs/
│   └── Shedscale.xml                            # NEW — the recipe-side TattooMagicDef (tattooType=
│                                                     #   Passive, appliedHediff=TattooMagic_Hediff_
│                                                     #   Shedscale, standard ingredients/workAmount
│                                                     #   shape). No cap-related field — unlike Phoenix
├── HediffDefs/
│   ├── Tattoos/
│   │   └── Shedscale.xml                        # NEW — TattooMagic_Hediff_Shedscale, HediffWithComps
│   │                                                 #   + HediffCompProperties_ShedscaleEffect
│   │                                                 #   (data-model.md), every numeric a placeholder
│   │                                                 #   pending this spec's own Assumptions pass
│   ├── ShedscaleStrain.xml                      # NEW — TattooMagic_Hediff_ShedscaleStrain, sits
│   │                                                 #   alongside TattooTracker.xml/FrostSigilSlow.xml/
│   │                                                 #   ParalyticAbasia.xml (support hediffs, not under
│   │                                                 #   Tattoos/); plain Hediff, stages keyed by
│   │                                                 #   minSeverity 1-5 setting hungerRateFactorOffset/
│   │                                                 #   painOffset (research.md R4)
│   └── ShedscaleImperfectRegrowth.xml           # NEW — TattooMagic_Hediff_ShedscaleImperfectRegrowth;
│                                                     #   hediffClass=Hediff_Injury + vanilla
│                                                     #   HediffCompProperties_GetsPermanent, no custom
│                                                     #   comp, no stages of its own (research.md R6)
└── ThoughtDefs/
    └── Shedscale.xml                             # NEW — TattooMagic_Thought_ShedscaleRegrowthActive,
                                                        #   mirrors StarlightWard.xml's exact shape with a
                                                        #   negative placeholder baseMoodEffect

Source/
├── Defs/
│   └── TattooMagicDefOf.cs                      # UPDATED — three new entries: TattooMagic_Hediff_
                                                      #   Shedscale, TattooMagic_Hediff_ShedscaleStrain,
                                                      #   TattooMagic_Hediff_ShedscaleImperfectRegrowth
                                                      #   (no TattooMagicDef entry — no cap to check)
├── Hediffs/
│   ├── HediffComp_ShedscaleEffect.cs            # NEW — HediffCompProperties_ShedscaleEffect +
│   │                                                 #   HediffComp_ShedscaleEffect (data-model.md):
│   │                                                 #   tier/day-worn-counter/parts-regrown-counter/
│   │                                                 #   tracked-regrowth-dictionary state, implements
│   │                                                 #   IProvidesTattooTierProgress directly (not via
│   │                                                 #   composed TattooTierProgress, research.md R11);
│   │                                                 #   CompPostTick drives the once-per-day scan/
│   │                                                 #   advance/complete pass and the Strain hediff's
│   │                                                 #   severity; CompPostPostRemoved cleans up the
│   │                                                 #   Strain hediff (research.md R10)
│   ├── Thought_ShedscaleRegrowthActive.cs       # NEW — mirrors Thought_StarlightWardTier2Active
│   │                                                 #   exactly (research.md R5), negative BaseMoodOffset
│   │                                                 #   routed through TattooEffectValues
│   └── ThoughtWorker_ShedscaleRegrowthActive.cs # NEW — mirrors ThoughtWorker_StarlightWardTier2Active's
│                                                     #   comp-lookup shape exactly; ActiveDefault iff
│                                                     #   HediffComp_ShedscaleEffect.ActiveRegrowthCount > 0
├── UI/
│   └── Alert_ShedscaleRegrowthBlocked.cs        # NEW — this mod's first custom Alert subclass
│                                                     #   (research.md R9); scans free colonists for a
│                                                     #   Hediff_AddedPart on a tier-eligible BodyPartDef,
│                                                     #   AlertReport.CulpritsAre(...), no persisted state
└── Debug/
    └── DebugAction_TestShedscaleRegrowth.cs     # NEW — Dev Mode tools to make this feature's real-
                                                      #   world-days timers testable without waiting them
                                                      #   out (quickstart.md), mirroring feature 013/014's
                                                      #   own deterministic-debug-tool precedent
```

No changes to `Source/Patches/`, `Source/Effects/` (no new `GameComponent`, no new shared interface — reuses
`IProvidesTattooTierProgress`/`TattooEffectValues`/`TattooHediffRemovalGuard` unmodified), or `Source/UI/
Dialog_ChooseTattoo.cs` (no rarity cap to gate availability on, unlike Phoenix — the single largest structural
difference from feature 014's own file list).

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–014 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) — no new project or scaffold. Every new file sits in the folder its existing
sibling files already occupy (`Source/Hediffs/` for the tattoo's own comp and the mood-thought pair,
`Source/Debug/` for Dev Mode tooling). `Source/UI/Alert_ShedscaleRegrowthBlocked.cs` is the one placement
judgment call this feature makes without a strong prior in-mod precedent for "where do Alert subclasses live":
placed alongside `Dialog_ChooseTattoo.cs`/`ITab_Pawn_Tattoos.cs`/`Dialog_TattooCodex.cs` in `Source/UI/` since
it is colonist-facing UI surface reporting on tattoo state, not itself a tattoo *effect* mechanism the way
`Source/Effects/`'s existing contents are (shared cross-tattoo infrastructure) or `Source/Hediffs/`'s existing
contents are (a specific tattoo's own comp). `TattooTierProgress`, `IOnIncomingDamageTattooEffect`,
`IOnMeleeHit*TattooEffect`, every existing Harmony patch, and every other tattoo's own files are untouched.

## Complexity Tracking

*No entries — Constitution Check reported no violations requiring justification.*
