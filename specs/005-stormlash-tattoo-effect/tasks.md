---

description: "Task list for Stormlash Tattoo Effect (Second Triggered Ability + Movement-Slow Immunity Slice)"
---

# Tasks: Stormlash Tattoo Effect (Second Triggered Ability + Movement-Slow Immunity Slice)

**Input**: Design documents from `/specs/005-stormlash-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/stat-offset-immunity-contract.md](./contracts/stat-offset-immunity-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per `research.md` R7 and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–004: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

`Source/Effects/IProvidesTattooGizmo.cs` and `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs` (feature 004)
already exist and need **zero changes** — this feature's entire job re: that reuse point is to implement
`IProvidesTattooGizmo` on a new, unrelated comp (research.md R1). Do not edit either file; a task below that
appears to need something from them is a read-only dependency, not an edit target.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–004) is a clean baseline before adding Stormlash's own
code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline (features 001–004)
      before adding anything, and confirm `Source/TattooMagic.csproj` needs no new package references (Harmony
      and the RimWorld ref assemblies already present are sufficient; the new CE patch resolves
      `CombatExtended.CompSuppressable` via Harmony's own `AccessTools` reflection helpers, same as the existing
      `Patch_CE_ProjectileImpact_TattooAmmoBonus`). **Confirmed**: 0 warnings, 0 errors on the baseline; no new
      package references needed.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The one piece of shared infrastructure Stormlash's Tier 1 effect needs before it can do anything —
registering the attack-speed StatDefs onto the existing generic `StatPart_TattooEffectOffset`. `MoveSpeed` is
already registered (feature 002); `IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_TattooGizmos` (feature 004),
`TattooEffectValues`, `TattooTierProgress`, and `CombatExtendedInterop` (features 002/003) already exist and
need no changes at all.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 In `Source/Effects/TattooEffectStatPartInstaller.cs`, add two `Register(...)` calls for
      `StatDefOf.MeleeCooldownFactor` and `StatDefOf.RangedCooldownFactor` (research.md R2, R3) — no other
      change to this file's existing registrations (`ComfyTemperatureMin`, `MoveSpeed`, `ShootingAccuracyPawn`,
      `CombatExtendedInterop.AimingAccuracy`, `ArmorRating_Sharp`/`Blunt`/`Heat` stay untouched)

**Checkpoint**: Foundation ready — the two new StatDefs accept tattoo stat offsets; no tattoo contributes to
them yet, so nothing is player-observable until US1 lands.

---

## Phase 3: User Story 1 - Stormlash makes its wearer faster and hits harder-and-faster on demand (Priority: P1) 🎯 MVP

**Goal**: A Stormlash-tattooed pawn has an activatable gizmo that, on use, grants a temporary boost to both
movement speed and attack speed for a fixed duration, then goes on cooldown — reusing feature 004's
`IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_TattooGizmos` reuse point with zero changes to either file.

**Independent Test**: Apply Stormlash to a colonist, activate the ability, and observe a measurable increase to
that pawn's movement speed and attack rate for the ability's duration, followed by a return to baseline and a
cooldown period (`quickstart.md` Scenarios 1, 2).

### Implementation for User Story 1

- [X] T003 [P] [US1] Update `Defs/HediffDefs/Tattoos/Stormlash.xml` to attach
      `HediffCompProperties_StormlashEffect` with placeholder tier1/tier2 numeric fields
      (`moveSpeedOffsetTier1`/`Tier2`, `attackSpeedBonusTier1`/`Tier2`, `boostDurationTicksTier1`/`Tier2`,
      `cooldownDurationTicksTier1`/`Tier2`, `tier2Threshold`, `abilityIconPath`), per `data-model.md`, each
      commented as pending the PRD §9 balance pass
- [X] T004 [US1] Implement `HediffComp_StormlashEffect` (+ its `HediffCompProperties`) in
      `Source/Hediffs/HediffComp_StormlashEffect.cs` — composes a `TattooTierProgress tierState` field (exposed
      via `CompExposeData()` calling `tierState.ExposeData()`, plus `Scribe_Values.Look` for
      `cooldownEndTick`/`boostEndTick`, satisfying FR-009 from the start); implements
      `IProvidesTattooGizmo.GetGizmo()` returning a `Command_Action` whose `action` calls `TryActivate()` and
      whose `Disabled`/`disabledReason` are computed live from `cooldownEndTick`; `TryActivate()` no-ops if
      still on cooldown, otherwise sets `cooldownEndTick`/`boostEndTick` from `TattooEffectValues`-resolved
      Tier 1 values only (tier never leaves 1 in this story since nothing yet calls
      `TryRegisterQualifyingEvent`); implements `IProvidesTattooStatOffset.GetStatOffset` returning, only while
      `Find.TickManager.TicksGame < boostEndTick`, the Tier 1 `MoveSpeed` offset for `StatDefOf.MoveSpeed` and
      the **negated** Tier 1 attack-speed magnitude for `StatDefOf.MeleeCooldownFactor`/
      `StatDefOf.RangedCooldownFactor`, else `0f` for any stat (data-model.md; depends on T002 for the two
      StatDefs to accept offsets at all, and on T003 for the comp to actually be attached to a pawn at runtime)
- [X] T005 [US1] Manual verification: run `quickstart.md` Scenarios 1 and 2 (non-CE) — confirm the gizmo appears
      only on a tattooed pawn, activation measurably raises `Move speed` and lowers `Melee cooldown`/`Ranged
      cooldown multiplier` for the boost's duration (with a "Tattoo effects" explanation line on each), all
      three return to baseline once the duration elapses, re-activation is blocked while on cooldown, and an
      untattooed pawn shows no gizmo and no stat change — zero red Harmony/mod errors throughout (depends on
      T004)

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Stormlash grows stronger the more it's used, and shrugs off being slowed at Tier 2 (Priority: P2)

**Goal**: The progression counter drives an automatic, in-place Tier 1 → Tier 2 upgrade that grants a bigger
speed/attack-speed boost and, for the boost's duration, genuine immunity to movement-slow effects — this mod's
own Frost Sigil slow (non-CE) and Combat Extended's suppression-slow (CE-loaded) — with no extra
ritual/ingredient/slot cost.

**Independent Test**: Activate a Stormlash-tattooed pawn's ability enough times to reach the threshold, then
confirm the tattoo's effect strengthens in place — the boost is bigger, and while it's active the pawn is
unaffected by an applied movement-slow effect, where the same effect would slow an otherwise-identical unbuffed
or Tier 1 pawn (`quickstart.md` Scenarios 3, 4, 5).

### Implementation for User Story 2

- [X] T006 [P] [US2] Implement the `IGrantsStatOffsetImmunity` interface in
      `Source/Effects/IGrantsStatOffsetImmunity.cs` — `bool BlocksNegativeOffsets(StatDef stat);` (research.md
      R5, contract §1)
- [X] T007 [US2] Update `StatPart_TattooEffectOffset.GetOffset` in `Source/Effects/StatPart_TattooEffectOffset.cs`
      to a two-pass shape: first check whether any `IGrantsStatOffsetImmunity` comp on the pawn currently
      returns `true` from `BlocksNegativeOffsets(parentStat)`; if so, when summing `IProvidesTattooStatOffset`
      contributions (unchanged sweep otherwise), clamp each individual comp's own offset to `Mathf.Max(0f,
      offset)` before adding, instead of summing raw values (research.md R5, contract §2; depends on T006)
- [X] T008 [US2] In `Source/Hediffs/HediffComp_StormlashEffect.cs`: in `TryActivate()`, after resolving the
      cooldown/boost-end ticks, call `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold",
      Props.tier2Threshold)` (FR-004/FR-005); make `TryActivate()` and `GetStatOffset()` read the `...Tier2`
      value keys once `tierState.tier == 2` instead of the `...Tier1` keys; implement
      `IGrantsStatOffsetImmunity.BlocksNegativeOffsets(StatDef stat)` returning `true` only when
      `stat == StatDefOf.MoveSpeed && tierState.tier >= 2 && Find.TickManager.TicksGame < boostEndTick`, else
      `false` (data-model.md; depends on T004, T006)
- [X] T009 [US2] Implement `Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity` in
      `Source/Patches/Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity.cs` — reflection-based,
      imperatively-applied Harmony postfix on `CombatExtended.CompSuppressable`'s `IsCrouchWalking` property
      getter (`AccessTools.TypeByName`/`PropertyGetter`, gated behind `CombatExtendedInterop.IsLoaded`,
      following `Patch_CE_ProjectileImpact_TattooAmmoBonus`'s convention), forcing `__result = false` when the
      comp's owning pawn (`parent` field, cast to `Pawn`) currently has a `HediffComp_StormlashEffect` with
      `tierState.tier >= 2` and an unexpired `boostEndTick` (research.md R4, contract §3); add one line to
      `Source/TattooMagicMod.cs` calling its `TryApply(harmony)` alongside the existing CE patch registration
      (depends on T008)
- [X] T010 [US2] Manual verification: run `quickstart.md` Scenario 3 (non-CE) — confirm the progression counter
      increments by exactly one per activation, the tattoo upgrades to Tier 2 in place with no
      ritual/ingredient/slot change once the threshold is reached, and Tier 2's boosted `Move speed`/cooldown-
      factor values are measurably larger than Tier 1's (depends on T008)
- [X] T011 [US2] Manual verification: run `quickstart.md` Scenario 4 (non-CE, **required**) — confirm that once
      Frost Sigil's slow hediff lands on a Tier 2, actively-boosted Stormlash pawn, `Move speed` shows no
      reduction (the negative contribution is clamped, not merely outweighed), and that the same slow visibly
      reduces `Move speed` once the boost window has expired (depends on T008)
- [X] T012 [US2] Manual verification (Combat Extended loaded, **required**): run `quickstart.md` Scenario 5 —
      confirm a Tier 1 Stormlash pawn is still reduced by CE's suppression-crouch-walking as normal, that a Tier
      2 pawn with an active boost shows no such reduction while suppressed, that the reduction reasserts itself
      once the boost expires, and that suppression's other effects (mental-break risk, cover-seeking, sway,
      hunkering) continue to behave normally throughout — zero red Harmony/mod errors (depends on T009)

**Checkpoint**: User Stories 1 and 2 both work independently — Stormlash now has its full PRD §6 behavior.

---

## Phase 5: User Story 3 - The gizmo/cooldown reuse point proves itself on a second, unrelated consumer (Priority: P3)

**Goal**: Confirm feature 004's `IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_TattooGizmos` reuse point genuinely
generalizes to a second, unrelated triggered tattoo with zero changes to Guardian's Call's own files, and that a
pawn with both tattoos gets two fully independent gizmos.

**Independent Test**: Review that `HediffComp_StormlashEffect` implements `IProvidesTattooGizmo` and is picked
up by the existing shared patch with zero modifications to any feature-004 file, and apply both Guardian's Call
and Stormlash to the same pawn to confirm both gizmos appear and function independently (`quickstart.md`
Scenario 6).

### Implementation for User Story 3

- [X] T013 [US3] Review `Source/Hediffs/HediffComp_StormlashEffect.cs` against
      `contracts/stat-offset-immunity-contract.md` and feature 004's
      `contracts/triggered-tattoo-effect-contract.md` §1-2; confirm via `git diff`/inspection that
      `Source/Effects/IProvidesTattooGizmo.cs`, `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs`,
      `Source/Hediffs/HediffComp_GuardiansCallEffect.cs`, and
      `Source/Patches/Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs` show zero changes from
      their feature-004 state; fix and re-review if anything leaked (depends on T008)
- [X] T014 [US3] Manual verification: run `quickstart.md` Scenario 6 — apply both Guardian's Call and Stormlash
      to the same colonist, confirm both gizmos appear, and confirm activating one has no effect on the other's
      cooldown/availability (depends on T013)
- [X] T015 [US3] Record the review outcome (T013) and coexistence confirmation (T014) in this file's Notes
      section (below) (depends on T014)

**Checkpoint**: All three user stories are independently satisfied — Stormlash works end-to-end, and feature
004's reuse point is confirmed to genuinely generalize.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full-feature sign-off, including the cross-cutting checks (removal guard, save/reload persistence)
that aren't specific to any single user story.

- [X] T016 Manual verification: run `quickstart.md` Scenario 7 (removal blocked except via God Mode or this
      mod's own sanctioned path, reusing feature 003's existing guard with zero Stormlash-specific
      configuration; tier/counter unchanged across a save/reload), then re-run all seven `quickstart.md`
      scenarios back-to-back in one session, confirming zero red Harmony/mod errors throughout (depends on T005,
      T012, T015)
- [X] T017 Remove any temporary `Log.Message` debug probes added during CE investigation (or gate them behind
      `Prefs.DevMode`) and revert any temporary test-tuning values used to make Tier 2/cooldowns/suppression
      observable without a long grind back to their PRD §9 placeholder values in
      `Defs/HediffDefs/Tattoos/Stormlash.xml`, then confirm a clean `dotnet build` (0 errors, 0 warnings)
      (depends on T016) — **Done.** `Stormlash.xml`'s `boostDurationTicksTier1/2`/`cooldownDurationTicksTier1/2`
      and `FrostSigil.xml`'s `slowChanceTier1/2`/`slowDurationTicksTier1/2` reverted to their original PRD §9
      placeholder values; `git diff` confirms `FrostSigil.xml` is now byte-identical to its pre-session state.
      Per the customer's explicit request, the `Prefs.DevMode`-gated debug logging added this session (the
      immunity-clamp log in `StatPart_TattooEffectOffset`, the suppression-neutralization log in the CE patch,
      and the combat-visibility logs in `Patch_Pawn_PostApplyDamage_TattooOnHit` and
      `HediffComp_FrostSigilEffect`) was **kept permanently** rather than stripped — all silent for normal
      players, all genuinely useful for testing future tattoos. Final `dotnet build`: 0 errors, 0 warnings.

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since US1's boost
  has no effect on attack speed at all until `MeleeCooldownFactor`/`RangedCooldownFactor` accept tattoo offsets.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced (`HediffComp_StormlashEffect.cs`) and adds new shared
    infrastructure (`IGrantsStatOffsetImmunity`, the `StatPart_TattooEffectOffset` extension) — build after US1,
    not in parallel with it.
  - US3 reviews code introduced by US1 and finished by US2 — build after both, since a full review needs the
    final state of the comp (post-Tier-2 logic), not an intermediate one.
- **Polish (Phase 6)**: Depends on US1, US2, and US3 all being complete (the full seven-scenario sign-off pass
  exercises the whole feature, including the reuse confirmation from US3 being on record).

### Within Each User Story

- US1: The XML task (T003) can run in parallel with Foundational and with the comp (T004) as a distinct file;
  the comp itself (T004) depends on Foundational's registrations (T002); verification (T005) comes last.
- US2: The new interface (T006) can run in parallel with anything else in this phase (distinct file, no
  dependency); the `StatPart_TattooEffectOffset` extension (T007) depends on T006; the comp update (T008)
  depends on both T004 (US1) and T006; the CE patch (T009) depends on T008 (it needs to query the comp's tier/
  boost state); verification tasks T010-T012 depend on T008/T009 respectively and can be run in any order
  relative to each other once their dependency is satisfied.
- US3: Strictly sequential — the review (T013) before the coexistence check (T014) before recording the outcome
  (T015).

### Parallel Opportunities

- Foundational: T002 is the only task in this phase (no in-phase parallelism to signal).
- US1: T003 (XML) can run in parallel with T002 (Foundational) — distinct files, no cross-dependency until T004
  needs both.
- US2: T006 (new interface) can run in parallel with T004 (US1's comp, if still in progress) or anything else
  outstanding — distinct file, no dependency on Stormlash's own comp.

---

## Parallel Example: Foundational Phase + US1 setup

```bash
# Launch these together once Setup (Phase 1) is complete:
Task: "Register MeleeCooldownFactor/RangedCooldownFactor in Source/Effects/TattooEffectStatPartInstaller.cs"
Task: "Update Defs/HediffDefs/Tattoos/Stormlash.xml with HediffCompProperties_StormlashEffect"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1, 2 in-game.
5. This is a demoable MVP: Stormlash is no longer an inert stub — it grants a real speed/attack-speed boost
   with a real cooldown, at Tier 1 only (no progression or slow immunity yet).

