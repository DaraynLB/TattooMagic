# Implementation Plan: Soul Tether Tattoo Effect

**Branch**: `017-soul-tether-tattoo-effect` | **Date**: 2026-09-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/017-soul-tether-tattoo-effect/spec.md`

## Summary

A two-tier triggered tattoo that lets its wearer pick a single friendly pawn or corpse within range and instantly
pull it to their side, granting a flat tend speed/quality buff on arrival (living targets only). Tier 2 only
extends max pull range and buff magnitude — the pull stays single-target at both tiers. Technically, this is the
mod's first pawn/corpse-targeted ability (every prior targeted tattoo targets a ground cell) and requires two small,
new, narrowly-scoped dispatch points because vanilla's tend-quality/speed stats are queried on the doctor, not the
patient (research.md R4/R5) — otherwise it reuses every existing convention (two-phase gizmo targeting, tier
progression, `TattooEffectValues`, Frost Sigil's cross-pawn transient-hediff shape) unmodified.

## Technical Context

**Language/Version**: C# 7.3, .NET Framework 4.7.2 (project-wide, unchanged).

**Primary Dependencies**: `Krafs.Rimworld.Ref` 1.6.x, `Lib.Harmony` (hard); Combat Extended (soft, runtime-detected
only — this feature needs no CE-specific branching per research.md R4's finding that tend quality/speed are
vanilla-only mechanics CE does not override).

**Storage**: RimWorld save file via `Scribe`/`IExposable` (unchanged pattern) — no new persistence design; see
data-model.md's Persistence summary.

**Testing**: Manual in-game verification only (`quickstart.md`), per Constitution Principle II and this project's
unbroken precedent across features 001-016 — no automated test suite exists for `Verse`/`RimWorld`-dependent logic.

**Target Platform**: RimWorld 1.6, Windows/Mac/Linux (unchanged).

**Project Type**: Single RimWorld mod project (`Source/` → `Assemblies/TattooMagic.dll`, plus `Defs/`/`Textures/`).

**Performance Goals**: No new hot-path concerns — the two new dispatch points (research.md R4/R5) are a
Harmony postfix on a method already called once per tend action, and a `StatPart` on a stat already queried per
tend-toil-duration calculation; neither adds per-tick or per-frame cost.

**Constraints**: N/A beyond the Constitution's own (data-driven balance values, dual-mode CE compatibility, in-game
verification before "done").

**Scale/Scope**: One tattoo effect; two new HediffDefs (`TattooMagic_Hediff_SoulTether`,
`TattooMagic_Hediff_SoulTetherTendBuff`), one new TattooDef, one new HediffComp pair, one new `StatPart`, one new
Harmony patch, one new small static utility.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design — still passes.*

- **I. Dual-Mode Compatibility**: PASS. Research.md R4 confirms tend quality/speed are vanilla-only stats with no CE
  override to reconcile; no combat stat (armor/accuracy/speed/suppression/ammo) is touched by this feature at all,
  so Principle I's branching requirement does not apply here (nothing to branch). Verified empirically anyway in
  quickstart.md Scenario 5.
- **II. Verify Before Claiming Done**: PASS. `quickstart.md`'s five scenarios cover every user story, edge case, and
  a CE-loaded pass; this feature is not "done" until all five are run in a live instance with zero console errors.
- **III. Data-Driven, Tunable Balance**: PASS. Every numeric value (ranges, cooldown, tend bonuses, buff duration,
  tier threshold) is an XML-configurable field on `HediffCompProperties_SoulTetherEffect`, read only through
  `TattooEffectValues.Get`, each flagged as a placeholder pointing at this spec's Assumptions section.
- **IV. Two-Path Combat Patching**: N/A. This feature does not intercept vanilla targeting or combat-decision logic
  (no `AttackTargetFinder`-style patch); the two new patches (research.md R4/R5) touch the medical-tend system, a
  single vanilla-only code path with no CE-equivalent override to duplicate.
- **V. Progression Integrity Across Saves**: PASS. `data-model.md`'s Persistence summary covers both the caster's
  tier/cooldown state and the target's tend-buff hediff state, both via already-proven `Scribe` mechanisms; no new
  persistence design is introduced. Verified in `quickstart.md` Scenario 5.

No violations; Complexity Tracking is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/017-soul-tether-tattoo-effect/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── pull-and-apply-effect-contract.md
└── tasks.md             # Phase 2 output (/speckit-tasks — not created by /speckit-plan)
```

### Source Code (repository root)

```text
Source/
├── Hediffs/
│   ├── HediffComp_SoulTetherEffect.cs           # new — caster's gizmo/cooldown/tier/pull comp
│   └── HediffComp_SoulTetherTendBuff.cs         # new — the granted buff's comp (quality/speed magnitude)
├── Effects/
│   ├── SoulTetherTendBuffUtility.cs             # new — grant/refresh + read active bonuses
│   └── StatPart_SoulTetherTendSpeedBuff.cs      # new — doctor-side MedicalTendSpeed lookup (research.md R5)
├── Patches/
│   └── Patch_TendUtility_CalculateBaseTendQuality_SoulTetherBuff.cs   # new (research.md R4)
├── Defs/
│   └── TattooMagicDefOf.cs                      # updated — two new HediffDef entries
└── Effects/TattooEffectStatPartInstaller.cs      # updated — register StatPart_SoulTetherTendSpeedBuff on MedicalTendSpeed

Defs/
├── HediffDefs/Tattoos/SoulTether.xml            # new
├── HediffDefs/SoulTetherTendBuff.xml            # new
└── TattooDefs/SoulTether.xml                    # new
```

**Structure Decision**: Follows this mod's single-project layout exactly (`Source/<Category>/`, `Defs/<Category>/`),
matching Bastion Ward/Wraithstep's own file organization — no new top-level directory or project needed.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
