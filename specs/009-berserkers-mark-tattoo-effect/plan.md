# Implementation Plan: Berserker's Mark Tattoo Effect (Fifth and Final Triggered Ability Slice)

**Branch**: `009-berserkers-mark-tattoo-effect` | **Date**: 2026-08-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/009-berserkers-mark-tattoo-effect/spec.md`

## Summary

Wires up Berserker's Mark's gameplay effect (PRD §6, roster item 8) — the tattoo's `TattooDef`/`HediffDef`
scaffolding already exists (feature 001) but is inert. This is the fifth and, per the PRD §6 starter roster, last
triggered tattoo: an activatable gizmo that opens a temporary window boosting melee damage and pain threshold
while reducing defense, on a cooldown, using the exact same "gizmo + cooldown + `TattooTierProgress` +
`TattooEffectValues`" shape already proven four times (Guardian's Call, Stormlash, Bloodrune, Wraithstep). Unlike
Bloodrune (which needed a new `Hediff.PainOffset` override because "current pain" has no `StatDef`), all three of
this tattoo's effects turn out to be ordinary per-pawn `StatDef`s — `StatDefOf.MeleeDamageFactor`,
`StatDefOf.PainShockThreshold`, and the same `StatDefOf.ArmorRating_Sharp/Blunt/Heat` triplet Guardian's Call's
Tier 2 armor buff already offsets — so this feature needs **zero new infrastructure**: one comp implementing the
already-existing `IProvidesTattooGizmo` and `IProvidesTattooStatOffset` interfaces, gated the same way Guardian's
Call's Tier 2 armor offset is gated (tier + an active window check), plus two new `TattooEffectStatPartInstaller`
registrations for the two stats nothing has registered yet. Research (below) confirms Combat Extended replaces
neither stat, so no CE-specific branching is needed anywhere in this feature — the whole thing is CE-agnostic by
construction, verified against CE's own `Stats_Pawns_Combat.xml`, not merely assumed.

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–008 and
this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (no new patch — activation is contributed via the existing shared
`Patch_Pawn_GetGizmos_TattooGizmos`/`IProvidesTattooGizmo` reuse point from feature 004, per FR-015; the stat
effects route through the existing `StatPart_TattooEffectOffset`/`IProvidesTattooStatOffset` reuse point from
features 002-004, per FR-009), `Krafs.Rimworld.Ref` 1.6.* — same as features 001–008, no new external dependency,
no DLC dependency, no Combat Extended-specific stat interaction needed (research.md R2/R3 confirm CE neither
replaces nor remaps `MeleeDamageFactor`, `PainShockThreshold`, or the vanilla armor-rating stats)

**Storage**: RimWorld's built-in Scribe save/load system — Berserker's Mark's own `cooldownEndTick`/
`boostEndTick` fields and its `TattooTierProgress` state persist via `Scribe_Values.Look`, the exact same shape as
every prior triggered tattoo's own comp

**Testing**: No automated unit-test harness (unchanged from features 001–008); validated via manual in-game
verification in Dev Mode per Constitution Principle II, using `quickstart.md`. No CE-specific behavior is
expected, so no CE-loaded behavioral scenario is uniquely required beyond the standing "loads cleanly with CE
present" check (mirrors Bloodrune's precedent).

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime)

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–008; no new project/scaffold needed

**Performance Goals**: Gizmo contribution cost is identical to every prior triggered tattoo's existing cost
profile (feature 004 R1) — one gizmo collected per `GetGizmos()` call. `GetStatOffset` is a cheap tier/tick
comparison per registered stat per query, the same cost shape Guardian's Call's Tier 2 armor offset and
Stormlash's Tier 2 speed offset already add with no observed issue.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony + CE
(Constitution Principle I) — expected to be trivially satisfied since research confirms this feature touches no
CE-replaced stat (below). All tunable numbers (melee-damage-boost magnitude per tier, pain-threshold-increase
magnitude, defense-reduction magnitude per tier, window duration, cooldown, Tier 2 threshold) remain placeholders
pending PRD §9's balance pass, routed through `TattooEffectValues` per FR-013, using deliberately small/
fast-to-test values rather than "plausible-looking" ones (feature 006 research.md R8 precedent).

**Scale/Scope**: One existing inert `HediffDef` (`TattooMagic_Hediff_BerserkersMark`) gains a `<comps>` block; one
new `HediffComp` (the ability itself — gizmo, cooldown, tier progression, three gated stat offsets); two new
`Register(...)` calls in the existing `TattooEffectStatPartInstaller` (`StatDefOf.MeleeDamageFactor`,
`StatDefOf.PainShockThreshold` — `ArmorRating_Sharp/Blunt/Heat` are already registered by feature 004) — no new
`HediffDef`, no new Harmony patch, no new interface, no changes to any other tattoo's own files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Touches combat stats (melee damage, armor), so the question is live, but research.md R2/R3 confirm CE neither replaces nor remaps `MeleeDamageFactor`, `PainShockThreshold`, or the vanilla `ArmorRating_Sharp/Blunt/Heat` stats (verified against CE's own `Stats_Pawns_Combat.xml`, and against feature 004's own prior finding for armor) | **PASS** — no CE-specific branching is needed anywhere in this feature; the standing "loads cleanly with Harmony + CE" check still applies and is covered by quickstart.md's final scenario |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines the required in-game Dev Mode scenarios covering the Tier 1 window (damage/pain/defense all measurably change together), the cooldown gate, Tier 2's bigger boost and smaller penalty, the pain-shock-on-window-close edge case, persistence, the shared-gizmo/stat-offset reuse-point regression check, and the mastery-track pass-through check |
| III. Data-Driven, Tunable Balance | Yes — melee-damage-boost magnitude (both tiers), pain-threshold-increase magnitude, defense-reduction magnitude (both tiers), window duration, cooldown, Tier 2 threshold | **PASS** — every value lives as an XML field on `HediffCompProperties_BerserkersMarkEffect` and is read exclusively through `TattooEffectValues` (FR-013), never as an inline magic number; placeholders trace to PRD §9 |
| IV. Two-Path Combat Patching | No — this feature adds no targeting or combat-decision interception of any kind | **N/A** — same non-applicability as features 002/003/005/006/007/008, none of which touched targeting AI (only Guardian's Call, feature 004, did) |
| V. Progression Integrity Across Saves | Yes — Berserker's Mark's own tier/progress counter and cooldown/window timers | **PASS** — all persist via `CompExposeData()`'s `Scribe_Values.Look` calls; tier-crossing logic reuses `TattooTierProgress.TryRegisterQualifyingEvent` unmodified, which is already idempotent and deterministic (feature 002 precedent) |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` confirms the design adds no new `HediffDef`, no new Harmony patch, no
new interface, and touches no other tattoo's own file — only two `Register(...)` additions to the shared,
already-generic `TattooEffectStatPartInstaller`. Table above still holds; no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/009-berserkers-mark-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

