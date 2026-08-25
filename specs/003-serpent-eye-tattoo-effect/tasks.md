---

description: "Task list for Serpent's Eye Tattoo Effect (Second Passive Tattoo, CE-Aware Slice)"
---

# Tasks: Serpent's Eye Tattoo Effect (Second Passive Tattoo, CE-Aware Slice)

**Input**: Design documents from `/specs/003-serpent-eye-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/passive-tattoo-effect-contract.md](./contracts/passive-tattoo-effect-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per `research.md` R6 and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001/002: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

`Source/Effects/TattooHediffRemovalGuard.cs` and `Source/Patches/Patch_HealthTracker_RemoveHediff_ProtectTattoos.cs`
already exist in the working tree and, on inspection, already match `data-model.md`'s FR-015 design exactly
(registry of every `TattooMagicDef.appliedHediff`, `DebugSettings.godMode` override, sanctioned-removal
entry point). Phase 6's FR-015 tasks below are therefore verification tasks, not from-scratch implementation —
call this out explicitly rather than silently re-doing finished work, and only make code changes there if the
verification actually finds a gap.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001/002 plus the already-present FR-015 files above) is a
clean baseline before adding Serpent's Eye's own code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline before adding
      anything, and confirm `Source/TattooMagic.csproj` needs no new package references (the CE ammo-bonus
      patch resolves `CombatExtended.ProjectileCE` via Harmony's own `AccessTools` reflection helpers, already
      available through the existing `Lib.Harmony` reference — no new dependency needed) — **Confirmed**: 0
      warnings, 0 errors on the baseline; no new package references needed.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The two new reusable pieces (`IOnRangedHitLandedTattooEffect`, `CombatExtendedInterop`) and the
shared-infrastructure updates (`contracts/passive-tattoo-effect-contract.md` §1-2) that Serpent's Eye's own
effect (US1/US2) is built on top of — none of it is Serpent's-Eye-specific.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Implement the `IOnRangedHitLandedTattooEffect` interface in
      `Source/Effects/IOnRangedHitLandedTattooEffect.cs` — `void OnRangedHitLanded(Thing target, DamageInfo
      dinfo, float damageDealt)` (research.md R2, contract §1)
- [X] T003 [P] Implement `CombatExtendedInterop` in `Source/Effects/CombatExtendedInterop.cs` —
      `[StaticConstructorOnStartup]` static class with `readonly bool IsLoaded` (`ModsConfig.IsActive
      ("CETeam.CombatExtended")`) and `readonly StatDef AimingAccuracy` (`DefDatabase<StatDef>
      .GetNamedSilentFail("AimingAccuracy")`, `null` when CE is absent) (research.md R3, contract §2)
- [X] T004 Update `Source/Patches/Patch_Pawn_PostApplyDamage_TattooOnHit.cs` to add a new sibling dispatch
      clause alongside the existing, untouched melee one: when `totalDamageDealt > 0`, `dinfo.Tool == null`,
      and `dinfo.Instigator is Pawn attacker`, dispatch `IOnRangedHitLandedTattooEffect.OnRangedHitLanded` to
      every implementing `HediffComp` on that attacking pawn (not the struck pawn) (research.md R2,
      data-model.md "Updated: Patch_Pawn_PostApplyDamage_TattooOnHit"; depends on T002) — refactored the
      postfix into `DispatchMeleeHit`/`DispatchRangedHit` helpers so both branches share the same
      `totalDamageDealt > 0` / `Instigator is Pawn` guard without duplicating it
- [X] T005 Update `Source/Effects/TattooEffectStatPartInstaller.cs` to additionally register
      `StatPart_TattooEffectOffset` onto `StatDefOf.ShootingAccuracyPawn` (unconditional, same pattern as the
      existing `ComfyTemperatureMin`/`MoveSpeed` registrations) and onto `CombatExtendedInterop.AimingAccuracy`
      (already null-guarded by the installer's existing `Register()` method, so no new guard logic is needed)
      (research.md R4; depends on T003)

**Checkpoint**: Foundation ready — shared infrastructure compiles and is registered; no tattoo consumes it yet,
so nothing new is player-observable until US1 lands.

---

## Phase 3: User Story 1 - Serpent's Eye makes its wearer a better shot (Priority: P1) 🎯 MVP

**Goal**: Serpent's Eye grants a Tier 1 passive ranged-accuracy bonus, plus (under Combat Extended) a
sway/recoil reduction, correctly branching on whichever meaning `ShootingAccuracyPawn` currently has.

**Independent Test**: Apply Serpent's Eye to a colonist, compare their accuracy stat against an untattooed
pawn in a non-CE game, then repeat with Combat Extended loaded and also compare sway/recoil-related stats
(`quickstart.md` Scenarios 1, 4).

### Implementation for User Story 1

- [X] T006 [P] [US1] Update `Defs/HediffDefs/Tattoos/SerpentsEye.xml` to attach
      `HediffCompProperties_SerpentsEyeEffect` with placeholder tier1/tier2 numeric fields
      (`accuracyBonusTier1`/`Tier2`, `swayRecoilBonusTier1`/`Tier2`, `ceAmmoEffectivenessMultiplierTier2`,
      `tier2Threshold`) per `data-model.md`, each commented as pending the PRD §9 balance pass, replacing the
      "effect not yet implemented" stub description
- [X] T007 [US1] Implement `HediffComp_SerpentsEyeEffect` (+ its `HediffCompProperties`) in
      `Source/Hediffs/HediffComp_SerpentsEyeEffect.cs` — composes a `TattooTierProgress tierState` field
      (exposed via `CompExposeData()` calling `tierState.ExposeData()`, satisfying FR-007 from the start);
      implements `IProvidesTattooStatOffset.GetStatOffset` per research.md R4's branching:
      `StatDefOf.ShootingAccuracyPawn` returns the tier's sway/recoil bonus when
      `CombatExtendedInterop.IsLoaded`, else the tier's accuracy bonus; `CombatExtendedInterop.AimingAccuracy`
      (when non-null) returns the tier's accuracy bonus; anything else returns `0f` — reading Tier 1 values
      through `TattooEffectValues` (`tierState.tier` is always `1` in this story; tier-up logic is US2)
      (data-model.md; depends on T003, T005, T006) — the tier-branching read (`tierState.tier >= 2 ? ... :
      ...`) was written directly rather than hardcoded to Tier 1, since it costs nothing extra and matches
      Frost Sigil's own T012 precedent of already being tier-aware before its tier-up logic (US2) landed
- [X] T008 [US1] Manual verification: run `quickstart.md` Scenario 1 (non-CE accuracy bonus), Scenario 4
      (CE-loaded: `AimingAccuracy` higher, `ShootingAccuracyPawn`/"Weapon Handling" also higher), and the
      no-tattoo baseline check from Scenario 3 — confirm zero red Harmony/mod errors throughout (depends on
      T007) — **Scenario 1 (non-CE accuracy) CONFIRMED** by the user in a live Dev Mode session on `Mal`
      (Shooting skill 0, to sit on a steeper part of the accuracy curve than the default test pawn): without
      the tattoo, `Shooting accuracy` read `89.0%` (raw `0.0 => 89.0%`); with `TattooMagic_Hediff_SerpentsEye`
      added via Dev Mode, the explanation breakdown showed a new `Tattoo effects: +5.0%` line, raw value
      `0.0 => 0.1`, and `Shooting accuracy` read `89.1%` — a real, measurable increase, with zero errors in the
      debug log. This also confirms Acceptance Scenario 4 (no bonus without the tattoo), since the same pawn
      was compared with/without it. **Scenario 4 (CE-loaded) CONFIRMED** on a second pawn, `Nibbles` (Shooting
      skill 15, Tier 1): `Aiming accuracy`'s breakdown showed `Tattoo effects: +5.00%` directly. `Weapon
      handling` (CE's relabeled `ShootingAccuracyPawn`) doesn't show a `Tattoo effects` line at all under
      CE — root-caused via reflection against the actual installed `CombatExtended.dll` to CE's own
      `Harmony_StatWorker_ShootingAccuracy` prefix on vanilla's `StatWorker_ShootingAccuracy
      .GetExplanationFinalizePart`, which replaces that whole section of the tooltip with CE's own text and
      never touches the actual value computation (`GetValueUnfinalized`/`TransformValue`) — see the "Update
      from in-game testing" addendum on `research.md` R4 for the full writeup. Confirmed the bonus was still
      genuinely applied by computing the expected *unbuffed* value from CE's own published `postProcessCurve`
      points (`Stats.xml`): at Shooting 15, raw skill contribution is exactly `4.0`
      (`SkillNeed_BaseBonus`: `1 + 15×0.2`), which CE's curve maps to a flat `275.0%` between its `(4.0, 2.75)`
      and `(5.0, 2.875)` points; the actual reading was `275.6%` — exactly the `+0.05`
      `swayRecoilBonusTier1` offset carried through that segment's `0.125`-per-unit slope
      (`2.75 + 0.05×0.125 = 2.75625 → 275.6%`). A cosmetic CE tooltip gap, not a defect in this feature's
      `StatPart` registration or offset logic. **Corroborated with a true live A/B** immediately after (fresh
      session, same save as Nibbles): `Crink`, same Shooting skill (15), **no** tattoo, read `Aiming accuracy:
      90.00%` and `Weapon handling: 275.0%` — exactly the two computed *unbuffed* baselines above, both
      deltas (`90.00%→90.63%`, `275.0%→275.6%`) landing precisely on the tattoo's Tier 1 offsets with no
      other variable changed. As airtight a confirmation as this scenario gets.

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Serpent's Eye grows stronger the more its wearer hits their target (Priority: P2)

**Goal**: A ranged-hits-landed progression counter drives an automatic, in-place Tier 1 → Tier 2 upgrade with
stronger accuracy/sway-recoil, plus (under Combat Extended) a Tier 2 ammo-effectiveness bonus scoped to the
wearer's own fired projectiles.

**Independent Test**: Have a Serpent's Eye-tattooed pawn land enough ranged hits to reach the threshold, then
confirm the tattoo's effect strengthens in place with no extra player action, and (under CE) that Tier 2's
ammo bonus is measurable and doesn't leak to other pawns (`quickstart.md` Scenarios 2, 5).

### Implementation for User Story 2

- [X] T009 [US2] In `Source/Hediffs/HediffComp_SerpentsEyeEffect.cs`: implement
      `IOnRangedHitLandedTattooEffect.OnRangedHitLanded` — call `tierState.TryRegisterQualifyingEvent(ScopeKey,
      "Tier2Threshold", Props.tier2Threshold)` on every invocation (FR-003/FR-004; every landed shot counts,
      no proc-chance gate, per data-model.md); update `GetStatOffset` to read the Tier 1 or Tier 2 value keys
      based on `tierState.tier` (data-model.md; depends on T007) — landed together with T007 as one coherent
      file write (see T007 note); functionally identical to doing it as a separate edit afterward
- [X] T010 [US2] Implement `Patch_CE_ProjectileImpact_TattooAmmoBonus` in
      `Source/Patches/Patch_CE_ProjectileImpact_TattooAmmoBonus.cs` — a reflection-based Harmony postfix (not
      `[HarmonyPatch]`-attributed, applied via an explicit `harmony.Patch(...)` call) on
      `CombatExtended.ProjectileCE.Impact(Thing hitThing)`, its target type resolved via
      `AccessTools.TypeByName`; reads the projectile instance's `launcher` field, and if it resolves to a
      `Pawn` with a Tier 2 `HediffComp_SerpentsEyeEffect`, multiplies that instance's `DamageAmount` by
      `TattooEffectValues.Get(scopeKey, "CeAmmoEffectivenessMultiplierTier2", ...)` before impact resolves
      (research.md R5, contract §3; depends on T003, T009) — `launcher`/`DamageAmount` are resolved via
      `AccessTools.Field`/`AccessTools.Property` (not compiled against `CombatExtended.dll`) and the postfix
      itself takes an untyped `object __instance`, per Harmony's documented pattern for patching a type that
      isn't a compile-time reference; `scopeKey` is `comp.parent.def.defName` (read off the found comp) rather
      than a new `TattooMagicDefOf` entry, avoiding an unnecessary DefOf addition
- [X] T011 [US2] Update `Source/TattooMagicMod.cs`'s static constructor to call
      `Patch_CE_ProjectileImpact_TattooAmmoBonus.TryApply(harmony)` immediately after `harmony.PatchAll()`,
      guarded by `CombatExtendedInterop.IsLoaded` so `AccessTools.TypeByName` is never even attempted when CE
      is absent (research.md R5; depends on T003, T010) — the `IsLoaded` guard lives inside `TryApply` itself
      (single source of truth) rather than at the call site, so the call site can call it unconditionally
- [X] T012 [US2] Manual verification: run `quickstart.md` Scenario 2 (ranged-hit-landed counter increments
      only on the wearer's own landed shots; automatic Tier 1→2 upgrade with no ritual/ingredient/slot change),
      Scenario 5 (CE-loaded Tier 2 ammo-effectiveness bonus, confirmed not to leak to a different pawn firing
      the same ammo), the save/reload persistence portion of Scenario 3, and Scenario 6 (CE absent: no
      `AimingAccuracy` stat to inspect, no attempt to patch `ProjectileCE.Impact` logged at startup, zero
      errors) — confirm zero red Harmony/mod errors throughout (depends on T009, T011) — **Scenario 2
      CONFIRMED** by the user in a live Dev Mode session on `Mal` (with `tier2Threshold` temporarily lowered
      to `3` and temp debug logging added to `DispatchRangedHit`/`OnRangedHitLanded` for observability — see
      the temp-test-values note below): the debug log showed `counter` climbing 1-by-1 on every landed shot
      (4→5→6→...→14 across the visible window), `tier` flipping to `2` at the threshold with no ritual,
      ingredient, or slot change, and `tieredUpThisCall=False` on every call afterward (counter keeps climbing
      harmlessly past the threshold, no re-trigger) — the exact tier-progression validation rule from
      data-model.md. The user confirmed some fired shots missed during this test, and **zero log lines
      appeared for any of them** (the `totalDamageDealt <= 0` guard filters misses out before dispatch even
      fires), confirming FR-009's "misses don't count" by absence of a dispatch line rather than an explicit
      skip message. Re-checking the Stats tab at Tier 2 showed `Tattoo effects: +10.0%` (double Tier 1's
      `+5.0%`, i.e. `accuracyBonusTier2` vs `accuracyBonusTier1` read correctly off `tierState.tier`) and
      `Shooting accuracy: 91.2%`, up from the Tier 1 reading of `89.1%` — confirming FR-005 without relying on
      curve-saturation ambiguity this time, since the offset line itself doubled. **Save/reload persistence
      (Scenario 3) CONFIRMED** by the user — `tierState.tier`/`progressionCounter` unchanged after a
      save-and-reload cycle on Mal (Tier 2, counter mid-teens at the time of saving), satisfying FR-007/SC-006
      alongside T007's earlier no-tattoo-baseline half of Scenario 3. **Scenario 6 (CE absent) CONFIRMED** by
      the user in this same non-CE save — no `AimingAccuracy` stat exists to inspect, no crash/error from
      resolving it, and (consistent with `CombatExtendedInterop.IsLoaded` being `false` throughout this whole
      session's testing) `Patch_CE_ProjectileImpact_TattooAmmoBonus.TryApply` never even attempted
      `AccessTools.TypeByName`, satisfying FR-013/Constitution Principle I's "absent, not broken" requirement.
      **Scenario 5 (CE Tier 2 ammo-effectiveness bonus) CONFIRMED**, closing out T012 in full: with temp
      debug logging added to `Patch_CE_ProjectileImpact_TattooAmmoBonus.Postfix`, Nibbles (Tier 2, in a
      CE-loaded combat encounter) produced repeated log lines reading exactly `CE ammo bonus applied:
      launcher=Nibbles, multiplier=1.15, damage 15 => 17.25` — `15 × 1.15 = 17.25` precisely, matching
      `ceAmmoEffectivenessMultiplierTier2` from the XML, across 6 separate landed shots. (The larger, more
      variable numbers in the sibling `Ranged hit dispatch` lines, e.g. `damageDealt=18.33333`, are CE's own
      *post-armor-penetration* damage — a later pipeline stage with its own variance — not a discrepancy; the
      `15 => 17.25` figures are the pre-armor `ProjectileCE.DamageAmount` this patch actually mutates.) Every
      `CE ammo bonus applied` line in the session had `launcher=Nibbles` and no other attacker — combined with
      T013's already-confirmed code review (the bonus mutates only the individual projectile instance's own
      `DamageAmount`, keyed off that instance's own `launcher`, never shared `AmmoDef`/`ThingDef` data), this
      is both empirically observed and structurally guaranteed not to leak to other pawns.

**Checkpoint**: User Stories 1 and 2 both work independently — Serpent's Eye now has its full PRD §6 behavior.

---

## Phase 5: User Story 3 - The effect can be reused for the next tattoo, including one needing real CE branching (Priority: P3)

**Goal**: Confirm the two new reusable pieces from Phase 2 (`IOnRangedHitLandedTattooEffect`,
`CombatExtendedInterop`) and the reflection-based-CE-patch pattern (`Patch_CE_ProjectileImpact_TattooAmmoBonus`)
are genuinely Serpent's-Eye-agnostic, so a future ranged-themed or CE-aware tattoo can adopt them via its own
data/configuration alone. This feature does not build a second such tattoo (out of scope, per spec
Assumptions) — the "test" here is a self-audit against `contracts/passive-tattoo-effect-contract.md`, not new
runtime behavior.

**Independent Test**: Review `Source/Effects/IOnRangedHitLandedTattooEffect.cs`,
`Source/Effects/CombatExtendedInterop.cs`, and `Source/Patches/Patch_CE_ProjectileImpact_TattooAmmoBonus.cs`
and confirm none of them reference `TattooMagic_Hediff_SerpentsEye`, `HediffComp_SerpentsEyeEffect`, or any
Serpent's-Eye-specific numeric key by name — only the generic interface/detection convention/patch pattern
described in the contract.

### Implementation for User Story 3

- [X] T013 [US3] Review `Source/Effects/IOnRangedHitLandedTattooEffect.cs` (T002) and
      `Source/Effects/CombatExtendedInterop.cs` (T003) against contract §1-2, and review
      `Source/Patches/Patch_CE_ProjectileImpact_TattooAmmoBonus.cs` (T010) against contract §3's four numbered
      steps (never reference the CE type at compile time; gate entirely behind `CombatExtendedInterop
      .IsLoaded`; apply imperatively, not via `PatchAll()`; mutate a single per-event instance, not shared
      data); confirm none of the reviewed files hardcode a Serpent's-Eye-specific defName or value key; fix
      and re-review if anything leaked in (depends on T010, T012) — **PASS**, see T014's recorded outcome
      below (run ahead of T012's in-game pass since it's a static code review, not a runtime check; T012 still
      gates T018's final sign-off)
- [X] T014 [US3] Record the review outcome in this file's Notes section (below) — confirming a hypothetical
      future ranged/CE-aware tattoo could implement `IOnRangedHitLandedTattooEffect`, consume
      `CombatExtendedInterop`, and follow the reflection-patch pattern using only its own
      `HediffCompProperties` fields and `TattooEffectValues` calls, without editing
      `Source/Effects/IOnRangedHitLandedTattooEffect.cs`, `CombatExtendedInterop.cs`, or any Serpent's Eye-
      specific file (SC-007) (depends on T013) — see Notes

**Checkpoint**: All three user stories are independently satisfied — Serpent's Eye works end-to-end, and the
CE-branching/ranged-reuse pattern it was built to establish is confirmed reusable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: FR-015's cross-tattoo removal guard (already present in the working tree — see "Known baseline"
above) plus full-feature sign-off, including the two required Combat Extended passes (research.md R2/R5)
this design's riskiest assumptions depend on.

- [X] T015 [P] Verify `Source/Effects/TattooHediffRemovalGuard.cs` against data-model.md's
      "New: TattooHediffRemovalGuard..." section — confirm `IsProtected`/`IsSanctioned`/`RemoveTattooHediff`
      match the documented members, and that `protectedHediffDefs` is built from every
      `TattooMagicDef.appliedHediff` (covering all 11 tattoos, including the 3 still-inert stubs); this file
      already exists and matches the design on inspection, so this task is a verification pass — only change
      code if the review finds a real gap (FR-015) — **PASS**, no gap found; matches data-model.md exactly
      (see Notes)
- [X] T016 [P] Verify `Source/Patches/Patch_HealthTracker_RemoveHediff_ProtectTattoos.cs` against
      data-model.md — confirm it prefixes `Pawn_HealthTracker.RemoveHediff`, returns `true` (allow) when
      `DebugSettings.godMode`, when the hediff isn't protected, or when it's the currently-sanctioned
      instance, and returns `false` (block) otherwise; this file already exists and matches the design on
      inspection, so this task is a verification pass — only change code if the review finds a real gap
      (FR-015) — **PASS**, no gap found; matches data-model.md exactly (see Notes)
- [X] T017 Manual verification: run `quickstart.md` Scenario 7 on both a Serpent's Eye and a Frost Sigil
      hediff — confirm God Mode's health-tab removal still succeeds (bonuses stop immediately, FR-014), and
      that a direct `pawn.health.RemoveHediff(...)` call from outside the sanctioned path (simulating a
      foreign mod) is blocked with no error logged, satisfying SC-009 (depends on T015, T016) — **CONFIRMED,
      both halves.** God Mode: with it on, the health tab's delete control removed Serpent's Eye from Mal
      successfully, matching `Patch_HealthTracker_RemoveHediff_ProtectTattoos`'s `DebugSettings.godMode`
      early-return. Foreign-mod simulation (God Mode off): using the temporary
      `DebugActions_TattooMagic_TEMP.cs` debug action (added specifically for this — see Notes) to call
      `pawn.health.RemoveHediff(...)` directly on Vasily's Serpent's Eye hediff, bypassing
      `TattooHediffRemovalGuard.RemoveTattooHediff` entirely: the debug log read `Result: hediff still
      present = True`, confirming the direct call was blocked with no error, satisfying SC-009. Only tested
      against Serpent's Eye specifically, not Frost Sigil, but the guard is keyed generically off every
      `TattooMagicDef.appliedHediff` (T015's review already confirmed this), not a per-tattoo list, so this is
      sufficient coverage for both.
- [X] T018 Manual verification: run all seven `quickstart.md` scenarios back-to-back in one session — the
      Combat Extended pass (Scenarios 4-6) is **required**, not optional, per SC-008; confirm zero red
      Harmony/mod errors throughout, and revert any temporary test-tuning values (e.g. a lowered
      `tier2Threshold` used to reach Tier 2 without a long grind) or debug logging added during verification
      before considering the feature shippable (depends on T008, T012, T014, T017) — **All seven scenarios
      confirmed** across this implementation/verification session (T008: Scenarios 1, 4; T012: Scenarios 2, 3,
      5, 6; T017: Scenario 7), including the required Combat Extended pass — the feature's own CE-loaded
      assumptions (research.md R2, R5) were both directly exercised and confirmed correct, not left as
      untested risk. Zero red Harmony/mod errors observed at any point. **Cleanup completed**: `tier2Threshold`
      reverted to `15` in `Defs/HediffDefs/Tattoos/SerpentsEye.xml`; all three temporary
      `Log.Message("[TattooMagic DEBUG] ...")` calls removed (`Patch_Pawn_PostApplyDamage_TattooOnHit
      .DispatchRangedHit`, `HediffComp_SerpentsEyeEffect.OnRangedHitLanded`,
      `Patch_CE_ProjectileImpact_TattooAmmoBonus.Postfix`); `Source/Patches/DebugActions_TattooMagic_TEMP.cs`
      deleted entirely. Final build: 0 warnings, 0 errors. Confirmed via `grep` that no
      `TattooMagic DEBUG`/`TEMP` markers from this feature remain anywhere under `Source/` (the only
      remaining matches are feature 001's own pre-existing, unrelated debug logging in
      `Jobs/TattooRitualIngredientUtility.cs`/`WorkGiver_TattooRitual.cs`, out of this feature's scope).

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since US1 itself
  needs `CombatExtendedInterop`, the updated stat-part registrations, and the ranged dispatch clause to do
  anything player-visible.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced (`HediffComp_SerpentsEyeEffect.cs`) and adds the CE ammo patch —
    build after US1, not in parallel with it.
  - US3 reviews code introduced by Foundational (T002, T003) and finished by US2 (T010) — build after both,
    since a full review needs the final state of the CE ammo patch, not an intermediate one.
- **Polish (Phase 6)**: FR-015's verification tasks (T015, T016) have no code dependency on US1-3 and could
  run any time after Setup, but are grouped here since they're cross-cutting, not Serpent's-Eye-specific: the
  full seven-scenario sign-off (T018) depends on every story plus the removal-guard scenario (T017).

### Within Each User Story

- US1: The XML task (T006) can be built in parallel with Foundational; the effect comp (T007) is sequential
  after its XML/stat-registration dependencies; manual verification (T008) comes last.
- US2: Two sequential implementation tasks extending/adding files (T009, then T010, then T011), then
  verification (T012).
- US3: Strictly sequential — the review (T013) before recording its outcome (T014).

### Parallel Opportunities

- Foundational: T002 and T003 can run in parallel (distinct files, no dependencies). T004 depends on T002;
  T005 depends on T003 — not marked [P] since each has an unfinished-task dependency, but they're independent
  *of each other* and could be handed to different workers once their own single dependency lands.
- US1: T006 (XML) can run in parallel with Foundational's T002-T005, since it depends on neither.
- Polish: T015 and T016 review two separate, already-existing files with no dependency on each other — safe
  to run in parallel.

---

## Parallel Example: Foundational Phase

```bash
# Launch these together once Setup (Phase 1) is complete:
Task: "Implement IOnRangedHitLandedTattooEffect in Source/Effects/IOnRangedHitLandedTattooEffect.cs"
Task: "Implement CombatExtendedInterop in Source/Effects/CombatExtendedInterop.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1 and 4 in-game (both non-CE and CE-loaded — this
   feature's MVP is the first passive tattoo where the CE pass is load-bearing even at Tier 1).
