# Implementation Plan: Ironskin Glyph Tattoo Effect (Eighth Passive Tattoo, Physical Armor Slice)

**Branch**: `012-ironskin-glyph-tattoo-effect` | **Date**: 2026-08-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/012-ironskin-glyph-tattoo-effect/spec.md`

## Summary

Turn Ironskin Glyph from an inert stub Hediff (shipped by feature 001) into a working PRD §6 tattoo: a permanent
armor-rating bonus covering both Sharp and Blunt physical damage categories, upgrading in place to stronger values
plus a Tier 2-exclusive chance to fully negate a hit, via a "physical hits absorbed" progression counter (PRD
§5.4), reusing feature 002's value accessor (`TattooEffectValues`) and tier tracker (`TattooTierProgress`) and
feature 011's pre-armor incoming-damage reuse point (`IOnIncomingDamageTattooEffect` /
`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`) entirely unmodified. This is the eighth passive tattoo and the
second consumer of everything feature 011 built — no new interface, no new Harmony patch, and no new stat-part
registration are needed (`ArmorRating_Sharp`/`ArmorRating_Blunt` are already registered by an earlier feature,
unused until now). The one genuinely new piece of research this feature required (per its own Input) was
independently verifying — by decompiling Combat Extended's own `ArmorUtilityCE.GetAfterArmorDamage` non-ambient
(direct-hit) code path, the one Sharp/Blunt weapon and melee damage actually takes — whether it has the same
pawn-stat-reading gap feature 011 found in CE's *ambient* (fire-tick) path. It does (research.md R3): CE's
direct-hit path also only reads a pawn's own armor-category stat when the hit body part belongs to the
`CoveredByNaturalArmor` body-part group, never true for ordinary humans. The fix is the identical pattern feature
011 already established — a CE-gated direct `dinfo.Amount` reduction inside `TryAbsorbIncomingDamage`, made
possible with zero interface change because that method already takes `dinfo` by `ref`.

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–011 and
this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (zero new patches — reuses feature 011's existing
`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` unmodified), `Krafs.Rimworld.Ref` 1.6.* — same as features 001–011,
no new external dependency, no DLC dependency. Combat Extended interaction confirmed by decompiling the real,
locally installed `CombatExtended.dll` (Steam Workshop item `2890901044`): CE's `ArmorUtilityCE.
GetAfterArmorDamage`'s non-ambient (direct-hit) branch — the one Sharp/Blunt weapon/melee damage takes, distinct
from the ambient/fire-tick branch feature 011 investigated — has the same `CoveredByNaturalArmor`-gated
pawn-stat-reading gap (research.md R3), requiring the same CE-gated direct-reduction fix feature 011 already
established, with zero interface or patch changes needed.

**Storage**: RimWorld's built-in Scribe save/load system — Ironskin Glyph's tier and progression counter persist as
`HediffComp` state on the existing `TattooMagic_Hediff_IronskinGlyph` hediff, the exact same shape every prior
tattoo's own comp already uses.

**Testing**: No automated unit-test harness (unchanged from features 001–011 — RimWorld's `Verse`/`RimWorld` game
types aren't practically runnable outside the game process); validated via manual in-game verification in Dev Mode
per Constitution Principle II, using `quickstart.md`. The CE-loaded pass is a standing "loads cleanly and behaves
correctly with CE present" check that research.md R3 predicts will show correct, guaranteed reduction once the
CE-gated direct fix is in place — not a CE-only code path the way Serpent's Eye's own CE scenario is.

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime). `Verse.DamageDef.
armorCategory`, `RimWorld.DamageArmorCategoryDef.armorRatingStat`, and CE's `ArmorUtilityCE.GetAfterArmorDamage`
(both its ambient and non-ambient sub-paths) were all grounded against the real, installed `Assembly-CSharp.dll`
and `CombatExtended.dll` (decompiled read-only via `ilspycmd`, no game code executed) rather than assumed,
mirroring features 007/010/011's own decompile-to-verify approach.

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–011; no new project/scaffold needed.

**Performance Goals**: Ironskin Glyph's `TryAbsorbIncomingDamage` is dispatched by feature 011's existing
`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` prefix, which already runs once per incoming `DamageInfo` per pawn
regardless of this feature (an existing, already-frequent vanilla call site); this feature adds one more comp to
that existing dispatch loop, the same per-hit cost shape Ember Ward already added. The
`ArmorRating_Sharp`/`ArmorRating_Blunt` stat offsets add no new per-tick cost — queried by the existing `StatPart`
machinery only when something reads those stats, same as every other tattoo's stat offset.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony + CE
(Constitution Principle I) — satisfied by the CE-gated direct-reduction fix (research.md R3), the same pattern
already proven correct for Ember Ward. All tunable numbers (Tier 1/Tier 2 armor-rating bonus, the Tier 2 full-negate
chance, the Tier 1→2 threshold) remain placeholders pending PRD §9's balance pass, routed through
`TattooEffectValues` per FR-012, using deliberately small/fast-to-test values rather than "plausible-looking" ones
(feature 006 research.md R8 precedent, reused by every tattoo since).

**Scale/Scope**: One existing inert `HediffDef` (`TattooMagic_Hediff_IronskinGlyph`) gains a `<comps>` block; one
new `HediffComp` (the effect itself — tier progression, one stat offset shared across two `StatDef`s, the Tier 2
full-negate roll) — no new `HediffDef`, no changes to Frost Sigil's, Serpent's Eye's, Guardian's Call's,
Stormlash's, Bloodrune's, Wraithstep's, Berserker's Mark's, Vampiric Thorn's, or Ember Ward's own files, no changes
to `TattooEffectStatPartInstaller` (registrations already present), and no changes to
`IOnIncomingDamageTattooEffect` or `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Yes — grants an offset to combat armor stats and reuses the incoming-damage patch | **PASS** — research.md R3 independently confirms (by decompiling `ArmorUtilityCE.GetAfterArmorDamage`'s non-ambient/direct-hit branch, not by assuming feature 011's ambient-path finding transfers) that CE has the same `CoveredByNaturalArmor`-gated pawn-stat-reading gap for Sharp/Blunt damage that feature 011 found for Heat. The fix — a CE-gated direct `dinfo.Amount` reduction in `TryAbsorbIncomingDamage`, gated on `CombatExtendedInterop.IsLoaded` — is the same proven pattern, requiring zero interface change since `dinfo` is already passed by `ref` |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines the required in-game Dev Mode scenarios covering Tier 1 armor bonus and damage reduction (including the CE debug-tool caveat, research.md R3), the non-physical-damage exclusion, the progression counter and Tier 2 upgrade, the Tier 2 full-negate roll, the no-tattoo/removal no-ops, persistence, the removal guard, and a regression/CE-load check against existing incoming-damage consumers (Ember Ward) |
| III. Data-Driven, Tunable Balance | Yes — Tier 1/Tier 2 armor-rating bonus, the Tier 2 full-negate chance, the Tier 2 threshold | **PASS** — every value lives on `HediffCompProperties_IronskinGlyphEffect` XML fields (data-model.md) and is read exclusively through `TattooEffectValues` (FR-012), never as an inline magic number; placeholders trace to PRD §9 via the same XML comment convention every prior tattoo carries |
| IV. Two-Path Combat Patching | Only required for targeting/AI-decision logic (Guardian's Call is the PRD example); this feature adds no targeting or attack-target-selection interception of any kind | **PASS (N/A)** — same non-applicability as every passive/triggered tattoo except Guardian's Call; this feature reuses an existing damage-resolution dispatch point, not verb selection or targeting AI |
| V. Progression Integrity Across Saves | Yes — Ironskin Glyph's own tier/progress counter | **PASS** — persists via `CompExposeData()`'s `Scribe_Values.Look` calls through the composed `TattooTierProgress` (data-model.md); tier-crossing logic reuses `TattooTierProgress.TryRegisterQualifyingEvent` unmodified, already idempotent and deterministic (feature 002 precedent); no time-boxed/derived state exists to reconstruct on load, unlike Vampiric Thorn's post-kill window |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` confirms the design adds exactly one new `HediffComp` (+ its properties
class), implementing three existing, unmodified interfaces (`IProvidesTattooStatOffset`,
`IOnIncomingDamageTattooEffect`, `IProvidesTattooTierProgress`) — zero new interfaces, zero new Harmony patches,
zero new `TattooEffectStatPartInstaller` registrations. Table above still holds; no new violations. This is the
smallest-footprint passive tattoo shipped so far in terms of new shared infrastructure, exactly matching User Story
3's own expectation.

