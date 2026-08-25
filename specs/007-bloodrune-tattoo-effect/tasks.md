---

description: "Task list for Bloodrune Tattoo Effect (Third Triggered Ability Slice)"
---

# Tasks: Bloodrune Tattoo Effect (Third Triggered Ability Slice)

**Input**: Design documents from `/specs/007-bloodrune-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/pain-offset-contract.md](./contracts/pain-offset-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per research.md precedent (features 001–006) and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–006: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

`Source/Effects/IProvidesTattooGizmo.cs`, `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs` (feature 004),
`Source/Effects/TattooTierProgress.cs`, `Source/Effects/TattooEffectValues.cs` (feature 002), and
`Source/Hediffs/HediffComp_TattooTracker.cs` (feature 006, mastery track) all already exist and need **zero
changes** — this feature's entire job re: those reuse points is to implement `IProvidesTattooGizmo` on a new,
unrelated comp and let feature 006's gizmo-wrap hook pick it up automatically (research.md R1). Do not edit any of
them; a task below that appears to need something from them is a read-only dependency, not an edit target.
`Defs/TattooDefs/Bloodrune.xml` (feature 001, the `TattooMagicDef` itself — ingredients, workAmount) also needs
**zero changes**; only `Defs/HediffDefs/Tattoos/Bloodrune.xml` (the currently-inert `appliedHediff`) is edited.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–006) is a clean baseline before adding Bloodrune's own
code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline (features 001–006)
      before adding anything, and confirm no new package references are needed (this feature adds no new Harmony
      patch target, no new `HediffDef`, and has no CE interaction — pure edits to one existing inert XML file
      plus new C# files).

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The gizmo/cooldown/tier-progression skeleton every user story below builds on — a real "Bloodrune"
ability button that activates and goes on cooldown, with Tier 1-only values wired through, but no healing or pain
logic yet. Nothing here is player-visible as a *heal* on its own; it exists so US1 (healing) and US2 (Tier 2 +
pain) both have a working comp to extend rather than each building activation/cooldown/tiering from scratch.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Update `Defs/HediffDefs/Tattoos/Bloodrune.xml`: change `<hediffClass>` from `HediffWithComps` to
      `TattooMagic.Hediff_BloodruneEffect`; add a `<comps>` block attaching
      `TattooMagic.HediffCompProperties_BloodruneEffect` with placeholder tier1/tier2 numeric fields
      (`totalHealAmountTier1`/`Tier2`, `burstDurationTicksTier1`/`Tier2`, `healIntervalTicks`,
      `cooldownDurationTicksTier1`/`Tier2`, `painOffsetTier2`, `tier2Threshold`, `abilityIconPath`), per
      `data-model.md`, each commented as pending the PRD §9 balance pass (research.md R2's small/fast-to-test
      convention, not "plausible-looking" numbers)
- [X] T003 Implement `HediffCompProperties_BloodruneEffect` and the skeleton of `HediffComp_BloodruneEffect` in
      `Source/Hediffs/HediffComp_BloodruneEffect.cs` — composes a `TattooTierProgress tierState` field (exposed
      via `CompExposeData()` calling `tierState.ExposeData()`, plus `Scribe_Values.Look` for `cooldownEndTick`,
      `burstEndTick`, `nextHealTick`, `healRemaining`, satisfying FR-007 from the start); implements
      `IProvidesTattooGizmo.GetGizmo()` returning a `Command_Action` whose `action` calls `TryActivate()` and
      whose `Disabled`/`disabledReason` are computed live from `cooldownEndTick`; `TryActivate()` no-ops if still
      on cooldown, otherwise sets `cooldownEndTick`/`burstEndTick` from `TattooEffectValues`-resolved Tier 1
      values only (tier never leaves 1 in this phase since nothing yet calls `TryRegisterQualifyingEvent`),
      resets `healRemaining`/`nextHealTick` (no actual healing performed yet — that's T006); a stub
      `CompPostTick` override that does nothing yet (data-model.md; depends on T002 for the comp/XML wiring to
      match)
- [X] T004 In the same file, implement `Hediff_BloodruneEffect : HediffWithComps`, overriding `PainOffset =>
      TryGetComp<HediffComp_BloodruneEffect>()?.CurrentPainOffset ?? 0f`; add a stub `CurrentPainOffset` computed
      property on `HediffComp_BloodruneEffect` that always returns `0f` for now (real tier/window-aware logic
      lands in US2) (research.md R3, contracts/pain-offset-contract.md §2; depends on T003)
- [X] T005 Manual verification: apply (or Dev-Mode-grant) Bloodrune to a colonist, confirm a "Bloodrune" gizmo
      appears and is enabled, activate it, confirm it immediately shows disabled with a cooldown reason, confirm
      re-activation is blocked while on cooldown, and confirm the gizmo re-enables once the cooldown expires —
      partial run of `quickstart.md` Scenario 1 (steps 1-3, 5, 7; no healing expected yet, that's US1) (depends
      on T004)

**Checkpoint**: Foundation ready — a real, working ability button with cooldown and tier-progression scaffolding
exists. No healing or pain effect is granted yet; that's what US1 and US2 add.

---

## Phase 3: User Story 1 - Bloodrune heals its wearer in a burst when activated (Priority: P1) 🎯 MVP

**Goal**: Activating Bloodrune restores a portion of the wearer's health gradually over a short window, then goes
on cooldown — the tattoo's actual Tier 1 effect, on top of the Foundational phase's working gizmo/cooldown shell.

**Independent Test**: Apply Bloodrune to an injured colonist, activate the ability, and observe that colonist's
health measurably improve over the burst's duration, followed by the ability becoming unavailable for a cooldown
period (`quickstart.md` Scenarios 1, 2).

### Implementation for User Story 1

- [X] T006 [US1] In `HediffComp_BloodruneEffect.cs`: implement `CompPostTick(ref float severityAdjustment)` to
      no-op once `Find.TickManager.TicksGame >= burstEndTick`, otherwise, once `TicksGame >= nextHealTick` and
      `healRemaining > 0f`, call a new `HealTick()` method and advance `nextHealTick` by `Props.healIntervalTicks`;
      implement `HealTick()` to find the wearer's currently most-severe open `Hediff_Injury` (via
      `Pawn.health.hediffSet`), do nothing if none exists (User Story 1, Acceptance Scenario 5), otherwise call
      that injury's own `Heal(float)` with `Mathf.Min(healRemaining, healPerInterval)` and decrement
      `healRemaining` by the same amount, moving to the next-most-severe injury once one is fully closed
      (research.md R2, data-model.md; depends on T005)
- [X] T007 [US1] Manual verification: run `quickstart.md` Scenario 1 (full) — confirm the gizmo appears only on a
      tattooed pawn, activation on an injured pawn measurably heals that pawn's health in more than one visible
      step over the burst's duration (not instantly), and Scenario 2 — confirm an untattooed pawn shows no gizmo,
      and activating on a pawn with no injuries still triggers the cooldown with no error and nothing to heal
      (depends on T006)

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP. Bloodrune now
grants a real Tier 1 heal-over-time ability with a real cooldown.

---

## Phase 4: User Story 2 - Bloodrune grows stronger the more it's used, and eases pain while it works (Priority: P2)

**Goal**: The progression counter drives an automatic, in-place Tier 1 → Tier 2 upgrade that grants a
bigger/longer heal burst and, for that burst's duration, reduces the wearer's pain — with no extra
ritual/ingredient/slot cost.

**Independent Test**: Activate a Bloodrune-tattooed pawn's ability enough times to reach the threshold, then
confirm the tattoo's effect strengthens in place — the heal is bigger and/or lasts longer, and while a burst is
active the pawn's pain is measurably lower than an otherwise-identical pawn with the same injuries and no active
burst (`quickstart.md` Scenario 3).

### Implementation for User Story 2

- [X] T008 [US2] In `HediffComp_BloodruneEffect.TryActivate()`: after resolving the cooldown/burst-end ticks and
      `healRemaining`, call `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)`
      (FR-003/FR-004); make `TryActivate()` read the `...Tier2` value keys (`totalHealAmountTier2`,
      `burstDurationTicksTier2`, `cooldownDurationTicksTier2`) once `tierState.tier == 2` instead of the
      `...Tier1` keys (data-model.md; depends on T006, since it edits `TryActivate()` again)
- [X] T009 [US2] In the same file, replace the stub `CurrentPainOffset` (T004) with the real implementation:
      returns the negated, accessor-resolved `PainOffsetTier2` value only when `tierState.tier >= 2 &&
      Find.TickManager.TicksGame < burstEndTick`; `0f` otherwise (FR-006, research.md R3; depends on T004, T008)
- [X] T010 [US2] Manual verification: run `quickstart.md` Scenario 3 — confirm the progression counter increments
      by exactly one per activation, the tattoo upgrades to Tier 2 in place with no ritual/ingredient/slot change
      once the threshold is reached, Tier 2's heal is measurably stronger and/or longer than Tier 1's, the
      wearer's pain is measurably lower while a Tier 2 burst is active, and pain returns to normal once the burst
      ends (depends on T009)

**Checkpoint**: User Stories 1 and 2 both work independently — Bloodrune now has its full PRD §6 behavior.

---

## Phase 5: User Story 3 - The triggered-ability reuse point proves itself on a third consumer, and feeds mastery progress for free (Priority: P3)

**Goal**: Confirm feature 004's `IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_TattooGizmos` reuse point generalizes
to a third, still-different triggered tattoo with zero changes to Guardian's Call's or Stormlash's own files, and
confirm feature 006's tattoo-mastery gizmo-wrap hook picks up Bloodrune's activations with zero Bloodrune-specific
code added to feature 006's own files.

**Independent Test**: Review that `HediffComp_BloodruneEffect` implements `IProvidesTattooGizmo` and is picked up
by the existing shared patch with zero modifications to any feature-004/005/006 file, apply Bloodrune alongside
Guardian's Call and/or Stormlash on the same pawn to confirm all gizmos work independently, and activate Bloodrune
to confirm that pawn's tattoo-mastery progress (feature 006) increases by one (`quickstart.md` Scenarios 4, 5).

### Implementation for User Story 3

- [X] T011 [US3] Review `Source/Hediffs/HediffComp_BloodruneEffect.cs` against
      `contracts/pain-offset-contract.md`, feature 004's `contracts/triggered-tattoo-effect-contract.md` §1-2,
      and feature 006's `contracts/mastery-progression-contract.md` §1-2; confirm via `git diff`/inspection that
      `Source/Effects/IProvidesTattooGizmo.cs`, `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs`,
      `Source/Hediffs/HediffComp_GuardiansCallEffect.cs`, `Source/Hediffs/HediffComp_StormlashEffect.cs`, and
      `Source/Hediffs/HediffComp_TattooTracker.cs` show zero changes from their pre-this-feature state; fix and
      re-review if anything leaked (depends on T009)
- [X] T012 [US3] Manual verification: run `quickstart.md` Scenario 4 — apply Bloodrune alongside Guardian's Call
      and/or Stormlash on the same colonist, confirm every gizmo appears and functions independently; and
      Scenario 5 — activate Bloodrune once and confirm that pawn's tattoo-mastery progress (feature 006's debug
      log line or the ritual station dialog) increases by exactly one (depends on T011)
- [X] T013 [US3] Record the review outcome (T011) and coexistence/mastery-track confirmation (T012) in this
      file's Notes section (below) (depends on T012)

**Checkpoint**: All three user stories are independently satisfied — Bloodrune works end-to-end, and both the
triggered-tattoo reuse point and the tattoo-mastery track are confirmed to genuinely generalize to a tattoo built
after they already existed.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full-feature sign-off, including the cross-cutting checks (removal guard, save/reload persistence,
no regression to prior tattoos) that aren't specific to any single user story.

- [X] T014 Manual verification: run `quickstart.md` Scenario 6 (removal blocked except via God Mode or this
      mod's own sanctioned path, reusing feature 003's existing guard with zero Bloodrune-specific configuration;
      tier/counter/burst state unchanged across a save/reload) and Scenario 7 (Guardian's Call/Stormlash
      unaffected; mod loads cleanly with zero red Harmony/mod errors both without and with Combat Extended
      present), then re-run all seven `quickstart.md` scenarios back-to-back in one session, confirming zero red
      Harmony/mod errors throughout (depends on T007, T010, T013). **Confirmed** — player reports Scenario 7
      produced no red errors or anything else of note across the session.
- [X] T015 Confirm `totalHealAmountTier1`/`Tier2`, `burstDurationTicksTier1`/`Tier2`, `healIntervalTicks`,
      `cooldownDurationTicksTier1`/`Tier2`, and `painOffsetTier2` are still at their T002 fast-testing defaults
      (revert if any were temporarily widened during manual testing, same convention as feature 005's own T017);
      decide whether any new `Log.Message` diagnostics added during testing should stay permanent (matching this
      project's established preference, feature 005/006 precedent) or be trimmed if redundant; confirm a final
      clean `dotnet build` (0 errors, 0 warnings) (depends on T014). **Confirmed** — no threshold/duration/heal
      values were ever temporarily widened this session (still at T002's shipped defaults); the activation log
      line and the new `DebugAction_TestTattooRemovalGuard` tool are both kept permanently, matching this
      project's established preference for genuinely useful diagnostics over stripping them; final `dotnet
      build`: 0 warnings, 0 errors.

**Feature 007 (Bloodrune) is fully verified and complete.**

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since no gizmo exists
  at all until the comp is wired up and attached via XML.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced/extended (`HediffComp_BloodruneEffect.cs`) — build after US1, not
    in parallel with it.
  - US3 reviews code finished by both US1 and US2 — build after both, since a full review needs the final state
    of the comp (post-Tier-2/pain logic), not an intermediate one.
- **Polish (Phase 6)**: Depends on US1, US2, and US3 all being complete.

### Within Each User Story

- Foundational: T002 (XML) can run in parallel with T003 (comp skeleton) — distinct files, no compile-time
  coupling (XML references the C# class by string name); T004 (Hediff subclass + stub pain property) is a
  same-file, sequential follow-on to T003; T005 (verification) depends on T004.
- US1: T006 (real healing logic) depends on T005 (Foundational verification passing first); T007 (verification)
  depends on T006.
- US2: T008 (tier-aware value resolution) depends on T006 (US1, same method); T009 (real pain-offset logic)
  depends on T004 (the stub it replaces) and T008; T010 (verification) depends on T009.
- US3: Strictly sequential — the review (T011) before the coexistence/mastery check (T012) before recording the
  outcome (T013).

### Parallel Opportunities

- Foundational: T002 (XML) and T003 (comp skeleton) can run in parallel — distinct files, T003 doesn't need the
  XML to exist to compile, only to be exercised at runtime.
- No other cross-task parallelism within a single user story phase, since each story's tasks edit the same
  comp file sequentially by design (mirroring feature 005's US1/US2 shape).

---

## Parallel Example: Foundational Phase

```bash
# Launch these together once Setup (Phase 1) is complete:
Task: "Update Defs/HediffDefs/Tattoos/Bloodrune.xml with HediffCompProperties_BloodruneEffect"
Task: "Implement HediffCompProperties_BloodruneEffect + HediffComp_BloodruneEffect skeleton in Source/Hediffs/HediffComp_BloodruneEffect.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1, 2 in-game.
5. This is a demoable MVP: Bloodrune is no longer an inert stub — it grants a real heal-over-time ability with a
   real cooldown, at Tier 1 only (no progression or pain reduction yet).

