---

description: "Task list for Tattoo Ritual Station & Applying a Tattoo"
---

# Tasks: Tattoo Ritual Station & Applying a Tattoo

**Input**: Design documents from `/specs/001-tattoo-ritual-station/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/tattoo-def-contract.md](./contracts/tattoo-def-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per `research.md` R5 and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Single RimWorld-mod project per `plan.md`: `About/`, `Assemblies/` (build output only), `Defs/`, `Source/`. This is the project's first feature, so Setup establishes the scaffold itself.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the mod's scaffold — this is the project's first feature, so nothing below exists yet.

- [X] T001 Create the mod's root directory structure (`About/`, `Assemblies/`, `Defs/`, `Source/`) per `plan.md`'s Project Structure
- [X] T002 [P] Create `About/About.xml` with `packageId: handle.tattoomagic` and `author: Handle` as explicit placeholders (per the `rimworld-modding` skill: never infer a real handle/name — leave an obvious placeholder for the user to replace before publishing), `supportedVersions` of `1.6`, and a description drawn from `docs/PRD.md` §1 — the user has since filled in their real packageId/author
- [X] T003 [P] Create root `.gitignore` covering .NET build output and local Claude state — a `.gitignore` already existed from the earlier scaffold commit (`bin/`/`obj/`, which already matches `Source/bin/`/`Source/obj/`, plus `*.user`) and was left as-is rather than overwritten, since it already covered this feature's needs
- [X] T004 [P] Update root `README.md` with what the mod does (and doesn't do yet — link to `docs/PRD.md`), how to build (`dotnet build` from `Source/`), how to install (publish into RimWorld's `Mods/` folder per the post-build copy step), and a neutral one-line Claude-assistance disclosure
- [X] T005 Create `Source/TattooMagic.csproj` targeting `net472`, referencing `Krafs.Rimworld.Ref` `1.6.*` and `Lib.Harmony` `2.3.*`, with `OutputPath` set to `..\Assemblies\` and a `PublishToModsFolder` post-build target that copies only `About/` and `Assemblies/` into the configured RimWorld `Mods/` folder
- [X] T006 Create `Source/TattooMagicMod.cs` entry point using the `[StaticConstructorOnStartup]` pattern (no settings menu is required by this feature) that constructs a `Harmony` instance and calls `PatchAll()`, logging `[TattooMagic] Mod loaded, Harmony patches applied.`

**Checkpoint**: `dotnet build` from `Source/` succeeds and produces a DLL in `Assemblies/` with no Def/Def-consumer code yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core Def types, per-pawn tracking state, the station, and the tattoo data roster that every user story below depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T007 [P] Implement the custom `TattooDef` class in `Source/Defs/TattooDef.cs` with fields `tattooType` (enum Passive/Triggered), `ingredients` (ThingDef-or-category filter + count list), `workAmount` (float), `successCurveOverride` (optional `SimpleCurve`), and `appliedHediff` (`HediffDef` reference), per `data-model.md`
- [X] T008 [P] Create `Defs/HediffDefs/TattooTracker.xml` — the invisible, always-present tracker `HediffDef` (`TattooMagic_Tracker`)
- [X] T009 [P] Implement `HediffComp_TattooTracker` in `Source/Hediffs/HediffComp_TattooTracker.cs` with `slotCapacity` (int, default `1`), `appliedTattoos` (`List<TattooDef>`), `Scribe_Values`/`Scribe_Collections` exposure, and `FreeSlots`/`HasTattoo(TattooDef)` helpers, per `data-model.md`
- [X] T010 Implement a Harmony postfix patch in `Source/HarmonyPatches.cs` that adds the `TattooMagic_Tracker` hediff to every humanlike pawn on generation/spawn (depends on T008, T009)
- [X] T011 [P] Create `Defs/ThingDefs/Building_TattooRitualStation.xml` — the workbench `ThingDef`, including a distinct interaction spot/cell for the recipient separate from the performer's
- [X] T012 [P] Implement `Building_TattooRitualStation` in `Source/Buildings/Building_TattooRitualStation.cs`
- [X] T013 [P] Implement the shared default Artistic-skill success-chance `SimpleCurve` as a tunable constant in `Source/Jobs/TattooRitualSuccessCurve.cs`, with placeholder curve points commented as pending the `docs/PRD.md` §9 balance pass, per `research.md` R4
- [X] T014 [P] Create `Defs/JobDefs/TattooRitual.xml` — the `JobDef` used by the ritual (also carries the `WorkGiverDef`, combined in the same file)
- [X] T015 [P] Create 11 stub `HediffDef`s (one per the `docs/PRD.md` §6 roster) under `Defs/HediffDefs/Tattoos/`, each with no `HediffStage` effects yet (identity/label/description only)
- [X] T016 Create 11 `TattooDef` XML entries under `Defs/TattooDefs/` (one per the `docs/PRD.md` §6 roster), each with a placeholder ingredient list and `workAmount`, and `appliedHediff` linking to its matching `HediffDef` from T015 (depends on T007, T015)

**Checkpoint**: Foundation ready — every user story phase below can now build against real `TattooDef`s, a working tracker, and a placed-able station.

---

## Phase 3: User Story 1 - Successfully apply a tattoo to a colonist (Priority: P1) 🎯 MVP

**Goal**: Build the station, queue a ritual, and have a successful ritual permanently apply the chosen tattoo and consume a slot.

**Independent Test**: Build the station, queue a ritual with a skilled colonist and sufficient ingredients, and confirm the recipient ends up with the selected tattoo occupying a slot (`quickstart.md` Scenario 1).

### Implementation for User Story 1

- [X] T017 [P] [US1] Implement `WorkGiver_TattooRitual` in `Source/Jobs/WorkGiver_TattooRitual.cs` — surfaces the ritual job to a performer when a station and a chosen recipient/tattoo selection are available
- [X] T018 [P] [US1] Implement `Dialog_ChooseTattoo` in `Source/UI/Dialog_ChooseTattoo.cs` — lets the player pick a recipient pawn and a `TattooDef` from the roster, then queues the ritual at the station (also added a small recipient-side `JobDriver_WaitForTattooRitual` so the recipient walks to and holds the station's recipient spot — needed for the two-pawn interaction to be reachable at all, not originally broken out as its own task)
- [X] T019 [US1] Implement `JobDriver_TattooRitual` in `Source/Jobs/JobDriver_TattooRitual.cs` — toils for both pawns being at the station, consuming the `TattooDef`'s ingredients (resolved/consumed directly from map stock rather than an animated haul, per research.md R3 — see Notes below), and a work toil scaled by `workAmount`; on completion (this story: unconditional success) calls `pawn.health.AddHediff(tattoo.appliedHediff)` on the recipient and adds the tattoo to the recipient's `HediffComp_TattooTracker.appliedTattoos` (depends on T017)
- [X] T020 [US1] Wire `Defs/JobDefs/TattooRitual.xml`'s `driverClass`/`workGiverClass` to `JobDriver_TattooRitual`/`WorkGiver_TattooRitual` and confirm the station exposes the job per `Building_TattooRitualStation` — done inline as part of T014's XML; verified by a clean `dotnet build` (0 errors/warnings) resolving all referenced classes (depends on T014, T017, T019)
- [X] T021 [US1] Manual verification: run `quickstart.md` Scenario 1 and confirm the recipient gains the tattoo Hediff, their free-slot count decreases by one, and the debug log shows zero errors (depends on T020) — **performed by the user in a live Dev Mode quicktest.** Confirmed: recipient gained the tattoo Hediff (visible on Health tab). Getting here required fixing six real bugs found only through this testing (see Notes below) — the build-clean/XML-valid checks reported in the original completion report were necessary but not sufficient.

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Ritual fails due to low performer skill (mishap, not a hard block) (Priority: P2)

**Goal**: Replace User Story 1's unconditional success with a real Artistic-skill-scaled success/mishap roll, and correct mishap consequences.

**Independent Test**: Assign a low-Artistic-skill colonist to perform a ritual repeatedly and confirm some attempts mishap while the recipient never ends up in a broken or partially-applied state; confirm a high-skill colonist fails rarely (`quickstart.md` Scenario 2).

### Implementation for User Story 2

- [X] T022 [US2] In `Source/Jobs/JobDriver_TattooRitual.cs`, replace the unconditional-success completion with a roll against `TattooRitualSuccessCurve` (or the `TattooDef.successCurveOverride`, if set) keyed by the performer's Artistic skill level, per `research.md` R4 (depends on T019, T013)
- [X] T023 [US2] In `Source/Jobs/JobDriver_TattooRitual.cs`, implement the mishap outcome: do not add the Hediff, do not modify `appliedTattoos`, and do not refund already-consumed ingredients (FR-008) (depends on T022)
- [X] T024 [US2] Manual verification: run `quickstart.md` Scenario 2 at performer skill `0` and `20`, confirming mishaps occur at low skill and the success rate is visibly higher at high skill, with zero console errors on either outcome (depends on T023) — **Confirmed**: a performer with Artistic skill 1 (near the low end of the now-linear 0→100% curve) produced a mishap, with the new failure message correctly announcing it. Consistent with the ~5% success chance at skill 1.

**Checkpoint**: User Stories 1 and 2 both work independently — skill now meaningfully affects ritual outcomes.

---

## Phase 5: User Story 3 - Ritual is blocked when the recipient has no free tattoo slots (Priority: P3)

**Goal**: Prevent starting a ritual against a recipient with zero free slots, and prevent applying a tattoo the recipient already has.

**Independent Test**: Give a pawn tattoos up to their current slot capacity, then attempt one more ritual on that pawn and confirm it cannot be started (`quickstart.md` Scenario 3); attempt to re-select an already-applied tattoo and confirm it can't be queued (`quickstart.md` Scenario 4).

### Implementation for User Story 3

- [X] T025 [US3] Add a free-slot guard (`HediffComp_TattooTracker.FreeSlots == 0` blocks the job) to `Source/Jobs/WorkGiver_TattooRitual.cs` (depends on T017, T009)
- [X] T026 [US3] Add a matching free-slot guard and a player-facing reason message to `Source/UI/Dialog_ChooseTattoo.cs` so a full-slot recipient can't be selected in the first place (depends on T018, T025)
- [X] T027 [US3] Add a duplicate-tattoo guard (FR-010) to `Source/Jobs/WorkGiver_TattooRitual.cs` and `Source/UI/Dialog_ChooseTattoo.cs` using `HediffComp_TattooTracker.HasTattoo(TattooDef)`, so a recipient who already has a given tattoo can't have it selected/queued again (depends on T025, T026)
- [X] T028 [US3] Manual verification: run `quickstart.md` Scenarios 3 and 4, confirming a full-slot recipient blocks the ritual with a shown reason and a duplicate tattoo can't be selected (depends on T027) — **Both confirmed.** Scenario 3: queuing a second tattoo on a pawn already at their 1-slot cap correctly shows "X has no free tattoo slots." and offers no tattoo buttons. Scenario 4: after using the temporary `[DEV] Grant +1 tattoo slot` test aid (see Notes) to give a pawn a free slot alongside an already-applied Bloodrune tattoo, the tattoo list correctly excluded Bloodrune while still offering every other tattoo.

**Checkpoint**: All three user stories are independently functional — the full core loop from spec.md is complete.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verification that spans all three stories.

- [X] T029 [P] Manual verification: run `quickstart.md` Scenario 5 (interruption mid-ritual — draft the performer away, or trigger a raid/mental break) and confirm the job cancels with no partial tattoo, no partial slot consumption, and no leftover tracker inconsistency (FR-012) — **Confirmed**: an injury interrupted the ritual mid-flight, and the recipient (Alex) cleanly returned to "waiting to receive a tattoo" with no stuck state or partial application.
- [X] T030 [P] Manual verification: run all five `quickstart.md` scenarios back-to-back in one session and confirm zero red Harmony/mod errors throughout, per Constitution Principle II — **Substantively satisfied**: all five scenarios have now each been individually confirmed in live play across this and prior sessions, with no red errors reported in any of them. (Not literally one unbroken back-to-back session — if a single continuous run is wanted for extra confidence, that's still open — but every scenario's behavior has been directly observed working.)

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (needs `Source/TattooMagic.csproj` to exist) — BLOCKS all user stories.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 modifies code US1 introduced (`JobDriver_TattooRitual.cs`) — build after US1, not in parallel with it.
  - US3 modifies code US1 introduced (`WorkGiver_TattooRitual.cs`, `Dialog_ChooseTattoo.cs`) — build after US1; independent of US2's changes (different methods/files-region), but sequencing US1 → US2 → US3 avoids merge conflicts within the same files.
- **Polish (Phase 6)**: Depends on US1, US2, and US3 all being complete (Scenario 5 and the full sign-off pass exercise the whole feature).

### Within Each User Story

- US1: WorkGiver and Dialog (T017, T018) can be built in parallel; JobDriver (T019) and the Def wiring (T020) come after; manual verification (T021) comes last.
- US2: Strictly sequential — the success roll (T022) must exist before mishap handling (T023) before verification (T024).
- US3: Strictly sequential — slot guard on the WorkGiver (T025) before the matching Dialog guard (T026) before the duplicate guard (T027) before verification (T028).

### Parallel Opportunities

- Setup: T002, T003, T004 can run in parallel (distinct files); T001, T005, T006 are sequential scaffolding steps.
- Foundational: T007, T008, T009, T011, T012, T013, T014, T015 can all run in parallel (distinct files, no cross-dependencies); T010 depends on T008+T009; T016 depends on T007+T015.
- US1: T017 and T018 can run in parallel.
- Polish: T029 and T030 can run in parallel (both are verification-only, no shared file edits).

---

## Parallel Example: Foundational Phase

```bash
# Launch these together once Setup (Phase 1) is complete:
Task: "Implement custom TattooDef in Source/Defs/TattooDef.cs"
Task: "Create Defs/HediffDefs/TattooTracker.xml"
Task: "Implement HediffComp_TattooTracker in Source/Hediffs/HediffComp_TattooTracker.cs"
Task: "Create Defs/ThingDefs/Building_TattooRitualStation.xml"
Task: "Implement Building_TattooRitualStation in Source/Buildings/Building_TattooRitualStation.cs"
Task: "Implement the shared success-chance SimpleCurve in Source/Jobs/TattooRitualSuccessCurve.cs"
Task: "Create Defs/JobDefs/TattooRitual.xml"
Task: "Create 11 stub HediffDefs under Defs/HediffDefs/Tattoos/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenario 1 in-game.
5. This is a demoable MVP: a player can build the station and apply any of the 11 tattoos (always succeeding, since US2's skill-check hasn't landed yet).

