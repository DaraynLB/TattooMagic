# Implementation Plan: Bloodrune Tattoo Effect (Third Triggered Ability Slice)

**Branch**: `007-bloodrune-tattoo-effect` | **Date**: 2026-08-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-bloodrune-tattoo-effect/spec.md`

## Summary

Wires up Bloodrune's gameplay effect (PRD §6, roster item 3) — the tattoo's `TattooDef`/`HediffDef` scaffolding already
exists (feature 001) but is inert. This feature makes it a real triggered ability: an activatable gizmo that opens a
short self-heal burst on a cooldown, using the exact same "gizmo + cooldown + `TattooTierProgress` + `TattooEffectValues`"
shape already proven twice (Guardian's Call, feature 004; Stormlash, feature 005). At Tier 2, the burst is bigger and/or
longer, and additionally reduces the wearer's pain for the duration of that burst. This is the third triggered tattoo,
built after the pawn-wide tattoo-mastery XP track (feature 006) already existed — its activation is expected to feed
that track automatically with zero new code, since feature 006's hook lives on the shared gizmo-collection point, not
per-tattoo. The one genuinely new piece of infrastructure this feature adds is a way for a tattoo to affect the
wearer's *pain* — RimWorld computes pain directly from each `Hediff`'s own `PainOffset`, not through the `StatDef`/
`StatPart` system the existing `IProvidesTattooStatOffset` mechanism targets, so Bloodrune's own `Hediff` subclass
overrides `PainOffset` and delegates to its comp, rather than reusing that mechanism (research.md R3).

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–006 and this
project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (no new patch — activation is contributed via the existing shared
`Patch_Pawn_GetGizmos_TattooGizmos`/`IProvidesTattooGizmo` reuse point from feature 004, per FR-012), `Krafs.Rimworld.Ref`
1.6.* — same as features 001–006, no new external dependency, no DLC dependency, no Combat Extended-specific stat
interaction expected (healing and pain are not stats CE replaces or remaps, unlike armor/accuracy/movement-speed;
confirmed below in the Constitution Check, not merely assumed)

**Storage**: RimWorld's built-in Scribe save/load system — Bloodrune's own `cooldownEndTick`/`burstEndTick` fields and
its `TattooTierProgress` state persist via `Scribe_Values.Look`, the exact same shape as Guardian's Call's and
Stormlash's own comps

**Testing**: No automated unit-test harness (unchanged from features 001–006 — RimWorld's `Verse`/`RimWorld` game types
aren't practically runnable outside the game process); validated via manual in-game verification in Dev Mode per
Constitution Principle II, using `quickstart.md`. No CE-specific behavior is expected, so no CE-loaded pass is uniquely
required beyond the standing "loads cleanly with CE present" check.

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime)

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features 001–006; no
new project/scaffold needed

**Performance Goals**: Gizmo contribution cost is identical to Guardian's Call's/Stormlash's existing cost profile
(feature 004 R1) — one gizmo collected per `GetGizmos()` call. The periodic heal-tick only runs while a burst window is
open (a few seconds at most per activation, gated the same way Guardian's Call's `CompPostTick` reassert loop already
is), not on every tick for every pawn.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony + CE
(Constitution Principle I) — expected to be trivially satisfied since this feature touches no combat stat, targeting,
or CE-specific code path (confirmed, not assumed, in the Constitution Check below). All tunable numbers (heal
amount/rate, burst duration, cooldown, pain-reduction magnitude, Tier 2 threshold) remain placeholders pending PRD §9's
balance pass, routed through `TattooEffectValues` per FR-010, using deliberately small/fast-to-test values rather than
"plausible-looking" ones (feature 006 research.md R8 precedent).

**Scale/Scope**: One existing inert `HediffDef` (`TattooMagic_Hediff_Bloodrune`) gains a `<comps>` block and a custom
`hediffClass`; one new `HediffComp` (the ability itself — gizmo, cooldown, tier progression, periodic heal); one new
small `Hediff` subclass override (`PainOffset`, delegating to that comp) — no new `HediffDef`, no new Harmony patch, no
changes to Guardian's Call's, Stormlash's, or feature 006's own files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | No — healing and pain are not stats Combat Extended replaces or remaps (unlike armor, accuracy, movement speed, or suppression); this feature touches no CE-specific code path | **N/A** — same non-applicability as Frost Sigil's core slow effect; the standing "loads cleanly with Harmony + CE" check still applies generically and is covered by quickstart.md's final scenario |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines the required in-game Dev Mode scenarios covering the Tier 1 heal, the cooldown gate, Tier 2's upgrade and pain reduction, persistence, the shared-gizmo-point regression check, and the mastery-track pass-through check |
| III. Data-Driven, Tunable Balance | Yes — heal amount/rate, burst duration, cooldown, pain-reduction magnitude, Tier 2 threshold | **PASS** — every value lives as an XML field on `HediffCompProperties_BloodruneEffect` and is read exclusively through `TattooEffectValues` (FR-010), never as an inline magic number; placeholders trace to PRD §9 |
| IV. Two-Path Combat Patching | No — this feature adds no targeting or combat-decision interception of any kind | **N/A** — same non-applicability as features 002/003/005/006, which also didn't touch targeting AI |
| V. Progression Integrity Across Saves | Yes — Bloodrune's own tier/progress counter and cooldown/burst timers | **PASS** — all persist via `CompExposeData()`'s `Scribe_Values.Look` calls; tier-crossing logic reuses `TattooTierProgress.TryRegisterQualifyingEvent` unmodified, which is already idempotent and deterministic (feature 002 precedent) |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` and `contracts/pain-offset-contract.md` confirm the design adds no new
`HediffDef`, no new Harmony patch, and touches no other tattoo's own file. The one new pattern (a `Hediff` subclass
overriding `PainOffset`) is documented as a small, tattoo-owned extension, not a new interface other tattoos must
implement against. Table above still holds; no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/007-bloodrune-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── pain-offset-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── Tattoos/
        └── Bloodrune.xml                       # UPDATED — hediffClass becomes TattooMagic.Hediff_BloodruneEffect;
                                                   #   adds <comps><li Class="TattooMagic.
                                                   #   HediffCompProperties_BloodruneEffect"> with the tunable
                                                   #   heal/duration/cooldown/pain/threshold fields (data-model.md)

Source/
└── Hediffs/
    └── HediffComp_BloodruneEffect.cs           # NEW — gizmo (IProvidesTattooGizmo), cooldown, TattooTierProgress,
                                                   #   periodic heal-tick during the burst window, and the tier/burst-
                                                   #   aware pain-offset value its sibling Hediff class reads
                                                   #   (research.md R1-R3). Also defines Hediff_BloodruneEffect
                                                   #   (the HediffWithComps subclass overriding PainOffset) and
                                                   #   HediffCompProperties_BloodruneEffect in the same file,
                                                   #   matching every prior tattoo effect's one-file-per-tattoo
                                                   #   convention (Props class + effect class together).
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–006 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) rather than introducing a new project, Def type, or interface. Bloodrune's ability
logic lives in `Source/Hediffs/`, next to every other tattoo-effect comp, since it's the exact same
gizmo/cooldown/tier-progression shape those files already establish. No new `HediffDef` is introduced for the pain
mechanism — unlike Frost Sigil's slow effect (which needs a companion hediff because it targets a *different* pawn,
the attacker), Bloodrune's pain reduction applies to its own wearer, so it's implemented as a direct override on
Bloodrune's own `Hediff` subclass rather than a second, temporary hediff.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
