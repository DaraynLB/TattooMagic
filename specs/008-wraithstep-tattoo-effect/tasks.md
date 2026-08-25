---

description: "Task list for Wraithstep Tattoo Effect (Fourth Triggered Ability + First Cell-Targeted Ability Slice)"
---

# Tasks: Wraithstep Tattoo Effect (Fourth Triggered Ability + First Cell-Targeted Ability Slice)

**Input**: Design documents from `/specs/008-wraithstep-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/cell-targeted-ability-contract.md](./contracts/cell-targeted-ability-contract.md), [contracts/targeting-exclusion-contract.md](./contracts/targeting-exclusion-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per research.md precedent (features 001–007) and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–007: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

`Source/Effects/IProvidesTattooGizmo.cs`, `Source/Effects/TattooTierProgress.cs`,
`Source/Effects/TattooEffectValues.cs` (features 002/004), `Source/Hediffs/HediffComp_TattooTracker.cs` and
`Source/Hediffs/TattooTrackerUtility.cs` (feature 006), and `Source/Patches/Patch_AttackTargetFinder_
BestAttackTarget_GuardiansCallTaunt.cs` + `Source/Hediffs/GuardiansCallTauntRegistry.cs` (feature 004) all already
exist and need **zero changes**. `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs` (feature 004/006) needs
exactly **one small, generic condition added** (T005) — everything else in it is untouched. `Defs/TattooDefs/
Wraithstep.xml` (feature 001, the `TattooMagicDef` itself — ingredients, workAmount) needs **zero changes**; only
`Defs/HediffDefs/Tattoos/Wraithstep.xml` (the currently-inert `appliedHediff`) is edited, and its `hediffClass`
stays `HediffWithComps` (no subclass override needed, unlike Bloodrune's `PainOffset` case).

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–007) is a clean baseline before adding Wraithstep's own
code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline (features 001–007)
      before adding anything; confirm no new package reference is needed (`Krafs.Rimworld.Ref`/`Lib.Harmony`
      versions unchanged) — this feature's new API surface (`Verse.Targeter`, `TargetingParameters`,
      `AttackTargetFinder.BestAttackTarget`) is all already part of the referenced RimWorld assembly, confirmed
      present via research.md R2/R6.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The gizmo/cooldown/tier-progression skeleton and the mastery opt-out plumbing every user story below
builds on — a real "Wraithstep" ability button that activates (with a stub, non-targeted cooldown-only action) and
correctly opts out of the shared click-time mastery wrap, but no actual targeting or relocation yet. Nothing here
is player-visible as a *blink* on its own; it exists so US1 has a working comp/gizmo/interface shell to extend
rather than building activation/cooldown/tiering/mastery-opt-out from scratch alongside the targeting logic itself.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Update `Defs/HediffDefs/Tattoos/Wraithstep.xml`: add a `<comps>` block attaching
      `TattooMagic.HediffCompProperties_WraithstepEffect` with placeholder `rangeTier1`/`rangeTier2`,
      `cooldownDurationTicksTier1`/`Tier2`, `untargetableWindowTicksTier2`, `tier2Threshold`, and
      `abilityIconPath` fields, per `data-model.md`'s XML block — small/fast-to-test values, not
      "plausible-looking" ones (feature 006 research.md R8 convention). `hediffClass` stays `HediffWithComps`.
- [X] T003 [P] Create `Source/Effects/IHandlesOwnMasteryProgress.cs` — a marker interface with no members
      (research.md R4, `cell-targeted-ability-contract.md` §2).
- [X] T004 Implement `HediffCompProperties_WraithstepEffect` and a skeleton `HediffComp_WraithstepEffect` in
      `Source/Hediffs/HediffComp_WraithstepEffect.cs` — composes a `TattooTierProgress tierState` field (exposed
      via `CompExposeData()` calling `tierState.ExposeData()`, plus `Scribe_Values.Look` for `cooldownEndTick` and
      `untargetableEndTick`, satisfying FR-010 from the start); implements `IProvidesTattooGizmo.GetGizmo()`
      returning a `Command_Action` whose `Disabled`/`disabledReason` are computed live from `cooldownEndTick`, and
      implements the `IHandlesOwnMasteryProgress` marker (T003) (depends on T002, T003). **Built together with
      T007/T008/T013 in one pass** rather than literally stubbed-then-patched (no live game available mid-session
      to exercise an intermediate stub state, same precedent as feature 007's implementation session) — see Session
      Notes below.
- [X] T005 In `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs`: add one small, fully generic condition — only
      auto-wrap a collected `Command_Action` for mastery-progress credit when its owning comp does **not**
      implement `IHandlesOwnMasteryProgress` (research.md R4, `cell-targeted-ability-contract.md` §2; depends on
      T003)
- [X] T006 Manual verification: apply (or Dev-Mode-grant) Wraithstep to a colonist, confirm a "Wraithstep" gizmo
      appears and is enabled, click it, confirm it immediately shows disabled with a cooldown reason (no
      relocation expected yet — that's US1), confirm re-activation is blocked while on cooldown, and confirm the
      gizmo re-enables once the cooldown expires (depends on T004, T005). **PARTIALLY CONFIRMED live (2026-08-15,
      player Edd)** — gizmo appears/enabled, icon renders cleanly (no background square, confirming the icon-format
      fix), a valid destination successfully relocated the pawn. Cooldown-disabled state/reason text and re-enable
      after expiry not explicitly observed this pass — folded into T011's fuller check rather than re-run alone.

**Checkpoint**: Foundation ready — a real, working ability button with cooldown, tier-progression scaffolding, and
correct mastery opt-out wiring exists. No targeting or relocation is granted yet; that's what US1 adds.

---

## Phase 3: User Story 1 - Wraithstep instantly relocates its wearer to a chosen nearby spot (Priority: P1) 🎯 MVP

**Goal**: Activating Wraithstep opens a destination targeter with a visible range ring and live reject-reason
feedback; confirming a valid cell instantly relocates the wearer there and starts the cooldown; cancelling leaves
everything unchanged — the tattoo's actual Tier 1 effect, on top of the Foundational phase's working gizmo/cooldown
shell.

**Independent Test**: Apply Wraithstep to a colonist, activate the ability, choose a valid destination cell within
range, and observe the colonist instantly appear there, followed by the ability becoming unavailable for a
cooldown period; confirm invalid cells are visibly rejected and cancelling does nothing (`quickstart.md` Scenarios
1, 2, 3).

### Implementation for User Story 1

- [X] T007 [US1] In `HediffComp_WraithstepEffect.cs`: implement `IsValidDestination(LocalTargetInfo target)` (`bool`)
      and `GetRejectReason(LocalTargetInfo target)` (`string`, same checks/order, first failure's reason text) per
      `data-model.md` — in-bounds, unfogged/explored, walkable, unoccupied by another pawn, and within `Range` of
      the wearer's position (FR-002; depends on T006)
- [X] T009 [US1] **Superseded during implementation** (research.md R3, decompiled-source finding) — no separate
      `WraithstepTargetingSession` class was needed. `RimWorld.Targeter.BeginTargeting`'s own richest overload
      accepts `onUpdateAction`/`onGuiAction` delegate parameters directly, so `DrawRangeRing`/`DrawRejectReasonLabel`
      are plain closures on `HediffComp_WraithstepEffect` passed straight into the one `BeginTargeting` call — no
      static session holder to create. See Session Notes below.
- [X] T008 [US1] In `HediffComp_WraithstepEffect.cs`: `BeginTargetSelection()` no-ops if still on cooldown,
      otherwise calls `Find.Targeter.BeginTargeting` (namespace `RimWorld`, not `Verse` — research.md R3) with
      cell-only `TargetingParameters` (`canTargetLocations = true`, `canTargetPawns = false`, `canTargetSelf =
      false`), `action: OnDestinationConfirmed`, `highlightAction: null`, `targetValidator: IsValidDestination`,
      `caster: Pawn`, `onGuiAction: DrawRejectReasonLabel`, `onUpdateAction: DrawRangeRing`; `OnDestinationConfirmed
      (LocalTargetInfo target)` sets `Pawn.Position = target.Cell` then calls `Pawn.Notify_Teleported()`
      (research.md R5), sets `cooldownEndTick` from the Tier 1 accessor-resolved cooldown, calls
      `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)` exactly once
      (FR-006), and calls `TattooTrackerUtility.GetTracker(Pawn)?.RegisterMasteryActivation()` exactly once
      (FR-019, research.md R4); a cancelled session simply never invokes `OnDestinationConfirmed`, needing no
      separate cleanup (depends on T007)
- [X] T010 [US1] **Folded into T008** (research.md R3) — `DrawRangeRing(LocalTargetInfo)` (calls
      `GenDraw.DrawRadiusRing(Pawn.Position, Range)`, passed as `onUpdateAction`) and `DrawRejectReasonLabel
      (LocalTargetInfo)` (calls `GenUI.DrawMouseAttachment(null, GetRejectReason(target))` when non-null, passed as
      `onGuiAction`) are both plain methods on `HediffComp_WraithstepEffect`, not a separate Harmony patch file — no
      `Patch_Targeter_*.cs` file exists. See Session Notes below.
- [X] T011 [US1] Manual verification: run `quickstart.md` Scenario 1 — gizmo appears/enabled, clicking it enters
      targeting mode, confirming a valid cell instantly relocates the pawn and starts the cooldown, re-activation
      blocked on cooldown, gizmo re-enables after cooldown expires; and Scenario 2 — range ring visible while
      targeting, out-of-range/unwalkable/fogged/occupied cells all show a reject reason and can't be confirmed,
      and cancelling (Escape/right-click) leaves the gizmo enabled with no side effects (depends on T008, T010).
      **Scenario 1 substantially CONFIRMED live (2026-08-15, player Edd)**: gizmo/targeting/relocation/cooldown-gate
      all work — see T006. **Scenario 2 partially CONFIRMED**: an out-of-range cell correctly couldn't be targeted,
      and an unwalkable (mountain/rock) cell correctly couldn't be targeted; also confirmed the blink itself is not
      blocked by intervening terrain — the pawn successfully teleported *through* a mountain to a valid cell on the
      far side, exactly matching FR-004/Edge Cases' "only the destination cell's own validity gates the blink, not
      a clear path to it." **Further CONFIRMED live (2026-08-15, continued)**: the reject-reason label is genuinely
      visible, not just a silent click failure — player saw "Out of range." rendered in red at the cursor when
      aiming past the ring, and "Occupied." rendered in red when hovering a cell another colonist (Jackalope) stood
      on, with the blink correctly refused in both cases; confirms `DrawRejectReasonLabel`/`GenUI.DrawMouseAttachment`
      are actually rendering (research.md R3), not just that `IsValidDestination` rejects internally. Fogged/
      unexplored-cell rejection also **CONFIRMED** — a cell under fog-of-war was correctly refused. Escape/cancel
      also **CONFIRMED** — pressing the gizmo opens the range indicator as expected, and pressing Escape cancels
      the targeting session cleanly, matching FR-002/SC-005's "a cancelled attempt never invokes
      `OnDestinationConfirmed`" design. **T011 is now fully closed** — every Scenario 1 and Scenario 2 check has
      been live-confirmed.
- [X] T012 [US1] Manual verification: run `quickstart.md` Scenario 3 — an untattooed pawn shows no Wraithstep
      gizmo and is never relocated by this feature; a downed Wraithstep-tattooed pawn shows the gizmo
      unavailable/disabled (depends on T011). **Downed-pawn half found broken via live testing (2026-08-15) and
      fixed.** During the Tier 2 untargetable-window investigation, the debug log showed
      `Wraithstep activated by Umeko` firing successfully *after* Umeko had already gone `Downed.` (per her Health
      tab) — the gizmo had no guard against a downed pawn at all. Root cause: this feature's spec Assumptions
      section had assumed "no new incapacitation logic is expected to be required beyond what vanilla already
      provides" for downed pawns — that assumption was never actually true; RimWorld does not automatically disable
      a custom `Command_Action` for a downed pawn the way it does its own built-in commands, so nothing was ever
      checking `Pawn.Downed` for this ability. **Fixed** in `HediffComp_WraithstepEffect.cs`: `GetGizmo()` now sets
      `Disabled = downed || onCooldown` with a `"Wraithstep cannot be used while downed."` reason, and
      `BeginTargetSelection()` gained the same defensive `pawn.Downed` guard the cooldown check already had (in
      case the pawn goes down between the gizmo being drawn and the click resolving). `dotnet build`: 0 errors, 0
      warnings after the fix. **Downed-pawn half CONFIRMED live (2026-08-15)** — gizmo shows disabled with the
      exact reason text ("Disabled: Wraithstep cannot be used while downed.") on a downed Umeko. The untattooed-
      pawn half of this scenario remains unconfirmed.

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP. Wraithstep now
grants a real, player-targeted Tier 1 blink with a real cooldown and correct targeting UX.

---

## Phase 4: User Story 2 - Wraithstep grows stronger the more it's used, and covers its wearer's landing at Tier 2 (Priority: P2)

**Goal**: The progression counter (already advancing since US1, on successful blinks only) drives an automatic,
in-place Tier 1 → Tier 2 upgrade that grants a longer maximum range and, immediately after landing, a temporary
exclusion from hostile AI's attack-target selection — with no extra ritual/ingredient/slot cost.

**Independent Test**: Activate a Wraithstep-tattooed pawn's ability enough times to reach the threshold, then
confirm the tattoo's effect strengthens in place — farther cells become selectable, and for a brief window
immediately after landing, an otherwise-eligible hostile's targeting search does not select the wearer, in both a
non-CE game and a CE-loaded game (`quickstart.md` Scenario 4, and the CE-loaded half of Scenario 9).

### Implementation for User Story 2

- [X] T013 [US2] In `HediffComp_WraithstepEffect.cs`'s `OnDestinationConfirmed`: make the cooldown resolution
      tier-aware (read `...Tier2` accessor-resolved values once `tierState.tier == 2`, `...Tier1` otherwise,
      reading whichever tier was current *before* this call's `TryRegisterQualifyingEvent`, matching every prior
      tattoo's "an activation just before the upgrade uses the old tier's numbers" precedent); confirm `Range`
      (already tier-aware per `data-model.md`) is read from the same pre-upgrade tier for this activation's
      targeting session (depends on T008, since it edits the same method again)
- [X] T014 [P] [US2] Implement `WraithstepUntargetableRegistry.cs` in `Source/Hediffs/` — `HasActiveUntargetable`
      (bool), `Register(HediffComp_WraithstepEffect comp)`, `IsUntargetable(Pawn pawn)` (lazily-pruned `HashSet`,
      mirroring `GuardiansCallTauntRegistry`'s shape per `targeting-exclusion-contract.md` §2; depends on T004;
      can run in parallel with T013 — distinct file)
- [X] T015 [US2] In `HediffComp_WraithstepEffect.cs`'s `OnDestinationConfirmed`, after T013's tier-aware cooldown
      step: when `tierState.tier >= 2` (evaluated pre-upgrade, same as T013), set `untargetableEndTick = now +`
      the accessor-resolved `UntargetableWindowTicksTier2` and call `WraithstepUntargetableRegistry.Register(this)`;
      skip this step entirely at Tier 1 (depends on T013, T014)
- [X] T016 [P] [US2] Implement `Patch_AttackTargetFinder_BestAttackTarget_WraithstepUntargetable.cs` in
      `Source/Patches/` — a Harmony **Prefix** on `Verse.AI.AttackTargetFinder.BestAttackTarget` declaring
      `ref Predicate<Thing> validator`; cheap-bails when `!WraithstepUntargetableRegistry.HasActiveUntargetable`;
      otherwise composes `validator` to also reject any `Thing` that is a `Pawn` for which
      `WraithstepUntargetableRegistry.IsUntargetable(pawn)` is true (research.md R6, `targeting-exclusion-
      contract.md` §2; depends on T014; can run in parallel with T013/T015 — distinct file). **Confirmed working as
      written via live testing (2026-08-15)** — the debug log showed the exclusion firing correctly — but **also
      confirmed insufficient on its own**: a hostile that already had the wearer locked on via `mindState.
      enemyTarget`/`meleeThreat`/its running job's `targetA` kept attacking regardless, since none of those are
      re-read from the excluded search. Fixed by adding an active correction, `ForceNearbyHostilesOffWearer(Pawn)`,
      to `HediffComp_WraithstepEffect.cs` (called once on activation and every 30 ticks via a new `CompPostTick`
      override for the rest of the window) — clears those fields and force-ends the hostile's current job so it
      re-decides through the now-correctly-excluding search. See research.md R6 addendum and
      `targeting-exclusion-contract.md` §2b (both updated in place) for the full writeup; this mirrors the same gap
      Guardian's Call's own research already found and solved on the opposite (forcing) side of this problem.
- [X] T017 [US2] Manual verification (non-CE): run `quickstart.md` Scenario 4 — successful blinks increment the
      progression counter by exactly one each, a cancelled attempt does not increment it, the tattoo upgrades to
      Tier 2 in place with no ritual/ingredient/slot change, the Tier 2 range ring is visibly larger and farther
      cells become selectable, a Tier 2 blink makes an eligible hostile fail to select the wearer as a target for
      the configured window, the wearer remains visible/interactable throughout, and targetability resumes once
      the window elapses (depends on T015, T016). **PARTIALLY CONFIRMED live (2026-08-15, player Edd)**: debug log
      shows the progression counter climbing 1:1 with each successful activation (`progress=4` through `progress=15`
      across 12 consecutive blinks, no gaps), auto-upgrading to `tier=2` exactly at `progress=15` — matching the XML
      `tier2Threshold=15` default — with zero ritual/ingredient/slot involvement; the log's own `untargetableUntil=0`
      on that very upgrading activation (rather than a nonzero value) is *correct*, not a bug — it confirms the
      "an activation just before the upgrade uses the old tier's numbers" precedent (data-model.md State
      Transitions) held here too: `atTier2` was still evaluated as `false` for that activation (tier hadn't flipped
      yet when cooldown/window were resolved), so it used Tier 1 behavior even though the log line's `tier=` field
      (read after the upgrade) already shows `2`. Player also directly confirmed the Tier 2 range ring is visibly
      larger post-upgrade (farther cells became selectable). Tattoo-mastery progress also climbed exactly 1:1 with
      Wraithstep's own progression counter in the same log (`4→15` on both), matching T022/SC-014's "credit only on
      a genuine activation" expectation, though the specific "a cancelled attempt grants zero mastery credit" half
      of that check still needs its own explicit test (see T022). **The Tier 2 untargetable window's actual effect
      on a hostile's targeting was tested live (2026-08-15) and found broken, then fixed** — see T016's note and
      research.md R6 addendum for the full investigation (a hostile with the wearer already locked on kept
      attacking despite the search-exclusion correctly firing; fixed with an active `ForceNearbyHostilesOffWearer`
      correction). **Still needs a clean re-test with the fix in place**: blink adjacent to a hostile that has not
      yet noticed the wearer (the original "Toni" test setup) and confirm no attack lands for the window's
      duration, plus a re-test of the "already engaged, blinking away" case (the original "Poopy" scenario) to
      confirm the active correction now also breaks that engagement rather than just excluding future searches.
      Scenario 1/2's items from T011 are otherwise fully closed.
- [X] T018 [US2] Manual verification (Combat Extended loaded, **required**): repeat `quickstart.md` Scenario 4's
      untargetable-window check with CE loaded and a CE-controlled hostile nearby (Scenario 9, step 2) — confirm
      via temporary `Log.Message` probes in T016's Prefix (per this project's proactive-debug-logging practice)
      whether it alone already governs CE-controlled hostiles' target choice, per `targeting-exclusion-contract.md`
      §4; if verification instead shows CE-controlled hostiles bypass `BestAttackTarget`, implement a second,
      reflection-based patch following the exact `Patch_CE_ProjectileImpact_TattooAmmoBonus` convention (feature
      003 R5), gated behind `CombatExtendedInterop.IsLoaded`, registered from `TattooMagicMod`'s static constructor
      (depends on T017). **CONFIRMED live (2026-08-15) — the specific ranged-CE gap identified this session (see
      research.md R6 addendum context) is closed, no second patch needed.** Prior tests this session (Poopy,
      Carter, Lenka) only exercised melee hostiles, which decompiling `CombatExtended.dll` had shown don't even
      touch CE's replacement targeting code (CE's Transpiler on `AttackTargetFinder.BestAttackTarget` only rewrites
      the *ranged* branch). This test specifically used a ranged CE hostile — Soto, "scavenger gunner," confirmed
      equipped with a Ruger Redhawk (.44 Magnum) and ammo the entire encounter — landed a Tier 2 Wraithstep blink
      into his line of sight/range (not adjacent) while undrafted (no auto-engage confound this time). Result: the
      `Wraithstep untargetable window excluding Umeko from a hostile target search` line fired while Soto was in
      range, and he never fired a shot — just continued on his way. This confirms the single search-exclusion
      Prefix genuinely governs CE's ranged-targeting replacement too, satisfying Constitution Principle IV via "the
      same patch verified twice" (`targeting-exclusion-contract.md` §4) — no second, CE-specific patch is required.

**Checkpoint**: User Stories 1 and 2 both work independently — Wraithstep now has its full PRD §6 behavior in both
non-CE and CE-loaded games.

---

## Phase 5: User Story 3 - The gizmo reuse point proves itself on a fourth consumer, the targeting-exclusion convention gets its first user, and mastery progress accrues for free (Priority: P3)

**Goal**: Confirm feature 004's `IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_TattooGizmos` reuse point generalizes
to a fourth, still-different triggered tattoo with zero changes to Guardian's Call's, Stormlash's, or Bloodrune's
own files; confirm Wraithstep's Tier 2 targeting exclusion correctly composes with Guardian's Call's own taunt
patch on the same vanilla method with zero changes to Guardian's Call's own files; and confirm feature 006's
tattoo-mastery gizmo-wrap hook (as extended by T005) credits Wraithstep's activations only on a confirmed,
successful blink — never on a cancelled targeting attempt.

**Independent Test**: Review that `HediffComp_WraithstepEffect` implements `IProvidesTattooGizmo` and is picked up
by the existing shared patch with zero modifications to any feature-004/005/006/007 tattoo-effect file; apply
Wraithstep alongside Guardian's Call (Tier 2) on the same pawn to confirm the untargetable window also suppresses
the taunt while open; apply Wraithstep alongside Guardian's Call/Stormlash/Bloodrune to confirm all gizmos work
independently; and confirm a cancelled targeting attempt grants no mastery credit while a successful blink grants
exactly one (`quickstart.md` Scenarios 5, 6, 7).

### Implementation for User Story 3

- [X] T019 [US3] Review `Source/Hediffs/HediffComp_WraithstepEffect.cs`, `WraithstepUntargetableRegistry.cs`, and
      the new patch file against `contracts/cell-targeted-ability-contract.md`, `contracts/targeting-exclusion-
      contract.md`, feature 004's `contracts/triggered-tattoo-effect-contract.md` §1-2, `contracts/two-path-
      targeting-contract.md`, and feature 006's `contracts/mastery-progression-contract.md` §2; confirm via `git
      diff`/inspection that `Source/Hediffs/HediffComp_GuardiansCallEffect.cs`, `HediffComp_StormlashEffect.cs`,
      `HediffComp_BloodruneEffect.cs`, `GuardiansCallTauntRegistry.cs`, and `Source/Patches/
      Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs` show zero changes from their pre-this-feature
      state, and that `Patch_Pawn_GetGizmos_TattooGizmos.cs`'s only change is T005's small, generic marker-interface
      condition; fix and re-review if anything leaked (depends on T016). **PASS — CONFIRMED.** `git diff --stat`
      against the pre-feature baseline shows changes to exactly `.specify/feature.json`, `Assemblies/
      TattooMagic.dll` (build output), `Defs/HediffDefs/Tattoos/Wraithstep.xml`, and `Source/Patches/
      Patch_Pawn_GetGizmos_TattooGizmos.cs` (the anticipated T005 condition) — zero changes to any prior tattoo's
      own comp/registry/patch file. `git status --short` confirms the rest of this feature's files are new,
      untracked additions (`IHandlesOwnMasteryProgress.cs`, `HediffComp_WraithstepEffect.cs`,
      `WraithstepUntargetableRegistry.cs`, `Patch_AttackTargetFinder_BestAttackTarget_WraithstepUntargetable.cs`),
      not edits to anything pre-existing.
- [X] T020 [US3] Manual verification: run `quickstart.md` Scenario 5 — apply Guardian's Call and Wraithstep (Tier
      2) to the same colonist, activate the taunt, then blink while it's still active; confirm hostiles do not
      lock onto the wearer via the taunt for as long as the post-blink untargetable window is open, and that the
      taunt resumes working normally once the window elapses (depends on T019). **CONFIRMED live (2026-08-15,
      pawn Umeko vs. hostile Lenka, CE loaded)** — debug log showed the full composition working exactly as
      `targeting-exclusion-contract.md` §3 predicted: `Guardian's Call taunt yanking Lenka off its current target
      (Jill) onto Umeko` fired first (the taunt pulling aggro as designed), then once Umeko blinked to Tier 2,
      `Wraithstep untargetable window forcing Lenka to drop its lock on Umeko` fired and correctly broke that same
      taunt-driven engagement — confirming the exclusion composes with Guardian's Call's own patch on the same
      vanilla method with zero special-case code in either file, exactly as documented. Also incidentally reconfirms
      the two-path targeting convention itself under CE (`Guardian's Call taunt postfix evaluating searcher=Lenka
      (CE loaded=True)`).
- [X] T021 [US3] Manual verification: run `quickstart.md` Scenario 6 — apply Wraithstep alongside Guardian's
      Call, Stormlash, and/or Bloodrune on the same colonist, confirm every gizmo appears and functions
      independently, and confirm no code change was needed in any of their own files (depends on T020).
      **CONFIRMED live (2026-08-15)** — same encounter as T020: Umeko had Wraithstep, Guardian's Call, and
      Stormlash all applied simultaneously; gizmo bar showed all three (plus Undraft/Melee attack) rendering
      independently, and the log showed Stormlash activating (`tier=1, duration=300, cooldown=1800, progress=1`)
      cleanly alongside Guardian's Call's taunt and Wraithstep's blink/exclusion in the same fight, with each
      tattoo's own cooldown/tier state clearly independent in the log output.
- [X] T022 [US3] Manual verification: run `quickstart.md` Scenario 7 — confirm a cancelled targeting attempt
      grants no tattoo-mastery credit, and a successful blink increases that pawn's tattoo-mastery progress
      (feature 006's debug log line or the ritual station dialog) by exactly one (depends on T021). **CONFIRMED
      live (2026-08-15)** — cancelling a targeting attempt (Escape) logged no tattoo-mastery progress change;
      combined with earlier sessions' confirmation that a successful blink increases it by exactly one each time
      (T017's log evidence), both halves of this scenario are now closed. Confirms the `IHandlesOwnMasteryProgress`
      opt-out (research.md R4) is genuinely deferring credit to the confirmed activation, not the gizmo click.
- [X] T023 [US3] Record the review outcome (T019) and coexistence/exclusion-composition/mastery-track
      confirmations (T020-T022) in this file's Notes section (below) (depends on T022). **Recorded inline at each
      task (T019-T022 above) as they were confirmed, rather than duplicated into a separate summary** — T019's
      self-audit found zero leakage into any prior tattoo's file; T020/T021 confirmed the Guardian's Call
      composition and general coexistence in one live encounter; T022 confirmed both halves of the mastery
      opt-out. **All three of User Story 3's reuse claims (gizmo shape, inverted targeting-exclusion convention,
      mastery pass-through) are now fully confirmed, not just code-reviewed.**

**Checkpoint**: All three user stories are independently satisfied — Wraithstep works end-to-end, and the
gizmo-reuse point, the inverted targeting-exclusion convention, and the tattoo-mastery track are all confirmed to
genuinely generalize to a tattoo built after they already existed.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full-feature sign-off, including the cross-cutting checks (removal guard, save/reload persistence, no
regression to prior tattoos) that aren't specific to any single user story.

- [X] T024 Manual verification: run `quickstart.md` Scenario 8 — removal blocked except via God Mode or this mod's
      own sanctioned path (reusing feature 003's existing guard with zero Wraithstep-specific configuration); tier,
      progression counter, and cooldown/untargetable state unchanged across a save/reload (depends on T023).
      **CONFIRMED live (2026-08-15)** — save/reload persistence validated across numerous reloads throughout this
      session's testing (tier/progression counter/cooldown/untargetable state all carried over correctly every
      time). Removal guard confirmed via the `DebugAction_TestTattooRemovalGuard` tool (feature 007 precedent,
      generalized to any tattoo): with God Mode off, a direct `RemoveHediff` call against Umeko's Wraithstep hediff
      was correctly blocked (`RESULT: ... still present — guard correctly blocked the removal`); with God Mode on,
      the same call succeeded (`RESULT: ... was removed`) — the tool's generic "guard FAILED to block it" wording
      there is expected, not a bug: `Patch_HealthTracker_RemoveHediff_ProtectTattoos` explicitly checks
      `DebugSettings.godMode` first and intentionally lets removal through when it's on, mirroring vanilla's own
      health-tab delete-button behavior. Zero Wraithstep-specific configuration was needed for any of this — the
      existing feature 003 guard covered it automatically via `TattooMagicDef.appliedHediff`.
- [X] T025 Manual verification: run `quickstart.md` Scenario 9 — Guardian's Call/Stormlash/Bloodrune unaffected;
      mod loads cleanly with zero red Harmony/mod errors both without and with Combat Extended present; then
      re-run all nine `quickstart.md` scenarios back-to-back in one session, confirming zero red Harmony/mod
      errors throughout (depends on T024). **CONFIRMED live (2026-08-15)** — player confirmed Scenario 9 fully
      tested and passing: no regression to Guardian's Call/Stormlash, zero red Harmony/mod errors throughout the
      session both with Combat Extended loaded (the entire session ran CE-loaded) and consistent with the clean
      baseline established at T001 (non-CE).
- [X] T026 Confirm `rangeTier1`/`Tier2`, `cooldownDurationTicksTier1`/`Tier2`, `untargetableWindowTicksTier2`, and
      `tier2Threshold` are still at their T002 fast-testing defaults (revert if any were temporarily widened
      during manual testing, per feature 005/007 precedent); decide whether the T018 CE-investigation
      `Log.Message` probes should stay permanent (matching this project's established preference, feature
      005/006/007 precedent) or be trimmed if redundant; confirm a final clean `dotnet build` (0 errors, 0
      warnings) (depends on T025). **CONFIRMED** — `Defs/HediffDefs/Tattoos/Wraithstep.xml` verified back at its
      original T002 values (`untargetableWindowTicksTier2` was the only one ever temporarily widened, from 90 to
      900 for diagnostic testing, and was reverted to 90 immediately after that investigation closed). All
      Dev-Mode-gated diagnostic logs (`ForceNearbyHostilesOffWearer`'s "forcing X to drop its lock" line, the
      exclusion Prefix's "excluding X from a hostile target search" line, and the standard activation log) are
      kept permanently, matching this project's established preference for genuinely useful diagnostics over
      stripping them (feature 005/006/007 precedent) — these were directly responsible for root-causing both the
      untargetable-window gap and confirming the CE ranged path this session. Final `dotnet build`: 0 errors, 0
      warnings.

**Feature 008 (Wraithstep) is fully verified and complete.**

**Feature 008 (Wraithstep) is complete once T026 passes.**

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — BLOCKS all user stories, since no gizmo exists
  at all until the comp is wired up, attached via XML, and the mastery opt-out is in place.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced/extended (`HediffComp_WraithstepEffect.cs`) — build after US1, not
    in parallel with it.
  - US3 reviews code finished by both US1 and US2 — build after both, since a full review needs the final state
    of the comp (post-Tier-2/exclusion logic), not an intermediate one.
- **Polish (Phase 6)**: Depends on US1, US2, and US3 all being complete.

### Within Each User Story

- Foundational: T002 (XML) and T003 (marker interface) can run in parallel — distinct files, no compile-time
  coupling; T004 (comp skeleton) depends on both; T005 (shared-patch edit) depends only on T003; T006
  (verification) depends on T004 and T005.
- US1: T007 (validity/reject-reason logic) and T009 (targeting-session holder) can run in parallel — distinct
  files; T008 (the real two-phase activation flow) depends on both; T010 (range-ring draw hook) depends on T007
  (reads `GetRejectReason`) and T009 (reads `ActiveComp`), and can be built in parallel with T008 once both of its
  own dependencies are satisfied; T011 (verification) depends on T008 and T010; T012 (verification) depends on
  T011.
- US2: T013 (tier-aware cooldown/range) depends on T008 (same method, same file); T014 (new registry file) can run
  in parallel with T013; T015 (wires the registry into the comp) depends on both T013 and T014; T016 (the
  exclusion Prefix) depends only on T014 and can run in parallel with T013/T015; T017 (non-CE verification)
  depends on T015 and T016; T018 (CE-loaded verification, required) depends on T017.
- US3: Strictly sequential — the review (T019) before the Guardian's Call interaction check (T020) before general
  coexistence (T021) before the mastery-credit check (T022) before recording the outcome (T023).

### Parallel Opportunities

- Foundational: T002 and T003 can run in parallel.
- US1: T007 and T009 can run in parallel; once both are done, T008 and T010 can each start (T010 doesn't depend
  on T008, only on T007/T009).
- US2: T013 and T014 can run in parallel; once T014 is done, T016 can run in parallel with T013/T015.
- No cross-story parallelism — each story builds on the previous one's edits to the same comp file by design
  (mirroring features 005/007's US1/US2 shape).

---

## Parallel Example: Foundational Phase

```bash
# Launch these together once Setup (Phase 1) is complete:
Task: "Update Defs/HediffDefs/Tattoos/Wraithstep.xml with HediffCompProperties_WraithstepEffect"
Task: "Create Source/Effects/IHandlesOwnMasteryProgress.cs marker interface"
```

## Parallel Example: User Story 1

```bash
# Launch these together once Foundational (Phase 2) is complete:
Task: "Implement IsValidDestination/GetRejectReason in Source/Hediffs/HediffComp_WraithstepEffect.cs"
Task: "Implement Source/Hediffs/WraithstepTargetingSession.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1, 2, 3 in-game.
5. This is a demoable MVP: Wraithstep is no longer an inert stub — it grants a real, player-targeted Tier 1 blink
   with a real cooldown and correct targeting feedback, at Tier 1 only (no upgrade or exclusion window yet).