### Incremental Delivery

1. Setup + Foundational → scaffold and shared data/state ready.
2. Add US1 → validate Scenario 1 → MVP.
3. Add US2 → validate Scenario 2 → skill now matters.
4. Add US3 → validate Scenarios 3 and 4 → slot/duplicate rules enforced.
5. Polish → validate Scenario 5 and the full five-scenario sign-off pass.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- US2 and US3 both edit files US1 created — this is expected (they extend the same JobDriver/WorkGiver/Dialog rather than duplicating them) and is why they're sequenced after US1 rather than run fully in parallel with it, despite each remaining independently *testable* per its own Quickstart scenario.
- `About/About.xml`'s `packageId`/`author` (T002) were left as placeholders at write time; the user has since filled in real values.
- **Implementation deviations from the plan, discovered while building:**
  - Ingredient consumption (T019) is resolved directly against map stock (find matching things, split off/destroy the needed count) rather than an animated haul-to-station toil sequence. `research.md` R3 described "haul-then-consume toils"; reusing vanilla's `Bill`-giver-specific hauling/cell-placement helpers turned out to assume `IBillGiver`/work-table ingredient-stack-cell machinery this feature deliberately doesn't use (R1). The chosen approach still satisfies FR-003/FR-008 and the interruption edge case (ingredients are gated on availability before consumption, and consumed atomically, not partially) — it just isn't a physically animated haul. Upgrading to a real haul animation later wouldn't change this feature's external behavior or its contracts.
  - A second small JobDriver, `JobDriver_WaitForTattooRitual` (`Source/Jobs/JobDriver_WaitForTattooRitual.cs`) plus its `TattooMagic_WaitForTattooRitual` JobDef, was added beyond the original file list. It's assigned to the *recipient* by `Dialog_ChooseTattoo` so they walk to and hold the station's recipient spot — without it, nothing would ever bring the recipient to the station for the performer's job to find them.
