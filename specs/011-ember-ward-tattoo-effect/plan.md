# Implementation Plan: Ember Ward Tattoo Effect (Seventh Passive Tattoo, Burn Resistance Slice)

**Branch**: `011-ember-ward-tattoo-effect` | **Date**: 2026-08-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/011-ember-ward-tattoo-effect/spec.md`

## Summary

Turn Ember Ward from an inert stub Hediff (shipped by feature 001) into a working PRD §6 tattoo: a permanent
heat-resistance bonus and burn-damage-reduction offset, upgrading in place to stronger values plus a Tier
2-exclusive chance to fully ignore a fire/burn instance, via a "fire/burn damage absorbed" progression counter
(PRD §5.4), reusing feature 002's value accessor (`TattooEffectValues`) and tier tracker (`TattooTierProgress`)
unmodified. This is the seventh passive tattoo. Two of its three effect components reuse existing infrastructure
with zero or near-zero new code: heat resistance is a new `StatDefOf.ComfyTemperatureMax` registration
(mirroring Frost Sigil's existing `ComfyTemperatureMin` offset) and burn-damage reduction is a contribution to
`StatDefOf.ArmorRating_Heat` — already registered by an earlier feature but unused until now — which decompiling
RimWorld's and Combat Extended's own armor code (research.md R3) confirms both games resolve identically for
`Flame`/`Burn` damage, needing **zero CE-specific branching**. The third component — the progression counter and
the Tier 2 full-ignore roll — cannot reuse the existing `Patch_Pawn_PostApplyDamage_TattooOnHit` postfix (research
.md R4: it runs too late to prevent damage, and its own guards would wrongly exclude both full-ignores and
no-pawn-instigator environmental fire), so this feature adds one new, tattoo-agnostic Harmony prefix on
`Pawn.PreApplyDamage` and one new reuse point, `IOnIncomingDamageTattooEffect`, dispatched from it.

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–010 and
this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (one new patch, `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`, on
`Pawn.PreApplyDamage` — distinct from the existing `Patch_Pawn_PostApplyDamage_TattooOnHit` family, per research
.md R4), `Krafs.Rimworld.Ref` 1.6.* — same as features 001–010, no new external dependency, no DLC dependency.
Combat Extended interaction confirmed by decompiling the real, locally installed `CombatExtended.dll` (Steam
Workshop item `2890901044`): CE's `ArmorUtilityCE` resolves `Flame`/`Burn`'s `Heat` armor category to
`StatDefOf.ArmorRating_Heat` exactly as vanilla does (research.md R3), and CE does not intercept
`Pawn.PreApplyDamage` (research.md R4) — no CE-specific branching needed anywhere in this feature.

**Storage**: RimWorld's built-in Scribe save/load system — Ember Ward's tier and progression counter persist as
`HediffComp` state on the existing `TattooMagic_Hediff_EmberWard` hediff, the exact same shape every prior
tattoo's own comp already uses.

**Testing**: No automated unit-test harness (unchanged from features 001–010 — RimWorld's `Verse`/`RimWorld` game
types aren't practically runnable outside the game process); validated via manual in-game verification in Dev Mode
per Constitution Principle II, using `quickstart.md`. The CE-loaded pass is a standing "loads cleanly and behaves
correctly with CE present" check that research.md R3/R4 predict will show no CE-specific behavioral difference,
not a CE-only code path the way Serpent's Eye's own CE scenario is.

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime). The
`Pawn.PreApplyDamage` signature, the `Flame`/`Burn` `DamageDef`s' `armorCategory` XML, `Verse.ArmorUtility`'s
armor-roll logic, and CE's `ArmorUtilityCE`/`Harmony_DamageWorker_AddInjury_ApplyDamageToPart` were all grounded
against the real, installed `Assembly-CSharp.dll` and `CombatExtended.dll` (decompiled read-only via `ilspycmd`,
no game code executed) rather than assumed, mirroring features 007/010's own decompile-to-verify approach.

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–010; no new project/scaffold needed.

**Performance Goals**: The new `Pawn.PreApplyDamage` prefix runs once per incoming `DamageInfo` per pawn (an
existing, already-frequent vanilla call site regardless of this feature) and iterates each pawn's hediff comps —
the same per-hit cost shape the existing `Patch_Pawn_PostApplyDamage_TattooOnHit` dispatch loops already have;
`IOnIncomingDamageTattooEffect` implementers are expected to early-return `false` for non-matching `DamageDef`s in
O(1), so the added cost for pawns without a matching tattoo is one interface-type check per comp, negligible
against the surrounding damage-application work RimWorld already performs unconditionally. The `ArmorRating_Heat`
/ `ComfyTemperatureMax` stat offsets add no new per-tick cost — they're queried by the existing `StatPart`
machinery only when something reads those stats, the same as every other tattoo's stat offset.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony + CE
(Constitution Principle I) — expected to be trivially satisfied since research confirms this feature touches no
CE-replaced stat and no CE-patched method (research.md R3-R4). All tunable numbers (Tier 1/Tier 2 heat resistance,
Tier 1/Tier 2 `ArmorRating_Heat` offset, the Tier 2 full-ignore chance, the Tier 1→2 threshold) remain placeholders
pending PRD §9's balance pass, routed through `TattooEffectValues` per FR-012, using deliberately small/
fast-to-test values rather than "plausible-looking" ones (feature 006 research.md R8 precedent).

**Scale/Scope**: One existing inert `HediffDef` (`TattooMagic_Hediff_EmberWard`) gains a `<comps>` block; one new
`HediffComp` (the effect itself — tier progression, two stat offsets, the Tier 2 full-ignore roll); one new
reusable interface (`IOnIncomingDamageTattooEffect`); one new reusable Harmony patch
(`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`); one new registration line in the existing
`TattooEffectStatPartInstaller` (`ComfyTemperatureMax`) — no new `HediffDef`, no changes to Frost Sigil's,
Serpent's Eye's, Guardian's Call's, Stormlash's, Bloodrune's, Wraithstep's, Berserker's Mark's, or Vampiric
Thorn's own files, and no changes to the existing `Patch_Pawn_PostApplyDamage_TattooOnHit` file.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Yes — grants an offset to a combat armor stat and adds a new damage-pipeline patch | **PASS** — research.md R3 confirms (by decompiling `ArmorUtilityCE`) that CE resolves `Flame`/`Burn`'s `Heat` armor category to the exact same `StatDefOf.ArmorRating_Heat` vanilla uses, no CE-replacement stat exists for this category; research.md R4 confirms CE does not patch or bypass `Pawn.PreApplyDamage`. No `if (CombatExtendedInterop.IsLoaded)` branch exists anywhere in this feature's design — both mechanisms are CE-correct by construction |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines the required in-game Dev Mode scenarios covering Tier 1 heat resistance and damage reduction, the non-fire/burn exclusion, the progression counter and Tier 2 upgrade, the Tier 2 full-ignore roll, the no-tattoo/removal no-ops, persistence, the removal guard, and a regression/CE-load check against existing on-hit consumers |
| III. Data-Driven, Tunable Balance | Yes — Tier 1/Tier 2 heat resistance, Tier 1/Tier 2 `ArmorRating_Heat` offset, the Tier 2 full-ignore chance, the Tier 2 threshold | **PASS** — every value lives on `HediffCompProperties_EmberWardEffect` XML fields (data-model.md) and is read exclusively through `TattooEffectValues` (FR-012), never as an inline magic number; placeholders trace to PRD §9 via the same XML comment convention every prior tattoo carries |
| IV. Two-Path Combat Patching | Only required for targeting/AI-decision logic (Guardian's Call is the PRD example); this feature adds no targeting or attack-target-selection interception of any kind | **PASS (N/A)** — same non-applicability as every passive/triggered tattoo except Guardian's Call; this feature's new patch hooks a damage-resolution step, not verb selection or targeting AI |
| V. Progression Integrity Across Saves | Yes — Ember Ward's own tier/progress counter | **PASS** — persists via `CompExposeData()`'s `Scribe_Values.Look` calls through the composed `TattooTierProgress` (data-model.md); tier-crossing logic reuses `TattooTierProgress.TryRegisterQualifyingEvent` unmodified, already idempotent and deterministic (feature 002 precedent); no time-boxed/derived state exists to reconstruct on load, unlike Vampiric Thorn's post-kill window |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` and `contracts/incoming-damage-contract.md` confirm the design adds one
new interface (`IOnIncomingDamageTattooEffect`), one new Harmony patch
(`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`, a prefix on a method no other patch in this codebase or in CE
intercepts), and one additive registration line in the existing `TattooEffectStatPartInstaller` — none touch
targeting/AI logic or any CE-replaced mechanism, and all persisted state routes through the same Scribe-backed
`HediffComp` mechanism every prior feature already established. Table above still holds; no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/011-ember-ward-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── incoming-damage-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── Tattoos/
        └── EmberWard.xml                       # UPDATED — add HediffCompProperties_EmberWardEffect
                                                  #   entry with tier1/tier2 numeric fields
                                                  #   (data-model.md), replacing the "effect not yet
                                                  #   implemented" stub; hediffClass stays HediffWithComps

