---

description: "Task list for Soul Tether Tattoo Effect (First Pawn/Corpse-Targeted Ability + First Doctor/Patient-Split Stat Buff)"
---

# Tasks: Soul Tether Tattoo Effect (First Pawn/Corpse-Targeted Ability + First Doctor/Patient-Split Stat Buff)

**Input**: Design documents from `/specs/017-soul-tether-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/pull-and-apply-effect-contract.md](./contracts/pull-and-apply-effect-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per research.md precedent (features 001–016) and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–016: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

`Source/Effects/IProvidesTattooGizmo.cs`, `Source/Effects/IHandlesOwnMasteryProgress.cs` (feature 008),
`Source/Effects/TattooTierProgress.cs`, `Source/Effects/TattooEffectValues.cs` (features 002/004),
`Source/Effects/StatPart_TattooEffectOffset.cs` / `TattooEffectStatPartInstaller.cs` (feature 002),
`Source/Hediffs/TattooTrackerUtility.cs` (feature 006), and `Source/Hediffs/HediffComp_FrostSigilEffect.cs`
(feature 002, the cross-pawn transient-hediff-grant precedent, research.md R6) all already exist and need **zero
changes** except `TattooEffectStatPartInstaller.cs`, which needs exactly **one new `Register(...)` line appended**
(T013) — its existing registrations and every current consumer are untouched. `Defs/TattooDefs/SoulTether.xml`,
`Defs/HediffDefs/Tattoos/SoulTether.xml`, and `Defs/HediffDefs/SoulTetherTendBuff.xml` are all currently absent and
created fresh by this feature. This is this mod's **first pawn/corpse-targeted** ability (every prior targeted
tattoo targets a ground cell, feature 008/016) and its **first doctor/patient-split stat buff** (every prior stat
offset is self-only) — research.md R1–R6 document why the existing self-targeted/self-buff patterns don't
mechanically fit and what small, additive extensions are needed instead.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–016) is a clean baseline before adding Soul Tether's own
code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline (features 001–016) before
      adding anything; confirm no new package reference is needed — this feature's new API surface
      (`RimWorld.TargetingParameters` with `canTargetPawns`/`canTargetCorpses`, `RimWorld.TendUtility`,
      `RimWorld.JobDefOf.TendPatient`, `RimWorld.StatDefOf.MedicalTendSpeed`) is all already part of the referenced
      RimWorld assembly, confirmed present via research.md's decompile of the actual installed
      `Assembly-CSharp.dll`.
      **CONFIRMED** — baseline built clean (0 errors, 0 warnings) before any new file was added.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The gizmo/cooldown/tier-progression/mastery-opt-out skeleton every user story below builds on — a real
"Soul Tether" ability button that activates (with a stub, non-targeted action) and correctly opts out of the shared
click-time mastery wrap, but pulls nothing and grants no buff yet.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Create `Defs/HediffDefs/Tattoos/SoulTether.xml`: `TattooMagic_Hediff_SoulTether`, `hediffClass =
      HediffWithComps`, `<comps>` block attaching `TattooMagic.HediffCompProperties_SoulTetherEffect` with
      placeholder `rangeTier1`/`rangeTier2`, `cooldownDurationTicks`, `tier2Threshold`,
      `tendQualityBonusTier1`/`tendQualityBonusTier2`, `tendSpeedBonusTier1`/`tendSpeedBonusTier2`,
      `tendBuffDurationTicks`, and `abilityIconPath` fields per `data-model.md`'s field table — small/fast-to-test
      values, not "plausible-looking" ones (feature 006 research.md R8 convention).
      **DONE** — created with all fields per data-model.md.
- [X] T003 [P] Create `Defs/TattooDefs/SoulTether.xml`: `TattooMagicDef` `TattooMagic_SoulTether` (`tattooType =
      Triggered`, `appliedHediff = TattooMagic_Hediff_SoulTether`, standard ingredients/`workAmount` shape) —
      mirrors `Defs/TattooDefs/BastionWard.xml` exactly (data-model.md).
      **DONE**.
- [X] T004 [P] Create `Defs/HediffDefs/SoulTetherTendBuff.xml`: `TattooMagic_Hediff_SoulTetherTendBuff`,
      `hediffClass = HediffWithComps`, `isBad = false`, `<comps>` carrying `HediffCompProperties_Disappears`
      (`showRemainingTime = true`, no fixed `disappearsAfterTicks` — set at grant time) and
      `TattooMagic.HediffCompProperties_SoulTetherTendBuff` (no XML fields) — mirrors
      `Defs/HediffDefs/FrostSigilSlow.xml`'s shape (research.md R6).
      **DONE**.
- [X] T005 [P] Add `public static HediffDef TattooMagic_Hediff_SoulTetherTendBuff;` to
      `Source/Defs/TattooMagicDefOf.cs` — the tend-buff entry is needed because `SoulTetherTendBuffUtility` (US1)
      references it directly by static field. **No `TattooMagic_Hediff_SoulTether` entry was added** — like every
      prior triggered tattoo, `HediffComp_SoulTetherEffect` resolves its own scope via `parent.def.defName` and
      never needs a static reference to its own hediff def, mirroring Bastion Ward's own T004 finding.
- [X] T006 Implement `HediffCompProperties_SoulTetherEffect` and a skeleton `HediffComp_SoulTetherEffect` in
      `Source/Hediffs/HediffComp_SoulTetherEffect.cs` — composes a `TattooTierProgress tierState` field (exposed
      via `CompExposeData()` calling `tierState.ExposeData()`, plus `Scribe_Values.Look` for `cooldownEndTick`,
      satisfying FR-023 from the start); implements `IProvidesTattooGizmo.GetGizmo()` returning a `Command_Action`
      (icon from `abilityIconPath`, label "Soul Tether") whose `Disabled`/`disabledReason` are computed live from
      `cooldownEndTick`, and whose `action` is a stub `Activate()` that immediately sets `cooldownEndTick` from
      `cooldownDurationTicks`, calls `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold",
      Props.tier2Threshold)`, and calls `TattooTrackerUtility.GetTracker(Pawn)?.RegisterMasteryActivation()` — **no
      targeting session, no pull, and no buff grant yet**, purely to prove the gizmo/cooldown/tier/mastery-opt-out
      plumbing in isolation; implements `IHandlesOwnMasteryProgress` (the existing feature 008 marker interface,
      reused unmodified) (depends on T002, T005).
      **Built together with T010/T011/T015/T016/T018 in one coherent pass** (`Source/Hediffs/
      HediffComp_SoulTetherEffect.cs`), not literally stubbed-then-patched — no live game available this session
      to exercise an intermediate stub state, same precedent as Bastion Ward's own T005. The final file matches
      this task's description plus every later task's, combined; nothing was skipped.
- [X] T007 Manual verification: apply (or Dev-Mode-grant) Soul Tether to a colonist, confirm a "Soul Tether" gizmo
      appears and is enabled, click it, confirm it immediately shows disabled with a cooldown reason (no targeting
      prompt and no pull/buff expected yet — that's US1), confirm re-activation is blocked while on cooldown, and
      confirm the gizmo re-enables once the cooldown expires (depends on T006).
      **DONE (2026-10-21, RimWorld 1.7.4985, via T014's run)** — the skeleton stub state never existed (see T006),
      so this was checked against the final build: the gizmo appeared enabled on a Dev-Mode-granted colonist, and
      a second activation on the same caster went through only after the 1800-tick cooldown had passed (log:
      `resolvesAt` 1517 → 3400, progress 1 → 2). The explicit "blocked while still cooling down" check is
      re-run in full as part of T019 (Scenario 3).

**Checkpoint**: Foundation ready — a real, working ability button with cooldown, tier-progression scaffolding, and
correct mastery opt-out wiring exists. No targeting, pull, or buff is granted yet; that's what US1 adds.

---

## Phase 3: User Story 1 - Pulling a downed ally to safety (Priority: P1) 🎯 MVP

**Goal**: Activating Soul Tether opens a range-capped pawn/corpse targeter (friendly-only, live range-ring preview
and reject-reason feedback); confirming a valid living target instantly relocates them next to the caster and
grants a flat, temporary tend speed/quality bonus.

**Independent Test**: Apply Soul Tether to a colonist, provoke a separate colonist into being downed at range,
activate the ability targeting the downed colonist, and confirm they instantly appear next to the caster with the
tend speed/quality buff active (`quickstart.md` Scenario 1).

### Implementation for User Story 1

- [X] T008 [P] [US1] Implement `HediffCompProperties_SoulTetherTendBuff` and `HediffComp_SoulTetherTendBuff` in
      `Source/Hediffs/HediffComp_SoulTetherTendBuff.cs`: `qualityBonus`/`speedBonus` float fields, a `Configure
      (float quality, float speed)` method, and `CompExposeData()` persisting both (data-model.md).
      **DONE**.
- [X] T009 [P] [US1] Implement `SoulTetherTendBuffUtility` in `Source/Effects/SoulTetherTendBuffUtility.cs`:
      `GrantOrRefresh(Pawn patient, float qualityBonus, float speedBonus, int durationTicks)` (reuses an existing
      `TattooMagic_Hediff_SoulTetherTendBuff` instance on the patient if present, refreshing its `Configure` values
      and its `HediffComp_Disappears` duration; otherwise grants a new one via `HediffMaker.MakeHediff` +
      `patient.health.AddHediff`, mirroring `HediffComp_FrostSigilEffect.GrantSlow`'s exact shape, research.md R6),
      `GetActiveQualityBonus(Pawn patient)`, and `GetActiveSpeedBonus(Pawn patient)` (both null-safe, returning
      `0f` for a patient with no active buff hediff).
      **DONE**.
- [X] T010 [US1] Implement targeting in `HediffComp_SoulTetherEffect.cs`: `BeginTargetSelection()` opens
      `Find.Targeter.BeginTargeting` with `TargetingParameters { canTargetLocations = false, canTargetPawns = true,
      canTargetSelf = false, neverTargetHostileFaction = true }` (corpse-targeting added in US2, T015 — deliberately
      left off here so US1 is independently testable against living targets only), `targetValidator: IsValidTarget`,
      `onGuiAction: DrawRejectReasonLabel`, `onUpdateAction: DrawRangeRing`; `IsValidTarget`/`GetRejectReason` per
      data-model.md (friendly `Pawn`, on caster's map, not self, within `CurrentTierRange`); `DrawRangeRing` calls
      `GenDraw.DrawRadiusRing(Pawn.Position, CurrentTierRange)` (depends on T006).
      **DONE, built together with T015's `canTargetCorpses`/`InnerPawn`-aware check from the start** (single
      coherent pass, per T006's note) rather than phased separately — `GetRejectReason` already resolves
      `effectivePawn = target.Thing as Pawn ?? (target.Thing as Corpse)?.InnerPawn` per research.md R2. US1's own
      independent testability (Scenario 1, living targets only) is unaffected — a corpse simply also happens to
      already work, verified separately in Scenario 2 (T017).
- [X] T011 [US1] Implement `OnTargetConfirmed(LocalTargetInfo target)` in `HediffComp_SoulTetherEffect.cs`,
      replacing T006's stub `Activate()`: no-ops if still on cooldown; finds a walkable cell adjacent to the caster
      (`CellFinder.TryFindRandomCellNear(Pawn.Position, Pawn.Map, 2, c => c.Standable(Pawn.Map), out IntVec3
      destination)`, falling back to `Pawn.Position` if none found, per FR-007); relocates the target
      (`target.Thing.Position = destination; (target.Thing as Pawn)?.Notify_Teleported();`, research.md R3); grants
      the tend buff via `SoulTetherTendBuffUtility.GrantOrRefresh` using the current tier's
      `tendQualityBonusTier1/2`/`tendSpeedBonusTier1/2`/`tendBuffDurationTicks` resolved through
      `TattooEffectValues`; sets `cooldownEndTick`; calls `tierState.TryRegisterQualifyingEvent(...)`; calls
      `TattooTrackerUtility.GetTracker(Pawn)?.RegisterMasteryActivation()` (depends on T008, T009, T010).
      **DONE**, also tier-aware from the start (T018 folded in — see below) and already skips the tend-buff grant
      for a `Corpse` target (T016 folded in) via `if (targetThing is Pawn pulledPawn)`.
- [X] T012 [P] [US1] Implement `Patch_TendUtility_CalculateBaseTendQuality_SoulTetherBuff` in
      `Source/Patches/Patch_TendUtility_CalculateBaseTendQuality_SoulTetherBuff.cs`: a Harmony postfix on
      `TendUtility.CalculateBaseTendQuality(Pawn doctor, Pawn patient, ThingDef medicine)` adding
      `SoulTetherTendBuffUtility.GetActiveQualityBonus(patient)` to `__result` (research.md R4) (depends on T009).
      **DONE**.
- [X] T013 [P] [US1] Implement `StatPart_SoulTetherTendSpeedBuff` in
      `Source/Effects/StatPart_SoulTetherTendSpeedBuff.cs` per data-model.md (`req.Thing` as the doctor; reads
      `doctor.CurJob`, checks `job.def == JobDefOf.TendPatient`, resolves the patient via
      `job.GetTarget(TargetIndex.A).Pawn`, adds `SoulTetherTendBuffUtility.GetActiveSpeedBonus(patient)`); register
      it on `StatDefOf.MedicalTendSpeed` with one new `Register(StatDefOf.MedicalTendSpeed);` line in
      `Source/Effects/TattooEffectStatPartInstaller.cs` (research.md R5) (depends on T009).
      **DONE** — registered via a new `RegisterSoulTetherTendSpeedBuff` helper (kept separate from the existing
      generic `Register`, since this StatPart isn't `StatPart_TattooEffectOffset`), called from the installer's
      static constructor.
- [X] T014 [US1] Manual verification: run `quickstart.md` Scenario 1 in full — targeting rejects self/hostile/
      out-of-range candidates, accepts a downed friendly pawn, the pull relocates them adjacent to the caster, the
      tend-buff hediff appears with a visible countdown, a tend on the buffed pawn shows measurably higher quality
      and completes measurably faster than an unbuffed baseline, and the buff correctly expires (depends on T011,
      T012, T013).
      **DONE (2026-10-21, RimWorld 1.7.4985, dual-build `net10.0` DLL, Combat Extended loaded)** — live-tested
      by the user, with the log checked alongside:
      - Targeting: self, hostile, and >10-cell friendlies were rejected with reasons; a downed friendly within
        range was accepted.
      - Pull: activation → `pull resolved` 0.32 s apart (20 ticks, as designed), with the target placed next to
        the caster and `tendBuffGranted=True`; "soul-tethered" showed in the Health tab with a countdown.
      - Speed: the doctor's Medical tend speed breakdown showed `Soul Tether: +50%` (76% → 126%) only while
        tending the buffed patient, and 76% with no Soul Tether line before the pull and after expiry.
      - Quality: the log showed `tend-quality bonus of 0.15 applied` on each of the buffed patient's three
        tends (base 0.24 → 0.39). A tend after expiry logged no bonus. Quality is checked through the log rather
        than the stat card, because the bonus is applied per patient inside
        `TendUtility.CalculateBaseTendQuality` and the doctor's Medical tend quality stat doesn't change.
      - Expiry: the hediff disappeared from the Health tab when its countdown ended.

**Checkpoint**: MVP complete — Soul Tether pulls a chosen living friendly pawn to the caster and grants a real,
measurable tend speed/quality bonus, at Tier 1.

---

## Phase 4: User Story 2 - Recovering a dead colonist's body (Priority: P2)

**Goal**: The same ability also accepts a friendly pawn's corpse as a valid target, pulling it to the caster
without attempting to grant a tend buff, while continuing to correctly reject a hostile faction's corpse.

**Independent Test**: Let a colonist die and become a corpse, activate Soul Tether targeting that corpse, and
confirm the corpse is relocated next to the caster with no tend buff and no errors (`quickstart.md` Scenario 2).

### Implementation for User Story 2

- [X] T015 [US2] Extend `HediffComp_SoulTetherEffect`'s `TargetingParameters` (T010) to add `canTargetCorpses =
      true`, and extend `GetRejectReason`/`IsValidTarget` to resolve friendliness via research.md R2's
      `InnerPawn`-aware check: `Pawn effectivePawn = target.Thing as Pawn ?? (target.Thing as Corpse)?.InnerPawn;`
      then `!GenHostility.HostileTo(effectivePawn, Pawn)` — **never** `GenHostility.HostileTo(target.Thing, Pawn)`
      called directly on a bare `Corpse`, which research.md R2 confirms always evaluates as "not hostile" regardless
      of the corpse's actual original faction (depends on T010).
      **DONE** (built as part of T010's single coherent pass — see that task's note).
- [X] T016 [US2] Extend `OnTargetConfirmed` (T011) to only call `SoulTetherTendBuffUtility.GrantOrRefresh` when
      `target.Thing is Pawn` — i.e. skip the tend-buff grant entirely when the relocated target is a `Corpse`
      (FR-011); the relocation step itself (T011's `Position`/`Notify_Teleported()` logic) already works
      identically for a `Corpse` per research.md R3 and needs no change (depends on T011, T015).
      **DONE** (built as part of T011's single coherent pass — see that task's note).
- [ ] T017 [US2] Manual verification: run `quickstart.md` Scenario 2 in full — a friendly corpse is selectable and
      pulls correctly with no buff granted and no errors; a hostile-faction corpse is correctly rejected as a
      target, confirming the `InnerPawn`-aware check (T015) actually matters and isn't a no-op (depends on T016).
      **Not run** — pending the user's own in-game testing.

**Checkpoint**: Soul Tether now handles both living friendly pawns and friendly corpses correctly, with hostile
corpses correctly excluded.

---

## Phase 5: User Story 3 - Soul Tether grows stronger the more it's used (Priority: P3)

**Goal**: The tattoo automatically upgrades from Tier 1 to Tier 2 after enough activations, at which point max pull
range is longer and the granted tend buff is stronger, with the pull remaining strictly single-target.

**Independent Test**: Activate a Soul Tether-tattooed pawn's ability enough times to reach the Tier 2 threshold,
then confirm the maximum pull range is measurably larger on the next cast and the tend buff applied is measurably
stronger than at Tier 1 (`quickstart.md` Scenario 3).

### Implementation for User Story 3

- [X] T018 [US3] Confirm (and finalize if not already correct from T010/T011) that `CurrentTierRange` and the
      tend-bonus resolution in `OnTargetConfirmed` both branch on `tierState.tier >= 2` to select
      `rangeTier2`/`tendQualityBonusTier2`/`tendSpeedBonusTier2` over their Tier 1 counterparts via
      `TattooEffectValues`, exactly mirroring `HediffComp_WraithstepEffect.Range`'s and
      `HediffComp_BastionWardEffect.CurrentTierRadius`'s own tier-branching pattern — writing T010/T011 tier-aware
      from the start (per that established precedent) should make this task a verification-only pass, not new
      logic (depends on T011, T016).
      **CONFIRMED by code review** — `CurrentTierRange` and both bonus resolutions in `OnTargetConfirmed` all
      branch on `tierState.tier >= 2` exactly as required; no further change needed.
- [ ] T019 [US3] Manual verification: run `quickstart.md` Scenario 3 in full — cooldown blocks immediate
      reactivation, repeated successful activations increment the progression counter and trigger the Tier 1→2
      upgrade with no new ritual/ingredient/slot change, Tier 2's range is measurably longer, Tier 2's tend buff is
      measurably stronger, and a Tier 2 cast still affects exactly one target (depends on T018).
      **Not run** — pending the user's own in-game testing.

**Checkpoint**: All three user stories complete and independently verified — Soul Tether is feature-complete per
spec.md.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Edge-case coverage, save/reload and Combat Extended verification, and final cleanup.

- [ ] T020 [P] Manual verification: run `quickstart.md` Scenario 4 in full — off-map targets are unselectable,
      pulling an already-adjacent target still grants the buff, an in-progress job/mental state/bleeding status
      survives the pull unchanged besides position, removing the caster's Soul Tether hediff stops future casts
      without retroactively affecting an already-granted buff elsewhere (FR-024), and re-pulling an already-buffed
      target refreshes rather than stacks the buff hediff (depends on T019).
      **Not run** — pending the user's own in-game testing.
- [ ] T021 [P] Manual verification: run `quickstart.md` Scenario 5 in full — the caster's tier/progression-counter
      and a target's active tend-buff remaining duration both survive a save/reload unchanged (FR-023, SC-008);
      with Combat Extended loaded, the full pull-and-buff flow still works with zero errors, and tending an
      unbuffed pawn (the overwhelming majority case) still produces vanilla-identical quality/speed — confirming
      the two new patches (T012, T013) are truly additive-only (depends on T019).
      **Not run** — pending the user's own in-game testing (and access to a CE-loaded save).
- [ ] T022 Confirm the existing cross-tattoo removal guard (feature 003) protects the Soul Tether hediff with zero
      additional per-tattoo configuration (FR-025), via `DebugAction_TestTattooRemovalGuard` (feature 007
      precedent) — with God Mode off, a direct `RemoveHediff` call is blocked; with God Mode on, it succeeds
      (depends on T021).
      **Not run live**, but confirmed by code inspection: the removal guard (feature 003) is registered generically
      against every tattoo-applied `HediffWithComps`, with no per-def opt-in list to add `TattooMagic_Hediff_
      SoulTether` to — matching every other tattoo's own zero-configuration protection. Live confirmation still
      pending.
- [ ] T023 Confirm every placeholder value in `Defs/HediffDefs/Tattoos/SoulTether.xml` is still at its T002
      fast-testing default (revert if any were temporarily widened during manual testing, per features
      005/007/008/016 precedent); confirm a final clean `dotnet build` (0 errors, 0 warnings) (depends on T022).
      **Build confirmed clean** (0 errors, 0 warnings) with all placeholder values unchanged from T002 (no live
      testing occurred this session to have widened any of them). Full task completion still depends on T020-T022's
      live verification.
- [X] T024 [P] Implement the pull VFX/SFX addendum (spec.md Clarifications, session 2026-09-19; research.md R9;
      FR-007a): add `pullEffectDurationTicks` to `HediffCompProperties_SoulTetherEffect`
      (`Defs/HediffDefs/Tattoos/SoulTether.xml`, placeholder `20`); add pending-pull fields (`pendingTarget`,
      `pendingDestination`, `pullResolveTick`, `pendingTendQualityBonus`, `pendingTendSpeedBonus`,
      `pendingTendBuffDurationTicks`) and `Scribe` them in `CompExposeData`; change `OnTargetConfirmed` to play the
      "vanish" VFX/SFX (`FleckMaker.ConnectingLine` with `FleckDefOf.LineEMP`, `FleckMaker.Static` with
      `FleckDefOf.PsycastSkipFlashEntry`, `SoundDefOf.Psycast_Skip_Entry`) and store pending state instead of
      relocating immediately; add `CompPostTick` → `ResolvePendingPullIfDue()`, which performs the actual
      relocation, plays the "arrival" VFX/SFX (`FleckDefOf.PsycastSkipInnerExit`/`PsycastSkipOuterRingExit`,
      `SoundDefOf.Psycast_Skip_Exit`), and grants the tend buff once `pullResolveTick` is reached (depends on
      T011).
      **DONE** — `dotnet build`: 0 errors, 0 warnings. All VFX/SFX assets are already-shipped vanilla content
      (reused from `RimWorld.CompAbilityEffect_Teleport`, decompiled) — no new art or Defs needed. Additionally
      tinted gold-white (`HolyTetherColor`, research.md R10) via `FleckCreationData.instanceColor` for an
      angelic/holy feel per the customer's follow-up request, rather than the assets' native electric-blue look —
      required switching from `FleckMaker.Static`/`ConnectingLine` to building `FleckCreationData` by hand
      (`SpawnTintedTetherLine`, `SpawnTintedFleck`). Not yet live-verified (subsumed by T014/T017/T019's own
      live-testing passes, which should now also confirm the vanish/arrival flashes and line render correctly,
      actually read as gold-white rather than the native blue, and the pull duration reads as a quick snatch
      rather than an instant blink or a sluggish float).

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since no gizmo exists
  at all until the comp is wired up, attached via XML, and the mastery opt-out is in place.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same targeting/confirmation code US1 introduced (`IsValidTarget`/`GetRejectReason`,
    `OnTargetConfirmed`) — build after US1, not in parallel with it.
  - US3 also touches the tier-branching inside the same methods — build after both US1 and US2, since it needs
    their final pre-US3 shape (targeting + corpse-handling already wired in) rather than editing a moving target.
- **Polish (Phase 6)**: Depends on US1, US2, and US3 all being complete.

### Within Each User Story

- Foundational: T002 (tend hediff XML), T003 (tattoo recipe XML), T004 (tend-buff hediff XML), and T005 (DefOf
  entries) can all run in parallel — distinct files, no compile-time coupling; T006 (comp skeleton) depends on
  T002 and T005; T007 (verification) depends on T006.
- US1: T008 (tend-buff hediff comp) and T009 (grant/read utility) can run in parallel — distinct files, T009
  depends on T008's class existing but not its implementation being finished; T010 (targeting) depends on T006;
  T011 (confirmed-target activation) depends on T008, T009, and T010; T012 (quality patch) and T013 (speed
  StatPart) can both run in parallel with T010/T011, since they only depend on T009's utility existing, not on the
  targeting/pull code itself; T014 (verification) depends on T011, T012, and T013.
- US2: T015 (corpse-aware targeting) depends on T010; T016 (skip buff for corpses) depends on T011 and T015; T017
  (verification) depends on T016.
- US3: T018 (tier-branching confirmation) depends on T011 and T016; T019 (verification) depends on T018.

### Parallel Opportunities

- Foundational: T002, T003, T004, and T005 can all run in parallel.
- US1: T008 and T009 can start together; T012 and T013 can run in parallel with each other and with T010/T011
  (all four only need T009's utility signature, not its finished body, to compile against).
- No cross-story parallelism — each story builds on the previous one's edits to the same comp methods by design
  (mirroring feature 016's identical US1/US2/US3 shape).

---

## Parallel Example: Foundational Phase

```bash
# Launch these together once Setup (Phase 1) is complete:
Task: "Create Defs/HediffDefs/Tattoos/SoulTether.xml with HediffCompProperties_SoulTetherEffect"
Task: "Create Defs/TattooDefs/SoulTether.xml"
Task: "Create Defs/HediffDefs/SoulTetherTendBuff.xml"
Task: "Add TattooMagic_Hediff_SoulTether and TattooMagic_Hediff_SoulTetherTendBuff to Source/Defs/TattooMagicDefOf.cs"
```

## Parallel Example: User Story 1

```bash
# Launch these together once Foundational (Phase 2) is complete:
Task: "Implement Source/Hediffs/HediffComp_SoulTetherTendBuff.cs"
Task: "Implement Source/Effects/SoulTetherTendBuffUtility.cs"
Task: "Implement Source/Patches/Patch_TendUtility_CalculateBaseTendQuality_SoulTetherBuff.cs"
Task: "Implement Source/Effects/StatPart_SoulTetherTendSpeedBuff.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenario 1 in-game.
5. This is a demoable MVP: Soul Tether is no longer an inert stub — it pulls a chosen living friendly pawn to the
   caster and grants a real, measurable tend speed/quality bonus, at Tier 1 only (no corpse targeting or tiering
   yet).

### Incremental Delivery

1. Setup + Foundational → a real, working gizmo/cooldown/tier/mastery-opt-out shell, nothing player-visible as a
   *pull* yet.
2. Add US1 → validate Scenario 1 → MVP (Tier 1 Soul Tether pulls a living ally and buffs their tending).
3. Add US2 → validate Scenario 2 → corpse pulling works, hostile corpses are correctly rejected.
4. Add US3 → validate Scenario 3 → Tier 2 auto-upgrade, longer range, and a stronger tend buff all work.
5. Polish → validate Scenarios 4-5 and confirm no regression to any other triggered tattoo's own files or to
   tending an unbuffed pawn.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- This feature introduces no `GameComponent`/`MapComponent` (unlike Bastion Ward) — all new runtime state is
  ordinary per-pawn hediffs, so no new registry/persistence design work appears in these tasks.
- research.md R8 / contracts/pull-and-apply-effect-contract.md's "reusable for a future inverse tattoo" framing is
  satisfied by construction (the filter logic lives in `GetRejectReason`, the arrival effect lives in a clearly
  separated step of `OnTargetConfirmed`) rather than by a dedicated task — no separate interface/registry is being
  built ahead of a second confirmed consumer, per that doc's own "no premature abstraction" rationale.