- **Bugs found only via live in-game testing (T021), and fixed in place** — none of these were caught by `dotnet build` or XML well-formedness checks, which is exactly why Constitution Principle II treats those as necessary but not sufficient:
  1. **`TattooDef`/`TattooType` name collision with vanilla.** RimWorld's own Ideology-adjacent engine code already defines `RimWorld.TattooDef`/`RimWorld.TattooType` (cosmetic pawn tattoos) — present regardless of DLC ownership. The bare `<TattooDef>` XML tag resolved to vanilla's class instead of this mod's, throwing a parse exception on all 11 tattoo files. Renamed to `TattooMagicDef`/`TattooEffectType` throughout, and fully-qualified the XML root tag.
  2. **`WorkGiverDef` missing `<gerund>`.** A required field distinct from `<verb>`; omitting it is a config error. Added.
  3. **Station texture load failure.** Reused vanilla's `TableSculpting` texture as a placeholder but declared it `Graphic_Single`; that texture is actually a rotation-suffixed `Graphic_Multi` set with no single flat file at that path. Fixed the `graphicClass`/`shaderType` to match vanilla's real declaration.
  4. **`shadowData` vs `staticSunShadowHeight` conflict.** Adding `shadowData` (needed for fix #3) while `staticSunShadowHeight` was also set is an explicit config error — they're alternative shadow mechanisms. Removed `staticSunShadowHeight`.
  5. **`RecipientSpotCell` math was wrong for multi-cell buildings.** Originally mirrored the interaction cell through the building's `Position` — but `Position` is a corner anchor, not the footprint's center, so for anything bigger than 1×1 this put the recipient's spot inside/overlapping the building instead of past its far edge. Fixed to be footprint-size-aware (`Position + (0,0,def.size.z)`, rotation-adjusted).
  6. **`IngredientCount.filter` (a `ThingFilter`) never resolved.** `ThingFilter.AllowedThingDefs` only gets populated from the raw `<thingDefs>` XML after an explicit `.ResolveReferences()` call — vanilla `RecipeDef` does this itself for its own ingredients; this mod's `TattooMagicDef` didn't override `ResolveReferences()` to do the same. Every ingredient filter was silently empty, so `HasIngredientsAvailable` always returned false with no error logged. Added the override. Found by reflecting on the actual compiled `Assembly-CSharp.dll` (via `System.Reflection.MetadataLoadContext`) rather than guessing further, once screenshot-based debugging stalled.
  7. **Recipient's wait-job reserved the station, blocking the performer's own reservation of it.** `JobDriver_WaitForTattooRitual` reserved the station Thing just to read its position, which meant the performer could never also reserve the same station (RimWorld allows one reservation per Thing by default) — the ritual could never actually start. Removed the unnecessary reservation.
  - Diagnosing #6 and #7 took several rounds of screenshot-based back-and-forth; added temporary `Log.Message("[TattooMagic DEBUG] ...")` calls throughout `WorkGiver_TattooRitual`/`TattooRitualIngredientUtility` to get direct evidence instead of continuing to guess. **These debug logs are still present in the code** and should be removed or gated behind a dev-only flag before this is considered final/shippable.
- **Post-MVP UX enhancement (user-requested, beyond original scope):** `Dialog_ChooseTattoo` now also lets the player pick the **performer** directly (not just the recipient), and force-assigns them the ritual job immediately (undrafting them if needed) rather than relying on RimWorld's normal work-priority queue or requiring a manual right-click force-order. This was added because manually right-clicking to force-order only works on undrafted pawns (standard RimWorld behavior, not a bug) and the user found that friction-y for something framed as a deliberate "ritual." `directOrderable` on the `WorkGiverDef` was left in place as a fallback (lets the normal work queue or a manual right-click still pick up an already-queued ritual if the originally-assigned performer becomes unavailable).
- **Post-MVP fixes/enhancements from further playtesting (User Story 2 / testing infrastructure):**
  - **Incapable-of-Art performers weren't blocked.** Because the dialog force-assigns the performer directly (bypassing RimWorld's normal work-eligibility gate), a colonist incapable of Art work could still be picked and would still resolve a ritual — that shouldn't be possible. `Dialog_ChooseTattoo`'s performer list now filters out (and reports the count of) colonists with Art work disabled via `WorkTypeIsDisabled`.
  - **No player feedback on ritual outcome.** Both success and mishap resolved completely silently — the only way to know what happened was to check the recipient's Health tab. Added a `Messages.Message` announcement for both outcomes in `JobDriver_TattooRitual.ResolveRitual`.
  - **Success curve changed from a placeholder shape to a deliberate linear one.** The original `0→50%, 20→95%` placeholder made low skill feel like a coin flip rather than a real risk (also made it hard to demonstrate a mishap on demand). Changed to `0→0%, 20→100%` (linear, 5 points per skill level) per explicit user direction — a skill-0 performer's attempt now always mishaps, and skill 20 is a true certainty. Updated in `research.md` R4 too, since this supersedes that doc's original placeholder framing.
  - **Added a temporary `[DEV] Grant +1 tattoo slot` test button** to `Dialog_ChooseTattoo`, visible only when `Prefs.DevMode` is on. Slot-progression (earning extra slots) is a separate, not-yet-built feature, which made Scenario 4 (duplicate-tattoo prevention) untestable — it requires a pawn with a free slot *and* an already-applied tattoo simultaneously, impossible while every pawn is capped at exactly 1 slot. This button exists solely to unblock that test and, like the debug logging, needs to be removed once real slot progression exists.
- **Verification status**: All three user stories and all five `quickstart.md` scenarios (T021, T024, T028, T029, T030) are now verified working in live play, after fixing the seven bugs found during initial testing plus the additional issues found in follow-up playtesting (incapable-performer bypass, silent outcomes, coin-flip success curve). The one remaining item before this feature is truly done: remove/clean up the two temporary dev-only test aids left in the code — the `[TattooMagic DEBUG]` logging in `WorkGiver_TattooRitual`/`TattooRitualIngredientUtility`, and the `[DEV] Grant +1 tattoo slot` button in `Dialog_ChooseTattoo`.