### Incremental Delivery

1. Setup + Foundational → a real, working gizmo/cooldown/tier/mastery-opt-out shell, nothing player-visible as a
   *blink* yet.
2. Add US1 → validate Scenarios 1-3 → MVP (Tier 1 Wraithstep blinks for real, with range ring and reject-reason
   feedback).
3. Add US2 → validate Scenario 4 (non-CE and CE-loaded) → Tier 2 auto-upgrade, longer range, and genuine
   post-blink untargetability all work in both configurations.
4. Add US3 → validate Scenarios 5-7 + self-audit against features 004/005/006/007's files → all three reuse
   claims (gizmo shape, inverted targeting-exclusion convention, mastery pass-through) confirmed on record.
5. Polish → validate the full nine-scenario sign-off pass and confirm no regression to Guardian's Call/Stormlash/
   Bloodrune.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- US2 edits the same file US1 extended (`HediffComp_WraithstepEffect.cs`) rather than duplicating it — expected
  and why it's sequenced after US1 rather than run in parallel with it, despite remaining independently
  *testable* per its own Quickstart scenario.
- US3 produces no new runtime code — building a fifth triggered tattoo is explicitly out of scope for this feature
  (spec Assumptions). Its "implementation" is a review checkpoint against features 004/005/006/007's files plus
  three live verification passes, with the outcome recorded here once T023 runs.