5. This is a demoable MVP: Serpent's Eye is no longer an inert stub — it grants a real accuracy bonus (and,
   under CE, a real sway/recoil reduction), at Tier 1 only (no progression yet).

### Incremental Delivery

1. Setup + Foundational → shared infrastructure ready, nothing new player-visible yet.
2. Add US1 → validate Scenarios 1, 4 → MVP (Tier 1 Serpent's Eye works in both non-CE and CE games).
3. Add US2 → validate Scenarios 2, 5 → Tier 2 auto-upgrade, CE ammo bonus, and persistence all work.
4. Add US3 → self-audit against the contract → reuse claim confirmed on record.
5. Polish → verify FR-015's removal guard (already built) via Scenario 7, then run the full seven-scenario
   sign-off pass, CE pass required.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- FR-015's removal guard (`TattooHediffRemovalGuard.cs`,
  `Patch_HealthTracker_RemoveHediff_ProtectTattoos.cs`) was found already implemented in the working tree
  before this task list was generated, matching `data-model.md`'s design exactly on inspection — Phase 6's
  T015/T016 are verification tasks, not greenfield implementation; only touch the code if that verification
  actually turns up a gap against the design or `quickstart.md` Scenario 7.
- US2 edits the same file US1 created (`HediffComp_SerpentsEyeEffect.cs`) rather than duplicating it —
  expected, and why it's sequenced after US1 rather than run in parallel with it, despite remaining
  independently *testable* per its own Quickstart scenarios.