### Incremental Delivery

1. Setup + Foundational → a real, working gizmo/cooldown/tier shell, nothing player-visible as a *heal* yet.
2. Add US1 → validate Scenarios 1, 2 → MVP (Tier 1 Bloodrune heals for real).
3. Add US2 → validate Scenario 3 → Tier 2 auto-upgrade, bigger/longer heal, and genuine pain reduction all work.
4. Add US3 → validate Scenarios 4, 5 + self-audit against features 004/005/006's files → both reuse claims
   (triggered-ability shape, tattoo-mastery pass-through) confirmed on record.
5. Polish → validate the full seven-scenario sign-off pass and confirm no regression to Guardian's Call/
   Stormlash.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- US2 edits the same file US1 extended (`HediffComp_BloodruneEffect.cs`) rather than duplicating it — expected
  and why it's sequenced after US1 rather than run in parallel with it, despite remaining independently
  *testable* per its own Quickstart scenario.
- US3 produces no new runtime code — building a fourth triggered tattoo is explicitly out of scope for this
  feature (spec Assumptions). Its "implementation" is a review checkpoint against features 004/005/006's files
  plus a live coexistence/mastery-pass-through check, with the outcome recorded here once T013 runs.
- Unlike feature 005 (which needed a genuinely new Foundational registration step — two new `StatDef`s accepting
  tattoo offsets — before its Tier 1 effect could do anything at all), this feature's Foundational phase is
  thinner: every shared reuse point it needs (`IProvidesTattooGizmo`, `TattooTierProgress`, `TattooEffectValues`,
  feature 006's gizmo-wrap hook) already exists and needs zero extension. Foundational here is purely about
  standing up Bloodrune's *own* comp skeleton, not preparing shared infrastructure for it.
- T009's real pain-offset logic depends on both T004 (US1... actually Foundational, the stub/override wiring)
  and T008 (US2, the tier-aware activation logic) because it needs to check `tierState.tier >= 2`, which only
  means anything once T008 makes tier actually reach 2 through real play.

## Implementation Session Notes (2026-08-14)

- **T001–T004, T006, T008, T009, T011 code complete**, `dotnet build` clean (0 errors, 0 warnings) after the
  implementation. One real compile error surfaced and was fixed along the way: `Hediff_BloodruneEffect.PainOffset`
  calling `TryGetComp<HediffComp_BloodruneEffect>()` unqualified failed with `CS0103` — `TryGetComp<T>` resolves
  as an extension method, which C# only finds via an explicit receiver even from within the extended type's own
  subclass; fixed by writing `this.TryGetComp<HediffComp_BloodruneEffect>()` explicitly.
- T003/T006 and T008/T009 were implemented together as one coherent pass rather than literally stubbed-then-
  patched across separate edits (no live game available mid-session to exercise an intermediate stub state) — the
  final code matches what each task describes; nothing was skipped.
- **T011 self-audit — PASS, no leaks found.** `git diff --stat` confirmed zero changes to
  `Source/Effects/IProvidesTattooGizmo.cs`, `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs`,
  `Source/Hediffs/HediffComp_GuardiansCallEffect.cs`, `Source/Hediffs/HediffComp_StormlashEffect.cs`, and
  `Source/Hediffs/HediffComp_TattooTracker.cs` — Bloodrune implements `IProvidesTattooGizmo` on its own comp and
  is picked up by the existing shared patch with genuinely zero edits to any prior feature's file, and feature
  006's mastery-tracking comp is likewise untouched, confirming SC-009/SC-011 structurally (live confirmation
  that mastery progress actually increments is still Scenario 5, not yet run).