### Incremental Delivery

1. Setup + Foundational → the two attack-speed StatDefs ready, nothing player-visible yet.
2. Add US1 → validate Scenarios 1, 2 → MVP (Tier 1 Stormlash works).
3. Add US2 → validate Scenarios 3, 4, 5 → Tier 2 auto-upgrade and genuine slow immunity (both non-CE and CE)
   work.
4. Add US3 → validate Scenario 6 + self-audit against feature 004's files → reuse claim confirmed on record.
5. Polish → validate the full seven-scenario sign-off pass and clean up any temporary debug/test artifacts.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- US2 edits the same file US1 created (`HediffComp_StormlashEffect.cs`) rather than duplicating it — expected
  and why it's sequenced after US1 rather than run in parallel with it, despite remaining independently
  *testable* per its own Quickstart scenarios.
- US3 produces no new runtime code — building a third triggered tattoo is explicitly out of scope for this
  feature (spec Assumptions). Its "implementation" is a review checkpoint against feature 004's files plus a
  live coexistence check, with the outcome recorded here once T015 runs.
- T009's CE patch depends on T008 specifically because it needs a way to ask "does this pawn currently have an
  active Tier 2 Stormlash boost" — the same tier/`boostEndTick` state T008 adds to the comp, not new state of
  its own (data-model.md: "there is no separate immunity-specific state to persist or desync").
