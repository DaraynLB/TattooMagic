---

description: "Task list for Berserker's Mark Tattoo Effect (Fifth and Final Triggered Ability Slice)"
---

# Tasks: Berserker's Mark Tattoo Effect (Fifth and Final Triggered Ability Slice)

**Input**: Design documents from `/specs/009-berserkers-mark-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per research.md precedent (features 001–008) and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–008: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

`Source/Effects/IProvidesTattooGizmo.cs`, `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs` (feature 004),
`Source/Effects/IProvidesTattooStatOffset.cs`, `Source/Effects/StatPart_TattooEffectOffset.cs`,
`Source/Effects/TattooTierProgress.cs`, `Source/Effects/TattooEffectValues.cs` (feature 002),
`Source/Effects/IGrantsStatOffsetImmunity.cs` (feature 005), and `Source/Hediffs/HediffComp_TattooTracker.cs`
(feature 006, mastery track) all already exist and need **zero changes** — this feature's entire job re: those
reuse points is to implement `IProvidesTattooGizmo` and `IProvidesTattooStatOffset` on a new, unrelated comp and
let feature 006's gizmo-wrap hook pick it up automatically (research.md R1). Do not edit any of them; a task
below that appears to need something from them is a read-only dependency, not an edit target. Every other
triggered tattoo's own comp file (`HediffComp_GuardiansCallEffect.cs`, `HediffComp_StormlashEffect.cs`,
`HediffComp_BloodruneEffect.cs`, `HediffComp_WraithstepEffect.cs`) likewise needs zero changes.
`Defs/TattooDefs/BerserkersMark.xml` (feature 001, the `TattooMagicDef` itself — ingredients, workAmount) also
needs **zero changes**; only `Defs/HediffDefs/Tattoos/BerserkersMark.xml` (the currently-inert `appliedHediff`)
is edited. `Source/Effects/TattooEffectStatPartInstaller.cs` needs exactly two new lines (Phase 2) and no other
change — its existing registrations (`ComfyTemperatureMin`, `MoveSpeed`, `ShootingAccuracyPawn`,
`CombatExtendedInterop.AimingAccuracy`, `ArmorRating_Sharp`/`Blunt`/`Heat`, `MeleeCooldownFactor`,
`RangedCooldownFactor`) stay untouched.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–008) is a clean baseline before adding Berserker's
Mark's own code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline (features 001–008)
      before adding anything, and confirm `Source/TattooMagic.csproj` needs no new package references (this
      feature adds no new Harmony patch target, no new `HediffDef`, no new interface, and — per research.md
      R2/R3 — no CE interaction; pure edits to one existing inert XML file, one existing installer file, plus
      one new C# file).

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The one piece of shared infrastructure Berserker's Mark's Tier 1 effect needs before it can do
anything at all — registering the two `StatDef`s nothing currently targets onto the existing generic
`StatPart_TattooEffectOffset`. `ArmorRating_Sharp`/`Blunt`/`Heat` are already registered (feature 004);
`IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_TattooGizmos` (feature 004), `IProvidesTattooStatOffset`,
`TattooEffectValues`, `TattooTierProgress` (features 002/003) already exist and need no changes at all.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 In `Source/Effects/TattooEffectStatPartInstaller.cs`, add two `Register(...)` calls for
      `StatDefOf.MeleeDamageFactor` and `StatDefOf.PainShockThreshold` (research.md R2, R3) — no other change to
      this file's existing registrations

**Checkpoint**: Foundation ready — the two new StatDefs accept tattoo stat offsets; no tattoo contributes to
them yet, so nothing is player-observable until US1 lands.

---

## Phase 3: User Story 1 - Berserker's Mark trades defense for melee damage and staying power on demand (Priority: P1) 🎯 MVP

**Goal**: A Berserker's Mark-tattooed pawn has an activatable gizmo that, on use, grants a temporary boost to
melee damage and pain threshold while reducing defense, all for a fixed duration, then goes on cooldown —
reusing feature 004's `IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_TattooGizmos` reuse point and feature 002's
`IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset` reuse point with zero changes to any of those files.

**Independent Test**: Apply Berserker's Mark to a colonist, activate the ability, and observe a measurable
increase to that pawn's melee damage and pain threshold alongside a measurable decrease to defense, all for the
ability's duration, followed by a full reversion to baseline and a cooldown period (`quickstart.md` Scenarios 1,
2).

### Implementation for User Story 1

- [X] T003 [P] [US1] Update `Defs/HediffDefs/Tattoos/BerserkersMark.xml` to attach
      `HediffCompProperties_BerserkersMarkEffect` with placeholder tier1/tier2 numeric fields
      (`meleeDamageOffsetTier1`/`Tier2`, `painThresholdOffsetTier1`/`Tier2`, `defenseOffsetTier1`/`Tier2`,
      `boostDurationTicksTier1`/`Tier2`, `cooldownDurationTicksTier1`/`Tier2`, `tier2Threshold`,
      `abilityIconPath`), per `data-model.md`, each commented as pending the PRD §9 balance pass, with
      `defenseOffsetTier2 < defenseOffsetTier1` to encode FR-007's "smaller Tier 2 penalty" directly in the
      placeholder data; `hediffClass` stays `HediffWithComps` (research.md R4 — no new `Hediff` subclass needed)
- [X] T004 [US1] Implement `HediffCompProperties_BerserkersMarkEffect` and `HediffComp_BerserkersMarkEffect` in
      `Source/Hediffs/HediffComp_BerserkersMarkEffect.cs` — composes a `TattooTierProgress tierState` field
      (exposed via `CompExposeData()` calling `tierState.ExposeData()`, plus `Scribe_Values.Look` for
      `cooldownEndTick`/`boostEndTick`, satisfying FR-010 from the start); implements
      `IProvidesTattooGizmo.GetGizmo()` returning a `Command_Action` whose `action` calls `TryActivate()` and
      whose `Disabled`/`disabledReason` are computed live from `cooldownEndTick`; `TryActivate()` no-ops if
      still on cooldown, otherwise sets `cooldownEndTick`/`boostEndTick` from `TattooEffectValues`-resolved
      Tier 1 values only (tier never leaves 1 in this story since nothing yet calls
      `TryRegisterQualifyingEvent`); implements `IProvidesTattooStatOffset.GetStatOffset(StatDef stat)`
      returning `0f` immediately once `Find.TickManager.TicksGame >= boostEndTick`, else the Tier 1
      `MeleeDamageOffset` for `StatDefOf.MeleeDamageFactor`, the Tier 1 `PainThresholdOffset` for
      `StatDefOf.PainShockThreshold`, the **negated** Tier 1 `DefenseOffset` for `StatDefOf.ArmorRating_Sharp`/
      `Blunt`/`Heat`, and `0f` for any other stat (data-model.md; depends on T002 for the two new StatDefs to
      accept offsets at all, and on T003 for the comp to actually be attached to a pawn at runtime)
- [X] T005 [US1] Manual verification: run `quickstart.md` Scenarios 1 and 2 — confirm the gizmo appears only on
      a tattooed pawn, activation measurably raises `Melee damage factor` and `Pain shock threshold` and lowers
      armor rating (Sharp/Blunt/Heat) for the window's duration (each with a "Tattoo effects" explanation line
      on its Stats-tab tooltip), all three revert to baseline the instant the window ends, re-activation is
      blocked while on cooldown, and an untattooed pawn shows no gizmo and no stat change — zero red Harmony/mod
      errors throughout (depends on T004)

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP. Berserker's Mark
now grants a real Tier 1 risk/reward window with a real cooldown.

---

## Phase 4: User Story 2 - Berserker's Mark grows stronger the more it's used, and the risk eases at Tier 2 (Priority: P2)

**Goal**: The progression counter drives an automatic, in-place Tier 1 → Tier 2 upgrade that grants a bigger
melee-damage boost and a smaller (but still genuine) defense penalty — with no extra ritual/ingredient/slot
cost.

**Independent Test**: Activate a Berserker's Mark-tattooed pawn's ability enough times to reach the threshold,
then confirm the tattoo's effect strengthens in place — the melee-damage boost is bigger, and the defense
reduction is smaller in magnitude than Tier 1's, for an otherwise-identical pawn (`quickstart.md` Scenarios 3,
4).

### Implementation for User Story 2

- [X] T006 [US2] In `HediffComp_BerserkersMarkEffect.TryActivate()`: after resolving the cooldown/boost-end
      ticks, call `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)`
      (FR-004/FR-005); make `TryActivate()` and `GetStatOffset()` read the `...Tier2` value keys
      (`meleeDamageOffsetTier2`, `painThresholdOffsetTier2`, `defenseOffsetTier2`, `boostDurationTicksTier2`,
      `cooldownDurationTicksTier2`) once `tierState.tier == 2` instead of the `...Tier1` keys (data-model.md;
      depends on T004, since it edits `TryActivate()`/`GetStatOffset()` again)
- [X] T007 [US2] Manual verification: run `quickstart.md` Scenario 3 — confirm the progression counter
      increments by exactly one per activation, the tattoo upgrades to Tier 2 in place with no
      ritual/ingredient/slot change once the threshold is reached, Tier 2's `Melee damage factor` increase is
      measurably larger than Tier 1's, and Tier 2's armor-rating decrease is measurably smaller in magnitude
      than Tier 1's while remaining non-zero (FR-007; depends on T006)
- [X] T008 [US2] Manual verification: run `quickstart.md` Scenario 4 (the pain-threshold-reversion edge case) —
      injure a tattooed pawn so their pain sits between their normal and boosted `Pain shock threshold`,
      activate the ability, confirm they stay conscious during the window, then let the window close without
      further healing and confirm they are correctly re-evaluated (and may go down) the instant the
      `PainShockThreshold` offset reverts — this is the intended trade-off, not a bug (Edge Cases; depends on
      T006)

**Checkpoint**: User Stories 1 and 2 both work independently — Berserker's Mark now has its full PRD §6
behavior.

---

## Phase 5: User Story 3 - The triggered-ability reuse point closes out the full roster of five (Priority: P3)

**Goal**: Confirm feature 004's `IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_TattooGizmos` reuse point and
feature 002's `IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset` reuse point both generalize to this
fifth and final triggered tattoo with zero changes to any prior triggered tattoo's own files, and confirm
feature 006's tattoo-mastery gizmo-wrap hook picks up Berserker's Mark's activations with zero
Berserker's-Mark-specific code added to feature 006's own files.

**Independent Test**: Review that `HediffComp_BerserkersMarkEffect` implements `IProvidesTattooGizmo` and
`IProvidesTattooStatOffset` and is picked up by the existing shared mechanisms with zero modifications to any
other triggered tattoo's file, apply Berserker's Mark alongside at least one other triggered tattoo on the same
pawn to confirm every gizmo works independently and that stat contributions stack correctly, and activate
Berserker's Mark to confirm that pawn's tattoo-mastery progress (feature 006) increases by one (`quickstart.md`
Scenarios 5, 6).

### Implementation for User Story 3

- [X] T009 [US3] Review `Source/Hediffs/HediffComp_BerserkersMarkEffect.cs` and
      `Source/Effects/TattooEffectStatPartInstaller.cs` against feature 004's
      `contracts/triggered-tattoo-effect-contract.md` §1-2, feature 002's `contracts/
      passive-tattoo-effect-contract.md` (extended by feature 003's), and feature 006's
      `contracts/mastery-progression-contract.md` §1-2; confirm via `git diff`/inspection that
      `Source/Effects/IProvidesTattooGizmo.cs`, `Source/Effects/IProvidesTattooStatOffset.cs`,
      `Source/Effects/StatPart_TattooEffectOffset.cs`, `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs`,
      every other triggered tattoo's own comp file (`HediffComp_GuardiansCallEffect.cs`,
      `HediffComp_StormlashEffect.cs`, `HediffComp_BloodruneEffect.cs`, `HediffComp_WraithstepEffect.cs`), and
      `Source/Hediffs/HediffComp_TattooTracker.cs` show zero changes from their pre-this-feature state, and that
      `TattooEffectStatPartInstaller.cs`'s only change is the two `Register(...)` lines from T002; fix and
      re-review if anything leaked (depends on T006)
- [X] T010 [US3] Manual verification: run `quickstart.md` Scenario 5 — apply Berserker's Mark alongside at least
      one other triggered tattoo on the same colonist, confirm every gizmo appears and functions independently,
      and (if the other tattoo is Guardian's Call at Tier 2, or a future armor-boosting passive) confirm the
      armor-rating Stats-tab tooltip shows both contributions summed together rather than one overriding the
      other (FR-009; depends on T009)
- [X] T011 [US3] Manual verification: run `quickstart.md` Scenario 6 — activate Berserker's Mark once and
      confirm that pawn's tattoo-mastery progress (feature 006's debug log line or the ritual station dialog)
      increases by exactly one (depends on T010)
- [X] T012 [US3] Record the review outcome (T009) and coexistence/mastery-track confirmation (T010/T011) in this
      file's Notes section (below) (depends on T011)

**Checkpoint**: All three user stories are independently satisfied — Berserker's Mark works end-to-end, and both
reuse points are confirmed to genuinely generalize to the fifth and final triggered tattoo, completing that half
of the PRD §6 starter roster.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full-feature sign-off, including the cross-cutting checks (removal guard, save/reload persistence,
no regression to prior tattoos, CE-agnostic confirmation) that aren't specific to any single user story.

- [X] T013 Manual verification: run `quickstart.md` Scenario 7 (removal blocked except via God Mode or this
      mod's own sanctioned path, reusing feature 003's existing guard with zero Berserker's-Mark-specific
      configuration; tier/counter/window state unchanged across a save/reload) and Scenario 8 (at least one
      other triggered tattoo unaffected; mod loads cleanly with zero red Harmony/mod errors both without and
      with Combat Extended present, with all three effects applying identically in both configurations per
      research.md R2/R3), then re-run all eight `quickstart.md` scenarios back-to-back in one session, confirming
      zero red Harmony/mod errors throughout (depends on T005, T007, T008, T012)
- [X] T014 Confirm `meleeDamageOffsetTier1`/`Tier2`, `painThresholdOffsetTier1`/`Tier2`,
      `defenseOffsetTier1`/`Tier2`, `boostDurationTicksTier1`/`Tier2`, `cooldownDurationTicksTier1`/`Tier2`, and
      `tier2Threshold` are still at their T003 fast-testing defaults (revert if any were temporarily widened
      during manual testing, same convention as feature 007's T015); decide whether any new `Log.Message`
      diagnostics added during testing should stay permanent (matching this project's established preference,
      features 005-008 precedent) or be trimmed if redundant; confirm a final clean `dotnet build` (0 errors, 0
      warnings) (depends on T013)

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete. With this
feature done, all five of the PRD §6 starter roster's triggered tattoos (Guardian's Call, Stormlash, Bloodrune,
Wraithstep, Berserker's Mark) are fully implemented — only the six passive tattoos remain, of which two (Frost
Sigil, Serpent's Eye) already ship.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since neither
  `MeleeDamageFactor` nor `PainShockThreshold` accepts a tattoo offset until T002 registers them.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced (`HediffComp_BerserkersMarkEffect.cs`) — build after US1, not in
    parallel with it.
  - US3 reviews code finished by both US1 and US2 — build after both, since a full review needs the final state
    of the comp (post-Tier-2 logic), not an intermediate one.
- **Polish (Phase 6)**: Depends on US1, US2, and US3 all being complete.

### Within Each User Story

- Foundational: T002 is the only task, single-file, no parallelism to schedule.
- US1: T003 (XML) can run in parallel with nothing else in this phase, but has no dependency on T004 compiling
  — T004 (comp) depends on T002 (Foundational) and, for runtime attachment, T003; T005 (verification) depends
  on T004.
- US2: T006 (tier-aware value resolution + threshold registration) depends on T004 (US1, same file); T007 and
  T008 (verification) both depend on T006.
- US3: Strictly sequential — the review (T009) before the coexistence/mastery check (T010/T011) before recording
  the outcome (T012).

### Parallel Opportunities

- US1: T003 (XML) has no code dependency on T004 (comp) to *write*, though T004 needs T003 to be *exercised* at
  runtime — both may be authored in parallel, matching every prior tattoo's XML+comp pairing.
- No other cross-task parallelism within a single user story phase, since each story's tasks edit the same comp
  file sequentially by design (mirroring features 005/007's US1/US2 shape).

---

## Parallel Example: User Story 1

```bash
# Can be authored together once Foundational (Phase 2) is complete:
Task: "Update Defs/HediffDefs/Tattoos/BerserkersMark.xml with HediffCompProperties_BerserkersMarkEffect"
Task: "Implement HediffCompProperties_BerserkersMarkEffect + HediffComp_BerserkersMarkEffect in Source/Hediffs/HediffComp_BerserkersMarkEffect.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1, 2 in-game.
5. This is a demoable MVP: Berserker's Mark is no longer an inert stub — it grants a real Tier 1 risk/reward
   window (melee damage + pain threshold up, defense down) with a real cooldown.

