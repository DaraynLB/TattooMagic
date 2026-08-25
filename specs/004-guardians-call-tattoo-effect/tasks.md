---

description: "Task list for Guardian's Call Tattoo Effect (First Triggered Ability + Two-Path AI Targeting Slice)"
---

# Tasks: Guardian's Call Tattoo Effect (First Triggered Ability + Two-Path AI Targeting Slice)

**Input**: Design documents from `/specs/004-guardians-call-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/triggered-tattoo-effect-contract.md](./contracts/triggered-tattoo-effect-contract.md), [contracts/two-path-targeting-contract.md](./contracts/two-path-targeting-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per `research.md` R7 and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–003: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–003) is a clean baseline before adding new files — no
new dependencies or scaffold changes are needed for this feature.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline (features 001–003)
      before adding anything, and confirm `Source/TattooMagic.csproj` needs no new package references (Harmony
      and the RimWorld ref assemblies already present are sufficient).

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The new tattoo-agnostic gizmo reuse point (`contracts/triggered-tattoo-effect-contract.md` §1)
that Guardian's Call's own ability (US1) is built on top of. Everything else this feature needs
(`TattooEffectValues`, `TattooTierProgress`, `IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset`,
`CombatExtendedInterop`, the CE reflection-patch convention) already exists from features 002/003, unmodified.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Implement the `IProvidesTattooGizmo` interface in `Source/Effects/IProvidesTattooGizmo.cs` —
      `Gizmo GetGizmo();` (research.md R1, contract §1)
- [X] T003 Implement `Patch_Pawn_GetGizmos_TattooGizmos` in
      `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs` — Harmony postfix on `Pawn.GetGizmos()` that
      iterates the pawn's `HediffWithComps`/comps and yields `GetGizmo()` for every comp implementing
      `IProvidesTattooGizmo`, alongside vanilla's own gizmos (research.md R1, contract §1; depends on T002)

**Checkpoint**: Foundation ready — the gizmo reuse point compiles and is registered; no tattoo consumes it yet,
so nothing is player-observable until US1 lands.

---

## Phase 3: User Story 1 - Guardian's Call makes its wearer the target everyone attacks (Priority: P1) 🎯 MVP

**Goal**: A Guardian's Call-tattooed pawn has an activatable gizmo that, on use, forces nearby hostiles to
prioritize attacking that pawn for a duration, then goes on cooldown — verified in both a non-CE and a
CE-loaded game.

**Independent Test**: Apply Guardian's Call to a colonist, provoke a fight with multiple hostiles nearby,
activate the ability, and observe hostiles redirect their attacks to the tattooed colonist for the duration,
then resume normal targeting once it ends — in both a non-CE game and a CE-loaded game (`quickstart.md`
Scenarios 1, 2, 3, 5).

### Implementation for User Story 1

- [X] T004 [P] [US1] Update `Defs/HediffDefs/Tattoos/GuardiansCall.xml` to attach
      `HediffCompProperties_GuardiansCallEffect` with placeholder tier1/tier2 numeric fields
      (`tauntRangeTier1`/`Tier2`, `tauntDurationTicksTier1`/`Tier2`, `cooldownDurationTicksTier1`/`Tier2`,
      `tier2Threshold`, `armorOffsetTier2`, `abilityIconPath`), per `data-model.md`, each commented as pending
      the PRD §9 balance pass
- [X] T005 [US1] Implement `GuardiansCallTauntRegistry` in `Source/Hediffs/GuardiansCallTauntRegistry.cs` — a
      static `HashSet<HediffComp_GuardiansCallEffect>` of comps with a currently-active taunt, a `Register`
      method, and an `ActiveTaunters()` enumerator that lazily prunes any entry whose owning pawn is
      dead/downed/no longer carries the hediff, or whose `tauntEndTick` has passed (research.md R3,
      data-model.md)
- [X] T006 [US1] Implement `HediffComp_GuardiansCallEffect` (+ its `HediffCompProperties`) in
      `Source/Hediffs/HediffComp_GuardiansCallEffect.cs` — composes a `TattooTierProgress tierState` field
      (exposed via `CompExposeData()` calling `tierState.ExposeData()`, plus `Scribe_Values.Look` for
      `cooldownEndTick`/`tauntEndTick`, satisfying FR-008 from the start); implements
      `IProvidesTattooGizmo.GetGizmo()` returning a `Command_Action` whose `action` calls `TryActivate()` and
      whose `disabled`/`disabledReason` are computed live from `cooldownEndTick`; `TryActivate()` no-ops if
      still on cooldown, otherwise sets `cooldownEndTick`/`tauntEndTick` from `TattooEffectValues`-resolved
      Tier 1 values and calls `GuardiansCallTauntRegistry.Register(this)`; implements
      `IProvidesTattooStatOffset.GetStatOffset` returning `0f` unconditionally for now (Tier 2's armor buff is
      US2's concern — tier never leaves 1 in this story since nothing yet calls
      `TryRegisterQualifyingEvent`) (data-model.md; depends on T002, T004, T005)
- [X] T007 [US1] Implement `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt` in
      `Source/Patches/Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs` — Harmony postfix on
      `Verse.AI.AttackTargetFinder.BestAttackTarget` that, when `searcher.Thing is Pawn searcherPawn` hostile to
      any pawn in `GuardiansCallTauntRegistry.ActiveTaunters()` within that taunter's accessor-resolved taunt
      range, and that taunter passes the original `validator` (if any) plus
      `searcherPawn.CanReach(taunterPawn, PathEndMode.Touch, Danger.Some)`, overrides `__result` with the
      nearest such qualifying taunter — otherwise leaves vanilla's own result untouched (research.md R4,
      contract `two-path-targeting-contract.md` §1; depends on T005, T006)
- [X] T008 [US1] Manual verification: run `quickstart.md` Scenarios 1, 2, and 3 (non-CE) — confirm the gizmo
      appears only on a tattooed pawn, activation redirects nearby hostiles' targeting and starts the
      cooldown, re-activation is blocked while on cooldown, the taunt is a live standing check (a hostile
      arriving mid-duration is also redirected), and the taunt ends immediately if the tattooed pawn is
      downed/killed/loses the hediff — zero red Harmony/mod errors throughout (depends on T007)
- [X] T009 [US1] Manual investigation (Combat Extended loaded): add temporary `Log.Message` probes to
      `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt`'s postfix (per this project's proactive-
      debug-logging practice) and run `quickstart.md` Scenario 5 steps 1-3 to determine whether the postfix
      already fires for and correctly redirects CE-controlled hostile pawns (research.md R5). Record which
      outcome (A: already covered, or B: CE bypasses `BestAttackTarget`) was observed in this file's Notes
      section below (depends on T008; requires Combat Extended installed)
- [X] T010 [US1] **Conditional on T009's outcome** — N/A, Outcome A confirmed (see Notes) — only if Outcome B was observed: implement
      `Patch_CE_AttackTargetFinder_GuardiansCallTaunt` in
      `Source/Patches/Patch_CE_AttackTargetFinder_GuardiansCallTaunt.cs` following
      `Patch_CE_ProjectileImpact_TattooAmmoBonus`'s reflection convention (`AccessTools.TypeByName`/`Method`
      resolution gated behind `CombatExtendedInterop.IsLoaded`, consulting the same
      `GuardiansCallTauntRegistry`), per `contracts/two-path-targeting-contract.md` §2, and add one line to
      `TattooMagicMod.cs` calling its `TryApply(harmony)` alongside the existing CE patch registration. If
      Outcome A was observed, mark this task N/A in Notes and skip it (depends on T009)
- [X] T011 [US1] Manual verification: run `quickstart.md` Scenario 5 in full (both ranged and melee CE-equipped
      hostiles) and confirm taunt redirection now works correctly under Combat Extended with zero red
      Harmony/mod errors — this is the second of Constitution Principle IV's two required, independently-
      verified paths (depends on T009, T010)

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Guardian's Call grows stronger the more it's used (Priority: P2)

**Goal**: The progression counter drives an automatic, in-place Tier 1 → Tier 2 upgrade that additionally
grants a temporary armor buff for each activation's taunt duration, with no extra ritual/ingredient/slot cost.

**Independent Test**: Activate a Guardian's Call-tattooed pawn's ability enough times to reach the threshold,
then confirm the tattoo's effect strengthens in place — the taunt still works, and the Tier 2 armor buff is now
present for the duration of subsequent activations (`quickstart.md` Scenario 4).

### Implementation for User Story 2

- [X] T012 [P] [US2] In `Source/Effects/TattooEffectStatPartInstaller.cs`, add three `Register(...)` calls for
      `StatDefOf.ArmorRating_Sharp`, `StatDefOf.ArmorRating_Blunt`, and `StatDefOf.ArmorRating_Heat` (research.md
      R6) — no other change to this file's existing registrations (`ComfyTemperatureMin`, `MoveSpeed`,
      `ShootingAccuracyPawn`, `CombatExtendedInterop.AimingAccuracy` stay untouched)
- [X] T013 [US2] In `Source/Hediffs/HediffComp_GuardiansCallEffect.cs`: in `TryActivate()`, after resolving the
      cooldown/taunt-end ticks, call `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold",
      Props.tier2Threshold)` (FR-004/FR-005); make `TryActivate()` read the `...Tier2` range/duration/cooldown
      value keys once `tierState.tier == 2` instead of the `...Tier1` keys; replace `GetStatOffset`'s
      unconditional `0f` with: return `TattooEffectValues.Get(ScopeKey, "ArmorOffsetTier2",
      Props.armorOffsetTier2)` for `ArmorRating_Sharp`/`Blunt`/`Heat` only when `tierState.tier >= 2 &&
      Find.TickManager.TicksGame < tauntEndTick`, else `0f` (data-model.md; depends on T006, T012)
- [X] T014 [US2] Manual verification: run `quickstart.md` Scenario 4 — confirm the progression counter
      increments by exactly one per activation, the tattoo upgrades to Tier 2 in place with no ritual/
      ingredient/slot change once the threshold is reached, the Tier 2 armor buff appears (visible in the
      Stats tab's "Tattoo effects" explanation) only during an active taunt window and only at Tier 2, and
      returns to normal once that window ends, with the Tier 1 taunt behavior itself unchanged (depends on
      T013)

**Checkpoint**: User Stories 1 and 2 both work independently — Guardian's Call now has its full PRD §6
behavior.

---

## Phase 5: User Story 3 - The ability and targeting patterns can be reused for future tattoos (Priority: P3)

**Goal**: Confirm the gizmo/cooldown reuse point (Phase 2) and the two-path targeting convention
(`contracts/two-path-targeting-contract.md`) are genuinely reusable by a future triggered or targeting-related
tattoo, without needing to touch Guardian's Call's own files. This feature does not build a second triggered
tattoo (out of scope, per spec Assumptions) — the "test" here is a self-audit against both contracts, not new
runtime behavior.

**Independent Test**: Review `Source/Effects/IProvidesTattooGizmo.cs` and
`Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs` and confirm neither references
`TattooMagic_Hediff_GuardiansCall`, Guardian's Call's defNames, or any Guardian's-Call-specific value key by
name; separately confirm the two-path targeting patches' Guardian's-Call-specific pieces (the registry, the
taunt bias logic) are clearly documented as non-reusable while the *pattern* they follow is what a future
tattoo would actually reuse.

### Implementation for User Story 3

- [X] T015 [US3] Review `Source/Effects/IProvidesTattooGizmo.cs` and
      `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs` (T002-T003) against
      `contracts/triggered-tattoo-effect-contract.md` §1, and review
      `Source/Patches/Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs` (and
      `Patch_CE_AttackTargetFinder_GuardiansCallTaunt.cs` if T010 produced one) against
      `contracts/two-path-targeting-contract.md`; confirm none of the Phase 2 gizmo pieces hardcode a
      Guardian's-Call-specific defName or value key, and confirm the targeting contract accurately documents
      which pieces are reusable (the pattern) vs. tattoo-specific (the registry, the bias logic) as actually
      implemented; fix and re-review if anything Guardian's-Call-specific leaked into the gizmo reuse point
      (depends on T013)
- [X] T016 [US3] Record the review outcome in this file's Notes section (below) — confirming a hypothetical
      future triggered tattoo (Bloodrune, Stormlash, Wraithstep, Berserker's Mark) could implement
      `IProvidesTattooGizmo` on its own comp and compose its own cooldown-tick field, picked up automatically
      by the shared patch with no changes to Guardian's Call's files or the patch itself, and a hypothetical
      future targeting-related tattoo could follow the two-path convention documented in
      `two-path-targeting-contract.md` §3 (SC-008, SC-009) (depends on T015)

**Checkpoint**: All three user stories are independently satisfied — Guardian's Call works end-to-end, and the
patterns it was built to establish are confirmed reusable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full-feature sign-off, including the cross-cutting checks (no effect without the tattoo, the
existing removal guard, save/reload persistence) that aren't specific to any single user story.

- [X] T017 Manual verification: run `quickstart.md` Scenario 6 (no gizmo/taunt without the tattoo; removal
      blocked except via God Mode or this mod's own sanctioned path, reusing feature 003's existing guard with
      zero Guardian's-Call-specific configuration; tier/counter unchanged across a save/reload), then re-run
      all six `quickstart.md` scenarios back-to-back in one session, confirming zero red Harmony/mod errors
      throughout (depends on T008, T011, T014, T016)
- [X] T018 Remove any temporary `Log.Message` debug probes added for T009/T011's CE investigation (or gate
      them behind `Prefs.DevMode`) and revert any temporary test-tuning values used to make Tier 2/cooldowns
      observable without a long grind back to their PRD §9 placeholder values in
      `Defs/HediffDefs/Tattoos/GuardiansCall.xml`, then confirm a clean `dotnet build` (0 errors, 0 warnings)
      (depends on T017)

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since US1 itself
  needs the gizmo interface/patch to make the ability visible to the player at all.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced (`HediffComp_GuardiansCallEffect.cs`) and the existing
    `TattooEffectStatPartInstaller.cs` — build after US1, not in parallel with it.
  - US3 reviews code introduced by US1 and finished by US2 — build after both, since a full review needs the
    final state of the targeting patches and comp (post-Tier-2 logic), not an intermediate one.
- **Polish (Phase 6)**: Depends on US1, US2, and US3 all being complete (the full six-scenario sign-off pass
  exercises the whole feature, including the reuse confirmation from US3 being on record).

### Within Each User Story

- US1: The XML task (T004) can run in parallel with Foundational; the registry (T005) and comp (T006) are
  sequential (comp depends on registry); the vanilla targeting patch (T007) depends on both; non-CE
  verification (T008) comes next; the CE investigation (T009), its conditional patch (T010), and CE
  verification (T011) are strictly sequential, since T010's very existence depends on what T009 finds.
- US2: The stat-installer edit (T012) can run in parallel with anything else in this phase (different file,
  no dependency on T013); the comp edit (T013) depends on both T006 (US1) and T012; verification (T014) comes
  last.
- US3: Strictly sequential — the review (T015) before recording its outcome (T016).

### Parallel Opportunities

- Foundational: T002 has no dependencies; T003 depends on T002 (not marked [P]).
- US1: T004 (XML) can run in parallel with T002/T003 (Foundational) and with T005 (registry) — distinct files,
  no cross-dependency until T006 needs all three.
- US2: T012 (stat installer) can run in parallel with T006 (US1's comp) once Foundational is done — distinct
  files; T013 is the one task in this feature that depends on work from two different phases (T006 from US1,
  T012 from this phase).

---

## Parallel Example: Foundational Phase + US1 setup

```bash
# Launch these together once Setup (Phase 1) is complete:
Task: "Implement IProvidesTattooGizmo in Source/Effects/IProvidesTattooGizmo.cs"
Task: "Update Defs/HediffDefs/Tattoos/GuardiansCall.xml with HediffCompProperties_GuardiansCallEffect"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1, including the mandatory CE verification (T009-T011) — per Constitution
   Principle IV, US1 is not actually done until the CE path is confirmed, not just the vanilla one.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1, 2, 3, 5 in-game.
