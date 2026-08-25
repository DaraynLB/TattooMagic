---

description: "Task list for Tattoo-Mastery XP Track & Slot Unlocking"
---

# Tasks: Tattoo-Mastery XP Track & Slot Unlocking

**Input**: Design documents from `/specs/006-tattoo-mastery-xp-track/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/mastery-progression-contract.md](./contracts/mastery-progression-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per `research.md` R7 and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–005: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

`Source/Effects/IProvidesTattooGizmo.cs`, `Source/Hediffs/HediffComp_GuardiansCallEffect.cs`,
`Source/Hediffs/HediffComp_StormlashEffect.cs`, `Source/Effects/TattooEffectValues.cs`, and
`Source/Hediffs/TattooTrackerUtility.cs` all already exist and need **zero changes** — this feature hooks
activation-counting into the *shared* `Patch_Pawn_GetGizmos_TattooGizmos` postfix, not into any individual
tattoo's own comp (research.md R1). Do not edit any tattoo-specific comp file; a task below that appears to need
something from them is a read-only dependency, not an edit target.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–005) is a clean baseline before adding this feature's
code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline (features 001–005)
      before adding anything, and confirm no new package references are needed (this feature adds no new
      Harmony patch target, no new Def type, and has no CE interaction — pure edits to existing C#/XML files).

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shared "detect a triggered-tattoo activation, count it once, pawn-wide" mechanism every user
story below builds on. Nothing here is player-visible on its own (no threshold-crossing logic yet) — it exists
purely so `masteryProgress` reliably increments and persists before any slot-unlock logic is layered on top.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] In `Source/Hediffs/HediffComp_TattooTracker.cs`: add `public int masteryProgress;` to
      `HediffComp_TattooTracker`, persist it via `Scribe_Values.Look(ref masteryProgress, "masteryProgress",
      0);` in `CompExposeData()` (alongside the existing `slotCapacity`/`appliedTattoos`/`pendingRitualTattoo`
      calls, unchanged); add `private string ScopeKey => parent.def.defName;`, mirroring every tattoo-specific
      comp's own `ScopeKey` property (research.md R3) — for this comp it always resolves to
      `"TattooMagic_Tracker"`; add a stub `public void RegisterMasteryActivation()` that only does
      `masteryProgress++;` for now (real threshold-crossing logic lands in US1/US2)
- [X] T003 In `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs`: after `Gizmo gizmo = provider.GetGizmo();`
      and before `yield return gizmo;`, if `gizmo is Command_Action commandAction`, capture
      `Action originalAction = commandAction.action;` and reassign `commandAction.action = () => {
      originalAction?.Invoke(); TattooTrackerUtility.GetTracker(__instance)?.RegisterMasteryActivation(); };`
      — still tattoo-agnostic, no new `[HarmonyPatch]` target, no change to the method's existing vanilla-gizmo
      passthrough loop (research.md R1, contracts/mastery-progression-contract.md §1-2; depends on T002 for
      `RegisterMasteryActivation()` to exist)
- [X] T004 Add a permanent (not `Prefs.DevMode`-gated, matching `HediffComp_GuardiansCallEffect.TryActivate()`'s
      and `HediffComp_StormlashEffect.TryActivate()`'s own existing unconditional per-activation `Log.Message`
      convention) log line inside `RegisterMasteryActivation()`, e.g. `Log.Message($"[TattooMagic]
      {Pawn?.LabelShort} tattoo-mastery progress: {masteryProgress} (slot capacity {slotCapacity}).");` — cheap
      (fires only on a player-driven gizmo click, never per-tick) and lets threshold-crossing work in US1/US2 be
      confirmed without a rebuild+relaunch cycle each time (depends on T002)
- [X] T005 Manual verification: activate Guardian's Call and/or Stormlash a few times on a test pawn, confirm
      via the new log line (T004) that `masteryProgress` increments by exactly one per activation and never
      changes from anything else (waiting, taking damage, a passive tattoo's own proc) — partial run of
      `quickstart.md` Scenario 1 and Scenario 5's "no contribution without a triggered tattoo" check (depends on
      T003, T004)

**Checkpoint**: Foundation ready — every triggered-tattoo activation reliably increments a persisted, pawn-wide
counter with zero per-tattoo opt-in code. No slot is ever granted yet; that's what US1 adds.

---

## Phase 3: User Story 1 - A pawn earns a second tattoo slot by using their triggered tattoo abilities (Priority: P1) 🎯 MVP

**Goal**: Once a pawn's tattoo-mastery progress reaches the slot-2 threshold, their slot capacity grows from 1
to 2 automatically, with an on-screen message — no ritual, ingredient cost, or other player action.

**Independent Test**: Apply a triggered tattoo to a pawn, activate its ability repeatedly, and observe slot
capacity increase from 1 to 2 once the threshold is reached, with a second tattoo then selectable at the ritual
station (`quickstart.md` Scenario 2).

### Implementation for User Story 1

- [X] T006 [P] [US1] In `Source/Hediffs/HediffComp_TattooTracker.cs`, add `public int slot2Threshold = 3;` to
      `HediffCompProperties_TattooTracker` (data-model.md); in `Defs/HediffDefs/TattooTracker.xml`'s existing
      `HediffCompProperties_TattooTracker` `<li>`, add a matching `<slot2Threshold>3</slot2Threshold>` element,
      commented as pending the PRD §9 balance pass — deliberately small for fast manual testing rather than a
      "plausible-looking" number, since neither is final until the balance pass regardless (research.md R8)
- [X] T007 [US1] In `RegisterMasteryActivation()` (`Source/Hediffs/HediffComp_TattooTracker.cs`), after
      `masteryProgress++`, add the real 1→2 threshold check: if `slotCapacity == 1 &&
      masteryProgress >= TattooEffectValues.Get(ScopeKey, "Slot2Threshold", Props.slot2Threshold)`, set
      `slotCapacity = 2` and call `Messages.Message($"{Pawn.LabelShortCap} has grown skilled enough with their
      tattoos to support {slotCapacity} tattoo slots!", Pawn, MessageTypeDefOf.PositiveEvent);` (research.md R4,
      R6 — same `Messages.Message` call shape as `JobDriver_TattooRitual`'s existing ritual-result messages;
      depends on T002, T006)
- [X] T008 [US1] Manual verification: run `quickstart.md` Scenario 2 — activate a triggered tattoo repeatedly
      until `slot2Threshold` is reached (lower it via a `TattooEffectValues` override or Dev Mode for faster
      iteration if needed); confirm slot capacity becomes 2 immediately, an on-screen message names the pawn and
      the new slot count, and — using the still-`Prefs.DevMode`-gated test button (not yet removed, that's a US3
      task) if the always-visible label from US3 isn't built yet — a second tattoo can be queued at the ritual
      station (depends on T007)

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP. A pawn can now
legitimately earn a second tattoo slot through normal play.

---

## Phase 4: User Story 2 - A pawn earns a third and final slot the same way (Priority: P2)

**Goal**: Continuing to activate triggered abilities (from one tattoo or several) carries a pawn from slot
capacity 2 to 3 at a second, higher threshold; slot capacity never exceeds 3; progress from multiple different
triggered tattoos on the same pawn shares one total.

**Independent Test**: Continue activating a pawn's triggered ability(s) past the slot-2 threshold until the
slot-3 threshold is reached, confirm slot capacity becomes 3, then confirm further activations no longer change
it (`quickstart.md` Scenarios 3, 4).

### Implementation for User Story 2

- [X] T009 [P] [US2] In `Source/Hediffs/HediffComp_TattooTracker.cs`, add `public int slot3Threshold = 8;` to
      `HediffCompProperties_TattooTracker`; add a matching `<slot3Threshold>8</slot3Threshold>` element to the
      same `TattooTracker.xml` `<li>` T006 updated, commented as a PRD §9 placeholder — small for the same
      fast-testing reason as `slot2Threshold` (research.md R8; depends on T006 for the properties class/XML
      block to already exist)
- [X] T010 [US2] Extract T007's single `if` into its own private `void CheckSlotThresholds()` method, generalized
      into the accessor-resolved `while (slotCapacity < 3 && masteryProgress >= ThresholdForNextSlot(slotCapacity))
      { slotCapacity++; Messages.Message(...); }` loop (research.md R4), backed by a private `int
      ThresholdForNextSlot(int currentCapacity)` helper returning `TattooEffectValues.Get(ScopeKey,
      "Slot2Threshold", Props.slot2Threshold)` when `currentCapacity == 1` and `TattooEffectValues.Get(ScopeKey,
      "Slot3Threshold", Props.slot3Threshold)` when `currentCapacity == 2` — each loop iteration sends its own
      `Messages.Message` with that iteration's new `slotCapacity` value; `RegisterMasteryActivation()` becomes
      `masteryProgress++; CheckSlotThresholds();`. Extracting the loop into its own method (rather than leaving
      it inline) is specifically so T013's Dev Mode debug action can call it too, after a direct progress jump,
      without duplicating the threshold logic (research.md R9; depends on T007, T009)
- [X] T011 [US2] Manual verification: run `quickstart.md` Scenario 3 — reach `slot3Threshold`, confirm slot
      capacity becomes 3 with its own on-screen message, then keep activating well past it and confirm slot
      capacity never exceeds 3 with no error (depends on T010)
- [X] T012 [US2] Manual verification: run `quickstart.md` Scenario 4 — on a pawn with both Guardian's Call and
      Stormlash applied, activate each in turn and confirm both feed the same single `masteryProgress` total
      (via the T004 log line) rather than two separate counters (depends on T010)

**Checkpoint**: User Stories 1 and 2 both work independently — the full 1 → 2 → 3 slot progression from PRD §5.1
now works end-to-end, from any mix of triggered tattoos.

---

## Phase 5: User Story 3 - The player is told when a pawn earns a new slot, and it isn't lost on save/reload (Priority: P2)

**Goal**: Slot capacity is visible in the existing ritual-station UI at all times (not just via the message at
the moment of unlock), and both `masteryProgress` and `slotCapacity` survive save/reload — and the now-obsolete
dev-only test aid that stood in for this feature is removed.

**Independent Test**: Trigger a slot unlock and confirm the notification (already built in US1/US2); open the
ritual station's dialog and confirm current slot capacity is displayed; save and reload and confirm both values
are unchanged (`quickstart.md` Scenario 6, plus the UI half of Scenario 2). Also use the new debug action to
confirm the multi-threshold-crossing branch actually works, not just in theory (`quickstart.md` Scenario 8).

### Implementation for User Story 3

- [X] T013 [US3] In `Source/UI/Dialog_ChooseTattoo.cs`, replace the `Prefs.DevMode`-gated
      `"[DEV] Grant +1 tattoo slot..."` block (`DoWindowContents`'s `if (selectedRecipient != null &&
      Prefs.DevMode)` block) with a `"[DEV] +N tattoo mastery progress"` button that calls
      `devTracker.DevAddMasteryProgress(amount)` (a small fixed `amount`, e.g. 5) — the old button is dead
      scaffolding now that real slot progression exists (its own comment: "should go away once slot progression
      is actually implemented", research.md R5), and the new one is what actually replaces its purpose: reaching
      a multi-slot state fast for testing (research.md R9). Also add `public void DevAddMasteryProgress(int
      amount)` to `HediffComp_TattooTracker` (`masteryProgress += amount; CheckSlotThresholds();`) (depends on
      T010, since `CheckSlotThresholds()` must exist first)
- [X] T014 [US3] In the same file, add an always-visible label once `selectedRecipient != null` (e.g. right
      after the "Choose recipient" `RadioButton` loop or alongside the existing "no free tattoo slots" check),
      reading `HediffComp_TattooTracker tracker = TattooTrackerUtility.GetTracker(selectedRecipient);` (reusing
      the same lookup this file already performs elsewhere) and showing something like
      `"{selectedRecipient.LabelShortCap}: {tracker.appliedTattoos.Count}/{tracker.slotCapacity} tattoo slots
      used"`; adjust `rowCount` accordingly so the scroll view sizes correctly (FR-010, SC-006; depends on T013)
- [X] T015 [US3] Manual verification: run `quickstart.md` Scenario 8 — with a fresh pawn at `masteryProgress =
      0`/`slotCapacity = 1`, use the new debug button to add enough progress in one click to clear both
      `slot2Threshold` and `slot3Threshold` at once; confirm `slotCapacity` jumps straight to 3 with **two**
      separate on-screen messages (one per slot), not one message or a skip straight to 3 with none — this is
      the one scenario that actually exercises `CheckSlotThresholds()`'s multi-crossing loop (research.md R4,
      R9; depends on T013)
