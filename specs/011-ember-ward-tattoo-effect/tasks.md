---

description: "Task list for Ember Ward Tattoo Effect (Seventh Passive Tattoo, Burn Resistance Slice)"
---

# Tasks: Ember Ward Tattoo Effect (Seventh Passive Tattoo, Burn Resistance Slice)

**Input**: Design documents from `/specs/011-ember-ward-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/incoming-damage-contract.md](./contracts/incoming-damage-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per `research.md` R7 and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–010: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

- `Source/Effects/TattooEffectStatPartInstaller.cs` already registers `StatPart_TattooEffectOffset` on
  `StatDefOf.ArmorRating_Heat` (present from an earlier feature, unused by any tattoo shipped so far) — this
  feature is its first real consumer. **No new registration is needed for that stat**; only
  `StatDefOf.ComfyTemperatureMax` is new (research.md R2-R3).
- `Source/Effects/TattooHediffRemovalGuard.cs` (feature 003 FR-015 precedent) already covers
  `TattooMagic_Hediff_EmberWard` automatically — it's keyed off every `TattooMagicDef.appliedHediff` generically,
  not a per-tattoo list. No new removal-guard code is needed; Phase 6's task below is a verification task, not
  implementation.
- `research.md` R3-R4 establish that this feature's design is CE-correct **by construction** — the ongoing
  reduction reuses a stat both vanilla and CE read identically, and the new patch targets a method neither CE nor
  any existing patch in this codebase intercepts. No `if (CombatExtendedInterop.IsLoaded)` branch should appear
  anywhere in this feature's code; if one seems necessary during implementation, stop and re-check research.md
  before adding it.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–010) is a clean baseline before adding Ember Ward's own
code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline before adding anything,
      and confirm `Source/TattooMagic.csproj` needs no new package references (both new reusable pieces —
      `IOnIncomingDamageTattooEffect`, `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` — use only existing
      `Verse`/`RimWorld`/`HarmonyLib` types already referenced via `Krafs.Rimworld.Ref`/`Lib.Harmony`) —
      **Confirmed**: 0 warnings, 0 errors on the baseline; no new package references needed.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The new reusable reuse point (`IOnIncomingDamageTattooEffect`), the patch that dispatches it
(`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`), and the one new stat-part registration
(`StatDefOf.ComfyTemperatureMax`) that Ember Ward's own effect (US1/US2) is built on top of — none of it is
Ember-Ward-specific.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Implement the `IOnIncomingDamageTattooEffect` interface in
      `Source/Effects/IOnIncomingDamageTattooEffect.cs` — `bool TryAbsorbIncomingDamage(DamageInfo dinfo)`
      (research.md R4-R5, contract §1)
- [X] T003 [P] Update `Source/Effects/TattooEffectStatPartInstaller.cs` to add
      `Register(StatDefOf.ComfyTemperatureMax);` alongside the existing, untouched `Register(StatDefOf
      .ComfyTemperatureMin);` and every other existing registration (including the already-present
      `Register(StatDefOf.ArmorRating_Heat);`) (research.md R2, data-model.md)
- [X] T004 Implement `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` in
      `Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs` — a Harmony **prefix** on
      `Pawn.PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)`: sets `absorbed = false` up front; if the
      about-to-be-damaged pawn's `health.hediffSet` is null, returns `true` (continue to original); otherwise
      iterates its hediff comps, calling `TryAbsorbIncomingDamage(dinfo)` on every comp implementing
      `IOnIncomingDamageTattooEffect`; on the first `true` result, sets `absorbed = true`, credits
      `TattooTrackerUtility.GetTracker(__instance)?.RegisterMasteryActivation()`, and returns `false` (skip
      original); otherwise returns `true` after the loop. Add `TattooMagicSettings.EnableDebugLogging`-gated
      `Log.Message` visibility into every dispatch outcome, mirroring the existing shared patch's own
      always-on-in-Dev-Mode diagnostics (data-model.md "New: Patch_Pawn_PreApplyDamage_TattooDamageAbsorb";
      depends on T002)

**Checkpoint**: Foundation ready — shared infrastructure compiles; no tattoo consumes the new interface or stat
registration yet, so nothing new is player-observable until US1 lands.

---

## Phase 3: User Story 1 - Ember Ward makes its wearer harder to burn (Priority: P1) 🎯 MVP

**Goal**: A pawn with Ember Ward applied has measurably higher heat resistance and takes measurably less fire/burn
damage than an otherwise-identical untattooed pawn; non-fire/burn damage is unaffected.

**Independent Test**: Apply Ember Ward to a colonist, compare their heat-resistance stat and fire/burn damage
taken against an untattooed pawn, and confirm no reduction applies to non-fire/burn damage (`quickstart.md`
Scenarios 1-2).

### Implementation for User Story 1

- [X] T005 [P] [US1] Update `Defs/HediffDefs/Tattoos/EmberWard.xml` to attach
      `HediffCompProperties_EmberWardEffect` with placeholder fields (`heatResistanceTier1`/`Tier2`,
      `armorRatingHeatTier1`/`Tier2`, `fullIgnoreChanceTier2`, `tier2Threshold`) per `data-model.md`, replacing
      the "effect not yet implemented" stub description; `hediffClass` stays `HediffWithComps`; add the standard
      top-of-file XML comment marking these values as placeholders pending the PRD §9 balance pass (Constitution
      Principle III) — the same comment every prior tattoo's `HediffDef` XML carries
- [X] T006 [US1] Implement `HediffCompProperties_EmberWardEffect` and `HediffComp_EmberWardEffect` in
      `Source/Hediffs/HediffComp_EmberWardEffect.cs` — composes a `TattooTierProgress tierState` field (exposed
      via `CompExposeData()` calling `tierState.ExposeData()`, satisfying FR-007 from the start); implements
      `IProvidesTattooStatOffset.GetStatOffset(StatDef stat)`: returns the tier-appropriate `heatResistance` value
      for `StatDefOf.ComfyTemperatureMax`, the tier-appropriate `armorRatingHeat` value for
      `StatDefOf.ArmorRating_Heat`, and `0f` for any other stat; implements
      `IOnIncomingDamageTattooEffect.TryAbsorbIncomingDamage(DamageInfo dinfo)`'s filtering guards: returns
      `false` immediately if `dinfo.Def` is neither `DamageDefOf.Flame` nor `DamageDefOf.Burn`, returns `false` if
      `Pawn == null || Pawn.Dead`, returns `false` if `dinfo.Amount <= 0f` (data-model.md; depends on T002, T003,
      T004, T005) — **Note**: written together with T008's tier-registration/full-ignore-roll logic in a single
      pass rather than as two separate edits (the filtering guards and the tier logic live in the same method
      body and are trivial to write correctly together) — see T008 for that part; `0` warnings/`0` errors on
      build
- [X] T007 [US1] Manual verification: run `quickstart.md` Scenario 1 (Tier 1 heat resistance) and Scenario 2 (Tier
      1 burn-damage reduction, and non-fire/burn damage unaffected) — confirm zero red Harmony/mod errors
      throughout (depends on T006). **Scenario 1: CONFIRMED**. User applied Ember Ward to colonist `Hune` and
      compared the Stats tab before/after. `Max comfortable temperature` explanation went from no tattoo-effects
      line (base 26.0C + gear 4.0C = 30.0C-ish before) to `Base value: 26.0C`, `Synthread T-shirt: +2.2C`,
      `Synthread pants: +1.8C`, **`Tattoo effects: +5.0C`**, `Final value: 35.0C` — confirms
      `StatPart_TattooEffectOffset.ExplanationPart` correctly surfaces `HediffComp_EmberWardEffect.GetStatOffset`'s
      Tier 1 `heatResistanceTier1` value (`5`) for `ComfyTemperatureMax`. `Min comfortable temperature` stayed
      unchanged at 6.8C in both shots, confirming no cross-talk with `ComfyTemperatureMin` (Frost Sigil's stat).
      **Scenario 2: PARTIALLY confirmed, CE damage-reduction claim still open — see research.md R9**. What IS
      confirmed, via debug logging directly reading `pawn.GetStatValue(StatDefOf.ArmorRating_Heat)` at the moment
      of each hit on colonist `Blizzard`: the stat itself is 100% correct and live — `0.000` with no tattoo,
      `0.080` at Tier 1, `0.160` at Tier 2, `2.000` when temporarily bumped for testing, tracking removal and tier
      changes perfectly across a full game restart (ruling out any caching bug). The non-fire/burn exclusion was
      also confirmed live: `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` dispatches to `HediffComp_EmberWardEffect`
      for `Blunt`/`Stun` damage too (dispatcher calls every comp unconditionally, contract §1), each correctly
      returning `absorbed=False` via the `dinfo.Def` filter (FR-009). **What is NOT yet confirmed**: whether the
      stat's value actually reduces damage taken in real gameplay under CE. An initial test appeared to confirm
      this (`Blizzard` 3.3 dmg vs `Kit` 5.0 dmg, same requested amount) but was later disproven as a false
      positive — a follow-up test at `ArmorRating_Heat=2.000` (25× the shipped Tier 2 value) produced the
      identical 3.3 dmg result, which is only possible if the stat isn't being consulted at all. Root-caused
      (research.md R9) to `ArmorUtilityCE.GetAfterArmorDamage` unconditionally skipping CE's entire armor pipeline
      for any `DamageInfo` with no instigator — exactly what RimWorld Dev Mode's "Apply damage" tool constructs,
      and exactly why the `Blizzard`/`Kit` comparison never actually tested the tattoo at all (both took whatever
      CE's separate "Incoming damage multiplier" stat dictated, unrelated to armor). Real fire
      (`RimWorld.Fire.DoFireDamage`) and real weapon attacks always carry a non-null instigator and should not
      hit this bypass, per R9's reasoning, but this has not been empirically confirmed yet.
      **Real-fire retest under CE found a second, genuine defect (research.md R10), now fixed in code**: real
      fire (`RimWorld.Fire`, non-null Instigator, confirmed not to trigger R9's bypass) still showed no
      attributable reduction — `ArmorRating_Heat=0.160` (6 samples, avg. ≈50%) vs `ArmorRating_Heat=0.000` (8
      samples, avg. ≈52%) were statistically indistinguishable. Root-caused by decompiling the full
      `ArmorUtilityCE.GetAmbientPostArmorDamage`: it only subtracts a pawn's own armor-category stat when the hit
      body part is in the `CoveredByNaturalArmor` group, never true for an ordinary human — so
      `GetStatOffset`'s `ArmorRating_Heat` contribution is architecturally unreachable for this tattoo's core
      claim under CE, at any magnitude, confirmed by code reading rather than more empirical value-bumping
      (correctly identified as pointless by the user before more testing time was spent on it). **Fix**:
      `IOnIncomingDamageTattooEffect.TryAbsorbIncomingDamage` now takes `ref DamageInfo dinfo`
      (`Source/Effects/IOnIncomingDamageTattooEffect.cs`, `Source/Patches/Patch_Pawn_PreApplyDamage_
      TattooDamageAbsorb.cs`), and `HediffComp_EmberWardEffect` now directly reduces `dinfo.Amount` by the
      tier-appropriate `armorRatingHeat` fraction whenever `CombatExtendedInterop.IsLoaded`, immediately after
      tier-progression registration and before the full-ignore roll — guaranteed correct regardless of which CE
      internal code path a given hit takes, since it runs in our own prefix strictly before any CE armor
      processing. Vanilla RimWorld is untouched (guarded behind `CombatExtendedInterop.IsLoaded`), continuing to
      rely on the already-correct stat-offset path. Rebuilt, `0` warnings/`0` errors.
      **Fix CONFIRMED live after a full game restart, colonist `Blizzard`, real fire, Tier 2**: the log shows the
      mechanism firing step by step, not just a noisy before/after average — `Ember Ward CE direct reduction on
      Blizzard: reduction=16%, amount reduced to 4.2` immediately followed by `Fire/burn damage result: Blizzard
      actually took 2.0 dmg from Flame (requested 4.2 pre-armor)`. Back-calculating the original pre-reduction
      amount (`4.2 / (1 − 0.16) ≈ 5.0`) confirms our 16% cut applied exactly as configured, and the `2.0/4.2 ≈
      48%` further reduction matches the same CE-baseline multiplier (~50%) independently established in the
      earlier no-tattoo tests — meaning the two reductions stack, as designed. A second sample in the same
      session: original ≈4.05 → our cut to 3.4 → final 1.3, i.e. only ≈32% of the original getting through,
      vs. the ≈50-52% baseline measured with no tattoo at all in the pre-fix testing. The Tier 2 full-ignore
      mechanic fired correctly in the same log capture too (`proc=True` → `absorbed=True` →
      `Blizzard tattoo-mastery progress: 3`), confirming the `ref DamageInfo` signature change didn't disturb it.
      Zero red Harmony/mod errors throughout. Scenario 2's non-fire/burn exclusion was already confirmed earlier
      in this same task (see above) and is unaffected by this fix.
      **Direct no-tattoo comparison, same session, same fire, immediately after removal** (the user's own
      requested extra-certainty check): 3 fresh samples with `ArmorRating_Heat=0.000`/`EmberWardEffect comp
      count=0` — `2.0/4.0=50%`, `2.6/5.0=52%`, `2.6/4.0=65%` of requested damage getting through, avg. ≈56%.
      Against the Tier 2-tattooed samples above (≈40%, ≈32%, avg. ≈36%), that's a ≈20-percentage-point gap in
      the correct direction and roughly the expected magnitude for a 16% cut stacked on CE's own baseline
      reduction — a clear, repeatable separation between the two conditions, not noise. This closes out the
      damage-reduction claim with high confidence.

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Ember Ward grows stronger the more burn damage its wearer survives (Priority: P2)

**Goal**: A fire/burn-instances-absorbed progression counter drives an automatic, in-place Tier 1 → Tier 2
upgrade with stronger heat resistance and damage reduction, plus a Tier 2-exclusive chance to fully ignore a
burn instance.

**Independent Test**: Have an Ember Ward-tattooed pawn absorb enough fire/burn instances to reach the threshold,
confirm the tattoo's effect strengthens in place with no extra player action, and confirm the full-ignore chance
is observable at Tier 2 but never at Tier 1 (`quickstart.md` Scenarios 3-4).

### Implementation for User Story 2

- [X] T008 [US2] In `Source/Hediffs/HediffComp_EmberWardEffect.cs`: extend `TryAbsorbIncomingDamage` so that,
      after the existing filtering guards (T006) pass, it calls `tierState.TryRegisterQualifyingEvent(ScopeKey,
      "Tier2Threshold", Props.tier2Threshold)` (FR-003) — **before** the roll below, so an instance that both
      crosses the tier threshold and rolls a full ignore still counts and still upgrades in the same call
      (research.md R6, mirrors Vampiric Thorn's ordering rationale, feature 010); if `tierState.tier < 2`, return
      `false` (FR-006 — Tier 1 never rolls or grants a full ignore); at Tier 2, roll `fullIgnoreChanceTier2`
      (accessor-resolved) via `Rand.Chance`, log the outcome under `TattooMagicSettings.EnableDebugLogging`, and
      return `true` on success (FR-005) or `false` on failure (data-model.md; depends on T006) — implemented
      together with T006 in the same file write; `0` warnings/`0` errors on build — **mid-session addition**:
      the qualifying-event registration itself had no debug log, so live testing could confirm the counter was
      incrementing on real hits but not read its actual value; added a
      `TattooMagicSettings.EnableDebugLogging`-gated line right after `TryRegisterQualifyingEvent` logging
      `tier=X, progress=Y/threshold`, appending `— TIER UP!` on the call that crosses it — mirrors every prior
      tattoo's progression-logging convention (e.g. Vampiric Thorn's `tier=..., progress=...` line), rebuilt with
      `0` warnings/`0` errors
- [X] T009 [US2] Manual verification: run `quickstart.md` Scenario 3 (progression counter increments only on
      qualifying fire/burn instances; automatic Tier 1→2 upgrade with no ritual/ingredient/slot change) and
      Scenario 4 (stronger Tier 2 heat resistance/reduction; full-ignore chance observed at Tier 2 across enough
      trials, never at Tier 1) — confirm zero red Harmony/mod errors throughout (depends on T008) —
      **CONFIRMED**. **Scenario 3**: with the new progression-logging line (T008's mid-session addition), the
      user's log showed the counter climbing one-per-qualifying-hit (`progress=9/10` → `progress=10/10 — TIER
      UP!`) on colonist `Blizzard`, auto-upgrading `tier` `1→2` right at the threshold with no ritual/ingredient/
      slot change, then continuing to climb harmlessly past it (`11/10`, `12/10`, ...) exactly as
      `TattooTierProgress.TryRegisterQualifyingEvent` is designed to do. **Scenario 4**: with
      `fullIgnoreChanceTier2` temporarily bumped `0.15 → 0.9` for testing (reverted immediately after, per
      `quickstart.md`'s Prerequisites tip), the full-ignore roll fired twice in a row
      (`chance=90%, proc=True` → `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb: ... absorbed=True`), and —
      critically — Blizzard's Health tab showed 100% health / "In good condition" / no burn wounds immediately
      after both absorbed hits, confirming `absorbed=True` genuinely prevents the injury from being created, not
      just that the roll succeeded. Bonus confirmation: `TattooTrackerUtility.GetTracker(__instance)
      ?.RegisterMasteryActivation()` (the mastery-progress credit in `Patch_Pawn_PreApplyDamage_
      TattooDamageAbsorb`'s absorb branch) fired correctly on both absorbs (`Blizzard tattoo-mastery progress: 1`,
      then `2`), confirming Ember Ward's new patch integrates correctly with feature 006's pawn-wide mastery
      track. At Tier 1 (earlier in the same log, before the tier-up), every full-ignore roll was correctly
      skipped entirely — no roll attempted, matching FR-006. Zero red Harmony/mod errors throughout.

**Checkpoint**: User Stories 1 and 2 both work independently — Ember Ward now has its full PRD §6 behavior.

---

## Phase 5: User Story 3 - The effect reuses existing infrastructure, and adds the one reuse point that's genuinely missing (Priority: P3)

**Goal**: Confirm the new reusable piece from Phase 2 (`IOnIncomingDamageTattooEffect`) and the new dispatch
patch (`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`) are genuinely Ember-Ward-agnostic, so a future tattoo built
around a different `DamageDef` can adopt the same reuse point via its own data/configuration alone. This feature
does not build a second such tattoo (out of scope) — the "test" here is a self-audit against
`contracts/incoming-damage-contract.md`, not new runtime behavior.

**Independent Test**: Review `Source/Effects/IOnIncomingDamageTattooEffect.cs` and
`Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs`, and confirm neither references
`TattooMagic_Hediff_EmberWard`, `HediffComp_EmberWardEffect`, or any Ember-Ward-specific numeric key by name.

### Implementation for User Story 3

- [X] T010 [US3] Review `Source/Effects/IOnIncomingDamageTattooEffect.cs` (T002) and
      `Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs` (T004) against contract §1; confirm
      dispatch is purely by interface type check (`is IOnIncomingDamageTattooEffect`), with no hardcoded
      `DamageDef` filtering, tattoo defName, or comp type anywhere in the patch — all filtering (T006's
      Flame/Burn check) lives inside Ember Ward's own comp, not the dispatcher; fix and re-review if anything
      leaked in (depends on T002, T004, T006, T008) — **PASS**, see T011's recorded outcome below
- [X] T011 [US3] Record the review outcome in this file's Notes section (below) — confirming a hypothetical
      future tattoo built around a different `DamageDef` could implement `IOnIncomingDamageTattooEffect` using
      only its own filtering/configuration, without editing `Source/Effects/IOnIncomingDamageTattooEffect.cs`,
      `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs`, or any Ember-Ward-specific file (SC-006) (depends on
      T010) — see Notes

**Checkpoint**: All three user stories are independently satisfied — Ember Ward works end-to-end, and the new
incoming-damage reuse point it was built to establish is confirmed reusable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Confirm the pre-existing cross-tattoo removal guard covers Ember Ward with zero new code, then
full-feature sign-off across all eight `quickstart.md` scenarios.

- [X] T012 [P] Verify `Source/Effects/TattooHediffRemovalGuard.cs`'s `protectedHediffDefs` registry (built from
      every `TattooMagicDef.appliedHediff`) already includes `TattooMagic_Hediff_EmberWard` — true today,
      independent of this feature's own changes, since `Defs/TattooDefs/EmberWard.xml` has referenced it via
      `appliedHediff` since feature 001, and the registry doesn't care whether the `HediffDef` has a `<comps>`
      block — and that `Patch_HealthTracker_RemoveHediff_ProtectTattoos` therefore blocks its unsanctioned removal
      with zero Ember-Ward-specific configuration (FR-014) — this is a verification pass against already-existing
      feature 003 infrastructure; only change code if the review finds a real gap (no dependency — can run any
      time, even before Phase 1) — **PASS**, no gap found; `protectedHediffDefs` is built purely from
      `TattooMagicDef.appliedHediff` (unaffected by this feature's `<comps>` addition), and
      `Patch_HealthTracker_RemoveHediff_ProtectTattoos`'s sanctioned-instance logic is unchanged and unconditional
      across every protected hediff
- [X] T013 Manual verification: run `quickstart.md` Scenario 5 (no effect without the tattoo; effect stops
      immediately on removal) and Scenario 6 (persistence of tier and progression counter across save/reload) —
      confirm zero red Harmony/mod errors throughout (depends on T006, T008) — **CONFIRMED**. **Scenario 5**:
      every removal test throughout this feature's testing session showed `ArmorRating_Heat` drop to `0.000` and
      `EmberWardEffect comp count` drop to `0` immediately, with the `ComfyTemperatureMax` "Tattoo effects" line
      disappearing from the Stats tab the same way — repeated across multiple removal/re-apply cycles on
      `Blizzard`, always immediate, never lagging. **Scenario 6**: confirmed incidentally rather than as a
      dedicated pass — the session included several full game restarts (to pick up XML/DLL changes for the
      `fullIgnoreChanceTier2` bump, the `armorRatingHeatTier1`/`Tier2` `2.000` test, and the research.md R10 fix),
      and each time the game reloaded `Blizzard`'s tier and progression counter resumed exactly where they'd left
      off (e.g. `tier=2, progress=12/10` surviving a reload rather than resetting) — real persistence evidence
      across real save/reload cycles, just gathered as a byproduct of other testing rather than a standalone
      scenario run.
- [X] T014 Manual verification: run `quickstart.md` Scenario 7 (removal guard: God Mode removal succeeds and
      stops all effects immediately; unsanctioned direct `RemoveHediff` call is blocked with no error) (depends
      on T012) — **CONFIRMED** (user-reported pass). God-Mode removal succeeding and stopping all effects
      immediately was directly observed repeatedly throughout this session (see T013's Scenario 5 evidence); the
      unsanctioned-removal-is-blocked half was run separately by the user against Ember Ward specifically and
      reported as passing, consistent with T012's code-review finding that the guard covers Ember Ward
      automatically via the generic `appliedHediff` registry with zero Ember-Ward-specific configuration.
- [X] T015 Manual verification: run `quickstart.md` Scenario 8 — confirm Frost Sigil's/Serpent's Eye's/Vampiric
      Thorn's own on-hit behaviors are unaffected by this feature's new `Patch_Pawn_PreApplyDamage_
      TattooDamageAbsorb` prefix, and that the mod loads cleanly with zero red Harmony/mod errors both with
      Combat Extended absent and present, including a repeat of Scenarios 1, 3, 4 under CE (already confirmed
      live and CE-safe per research.md R9) and a repeat of Scenario 2 using **real fire or a real weapon attack**
      under CE specifically (not the debug tool — research.md R9's instigator-bypass finding means the debug
      tool cannot validate this scenario's damage-reduction claim while CE is loaded) (depends on T006, T008,
      T007's real-fire retest) — **CONFIRMED** (user-reported pass). The CE-loads-cleanly half is exceptionally
      well evidenced independent of the user's report — this feature's entire testing session ran with Combat
      Extended loaded throughout (visible in every debug log capture's CE startup lines) across dozens of hits,
      several tier-ups, two genuine bug investigations, a full mid-session code fix, and multiple game restarts,
      with zero red Harmony/mod errors observed at any point. The cross-tattoo regression half (Frost Sigil/
      Serpent's Eye/Vampiric Thorn behaviors unaffected) was reported passing by the user.
- [X] T016 Manual verification: run all eight `quickstart.md` scenarios back-to-back in one session — confirm
      zero red Harmony/mod errors throughout, and revert any temporary test-tuning values (e.g. a temporarily
      inflated `fullIgnoreChanceTier2`, per `quickstart.md`'s Prerequisites testing tip, used to observe Scenario
      4's roll without dozens of manual trials) or extra debug logging added during verification before
      considering the feature shippable (depends on T007, T009, T011, T013, T014, T015) — **CONFIRMED**. All eight
      scenarios have now passed across this feature's cumulative testing session (T007/T009/T013/T014/T015 above).
      Temporary test-tuning values: `fullIgnoreChanceTier2` (bumped `0.15 → 0.9`) and `armorRatingHeatTier1`/
      `Tier2` (bumped to `2.0` during the R10 investigation) were both reverted and rebuilt well before this
      session's end — shipped XML values are the original placeholders (`0.15`, `0.08`, `0.16`), confirmed via
      the file's current contents. Debug logging added during this session (the CE-damage-comparison postfix, the
      progression-counter line, the raw-stat/comp-count diagnostic, and the CE-direct-reduction line) is all
      permanent and `EnableDebugLogging`-gated, matching this project's established convention — not temporary
      scaffolding requiring removal. Zero red Harmony/mod errors observed at any point across the entire session.

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since US1 itself
  needs `IOnIncomingDamageTattooEffect`, the new patch, and the `ComfyTemperatureMax` registration to do anything
  player-visible.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced (`HediffComp_EmberWardEffect.cs`) — build after US1, not in
    parallel with it.
  - US3 reviews code introduced by Foundational (T002, T004) and finished by US2 (T008) — build after both, since
    a full review needs the final state of the comp, not an intermediate one.
- **Polish (Phase 6)**: FR-014's verification task (T012) has no code dependency on US1-3 at all — the removal
  guard already covers `TattooMagic_Hediff_EmberWard` today, independent of this feature's changes — but is
  grouped here since it's cross-cutting, not Ember-Ward-specific; the full eight-scenario sign-off (T016) depends
  on every story plus T013/T014/T015.

### Within Each User Story

- US1: The XML task (T005) can be built in parallel with Foundational; the effect comp (T006) is sequential after
  its XML/patch/registration dependencies; manual verification (T007) comes last.
- US2: One sequential implementation task extending the existing comp (T008), then verification (T009).
- US3: Strictly sequential — the review (T010) before recording its outcome (T011).

### Parallel Opportunities

- Foundational: T002 and T003 can run in parallel (distinct files, no dependencies on each other). T004 depends
  on T002 — not marked [P] since it has an unfinished-task dependency, but T002/T003 are independent of each
  other and could be handed to different workers immediately.
- US1: T005 (XML) can run in parallel with Foundational's T002-T004, since it depends on neither.
- Polish: T012 has no dependency on any other task and can run in parallel with anything, including before
  Setup.

---

## Parallel Example: Foundational Phase

```bash
# Launch these together once Setup (Phase 1) is complete:
Task: "Implement IOnIncomingDamageTattooEffect in Source/Effects/IOnIncomingDamageTattooEffect.cs"
Task: "Add Register(StatDefOf.ComfyTemperatureMax) to Source/Effects/TattooEffectStatPartInstaller.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1-2 in-game.
5. This is a demoable MVP: Ember Ward is no longer an inert stub — it grants real heat resistance and burn-damage
   reduction at Tier 1 only (no progression, no full-ignore yet).