No `contracts/` directory — this feature introduces no new interface or mechanism. It is a pure consumer of two
already-documented contracts: `IProvidesTattooGizmo`/the cooldown shape from
[`004-guardians-call-tattoo-effect/contracts/triggered-tattoo-effect-contract.md`](../004-guardians-call-tattoo-effect/contracts/triggered-tattoo-effect-contract.md),
and `IProvidesTattooStatOffset`/`TattooEffectValues`/`TattooTierProgress` from
[`002-frost-sigil-tattoo-effect/contracts/passive-tattoo-effect-contract.md`](../002-frost-sigil-tattoo-effect/contracts/passive-tattoo-effect-contract.md)
(extended by feature 003's) — both remain in force unmodified, matching the precedent Stormlash and Bloodrune set
for features that add no new reusable mechanism of their own.

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── Tattoos/
        └── BerserkersMark.xml                  # UPDATED — hediffClass stays HediffWithComps (no new Hediff
                                                    #   subclass needed, unlike Bloodrune); adds <comps><li
                                                    #   Class="TattooMagic.HediffCompProperties_
                                                    #   BerserkersMarkEffect"> with the tunable damage/pain/
                                                    #   defense/duration/cooldown fields (data-model.md)

Source/
├── Effects/
│   └── TattooEffectStatPartInstaller.cs        # UPDATED — two new Register(...) calls:
                                                    #   StatDefOf.MeleeDamageFactor, StatDefOf.PainShockThreshold
                                                    #   (ArmorRating_Sharp/Blunt/Heat already registered by
                                                    #   feature 004)
└── Hediffs/
    └── HediffComp_BerserkersMarkEffect.cs      # NEW — gizmo (IProvidesTattooGizmo), cooldown, TattooTierProgress,
                                                    #   and GetStatOffset (IProvidesTattooStatOffset) covering all
                                                    #   three effects, each gated on tier + an active window check
                                                    #   (research.md R1)
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–008 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) rather than introducing a new project, Def type, or interface. Berserker's
Mark's ability logic lives in `Source/Hediffs/`, next to every other tattoo-effect comp, since it's the same
gizmo/cooldown/tier-progression shape those files already establish, and its stat effects live entirely on that
same comp via `IProvidesTattooStatOffset` rather than a dedicated `Hediff` subclass — unlike Bloodrune, none of
this tattoo's three effects need a mechanism outside the `StatDef` system, so there is nothing for a custom
`Hediff` override to do here.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
