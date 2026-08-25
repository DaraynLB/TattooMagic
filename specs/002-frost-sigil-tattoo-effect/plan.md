# Implementation Plan: Frost Sigil Tattoo Effect (Effect Pattern Vertical Slice)

**Branch**: `002-frost-sigil-tattoo-effect` | **Date**: 2026-08-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-frost-sigil-tattoo-effect/spec.md`

## Summary

Turn Frost Sigil from an inert stub Hediff (shipped by feature 001) into a working PRD §6 tattoo: passive
cold resistance plus a chance to slow a melee attacker on hit, both stronger at Tier 2, with an automatic
Tier 1 → Tier 2 upgrade driven by a per-pawn progression counter (PRD §5.4). Alongside Frost Sigil itself,
this feature stands up small, reusable, interface-based infrastructure — a value accessor
(`TattooEffectValues`), a live stat-offset contract (`IProvidesTattooStatOffset` + one shared `StatPart`), an
on-hit-proc contract (`IOnMeleeHitTattooEffect` + one shared Harmony patch), and a composable tier/counter
helper (`TattooTierProgress`) — documented in `contracts/passive-tattoo-effect-contract.md` for the 5
remaining **passive** tattoos to build against without touching Frost Sigil's own code. The triggered/gizmo
tattoo pattern (Bloodrune, Stormlash, etc.) is explicitly out of scope here (see spec Assumptions).

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching feature 001 and
this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (the shared `Pawn.PostApplyDamage` postfix), `Krafs.Rimworld.Ref`
1.6.* (game API reference assemblies) — same as feature 001, no new external dependency

**Storage**: RimWorld's built-in Scribe save/load system — Frost Sigil's tier and progression counter persist
as `HediffComp` state on the existing `TattooMagic_Hediff_FrostSigil` hediff, exactly like feature 001's
`HediffComp_TattooTracker`; no external storage

**Testing**: No automated unit-test harness (unchanged from feature 001 — RimWorld's `Verse`/`RimWorld`
game types aren't practically runnable outside the game process); validated via manual in-game verification
in Dev Mode per Constitution Principle II, using `quickstart.md`

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime)

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from feature
001; no new project/scaffold needed

**Performance Goals**: No special target — the on-hit patch runs only on actual melee hits (a rare,
event-driven trigger, not per-frame), and the stat-offset `StatPart` runs only when RimWorld already queries
`ComfyTemperatureMin`/`MoveSpeed` (its normal caching applies); must not add per-tick work to pawns lacking
Frost Sigil

**Constraints**: Must load with zero Harmony/console errors with Harmony alone; per `research.md` R6, no
CE-specific stat branching is needed for Frost Sigil's two stats (`ComfyTemperatureMin`, `MoveSpeed`) since
CE doesn't replace either, but the melee-hit detection patch's CE-safety is an assumption that MUST be
confirmed in-game (quickstart Scenario 6) rather than taken on faith, per Constitution Principle I; all
numeric balance values remain placeholders pending PRD §9's balance pass, routed through the new
`TattooEffectValues` accessor per FR-012 rather than read directly at each use site

**Scale/Scope**: One tattoo's gameplay effect (Frost Sigil) + five small reusable types intended for reuse
by up to 5 more passive tattoos later (not built now) — no changes to feature 001's ritual/tracker system

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Yes — this feature modifies `MoveSpeed` and applies a stun, both combat-adjacent stats | **PASS** — `research.md` R6 confirms neither `ComfyTemperatureMin` nor `MoveSpeed` is CE-replaced, so no stat branching is needed; the melee-hit detection patch point (`Pawn.PostApplyDamage`, R2) is chosen specifically because it sits below CE's ballistics/armor rework rather than inside it, so it doesn't silently no-op under CE — verified in-game (quickstart Scenario 6), not assumed |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines required in-game Dev Mode scenarios, including a CE-loaded pass for the highest-risk assumption (R2); not "done" until run |
| III. Data-Driven, Tunable Balance | Yes — cold resistance, slow chance/magnitude/duration, freeze chance/duration, tier threshold | **PASS** — every value lives in `HediffCompProperties_FrostSigilEffect` XML fields (data-model.md) and is read exclusively through `TattooEffectValues` (FR-012), never as an inline magic number in patch/comp logic; placeholders trace to PRD §9 |
| IV. Two-Path Combat Patching | Only required for targeting/AI-decision logic (Guardian's Call is the PRD example); this feature does not touch targeting | **PASS (N/A)** — the on-hit patch hooks the universal post-damage-application step, not a verb-selection or targeting decision, so unlike Guardian's Call it does not need distinct vanilla/CE patches; this is treated as a verified finding (quickstart Scenario 6), not an assumption baked in without a check |
| V. Progression Integrity Across Saves | Yes — Frost Sigil's tier and progression counter | **PASS** — `TattooTierProgress` is Scribe-exposed via the owning `HediffComp`'s `CompExposeData()`, mirroring feature 001's already-proven `HediffComp_TattooTracker` pattern; tier-up is idempotent by construction (`TryRegisterQualifyingEvent` only flips `tier` once, at `1`→`2`, never re-triggers) |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` and `contracts/passive-tattoo-effect-contract.md` introduced two
new interfaces, one `StatPart`, one shared Harmony patch, and one composed (non-`HediffComp`) helper class —
none touch targeting/AI logic or CE-replaced stats, and all persisted state routes through the same
Scribe-backed `HediffComp` mechanism feature 001 already established. Table above still holds; no new
violations.