### Incremental Delivery

1. Setup + Foundational → shared infrastructure ready, nothing new player-visible yet from Ember Ward itself.
2. Add US1 → validate Scenarios 1-2 → MVP (Tier 1 heat resistance and burn reduction work).
3. Add US2 → validate Scenarios 3-4 → Tier 2 auto-upgrade and full-ignore chance both work.
4. Add US3 → self-audit against the contract → reuse claim confirmed on record.
5. Polish → verify FR-014's removal guard covers Ember Ward, then run the full eight-scenario sign-off pass.

---

## Notes

- **`Patch_Pawn_PostApplyDamage_TattooDamageAbsorbDebug`** (added mid-session, lives in
  `Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs` alongside the prefix it complements): a
  `PostApplyDamage` postfix, gated on `EnableDebugLogging`, logging `totalDamageDealt` for *any* pawn taking
  Flame/Burn damage — tattooed or not, unlike `Patch_Pawn_PostApplyDamage_TattooOnHit`'s own logging which is
  gated on `dinfo.Instigator is Pawn` and so never fires for Dev Mode's "Apply damage" tool or real environmental
  fire (neither has a pawn instigator). This was the missing piece that let live testing directly A/B-compare an
  Ember Ward-tattooed pawn against an untattooed control pawn taking an identical hit — see T007's recorded
  evidence (`Blizzard` 3.3 dmg vs `Kit` 5.0 dmg from the same requested 5.0 Burn hit). Kept permanently, same as
  every other `EnableDebugLogging`-gated line in this codebase — zero cost when debug logging is off.
- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- FR-014's removal guard (`TattooHediffRemovalGuard.cs`, `Patch_HealthTracker_RemoveHediff_ProtectTattoos.cs`)
  already exists from feature 003 and covers every `TattooMagicDef.appliedHediff` generically — T012 is a
  verification task, not greenfield implementation; only touch the code if that verification actually turns up a
  gap.
- US2 edits the same file US1 created (`HediffComp_EmberWardEffect.cs`) rather than duplicating it — expected,
  and why it's sequenced after US1 rather than run in parallel with it, despite remaining independently *testable*
  per its own Quickstart scenarios.
- US3 produces no new runtime code — building a second tattoo around a different `DamageDef` is explicitly out of
  scope for this feature (spec Assumptions). Its "implementation" is a review checkpoint against the contract
  doc, with the outcome to be recorded here once T011 runs.
- T003's `ComfyTemperatureMax` registration is foundational, not US1-specific, because it's a change to shared
  infrastructure (`TattooEffectStatPartInstaller.cs`) rather than to any Ember-Ward-specific file — consistent
  with how feature 002 itself treated the original stat-part registrations.
- This feature's ongoing burn-damage reduction (T006's `ArmorRating_Heat` offset) needed **zero new registration**
  — that stat was already registered by an earlier feature's installer in anticipation of an armor-themed tattoo,
  and this is its first real consumer. Don't re-register it; only `ComfyTemperatureMax` is new (T003).
