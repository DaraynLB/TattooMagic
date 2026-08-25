# Implementation Plan: Stormlash Tattoo Effect (Second Triggered Ability + Movement-Slow Immunity Slice)

**Branch**: `005-stormlash-tattoo-effect` | **Date**: 2026-08-08 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-stormlash-tattoo-effect/spec.md`

## Summary

Turn Stormlash from an inert stub Hediff (shipped by feature 001) into a working PRD §6 tattoo: an activatable
speed ability (gizmo + cooldown) that grants a temporary move-speed and attack-speed boost, upgrading in place
to a Tier 2 that grants a bigger boost plus genuine immunity to movement-slow effects (this mod's own Frost
Sigil slow, and Combat Extended's suppression-slow) for the boost's duration, driven by the same "times
activated" progression-counter pattern as Guardian's Call (PRD §5.4). This is the **second** triggered tattoo,
so its primary infrastructure job is proving feature 004's `IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_
TattooGizmos` reuse point genuinely generalizes to an unrelated consumer with zero changes to Guardian's Call's
own files (research.md R1). Its harder technical problem is Tier 2's slow immunity: research (R4) found that
Combat Extended's suppression-slow is applied *before* this mod's additive stat-offset mechanism runs, so a
bigger `MoveSpeed` offset alone cannot produce true immunity — this feature adds one new CE-only Harmony patch
(neutralizing CE's suppression-slow at its source) and one new small, generic interface consumed by the existing
shared `StatPart_TattooEffectOffset` (letting Tier 2 veto *any* other comp's negative `MoveSpeed` contribution,
covering Frost Sigil's own slow without either tattoo knowing about the other, research.md R5).

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–004 and
this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (a new postfix implementing `IProvidesTattooGizmo`'s dispatch —
no change to the shared patch itself — plus one new CE-only imperative postfix on
`CombatExtended.CompSuppressable`'s `IsCrouchWalking` getter), `Krafs.Rimworld.Ref` 1.6.* (game API reference
assemblies) — same as features 001–004, no new external dependency; no DLC dependency (this feature reuses
feature 004's confirmed DLC-free gizmo mechanism unmodified, research.md R1)

**Storage**: RimWorld's built-in Scribe save/load system — Stormlash's tier, progression counter, cooldown-end
tick, and boost-end tick persist as `HediffComp` state on the existing `TattooMagic_Hediff_Stormlash` hediff,
exactly like features 002–004's comps

**Testing**: No automated unit-test harness (unchanged from features 001–004 — RimWorld's `Verse`/`RimWorld`
game types, and Combat Extended's live suppression state specifically, aren't practically runnable outside the
game process); validated via manual in-game verification in Dev Mode per Constitution Principle II, using
`quickstart.md`, with **required** (not optional) passes for both of this feature's immunity claims — the
in-mod Frost-Sigil-vs-Stormlash interaction (non-CE) and genuine suppression-slow immunity (CE-loaded)

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime)

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–004; no new project/scaffold needed

**Performance Goals**: The new `IProvidesTattooGizmo` implementation runs only on gizmo-row refresh
(selection/UI), not per-tick, identical cost profile to Guardian's Call's own gizmo (feature 004 R1); the new
`GetStatOffset`/`BlocksNegativeOffsets` checks are called through the existing `StatPart_TattooEffectOffset`
sweep (feature 002), which RimWorld already caches per stat query rather than per-tick, so no new hot-path cost
is introduced; the new CE-only `IsCrouchWalking` postfix only runs when Combat Extended itself queries that
property (its own suppression-reaction tick, not every game tick for every pawn)

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony +
CE (Constitution Principle I); per research.md R3, CE does not replace `MeleeCooldownFactor`/
`RangedCooldownFactor`, so the attack-speed boost needs no CE branching at all — but per research.md R4, CE
*does* replace how `MoveSpeed`'s value is computed (a multiplicative suppression penalty applied before this
mod's additive offsets run), which is a genuinely new integration shape none of features 002–004 encountered
and is why Tier 2 needs a dedicated CE-only patch rather than just a bigger number; all numeric balance values
remain placeholders pending PRD §9's balance pass, routed through `TattooEffectValues` per FR-012

**Scale/Scope**: One tattoo's gameplay effect (Stormlash) + one new small reusable infrastructure piece (the
`IGrantsStatOffsetImmunity` veto interface on the shared stat-offset StatPart, intended for reuse by any future
tattoo that needs genuine — not just bigger-number — immunity to another effect's negative stat contribution) +
two new `TattooEffectStatPartInstaller` registrations (`MeleeCooldownFactor`, `RangedCooldownFactor`) — no
changes to Guardian's Call's own files (validates FR-014/FR-015's reuse-point-for-a-second-consumer requirement)
and no changes to Frost Sigil's own file (research.md R5)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Yes — attack-speed and move-speed boosts, plus Tier 2's CE suppression-slow immunity | **PASS** — research.md R3 confirms no CE-specific branching is needed for the attack-speed StatDefs (CE leaves them untouched); research.md R4 grounds the suppression-slow immunity patch in decompiled CE source (v16.7.3.0) and gates it behind `CombatExtendedInterop.IsLoaded`, never throwing or no-oping when CE is absent, matching `Patch_CE_ProjectileImpact_TattooAmmoBonus`'s existing safe-guard convention |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines required in-game Dev Mode scenarios, including a mandatory CE-loaded pass proving genuine (not just bigger-number) suppression-slow immunity under live suppression fire, and a mandatory non-CE pass proving genuine immunity to this mod's own Frost Sigil slow — both flagged in research.md (R4, R5) as static-analysis-grounded claims that still require live confirmation |
| III. Data-Driven, Tunable Balance | Yes — move-speed/attack-speed boost magnitudes, boost duration, cooldown, tier threshold | **PASS** — every value lives in `HediffCompProperties_StormlashEffect` XML fields (data-model.md) and is read exclusively through `TattooEffectValues` (FR-012), never as an inline magic number; placeholders trace to PRD §9 |
| IV. Two-Path Combat Patching | No — Stormlash does not intercept targeting or combat-decision logic (no `AttackTargetFinder`-equivalent involvement); it only affects stat values on its own wearer | **N/A** — same non-applicability as features 002/003 (Frost Sigil, Serpent's Eye), which also touched combat stats without touching targeting AI; Principle I still governs the CE-specific stat/patch work this feature does |
| V. Progression Integrity Across Saves | Yes — Stormlash's tier/progression counter, plus new cooldown-end/boost-end ticks | **PASS** — `tierState` reuses `TattooTierProgress`'s already-proven `ExposeData()`/idempotent tier-up mechanism unmodified; the two new tick fields are plain `Scribe_Values.Look`'d ints on the same `HediffComp`, no new persistence mechanism (research.md R6) |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` and `contracts/stat-offset-immunity-contract.md` introduce one new
interface (`IGrantsStatOffsetImmunity`), one small generic extension to the existing, shared
`StatPart_TattooEffectOffset.GetOffset` (feature 002, still tattoo-agnostic — it only knows "some comp currently
vetoes negative contributions to this stat," not which tattoo), two new `TattooEffectStatPartInstaller`
registrations, and one new CE-only imperative Harmony patch. No changes to `IProvidesTattooGizmo`,
`Patch_Pawn_GetGizmos_TattooGizmos`, `HediffComp_GuardiansCallEffect`, or `HediffComp_FrostSigilEffect`/
`HediffComp_FrostSigilSlow` — table above still holds; no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/005-stormlash-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── stat-offset-immunity-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── Tattoos/
        └── Stormlash.xml                     # UPDATED — add HediffCompProperties_StormlashEffect
                                                #   entry with tier1/tier2 numeric fields (data-model.md)

Source/
├── Effects/                                   # EXISTING (feature 002) — reusable tattoo-agnostic
│   │                                           #   infrastructure
│   ├── IGrantsStatOffsetImmunity.cs           # NEW — negative-offset veto contract (research.md R5)
│   ├── StatPart_TattooEffectOffset.cs         # UPDATED — consults IGrantsStatOffsetImmunity before
│   │                                           #   summing (research.md R5); still tattoo-agnostic
│   └── TattooEffectStatPartInstaller.cs       # UPDATED — register StatPart_TattooEffectOffset onto
│                                               #   MeleeCooldownFactor/RangedCooldownFactor
│                                               #   (research.md R2); no other change to this file's
│                                               #   existing registrations
├── Hediffs/
│   └── HediffComp_StormlashEffect.cs          # NEW — Stormlash-specific: composes TattooTierProgress,
│                                               #   implements IProvidesTattooGizmo (reused unmodified,
│                                               #   research.md R1) + IProvidesTattooStatOffset +
│                                               #   IGrantsStatOffsetImmunity, owns cooldownEndTick/
│                                               #   boostEndTick
└── Patches/
    └── Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity.cs   # NEW — CE-only imperative
                                                #   patch neutralizing CE's suppression-slow at its
                                                #   source for an active Tier 2 boost (research.md R4);
                                                #   TryApply(harmony) called from TattooMagicMod.cs
                                                #   alongside the existing CE patch
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–004 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) rather than introducing a new project or scaffold. The new stat-offset-
immunity interface lives alongside features 002/003's other reusable pieces in `Source/Effects/`, since it's
tattoo-agnostic infrastructure in the same spirit as `IProvidesTattooStatOffset`; `StatPart_TattooEffectOffset`
(feature 002) is extended in place rather than duplicated, since the veto check belongs in the same summation
loop it already runs. Stormlash's own comp lives in `Source/Hediffs/` next to the other tattoo-specific comps,
implementing feature 004's `IProvidesTattooGizmo` with zero changes to that interface or its shared patch. The
new CE-only patch joins the existing `Source/Patches/` folder, following the established
`Patch_<Type>_<Method>_<Purpose>` naming convention and mirroring `Patch_CE_ProjectileImpact_TattooAmmoBonus`'s
imperative (non-attribute-discovered) application.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
