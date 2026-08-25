# Implementation Plan: Serpent's Eye Tattoo Effect (Second Passive Tattoo, CE-Aware Slice)

**Branch**: `003-serpent-eye-tattoo-effect` | **Date**: 2026-08-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-serpent-eye-tattoo-effect/spec.md`

## Summary

Turn Serpent's Eye from an inert stub Hediff (shipped by feature 001) into a working PRD §6 tattoo: passive
ranged-accuracy bonus, upgrading in place to a stronger Tier 2 via a "ranged hits landed" progression counter
(PRD §5.4), reusing feature 002's value accessor (`TattooEffectValues`), stat-offset contract
(`IProvidesTattooStatOffset` + `StatPart_TattooEffectOffset`), and tier tracker (`TattooTierProgress`)
unmodified. Unlike Frost Sigil, this tattoo's effect genuinely branches on Combat Extended's presence at two
distinct points: (1) the same vanilla `StatDefOf.ShootingAccuracyPawn` stat CE itself repurposes from "shooting
accuracy" into "weapon handling" (driving sway/recoil, per CE's own in-game stat description) — so this
feature's stat-offset comp must offset a *different* meaning of that stat depending on CE's presence, and
additionally offset CE's own `AimingAccuracy` StatDef (CE's real accuracy-equivalent) only when CE is loaded;
(2) Tier 2's CE-only ammo-effectiveness bonus, which has no stock CE StatDef to hook (confirmed by inspecting
`CombatExtended.dll`'s actual Stat/Ammo defs) and is instead delivered via a reflection-based Harmony patch on
`CombatExtended.ProjectileCE.Impact`, scaling that one in-flight projectile instance's damage — never touching
shared `AmmoDef`/weapon data other pawns rely on. Alongside Serpent's Eye itself, this feature adds two new
reusable pieces for future tattoos: an `IOnRangedHitLandedTattooEffect` reuse point (the ranged-attacker-side
counterpart to feature 002's melee-defender-side `IOnMeleeHitTattooEffect`) and a small `CombatExtendedInterop`
helper that becomes this project's first concrete CE-detection convention. This slice also delivers FR-015, a
customer-mandated, cross-tattoo hardening requirement surfaced during this feature's work (not specific to
Serpent's Eye): no code outside this mod can remove an applied tattoo hediff through RimWorld's normal removal
API, while Developer Mode/God Mode's own removal tooling is deliberately left working. Because it's keyed off
every `TattooMagicDef.appliedHediff` generically, it also closes this gap for the already-shipped Frost Sigil
hediff without touching feature 002's own code.

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001/002 and
this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (the shared `Pawn.PostApplyDamage` postfix extension, plus a new
reflection-based patch onto Combat Extended's `ProjectileCE.Impact`), `Krafs.Rimworld.Ref` 1.6.* (game API
reference assemblies) — same as features 001/002, no new compile-time dependency. Combat Extended itself
remains a runtime-detected soft dependency only (`ModsConfig.IsActive("CETeam.CombatExtended")`, confirmed as
CE's actual `packageId` by inspecting the installed mod's own `About.xml`); its types are never referenced at
compile time — the ammo-effectiveness patch resolves `CombatExtended.ProjectileCE` via Harmony's `AccessTools`
reflection helpers at runtime, only when CE is detected.

**Storage**: RimWorld's built-in Scribe save/load system — Serpent's Eye's tier and progression counter persist
as `HediffComp` state on the existing `TattooMagic_Hediff_SerpentsEye` hediff, exactly like Frost Sigil's
`HediffComp_FrostSigilEffect.tierState`; no external storage

**Testing**: No automated unit-test harness (unchanged from features 001/002 — RimWorld's `Verse`/`RimWorld`
game types aren't practically runnable outside the game process); validated via manual in-game verification in
Dev Mode per Constitution Principle II, using `quickstart.md`, with required passes in both a non-CE game and a
CE-loaded game (this feature is the first to make that dual pass load-bearing rather than incidental)

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime), verified against a
real local Combat Extended install (Steam Workshop item `2890901044`, `CETeam.CombatExtended`, v16.7.3.0) during
planning to ground every CE-specific decision below in the mod's actual shipped Defs/assembly rather than
assumption

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001/002; no new project/scaffold needed

**Performance Goals**: No special target — the ranged-hit-landed patch and the CE ammo-effectiveness patch both
run only on actual event triggers (a shot landing / a CE projectile impacting), not per-frame or per-tick; the
stat-offset `StatPart` runs only when RimWorld already queries `ShootingAccuracyPawn`/`AimingAccuracy` (existing
stat-cache behavior applies); must not add per-tick work to pawns lacking Serpent's Eye

**Constraints**: Must load with zero Harmony/console errors with Harmony alone (the CE ammo-effectiveness patch
must not even attempt to resolve `CombatExtended.ProjectileCE` when CE is absent, per Constitution Principle I);
must also load and function with zero console errors with Harmony + CE; every tunable numeric value (accuracy
bonus, sway/recoil bonus, ammo-effectiveness multiplier, Tier 1→2 threshold, both tiers) remains a placeholder
pending PRD §9's balance pass, routed through `TattooEffectValues` per FR-011, consistent with feature 002's
FR-012

**Scale/Scope**: One tattoo's gameplay effect (Serpent's Eye) + two small reusable additions (a new interface, a
new CE-interop helper) intended for reuse by tattoos still pending (Ember Ward, Ironskin Glyph, Starlight Ward)
— no changes to features 001/002's ritual/tracker system, and no modification to feature 002's own
Frost-Sigil-specific code

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Yes — this is the feature's central concern | **PASS** — every CE-touching code path (`research.md` R3/R4/R5) is gated behind a runtime `CombatExtendedInterop.IsLoaded`/`GetNamedSilentFail` check and verified to neither throw nor silently no-op when CE is absent (spec Assumptions confirm sway/recoil and ammo-effectiveness have no vanilla equivalent, so "absent under vanilla" is correct behavior, not a gap) |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` requires both a non-CE and a CE-loaded in-game pass, per Constitution Principle II and this feature's own SC-008; the CE ammo-effectiveness patch and the dual-meaning `ShootingAccuracyPawn` offset are the highest-risk assumptions and are called out explicitly for verification, not assumed correct from research alone |
| III. Data-Driven, Tunable Balance | Yes — accuracy bonus, sway/recoil bonus, ammo-effectiveness multiplier, Tier 1→2 threshold, both tiers | **PASS** — every value lives in `HediffCompProperties_SerpentsEyeEffect` XML fields (data-model.md) and is read exclusively through `TattooEffectValues` (FR-011), never as an inline magic number; placeholders trace to PRD §9 |
| IV. Two-Path Combat Patching | Only required for targeting/AI-decision logic (Guardian's Call is the PRD example); this feature does not touch targeting or attack-target selection | **PASS (N/A)** — Serpent's Eye's patches hook stat evaluation and post-hit/post-impact chokepoints, not verb selection or targeting AI |
| V. Progression Integrity Across Saves | Yes — Serpent's Eye's tier and progression counter | **PASS** — reuses `TattooTierProgress`/`Scribe_Values` verbatim from feature 002, the same mechanism already proven for Frost Sigil; tier-up remains idempotent by construction |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` and the updated
`contracts/passive-tattoo-effect-contract.md` introduce one new interface
(`IOnRangedHitLandedTattooEffect`), one new CE-interop helper class, and one new reflection-based Harmony patch
— none touch targeting/AI logic, all persisted state routes through the same Scribe-backed `HediffComp`
mechanism feature 001/002 already established, and the one CE-only patch is additive and reflection-gated so it
cannot affect a non-CE game at all. Table above still holds; no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/003-serpent-eye-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── passive-tattoo-effect-contract.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── Tattoos/
        └── SerpentsEye.xml                 # UPDATED — add HediffCompProperties_SerpentsEyeEffect
                                             #   entry with tier1/tier2 numeric fields (data-model.md),
                                             #   replacing the "effect not yet implemented" stub

Source/
├── Effects/                                # Existing reusable passive-tattoo-effect infrastructure
│   │                                        #   (feature 002) — TattooEffectValues, TattooTierProgress,
│   │                                        #   IProvidesTattooStatOffset, StatPart_TattooEffectOffset
│   │                                        #   all reused UNMODIFIED (FR-012)
│   ├── IOnRangedHitLandedTattooEffect.cs   # NEW — the ranged-attacker-side counterpart to
│   │                                        #   IOnMeleeHitTattooEffect; tattoo-agnostic (FR-012)
│   ├── CombatExtendedInterop.cs            # NEW — [StaticConstructorOnStartup]; the single CE-detection
│   │                                        #   convention (IsLoaded flag + AimingAccuracy StatDef
│   │                                        #   resolution) future tattoos reuse (FR-013/User Story 3)
│   ├── TattooHediffRemovalGuard.cs         # NEW — FR-015; cross-tattoo (not Serpent's-Eye-specific)
│   │                                        #   registry of every TattooMagicDef.appliedHediff plus the one
│   │                                        #   sanctioned removal entry point; covers all 11 tattoos,
│   │                                        #   including Frost Sigil retroactively
│   └── TattooEffectStatPartInstaller.cs    # UPDATED — additionally registers StatPart_TattooEffectOffset
│                                            #   onto StatDefOf.ShootingAccuracyPawn (already vanilla-safe)
│                                            #   and, only if CombatExtendedInterop.IsLoaded, onto
│                                            #   CombatExtendedInterop.AimingAccuracy
├── Hediffs/
│   └── HediffComp_SerpentsEyeEffect.cs     # NEW — Serpent's Eye-specific: composes TattooTierProgress,
│                                            #   implements IProvidesTattooStatOffset (dual-meaning
│                                            #   ShootingAccuracyPawn branch + CE-only AimingAccuracy) and
│                                            #   IOnRangedHitLandedTattooEffect
└── Patches/
    ├── Patch_Pawn_PostApplyDamage_TattooOnHit.cs   # UPDATED — adds a second, sibling dispatch clause
    │                                                 #   (dinfo.Tool == null branch, dispatched to the
    │                                                 #   INSTIGATOR pawn's comps) to the same existing
    │                                                 #   postfix, alongside the untouched melee clause
    ├── Patch_CE_ProjectileImpact_TattooAmmoBonus.cs # NEW — reflection-based Harmony patch, applied
    │                                                  #   imperatively (not [HarmonyPatch]-attributed,
    │                                                  #   since the target type isn't a compile-time
    │                                                  #   reference) only when CombatExtendedInterop
    │                                                  #   .IsLoaded is true; postfixes
    │                                                  #   CombatExtended.ProjectileCE.Impact(Thing) to
    │                                                  #   scale that projectile instance's DamageAmount
    │                                                  #   when its launcher pawn has Tier 2 Serpent's Eye
    └── Patch_HealthTracker_RemoveHediff_ProtectTattoos.cs  # NEW — FR-015; [HarmonyPatch]-attributed
                                                              #   prefix on Pawn_HealthTracker.RemoveHediff,
                                                              #   picked up by the existing PatchAll() call;
                                                              #   blocks removal of any TattooHediffRemoval
                                                              #   Guard-protected hediff unless sanctioned
                                                              #   or DebugSettings.godMode is true

Source/TattooMagicMod.cs                      # UPDATED — after harmony.PatchAll(), explicitly calls
                                               #   Patch_CE_ProjectileImpact_TattooAmmoBonus.TryApply(harmony)
                                               #   since a reflection-target patch can't be picked up by
                                               #   PatchAll()'s attribute scan
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001/002 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) — no new project or scaffold. New reusable pieces land in the existing
`Source/Effects/` folder (feature 002's home for tattoo-agnostic infrastructure) and `Source/Patches/` folder
(feature 002's home for shared Harmony patches), keeping the "shared infrastructure vs. tattoo-specific effect"
boundary feature 002 established. Serpent's Eye's own comp lives in `Source/Hediffs/`, mirroring
`HediffComp_FrostSigilEffect.cs`'s placement exactly. No feature 002 file is modified except
`TattooEffectStatPartInstaller.cs` (adding registrations, not altering existing ones) and
`Patch_Pawn_PostApplyDamage_TattooOnHit.cs` (adding a sibling dispatch clause, not altering the existing melee
one) — both are explicitly shared, tattoo-agnostic infrastructure per feature 002's own contract, not "Frost
Sigil's own effect code" (FR-012's actual constraint).

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
