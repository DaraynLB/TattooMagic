# Implementation Plan: Tattoo-Mastery XP Track & Slot Unlocking

**Branch**: `006-tattoo-mastery-xp-track` | **Date**: 2026-08-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-tattoo-mastery-xp-track/spec.md`

## Summary

Turns PRD §5.1's "pawns unlock 2 additional slots via a tattoo-mastery XP track" from an explicitly-deferred gap
(`HediffComp_TattooTracker.slotCapacity` hardcoded to 1, per its own doc-comment) into a working pawn-wide
progression system: every triggered-tattoo ability activation (Guardian's Call, Stormlash today; any future
triggered tattoo automatically) increments a hidden per-pawn counter, and two ascending thresholds grow that
pawn's `slotCapacity` from 1 → 2 → 3 in place, with a player-facing message and visibility in the existing
ritual-station UI. This is a **systems** slice, not a new tattoo — no new `TattooMagicDef`, no new `Command_
Action`, no combat-stat or targeting change. Its only non-trivial design decision (research.md R1) is *where* to
detect "an activation happened" without requiring every triggered tattoo's own file to opt in: rather than
adding a call inside each tattoo's `TryActivate()`, this feature extends the existing shared `Patch_Pawn_
GetGizmos_TattooGizmos` postfix (feature 004) to wrap the `Command_Action` it already collects from any
`IProvidesTattooGizmo` comp — since only triggered tattoos ever implement that interface, this single, small,
still-tattoo-agnostic extension point structurally satisfies both "passive tattoos never contribute" (FR-004)
and "future triggered tattoos need zero opt-in code" (FR-015) with no change to Guardian's Call's or Stormlash's
own files. The rest of the feature reuses existing infrastructure end to end: `HediffComp_TattooTracker` (not a
new comp) gains the counter and threshold logic, `TattooEffectValues` (feature 002) supplies the two tunable
threshold numbers under the tracker's own already-unique `HediffDef` scope key, and `Dialog_ChooseTattoo`
(feature 001) gains an always-visible slot-capacity label in place of its now-obsolete `Prefs.DevMode`-only test
button (research.md R5).

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–005 and
this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (no new patch — this feature edits the body of the existing
`Patch_Pawn_GetGizmos_TattooGizmos` postfix in place, adding no new `[HarmonyPatch]` attribute or target
method), `Krafs.Rimworld.Ref` 1.6.* (game API reference assemblies) — same as features 001–005, no new external
dependency, no DLC dependency, no Combat Extended interaction at all (no combat stat or targeting logic is
touched by this feature)

**Storage**: RimWorld's built-in Scribe save/load system — the new `masteryProgress` counter persists as a plain
`Scribe_Values.Look`-backed field on the existing `HediffComp_TattooTracker`, alongside its already-persisted
`slotCapacity`, exactly like every other counter shipped so far (features 002–005's `TattooTierProgress`)

**Testing**: No automated unit-test harness (unchanged from features 001–005 — RimWorld's `Verse`/`RimWorld`
game types aren't practically runnable outside the game process); validated via manual in-game verification in
Dev Mode per Constitution Principle II, using `quickstart.md`. This feature has no CE-specific behavior, so no
CE-loaded pass is uniquely required by this feature (only the standing "loads cleanly with CE present" check)

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime)

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–005; no new project/scaffold needed

**Performance Goals**: The new wrap-the-action step in `Patch_Pawn_GetGizmos_TattooGizmos` runs only on
gizmo-row refresh (selection/UI), once per `IProvidesTattooGizmo` comp per call — identical cost profile to the
patch's existing behavior (feature 004 R1); it allocates one small closure per collected gizmo per refresh,
which is the same order of allocation the patch already performs by constructing a new `Command_Action` each
call. `RegisterMasteryActivation()` itself only runs when a gizmo action is actually invoked (a player click),
not per-tick.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony +
CE (Constitution Principle I) — trivially satisfied since this feature touches no combat stat, targeting, or
CE-specific code path at all; both threshold values remain placeholders pending PRD §9's balance pass, routed
through `TattooEffectValues` per FR-012.

**Scale/Scope**: One new counter + threshold-crossing method on an existing comp (`HediffComp_TattooTracker`),
one small in-place extension to one existing shared patch (`Patch_Pawn_GetGizmos_TattooGizmos`), two new XML
fields on one existing Def (`TattooTracker.xml`), and one existing-UI update (`Dialog_ChooseTattoo` gains a
label, loses its now-obsolete dev-only test button) — no new Def types, no new interfaces beyond documenting the
existing gizmo-collection extension in `contracts/mastery-progression-contract.md`, no changes to any specific
tattoo's own file (Guardian's Call, Stormlash, Frost Sigil, Serpent's Eye all untouched).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | No — this feature touches no combat stat, armor, accuracy, speed, suppression, or ammo/loadout logic, and no CE-specific code path | **N/A** — same non-applicability as any feature with no combat-stat surface; the standing "loads cleanly with Harmony + CE" check still applies generically and is covered by quickstart.md Scenario 7 |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines seven required in-game Dev Mode scenarios covering activation counting, both threshold crossings, the cross-tattoo shared-counter behavior, passive-tattoo non-contribution, save/reload persistence, and no-regression on Guardian's Call/Stormlash's own mechanisms |
| III. Data-Driven, Tunable Balance | Yes — the two slot-unlock thresholds | **PASS** — both live as XML fields on `HediffCompProperties_TattooTracker` (data-model.md) and are read exclusively through `TattooEffectValues` (FR-012), never as inline magic numbers; placeholders trace to PRD §9 |
| IV. Two-Path Combat Patching | No — this feature adds no targeting or combat-decision interception of any kind | **N/A** — same non-applicability as features 002/003/005, which also didn't touch targeting AI |
| V. Progression Integrity Across Saves | Yes — pawn-wide `masteryProgress` and `slotCapacity` | **PASS** — both persist via `HediffComp_TattooTracker.CompExposeData()`'s existing `Scribe_Values.Look` mechanism (one new call added, one already existing); threshold-crossing logic (research.md R4) mirrors `TattooTierProgress`'s already-proven idempotent, deterministic shape — re-evaluating past a threshold never double-grants a slot |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` and `contracts/mastery-progression-contract.md` confirm the design adds
no new Def type, no new interface (the gizmo-wrapping step is documented as an extension of the existing
`Patch_Pawn_GetGizmos_TattooGizmos` behavior, not a new contract surface tattoos implement against), and touches
no tattoo-specific file. Table above still holds; no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/006-tattoo-mastery-xp-track/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── mastery-progression-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── TattooTracker.xml                      # UPDATED — add slot2Threshold/slot3Threshold fields to
                                                 #   the existing HediffCompProperties_TattooTracker entry
                                                 #   (data-model.md)