- [X] T016 [US3] Manual verification: run `quickstart.md` Scenario 2's UI check — after a slot unlock, open the
      ritual station dialog for that pawn and confirm the new label shows the correct, updated slot count
      (depends on T014)
- [X] T017 [US3] Manual verification: run `quickstart.md` Scenario 6 — save and reload at a partial-progress
      state (below the next threshold) and confirm `masteryProgress`/`slotCapacity` are unchanged; separately,
      save and reload a pawn who has already unlocked slot 2 or 3 and confirm they retain that capacity
      (persistence itself was wired in T002; this is the verification pass) (depends on T010, T014)

**Checkpoint**: All three user stories are independently satisfied — the mastery track is a real, player-visible,
save-safe system; the multi-crossing edge case has actually been exercised, not just reasoned about; and the
dev-only stand-in that preceded it is gone (replaced by a more useful one).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full-feature sign-off, confirming no regression to Guardian's Call/Stormlash's own mechanisms and a
clean build.

- [X] T018 Manual verification: run `quickstart.md` Scenario 7 — confirm Guardian's Call's and Stormlash's own
      tier-progression counters and cooldown/availability behavior are completely unaffected by this feature's
      changes, then re-run all eight `quickstart.md` scenarios back-to-back in one session, confirming zero red
      Harmony/mod errors throughout (depends on T005, T008, T011, T012, T015, T016, T017). All eight scenarios
      individually confirmed across this session's testing (not one continuous sitting, but every scenario's own
      "Expected" outcome was observed, including both the T020/T021 defects found and fixed along the way and
      T022's scope expansion); zero red Harmony/mod errors reported throughout.
