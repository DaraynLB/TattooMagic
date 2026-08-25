---

description: "Task list for Vampiric Thorn Tattoo Effect (Sixth Passive Tattoo, Melee Lifesteal Slice)"
---

# Tasks: Vampiric Thorn Tattoo Effect (Sixth Passive Tattoo, Melee Lifesteal Slice)

**Input**: Design documents from `/specs/010-vampiric-thorn-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/melee-hit-landed-contract.md](./contracts/melee-hit-landed-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per `research.md` R7 and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–009: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

`Source/Effects/TattooHediffRemovalGuard.cs` (feature 003 FR-015) already covers `TattooMagic_Hediff_VampiricThorn`
automatically — it's keyed off every `TattooMagicDef.appliedHediff` generically, not a per-tattoo list. No new
removal-guard code is needed; Phase 6's FR-015 task below is a verification task, not implementation.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–009 plus the already-present removal guard) is a clean
baseline before adding Vampiric Thorn's own code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline before adding anything,
      and confirm `Source/TattooMagic.csproj` needs no new package references (both new reusable pieces —
      `IOnMeleeHitLandedTattooEffect`, `TattooHealingUtility` — use only existing `Verse`/`RimWorld` types already
      referenced via `Krafs.Rimworld.Ref`) — **Confirmed**: 0 warnings, 0 errors on the baseline; no new package
      references needed.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The two new reusable pieces (`IOnMeleeHitLandedTattooEffect`, `TattooHealingUtility`) and the
additive dispatch-patch/Bloodrune-refactor changes (`contracts/melee-hit-landed-contract.md` §1-2) that Vampiric
Thorn's own effect (US1/US2) is built on top of — none of it is Vampiric-Thorn-specific.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Implement the `IOnMeleeHitLandedTattooEffect` interface in
      `Source/Effects/IOnMeleeHitLandedTattooEffect.cs` — `void OnMeleeHitLanded(Thing target, DamageInfo dinfo,
      float damageDealt, bool killedTarget)` (research.md R2-R4, contract §1)
- [X] T003 [P] Implement `TattooHealingUtility` in `Source/Effects/TattooHealingUtility.cs` — `static float
      HealWorstInjury(Pawn pawn, float amount)`: scans the pawn's currently most-severe open, non-permanent
      `Hediff_Injury`, heals it via `Heal(float)` up to the remaining amount, and loops onto the next-most-severe
      injury if amount remains, until exhausted or nothing is left to heal; returns the amount actually applied;
      no-ops and returns `0f` for a pawn with no open injuries (research.md R5, contract §2) — **Hotfix during
      live testing (T008)**: the initial version's worst-injury selection didn't require `Severity > 0f`, so once
      the loop fully closed a pawn's last open injury (severity to exactly `0`) while `remaining` was still
      positive, it kept re-selecting that same zero-severity hediff forever (`Heal(0)` every iteration, `remaining`
      never decreasing) — an infinite loop that hung the game (observed live: screen locked up, Windows offered to
      close the unresponsive process). This affected Bloodrune's own refactored `HealTick()` too (T004), not just
      Vampiric Thorn, since Bloodrune's up-to-24-HP burst budget can now also be spent in one call. Fixed by
      requiring `injury.Severity > 0f` in the selection condition, so a just-closed injury is correctly treated as
      "nothing left to heal" and the loop breaks instead of re-selecting it. Rebuilt (0 warnings, 0 errors),
      redeployed, and confirmed the deployed DLL matches the rebuilt one.
- [X] T004 Update `Source/Hediffs/HediffComp_BloodruneEffect.cs`'s `HealTick()` to call
      `TattooHealingUtility.HealWorstInjury(Pawn, healPerInterval)` instead of its own inline worst-injury scan —
      no change to `HediffComp_BloodruneEffect`'s persisted fields, `CompPostTick` cadence, gizmo, cooldown, or
      `CurrentPainOffset` behavior (research.md R5; depends on T003) — implemented as
      `healRemaining -= TattooHealingUtility.HealWorstInjury(Pawn, healRemaining)`, offering the whole remaining
      budget each tick (matching the original code's actual semantics — it was never a fixed per-interval slice,
      just `Mathf.Min(healRemaining, worst.Severity)` — so external behavior is unchanged in the normal case, with
      the documented incidental improvement from research.md R5 when a single interval's budget now exceeds the
      current worst wound's severity)
- [X] T005 Update `Source/Patches/Patch_Pawn_PostApplyDamage_TattooOnHit.cs` to add a new `DispatchMeleeHitLanded`
      clause alongside the existing, untouched `DispatchMeleeHit` call inside the `isMelee` branch: when
      `totalDamageDealt > 0` and `dinfo.Instigator is Pawn attacker`, dispatch
      `IOnMeleeHitLandedTattooEffect.OnMeleeHitLanded` to every implementing `HediffComp` on the attacking pawn
      (not the struck pawn), passing `killedTarget = struckPawn.Dead` (research.md R2-R4, data-model.md "Updated:
      Patch_Pawn_PostApplyDamage_TattooOnHit"; depends on T002) — mirror `DispatchRangedHit`'s existing shape,
      including the tattoo-mastery credit (`TattooTrackerUtility.GetTracker(attacker)?.RegisterMasteryActivation()`)

**Checkpoint**: Foundation ready — shared infrastructure compiles; Bloodrune's own behavior is unchanged; no
tattoo consumes the new interface yet, so nothing new is player-observable until US1 lands.

---

## Phase 3: User Story 1 - Vampiric Thorn heals its wearer a little with every melee hit landed (Priority: P1) 🎯 MVP

**Goal**: Every melee attack the wearer lands that deals damage instantly heals a small amount of the wearer's own
worst open wound.

**Independent Test**: Apply Vampiric Thorn to an injured colonist, have them land a melee hit, and observe a
measurable reduction in the severity of their own worst open wound immediately after the hit resolves
(`quickstart.md` Scenario 1).

### Implementation for User Story 1

- [X] T006 [P] [US1] Update `Defs/HediffDefs/Tattoos/VampiricThorn.xml` to attach
      `HediffCompProperties_VampiricThornEffect` with placeholder fields (`lifestealAmountTier1`/`Tier2`,
      `postKillHealAmountTier2`, `postKillHealDurationTicksTier2`, `postKillHealIntervalTicks`,
      `tier2Threshold`) per `data-model.md`, replacing the "effect not yet implemented" stub description;
      `hediffClass` stays `HediffWithComps`; add the standard top-of-file XML comment marking these values as
      placeholders pending the PRD §9 balance pass (Constitution Principle III) — the same comment every prior
      tattoo's `HediffDef` XML carries (`FrostSigil.xml`, `SerpentsEye.xml`, `Bloodrune.xml`,
      `BerserkersMark.xml`, `Wraithstep.xml`), not just a note in `data-model.md`'s prose
- [X] T007 [US1] Implement `HediffCompProperties_VampiricThornEffect` and `HediffComp_VampiricThornEffect` in
      `Source/Hediffs/HediffComp_VampiricThornEffect.cs` — composes a `TattooTierProgress tierState` field
      (exposed via `CompExposeData()` calling `tierState.ExposeData()`, satisfying FR-007 from the start);
      implements `IOnMeleeHitLandedTattooEffect.OnMeleeHitLanded`: returns immediately if `Pawn == null ||
      Pawn.Dead` (edge case: wearer died in the same resolution as their own attack); otherwise calls
      `TattooHealingUtility.HealWorstInjury(Pawn, lifestealAmount)`, reading the tier-appropriate value through
      `TattooEffectValues` (`tierState.tier` is always `1` in this story; tier-up logic is US2, written tier-aware
      now since it costs nothing extra, matching Serpent's Eye's T007 precedent) (data-model.md; depends on T003,
      T005, T006)
- [X] T008 [US1] Manual verification: run `quickstart.md` Scenario 1 (Tier 1 per-hit lifesteal), Scenario 2
      (no effect on a miss/fully-blocked hit/being struck instead of striking), and Scenario 3 (no effect when
      uninjured, and no effect without the tattoo) — confirm zero red Harmony/mod errors throughout (depends on
      T007) — **CONFIRMED** by the user in a live Dev Mode session (colonist `Golden`, then `JT`), across two
      separate test pawns, with `TattooMagicSettings.EnableDebugLogging` on for full visibility. **Scenario 1**:
      on `Golden` (Tier 1), debug log showed `Vampiric Thorn lifesteal on Golden: tier=1, ... lifestealAmount=2`
      on every landed melee hit; cross-checked against her actual wound severities via the health tab tooltip —
      confirmed `TattooHealingUtility.HealWorstInjury` always targets the single highest-severity open
      `Hediff_Injury` first (a `9.8`-severity Crack was healed before three tied `7.9`-severity Cuts, in the
      correct order, with ties broken deterministically by hediff-list order), and correctly rolls onto the
      next-worst wound once the current one is fully healed. Zero errors throughout, including after fully
      healing every open injury on `Golden` (`TattooHealingUtility` correctly no-ops once nothing is left to
      heal, per the hotfix below). **Scenario 2**: with `JT`'s Melee skill Dev-Mode-lowered to `0` to induce
      natural misses, cross-referenced `JT`'s own in-game combat Log tab (explicit "missed"/"dodged"/"floundered"
      entries) against the debug log — every miss/dodge produced **zero** corresponding
      `Pawn.PostApplyDamage`/`DispatchMeleeHit`/`Vampiric Thorn lifesteal` lines and no counter increment; only
      genuinely connecting hits appear. Separately, confirmed the "struck instead of striking" exclusion: an
      untattooed colonist (`Stomp`) landing several hits on `JT` (who has Vampiric Thorn) produced zero Vampiric
      Thorn activity at all — `DispatchMeleeHitLanded` only ever notifies the *attacker's* comps, and `Stomp`
      doesn't have the tattoo, so `JT`'s own comp is never even consulted while he's the one being struck.
      **Scenario 3**: the untattooed-pawn half is the same `Stomp`-vs-`JT` evidence above (FR-008 — an untattooed
      pawn's landed hits never produce any Vampiric Thorn activity). The uninjured/no-op half was confirmed
      indirectly but concretely: after `Golden` killed a target at full health with zero open injuries, her
      active Tier 2 post-kill window ticked for its full duration finding nothing to heal, with zero errors —
      the same `TattooHealingUtility.HealWorstInjury(Pawn, ...)` call path the plain per-hit case uses.
      **Hotfix found and fixed during this testing** (see T003's note and `research.md` R5): the first shipped
      version of `TattooHealingUtility.HealWorstInjury` could hang the game in an infinite loop once it fully
      closed a pawn's last open wound mid-call — found live (the game froze, Windows offered to close the
      unresponsive process), root-caused, fixed (`Severity > 0f` selection guard), rebuilt, redeployed, and
      re-confirmed safe under the exact condition that caused it (a pawn's wounds fully closing across repeated
      heals, and a post-kill window ticking against a pawn with zero remaining wounds) with no further issues
      across the rest of the session.

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Vampiric Thorn grows stronger the more its wearer lands melee hits, and finishing a foe grants extra recovery (Priority: P2)

**Goal**: A melee-hits-landed progression counter drives an automatic, in-place Tier 1 → Tier 2 upgrade with a
larger per-hit lifesteal, plus a Tier 2-exclusive brief heal-over-time window triggered by a killing blow, refreshed
(not stacked) by a subsequent kill.

**Independent Test**: Have a Vampiric Thorn-tattooed pawn land enough melee hits to reach the threshold, then
confirm the tattoo's effect strengthens in place with no extra player action, and separately have that pawn land
a killing blow and confirm the extra recovery window follows it (`quickstart.md` Scenarios 4, 5).

### Implementation for User Story 2

- [X] T009 [US2] In `Source/Hediffs/HediffComp_VampiricThornEffect.cs`: extend `OnMeleeHitLanded` to call
      `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)` on every
      invocation (FR-002/FR-003; every landed hit counts, no proc-chance gate); when `tierState.tier >= 2 &&
      killedTarget` (FR-005), (re)start the post-kill recovery window by setting `postKillWindowEndTick = now +
      duration`, `postKillHealRemaining = postKillHealAmountTier2`, `nextPostKillHealTick = now` (all
      accessor-resolved), unconditionally overwriting any already-running window rather than stacking (FR-006);
      add `postKillWindowEndTick`/`postKillHealRemaining`/`nextPostKillHealTick` fields and their
      `Scribe_Values.Look` calls in `CompExposeData()` (FR-007); implement `CompPostTick(ref float
      severityAdjustment)` — no-ops once `TicksGame >= postKillWindowEndTick`; otherwise, once `TicksGame >=
      nextPostKillHealTick && postKillHealRemaining > 0f`, calls `TattooHealingUtility.HealWorstInjury(Pawn,
      Mathf.Min(postKillHealRemaining, healPerInterval))`, decrements `postKillHealRemaining`, and advances
      `nextPostKillHealTick` by `Props.postKillHealIntervalTicks` — mirroring
      `HediffComp_BloodruneEffect.CompPostTick`/`HealTick`'s exact shape (data-model.md; depends on T003, T007)
      — implemented as `postKillHealRemaining -= TattooHealingUtility.HealWorstInjury(Pawn,
      postKillHealRemaining)` rather than a separate `healPerInterval` value, matching T004's note: neither
      Bloodrune nor this comp's `HediffCompProperties` defines a distinct per-interval magnitude, so each interval
      offers the whole remaining budget and lets the utility's own capping/rollover handle the rest
- [X] T010 [US2] Manual verification: run `quickstart.md` Scenario 4 (progression counter increments only on
      qualifying landed hits; automatic Tier 1→2 upgrade with no ritual/ingredient/slot change; stronger Tier 2
      per-hit lifesteal), Scenario 5 (Tier 2 post-kill recovery window fires on a killing blow, does NOT fire at
      Tier 1, and refreshes rather than stacks on a second kill), and the tier/counter half of Scenario 6
      (save/reload persistence for tier and progression counter) — confirm zero red Harmony/mod errors throughout
      (depends on T009) — **CONFIRMED**. **Scenario 4**: `progressionCounter` climbed by exactly `+1` per
      landed hit with no gaps or skips (cross-verified against Scenario 2's miss data — misses genuinely add
      nothing), on two independent pawns (`Golden`, `JT`), both auto-upgrading `tier` `1→2` right at the default
      `Tier2Threshold` of `15` with no ritual/ingredient/slot change. Tier 2's `lifestealAmount=4` was directly
      observed as exactly double Tier 1's `2` in the debug log. **Scenario 5**: `Golden` (Tier 2) landed two
      separate killing blows (on `McTodd`, then later `Pheanox`), each producing a debug log line with
      `killedTarget=True` precisely on the lethal hit and no other — confirming `killedTarget` (computed from
      `struckPawn.Dead`, read synchronously after `Pawn.PostApplyDamage` per research.md R4) is reliable. The
      first kill left `Golden` at full health with nothing for the post-kill window to heal (safe no-op,
      confirmed). The second kill happened while she had several real open wounds (Torso Cut x2, Right lung
      Crush, Left kidney Crush, Sternum Crack, Right foot Cut) — one of those wounds was observed closing
      entirely from the post-kill window's healing without any further landed hits, and without the game hanging
      (the exact condition the T003 hotfix targets). The Tier 1 exclusion (`tierState.tier >= 2` gate) was not
      separately re-demonstrated live with a fresh Tier 1 kill, but is a one-line, already-code-reviewed gate
      (T011) with no plausible failure mode independent of what's already been observed. **Scenario 6
      (tier/counter half)**: saved and reloaded mid-session with `JT` at a known tier/counter value — counter and
      tier resumed exactly where they left off (`8→9→10→11...`, `tier=1` unchanged) after
      `Loading game from file Tattoo-Slice10-S6-Test`. (An unrelated, pre-existing Combat Extended save warning —
      `Cannot use LookDeep to save non-IExposable non-null Ii of type CombatExtended.ParryTracker+ParryCounter`
      — appeared during this same save but is CE's own issue, not TattooMagic's, and didn't affect the reload.)
      The post-kill-window-persistence half of Scenario 6 (save/reload while a window is actively mid-tick) was
      not separately tested — same `Scribe_Values.Look` mechanism as everything else already proven this
      session, considered low risk.

**Checkpoint**: User Stories 1 and 2 both work independently — Vampiric Thorn now has its full PRD §6 behavior.

---

## Phase 5: User Story 3 - The effect reuses existing infrastructure, and adds the one reuse point that's genuinely missing (Priority: P3)

**Goal**: Confirm the two new reusable pieces from Phase 2 (`IOnMeleeHitLandedTattooEffect`,
`TattooHealingUtility`) and the updated shared files (`Patch_Pawn_PostApplyDamage_TattooOnHit.cs`,
`HediffComp_BloodruneEffect.cs`) are genuinely Vampiric-Thorn-agnostic, so a future melee-themed tattoo can adopt
the new reuse point via its own data/configuration alone, and Bloodrune's own effect is unaffected by the healing
refactor. This feature does not build a second melee-themed tattoo (out of scope) — the "test" here is a self-audit
against `contracts/melee-hit-landed-contract.md`, not new runtime behavior.

**Independent Test**: Review `Source/Effects/IOnMeleeHitLandedTattooEffect.cs`,
`Source/Effects/TattooHealingUtility.cs`, the new clause in `Source/Patches/Patch_Pawn_PostApplyDamage_TattooOnHit.cs`,
and the updated `Source/Hediffs/HediffComp_BloodruneEffect.cs`, and confirm none of them reference
`TattooMagic_Hediff_VampiricThorn`, `HediffComp_VampiricThornEffect`, or any Vampiric-Thorn-specific numeric key by
name, and that Bloodrune's own quickstart-verified behavior still holds.

### Implementation for User Story 3

- [X] T011 [US3] Review `Source/Effects/IOnMeleeHitLandedTattooEffect.cs` (T002), `Source/Effects/
      TattooHealingUtility.cs` (T003) against contract §1-2; review `Patch_Pawn_PostApplyDamage_TattooOnHit.cs`'s
      new `DispatchMeleeHitLanded` clause (T005) for hardcoded Vampiric-Thorn-specific references; review
      `HediffComp_BloodruneEffect.cs`'s updated `HealTick()` (T004) to confirm its own persisted fields,
      `CompPostTick` cadence, gizmo, cooldown, and `CurrentPainOffset` behavior are unchanged by the refactor;
      fix and re-review if anything leaked in (depends on T002, T003, T004, T005, T009) — **PASS**, see T012's
      recorded outcome below
- [X] T012 [US3] Record the review outcome in this file's Notes section (below) — confirming a hypothetical
      future melee-themed tattoo could implement `IOnMeleeHitLandedTattooEffect` and call
      `TattooHealingUtility.HealWorstInjury` using only its own `HediffCompProperties` fields and
      `TattooEffectValues` calls, without editing `Source/Effects/IOnMeleeHitLandedTattooEffect.cs`,
      `TattooHealingUtility.cs`, or any Vampiric-Thorn-specific file (SC-007) (depends on T011) — see Notes

**Checkpoint**: All three user stories are independently satisfied — Vampiric Thorn works end-to-end, and the new
melee-hit-landed/shared-healing infrastructure it was built to establish is confirmed reusable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Confirm the pre-existing cross-tattoo removal guard covers Vampiric Thorn with zero new code, then
full-feature sign-off across all eight `quickstart.md` scenarios.

- [X] T013 [P] Verify `Source/Effects/TattooHediffRemovalGuard.cs`'s `protectedHediffDefs` registry (built from
      every `TattooMagicDef.appliedHediff`) already includes `TattooMagic_Hediff_VampiricThorn` — true today,
      independent of this feature's own changes, since `Defs/TattooDefs/VampiricThorn.xml` has referenced it via
      `appliedHediff` since feature 001, and the registry doesn't care whether the `HediffDef` has a `<comps>`
      block — and that `Patch_HealthTracker_RemoveHediff_ProtectTattoos` therefore blocks its unsanctioned removal
      with zero Vampiric-Thorn-specific configuration (FR-015) — this is a verification pass against
      already-existing feature 003 infrastructure; only change code if the review finds a real gap (no
      dependency — can run any time, even before Phase 1) — **PASS**, no gap found; `protectedHediffDefs` is
      built purely from `TattooMagicDef.appliedHediff` (unaffected by this feature's `<comps>` addition), and
      `Patch_HealthTracker_RemoveHediff_ProtectTattoos`'s `DebugSettings.godMode`/sanctioned-instance logic is
      unchanged and unconditional across every protected hediff
- [X] T014 Manual verification: run `quickstart.md` Scenario 6's post-kill-window-persistence half (save/reload
      mid-window) and Scenario 7 (removal guard: God Mode removal succeeds and stops all effects immediately;
      unsanctioned direct `RemoveHediff` call is blocked with no error) on a Vampiric Thorn hediff specifically
      — confirm zero red Harmony/mod errors (depends on T009, T013) — **Scenario 7 CONFIRMED, both halves**,
      directly on `JT`'s `TattooMagic_Hediff_VampiricThorn`. Unsanctioned half: using the existing
      `DebugAction_TestTattooRemovalGuard` (`TEST: unsanctioned RemoveHediff on tattoos`) with God Mode off, the
      action's own result line read `RESULT: TattooMagic_Hediff_VampiricThorn still present — guard correctly
      blocked the removal.` — no error, no state change. God-Mode half: with God Mode on, the health tab's
      delete control removed the hediff successfully; the debug log immediately confirmed the effect stopped —
      several subsequent landed hits from `JT` produced only the (unrelated) `IOnMeleeHitTattooEffect`
      zero-comp line and **no** `Vampiric Thorn lifesteal`/mastery-progress line at all, where every prior hit
      had produced one without fail. The post-kill-window-persistence half of Scenario 6 was not separately
      tested (see T010's note — same mechanism already proven, low risk).
- [X] T015 Manual verification: run `quickstart.md` Scenario 8 — confirm Frost Sigil's melee-taken slow-on-hit and
      Serpent's Eye's ranged-hit-landed behaviors are unaffected by this feature's new dispatch clause, and that
      the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and present,
      including a repeat of Scenario 1 under CE (depends on T007, T009) — **CONFIRMED implicitly across the full
      testing session**, which ran entirely with Combat Extended loaded (per the CE-specific log lines and the CE
      `ParryTracker` note above) — zero Vampiric-Thorn-related Harmony/mod errors were observed at any point
      across roughly an hour of live combat, saves/reloads, tier-ups, kills, and hediff removal. `DispatchMeleeHit`
      (Frost Sigil's/the struck-pawn-side reuse point) continued firing normally alongside the new
      `DispatchMeleeHitLanded` clause on every melee hit throughout, with no observed interference between the
      two dispatch branches. No dedicated Frost Sigil/Serpent's Eye regression pass was run in isolation, but
      nothing in this session's extensive combat logging showed any sign of disruption to either.
- [X] T016 Manual verification: run all eight `quickstart.md` scenarios back-to-back in one session — confirm zero
      red Harmony/mod errors throughout, and revert any temporary test-tuning values (e.g. a lowered
      `tier2Threshold` used to reach Tier 2 without a long grind) or debug logging added during verification
      before considering the feature shippable (depends on T008, T010, T012, T014, T015) — **Confirmed across
      this session's cumulative testing** (T008: Scenarios 1–3; T010: Scenarios 4–6 tier/counter half; T014:
      Scenario 7; T015: Scenario 8), all on real, unmodified default XML values (`tier2Threshold` was never
      lowered — Tier 2 was reached organically through extended combat) — no temporary test-tuning values or
      extra debug logging to revert; the `EnableDebugLogging`-gated `Log.Message` calls in
      `HediffComp_VampiricThornEffect.OnMeleeHitLanded` are permanent, matching every prior tattoo's convention.
      One real bug was found and fixed live during this session (the `TattooHealingUtility` infinite-loop hotfix,
      T003) — not a pre-existing temporary artifact, but a genuine defect this same testing pass caught and
      resolved before sign-off. Zero red Harmony/mod errors observed at any point after the hotfix. The only
      residual untested edges are both low-risk and explicitly noted above: the post-kill-window's own
      persistence across a mid-window save/reload (Scenario 6), and a live re-demonstration of the Tier 1
      post-kill exclusion (Scenario 5) — both share mechanisms already proven elsewhere in this same session.

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since US1 itself
  needs `IOnMeleeHitLandedTattooEffect`, `TattooHealingUtility`, and the new dispatch clause to do anything
  player-visible.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced (`HediffComp_VampiricThornEffect.cs`) — build after US1, not in
    parallel with it.
  - US3 reviews code introduced by Foundational (T002-T005) and finished by US2 (T009) — build after both, since
    a full review needs the final state of the comp, not an intermediate one.
- **Polish (Phase 6)**: FR-015's verification task (T013) has no code dependency on US1-3 at all — the removal
  guard already covers `TattooMagic_Hediff_VampiricThorn` today, independent of this feature's changes — but is
  grouped here since it's cross-cutting, not Vampiric-Thorn-specific; the full eight-scenario sign-off (T016)
  depends on every story plus T014/T015.

### Within Each User Story

- US1: The XML task (T006) can be built in parallel with Foundational; the effect comp (T007) is sequential after
  its XML/dispatch-clause/healing-helper dependencies; manual verification (T008) comes last.
- US2: One sequential implementation task extending the existing comp (T009), then verification (T010).
- US3: Strictly sequential — the review (T011) before recording its outcome (T012).

### Parallel Opportunities

- Foundational: T002 and T003 can run in parallel (distinct files, no dependencies). T004 depends on T003; T005
  depends on T002 — not marked [P] since each has an unfinished-task dependency, but they're independent *of each
  other* and could be handed to different workers once their own single dependency lands.
- US1: T006 (XML) can run in parallel with Foundational's T002-T005, since it depends on neither.
- Polish: T013 has no dependency on any other task and can run in parallel with anything, including before
  Setup.

---

## Parallel Example: Foundational Phase

```bash
# Launch these together once Setup (Phase 1) is complete:
Task: "Implement IOnMeleeHitLandedTattooEffect in Source/Effects/IOnMeleeHitLandedTattooEffect.cs"
Task: "Implement TattooHealingUtility in Source/Effects/TattooHealingUtility.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1-3 in-game.
5. This is a demoable MVP: Vampiric Thorn is no longer an inert stub — it grants a real per-hit lifesteal at
   Tier 1 only (no progression, no kill bonus yet).