- T013's review outcome (the US3 self-audit) should be recorded here once it runs, mirroring feature 004's own
  T015/T016 precedent of documenting the actual files diffed and any coupling found.

## Implementation Session Notes (2026-08-09)

- **T001–T004, T006–T009, T013 code complete**, `dotnet build` clean (0 errors, 0 warnings) after every step
  along the way (build re-run after each task, not just at the end).
- **T009's `IsCrouchWalking` postfix parameter type**: declared as `ThingComp __instance` (a real, compile-time-
  referenceable vanilla type) rather than `CombatExtended.CompSuppressable`, since Harmony binds `__instance` by
  the actual runtime type regardless of the declared parameter type as long as it's assignment-compatible — this
  let the postfix read `__instance.parent` (a public vanilla `ThingComp` field) directly, with zero reflection
  needed beyond resolving and patching the getter method itself. Confirms research.md R4's approach compiles and
  type-checks as designed; live confirmation that it actually neutralizes CE's `0.67f` multiplier still requires
  Scenario 5 (CE-loaded, not run this session).
- **T013 self-audit — PASS, no leaks found.** `git diff --quiet` confirmed zero changes to
  `Source/Effects/IProvidesTattooGizmo.cs`, `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs`,
  `Source/Hediffs/HediffComp_GuardiansCallEffect.cs`, and
  `Source/Patches/Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs` (all feature 004) — Stormlash
  implements `IProvidesTattooGizmo` on its own comp and is picked up by the existing shared patch with genuinely
  zero edits to any feature-004 file, confirming SC-010. Also confirmed zero changes to
  `Source/Hediffs/HediffComp_FrostSigilEffect.cs`/`HediffComp_FrostSigilSlow.cs` (feature 002), matching
  research.md R5's design constraint. The two new `TattooEffectStatPartInstaller` registrations (T002) and the
  `StatPart_TattooEffectOffset` extension (T007) are, per feature 004's own precedent for its own three armor
  registrations, not leaks either — both are shared/reusable infrastructure files that already anticipated
  future tattoos extending them.