### Incremental Delivery

1. Setup + Foundational → the two new StatDefs accept tattoo offsets, nothing player-visible yet.
2. Add US1 → validate Scenarios 1, 2 → MVP (Tier 1 Berserker's Mark trades defense for damage/staying power for
   real).
3. Add US2 → validate Scenarios 3, 4 → Tier 2 auto-upgrade, bigger damage boost, smaller defense penalty, and
   the pain-threshold-reversion edge case all work as intended.
4. Add US3 → validate Scenarios 5, 6 + self-audit against every prior triggered tattoo's files → both reuse
   claims (triggered-ability shape, tattoo-mastery pass-through) confirmed on record, completing the PRD §6
   triggered-tattoo roster.
5. Polish → validate the full eight-scenario sign-off pass and confirm no regression to any other triggered
   tattoo.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- US2 edits the same file US1 introduced (`HediffComp_BerserkersMarkEffect.cs`) rather than duplicating it —
  expected and why it's sequenced after US1 rather than run in parallel with it, despite remaining independently
  *testable* per its own Quickstart scenario.
- US3 produces no new runtime code — this is the fifth and final triggered tattoo the PRD §6 roster calls for;
  building a sixth is out of scope by definition. Its "implementation" is a review checkpoint against every
  prior triggered tattoo's files plus a live coexistence/mastery-pass-through check, with the outcome recorded
  here once T012 runs.
