# Implementation Plan: Bastion Ward Tattoo Effect

**Branch**: `016-bastion-ward-tattoo-effect` | **Date**: 2026-09-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/016-bastion-ward-tattoo-effect/spec.md`

## Summary

The 16th tattoo, and the first of the newly-approved four-tattoo batch (PRD §10). A two-tier triggered ability:
casting drops a ground-fixed protective zone (does not follow the caster) inside which any ally-faction pawn takes
zero melee/ranged damage for the ward's duration; casting also ejects any hostile currently in melee combat with an
ally who ends up inside the new zone; the ability has a cooldown and the standard "times activated" tier-progression
pattern (PRD §5.4); Tier 2 gives a larger radius plus a periodic heal for anyone inside. It reuses three proven,
unmodified reuse points — the two-phase ground-targeted gizmo contract (feature 008), `TattooTierProgress`/
`TattooEffectValues` (features 002/004), and `TattooHealingUtility.HealWorstInjury` (feature 007) — but is the
mod's first tattoo whose effect protects pawns other than its own wearer, which is not shape-compatible with the
existing per-wearer-hediff `IOnIncomingDamageTattooEffect` dispatch. Resolving that (research.md R2) requires one
small, additive extension to the shared `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` prefix, backed by a new
per-save `GameComponent_BastionWardZones` registry — this mod's first ground-fixed, caster-independent, duration-
driven zone, documented generically in `contracts/ground-fixed-aoe-zone-contract.md` for any future zone-shaped
tattoo to build on. The rescue-eject mechanic reuses this mod's existing melee-lock-detection triad (Guardian's
Call/Wraithstep) for detection, paired with the same teleport-and-notify position mutation Wraithstep already uses
for a different (self) purpose; per the spec's own Clarifications session, a hostile with no valid displacement
cell is simply left un-ejected rather than blocking the cast. The clarification session also settled that the
targeted zone center is capped to a max cast range per tier (a new `rangeTier1`/`rangeTier2` pair, reusing
Wraithstep's own range-limit convention verbatim even though its walkable/occupied checks don't apply to an AoE
center). No Combat Extended-specific branching is needed anywhere in this feature (research.md R3) — one open item
(whether CE's melee verb populates the same detection fields vanilla's does) is resolved by mandatory in-game
verification per Constitution Principle II, with a documented fallback if not.

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–015 and
this project's `rimworld-modding` toolchain).

**Primary Dependencies**: `Lib.Harmony` 2.3.* — one new additive change to the existing
`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` prefix (research.md R2), no new Harmony patch target. `Krafs.
Rimworld.Ref` 1.6.* — same as features 001–015, no new external dependency, no DLC dependency. Combat Extended:
confirmed to need zero CE-specific branching for damage negation (research.md R3, since `Pawn.PreApplyDamage` —
this feature's one interception point — is already decompile-confirmed CE-safe from feature 011's own research);
one open verification item for the rescue-eject mechanic's melee-detection fields under CE (research.md R5),
resolved by `quickstart.md` Scenario 6 rather than assumed.

**Storage**: RimWorld's built-in Scribe save/load system, split across two owners for the first time in this mod
(research.md R4): `HediffComp_BastionWardEffect`'s tier/progression-counter/cooldown state via `CompExposeData()`
on the Bastion Ward hediff (same mechanism every prior triggered tattoo already uses), and — genuinely new —
each active ward's own position/radius/remaining-duration/tier-at-cast/heal state via a new per-save
`GameComponent_BastionWardZones` (`Scribe_Collections` with `LookMode.Deep` over a list of plain `IExposable`
`BastionWardZone` objects), independent of any pawn, mirroring `GameComponent_PhoenixRegistry`'s (feature 014)
self-registering shape.

**Testing**: No automated unit-test harness (unchanged from features 001–015). Validated via manual in-game
verification in Dev Mode per Constitution Principle II, using `quickstart.md`'s six scenarios, including a
required (not optional) Combat Extended pass per Constitution Principle IV's spirit — even though this feature adds
no targeting-AI-interception patch of the kind Principle IV names, the rescue-eject mechanic reads AI-decision
state that could differ under CE, so it still gets a mandatory CE verification pass.

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime).

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–015; no new project/scaffold needed.

**Performance Goals**: `GameComponent_BastionWardZones.GameComponentTick()` runs once per game tick but does real
work only when `activeZones` is non-empty (the common case, given ~8-second placeholder durations and a cooldown,
is usually zero-to-few concurrent zones) — expiry is an `int` comparison per zone, and the heal pass is
additionally self-gated to once per `healIntervalTicksTier2` (placeholder 60 ticks) per zone, not every tick.
`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`'s new check only runs when a pawn is actually about to take damage
(already a bounded, non-hot-path call site per feature 011's own precedent) and short-circuits to a cheap
zero-zones check (`Get()?.TryAbsorb(...)`) when no ward is active anywhere, mirroring
`GuardiansCallTauntRegistry`'s (feature 004) own "cheap when nothing active" performance bar.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony + CE
(Constitution Principle I) — satisfied with no CE-specific code for damage negation (research.md R3); the rescue-
eject mechanic's CE behavior is verified, not assumed (research.md R5, quickstart.md Scenario 6), with a documented
fallback if verification shows a gap. All numeric balance values (duration, radius per tier, max cast range per
tier, cooldown, Tier 2 threshold, heal magnitude/interval) remain placeholders pending PRD §9's balance pass,
routed through `TattooEffectValues` (Constitution Principle III, FR-016).

**Scale/Scope**: One new tattoo `HediffDef`/`TattooMagicDef` pair (plain `HediffWithComps`, no custom `Hediff`
subclass); one new `HediffComp` (`HediffComp_BastionWardEffect`, composing the shared `TattooTierProgress`); one
new `GameComponent` + one new plain `IExposable` zone-state class (this mod's first ground-fixed zone
infrastructure, research.md R4); one new `MapComponent` (FR-005a's ground indicator — purely visual, owns no
state of its own, this mod's first `MapComponent` of any kind); one small additive change to one existing shared
Harmony patch (`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`, research.md R2) — no new Harmony patch target, no
CE-specific patch; one new `contracts/` doc generalizing the ground-fixed-zone shape for future reuse. Zero changes to any other
existing tattoo's own files, `IOnIncomingDamageTattooEffect`, `IOnMeleeHit*TattooEffect`,
`TattooTierProgress`/`TattooEffectValues` themselves, or `Dialog_ChooseTattoo` (no rarity cap, same reasoning as
Guardian's Call/Wraithstep).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Yes — this feature's damage negation intercepts a combat path, and its rescue-eject reads AI-decision state | **PASS** — research.md R3 confirms, via feature 011's own decompile of `CombatExtended.dll`, that `Pawn.PreApplyDamage` (this feature's only damage-interception point) is not bypassed by CE, so full negation needs no CE-specific branching at all. The rescue-eject's melee-detection fields under CE are an open item resolved by mandatory in-game verification (research.md R5, quickstart.md Scenario 6) with a documented fallback, not assumed — the same resolution pattern Constitution Principle I/feature 004's own research.md R5 already established as acceptable for this project. |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines six required in-game Dev Mode scenarios covering all three user stories, all Edge Cases, save/reload persistence, and a mandatory Combat Extended pass (Scenario 6), leaning on `TattooMagicSettings.EnableDebugLogging` probes per this project's proactive-debug-logging practice to make the zone registry's otherwise-invisible state (registration/expiry/absorb/heal/ejection decisions) directly observable. |
| III. Data-Driven, Tunable Balance | Yes — duration, radius per tier, max cast range per tier, cooldown, Tier 2 threshold, heal magnitude and interval | **PASS** — every value lives on `HediffCompProperties_BastionWardEffect` XML fields (data-model.md) and is read exclusively through `TattooEffectValues.Get(...)` at point of use (FR-016), matching every triggered tattoo shipped so far; placeholders trace to this spec's own Assumptions section via the standard XML top-of-file comment convention. The one deliberately non-XML value (the displacement search radius) is not a balance number per FR-016's own explicit list — an implementation detail, not a tuning surface. |
| IV. Two-Path Combat Patching | Only required for targeting/AI-decision-*overriding* logic (the taunt-targeting precedent it names); this feature's rescue-eject *reads* existing AI-decision fields to reposition a pawn rather than overriding `AttackTargetFinder`'s own algorithm, and negates damage via the existing CE-safe `Pawn.PreApplyDamage` point rather than a second, CE-specific damage patch | **PASS (N/A for a dual-patch requirement)** — no targeting-AI interception of the `BestAttackTarget`-override kind this principle names exists anywhere in this feature. The rescue-eject's CE-field question is still treated with the same rigor (mandatory verification, documented fallback, research.md R5) the principle's spirit calls for, even though it doesn't trigger the letter of "ship a distinct patch for each path." |
| V. Progression Integrity Across Saves | Yes — Bastion Ward's own tier/progression counter/cooldown, plus (new) each active ward's own position/radius/remaining-duration/tier-at-cast/heal state | **PASS** — the tier/counter/cooldown half persists via `CompExposeData()` exactly like every prior triggered tattoo (data-model.md), reusing `TattooTierProgress.ExposeData()` unmodified. The new active-ward half persists via `GameComponent_BastionWardZones.ExposeData()`/`Scribe_Collections` with `LookMode.Deep`, each `BastionWardZone.ExposeData()` using absolute `TicksGame` values for `expireTick`/`nextHealTick` (not relative counters), so a reload simply resumes comparing against the same absolute tick — deterministic and idempotent with no special "resuming a wait" logic needed. |

No violations requiring justification — **Complexity Tracking is not needed** for this feature. The one
deliberate deviation from "never modify a shared, tattoo-agnostic patch for a single tattoo" (extending
`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`, research.md R2) is necessary, not incidental: Bastion Ward is
architecturally the first tattoo whose protection is not carried by the protected pawn's own hediffs, so the
existing per-hediff-comp dispatch cannot express it without this addition, and the change is purely additive
(appended after the existing loop, changing nothing for Ember Ward/Ironskin Glyph/any future self-only consumer).

*Post-Phase 1 re-check*: `data-model.md` and `contracts/ground-fixed-aoe-zone-contract.md` confirm the design above
holds with no new violations. Every piece of new surface (the comp, the `GameComponent`, the zone class, the one
additive patch change) is required directly by this feature's own FRs, not speculative infrastructure — research.md
R4/R6's own "no premature abstraction" reasoning applies equally to the new `GameComponent`'s zone-state shape
(kept Bastion-Ward-specific rather than generalized ahead of a second real consumer) and to declining a new
`IHealsNearbyAlliesTattooEffect`-style interface. Table above still holds; no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/016-bastion-ward-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── ground-fixed-aoe-zone-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Defs/
├── TattooDefs/
│   └── BastionWard.xml                          # NEW — the recipe-side TattooMagicDef (tattooType=
│                                                       #   Triggered, appliedHediff=TattooMagic_Hediff_
│                                                       #   BastionWard, standard ingredients/workAmount
│                                                       #   shape, same convention as GuardiansCall.xml)
└── HediffDefs/
    └── Tattoos/
        └── BastionWard.xml                       # NEW — TattooMagic_Hediff_BastionWard, HediffWithComps
                                                        #   + HediffCompProperties_BastionWardEffect
                                                        #   (data-model.md), every numeric a placeholder
                                                        #   pending this spec's own Assumptions pass

Source/
├── Defs/
│   └── TattooMagicDefOf.cs                       # UPDATED — one new entry: TattooMagic_Hediff_
│                                                       #   BastionWard
├── Effects/                                       # EXISTING — reusable tattoo-agnostic infrastructure
│   ├── GameComponent_BastionWardZones.cs         # NEW — per-save zone registry (data-model.md):
│   │                                                   #   RegisterZone/TryAbsorb/GameComponentTick/
│   │                                                   #   ExposeData; mirrors GameComponent_
│   │                                                   #   PhoenixRegistry's self-registering shape
│   │                                                   #   (feature 014)
│   ├── BastionWardZone.cs                        # NEW — plain IExposable zone-state class (data-
│   │                                                     #   model.md); no back-reference to its caster
│   │                                                     #   (research.md R4, contracts/ground-fixed-
│   │                                                     #   aoe-zone-contract.md §1)
│   └── MapComponent_BastionWardZoneOverlay.cs    # NEW — FR-005a's ground indicator: per-map,
│                                                        #   auto-discovered, purely visual, reads
│                                                        #   activeZones live every frame (data-model.md)
├── Hediffs/
│   └── HediffComp_BastionWardEffect.cs           # NEW — HediffCompProperties_BastionWardEffect +
│                                                        #   HediffComp_BastionWardEffect (data-model.md):
│                                                        #   two-phase gizmo (research.md R1), tierState,
│                                                        #   cooldownEndTick, EjectMeleeAttackersFrom
│                                                        #   IncomingZone (research.md R5); implements
│                                                        #   IProvidesTattooGizmo + IHandlesOwnMastery
│                                                        #   Progress; does NOT implement IOnIncoming
│                                                        #   DamageTattooEffect (research.md R2)
└── Patches/
    └── Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs   # UPDATED — one additive check after the
                                                                #   existing hediff-comp loop, consulting
                                                                #   GameComponent_BastionWardZones.Get()
                                                                #   .TryAbsorb(...) (research.md R2); no
                                                                #   change to existing behavior for any
                                                                #   other consumer
```

No changes to `Source/Effects/IOnIncomingDamageTattooEffect.cs`, `IProvidesTattooGizmo.cs`,
`IHandlesOwnMasteryProgress.cs`, `TattooTierProgress.cs`, `TattooEffectValues.cs`, `TattooHealingUtility.cs`, any
other tattoo's own `HediffComp`, or `Source/UI/Dialog_ChooseTattoo.cs` (no rarity cap to gate on).

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–015 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) — no new project or scaffold. The new comp lives alongside every other tattoo's
own comp in `Source/Hediffs/`. The new `GameComponent` and its zone-state class live in `Source/Effects/` —
architecturally significant, reusable-shape infrastructure in the same spirit as `GameComponent_PhoenixRegistry`
(feature 014's own precedent for where a new `GameComponent` belongs), even though `BastionWardZone`'s own fields
are Bastion-Ward-specific (contracts/ground-fixed-aoe-zone-contract.md §3). The one existing file touched outside
this feature's own new files, `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs`, gets a single additive block
(research.md R2) — everything else in `Source/Patches/`, `Source/Effects/`, and every other tattoo's own files are
untouched.

## Complexity Tracking

*No entries — Constitution Check reported no violations requiring justification.*