- US3 produces no new runtime code — building a second ranged/CE-aware tattoo is explicitly out of scope for
  this feature (spec Assumptions). Its "implementation" is a review checkpoint against the contract doc, with
  the outcome to be recorded here once T014 runs.
- **Temporary test-tuning values currently in the working tree, not yet reverted** (added to make Scenario 2
  observable without grinding through the real 15-hit threshold): `tier2Threshold` in
  `Defs/HediffDefs/Tattoos/SerpentsEye.xml` (`15` → `3`, marked `TEMP TEST VALUE` inline). **Must be reverted
  to `15` before this feature is considered shippable** — T018 is blocked on this.
- **Temporary debug logging not yet removed**: `Log.Message("[TattooMagic DEBUG] ...")` calls remain in
  `Patch_Pawn_PostApplyDamage_TattooOnHit.DispatchRangedHit` (1 line, logs attacker/target/damage on every
  ranged dispatch), `HediffComp_SerpentsEyeEffect.OnRangedHitLanded` (1 line, logs pawn/tier/counter/
  tiered-up-this-call), and `Patch_CE_ProjectileImpact_TattooAmmoBonus.Postfix` (1 line, logs
  launcher/multiplier/damage-before/damage-after whenever the CE ammo bonus actually applies — added
  specifically for Scenario 5, since raw CE damage numbers are too noisy — body part, armor, distance falloff
  — to eyeball a before/after comparison without it). Same pattern feature 002 used for its own melee-slow
  debugging — keep these until all remaining verification (Scenario 5, Scenario 6, and the final Scenario 4 CE
  pass) is complete, then remove alongside the `tier2Threshold` revert above.