## Project Structure

### Documentation (this feature)

```text
specs/012-ironskin-glyph-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

No `contracts/` directory — unlike feature 011 (which added a brand-new reuse point,
`IOnIncomingDamageTattooEffect`, documented in `contracts/incoming-damage-contract.md`), this feature introduces no
new interface or reusable infrastructure to document a contract for (research.md R1; mirrors feature 009's own
precedent of skipping `contracts/` when a passive tattoo needed none).

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── Tattoos/
        └── IronskinGlyph.xml                    # UPDATED — add HediffCompProperties_IronskinGlyphEffect
                                                    #   entry with tier1/tier2 numeric fields
                                                    #   (data-model.md), replacing the "effect not yet
                                                    #   implemented" stub description; hediffClass stays
                                                    #   HediffWithComps

Source/
└── Hediffs/
    └── HediffComp_IronskinGlyphEffect.cs        # NEW — Ironskin Glyph-specific: composes
                                                    #   TattooTierProgress, implements
                                                    #   IProvidesTattooStatOffset (ArmorRating_Sharp +
                                                    #   ArmorRating_Blunt, research.md R5) and the existing
                                                    #   IOnIncomingDamageTattooEffect (progression counter +
                                                    #   CE-gated direct reduction + Tier 2 full-negate roll,
                                                    #   research.md R3-R4) — no other file changes
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–011 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) — no new project or scaffold. Ironskin Glyph's own comp lives in
`Source/Hediffs/`, mirroring every other tattoo-effect comp's placement exactly (including `HediffComp_
EmberWardEffect.cs`, its closest sibling). No file under `Source/Effects/` or `Source/Patches/` is touched at all —
every piece of shared infrastructure this feature needs (`TattooEffectValues`, `TattooTierProgress`,
`IProvidesTattooStatOffset`, `StatPart_TattooEffectOffset`, `TattooEffectStatPartInstaller`,
`IOnIncomingDamageTattooEffect`, `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`, `CombatExtendedInterop`,
`TattooHediffRemovalGuard`) already exists exactly as feature 011 left it, satisfying FR-013 and User Story 3's own
scope note precisely as predicted — the only genuinely new work this feature's planning phase produced was the CE
non-ambient-path research finding (research.md R3), not new shared code.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