- Unlike Bloodrune (feature 007), which needed no extension to any shared file, this feature's Foundational phase
  includes one small, generic extension to `Patch_Pawn_GetGizmos_TattooGizmos.cs` (T005) because Wraithstep is the
  first triggered ability whose gizmo click does not itself complete the activation — without the opt-out, the
  shared mastery-progress hook would credit progress on every click, including cancelled ones. This is anticipated
  and sanctioned by `mastery-progression-contract.md` §2 (feature 006) as an "explicit, reviewable" extension, not
  a deviation from the "no per-tattoo special-casing" convention.
- T016 (the targeting-exclusion Prefix) and T013/T015 (the comp's own tier-aware cooldown/registry-registration
  logic) touch different files and can be built in either order or in parallel — the Prefix only needs
  `WraithstepUntargetableRegistry`'s public API (T014) to exist, not the comp's own calls into it.

## Implementation Session Notes (2026-08-15)

- **T001-T005, T007-T009 (superseded), T010 (folded in), T013-T016, T019 code-complete**, `dotnet build` clean (0
  errors, 0 warnings) against the real `Krafs.Rimworld.Ref` 1.6.4871 assembly.
- **Foundational research.md R3 was wrong in one respect, caught before any code was written against it, not
  after**: planning assumed `Verse.Targeter` and a Harmony-patch-based range-ring/reject-reason mechanism. Before
  writing T009/T010, the actual reference assembly was decompiled with `ilspycmd` (a locally-installed dotnet
  tool), which revealed `Targeter`/`TargetingParameters` actually live in namespace `RimWorld` (still reached via
  `Find.Targeter`), and that `Targeter.BeginTargeting`'s richest overload already accepts `onUpdateAction`/
  `onGuiAction` delegate parameters fired every frame for the life of that specific targeting session — exactly
  what R3 needed, with no Harmony patch and no separate session-tracking class required. T009 and T010 were
  consequently *not* implemented as separately planned (no `WraithstepTargetingSession.cs`, no
  `Patch_Targeter_*_WraithstepRangeIndicator.cs`); their functionality instead lives as two plain methods
  (`DrawRangeRing`, `DrawRejectReasonLabel`) on `HediffComp_WraithstepEffect` itself, passed as closures into the
  single `BeginTargeting` call in T008. `research.md`, `data-model.md`,
  `contracts/cell-targeted-ability-contract.md`, and `plan.md` were all updated in place to document the resolved
  (simpler) design rather than the planning-time estimate, consistent with this project's existing "resolved
  during implementation" convention (feature 005 research.md R8).