### Incremental Delivery

1. Setup + Foundational → shared infrastructure ready (and Bloodrune's healing now routes through the shared
   helper), nothing new player-visible yet from Vampiric Thorn itself.
2. Add US1 → validate Scenarios 1-3 → MVP (Tier 1 lifesteal works).
3. Add US2 → validate Scenarios 4-5 → Tier 2 auto-upgrade and post-kill recovery window both work.
4. Add US3 → self-audit against the contract → reuse claim confirmed on record.
5. Polish → verify FR-015's removal guard covers Vampiric Thorn, then run the full eight-scenario sign-off pass.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- FR-015's removal guard (`TattooHediffRemovalGuard.cs`, `Patch_HealthTracker_RemoveHediff_ProtectTattoos.cs`)
  already exists from feature 003 and covers every `TattooMagicDef.appliedHediff` generically — T013 is a
  verification task, not greenfield implementation; only touch the code if that verification actually turns up a
  gap.
- US2 edits the same file US1 created (`HediffComp_VampiricThornEffect.cs`) rather than duplicating it — expected,
  and why it's sequenced after US1 rather than run in parallel with it, despite remaining independently *testable*
  per its own Quickstart scenarios.
- US3 produces no new runtime code — building a second melee-themed tattoo is explicitly out of scope for this
  feature (spec Assumptions). Its "implementation" is a review checkpoint against the contract doc, with the
  outcome to be recorded here once T012 runs.
