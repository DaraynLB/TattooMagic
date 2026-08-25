# Implementation Plan: Guardian's Call Tattoo Effect (First Triggered Ability + Two-Path AI Targeting Slice)

**Branch**: `004-guardians-call-tattoo-effect` | **Date**: 2026-08-07 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-guardians-call-tattoo-effect/spec.md`

## Summary

Turn Guardian's Call from an inert stub Hediff (shipped by feature 001) into a working PRD §6 tattoo: an
activatable taunt ability (gizmo + cooldown) that forces nearby hostiles to prioritize attacking the tattooed
pawn for a duration, upgrading in place to a Tier 2 that additionally grants a temporary armor buff for that
duration, driven by the same "times activated" progression-counter pattern as Frost Sigil/Serpent's Eye (PRD
§5.4). This is the first **triggered** tattoo (vs. features 002/003's passive tattoos) and the only one that
touches AI targeting, so it stands up two genuinely new pieces of reusable infrastructure documented in
`contracts/triggered-tattoo-effect-contract.md` (a gizmo/cooldown reuse point, since no DLC-free ability
framework exists to build on) and `contracts/two-path-targeting-contract.md` (the vanilla+CE dual-patch
convention Constitution Principle IV requires), while reusing `TattooEffectValues`, `TattooTierProgress`,
`IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset`, and `CombatExtendedInterop` from features 002/003
entirely unmodified.

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–003
and this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (a new postfix on `Pawn.GetGizmos()` for the gizmo reuse point,
and a new postfix on `Verse.AI.AttackTargetFinder.BestAttackTarget` for the taunt), `Krafs.Rimworld.Ref` 1.6.*
(game API reference assemblies) — same as features 001–003, no new external dependency; no DLC dependency
(confirmed research.md R1 — the vanilla `Ability`/`AbilityDef` framework is DLC-gated and explicitly not used)

**Storage**: RimWorld's built-in Scribe save/load system — Guardian's Call's tier, progression counter,
cooldown-end tick, and taunt-end tick persist as `HediffComp` state on the existing
`TattooMagic_Hediff_GuardiansCall` hediff, exactly like features 002/003's comps

**Testing**: No automated unit-test harness (unchanged from features 001–003 — RimWorld's `Verse`/`RimWorld`
game types, and `Verse.AI.AttackTargetFinder` specifically, aren't practically runnable outside the game
process); validated via manual in-game verification in Dev Mode per Constitution Principle II, using
`quickstart.md`, with a **required** (not optional) Combat Extended pass per Constitution Principle IV

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime)

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–003; no new project/scaffold needed

**Performance Goals**: The new `Pawn.GetGizmos()` postfix runs only on gizmo-row refresh (selection/UI), not
per-tick; the new `AttackTargetFinder.BestAttackTarget` postfix runs on a genuinely hot AI path (every hostile
targeting decision), so it MUST stay cheap when no taunt is active — research.md R3's `GuardiansCallTauntRegistry`
keeps the common (no active taunts) case a single empty-set check rather than a per-call hediff scan across all
map pawns

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony +
CE (Constitution Principle I); per research.md R6, no CE-specific armor StatDef exists — CE reuses vanilla's
`ArmorRating_Sharp`/`Blunt`/`Heat`, so the Tier 2 buff needs no CE branching, but the *targeting* patch's
CE-path is a genuine open question (research.md R5) that MUST be resolved by in-game verification, not assumed,
before this feature can be marked done; all numeric balance values remain placeholders pending PRD §9's balance
pass, routed through `TattooEffectValues` per FR-011

**Scale/Scope**: One tattoo's gameplay effect (Guardian's Call) + two small reusable infrastructure pieces
(the gizmo/cooldown reuse point, the two-path targeting convention) intended for reuse by up to 4 more
triggered tattoos and any future targeting-influencing tattoo later (not built now) — no changes to features
001–003's own ritual/tracker/passive-effect systems beyond the sanctioned `TattooEffectStatPartInstaller`
extension point (three new `Register(...)` calls for armor StatDefs)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Yes — this is the first tattoo touching targeting AI and combat stats (armor) | **PASS** — research.md R6 confirms no CE-specific armor StatDef exists (no branching needed there); research.md R5 requires the targeting patch's CE behavior to be empirically verified (quickstart Scenario 5) rather than assumed, with a defined fallback (a second reflection-based patch) if verification shows CE bypasses the vanilla entry point — either outcome satisfies "neither throw nor silently no-op in either configuration" |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines required in-game Dev Mode scenarios including a mandatory CE-loaded pass (Scenario 5) with temporary `Log.Message` probes (research.md R5) confirming the exact live behavior rather than trusting documentation about CE's internals |
| III. Data-Driven, Tunable Balance | Yes — taunt range/duration, cooldown, tier threshold, armor buff magnitude | **PASS** — every value lives in `HediffCompProperties_GuardiansCallEffect` XML fields (data-model.md) and is read exclusively through `TattooEffectValues` (FR-011), never as an inline magic number; placeholders trace to PRD §9 |
| IV. Two-Path Combat Patching | Yes — this is the PRD's own named example (Guardian's Call's taunt) | **PASS** — `contracts/two-path-targeting-contract.md` documents a vanilla postfix on `AttackTargetFinder.BestAttackTarget` (research.md R4) verified independently, plus a CE path resolved via mandatory in-game verification with a concrete fallback patch if needed (research.md R5) — both required to pass before the feature is done, not just the vanilla path |
| V. Progression Integrity Across Saves | Yes — Guardian's Call's tier/progression counter, plus new cooldown-end/taunt-end ticks | **PASS** — `tierState` reuses `TattooTierProgress`'s already-proven `ExposeData()`/idempotent tier-up mechanism unmodified; the two new tick fields are plain `Scribe_Values.Look`'d ints on the same `HediffComp`, no new persistence mechanism |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` and the two new contracts introduce one new interface
(`IProvidesTattooGizmo`), two new shared/tattoo-scoped Harmony patches (the gizmo postfix, tattoo-agnostic; the
targeting postfix, Guardian's-Call-specific), one Guardian's-Call-specific static registry, and three new
`Register(...)` calls in the existing `TattooEffectStatPartInstaller`. The targeting patch is the only piece
touching Principle IV/combat AI, and its design (research.md R4) always lets vanilla's own algorithm run to
completion before conditionally overriding an already-legal result, so it cannot leave a hostile targetless.
Table above still holds; no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/004-guardians-call-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── triggered-tattoo-effect-contract.md
│   └── two-path-targeting-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── Tattoos/
        └── GuardiansCall.xml               # UPDATED — add HediffCompProperties_GuardiansCallEffect
                                             #   entry with tier1/tier2 numeric fields (data-model.md)

Source/
├── Effects/                                # EXISTING (feature 002) — reusable tattoo-agnostic
│   │                                        #   infrastructure
│   ├── IProvidesTattooGizmo.cs             # NEW — gizmo-contribution contract (research.md R1)
│   └── TattooEffectStatPartInstaller.cs    # UPDATED — register StatPart_TattooEffectOffset onto
│                                            #   ArmorRating_Sharp/Blunt/Heat (research.md R6); no other
│                                            #   change to this file's existing registrations
├── Hediffs/
│   ├── HediffComp_GuardiansCallEffect.cs   # NEW — Guardian's Call-specific: composes
│   │                                        #   TattooTierProgress, implements IProvidesTattooGizmo +
│   │                                        #   IProvidesTattooStatOffset, owns cooldownEndTick/
│   │                                        #   tauntEndTick
│   └── GuardiansCallTauntRegistry.cs       # NEW — Guardian's-Call-specific active-taunter bookkeeping
│                                            #   consulted by the targeting patch (research.md R3)
└── Patches/
    ├── Patch_Pawn_GetGizmos_TattooGizmos.cs                        # NEW — shared, tattoo-agnostic
    │                                                                #   postfix on Pawn.GetGizmos()
    │                                                                #   (research.md R1)
    ├── Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs   # NEW — vanilla targeting
    │                                                                #   path (research.md R4)
    └── Patch_CE_AttackTargetFinder_GuardiansCallTaunt.cs           # NEW — CE targeting path, added
                                                                     #   only if in-game verification
                                                                     #   shows it's needed (research.md
                                                                     #   R5 Outcome B); TryApply(harmony)
                                                                     #   called from TattooMagicMod.cs
                                                                     #   alongside the existing CE patch
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–003 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) rather than introducing a new project or scaffold. The new gizmo interface
lives alongside features 002/003's other reusable pieces in `Source/Effects/`, since it's tattoo-agnostic
infrastructure in the same spirit as `IProvidesTattooStatOffset`. Guardian's Call's own comp and its taunt
registry live in `Source/Hediffs/` next to the other tattoo-specific comps. All three new Harmony patches join
the existing `Source/Patches/` folder, following the established `Patch_<Type>_<Method>_<Purpose>` naming
convention; the CE targeting patch mirrors `Patch_CE_ProjectileImpact_TattooAmmoBonus`'s imperative
(non-attribute-discovered) application, added conditionally per research.md R5's verification outcome.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