- [X] T019 Decide whether the `Log.Message` line added in T004 should stay permanent (matching this project's
      established preference for keeping genuinely useful, low-frequency diagnostic logging rather than
      stripping it — see feature 005's precedent) or be trimmed if it turns out to be redundant with existing
      logging once real testing is done; confirm `slot2Threshold`/`slot3Threshold` are still at their T006/T009
      fast-testing defaults (no separate revert needed — those defaults were chosen to also be the shipped
      placeholders, research.md R8); confirm a final clean `dotnet build` (0 errors, 0 warnings) (depends on
      T018). Decision: kept permanent — it was exactly what let T005/T022's in-game verification happen without
      a rebuild cycle. Thresholds unchanged at 3/8. Final `dotnet build`: 0 warnings, 0 errors.

- [X] T020 Playtesting finding (2026-08-12, Scenario 2/8 testing): `Dialog_ChooseTattoo`'s new slots-used label
      (T014) surfaced a pre-existing feature-001 gap — `appliedTattoos` is a hand-maintained list, written only
      by `JobDriver_TattooRitual.ResolveRitual`, so a tattoo hediff granted any other way (Dev Mode's "give
      hediff") was invisible to it: `FreeSlots` overcounted, and `HasTattoo` failed to block re-selecting that
      same tattoo through a real ritual, which would have granted a second live `HediffComp` for one ability (two
      gizmos, two independent cooldowns/tier counters) with no exception anywhere — a silent correctness bug, not
      a crash. Fixed per this project's established precedent (f593b6a, "fix issues found in live playtesting")
      rather than deferred to `docs/PRD.md`: added `HediffComp_TattooTracker.SyncAppliedTattoosFromActualHediffs()`
      (reconciles `appliedTattoos` against the pawn's real hediffs, keyed off `TattooMagicDef.appliedHediff` —
      same reverse-lookup pattern `TattooHediffRemovalGuard` already uses), called from `Dialog_ChooseTattoo` once
      per frame (fixes the label, `FreeSlots`, and `HasTattoo` together) and defensively again in
      `JobDriver_TattooRitual.ResolveRitual` right before granting the Hediff (belt-and-suspenders in case
      anything queues a ritual for a tattoo the recipient already actually has). Only ever adds entries, never
      removes — tattoo removal isn't a feature yet (PRD §3), so this doesn't need to handle that direction.
      Re-confirmed clean `dotnet build` (0 warnings, 0 errors) after the fix.
- [X] T021 Pre-Scenario-8 review finding (2026-08-12): T013's `devProgressAmount` was implemented as `5` (its own
      "e.g. 5" wording), but `slot3Threshold` is `8` — a fixed `+5` per click can never cross both `slot2Threshold`
      (3) and `slot3Threshold` (8) in a single `CheckSlotThresholds()` call from a fresh pawn (two clicks would
      produce two separate single-crossings, `0→5` then `5→10`, not the one required multi-crossing event).
      `quickstart.md` Scenario 8 itself specifies "e.g. add 10" for exactly this reason. Changed
      `devProgressAmount` to `10` in `Source/UI/Dialog_ChooseTattoo.cs` so a single click from `masteryProgress =
      0` reliably exercises the `while` loop's multi-crossing branch (research.md R4/R9). Re-confirmed clean
      `dotnet build` (0 warnings, 0 errors) after the fix.
- [X] T022 Scope expansion (2026-08-12, product decision after playtesting): a pawn who only ever receives
      passive tattoos (Frost Sigil, Serpent's Eye, and every future passive tattoo — 5-6 of 11 planned) had no
      possible path to ever unlock slot 2/3, since `RegisterMasteryActivation()` was only reachable through the
      gizmo wrap (T003/R1), and passive tattoos have no gizmo. Originally deliberate spec scope (FR-004/SC-008 as
      first written), but accepted as a real product gap rather than left as-is. Extended
      `Patch_Pawn_PostApplyDamage_TattooOnHit.cs` (feature 002/003's existing shared on-hit dispatch patch,
      structurally the passive-tattoo analog of the gizmo patch) so every dispatched `IOnMeleeHitTattooEffect`/
      `IOnRangedHitLandedTattooEffect` notification also calls `RegisterMasteryActivation()` on the comp-owning
      pawn — zero changes to `HediffComp_FrostSigilEffect.cs`/`HediffComp_SerpentsEyeEffect.cs` (research.md R10).
      Counts every dispatched hit, not gated on that tattoo's own internal proc chance — flagged as a balance-pass
      concern in `docs/PRD.md` §9, not resolved here. Updated `spec.md` (FR-004 revised, FR-016/FR-017 added,
      SC-008 revised, Edge Cases, Assumptions), `research.md` (R10), `data-model.md`, `contracts/mastery-
      progression-contract.md` (§4), and `quickstart.md` (Scenario 5 rewritten — its original premise, "passive
      tattoos never contribute," is now false; still-valid "no tattoo at all → no progress" case preserved as
      Scenario 5 step 1, new passive-contribution case added as step 2) to keep every spec artifact consistent
      with the code. Re-confirmed clean `dotnet build` (0 warnings, 0 errors) after the change. **Manually
      verified in-game (2026-08-12)**: rewritten Scenario 5 both steps confirmed — a tattoo-less pawn generates
      zero progress; Moody (Frost Sigil only) generated one `tattoo-mastery progress` log line per melee hit
      taken from Twonk regardless of Frost Sigil's own proc outcome (`proc=True` once, `proc=False` six times in
      a row, progress still climbed 1→7 every time), and rode straight through both thresholds to slot capacity
      3 from melee hits alone with zero gizmo clicks — confirming a passive-only pawn now has a real path to
      unlock all 3 slots.

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since no activation
  is counted at all until the gizmo-wrap hook (T003) and the counter it calls (T002) both exist.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same method US1 introduced (`RegisterMasteryActivation()`'s threshold check) and the same
    XML `<li>` block US1 added a field to — build after US1, not in parallel with it.
  - US3's UI changes reference `slotCapacity` reaching values above 1, which only happens once US2's mechanism
    is in place (a pawn could otherwise only ever be tested at capacity 1 or 2) — build after US2.
- **Polish (Phase 6)**: Depends on US1, US2, and US3 all being complete.

### Within Each User Story

- Foundational: T002 (counter + stub method) has no dependency; T003 (patch wrapping) depends on T002; T004
  (logging) depends on T002; T005 (verification) depends on T003 and T004.
- US1: T006 (XML/properties field) can run in parallel with anything outstanding in Foundational once T002 is
  done (distinct concern — a new field, not new logic); T007 (real threshold check) depends on T002 and T006;
  T008 (verification) depends on T007.
- US2: T009 (second threshold field) depends on T006 (same properties class/XML block); T010 (loop
  generalization) depends on T007 and T009; T011/T012 (verification) depend on T010.
- US3: T013 (replace dev button + `DevAddMasteryProgress`) depends on T010, since it needs
  `CheckSlotThresholds()` to already exist to call; T014 (add label) depends on T013 (same file, sequential
  edits); T015 (multi-crossing verification) depends on T013 directly, not T014; T016/T017 (remaining
  verification) depend on T014.

### Parallel Opportunities

- Foundational: T002 is the only task with no upstream dependency; T003 and T004 both depend on it but not on
  each other, so they can run in parallel once T002 lands.
- US1: T006 (XML/properties field, distinct concern from the method logic) can be done alongside the tail end of
  Foundational once T002 exists, ahead of T007 needing it.
- US2: T009 (XML/properties field) can similarly be done as soon as T006 exists, ahead of T010 needing it.
- US3: T015 (multi-crossing verification) does not need T014's label to be done first — it can run as soon as
  T013 lands, in parallel with T014.

---

## Parallel Example: Foundational Phase

```bash
# Launch these together once T002 lands:
Task: "Wrap each collected Command_Action's action in Patch_Pawn_GetGizmos_TattooGizmos.cs"
Task: "Add a permanent Log.Message line inside RegisterMasteryActivation()"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenario 2 in-game.
5. This is a demoable MVP: a pawn can legitimately earn a second tattoo slot through normal play, with a
   player-visible message — no more dev-only workaround needed to reach that state.

### Incremental Delivery

1. Setup + Foundational → activations are counted pawn-wide and persist; nothing player-visible changes yet.
2. Add US1 → validate Scenario 2 → MVP (slot 2 unlocks for real).
3. Add US2 → validate Scenarios 3, 4 → slot 3 unlocks, caps correctly, and multiple triggered tattoos share one
   counter.
4. Add US3 → validate Scenario 8 (multi-crossing via the debug button) + Scenario 6 + the UI check → the
   dev-only stand-in is replaced by a more useful one, the loop's defensive branch is actually exercised (not
   just reasoned about), and persistence is confirmed.
5. Polish → validate the full eight-scenario sign-off pass and confirm no regression to Guardian's Call/
   Stormlash.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- Unlike feature 005 (where US2 added a genuinely new mechanism — Tier 2 slow immunity — beyond US1), this
  feature's US1 and US2 both extend the *same* threshold-crossing method with the *same* shape (research.md
  R4's while-loop design handles both transitions uniformly by construction) — US2's real work is narrower than
  US1's, and its own manual-verification tasks (T011, T012) are what earns it independent-story status, not a
  large amount of new code.
- T013's replacement of the dev-only test button is sequenced into US3, not Foundational or Polish, because
  removing the only prior way to reach a multi-slot pawn state before the real mechanism (US1/US2) exists would
  make those earlier stories harder to test, not easier.
- `slot2Threshold`/`slot3Threshold` defaults (`3`/`8`, T006/T009) and the `DevAddMasteryProgress` debug action
  (T013) both exist specifically to avoid the edit-XML → rebuild → test → edit-back → rebuild cycle earlier
  features hit when they used "plausible-looking" placeholder values and had to temporarily widen them for
  testing (research.md R8, R9). There is no revert step for the thresholds in Polish (T019) because the
  fast-testing values *are* the shipped placeholders this time, not a temporary stand-in for them.