- Unlike Bloodrune (feature 007), which needed a new `Hediff.PainOffset` override because "current pain" has no
  backing `StatDef`, every one of this tattoo's three effects — melee damage, pain *threshold*, and defense — is
  an ordinary `StatDef` (research.md R2/R3), so this feature needs no new `Hediff` subclass and no new interface
  at all — only a new comp plus two `TattooEffectStatPartInstaller` registrations, an even thinner build than
  Bloodrune's.

### T012 review outcome (2026-08-15)

- **T009 (reuse-point review)**: Confirmed via `git diff --stat` that this feature's only changes are
  `Defs/HediffDefs/Tattoos/BerserkersMark.xml`, the two `Register(...)` lines in
  `Source/Effects/TattooEffectStatPartInstaller.cs`, and the new `Source/Hediffs/HediffComp_BerserkersMarkEffect.cs`
  — zero changes to `IProvidesTattooGizmo.cs`, `IProvidesTattooStatOffset.cs`, `StatPart_TattooEffectOffset.cs`,
  `Patch_Pawn_GetGizmos_TattooGizmos.cs`, any other triggered tattoo's own comp file, or
  `HediffComp_TattooTracker.cs`.
- **T010 (coexistence, Scenario 5)**: Live-tested alongside Wraithstep on the same colonist (Dalila Donaldson).
  Debug log confirms both tattoos' gizmos activate independently with fully separate tier/progression counters and
  cooldowns (Berserker's Mark `cooldown=60` vs Wraithstep `cooldown=1200` in the testing build) — activating one
  had zero effect on the other's state. Armor-stacking sub-check (FR-009, "if the other tattoo is Guardian's Call
  at Tier 2...") was not applicable for this pairing since Wraithstep doesn't touch armor rating; the underlying
  stacking mechanism was independently confirmed correct via T005's debug-trace check on `GetStatOffset`.
- **T011 (mastery pass-through, Scenario 6)**: Confirmed via debug log — `Donaldson tattoo-mastery progress: N`
  incremented by exactly 1 on every single Berserker's Mark activation throughout testing (progress 5 through 23
  observed), with zero Berserker's-Mark-specific code added to feature 006's own files.
- Both reuse-point claims (triggered-ability shape from feature 004, tattoo-mastery pass-through from feature 006)
  are confirmed to genuinely generalize to this fifth and final triggered tattoo, live in-game — not just by code
  review.