Source/
├── Effects/                                     # Existing reusable passive-tattoo-effect infrastructure
│   │                                             #   (features 002/003/010) — TattooEffectValues,
│   │                                             #   TattooTierProgress, IProvidesTattooStatOffset,
│   │                                             #   StatPart_TattooEffectOffset,
│   │                                             #   TattooEffectStatPartInstaller (gets one new
│   │                                             #   registration line, see below), CombatExtendedInterop,
│   │                                             #   TattooHediffRemovalGuard all reused UNMODIFIED except
│   │                                             #   the one noted registration addition
│   ├── TattooEffectStatPartInstaller.cs         # UPDATED — adds Register(StatDefOf.ComfyTemperatureMax);
│   │                                             #   one new line; every existing registration (including
│   │                                             #   the already-present, previously-unused
│   │                                             #   ArmorRating_Heat) is untouched
│   └── IOnIncomingDamageTattooEffect.cs         # NEW — the pre-armor incoming-damage reuse point;
│                                                 #   tattoo-agnostic (FR-013)
├── Hediffs/
│   └── HediffComp_EmberWardEffect.cs            # NEW — Ember Ward-specific: composes TattooTierProgress,
│                                                 #   implements IProvidesTattooStatOffset (heat resistance +
│                                                 #   ArmorRating_Heat) and IOnIncomingDamageTattooEffect
│                                                 #   (progression counter + Tier 2 full-ignore roll)
└── Patches/
    └── Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs   # NEW — dispatches IOnIncomingDamageTattooEffect
                                                            #   to the about-to-be-damaged pawn's own comps,
                                                            #   for every incoming DamageInfo; distinct from
                                                            #   the existing Patch_Pawn_PostApplyDamage_
                                                            #   TattooOnHit family (research.md R4), which
                                                            #   is entirely untouched by this feature
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–010 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) — no new project or scaffold. The new reusable pieces land in the existing
`Source/Effects/` folder (features 002/003's home for tattoo-agnostic infrastructure), and the one new patch lives
in the existing `Source/Patches/` folder (feature 002's home for shared Harmony patches), keeping the "shared
infrastructure vs. tattoo-specific effect" boundary those features established. Ember Ward's own comp lives in
`Source/Hediffs/`, mirroring every other tattoo-effect comp's placement exactly.
`TattooEffectStatPartInstaller.cs` is the one existing shared-infrastructure file this feature touches, and only
to add one additive registration line — not to change any existing registration or any other tattoo's stat-offset
behavior. No feature 002/003/004/005/006/007/008/009/010 tattoo-specific file is modified, and
`Patch_Pawn_PostApplyDamage_TattooOnHit.cs` (the existing on-hit dispatch patch) is untouched entirely, since
research.md R4 established this feature needs a structurally different patch, not an extension of that one.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