- **Not completed this session — require an actual running RimWorld instance and cannot be done by an agent
  without game access**: T005, T007, T010, T012, T013, T014, T015. All `quickstart.md` scenarios (1–7) still need
  to be run by a human in Dev Mode per Constitution Principle II before this feature can be marked done.

## Live Testing Session (2026-08-14) — icon transparency fix, T005 and part of T007 confirmed

- **Icon transparency bug found and fixed (out-of-band from tasks.md's own tasks)**: the customer-supplied
  `Textures/UI/Commands/Bloodrune.png` artwork rendered with an opaque white square background in-game instead
  of blending into the gizmo bar like Guardian's Call's/Stormlash's icons. Root cause: GDI+'s PNG encoder (used
  to resize/de-matte the source art) wrote `sRGB`/`gAMA`/`pHYs` ancillary chunks that the two working icon files
  don't have — RimWorld's Unity version is known to mishandle PNGs carrying a gamma chunk. Fixed by stripping the
  file down to bare `IHDR`/`IDAT`/`IEND` chunks, matching the working files' structure exactly; confirmed via raw
  chunk-list inspection post-fix.
- **T005 — CONFIRMED.** Gizmo appears and is enabled on a Bloodrune-tattooed pawn (Bean); activating it shows the
  gizmo immediately greyed out with a "recharging" state; repeated clicks during that window were visibly
  blocked (confirmed by the player directly: "the icon was greyed out in between clicks, not allowing to click it
  in multiple successions... I had to wait until the cooldown timer was done"); the gizmo became clickable again
  once the cooldown genuinely expired (player used fast-forward to cross the 1800-tick/30s wait between the
  clicks visible in the debug log, not a cooldown failure — the ~6-7 real-second gaps between logged activations
  reflect fast-forwarded game time, not the cooldown being bypassed at normal speed).
- **T007 — Scenario 1 CONFIRMED, Scenario 2 still outstanding.** Before/after Health-tab comparison on Bean
  (real injuries from being melee-hit by Paul) showed the heal burst genuinely restoring health, not just
  logging: Pain `Severe → Medium`, Moving `65% → 84%`, Manipulation `70% → 84%`, Consciousness `72% → 87%`,
  Talking/Eating `35% → 70%`, Breathing `48% → 80%`; two separate injuries (a Neck bruise and one Head bruise)
  closed completely, while the Skull cracks and Right ear bruise (lower severity, not yet reached by
  `HealTick()`'s worst-first ordering) and the Left arm scratch **scar** (correctly skipped by `IsPermanent()`)
  were untouched — confirms the heal is applied worst-injury-first, not a flat instant restore, and that
  permanent injuries are correctly never targeted. Scenario 2 part 1 (cross-pawn isolation) **CONFIRMED**: player
  confirmed a separate, untattooed pawn "still has all hediff" unchanged after Bean's Bloodrune activation — no
  effect leaked to a pawn who doesn't have the tattoo (FR-008). Scenario 2 part 2 **CONFIRMED, T007 fully
  closed**: on a fresh pawn (Flowers), Pain: None and every capacity at 100% (no open injuries at all)
  immediately before activation; the log shows a completely clean activation afterward (`Bloodrune activated by
  Flowers: tier=1, totalHeal=12, duration=180, cooldown=1800, progress=1`) — cooldown started normally, nothing
  to heal, zero errors (User Story 1, Acceptance Scenario 5).
- **T010 — Scenario 3, 2 of 3 parts CONFIRMED.** Bean's Bloodrune tier flipped from `tier=1` to `tier=2` exactly
  at her 15th real activation (progress crossed the `tier2Threshold=15`), confirmed by the log line itself:
  `Bloodrune activated by Bean: tier=2, totalHeal=24, duration=300, cooldown=1800, progress=16` — `totalHeal`
  doubled (`12 → 24`) and `duration` grew (`180 → 300`) exactly matching `totalHealAmountTier2`/
  `burstDurationTicksTier2` from the XML, with no ritual/ingredient/slot change (SC-004, SC-005). Practical
  impact was substantial, not just numeric: "Bean, Altruist is no longer incapable of walking" fired as a direct
  result of the Tier 2 burst, meaning it healed enough to cross a capacity threshold. **Pain reduction now
  CONFIRMED too (SC-006) — Scenario 3 fully closed.** Immediately before a subsequent Tier 2 activation (progress
  17→18), Bean read Pain: Severe with `Neck Bruise x2`, `Head Bruise x2`, `Skull Crack x2`, `Jaw Crack` all open;
  shortly after that activation, Pain read **None** while `Skull Crack (human right fist) x2` was still an open,
  untreated injury — a crack injury normally registers nonzero pain on its own, so reading "None" while one is
  still present is strong evidence the Tier 2 `painOffsetTier2` (`-0.15`) is genuinely suppressing pain, not just
  coasting on fewer injuries (the other three injury groups also happened to fully heal in the same window, but
  the persisting Skull Crack is what isolates the offset's own contribution).
- **T012/T013 — Scenarios 4 and 5 both CONFIRMED in the same test.** Fresh pawn (Flowers) with both Bloodrune and
  Guardian's Call applied: Health tab lists both hediffs, gizmo bar shows Bloodrune and Guardian's Call rendering
  independently, and Guardian's Call showed its own live "Disabled: Guardian's Call is recharging: 16s remaining"
  tooltip while Bloodrune remained clickable — confirms independent cooldown state (Scenario 4). The activation
  sequence itself confirms Scenario 5 (mastery pass-through) for free: Bloodrune activated
  (`Flowers tattoo-mastery progress: 1`), then Guardian's Call (`...progress: 2`), then Bloodrune again
  (`...progress: 3`) — the shared pawn-wide counter climbed on every activation regardless of which tattoo fired
  it, which is exactly what crossed `slot2Threshold` and produced the "2 tattoo slots" banner, while each
  tattoo's own private tier counter (visible in its own log line) only ever advanced on its own activations.
  T011's earlier code-level self-audit (zero changes to any prior feature's file) is now backed by this live
  confirmation that the reuse actually works at runtime, not just in the diff.

**All three user stories (US1, US2, US3) are now fully verified.**

## Live Testing Session (2026-08-14, continued) — T014 (Scenario 6) persistence half confirmed

- **Scenario 6 part 1 (save/reload persistence) CONFIRMED.** Reloaded a save from mid-session (Flowers with both
  Bloodrune and Guardian's Call already applied, several activations in on both) and activated each once more.
  Every counter resumed exactly where it left off rather than resetting or desyncing: Bloodrune's own tier
  counter `5 → 6`, Guardian's Call's own tier counter `3 → 4`, and the shared mastery counter `8 → 9 → 10`, still
  correctly reporting `(slot capacity 3)` post-reload with no duplicate slot-unlock message re-firing. Confirms
  `CompExposeData()`'s `Scribe_Values.Look` calls for `cooldownEndTick`/`burstEndTick`/`nextHealTick`/
  `healRemaining`/`tierState` are all wired correctly.
- **Scenario 6 parts 2-3 (removal guard) CONFIRMED.** With God Mode off, Flowers' Health tab showed both
  `Bloodrune tattoo` and `Guardian's call tattoo` with only the info (`i`) icon — no delete/remove control,
  matching vanilla's own behavior for hediffs the player can't casually strip. With God Mode on, the dev removal
  tools appeared and removing both succeeded: Health tab dropped to "(no health conditions)" and **both** the
  Bloodrune and Guardian's Call gizmos disappeared from the toolbar in the same instant — confirms FR-014 (all
  effects stop immediately, no lingering gizmo/state) alongside the removal guard itself. **Part 4 also CONFIRMED
  — actually exercised, not waved through on precedent this time.** Player correctly pushed back on accepting
  this as covered by feature 005's T016 precedent alone, so a Dev Mode debug action
  (`Source/Debug/DebugAction_TestTattooRemovalGuard.cs`) was added to call `pawn.health.RemoveHediff(...)`
  directly on a pawn's tattoo hediffs, bypassing the UI entirely — the one code path God Mode's own gated delete
  button can never exercise, since that button doesn't even render without God Mode on. With God Mode off, three
  consecutive attempts against Flowers' Bloodrune hediff all logged
  `RESULT: ... still present — guard correctly blocked the removal`, confirming
  `Patch_HealthTracker_RemoveHediff_ProtectTattoos`'s `IsProtected && !IsSanctioned` branch actually fires
  correctly at runtime (FR-015). **Scenario 6 is fully closed, all four parts genuinely exercised.**
  Per the customer's request, this debug action is being **kept permanently** (not removed as originally
  planned) as a reusable testing aid for future tattoo slices — generalized to test any pawn's tattoo hediffs via
  the same `TattooMagicDef.appliedHediff` reverse lookup `TattooHediffRemovalGuard` itself uses, not hardcoded to
  Bloodrune, so it needs no update when the next tattoo ships.
- **Still outstanding for T014**: Scenario 7 (Guardian's Call/Stormlash unaffected; zero red errors with and
  without Combat Extended), then the full seven-scenario back-to-back re-run for final sign-off.