5. This is a demoable MVP: Guardian's Call is no longer an inert stub — it grants a real taunt ability with a
   real cooldown, at Tier 1 only (no progression or armor buff yet), verified in both non-CE and CE games.

### Incremental Delivery

1. Setup + Foundational → gizmo reuse point ready, nothing player-visible yet.
2. Add US1 → validate Scenarios 1, 2, 3, 5 → MVP (Tier 1 Guardian's Call works, vanilla and CE).
3. Add US2 → validate Scenario 4 → Tier 2 auto-upgrade and armor buff work.
4. Add US3 → self-audit against both contracts → reuse claims confirmed on record.
5. Polish → validate the full six-scenario sign-off pass and clean up any temporary debug/test artifacts.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- US2 edits the same file US1 created (`HediffComp_GuardiansCallEffect.cs`) rather than duplicating it —
  expected and why it's sequenced after US1 rather than run in parallel with it, despite remaining
  independently *testable* per its own Quickstart scenario.
- US3 produces no new runtime code — building a second triggered tattoo, or a second targeting-influencing
  tattoo, is explicitly out of scope for this feature (spec Assumptions). Its "implementation" is a review
  checkpoint against both contracts, with the outcome recorded here once T016 runs.
- T009's outcome (Outcome A vs. Outcome B) and, if applicable, T010's implementation notes should be recorded
  here once the CE investigation actually runs — this determines whether a second Harmony patch ships with
  this feature or the single vanilla-path postfix is confirmed sufficient for both configurations.
- T016's review outcome (the US3 self-audit) should be recorded here once it runs, mirroring feature 002's
  T017 precedent of documenting the actual files reviewed and any coupling found (e.g. whether the
  `TattooEffectStatPartInstaller` registration in T012 counts as a "leak" — per feature 002's own precedent,
  it does not, since the installer's own doc comment already anticipates future tattoos registering additional
  StatDefs there).

## Implementation Session Notes (2026-08-08)

- **T001–T007, T012–T013 code complete**, `dotnet build` clean (0 errors, 0 warnings) after each phase. Two
  API corrections needed versus the plan's assumed member names, found only by compiling against
  `Krafs.Rimworld.Ref` 1.6.4871 (not guessable from docs alone): `Command_Action`'s cooldown-disable flag is
  the property `Disabled` (not a field `disabled`, which resolves to an inaccessible member on the `Gizmo`
  base type), and hostility checks use the static `RimWorld.GenHostility.HostileTo(Thing, Thing)` (there is no
  `Pawn.HostileTo` instance/extension method). Both are reflected in
  `Source/Hediffs/HediffComp_GuardiansCallEffect.cs` and
  `Source/Patches/Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs`.
- **T009 outcome: Outcome A confirmed** — for *fresh* targeting decisions specifically. Live CE-loaded testing
  (both ranged hostiles and a disarmed-into-melee hostile) showed
  `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt`'s existing vanilla-path postfix does fire for
  and correctly redirect CE-controlled pawns making a fresh `BestAttackTarget` decision — no gap there, no
  second patch needed for this part. **T010 remains N/A** on this basis.
- **A separate, deeper gap was found during CE testing, unrelated to T009/T010's scope**: a hostile *already*
  mid-combat with a non-tattooed pawn would not reliably get pulled onto the taunter, even after activation.
  Root-caused via live reflection inspection of the actual `Krafs.Rimworld.Ref`/`CombatExtended.dll` assemblies
  (not guessed from docs): melee combat re-derives its real per-swing target from `Pawn_MindState.meleeThreat`
  and the running `Job.targetA` each swing (via `Pawn_MeleeVerbs.TryMeleeAttack`), not from
  `Pawn_MindState.enemyTarget` or a fresh `BestAttackTarget` call — so biasing `BestAttackTarget` alone (T007's
  postfix) can never affect an already-engaged hostile, in vanilla or CE. Fixed by adding, to
  `HediffComp_GuardiansCallEffect` (not a new Harmony patch, and not CE-specific — this fix is identical code
  in both configurations): (1) an activation-time pass that directly sets `mindState.enemyTarget`,
  `mindState.meleeThreat`, and `CurJob.targetA` to the taunting pawn for every qualifying, reachable hostile in
  range already engaged with someone else, and (2) a `CompPostTick`-driven re-assertion of the same every 30
  ticks for the rest of the active taunt window, since a single activation-time correction wasn't sufficient —
  something (suspected but not confirmed to be a CE AI layer) could still pull a hostile back off within a few
  seconds. This was verified working end-to-end in **non-CE** play across four independent fights (a 3-vs-1
  brawl, a sustained multi-activation lock, and a clean disengage-and-retarget out of an existing melee
  engagement) — see the live combat log evidence gathered via a temporary `Patch_Pawn_PostApplyDamage_
  DebugCombatLog` diagnostic patch (added, used, then removed once the investigation concluded).
- **T011 — CE re-verification of the reworked mechanism: CONFIRMED.** Re-enabled Combat Extended and re-ran
  the already-engaged-hostile case (the exact scenario that originally exposed the bug via a CE hostile named
  Volz) against the current build. Direct combat-log evidence: hostile "Kennedy" was hitting colonist
  "Superbass" (`tick=11364`, 2.3 dmg); Guardian's Call activated and yanked it; the very next three connected
  hits all landed on the tattooed pawn Minla instead (`tick=11624`/`11700`/`11776`, 0.9/1.1/0.3 dmg) — real
  redirected damage, not just an attempted yank. The activation-time force-set plus 30-tick periodic
  re-assertion (`mindState.enemyTarget`/`meleeThreat`/`CurJob.targetA`) holds correctly under CE, confirming
  the fix generalizes rather than being a vanilla-only workaround. Combined with the earlier confirmation that
  fresh targeting decisions are already correctly biased under CE (Outcome A, unchanged from before the
  rework), Scenario 5 is fully confirmed: both the "not yet engaged" and "already engaged" cases work
  correctly in both vanilla and CE, with a single implementation (no CE-specific patch needed anywhere in this
  feature).
- **T015/T016 self-audit — PASS, no leaks found.** Re-read `Source/Effects/IProvidesTattooGizmo.cs` and
  `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs` in full: neither references
  `TattooMagic_Hediff_GuardiansCall`, any Guardian's-Call defName, or any Guardian's-Call-specific value key —
  both are genuinely tattoo-agnostic, exactly like `IOnMeleeHitTattooEffect`/
  `Patch_Pawn_PostApplyDamage_TattooOnHit`. `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt`
  does reference `GuardiansCallTauntRegistry` and `HediffComp_GuardiansCallEffect` directly — this is expected
  and contract-compliant, not a leak: `two-path-targeting-contract.md` §3 explicitly documents the registry,
  taunt-range/duration semantics, and nearest-taunter tie-break as Guardian's-Call-specific, with only the
  *pattern* (Postfix-after-vanilla-algorithm; verify-then-conditionally-add-a-CE-patch) being what a future
  targeting tattoo reuses. The three new `Register(...)` calls in `TattooEffectStatPartInstaller` (T012) are,
  per feature 002's own precedent, not a leak either — the installer's doc comment already anticipates future
  tattoos registering additional StatDefs there. A hypothetical future triggered tattoo (Bloodrune, Stormlash,
  Wraithstep, Berserker's Mark) can implement `IProvidesTattooGizmo` on its own comp today with zero changes
  to Guardian's Call's files or the shared patch; a hypothetical future targeting tattoo would write its own
  registry/patch pair against the documented convention rather than extending Guardian's Call's.
- **Not completed this session — require an actual running RimWorld instance and cannot be done by an
  agent without game access**: T008, T009 (the CE-loaded empirical check itself), T010 (contingent on T009),
  T011, T014, T017. All `quickstart.md` scenarios (1–6) still need to be run by a human in Dev Mode per
  Constitution Principle II before this feature can be marked done. T018's build-clean check has already been
  continuously verified after every code change in this session, but should be re-run one final time after any
  post-verification cleanup (e.g. if Scenario 5 requires adding T010's patch).