Source/
├── Hediffs/
│   └── HediffComp_TattooTracker.cs            # UPDATED — new masteryProgress field (Scribe-backed),
                                                 #   new RegisterMasteryActivation() method, new ScopeKey
                                                 #   (research.md R2/R3/R4)
├── Patches/
│   └── Patch_Pawn_GetGizmos_TattooGizmos.cs   # UPDATED — wraps each collected Command_Action's action
                                                 #   delegate to also call RegisterMasteryActivation()
                                                 #   (research.md R1); still tattoo-agnostic, no new
                                                 #   [HarmonyPatch] target
└── UI/
    └── Dialog_ChooseTattoo.cs                 # UPDATED — replaces the now-obsolete Prefs.DevMode "[DEV]
                                                 #   Grant +1 tattoo slot" block with a "[DEV] +N tattoo
                                                 #   mastery progress" debug button; adds an always-visible
                                                 #   slot-capacity label (research.md R5, R9)
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–005 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) rather than introducing a new project, Def type, or interface. The counter
and threshold-crossing logic lives on `HediffComp_TattooTracker` in `Source/Hediffs/`, next to every other
tattoo-related comp, since it's per-pawn bookkeeping state exactly like that comp's existing `slotCapacity`/
`appliedTattoos` fields. The activation-detection hook is an in-place edit to the existing `Patch_Pawn_
GetGizmos_TattooGizmos` in `Source/Patches/` rather than a new patch file, since it's extending that patch's
existing "collect a gizmo from each `IProvidesTattooGizmo` comp" loop with one additional generic step, not
introducing a new Harmony patch target. `Dialog_ChooseTattoo` in `Source/UI/` is updated in place since it
already owns the tattoo-selection UI this feature needs to surface slot capacity in.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
