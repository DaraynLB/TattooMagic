# Implementation Plan: Tattoo Ritual Station & Applying a Tattoo

**Branch**: `001-tattoo-ritual-station` | **Date**: 2026-08-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-tattoo-ritual-station/spec.md`

## Summary

Add a dedicated RimWorld workbench (the "Tattoo Ritual Station") plus a
custom job/skill-check pipeline that lets one colonist (the performer) apply
one of the 11 starter tattoos to another colonist (the recipient). Success
scales with the performer's Artistic skill; failure is a mishap that
consumes ingredients without applying anything. Applied tattoos are tracked
per-pawn as Scribe-persisted state that enforces a slot cap read from (but
not itself managed by) the separate slot-progression feature. This feature
does not implement what a tattoo *does* once applied (that's later, per-
tattoo work) — it only gets a tattoo from "selected at the station" to
"permanently applied and occupying a slot."

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target; matches the project's `rimworld-modding` toolchain, not a project choice made here)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (job/UI patches where vanilla hooks are insufficient), `Krafs.Rimworld.Ref` 1.6.* (game API reference assemblies)

**Storage**: RimWorld's built-in Scribe save/load system only — applied-tattoo and slot-capacity state is saved as part of the pawn's Hediff set in the normal colony save file; no external storage

**Testing**: No automated unit-test harness (RimWorld's `Verse`/`RimWorld`-namespace game logic isn't practically runnable outside the game process); validated via manual in-game verification in Dev Mode per Constitution Principle II, using the steps in `quickstart.md`

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime)

**Project Type**: RimWorld mod — single `Source/` C# project + XML Defs, per this repo's `rimworld-modding` scaffold conventions (not a web/mobile/library split)

**Performance Goals**: No special target — this feature's logic runs on job start/tick and Bill-adjacent events, not every render frame; must not introduce per-frame allocations or Update-loop work

**Constraints**: Must load with zero Harmony/console errors with only Harmony present (Combat Extended is irrelevant to this specific feature — no combat stats or targeting are touched); must not require the Ideology DLC; ingredient recipe and skill-check numbers are placeholders pending balance (PRD §9), but the mechanism enforcing them must be real and data-driven (Constitution Principle III)

**Scale/Scope**: Single new `Building` (the station) + one new custom Job/skill-check pipeline + one new per-pawn tracked state (applied tattoos, slot capacity) + Def scaffolding for the 11 starter tattoos' *identity and recipe only* (not their gameplay effects)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | No combat stats or CE-replaced stats are read/written by this feature | **PASS** — no CE branching needed here; revisit if a later feature adds CE-aware ingredients/stats to the ritual itself |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines the required in-game Dev Mode verification steps; feature is not "done" until run |
| III. Data-Driven, Tunable Balance | Yes — ingredient recipe, skill-check curve | **PASS** — recipe and skill-check curve are defined in `data-model.md` as Def-driven/tunable-constant, not inline magic numbers; placeholders are traceable to PRD §9 |
| IV. Two-Path Combat Patching | No targeting/combat-decision logic is touched | **PASS (N/A)** — this feature has no vanilla/CE targeting divergence to patch |
| V. Progression Integrity Across Saves | Yes — applied tattoos and slot capacity must survive save/load | **PASS** — modeled as Hediffs / a Scribe-exposed HediffComp (see `data-model.md`), which RimWorld persists natively; transitions (apply) are one-way and idempotent by construction (a tattoo is either present or not) |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` and `contracts/tattoo-def-contract.md`
introduced a tracker Hediff/HediffComp and a TattooDef/Hediff-pair schema —
neither touches combat stats, CE, or targeting logic, and both persist via
native Scribe mechanisms. Table above still holds; no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-tattoo-ritual-station/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
About/
├── About.xml                       # mod metadata (packageId/author left as
│                                    #   placeholders until the user supplies
│                                    #   a real handle — see research.md)
└── Preview.png

Assemblies/                         # build output only; nothing authored here

Defs/
├── ThingDefs/
│   └── Building_TattooRitualStation.xml   # the workbench ThingDef
├── HediffDefs/
│   ├── TattooTracker.xml           # invisible per-pawn tracker hediff
│   │                                #   (slot capacity + applied tattoos)
│   └── Tattoos/
│       └── <TattooID>.xml          # x11 stub HediffDefs, identity only —
│                                    #   granted on success; no stat effects
│                                    #   yet (later feature fills these in)
├── TattooDefs/
│   └── <TattooID>.xml              # x11 TattooDef: recipe (ingredients,
│                                    #   work, skill curve ref), links to the
│                                    #   matching HediffDef above
└── JobDefs/
    └── TattooRitual.xml            # the custom JobDef performers use

Source/
├── TattooMagic.csproj
├── TattooMagicMod.cs                # entry point ([StaticConstructorOnStartup])
├── Defs/
│   └── TattooDef.cs                 # custom Def subclass (recipe fields)
├── Buildings/
│   └── Building_TattooRitualStation.cs
├── Jobs/
│   ├── JobDriver_TattooRitual.cs    # toil sequence + skill-check resolution
│   └── WorkGiver_TattooRitual.cs
├── Hediffs/
│   └── HediffComp_TattooTracker.cs  # slot capacity + applied-tattoo set,
│                                    #   Scribe-exposed
└── UI/
    └── Dialog_ChooseTattoo.xml/.cs  # tattoo + recipient selection UI
```

**Structure Decision**: Single RimWorld-mod project per the repo's
`rimworld-modding` scaffold (`About/`, `Assemblies/`, `Defs/`, `Source/`) —
this is the project's first feature, so this plan also establishes the
initial scaffold layout rather than fitting into a pre-existing one.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