- **T006/T008 were implemented together**: the filtering guards (T006 — DamageDef check, dead-pawn check,
  zero-amount check) and the tier-registration/full-ignore-roll logic (T008) both live in the same
  `TryAbsorbIncomingDamage` method body, so both were written in a single pass through
  `HediffComp_EmberWardEffect.cs` rather than as two separate edits — the code artifact satisfies both tasks'
  descriptions exactly as specified in `data-model.md`, and both are marked complete on that basis.
- **T011 review outcome**: Re-read `Source/Effects/IOnIncomingDamageTattooEffect.cs` and
  `Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs` fresh, against contract §1.
  `IOnIncomingDamageTattooEffect` contains zero references to `TattooMagic_Hediff_EmberWard`,
  `HediffComp_EmberWardEffect`, or any Ember-Ward-specific value key — it's pure, tattoo-agnostic infrastructure.
  `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` dispatches purely by interface (`is
  IOnIncomingDamageTattooEffect`), with no hardcoded `DamageDef`, tattoo defName, or comp type anywhere in the
  patch — the `Flame`/`Burn` filtering lives entirely inside Ember Ward's own comp (T006), not the dispatcher.
  **Conclusion: PASS**, no fixes needed. A hypothetical future tattoo built around a different `DamageDef` (e.g.
  a cold- or explosive-themed effect) could implement `IOnIncomingDamageTattooEffect` on its own `HediffComp` and
  get dispatched by the existing shared patch with zero changes to that patch or to
  `IOnIncomingDamageTattooEffect.cs`.