## Project Structure

### Documentation (this feature)

```text
specs/002-frost-sigil-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── passive-tattoo-effect-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── Tattoos/
        ├── FrostSigil.xml                  # UPDATED — add HediffCompProperties_FrostSigilEffect
        │                                    #   entry with tier1/tier2 numeric fields (data-model.md)
        └── FrostSigilSlow.xml              # NEW — transient hediff granted to attackers; carries
                                             #   HediffCompProperties_Disappears (vanilla) + the new
                                             #   IProvidesTattooStatOffset-implementing comp

Source/
├── Effects/                                # NEW — reusable passive-tattoo-effect infrastructure,
│   │                                        #   intentionally Frost-Sigil-agnostic (FR-010)
│   ├── TattooEffectValues.cs               # override-capable value accessor (FR-012)
│   ├── IProvidesTattooStatOffset.cs        # live stat-bonus contract
│   ├── IOnMeleeHitTattooEffect.cs          # on-hit-proc contract
│   ├── TattooTierProgress.cs               # composed tier/counter helper (not a HediffComp)
│   ├── StatPart_TattooEffectOffset.cs      # generic StatPart; registered onto ComfyTemperatureMin
│   │                                        #   and MoveSpeed at startup
│   └── TattooEffectStatPartInstaller.cs    # [StaticConstructorOnStartup] — registers the StatPart
│                                            #   above onto the relevant vanilla StatDefs in C#
├── Hediffs/
│   ├── HediffComp_FrostSigilEffect.cs      # NEW — Frost Sigil-specific: composes TattooTierProgress,
│   │                                        #   implements IProvidesTattooStatOffset +
│   │                                        #   IOnMeleeHitTattooEffect
│   └── HediffComp_FrostSigilSlow.cs        # NEW — the transient attacker-side comp implementing
│                                            #   IProvidesTattooStatOffset against MoveSpeed
└── Patches/
    └── Patch_Pawn_PostApplyDamage_TattooOnHit.cs   # NEW — the one shared Harmony postfix (R2),
                                                     #   dispatches to IOnMeleeHitTattooEffect
```

**Structure Decision**: Extends the existing single RimWorld-mod project from feature 001 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) rather than introducing a new project or scaffold. New reusable
infrastructure lives under a fresh `Source/Effects/` folder — deliberately separate from feature 001's
`Source/Hediffs/` (which holds the ritual tracker) — to make the "this is shared, tattoo-agnostic
infrastructure" boundary visible in the file layout itself, matching FR-010's reuse intent. A new
`Source/Patches/` folder holds this feature's Harmony patch, leaving feature 001's existing flat
`Source/HarmonyPatches.cs` (the pawn-spawn tracker patch) untouched.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