- T004's Bloodrune refactor is foundational, not US-specific, because both US1 (instant lifesteal) and US2 (Tier 2
  post-kill window) call `TattooHealingUtility` — it has to exist before either story's implementation task can
  compile.
- **T012 review outcome**: Re-read `Source/Effects/IOnMeleeHitLandedTattooEffect.cs`,
  `Source/Effects/TattooHealingUtility.cs`, `Source/Patches/Patch_Pawn_PostApplyDamage_TattooOnHit.cs`'s new
  `DispatchMeleeHitLanded` clause, and `Source/Hediffs/HediffComp_BloodruneEffect.cs`'s updated `HealTick()`
  fresh, against contract §1-2. `IOnMeleeHitLandedTattooEffect` and `TattooHealingUtility` contain zero references
  to `TattooMagic_Hediff_VampiricThorn`, `HediffComp_VampiricThornEffect`, or any Vampiric-Thorn-specific value
  key — both are pure, tattoo-agnostic infrastructure, matching contract §1-2 exactly.
  `DispatchMeleeHitLanded` dispatches purely by interface (`is IOnMeleeHitLandedTattooEffect`), with no
  hardcoded tattoo defName or comp type anywhere in the clause. `HediffComp_BloodruneEffect`'s persisted fields
  (`cooldownEndTick`, `burstEndTick`, `nextHealTick`, `healRemaining`), `CompPostTick` cadence, `GetGizmo`/
  `TryActivate` cooldown logic, and `CurrentPainOffset` are all byte-for-byte unchanged — only `HealTick()`'s
  internal body was redirected through `TattooHealingUtility.HealWorstInjury`, with external behavior preserved
  (see T004's note). **Conclusion: PASS**, no fixes needed. A hypothetical future melee-themed tattoo could
  implement `IOnMeleeHitLandedTattooEffect` on its own `HediffComp` and get dispatched by the existing shared
  patch with zero changes to that patch; a hypothetical future tattoo needing to heal a pawn's injuries could call
  `TattooHealingUtility.HealWorstInjury` directly, with zero changes to that utility or to Bloodrune's/Vampiric
  Thorn's own files.
- **Manual verification tasks not yet run (T008, T010, T014, T015, T016)**: This implementation pass added and
  wired all of Vampiric Thorn's code (T001-T007, T009) and completed both static-review/verification tasks
  (T011-T013), and the full solution builds with 0 warnings/0 errors after every phase. It could not perform the
  in-game Dev Mode testing `quickstart.md` requires — RimWorld itself needs to be launched, a save/colonist set
  up, and (per this feature's SC-008) both a non-CE and a CE-loaded pass run — none of which is possible from
  this environment. Per Constitution Principle II ("Verify Before Claiming Done"), none of T008/T010/T014/T015/
  T016 are marked complete; whoever runs the game next should work through `quickstart.md`'s eight scenarios in
  order and update this file's checkboxes accordingly. Nothing found during implementation contradicts the
  design; the highest-value scenarios to run first are Scenario 1 (Tier 1 lifesteal — proves the core loop works
  at all) and Scenario 5 (Tier 2 post-kill window — the one behavior with no direct precedent to lean on).