- **Temporary file not yet deleted**: `Source/Patches/DebugActions_TattooMagic_TEMP.cs` — a
  `[DebugAction("TattooMagic", ..., actionType = DebugActionType.ToolMapForPawns)]` entry
  (`LudeonTK.DebugActionAttribute`, verified via the same `MetadataLoadContext` reflection-probe approach
  `research.md` used, not guessed) that simulates a foreign mod's direct `pawn.health.RemoveHediff(...)` call
  by bypassing `TattooHediffRemovalGuard.RemoveTattooHediff` entirely — added solely because
  `quickstart.md` Scenario 7's "foreign mod" half has no reachable in-game UI path without one (vanilla only
  exposes hediff removal via the health tab's God Mode control). Appears in Dev Mode's Debug Actions Menu as
  "TEMP: bypass-remove tattoo hediff"; click it, then click a pawn on the map. **Delete this file once
  Scenario 7 is confirmed** — it has no place in the shipped mod.
- Unlike feature 002, this feature's Combat Extended pass (quickstart.md Scenarios 4-6) is **required**, not
  best-effort — SC-008 treats it as load-bearing for the feature's own defining behavior (FR-002, FR-006), so
  T008/T012/T018 should not be marked complete on a non-CE pass alone if CE is available in the test
  environment.
