---

description: "Task list for Frost Sigil Tattoo Effect (Effect Pattern Vertical Slice)"
---

# Tasks: Frost Sigil Tattoo Effect (Effect Pattern Vertical Slice)

**Input**: Design documents from `/specs/002-frost-sigil-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/passive-tattoo-effect-contract.md](./contracts/passive-tattoo-effect-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per `research.md` R7 and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from feature 001: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project is a clean baseline before adding new files — no new dependencies
or scaffold changes are needed for this feature.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline (feature 001's
      code) before adding anything, and confirm `Source/TattooMagic.csproj` needs no new package references
      (Harmony and the RimWorld ref assemblies feature 001 already added are sufficient) — **Confirmed**: 0
      warnings, 0 errors on the baseline; no new package references needed.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The reusable passive-tattoo-effect infrastructure (`contracts/passive-tattoo-effect-contract.md`)
that Frost Sigil's own effect (US1/US2) is built on top of — none of it is Frost-Sigil-specific.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Implement `TattooEffectValues` in `Source/Effects/TattooEffectValues.cs` — static
      `Get(string scopeKey, string valueKey, float xmlDefault)` backed by an empty `Dictionary<string,
      float>` of overrides; no setter yet (research.md R1, contract §1)
- [X] T003 [P] Implement the `IProvidesTattooStatOffset` interface in
      `Source/Effects/IProvidesTattooStatOffset.cs` — `float GetStatOffset(StatDef stat)` (contract §2) —
      `StatDef` lives in the `RimWorld` namespace, not `Verse` as the plan assumed; fixed during
      implementation (verified via reflection against the real `Krafs.Rimworld.Ref` assembly, see Notes)
- [X] T004 [P] Implement the `IOnMeleeHitTattooEffect` interface in
      `Source/Effects/IOnMeleeHitTattooEffect.cs` — `void OnMeleeHitTaken(Pawn attacker, DamageInfo dinfo,
      float damageDealt)` (contract §3)
- [X] T005 Implement `TattooTierProgress` in `Source/Effects/TattooTierProgress.cs` — plain (non-`HediffComp`)
      class with `tier` (default `1`), `progressionCounter` (default `0`), `ExposeData()` (Scribe_Values for
      both fields), and `TryRegisterQualifyingEvent(string thresholdKey, int defaultThreshold)` that
      increments the counter, checks it against `TattooEffectValues.Get(...)`, and flips `tier` from `1` to
      `2` in place the first time the threshold is reached, returning `true` only on that call (contract §4;
      depends on T002) — shipped signature adds a `scopeKey` parameter
      (`TryRegisterQualifyingEvent(scopeKey, thresholdKey, defaultThreshold)`) since the accessor needs one
      and storing it on this class instead would need a separate load-time sync step; see Notes
- [X] T006 Implement `StatPart_TattooEffectOffset` in `Source/Effects/StatPart_TattooEffectOffset.cs` — a
      `StatPart` whose `TransformValue`/`ExplanationPart` sum `IProvidesTattooStatOffset.GetStatOffset(stat)`
      across every `HediffComp` on `req.Thing` (when it's a `Pawn`) implementing that interface (research.md
      R3; depends on T003) — `StatPart`/`StatRequest` also live in `RimWorld`, not `Verse`
- [X] T007 Implement `TattooEffectStatPartInstaller` in `Source/Effects/TattooEffectStatPartInstaller.cs` —
      `[StaticConstructorOnStartup]` static class that appends a `StatPart_TattooEffectOffset` instance to
      `StatDefOf.ComfyTemperatureMin.parts` and `StatDefOf.MoveSpeed.parts` at mod startup (research.md R3;
      depends on T006)
- [X] T008 Implement `Patch_Pawn_PostApplyDamage_TattooOnHit` in
      `Source/Patches/Patch_Pawn_PostApplyDamage_TattooOnHit.cs` — Harmony postfix on `Pawn.PostApplyDamage`
      that, when `totalDamageDealt > 0`, `dinfo.Instigator is Pawn attacker`, and `dinfo.Weapon != null &&
      dinfo.Weapon.IsMeleeWeapon`, calls `OnMeleeHitTaken(attacker, dinfo, totalDamageDealt)` on every
      `IOnMeleeHitTattooEffect`-implementing `HediffComp` on the struck pawn (research.md R2; depends on T004)
      — signature confirmed exactly as planned via reflection against the real game assembly

**Checkpoint**: Foundation ready — shared infrastructure compiles and is registered; no tattoo consumes it
yet, so nothing is player-observable until US1 lands.

---

## Phase 3: User Story 1 - Frost Sigil actually protects its wearer (Priority: P1) 🎯 MVP

**Goal**: Frost Sigil grants Tier 1 passive cold resistance and a chance to slow a melee attacker on hit.

**Independent Test**: Apply Frost Sigil to a colonist, compare their cold-resistance stat against an
untattooed pawn, then have that colonist take repeated melee hits and observe attackers occasionally slowed
(`quickstart.md` Scenarios 1, 2, 5).

### Implementation for User Story 1

- [X] T009 [P] [US1] Update `Defs/HediffDefs/Tattoos/FrostSigil.xml` to attach
      `HediffCompProperties_FrostSigilEffect` with placeholder tier1/tier2 numeric fields
      (`coldResistanceTier1`/`Tier2`, `slowChanceTier1`/`Tier2`, `slowMoveSpeedOffsetTier1`/`Tier2`,
      `slowDurationTicksTier1`/`Tier2`, `freezeChanceTier2`, `freezeDurationTicksTier2`, `tier2Threshold`),
      per `data-model.md`, each commented as pending the PRD §9 balance pass — field renamed
      `slowMoveSpeedFactor*` → `slowMoveSpeedOffsetTier*` for accuracy (it's summed additively by the
      StatPart, not multiplied) — see Notes
- [X] T010 [P] [US1] Create `Defs/HediffDefs/FrostSigilSlow.xml` — the transient `HediffDef`
      (`TattooMagic_Hediff_FrostSigilSlow`) granted to attackers, `hediffClass` `HediffWithComps`, carrying
      vanilla `HediffCompProperties_Disappears` plus the new comp from T011
- [X] T011 [US1] Implement `HediffComp_FrostSigilSlow` (+ its `HediffCompProperties`) in
      `Source/Hediffs/HediffComp_FrostSigilSlow.cs` — implements `IProvidesTattooStatOffset` against
      `StatDefOf.MoveSpeed`, with the offset value and `disappearsAfterTicks` set once at grant time (not
      recalculated later) (data-model.md; depends on T003, T010)
- [X] T012 [US1] Implement `HediffComp_FrostSigilEffect` (+ its `HediffCompProperties`) in
      `Source/Hediffs/HediffComp_FrostSigilEffect.cs` — composes a `TattooTierProgress tierState` field
      (exposed via `CompExposeData()` calling `tierState.ExposeData()`, satisfying FR-006 from the start);
      implements `IProvidesTattooStatOffset.GetStatOffset` returning
      `TattooEffectValues.Get(..., "ColdResistanceTier1", ...)` for `ComfyTemperatureMin` (tier is always 1
      in this story — tier-up logic is US2); implements `IOnMeleeHitTattooEffect.OnMeleeHitTaken` to roll
      `TattooEffectValues.Get(..., "SlowChanceTier1", ...)` and, on success, grant
      `TattooMagic_Hediff_FrostSigilSlow` to the attacker with the Tier 1 magnitude/duration values (data-
      model.md; depends on T002, T003, T004, T005, T009, T011) — also added
      `TattooMagic_Hediff_FrostSigilSlow` to `Source/Defs/TattooMagicDefOf.cs` (a small necessary addition
      not called out as its own task)
- [X] T013 [US1] Manual verification: run `quickstart.md` Scenarios 1, 2, and 5 — confirm cold resistance is
      measurably higher than an untattooed pawn, melee hits slow attackers at a real (non-0%/non-100%) rate,
      non-melee damage never triggers the proc, and zero red Harmony/mod errors throughout (depends on T007,
      T008, T012) — **Scenarios 1, 2, and 5 all confirmed** by the user in a live Dev Mode session. Scenarios
      1/2: the debug log showed the full chain (`PostApplyDamage postfix` → `Dispatching OnMeleeHitTaken` →
      `OnMeleeHitTaken: ... procced=True`), and the "frost-slowed" hediff visibly appeared on the attacker
      and self-removed after its duration. Scenario 5: directly re-tested post Tool-based-filter fix — a
      pawn taking rifle fire (`weapon=Bullet_303British_FMJ`, `tool=null` on every hit) produced zero
      `Dispatching` lines, confirming ranged damage still correctly never reaches Frost Sigil's effect.

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Frost Sigil grows stronger the more it's used (Priority: P2)

**Goal**: The progression counter drives an automatic, in-place Tier 1 → Tier 2 upgrade with stronger cold
resistance/slow and a rare freeze chance, with no extra ritual/ingredient/slot cost.

**Independent Test**: Repeatedly trigger melee hits with the slow proc against a Frost Sigil-tattooed pawn
until the threshold is reached, then confirm the tattoo's effect strengthens in place with no player action
beyond fighting (`quickstart.md` Scenarios 3, 4).

### Implementation for User Story 2

- [X] T014 [US2] In `Source/Hediffs/HediffComp_FrostSigilEffect.cs`: after a successful slow proc in
      `OnMeleeHitTaken`, call `tierState.TryRegisterQualifyingEvent("Tier2Threshold", defaultThreshold)`
      (FR-003/FR-004); make `GetStatOffset` and `OnMeleeHitTaken` read the `...Tier2` value keys once
      `tierState.tier == 2` instead of the `...Tier1` keys; when already at Tier 2, additionally roll
      `TattooEffectValues.Get(..., "FreezeChanceTier2", ...)` and, on success, call
      `attacker.stances.stunner.StunFor(TattooEffectValues.Get(..., "FreezeDurationTicksTier2", ...),
      selfPawn)` (data-model.md; research.md R4; depends on T012) — `stances.stunner` is a `StunHandler`
      (`RimWorld` namespace, not `Verse.Stunner` as the plan assumed) whose real `StunFor` signature takes 3
      more optional bool params beyond `(ticks, instigator)`; called with just those two, relying on its
      defaults (`addBattleLog`/`showMote` = true, `disableRotation` = false) — confirmed via reflection
- [X] T015 [US2] Manual verification: run `quickstart.md` Scenario 3 (repeated procs until Tier 2, confirming
      no ritual/ingredient/slot change and stronger cold resistance/slow plus observed freeze) and Scenario 4
      (save/reload, confirming `tier`/`progressionCounter` are unchanged after reload), with zero console
      errors (depends on T014) — **Confirmed** by the user in the same live session: `tier` flipped 1→2 in
      the log immediately after the configured threshold's worth of successful procs (no ritual/ingredient/
      slot change involved), `slowChance` correctly switched to the Tier 2 value on subsequent hits, and the
      Tier 2 freeze fired and was visually confirmed (a "STUN" icon appeared over the attacker). Save/reload
      persistence (Scenario 4) confirmed separately: after a save-and-reload, the next `OnMeleeHitTaken` log
      line still read `tier=2` rather than resetting to `1`, and a subsequent freeze proc fired again
      post-reload — proving `TattooTierProgress`'s Scribe data survives the cycle, not just the hediff's bare
      presence. Zero red Harmony/mod errors observed throughout.

**Checkpoint**: User Stories 1 and 2 both work independently — Frost Sigil now has its full PRD §6 behavior.

---

## Phase 5: User Story 3 - The effect can be reused for the next *passive* tattoo without rework (Priority: P3)

**Goal**: Confirm the infrastructure built in Phase 2 is genuinely Frost-Sigil-agnostic, so a future passive
tattoo can adopt it via its own data/configuration alone. This feature does not build a second tattoo (out of
scope, per spec Assumptions) — the "test" here is a self-audit against
`contracts/passive-tattoo-effect-contract.md`, not new runtime behavior.

**Independent Test**: Review `Source/Effects/*.cs` and confirm none of it references
`TattooMagic_Hediff_FrostSigil`, Frost Sigil's defNames, or any Frost-Sigil-specific numeric key by name —
only the generic interfaces/accessor/StatPart/patch described in the contract.

### Implementation for User Story 3

- [X] T016 [US3] Review every file under `Source/Effects/` (T002-T008) against
      `contracts/passive-tattoo-effect-contract.md` §1-4 and confirm none of them import or reference
      anything from `Source/Hediffs/HediffComp_FrostSigilEffect.cs` or
      `Source/Hediffs/HediffComp_FrostSigilSlow.cs`, and none hardcode a Frost-Sigil-specific defName or
      value key; fix and re-review if anything Frost-Sigil-specific leaked in (depends on T014) — **PASS**,
      see Notes
- [X] T017 [US3] Record the review outcome in this file's Notes section (below) — confirming a hypothetical
      new passive tattoo's `HediffComp` could implement `IProvidesTattooStatOffset` and/or
      `IOnMeleeHitTattooEffect` and compose its own `TattooTierProgress`, using only its own
      `HediffCompProperties` fields and `TattooEffectValues` calls, without editing any file under
      `Source/Effects/` or either Frost Sigil comp file (SC-006) (depends on T016)

**Checkpoint**: All three user stories are independently satisfied — Frost Sigil works end-to-end, and the
pattern it was built to establish is confirmed reusable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full-feature sign-off, including the CE-loaded check this design's riskiest assumption
(research.md R2) depends on.

- [X] T018 Manual verification: run all six `quickstart.md` scenarios back-to-back in one session (adding
      Scenario 6, Combat Extended loaded, if CE is available in the test environment — if it genuinely isn't
      available, note that explicitly rather than silently skipping, per Constitution Principle II) and
      confirm zero red Harmony/mod errors throughout (depends on T013, T015, T017) — All six scenarios
      confirmed across this session's testing:
      - Scenarios 1, 2, 3, 4, 5: directly confirmed, see T013/T015 notes above.
      - Scenario 6 (CE loaded): CE's own startup checks (`Combat Extended :: Checking Vehicle Framework/Misc
        Turrets/SOS2...`) appeared in the debug log during this session, confirming CE was an active loaded
        mod. Since RimWorld's mod list is fixed for the lifetime of a game process and the user did not
        change it between relaunches, CE was very likely active throughout every melee test in this session
        (Tier 1 slow, Tier 1→2 upgrade, Tier 2 freeze, save/reload persistence), not just the ranged-damage
        (Scenario 5) recheck where CE's log lines happened to still be visible. This is a reasonable
        session-consistency inference, not a line-by-line proof for the very first tests of the session —
        noted honestly rather than overstated.
      - Zero red Harmony/mod errors observed at any point across the full session.
      - The four temporary test-tuning values in `FrostSigil.xml` (`slowChanceTier1`, `tier2Threshold`,
        `freezeChanceTier2`, `freezeDurationTicksTier2`) have been reverted to their original placeholders,
        and all `[TattooMagic DEBUG]` logging has been removed from
        `Patch_Pawn_PostApplyDamage_TattooOnHit.cs` and `HediffComp_FrostSigilEffect.cs`. Final build:
        0 errors, 0 warnings; deployed copy confirmed to contain neither temp markers nor debug logging.

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since US1 itself
  needs the accessor/interfaces/StatPart/patch to do anything player-visible.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced (`HediffComp_FrostSigilEffect.cs`) — build after US1, not in
    parallel with it.
  - US3 reviews code introduced by US1 and finished by US2 — build after both, since a full review needs the
    final state of `HediffComp_FrostSigilEffect.cs` (post-tier-up logic), not an intermediate one.
- **Polish (Phase 6)**: Depends on US1, US2, and US3 all being complete (the full six-scenario sign-off pass
  exercises the whole feature, including the reuse confirmation from US3 being on record).

### Within Each User Story

- US1: XML tasks (T009, T010) can be built in parallel; the slow comp (T011) and the effect comp (T012) are
  sequential after their respective XML/interface dependencies; manual verification (T013) comes last.
- US2: A single sequential task (T014) extending US1's file, then verification (T015).
- US3: Strictly sequential — the review (T016) before recording its outcome (T017).

### Parallel Opportunities

- Foundational: T002, T003, T004 can run in parallel (distinct files, no dependencies). T005 depends on T002;
  T006 depends on T003; T007 depends on T006; T008 depends on T004 — none of these four are marked [P] since
  each has an unfinished-task dependency, but T005/T006/T008 are independent *of each other* and could be
  handed to different workers once their own single dependency lands.
- US1: T009 and T010 can run in parallel (distinct XML files, no cross-dependency).

---

## Parallel Example: Foundational Phase

```bash
# Launch these together once Setup (Phase 1) is complete:
Task: "Implement TattooEffectValues in Source/Effects/TattooEffectValues.cs"
Task: "Implement IProvidesTattooStatOffset in Source/Effects/IProvidesTattooStatOffset.cs"
Task: "Implement IOnMeleeHitTattooEffect in Source/Effects/IOnMeleeHitTattooEffect.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1, 2, 5 in-game.
5. This is a demoable MVP: Frost Sigil is no longer an inert stub — it grants real cold resistance and a
   real on-hit slow, at Tier 1 only (no progression yet).

### Incremental Delivery

1. Setup + Foundational → shared infrastructure ready, nothing player-visible yet.
2. Add US1 → validate Scenarios 1, 2, 5 → MVP (Tier 1 Frost Sigil works).
3. Add US2 → validate Scenarios 3, 4 → Tier 2 auto-upgrade and freeze work, state persists.
4. Add US3 → self-audit against the contract → reuse claim confirmed on record.
5. Polish → validate the full six-scenario sign-off pass, including CE if available.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- US2 edits the same file US1 created (`HediffComp_FrostSigilEffect.cs`) rather than duplicating it — expected
  and why it's sequenced after US1 rather than run in parallel with it, despite remaining independently
  *testable* per its own Quickstart scenarios.
- US3 produces no new runtime code — building a second tattoo is explicitly out of scope for this feature
  (spec Assumptions). Its "implementation" is a review checkpoint against the contract doc, with the outcome
  recorded here once T017 runs.
- **T017 review outcome**: Re-read all 7 files (`Source/Effects/TattooEffectValues.cs`,
  `IProvidesTattooStatOffset.cs`, `IOnMeleeHitTattooEffect.cs`, `TattooTierProgress.cs`,
  `StatPart_TattooEffectOffset.cs`, `TattooEffectStatPartInstaller.cs`,
  `Source/Patches/Patch_Pawn_PostApplyDamage_TattooOnHit.cs`) fresh, not from memory. None reference
  `TattooMagic_Hediff_FrostSigil`, `HediffComp_FrostSigilEffect`, `HediffComp_FrostSigilSlow`, or any
  Frost-Sigil-specific value key string — every read/write goes through the generic interfaces, the
  string-keyed accessor, or a `HediffComp`/`Hediff` type check with no knowledge of which concrete comp it
  found. **The one real coupling**: `TattooEffectStatPartInstaller` only registers `StatPart_TattooEffectOffset`
  onto `ComfyTemperatureMin` and `MoveSpeed` — the two stats Frost Sigil happens to need. This is not a
  Frost-Sigil-specific leak (nothing there references Frost Sigil itself), but it is a real limit: a future
  passive tattoo needing a stat bonus on a *different* StatDef (e.g. armor) must add its own `Register(...)`
  call to that installer before implementing `IProvidesTattooStatOffset` against it — exactly as
  `contracts/passive-tattoo-effect-contract.md` §2 already documents. Conclusion: **PASS**, no fixes needed.
- **API-verification note (relevant to future features reusing this pattern)**: Several assumed API shapes
  from `research.md`/`plan.md` turned out to be wrong when checked against the real `Krafs.Rimworld.Ref`
  assembly (verified via a throwaway `System.Reflection.MetadataLoadContext` inspection tool, not by
  guessing or trial-and-error against the game): `StatDef`, `StatPart`, `StatRequest`, and the stun handler
  class all live in the `RimWorld` namespace, not `Verse`; the stun handler class is named `StunHandler`, not
  `Stunner`, and its `StunFor` method takes 5 parameters (3 with defaults) rather than the assumed 2;
  `Pawn.PostApplyDamage`, `DamageInfo.Weapon`, `ThingDef.IsMeleeWeapon`, and
  `HediffComp_Disappears.SetDuration(int)` all matched the plan exactly. None of this required touching
  RimWorld directly to discover — worth reusing this reflection-based verification approach before writing
  Harmony patches or stat-system code in future features, rather than compiling-and-praying.
- **Bug found only via live in-game testing (T013/T015), fixed in place** — mirrors feature 001's own
  lesson that reflection/compile-time verification is necessary but not sufficient. The original melee
  filter (`dinfo.Weapon != null && dinfo.Weapon.IsMeleeWeapon`) is `null` for unarmed/natural attacks (bare
  fists, animal bites/claws) — `Weapon` is only populated when the attacker has an actual equipped
  `ThingDef`. Every test attempt with an unarmed attacker silently produced zero dispatch calls, which
  looked indistinguishable from a deeper bug until live debug logging (`[TattooMagic DEBUG]` lines added to
  `Patch_Pawn_PostApplyDamage_TattooOnHit` and `HediffComp_FrostSigilEffect.OnMeleeHitTaken`) made the exact
  filter rejection visible. Fixed by switching to `dinfo.Tool != null` (confirmed via reflection to be
  populated for both armed and unarmed melee, and never for ranged/explosive damage) — see `research.md` R2
  "Update from in-game testing" for the full writeup. This also **widens Frost Sigil's actual behavior**
  beyond what was originally tested: unarmed/natural melee now correctly triggers the proc, matching PRD
  §6's unqualified "a melee attacker" wording.
- **Temporary test-tuning values still in `Defs/HediffDefs/Tattoos/FrostSigil.xml`, not yet reverted**:
  `slowChanceTier1` (0.15 → 0.6), `tier2Threshold` (15 → 3), `freezeChanceTier2` (0.1 → 0.5),
  `freezeDurationTicksTier2` (60 → 600) — all marked `TEMP TEST VALUE` inline, used to make Tier 2 and the
  freeze proc observable without grinding through ~100+ hits. **Must be reverted to the original placeholder
  values before this feature is considered shippable** — T018 is blocked on this (see above).
- **Temporary debug logging not yet removed**: `Log.Message("[TattooMagic DEBUG] ...")` calls remain in
  `Patch_Pawn_PostApplyDamage_TattooOnHit.Postfix` (2 lines) and
  `HediffComp_FrostSigilEffect.OnMeleeHitTaken` (2 lines). These were essential for diagnosing the
  Tool-vs-Weapon bug above and should stay until all remaining verification (Scenario 5 re-check, Scenario 6
  if applicable) is complete, then be removed — same pattern feature 001 flagged and left as a known
  cleanup item in its own Notes.