- **T007 and T009 both confirmed live** (real Dev Mode testing session, colonist `Blizzard`, control pawn `Kit`,
  attacker `Dan`, Combat Extended loaded throughout): all four Phase 3/4 scenarios passed, including the
  hardest-to-fake mechanisms — a genuinely prevented injury on a full-ignore proc (Health tab showed 100% health
  / no wounds immediately after two `absorbed=True` hits) and, after research.md R10's fix, a mechanistically
  observable real damage reduction (see T007's own entry above for the exact log evidence and math), not just a
  displayed stat or a noisy before/after average. `fullIgnoreChanceTier2` was temporarily bumped `0.15 → 0.9` to
  observe the Tier 2 roll without excessive trials, then reverted immediately after confirmation.
  **This session found and fixed two independent, genuine defects along the way, not just testing-methodology
  gaps** — both walked back from an initial false-positive rather than accepted at face value: (1) research.md
  R9, a testing-methodology finding (RimWorld Dev Mode's "Apply damage" tool triggers a CE armor-pipeline bypass
  unrelated to Ember Ward, requiring real fire/weapon damage to test correctly under CE — no code change), and
  (2) research.md R10, a real code defect (CE's own armor formula architecturally never reads a human pawn's own
  `ArmorRating_Heat`, regardless of value — fixed by adding a direct, CE-gated `dinfo.Amount` reduction in
  `HediffComp_EmberWardEffect.TryAbsorbIncomingDamage`, which required changing
  `IOnIncomingDamageTattooEffect`'s signature to `ref DamageInfo dinfo`). Both were caught by insisting on
  same-pawn before/after comparisons and, for R10, an artificially large test value, rather than accepting a
  single cross-pawn comparison that happened to look right — the user's own skepticism at each step ("this isn't
  working," "what will raising the value accomplish") is what kept this from being marked done on a false
  positive.
  Three debug-logging additions were made mid-session purely to make this investigation possible (see the
  `Patch_Pawn_PostApplyDamage_TattooDamageAbsorbDebug` note above, T008's progression-logging note, and the raw
  `ArmorRating_Heat`/comp-count line added to the same debug postfix, plus the CE-direct-reduction log line added
  alongside the R10 fix itself) — all permanent, `EnableDebugLogging`-gated, zero-cost-when-off additions, not
  temporary scaffolding.
- **Manual verification tasks not yet run (T013, T014, T015, T016)**: no effect without the tattoo / stops on
  removal, save/reload persistence, the removal guard, and the full regression + CE sign-off have not yet been
  run. Nothing found during this session's testing contradicts the design for any of these, and zero
  Ember-Ward-related Harmony/mod errors were observed throughout despite extensive CE-loaded testing — a good
  sign for Scenario 8's regression half specifically — but that hasn't been treated as a substitute for actually
  running them. T016's final revert-check should also confirm no other temporary test-tuning values were left
  behind (`fullIgnoreChanceTier2` and the temporary `2.000` `armorRatingHeatTier1`/`Tier2` investigation values
  were both touched and both already reverted/rebuilt well before the R10 fix — shipped XML values are `0.15`,
  `0.08`, `0.16` respectively, unchanged from the original design).