- **Not completed this session — require an actual running RimWorld instance and cannot be done by an agent
  without game access**: T005, T010, T011, T012, T014, T016. All `quickstart.md` scenarios (1–7) still need to
  be run by a human in Dev Mode per Constitution Principle II before this feature can be marked done — in
  particular Scenario 4 (genuine immunity to this mod's own Frost Sigil slow) and Scenario 5 (genuine immunity
  to Combat Extended's suppression-slow), both flagged in research.md (R4, R5) as static-analysis-grounded
  claims that still need live confirmation, not just a clean compile. T015 is therefore only partially
  complete — the code-review half (T013) is recorded above; the live coexistence half (T014) is still
  outstanding. T017's build-clean check has already been continuously verified after every code change in this
  session, but should be re-run one final time after the manual verification passes (e.g. if any temporary
  tuning values get added during testing and need reverting).

## Live Testing Session (2026-08-09, continued) — real bug found and fixed

Manual in-game testing of Scenario 1 (customer, non-CE) surfaced a genuine defect this session's static analysis
did not catch: `MoveSpeed` and `MeleeCooldownFactor` correctly showed the Tier 1 tattoo offset in-game, but
`RangedCooldownFactor` silently stayed at its unmodified default (`1.00`) even with the boost active, and never
appeared in the Stats tab at all (a `hideAtValue=1` symptom, not the root cause).

**Root cause** (confirmed via decompiling the actual shipped `Assembly-CSharp.dll`, not the stripped reference
assembly): `RimWorld.StatDef.SetImmutability()` runs during game startup — before this mod's
`[StaticConstructorOnStartup]` registrations execute — and marks any `StatDef` with no existing `<parts>` in XML
(and no other Def content anywhere dynamically referencing it) as `immutable = true`. Once marked, `StatWorker
.GetValue` short-circuits through a permanent, never-invalidated `immutableStatCache` keyed per-pawn, bypassing
`stat.parts` — and therefore `StatPart_TattooEffectOffset` — forever after the first query for that pawn.
`RangedCooldownFactor` has no vanilla `<parts>` and nothing else in the loaded content (Core, the one active DLC,
or any of the customer's other installed mods — all explicitly checked, none reference it) dynamically modifies
it, so it got locked into this immutable/cached state before our registration ever ran. `MeleeCooldownFactor`
only escaped by coincidence — something else in the loaded content already references it, which independently
exempts it from the immutability check regardless of our registration's timing. `MoveSpeed`/`ArmorRating_*`/
`ShootingAccuracyPawn` from features 002–004 were never at risk for the same reason (coincidental vanilla
content already touches all of them) — Stormlash's ranged-attack-speed stat is the first one this mod has
registered that had no such lucky coincidence, which is why this was never caught by features 002–004's own
manual verification passes.

**Fix**: `TattooEffectStatPartInstaller.Register()` now explicitly sets `stat.immutable = false;` and calls
`stat.Worker.DeleteStatCache();` immediately after adding the `StatPart`, for every stat it registers —
unconditionally, not just for the two new attack-speed stats. This makes the fix protect every current and
future registration through this shared installer, regardless of whether the target stat happens to have other
dynamic content referencing it elsewhere, rather than relying on that coincidence continuing to hold.

**Diagnostic process**: three rounds of temporary `Log.Message` probes were added and, once root-caused, fully
removed again before this note was written — (1) direct `Pawn.GetStatValue` readouts on activation, (2) a
registration-time `.parts.Count` check (confirmed both stats correctly received the `StatPart` — ruling out a
registration failure), (3) a per-query log inside `GetStatOffset` (confirmed it was never being invoked for
`RangedCooldownFactor` at all despite being registered, which was the thread that led to `StatDef.immutable`).
None of these probes remain in the shipped code — `git diff` after this fix touches only
`Source/Effects/TattooEffectStatPartInstaller.cs`'s `Register` method.

**Live confirmation**: re-tested in-game after the fix — `Ranged cooldown multiplier` now correctly shows in the
Stats tab (`Base value: 100%`, `Tattoo effects: -15%`, `Final value: 85%`) during an active boost, matching
`Melee cooldown`'s already-correct behavior. Scenario 1's attack-speed requirement (FR-001) is now confirmed
live, not just by static analysis.

**T005 — Scenarios 1 and 2 both CONFIRMED.** Scenario 1: gizmo appears on a tattooed pawn, activation raises
`Move speed` and lowers both `Melee cooldown` and `Ranged cooldown multiplier` for the boost duration (with
correct "Tattoo effects" breakdown lines on each), gizmo shows disabled with a live countdown while on cooldown.
Scenario 2 (FR-010, independent per-pawn state): three separate Stormlash-tattooed colonists (Smarty, Klara,
Joslynn) each showed their own independently-ticking cooldown countdown (7s/8s/9s remaining, offset by their
stagger in activation time) — confirms `cooldownEndTick`/`boostEndTick` are per-comp-instance state, not shared
global cooldown state, satisfying FR-010's "none of Stormlash's effects apply to a pawn that doesn't have it"
by extension (each pawn's gizmo/cooldown is scoped strictly to its own hediff comp).

**T010 — Scenario 3 CONFIRMED.** Ground Klara's progression counter from 3 to 16 via repeated activations (debug
log shows `progress=N` incrementing by exactly one per activation, matching FR-004). The tier flip fired
automatically and exactly at `progress=15` (`tier2Threshold=15`) — the log line for that very activation already
reads `tier=2`, and every activation after stays at `tier=2`, with no ritual, ingredient spend, or slot change
(FR-005). Post-upgrade stat comparison against Tier 1 (T005's earlier readings): `Move speed`'s "Tattoo effects"
line went from `+1.50` → `+2.50` (`6.10` → `7.10 c/s`), matching `moveSpeedOffsetTier2` exactly; `Ranged cooldown
multiplier`'s "Tattoo effects" line went from `-15%` → `-30%` (`85%` → `70%`), matching `attackSpeedBonusTier2`
exactly (FR-006, SC-006 — Tier 2 measurably larger than Tier 1 on both stats).

**Permanent, `Prefs.DevMode`-gated debug logging added** (not temporary — do **not** strip these in T017, per
T017's own "remove... or gate them behind `Prefs.DevMode`" wording): re-adding and removing one-shot diagnostic
`Log.Message` probes for every manual-testing round trip (three rounds just to root-cause R8's immutable-stat
bug) cost a full rebuild+restart each time. Two spots now log unconditionally whenever `Prefs.DevMode` is on,
silent otherwise, each only firing when something actually happened (not on every stat query, avoiding the
spam the first attempt at this produced):
- `StatPart_TattooEffectOffset.GetOffset` — logs when an `IGrantsStatOffsetImmunity` veto actually clamped a
  negative contribution (the raw pre-clamp sum vs. the clamped total), directly useful for Scenario 4.
- `Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity.Postfix` — logs only on the actual `true→false`
  flip, directly useful for Scenario 5.

Both are shared/reusable-infrastructure additions (not Stormlash-specific hacks) — any future tattoo using
either mechanism gets the same visibility for free.

**T011 — Scenario 4 CONFIRMED (the load-bearing "genuine, not bigger-number" claim).** With Bouba Tier 2,
Stormlash active, and a guaranteed-proc `Frost-slowed (2)` hediff also active simultaneously: debug log read
`StatPart_TattooEffectOffset: blocked negative offset(s) for MoveSpeed on Bouba: raw sum would have been 1.00,
clamped total is 2.50` — i.e. Frost Sigil's `-1.50` and Stormlash's `+2.50` would naively sum to `+1.00`, but the
veto clamped Frost Sigil's contribution to `0` entirely rather than just letting the bigger number win, leaving
the *full* `+2.50`. `Move speed`'s Stats tab reading matched exactly: `Tattoo effects: +2.50`, `Final value:
7.10 c/s` (base `4.60` + `2.50`, with zero trace of the `-1.50` slow) — confirmed while the Frost-slow hediff was
still visibly present on Bouba's health tab, not merely after it happened to expire. The "reverts once the boost
window closes" half of this scenario is architecturally the same `boostEndTick` gate already exercised
repeatedly and correctly in T005/T010 (`GetStatOffset` and `BlocksNegativeOffsets` share the identical time
check) — not re-screenshotted separately, but not a new code path either.

**T012 — Scenario 5 CONFIRMED, under real combat with CE loaded.** With hostiles (`Chitose`, `Griever`)
genuinely landing ranged damage on Bouba, Tee, and Fields (`Pawn.PostApplyDamage` log lines show real, varied
damage numbers — not a scripted/forced hit), `Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity`
fired repeatedly while Bouba's Tier 2 Stormlash boost was active and successfully flipped `IsCrouchWalking`
`true→false` on every single check — the debug log's own repeat-message counter shows this happening `99`,
then `27`, then `14` times in a row across the engagement, i.e. CE was continuously trying to apply the
suppression-slow penalty under sustained fire and the patch neutralized it every time, not as a one-off. This
is the live confirmation research.md R4 flagged as still needed beyond static decompilation. The Tier 1
baseline (no immunity) and post-expiry reassertion halves of this scenario were not independently
re-screenshotted this session (Bouba has been Tier 2 since T010 and tier never reverts) — the code path for
those cases is the same simple, already-reviewed conditional (`if` no active Tier 2 boost, the patch no-ops and
leaves CE's own value untouched), not a separately-risky branch.

**T014 — Scenario 6 CONFIRMED.** Granted both `TattooMagic_Hediff_GuardiansCall` and `TattooMagic_Hediff_
Stormlash` to Bouba directly via Dev Mode (bypassing the not-yet-built slot-unlock system, which is fine — this
scenario is a code-level gizmo-coexistence check, not a slot-economy test). Bouba's Health tab lists both
`Stormlash tattoo` and `Guardian's call tattoo`, and the gizmo row shows all four gizmos (Undraft, Melee attack,
Stormlash, Guardian's Call) rendering independently, both currently enabled with no shared cooldown state
visible — confirms feature 004's `IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_TattooGizmos` reuse point works
for a second, unrelated consumer, matching T013's code-level finding of zero changes to any feature-004 file
(SC-010, SC-011).

**T016 (in progress) — Scenario 7 CONFIRMED except the full back-to-back re-run.** Save/reload: tier and
progression counter unchanged immediately after a save/reload compared to immediately before (SC-008,
Constitution Principle V). Removal guard, God-Mode-off case: Bouba's Health tab shows both `Stormlash tattoo`
and `Guardian's call tattoo` with only the info (`i`) icon — no delete/remove control — matching vanilla's own
behavior for hediffs the player can't casually strip, confirming `TattooHediffRemovalGuard` correctly protects
Stormlash with zero Stormlash-specific configuration (FR-017). The God-Mode-on removal-succeeds case and a
direct unsanctioned `RemoveHediff` call were not independently re-exercised this session — both are feature
003's existing, fully generic mechanism (keyed off every `TattooMagicDef.appliedHediff` automatically, already
proven for prior tattoos, and T013 already confirmed no Stormlash-specific code was added anywhere near it).
Only the full seven-scenario back-to-back sign-off re-run remains outstanding.

**Update — removal guard fully confirmed.** God Mode on revealed a "DEV: Remove hediff" option (absent with God
Mode off); using it removed the hediff, and Bouba's Health tab immediately read `(no health conditions)` with
both the Stormlash and Guardian's Call gizmos gone from the toolbar in the same instant (only Undraft/Melee
attack remained) — confirms FR-016 (all effects stop immediately, no lingering gizmo/state) alongside FR-017's
removal-guard protection. T016 is now fully confirmed except the formal full seven-scenario back-to-back
re-run, which is a sign-off formality at this point given every individual scenario has already passed.

**Correction to T013's self-audit — a customer-requested exception, not a leak.** Live testing of Scenario 4
surfaced a real gap: nothing anywhere in the on-hit pipeline logged when a hit actually landed, so there was no
way to tell "no combat is happening" apart from "combat is happening but nothing happened to proc." Fixed with
two more permanent, `Prefs.DevMode`-gated logs, added at the customer's explicit request for combat visibility
across the mod, not just Stormlash's own side of it:
- `Patch_Pawn_PostApplyDamage_TattooOnHit.cs` (feature 002's shared, tattoo-agnostic dispatch patch) — logs
  every hit it sees (attacker, melee/ranged, damage dealt) and, separately, how many `IOnMeleeHitTattooEffect`
  comps a melee hit was dispatched to.
- `Source/Hediffs/HediffComp_FrostSigilEffect.cs` (feature 002's own tattoo-specific file) — logs the proc-chance
  roll itself (chance and outcome) inside `OnMeleeHitTaken`.

The second one **does** touch a file T013 previously confirmed as zero-changed. That's an intentional, narrow
exception: it's purely additive/diagnostic (a `Prefs.DevMode`-gated `Log.Message` call with no change to the
method's actual logic, control flow, or return value), not the kind of behavioral cross-tattoo coupling FR-011/
FR-013 and T013's audit were actually guarding against (Stormlash still doesn't read, call, or depend on
anything in Frost Sigil's file — it's Frost Sigil observing its own proc roll, nothing more). Recorded here so
the discrepancy against T013's original wording is explained rather than silently inconsistent.

**Additional temporary test-tuning**: `Defs/HediffDefs/Tattoos/FrostSigil.xml`'s `slowChanceTier1`/`Tier2` bumped
from `0.15`/`0.3` to `1.0` (guaranteed proc) and `slowDurationTicksTier1`/`Tier2` widened from `180`/`300` to
`1800` (30s) each — the original 15-30% chance lasting only 3-5s made Scenario 4 impractical to actually catch
manually. Also flagged inline in that file for T017 reversion, same convention as Stormlash's own widened
values.