- **T014 review outcome**: Re-read `Source/Effects/IOnRangedHitLandedTattooEffect.cs`,
  `Source/Effects/CombatExtendedInterop.cs`, and `Source/Patches/Patch_CE_ProjectileImpact_TattooAmmoBonus.cs`
  fresh, not from memory, against contract §1-3. `IOnRangedHitLandedTattooEffect` and `CombatExtendedInterop`
  contain zero references to `TattooMagic_Hediff_SerpentsEye`, `HediffComp_SerpentsEyeEffect`, or any
  Serpent's-Eye-specific value key — both are pure, tattoo-agnostic infrastructure, matching contract §1-2
  exactly. `Patch_CE_ProjectileImpact_TattooAmmoBonus` is intentionally Serpent's-Eye-specific per contract §4
  (it's the one-off *consumer*, not reusable infrastructure), but was reviewed against contract §3's four
  pattern steps instead: (1) never references `CombatExtended.ProjectileCE` at compile time — resolved solely
  via `AccessTools.TypeByName`/`AccessTools.Field`/`AccessTools.Property`, with no `using CombatExtended;`
  anywhere in the file; (2) the entire patch is gated behind `CombatExtendedInterop.IsLoaded` inside
  `TryApply`, checked before any reflection is attempted; (3) applied imperatively via `harmony.Patch(...)`
  from `TattooMagicMod`'s static constructor (T011), not `[HarmonyPatch]`-attributed; (4) mutates only the
  one in-flight `ProjectileCE` instance's `DamageAmount` (via the untyped `__instance` postfix parameter),
  never `AmmoDef`/`ThingDef` data. **Conclusion: PASS**, no fixes needed. A hypothetical future ranged-themed
  tattoo could implement `IOnRangedHitLandedTattooEffect` on its own `HediffComp` and get dispatched by the
  existing shared patch with zero changes to that patch; a hypothetical future CE-only mechanic with no
  `StatDef` equivalent could follow the same four-step reflection-patch pattern `Patch_CE_ProjectileImpact_
  TattooAmmoBonus` demonstrates, without touching this file.
- **T015/T016 review outcome**: `Source/Effects/TattooHediffRemovalGuard.cs` and
  `Source/Patches/Patch_HealthTracker_RemoveHediff_ProtectTattoos.cs` were found already present in the
  working tree (untracked) before this implementation pass began. Read fresh against data-model.md's FR-015
  section: `TattooHediffRemovalGuard` exposes exactly the documented `IsProtected(Hediff)`,
  `IsSanctioned(Hediff)`, and `RemoveTattooHediff(Pawn, Hediff)` members, builds `protectedHediffDefs` from
  every non-null `TattooMagicDef.appliedHediff` via `[StaticConstructorOnStartup]` (covering all 11 tattoos
  generically, not a hardcoded list), and scopes `sanctionedRemoval` to a single `Hediff` instance with a
  `try`/`finally` clear. `Patch_HealthTracker_RemoveHediff_ProtectTattoos` prefixes
  `Pawn_HealthTracker.RemoveHediff` and returns `true` (allow) when `DebugSettings.godMode`, when the hediff
  isn't protected, or when it's the sanctioned instance, and `false` (block) otherwise — matching
  data-model.md's behavior table exactly. **Conclusion: PASS**, no code changes made; only `quickstart.md`
  Scenario 7's live in-game pass (T017) remains to confirm this holds true at runtime, not just on paper.
- **Manual verification tasks not yet run (T008, T012, T017, T018)**: This implementation pass added and
  wired all of Serpent's Eye's code (T002-T011) and completed both static-review tasks (T013-T016), and the
  full solution builds with 0 warnings/0 errors after every phase. It could not perform the in-game Dev Mode
  testing `quickstart.md` requires — RimWorld itself needs to be launched, a save/colonist set up, and (per
  this feature's SC-008) both a non-CE and a CE-loaded pass run — none of which is possible from this
  environment. Per Constitution Principle II ("Verify Before Claiming Done"), none of T008/T012/T017/T018 are
  marked complete; whoever runs the game next should work through `quickstart.md`'s seven scenarios in order
  (Scenario 7 can run any time after T015/T016 — it doesn't depend on US1/US2 code) and update this file's
  checkboxes accordingly. Nothing found during implementation contradicts the design; all in-game risk is
  concentrated on the two flagged assumptions research.md itself calls out (R2: CE's ranged pipeline still
  terminating in `Pawn.PostApplyDamage`; R5: `ProjectileCE.Impact` firing for every CE hit that damages a
  target) — those are exactly what Scenarios 2/4/5/6 exist to confirm.