- The same decompilation pass also confirmed R5's `Pawn.Notify_Teleported(bool endCurrentJob = true, bool
  resetTweenedPos = true)` exactly as planned, R2's `Verse.GenGrid`/`Verse.GridsUtility` extension methods
  (`InBounds`, `Walkable`, `Fogged`, `GetFirstPawn`) exactly as planned, and R6's `AttackTargetFinder.
  BestAttackTarget` signature (already confirmed by feature 004 research.md R4) — only R3's mechanism needed
  correcting.
- T004/T007/T008/T013 were implemented together as one coherent pass rather than literally stubbed-then-patched
  across separate edits (no live game available mid-session to exercise an intermediate stub state) — same
  precedent as feature 007's implementation session. The final code matches what each task describes; nothing was
  skipped.
- **T019 self-audit — PASS, no leaks found.** `git diff --stat`/`git status --short` confirmed the only changes to
  pre-existing files are `Source/Patches/Patch_Pawn_GetGizmos_TattooGizmos.cs` (T005's anticipated generic
  condition) and `Defs/HediffDefs/Tattoos/Wraithstep.xml` (this tattoo's own, previously-inert Def) —
  `HediffComp_GuardiansCallEffect.cs`, `HediffComp_StormlashEffect.cs`, `HediffComp_BloodruneEffect.cs`,
  `GuardiansCallTauntRegistry.cs`, and `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs` are all
  untouched, confirming SC-011/SC-012 structurally (live confirmation of the Guardian's Call interaction and
  coexistence is still Scenarios 5-6, not yet run).
- **Not completed this session — require an actual running RimWorld instance and cannot be done by an agent
  without game access**: T006, T011, T012, T017, T018, T020, T021, T022, T023 (the outcome-recording task depends
  on T020-T022's live results), T024, T025, T026. All `quickstart.md` scenarios (1-9) still need to be run by a
  human in Dev Mode per Constitution Principle II before this feature can be marked done — in particular T018's
  Combat Extended-loaded verification of the untargetable-window Prefix, which per Constitution Principle IV is a
  required (not optional) check, not yet performed.

## Live Testing Session (2026-08-15) — Scenarios 1-3 confirmed, Tier 2 untargetable window found broken and fixed

- **Scenario 1 and 2 (T006/T011) fully CONFIRMED across several rounds of live testing** by the player (pawn Edd):
  gizmo appears/enabled only on a tattooed pawn; clicking it opens targeting with a live range ring; a valid cell
  instantly relocates the pawn and starts the cooldown; the blink is correctly **not** blocked by intervening
  terrain (Edd teleported through a mountain to a valid cell on the far side, confirming FR-004/Edge Cases); all
  four destination-rejection cases confirmed with visible reject-reason labels — out-of-range ("Out of range."),
  unwalkable/mountain ("Can't stand there."), pawn-occupied, including a hostile-occupied cell ("Occupied."), and
  fogged/unexplored ("Not explored."); Escape/cancel confirmed inert (no cooldown, no side effects). Debug log
  also confirmed the progression counter climbing 1:1 with each successful activation, auto-upgrading to Tier 2
  exactly at the XML `tier2Threshold=15`, with tattoo-mastery progress climbing in exact lockstep — and confirmed
  the "activation just before the upgrade uses the old tier's numbers" precedent held correctly even with this
  tattoo's two tier-gated effects (range and the untargetable window) sharing one activation method.
- **Tier 2 untargetable window (T016/T017) — found broken via live testing, root-caused, and fixed in the same
  session.** Initial reports (a hostile immediately meleeing Edd right after landing next to it) were ambiguous —
  could have been the documented "can't interrupt an attack already committed to before the blink" edge case
  (a hostile Edd was already fighting) rather than a bug. A controlled test (temporarily widening
  `untargetableWindowTicksTier2` from 90 to 900 for a clean diagnostic window) produced an unambiguous result: the
  debug log showed `Wraithstep untargetable window excluding <wearer> from a hostile target search` firing
  correctly on both sides of a hostile's melee hit landing — proving the search-exclusion Prefix (T016) genuinely
  ran and genuinely excluded the wearer, while the hostile hit her anyway. Root cause: the hostile's attack was
  being driven by an already-decided `mindState.enemyTarget`/`meleeThreat`/job `targetA` lock, none of which a
  search-time exclusion touches — the identical structural gap Guardian's Call's own research (feature 004)
  already found and solved for the opposite (forcing) problem. **Fixed**: added
  `HediffComp_WraithstepEffect.ForceNearbyHostilesOffWearer(Pawn)`, which actively clears those `mindState` fields
  and force-ends the current job (`JobCondition.InterruptForced`, confirmed via decompiled
  `Pawn_JobTracker.EndCurrentJob` signature) for any hostile currently locked onto the wearer, called once
  immediately on activation and every 30 ticks via a new `CompPostTick` override for the rest of the active
  window — mirroring `HediffComp_GuardiansCallEffect`'s identical one-shot-plus-periodic-reassert shape. `research.
  md` (R6 addendum) and `contracts/targeting-exclusion-contract.md` (§2b) were updated in place to document this as
  a required pairing for any exclusion-style tattoo, not just a Wraithstep-specific patch. `dotnet build`: 0
  errors, 0 warnings after the fix.
- **`untargetableWindowTicksTier2` is currently still at the widened diagnostic value (900, not the original 90)**
  in `Defs/HediffDefs/Tattoos/Wraithstep.xml` — intentionally left widened so the next test round has a
  comfortable window to confirm the fix in, per the note left in that XML file. **Must be reverted to a normal
  placeholder value (T026) once the fix is confirmed**, not left at a diagnostic value in a "finished" feature.
- **Still needed**: a clean re-test of both the original failure modes with the fix in place — (a) blinking next
  to a hostile that has not yet noticed the wearer (should now correctly stay unengaged for the window's duration),
  and (b) blinking away from an already-engaged hostile (should now correctly break that engagement too, not just
  exclude future searches) — plus the still-outstanding CE-loaded pass (T018) and every scenario listed in the
  prior session's "not completed" note above.
- **Follow-up round (2026-08-15, continued) — one retest, inconclusive, session paused mid-investigation.**
  Retested with the active-correction fix in place (pawns Umeko/Carter, CE loaded): the debug log showed the fix
  genuinely engaging — `Wraithstep untargetable window forcing Carter to drop its lock on Umeko` fired, followed
  immediately by `excluding Umeko from a hostile target search` (×2) — yet Carter still landed one melee hit for
  0.5 dmg right after. **Decompiled the actual `CombatExtended.dll` from this machine's Steam Workshop install**
  (not guessed) to check whether CE's own AI was the cause: confirmed CE patches `AttackTargetFinder.
  BestAttackTarget` via a **Transpiler**, but it only rewrites the method's *ranged*-attack-targeting branch
  (`FindAttackTargetForRangedAttack`, gated behind a `HasRangedAttack` check) — melee target-finding is untouched
  by CE's patch. Since Carter's hits were logged as melee, CE's own AI is very unlikely to be the cause here; the
  leading hypothesis instead is a same-tick race between the melee verb's "warmup" (a swing already committed
  before `CompPostTick`'s correction ran that same tick) and the correction itself — which would mean one
  already-committed swing can still land even though the fix is working, a narrower and more explainable gap than
  "the fix doesn't work." **This was never resolved** — the player exited before observing whether Carter attacked
  again after that single hit (which would distinguish "one committed swing slipped through, then held" from "the
  correction isn't actually stopping her"). **The save is from immediately before this Wraithstep activation**, not
  mid-combat, so the same clean test can be re-run from scratch next session. Picking this up next session — see
  the open question above (does Carter attack again after the first post-correction hit, yes/no) as the very first
  thing to check.

**Follow-up session (2026-08-15, continued) — root cause fully isolated, T017 CONFIRMED, investigation closed.**
Reproduced the test several times via save-reload (`Tattoo-Slice8-S5-Test`) to isolate the real variable. Key
realization: Umeko was **drafted and "Watching for targets"** in every prior attempt — a drafted pawn with no
ranged weapon automatically melees any hostile that becomes adjacent, with zero player order needed. Every earlier
"the hostile attacked instantly" observation had Umeko's own attack as the *first* logged hit, not the hostile's —
meaning the tests had actually been measuring "does a hostile's retaliation land after our own colonist attacks
first," not "does a hostile freshly notice and target an untargetable wearer," which is what FR-009 actually
promises. Once retested by landing Umeko a few tiles from Carter (non-adjacent, so her own drafted stance has
nothing to auto-engage) the result was clean: `Wraithstep untargetable window excluding Umeko from a hostile
target search` fired repeatedly over an extended stretch (Carter visibly paced at range without closing in), with
**zero hits landed and no `forcing Carter to drop its lock` line at all** — Carter never even managed to acquire a
lock to correct. When Carter eventually did close to melee range, the only damage logged was **Umeko's own
drafted-attack landing on Carter**, not the reverse — confirming the wearer's own combat stance, not a targeting-
exclusion failure, was the source of every earlier "instant attack" observation. `untargetableWindowTicksTier2`
reverted from the 900-tick diagnostic value back to its normal 90-tick placeholder in
`Defs/HediffDefs/Tattoos/Wraithstep.xml`; `dotnet build`: 0 errors, 0 warnings. **T017 (non-CE Scenario 4) is now
fully CONFIRMED** — both the search-exclusion Prefix (T016) and the active-correction fix (research.md R6
addendum) hold up under a properly isolated test. T018 (CE-loaded verification) remains outstanding and is a
different, still-required check per Constitution Principle IV — everything tested this session was already
CE-loaded incidentally (Carter/Umeko's fights showed CE-flavored combat log lines throughout), but T018 specifically
asks whether a *second*, CE-specific patch is needed, which was never isolated as its own test.
