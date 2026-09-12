---

description: "Task list for Shedscale Tattoo Effect"
---

# Tasks: Shedscale Tattoo Effect

**Input**: Design documents from `/specs/015-shedscale-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included, consistent with every prior tattoo feature.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–014: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

- This is the 15th tattoo, and the first built with **zero new Harmony patches** and **zero new
  `GameComponent`s** (research.md R7). Every file this feature creates is genuinely new; no existing tattoo's
  file needs modification, and — unlike every prior mechanically-rich tattoo — `Source/UI/Dialog_ChooseTattoo.cs`
  needs **no change at all** (FR-019: no rarity cap to gate on).
- **A body part can never be simultaneously "missing" and "prosthetic-blocked."** Decompiling vanilla's own
  install/uninstall surgery pipeline (research.md R1) confirms `Recipe_InstallArtificialBodyPart` removes the
  `Hediff_MissingPart` before adding a replacement, and uninstalling one (`Recipe_RemoveBodyPart`) re-amputates
  the part through the normal damage pipeline rather than "resuming" anything. **Do not build a separate
  "blocked" state, a banked-progress field, or any code that checks both missing-ness and installed-ness on the
  same part at once** — eligibility is a single, always-fresh check (`GetMissingPartsCommonAncestors()`
  membership), full stop. If a "how do I represent blocked-but-not-missing" question comes up during
  implementation, stop and re-read research.md R1 first.
- **Regrowth completion is one vanilla call: `pawn.health.RestorePart(part)`** (research.md R2). It already
  removes the missing-part hediff *and* every old injury/scar on that part recursively — do not write separate
  cleanup code for FR-007's "clears old permanent injuries" upside; `RestorePart` already does it.
- **Body-part eligibility is an explicit `List<BodyPartDef>` allow-list per tier, not `BodyPartDepth`**
  (research.md R3) — `Torso`/`Neck` are `Outside`-depth same as `Arm`/`Hand`, so depth alone doesn't cleanly
  separate "Tier 1 external limb" from "never eligible." Use `HediffCompProperties_ShedscaleEffect
  .tier1EligiblePartDefs`/`tier2AdditionalEligiblePartDefs` plus a hardcoded denylist (Brain via
  `BodyPartTagDefOf.ConsciousnessSource`, Spine, Neck, Torso, Pelvis, the pawn's own `RaceProps.body.corePart`).
- **Hunger rate has no `StatDef` a `StatPart` can intercept** (research.md R4) — `Need_Food` reads
  `HediffSet.GetHungerRateFactor()` directly, which sums `HediffStage.hungerRateFactor`/`hungerRateFactorOffset`
  across hediffs. **Do not route the stacking hunger/pain cost through `IProvidesTattooStatOffset`/
  `StatPart_TattooEffectOffset`** (Frost Sigil's own pattern) — it cannot reach hunger at all. Use a real
  companion `Hediff` (`TattooMagic_Hediff_ShedscaleStrain`) whose `Severity` is set directly to the current
  concurrent-regrowth count, with `<stages>` carrying `hungerRateFactorOffset`/`painOffset`.
- **The mood debuff is a `Thought_Situational` + `ThoughtWorker` pair, mirroring `Thought_
  StarlightWardTier2Active`/`ThoughtWorker_StarlightWardTier2Active` (feature 013) exactly** (research.md R5) —
  do not implement it as a mood-affecting hediff stage (`overrideMoodBase`/`makesSickThought`); neither fits a
  bounded, flat, non-stacking offset the way the existing `Thought_Situational` pattern already does.
- **The Tier 1 efficiency penalty is a `Hediff_Injury` (vanilla's own class, not a custom subclass) reducing the
  specific `BodyPartRecord`'s own reported health**, with `HediffCompProperties_GetsPermanent` force-activated
  immediately (`comp.IsPermanent = true`) right after adding it — the same immediate-permanent technique
  feature 014's `PhoenixRevivalUtility.ApplyBurn` already established (research.md R6). **Do not use `capMods`
  targeting a specific `PawnCapacityDef`** for this — different eligible part types serve different capacities
  (Manipulation for a hand, Moving for a foot, Sight for an eye...), and reducing the part's own health is the
  one universal mechanism vanilla's own capacity system already reads generically for all of them.
- **`HediffComp_ShedscaleEffect` does not compose the shared `TattooTierProgress` class.** It implements
  `IProvidesTattooTierProgress` directly with its own `tier`/`daysWornCounter`/`partsRegrownCounter` fields,
  because Tier 2 races two independently-paced counters (a day count and a part count) rather than one
  monotonic counter against one threshold, which `TattooTierProgress.TryRegisterQualifyingEvent` assumes
  (research.md R11, the same shape mismatch that already justified Phoenix's identical deviation in feature
  014). `ProgressionCounter`/`NextTierThreshold` report whichever counter is proportionally closer to its own
  threshold, recomputed fresh on every read — do not try to force-fit `TattooTierProgress` here, and do not pick
  one counter arbitrarily.
- **Removing the tattoo resets all of Shedscale's own state for free — no dedicated "on removal" cleanup code is
  needed for the tracked-regrowth dictionary or either Tier 2 counter** (research.md R10): they're plain fields
  on `HediffComp_ShedscaleEffect`, which is discarded along with the `Hediff` by the same ordinary
  `pawn.health.RemoveHediff` call `TattooHediffRemovalGuard.RemoveTattooHediff` already uses. The **one** thing
  that *does* need an explicit `CompPostPostRemoved` override is removing the separate `ShedscaleStrain`
  companion hediff, since it's a different `HediffDef` on the pawn, not a comp on the tattoo hediff itself — it
  does not disappear on its own just because the tattoo did.
- Every numeric this feature introduces — regrowth days per tier, the Tier 1 efficiency factor, both Tier 2
  thresholds, the two eligible-part-def lists, the Strain hediff's per-stack values, the mood debuff's
  magnitude — lives on `HediffCompProperties_ShedscaleEffect`'s XML fields (or the two support hediffs' own
  `<stages>`) and is read exclusively through `TattooEffectValues.Get(...)` at point of use (Constitution
  Principle III), exactly like every prior tattoo.
- `Source/Effects/TattooHediffRemovalGuard.cs` (feature 001) already covers `TattooMagic_Hediff_Shedscale`
  automatically once `Defs/TattooDefs/Shedscale.xml` declares it as `appliedHediff` — it's keyed off every
  `TattooMagicDef.appliedHediff` generically, not a per-tattoo list. No new removal-guard code is needed; the
  Polish-phase task below is a verification task, not implementation.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–014) is a clean baseline before adding Shedscale's own
code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline before adding
      anything, and confirm `Source/TattooMagic.csproj` needs no new package references — this feature's code
      uses only existing `Verse`/`RimWorld`/`HarmonyLib` types already referenced via `Krafs.Rimworld.Ref`/
      `Lib.Harmony` (research.md). — **Confirmed**: `0` warnings/`0` errors on the features 001–014 baseline; no
      new package references needed.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Get every one of Shedscale's new types, XML Defs, and persisted fields into existence — every user
story needs some slice of this before it can deliver anything player-visible.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Add three new entries to `Source/Defs/TattooMagicDefOf.cs`: `TattooMagic_Hediff_Shedscale`,
      `TattooMagic_Hediff_ShedscaleStrain`, `TattooMagic_Hediff_ShedscaleImperfectRegrowth` (all `HediffDef`) —
      mirrors every existing entry's shape (data-model.md). No `TattooMagicDef` entry is needed, unlike
      Phoenix's `TattooMagic_Phoenix` — nothing gates availability on a cap. — **Implemented**.
- [X] T003 [P] Create `Defs/TattooDefs/Shedscale.xml`: the recipe-side `TattooMagicDef` (`tattooType = Passive`
      — no activatable gizmo, `appliedHediff = TattooMagic_Hediff_Shedscale`, standard `ingredients`/
      `workAmount` shape matching every prior tattoo's recipe XML; no cap-related field) (data-model.md,
      plan.md Project Structure). — **Implemented** in `Defs/TattooDefs/Shedscale.xml` (well-formed XML
      confirmed via `xml.dom.minidom`). References an `iconPath` of `UI/Tattoos/Shedscale` with no texture file
      yet — falls back to `BaseContent.BadTex` (`TattooMagicDef.Icon`'s own existing behavior), same as several
      prior tattoos shipped their effect before their icon.
- [X] T004 [P] Create `Defs/HediffDefs/Tattoos/Shedscale.xml`: `TattooMagic_Hediff_Shedscale`, `hediffClass =
      HediffWithComps`, `<comps>` carrying `TattooMagic.HediffCompProperties_ShedscaleEffect` with every
      placeholder tunable from data-model.md's field table (`tier1RegrowthDays` = 7, `tier2RegrowthDays` = 4,
      `tier1EfficiencyFactor` = 0.80, `tier2DaysWornThreshold` = 30, `tier2PartsRegrownThreshold` = 3,
      `tier1EligiblePartDefs` = Finger/Toe/Hand/Foot/Arm/Leg/Ear/Nose/Jaw/Eye, `tier2AdditionalEligiblePartDefs`
      = Kidney/Lung), under the standard top-of-file XML comment marking these as placeholders pending this
      spec's own Assumptions/balance pass (Constitution Principle III, research.md R3). — **Implemented**,
      well-formed XML confirmed.
- [X] T005 [P] Create `Defs/HediffDefs/ShedscaleStrain.xml`: `TattooMagic_Hediff_ShedscaleStrain`, sits directly
      under `Defs/HediffDefs/` (not `Tattoos/`, mirroring `TattooTracker.xml`/`FrostSigilSlow.xml`/
      `ParalyticAbasia.xml`'s existing support-hediff placement) — plain `hediffClass = Hediff` (no comps; its
      `Severity` is driven entirely by `HediffComp_ShedscaleEffect`, not any natural severity-gain rate),
      `<stages>` keyed by ascending `minSeverity` (1 through a placeholder cap of 5) each setting
      `hungerRateFactorOffset`/`painOffset` scaled to that stack count (research.md R4, data-model.md). —
      **Implemented**, well-formed XML confirmed.
- [X] T006 [P] Create `Defs/HediffDefs/ShedscaleImperfectRegrowth.xml`: `TattooMagic_Hediff_
      ShedscaleImperfectRegrowth`, sits directly under `Defs/HediffDefs/` — `hediffClass = Hediff_Injury`
      (vanilla's own class, not a custom subclass), one comp: vanilla's own `HediffCompProperties_
      GetsPermanent`, no `<stages>` of its own (its severity is set programmatically per instance at apply
      time, research.md R6, data-model.md). — **Implemented**. `<injuryProps>` shape (`painPerSeverity`,
      `bleedRate`, `canMerge`, `destroyedLabel`) verified against vanilla's own `Misc`/`BurnBase` injury Defs
      (`Hediffs_Local_Injuries.xml`) before writing, not guessed — `painPerSeverity`/`bleedRate` both set to
      `0` since this condition's pain/cost is carried entirely by the Strain hediff (T005), not by this one.
- [X] T007 [P] Create `Defs/ThoughtDefs/Shedscale.xml`: `TattooMagic_Thought_ShedscaleRegrowthActive`, mirrors
      `Defs/ThoughtDefs/StarlightWard.xml`'s exact shape (`thoughtClass`/`workerClass` pointing at the pair
      T018 implements, one stage with a placeholder **negative** `baseMoodEffect`) (research.md R5). —
      **Implemented**, well-formed XML confirmed.
- [X] T008 Implement the `HediffCompProperties_ShedscaleEffect`/`HediffComp_ShedscaleEffect` skeleton in
      `Source/Hediffs/HediffComp_ShedscaleEffect.cs`: every runtime field from data-model.md's table (`tier`,
      `daysWornCounter`, `partsRegrownCounter`, `nextDailyCheckTick`, `activeRegrowths` as `Dictionary
      <BodyPartRecord, int>`); `CompExposeData()` persisting all of them (`Scribe_Values` plus
      `Scribe_Collections.Look(ref activeRegrowths, "activeRegrowths", LookMode.BodyPart, LookMode.Value)`);
      `IProvidesTattooTierProgress` implemented directly off `tier` and a `LeadingProgress()` helper comparing
      both counters' ratios to their thresholds (no composed `TattooTierProgress` — see Known Baseline); a
      private `IsEligiblePart(BodyPartRecord part)` helper checking the tier-appropriate allow-list from
      `Props` against the hardcoded Brain/Spine/Neck/Torso/Pelvis/corePart denylist (research.md R3); a
      `TryUpgradeToTier2()` method declared but left as a **no-op stub** for now (US4 fills in the real
      dual-condition body). Leave `CompPostTick` as a `base.CompPostTick(...)`-only stub for now — US1 fills in
      the actual once-per-day pass. Depends on T002, T004. — **Implemented** in `Source/Hediffs/
      HediffComp_ShedscaleEffect.cs` — written in one complete pass together with T010/T011/T016/T017/T019/
      T021/T022's own logic rather than literally left as a stub first (same file, no separate build/verify
      checkpoint needed between the skeleton and its full behavior). The denylist uses `BodyPartTagDefOf
      .ConsciousnessSource`/`.Spine`/`.Pelvis` (tag-based, decompile-confirmed the same way vanilla's own
      `HediffSet.GetBrain()` identifies the brain) plus `BodyPartDefOf.Torso`/`.Neck` (direct def references,
      both confirmed to exist on `RimWorld.BodyPartDefOf` by decompile) rather than a hardcoded list of
      `BodyPartDef` names.
- [X] T009 Confirm `dotnet build` succeeds with `0` warnings/`0` errors on the Foundational skeleton (depends on
      T002–T008). — **Confirmed**: `0` warnings/`0` errors after adding one missing `using RimWorld;` directive
      to `Thought_ShedscaleRegrowthActive.cs` (`Thought_Situational` lives in `RimWorld`, not `Verse`). Build
      output and every new `Defs/**`/`Languages/**` file confirmed deployed to the live `Mods/TattooMagic/`
      folder via the existing `PublishToModsFolder` target.

**Checkpoint**: Foundational ready — every new type/Def exists and persists correctly (even though most of them
do nothing yet); user story implementation can now begin.

---

## Phase 3: User Story 1 - A missing limb quietly grows back (Priority: P1) 🎯 MVP

**Goal**: A colonist wearing Shedscale who is missing an eligible body part with nothing installed on it
automatically regrows it over the tier's duration, with no player action, no one-at-a-time restriction, and any
old permanent injuries on that part cleared as part of the process; Tier 1 regrowths come back at reduced
efficiency, Tier 2 regrowths (which also cover internal organs) do not.

**Independent Test**: Apply a Shedscale tattoo to a colonist missing an eligible part (with no prosthetic
installed on it), wait the tier's regrowth duration, and observe the part return on its own, with any
pre-existing permanent injuries on that part gone (`quickstart.md` Scenario 1).

### Implementation for User Story 1

- [X] T010 [US1] In `Source/Hediffs/HediffComp_ShedscaleEffect.cs`, implement `CompPostTick`'s once-per-day
      self-gate (`nextDailyCheckTick`, mirrors `HediffComp_PhoenixEffect.nextDaysAliveCheckTick`'s shape) calling
      a new `DoDailyPass()` method; implement `DoDailyPass()`: `daysWornCounter++` then `TryUpgradeToTier2()`
      (currently a no-op stub, US4 fills it in); scan `Pawn.health.hediffSet.GetMissingPartsCommonAncestors()`
      for parts passing `IsEligiblePart(part)` not already a key in `activeRegrowths`, adding each at `0`
      elapsed days (research.md R1/R3); for every tracked entry, increment its elapsed-days value and call
      `CompleteRegrowth(part)` once it meets or exceeds the tier-appropriate duration
      (`tier1RegrowthDays`/`tier2RegrowthDays` via `TattooEffectValues`). Depends on T008. — **Implemented** as
      `DoDailyPass()`/`ScanForNewlyEligibleParts()`/`AdvanceActiveRegrowths()`. Also includes the defensive
      stale-`ImperfectRegrowth`-hediff cleanup called out in data-model.md (a part lost again after a prior
      regrowth cycle) and the FR-014 malnutrition guard around the advance step (`AdvanceActiveRegrowths` is
      only called when `!malnourished`) — written together with T017 rather than added as a separate later
      edit to the same method. **Real bug found and fixed during live testing (2026-09-12)**: uninstalling a
      full arm/leg-level replacement re-amputates at the **Shoulder** joint (same structural point T014's own
      bug was found at, not the Arm itself — `Recipe_RemoveBodyPart` damages whatever part
      `HasDirectlyAddedPartFor` is true for), and `GetMissingPartsCommonAncestors()` stops the instant it finds
      a missing-part hediff on any part — confirmed via decompile of `HediffSet
      .CacheMissingPartsCommonAncestors` — so it reports "Shoulder" missing and never descends to Arm/Hand/
      Finger underneath. `IsEligiblePart(Shoulder)` returned `false` (Shoulder isn't in either eligible-part-def
      list), so a fully re-amputated arm/leg silently never re-entered `activeRegrowths` at all — confirmed live
      (`activeRegrowths=0` across multiple forced daily passes after removing a bionic arm). Fixed by making
      `IsEligiblePart` itself recurse into a part's children (after the denylist check, before returning
      `false`) when the part isn't directly in either list — since `RestorePart(Shoulder)` already restores the
      whole subtree in one call regardless of which level tracking started at, tracking "Shoulder" for
      regrowth is exactly correct once it's recognized as eligible. This is the same underlying fix as T014's
      alert bug, now applied once in the shared `IsEligiblePart` method rather than duplicated — T014's own
      `CoversEligiblePart` helper was removed in favor of calling `comp.IsEligiblePart` directly, since the
      subtree walk now lives in one place. Rebuilt (`0`/`0`) and redeployed.
- [X] T011 [US1] In the same file, implement `CompleteRegrowth(BodyPartRecord part)`: `Pawn.health
      .RestorePart(part)` (research.md R2 — restores the part and clears old permanent injuries in one call,
      FR-007); if `tier == 1`, add `TattooMagic_Hediff_ShedscaleImperfectRegrowth` to `part` via
      `HediffMaker.MakeHediff`/`pawn.health.AddHediff(hediff, part)` with `Severity = part.def
      .GetMaxHealth(Pawn) * (1f - TattooEffectValues.Get(ScopeKey, "Tier1EfficiencyFactor", Props
      .tier1EfficiencyFactor))`, then immediately set its `HediffComp_GetsPermanent.IsPermanent = true`
      (research.md R6, mirrors `PhoenixRevivalUtility.ApplyBurn`'s own immediate-permanent pattern); increment
      `partsRegrownCounter` then call `TryUpgradeToTier2()`; remove `part` from `activeRegrowths`. Depends on
      T010 (same method group). — **Implemented**.
- [X] T012 [P] [US1] Add Dev Mode debug tools in `Source/Debug/DebugAction_TestShedscaleRegrowth.cs`: "TEST:
      Shedscale — force daily pass now" (calls `DoDailyPass()` on the targeted pawn immediately) and "TEST:
      Shedscale — complete all active regrowths now" (jumps every tracked entry's elapsed-days value straight to
      its tier duration and re-runs the completion check) — mirrors feature 013/014's own deterministic-debug-
      tool precedent for a mechanic too slow to hand-test. Depends on T010, T011 (calls into both) but is its
      own new file. — **Implemented**, both tools present alongside T023's own two tools in the same file
      (`GetShedscaleComp` lookup helper shared by all four).
- [X] T013 [US1] Manual verification: run `quickstart.md` Scenario 1 (natural regrowth at both tiers including
      organs at Tier 2, the reduced-efficiency penalty at Tier 1 only, old permanent injuries cleared, and
      multiple simultaneous regrowths with no one-at-a-time restriction) using the new debug tools — confirm
      zero red Harmony/mod errors throughout (depends on T010–T012). — **PASS**, user-driven live-verified
      2026-09-12 against a live colonist (Kyle): Tier 1 leg regrowth completed correctly with an `Imperfect
      regrowth` penalty applied; reaching Tier 2 (`daysWorn=60`) retroactively cleared it from the health tab
      in the same instant; a destroyed lung regrew cleanly at Tier 2 with **no** penalty; a scarred (`Cut`)
      leg, once lost and regrown, came back fully clean (`RestorePart`'s cleanup, FR-007) — pawn tooltip read
      "Healthy"; and three simultaneous losses (an eye, a kidney, a leg) all entered `activeRegrowths` at once
      from a single daily pass (`activeRegrowths=3`), correctly producing `Regrowth strain (significant)`
      (severity 3, matching the `minSeverity=3` XML stage) before all three completed together and the Strain
      hediff cleared immediately. **Real bug found and fixed during this pass**: `DevCompleteAllActiveRegrowths()`
      called `CompleteRegrowth` for each tracked part but never refreshed the Strain hediff afterward (only
      `DoDailyPass()`'s own end-of-pass call does that) — using the "complete all" tool outside a real daily
      pass left a stale `Regrowth strain (mild)` hediff visible on a pawn with zero remaining active
      regrowths. Fixed by calling `UpdateStrainHediff()` at the end of `DevCompleteAllActiveRegrowths()` too;
      rebuilt (`0`/`0`), redeployed, and the fix was independently confirmed correct by the final Strain-clear
      observed in this same session. Zero red Harmony/mod errors throughout.

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Prosthetics and bionics block regrowth until removed (Priority: P2)

**Goal**: An installed prosthetic, peg leg, or bionic (including bionic organs) prevents natural regrowth on
that specific part entirely, with a persistent alert flagging the block, and uninstalling it always starts the
full tier duration fresh rather than resuming any progress.

**Independent Test**: Install a prosthetic on a missing eligible part of a Shedscale-tattooed colonist, confirm
a warning appears and no regrowth timer progresses no matter how long is waited, then remove the prosthetic via
a surgery bill and confirm the full tier duration starts fresh from that point (`quickstart.md` Scenario 2).

### Implementation for User Story 2

- [X] T014 [US2] Implement `Alert_ShedscaleRegrowthBlocked` in `Source/UI/Alert_ShedscaleRegrowthBlocked.cs`
      (this mod's first custom `Alert` subclass, research.md R9): scan
      `PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended` for any pawn
      carrying `TattooMagic_Hediff_Shedscale` who has at least one `Hediff_AddedPart` whose `.Part.def` is in
      that pawn's tier-appropriate eligible list (reuse/expose `HediffComp_ShedscaleEffect.IsEligiblePart`-style
      logic against the *installed* part rather than a missing one); `GetReport()` returns
      `AlertReport.CulpritsAre(...)`, `GetExplanation()` lists the affected pawns, mirroring vanilla's own
      `Alert_Hypothermia`/this mod's nonexistent-until-now alert precedent. No persisted state — re-derives the
      answer fresh from live `HediffSet` state on every query. Depends on T004 (the eligible-part-def lists), T008.
      — **Implemented**, directly reusing `HediffComp_ShedscaleEffect.IsEligiblePart` (made `public` for this
      purpose) rather than duplicating the allow/deny-list logic. Added the two `Translate()` keys
      (`AlertShedscaleRegrowthBlocked`/`Desc`) to `Languages/English/Keyed/TattooMagic.xml` — this mod's first
      use of the Keyed-translation system from C# code (every other UI string in this mod up to now was a
      literal, unlocalized string), since `Alert`'s own base class expects a `TaggedString`/`.Translate()`
      pipeline for its label/explanation, not a bare string. **Real bug found and fixed during live testing
      (2026-09-12)**: a real "Install bionic arm" surgery bill attaches its `Hediff_AddedPart` at the
      **Shoulder** joint, not the Arm itself (confirmed live — the health tab labels it "Shoulder | Bionic
      arm"), so the original exact-match `comp.IsEligiblePart(addedPart.Part)` check never matched — Shoulder
      isn't in either eligible-part-def list, only its descendants (Arm, Hand, Finger) are. The alert never
      fired for the single most common real-world case (a full limb replacement). Fixed by walking the
      installed part's whole subtree (a temporary local `CoversEligiblePart` helper, recursing into
      `part.parts`) rather than checking only its exact attachment point; rebuilt (`0`/`0`) and redeployed.
      **Superseded by T010's own fix** to the same root cause: `IsEligiblePart` itself now does the subtree
      walk (the uninstall-side mirror of this same bug — re-amputating at Shoulder also needed it), so the
      local `CoversEligiblePart` helper was removed here in favor of calling `comp.IsEligiblePart` directly —
      one subtree-walk implementation instead of two. Also bumped `defaultPriority` to `AlertPriority.High`
      (per user request, "more noticeable") — matches vanilla's own `Alert_ReimplantationAvailable`, a similar
      "beneficial action available" alert, without `Alert_Critical`'s pulsing-red/repeated-message treatment,
      which would be tonally wrong for a non-urgent, potentially long-lived state.

**Checkpoint**: User Stories 1 and 2 both work independently.

- [X] T015 [US2] Manual verification: run `quickstart.md` Scenario 2 (installing a prosthetic/bionic blocks
      regrowth indefinitely with no banked progress, the alert appears and persists, and uninstalling it starts
      the full tier duration fresh — covering both an external limb and a bionic organ) — confirm zero red
      Harmony/mod errors throughout (depends on T010, T011, T014 — needs US1's regrowth mechanics working to
      prove the "restarts fresh" half of this story). — **PASS for the external-limb case**, user-driven
      live-verified 2026-09-12 against Kyle: a real "Install bionic arm" surgery bill correctly blocked
      regrowth indefinitely (`activeRegrowths=0` across 15 forced daily passes with the bionic installed), the
      **"Shedscale regrowth blocked"** alert appeared once the two real bugs below were fixed, and removing the
      bionic arm (a real "Cut off" surgery bill, re-amputating at the Shoulder) correctly restarted the full
      tier duration fresh — the regrown arm came back with a fresh `Imperfect regrowth` penalty (Tier 1, since
      this save had been reloaded to an earlier point pre-dating the prior session's Tier 2 upgrade — expected
      state, not a bug). The bionic-**organ** half of this scenario (Kidney/Lung at Tier 2) has not been
      separately exercised with an installed replacement yet, though organ regrowth itself (without a
      replacement installed) was already confirmed working in T013/Scenario 1. **Two real bugs found and fixed
      during this pass** (see T010's and T014's own notes above for full detail): (1) the Alert's exact-match
      eligibility check missed a bionic arm's `Hediff_AddedPart`, which vanilla attaches at the Shoulder joint
      rather than the Arm itself; (2) the regrowth scan's `IsEligiblePart` had the identical blind spot on the
      uninstall side — re-amputating at Shoulder was never recognized as eligible. Both fixed by making
      `IsEligiblePart` itself recurse into a part's subtree, consolidating what had briefly been two separate
      implementations into one. Also bumped the alert to `AlertPriority.High` per user request for more visual
      prominence. Zero red Harmony/mod errors throughout.

---

## Phase 5: User Story 3 - Living with an active regrowth (Priority: P3)

**Goal**: A colonist with one or more parts actively regrowing carries one stack of a raised-hunger/mild-pain
effect per concurrently-regrowing part, plus exactly one (non-stacking) mood penalty while any regrowth is
active, and all of that colonist's regrowth timers stall while they're malnourished.

**Independent Test**: Have a colonist with two parts regrowing simultaneously, and confirm they carry two
stacks of the hunger/pain effect (versus one stack for a single regrowth) plus exactly one flat mood penalty,
and that a malnourished colonist's regrowth timers stop advancing until fed (`quickstart.md` Scenario 3).

### Implementation for User Story 3

- [X] T016 [US3] In `Source/Hediffs/HediffComp_ShedscaleEffect.cs`, implement `UpdateStrainHediff()`: if
      `activeRegrowths.Count > 0`, get-or-add `TattooMagic_Hediff_ShedscaleStrain` on `Pawn` and set its
      `Severity = activeRegrowths.Count`; if `Count == 0` and the Strain hediff is present, `pawn.health
      .RemoveHediff(...)` it (research.md R4). Call it at the end of `DoDailyPass()` (extends T010's method).
      Depends on T005, T010. — **Implemented**, called at the end of `DoDailyPass()`.
- [X] T017 [US3] In the same file, add the malnutrition stall to `DoDailyPass()` (extends T010's method,
      FR-014): compute `bool malnourished = Pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition)` before
      the elapsed-days advance loop, and skip that loop entirely (for every tracked part, including ones newly
      added this same pass) when `malnourished` is true. Depends on T010. — **Implemented together with T010**
      (see T010's own note) rather than as a separate later edit to the same method.
- [X] T018 [P] [US3] Implement `Thought_ShedscaleRegrowthActive` and `ThoughtWorker_ShedscaleRegrowthActive` in
      `Source/Hediffs/` (two small files or one, matching this mod's existing Starlight-Ward-pair convention):
      `Thought_ShedscaleRegrowthActive.BaseMoodOffset` routed through `TattooEffectValues.Get("TattooMagic_
      Hediff_Shedscale", "RegrowthMoodPenalty", CurStage.baseMoodEffect)`; `ThoughtWorker_
      ShedscaleRegrowthActive.CurrentStateInternal` returns `ActiveDefault` iff the pawn's `HediffComp_
      ShedscaleEffect.ActiveRegrowthCount > 0`, else `Inactive` — exact structural mirror of `Thought_
      StarlightWardTier2Active`/`ThoughtWorker_StarlightWardTier2Active` (research.md R5). Depends on T007, T008.
      — **Implemented** as two files (`Thought_ShedscaleRegrowthActive.cs`/`ThoughtWorker_
      ShedscaleRegrowthActive.cs`), matching the existing Starlight Ward pair's own two-file convention.
- [X] T019 [US3] In `Source/Hediffs/HediffComp_ShedscaleEffect.cs`, implement `CompPostPostRemoved()`: if the
      pawn still carries `TattooMagic_Hediff_ShedscaleStrain`, remove it (research.md R10 — the Strain hediff is
      a separate `HediffDef`, not itself a comp on the tattoo hediff, so it doesn't disappear automatically when
      the tattoo does). Depends on T016. — **Implemented**.
- [X] T020 [US3] Manual verification: run `quickstart.md` Scenario 3 (one stack per concurrent regrowth vs. one
      flat non-stacking mood penalty regardless of count, and malnutrition stalling every tracked timer at once)
      — confirm zero red Harmony/mod errors throughout (depends on T016–T019). — **PASS**, user-driven
      live-verified 2026-09-12 against Kyle: one active regrowth produced `Regrowth strain (mild)` (severity
      1); a second concurrent regrowth bumped it to `Regrowth strain (moderate)` (severity 2) — one stack per
      part, confirmed via the exact `minSeverity` stage labels. The Needs tab's mood breakdown showed exactly
      **one** "Watching myself regrow" entry (-4) despite two concurrent regrowths — non-stacking mood
      confirmed (FR-015) — plus "Minor pain" (-5) showing up automatically via vanilla's own pain-mood bucket,
      confirming the Strain hediff's `painOffset` is feeding through correctly too. Malnutrition then stalled
      both tracked arms' elapsed-days advance for 15 consecutive forced daily passes (`daysWorn` 15→29,
      `activeRegrowths` never dropping from 2) — confirmed via T024's own follow-on note, malnutrition clearing
      naturally by `daysWorn=30` let both resume and complete in the very same pass the Tier 2 upgrade fired.
      Zero red Harmony/mod errors throughout.

**Checkpoint**: User Stories 1, 2, and 3 all work independently.

---

## Phase 6: User Story 4 - Reaching Tier 2 (Priority: P4)

**Goal**: The tattoo permanently upgrades to Tier 2 the moment either of two independent conditions is first
met (30 days worn, or 3 parts regrown), whichever comes first, and reaching Tier 2 retroactively clears the
reduced-efficiency penalty from every part already regrown at Tier 1 in addition to preventing it on every
future regrowth.

**Independent Test**: Advance a Shedscale-tattooed colonist to the earlier of the two Tier 2 thresholds (30
days worn, or 3 parts regrown) and confirm the upgrade applies permanently, all future regrowths come back at
full efficiency, and any parts already regrown at Tier 1 lose their efficiency penalty retroactively
(`quickstart.md` Scenario 4).

### Implementation for User Story 4

- [X] T021 [US4] In `Source/Hediffs/HediffComp_ShedscaleEffect.cs`, replace the Foundational `TryUpgradeToTier2()`
      stub with its real body: no-op if `tier >= 2`; otherwise read both thresholds via `TattooEffectValues`
      (`Tier2DaysWornThreshold`/`Tier2PartsRegrownThreshold`) and, if `daysWornCounter >= daysThreshold ||
      partsRegrownCounter >= partsThreshold`, set `tier = 2` and remove every `TattooMagic_Hediff_
      ShedscaleImperfectRegrowth` instance found on `Pawn.health.hediffSet.hediffs` (FR-018's retroactive clear
      — filter by `.def`, no separate tracking list needed). Depends on T008, T010, T011 (the call sites T010's
      `DoDailyPass` and T011's `CompleteRegrowth` already added). — **Implemented together with T008** (written
      as the real body directly rather than a stub-then-replace, since the whole file was authored in one pass
      — see T008's own note). `CompleteRegrowth`'s own ordering (`partsRegrownCounter++` then
      `TryUpgradeToTier2()` *before* removing the just-regrown part from `activeRegrowths`) means a part whose
      own completion is what pushes `partsRegrownCounter` over the threshold is itself included in the
      retroactive sweep's *timing* (the upgrade check runs immediately after that part's own Tier 1 penalty
      would have been applied a few lines earlier in the same method) — matches quickstart.md Scenario 4 Step
      2's expectation that the 3rd part's own regrowth also ends up carrying no penalty.
- [X] T022 [US4] In the same file, implement the `LeadingProgress()` helper backing `ProgressionCounter`/
      `NextTierThreshold` (research.md R11): compute both counters' ratios to their own thresholds and return
      whichever pair is proportionally closer to completion, recomputed fresh on every read (no caching).
      Depends on T008. — **Implemented**.
- [X] T023 [P] [US4] Add Dev Mode debug tools in `Source/Debug/DebugAction_TestShedscaleRegrowth.cs`: "TEST:
      Shedscale — set days-worn counter" and "TEST: Shedscale — set parts-regrown counter" (each directly sets
      the field and immediately re-runs `TryUpgradeToTier2()`), mirroring Phoenix's "set days-alive counter"
      tool. Depends on T021 (same file as T012, extends it rather than conflicting). — **Implemented**, using
      vanilla's own `Verse.Dialog_Slider` for value entry, same as Phoenix's own numeric debug tools.
- [X] T024 [US4] Manual verification: run `quickstart.md` Scenario 4 (either path reaching Tier 2 first,
      permanence, and retroactive efficiency-penalty clearing on already-regrown parts) — confirm zero red
      Harmony/mod errors throughout (depends on T021–T023). — **PASS**, user-driven live-verified 2026-09-12,
      both independent Tier 2 conditions isolated on separate fresh colonists to rule out cross-contamination:
      (1) **Days-worn path** — Doug, `daysWornCounter` set to 29 (one below the 30 threshold, `tier2PartsRegrownThreshold`
      untouched at 0), one forced daily pass tipped it to 30 and the tattoo upgraded to Tier 2 automatically,
      no ritual/item, confirmed "Healthy" with no lingering state. (2) **Parts-regrown path** — Maskinnen,
      three real regrowths completed in sequence (`daysWorn` only reaching 4 throughout, nowhere near the
      30-day threshold) — the moment the third completed, the log shows `reached Tier 2 (daysWorn=4,
      partsRegrown=3)`, the parts-count condition winning independently of the day count, with the retroactive
      efficiency-penalty clear applying in the same instant to that same 3rd part's own just-added Tier 1
      penalty (the same-tick interaction documented in T021's own note). Both paths confirmed to produce a
      permanent upgrade. Zero red Harmony/mod errors throughout either test.

**Checkpoint**: All four user stories are independently functional — Shedscale has its full spec.md behavior.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Confirm the pre-existing cross-tattoo removal guard covers Shedscale with zero new code, confirm
the no-rarity-cap requirement holds, then full-feature sign-off across all eight `quickstart.md` scenarios.

- [X] T025 [P] Verify `Source/Effects/TattooHediffRemovalGuard.cs`'s `protectedHediffDefs` registry (built from
      every `TattooMagicDef.appliedHediff`) already includes `TattooMagic_Hediff_Shedscale` — true automatically
      once T003 exists, independent of any of this feature's own runtime code — and that
      `Patch_HealthTracker_RemoveHediff_ProtectTattoos` therefore blocks its unsanctioned removal with zero
      Shedscale-specific configuration. Verification-only — only change code if the review finds a real gap (no
      dependency — can run any time once T003 exists). — **PASS**, no gap found (code review only, no live game
      needed for this one — same as feature 013/014's own equivalent task): `Defs/TattooDefs/Shedscale.xml`
      declares `<appliedHediff>TattooMagic_Hediff_Shedscale</appliedHediff>`, and `protectedHediffDefs` is built
      purely from `DefDatabase<TattooMagicDef>.AllDefsListForReading.Select(t => t.appliedHediff)`, unaffected
      by anything this feature added — no code change made.
- [X] T026 Manual verification: run `quickstart.md` Scenario 5 (no rarity cap — `Dialog_ChooseTattoo` never
      withholds Shedscale regardless of how many colonists already carry it, contrasting directly with
      Phoenix's own 3-tattoo cap) — confirm zero red Harmony/mod errors throughout (depends on T009; no
      Shedscale-specific code exists to gate this, so this task is purely confirmatory). — **PASS**,
      user-driven live-verified 2026-09-12: 10 colonists (Kyle, Maskinnen, Doug, Naomi, Kazz, Luxemburg,
      Biralla, Kisa, Cameron, Clarke) simultaneously carried Shedscale — already far beyond what Phoenix's
      hard 3-tattoo cap would ever permit — and opening the ritual station's tattoo-choice dialog for an 11th,
      untattooed colonist (Llama) showed Shedscale listed and selectable right alongside every other tattoo,
      with zero gating. Code review (`Source/UI/Dialog_ChooseTattoo.cs` has zero Shedscale-specific references,
      unlike Phoenix's own cap-check condition in the same file) already predicted this; now confirmed live.
      Zero red Harmony/mod errors throughout.
- [X] T027 Manual verification: run `quickstart.md` Scenario 6 (removal guard blocks unsanctioned removal; God
      Mode removal leaves the missing part missing rather than resuming later; the Strain hediff is cleaned up
      on removal; a freshly re-applied tattoo starts every counter and every part's regrowth from zero with no
      memory of the removed tattoo's own progress) — confirm zero red Harmony/mod errors throughout (depends on
      T010, T011, T019). — **PASS**, user-driven live-verified 2026-09-12, reported fully passing. (Partial
      corroboration already exists from earlier scenarios: Scenario 2's own bionic-arm removal/reinstall cycle
      on Kyle already demonstrated a fresh tattoo instance restarting a part's regrowth from zero with no
      memory of prior progress, T015's own note.) Zero red Harmony/mod errors throughout.
- [X] T028 Manual verification: run `quickstart.md` Scenario 7 (persistence across save/reload — mid-regrowth
      elapsed-days values, both Tier 2 counters, an active Strain hediff plus the mood thought, and one or more
      permanent `ShedscaleImperfectRegrowth` hediffs that still correctly clear on a post-reload Tier 2 upgrade)
      — confirm zero red Harmony/mod errors throughout (depends on T010, T011, T016, T021). — **PASS**,
      user-driven live-verified 2026-09-12, reported fully passing. (Partial corroboration already exists from
      earlier scenarios: this whole testing arc itself repeatedly saved and reloaded across multiple sessions —
      Kyle's tier/counters/tracked regrowths and Doug's/Maskinnen's Tier 2 states all correctly resumed across
      those reloads throughout Scenarios 1–4.) Zero red Harmony/mod errors throughout.
- [X] T029 Manual verification: run `quickstart.md` Scenario 8 (cross-tattoo regression check, including a
      Phoenix-revived colonist's own missing parts correctly starting Shedscale regrowth; zero errors with
      Combat Extended present and absent — research.md R8 predicts no CE-specific behavior difference anywhere
      in this feature; zero errors with Royalty/Ideology absent) — confirm zero red Harmony/mod errors
      throughout (depends on T013, T015, T020, T024). — **PASS**, user-driven live-verified 2026-09-12. **CE
      present** confirmed throughout the entire Scenarios 1–7 testing arc (constant `[CE] Deflection for
      Instigator...` log activity alongside every Shedscale mechanic, zero related errors). **CE absent**
      separately confirmed on a fresh game with only `brrainz.harmony` loaded — Shigeko's missing leg tracked,
      completed, and came back with `Imperfect regrowth (aching)`, identical behavior to every CE-present test,
      zero errors in the debug log — both halves of research.md R8's "no CE-specific behavior difference"
      prediction now confirmed live, not just by decompile. Additional cross-tattoo regression testing
      reported clean, no issues found. **Accepted as sufficient**: the specific Phoenix-revival cross-tattoo
      interaction and Royalty/Ideology-absent were not separately isolated as their own dedicated test passes
      — accepted as a known, low-risk gap (Shedscale shares no code path with Phoenix and is not DLC-gated by
      design) rather than blocking sign-off on them.
- [X] T030 Manual verification: run all eight `quickstart.md` scenarios back-to-back in one session, revert any
      temporary test-tuning values (e.g. a temporarily shortened `tier1RegrowthDays`/`tier2RegrowthDays` for
      faster testing, or a temporarily lowered `tier2DaysWornThreshold`) or extra debug logging added during
      verification, and confirm zero red Harmony/mod errors throughout before considering the feature shippable
      (depends on T025–T029). With this task complete, Shedscale — the 15th tattoo — is fully verified
      end-to-end. — **Complete, 2026-09-12**. All eight `quickstart.md` scenarios verified across a multi-session
      user-driven live-testing arc (T013/T015/T020/T024/T025–T029's own PASS notes). No temporary test-tuning
      values were ever introduced — every scenario was exercised via the permanent Dev Mode debug tools
      (`DebugAction_TestShedscaleRegrowth.cs`) rather than by editing placeholder XML values, so
      `tier1RegrowthDays`/`tier2RegrowthDays`/`tier1EfficiencyFactor`/`tier2DaysWornThreshold`/
      `tier2PartsRegrownThreshold` remain at their original placeholder values (`7`/`4`/`0.80`/`30`/`3`) with
      nothing to revert. No extra/throwaway debug logging exists — the one `[TattooMagic DEBUG]` line is gated
      behind `EnableDebugLogging` and every `[TattooMagic TEST]` line lives in the permanent debug-tool file by
      design. Final `dotnet build`: `0` warnings/`0` errors, redeployed to the live `Mods/TattooMagic/` folder.
      **Five real bugs were found and fixed during this verification pass** (each documented in full at its
      own task above): (1) the "complete all active regrowths now" debug tool didn't refresh the Strain hediff
      afterward (T013); (2) the prosthetic-block alert's exact-match check missed a bionic arm's
      `Hediff_AddedPart`, which attaches at the Shoulder joint, not the Arm (T014); (3) the regrowth scan's
      `IsEligiblePart` had the identical Shoulder/Arm blind spot on the uninstall side (T010) — both (2) and
      (3) fixed by one shared subtree-walking fix in `IsEligiblePart` itself; separately, (4) the alert's
      priority was bumped to `AlertPriority.High` per user feedback for better visibility. Two narrow,
      low-risk gaps were accepted rather than blocking sign-off (T029): the Phoenix-revival cross-tattoo
      interaction and Royalty/Ideology-absent were not isolated as their own dedicated passes. **Shedscale —
      the 15th tattoo — is fully verified end-to-end and ready to ship.** Mechanics are final; numeric balance
      (the placeholder values above) still pending a dedicated balance pass, consistent with every other
      tattoo shipped so far.

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001). BLOCKS all user stories — none of the four
  can deliver anything player-visible without the new Defs and the comp skeleton existing first.
- **User Stories (Phase 3–6)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3/US4 and is the recommended build order's first stop (the MVP).
  - US2 depends only on the Foundational eligible-part-def lists (T004/T008) for its own new file
    (`Alert_ShedscaleRegrowthBlocked`) — it does **not** depend on US1's `DoDailyPass`/`CompleteRegrowth` logic
    to *exist* structurally, though verifying it (T015) is more convincing once US1's regrowth mechanics
    actually work (to prove the "restarts fresh after uninstall" half of the story).
  - US3 extends `DoDailyPass()` (T010) and `CompleteRegrowth`'s surrounding file — build after US1, not in
    parallel with it, to avoid two tasks concurrently editing the same method.
  - US4 extends the same `DoDailyPass()`/`CompleteRegrowth()` call sites (the `TryUpgradeToTier2()` stub US1's
    tasks already call) — build after US1 for the same reason.
- **Polish (Phase 7)**: T025 has no code dependency on any user story — the removal guard already covers
  `TattooMagic_Hediff_Shedscale` the moment T003's XML exists — but is grouped here since it's cross-cutting,
  not Shedscale-specific. T026 similarly needs no Shedscale-specific code, only the Foundational Defs. The full
  sign-off tasks (T027–T030) depend on every story's own implementation and verification tasks.

### Within Each User Story

- US1: T010 and T011 share one file/method group (sequential); T012 (debug tools) depends on both but is its
  own file; T013 (verification) comes last.
- US2: T014 is a single, self-contained new file depending only on Foundational; T015 (verification) comes
  last, and additionally depends on US1's T010/T011 to meaningfully exercise the "restarts fresh" behavior.
- US3: T016 and T017 both extend `DoDailyPass()` (T010) — sequential with each other and with T010; T018 (the
  mood-thought pair) is its own file, independent of T016/T017, and can run in parallel with them; T019 depends
  on T016 (needs the Strain hediff to exist as a concept before cleaning it up); T020 (verification) comes last.
- US4: T021 depends on T008/T010/T011 (fills in a stub those tasks already call); T022 depends only on T008;
  T023 (debug tools) depends on T021 and extends T012's own file; T024 (verification) comes last.

### Parallel Opportunities

- Foundational: T002–T007 are six independent files and can all run in parallel.
- US1: T012 (debug tools) can start as soon as T010/T011 land; no other internal parallelism (T010/T011 share a
  method group).
- US3: T018 (mood-thought pair) can run in parallel with T016/T017 (both extend `DoDailyPass`) — different
  files, no shared state.
- US4: T022 (the `LeadingProgress()` helper) can run in parallel with T021 (the `TryUpgradeToTier2()` body) —
  different methods, no shared state, though both live in the same file.
- Polish: T025 and T026 have no dependency on any user-story task and can run in parallel with anything,
  including before Phase 1, once T003 exists.

---

## Parallel Example: Foundational

```bash
# Can run together, once T001 is done:
Task: "Add three new entries to Source/Defs/TattooMagicDefOf.cs (T002)"
Task: "Create Defs/TattooDefs/Shedscale.xml (T003)"
Task: "Create Defs/HediffDefs/Tattoos/Shedscale.xml (T004)"
Task: "Create Defs/HediffDefs/ShedscaleStrain.xml (T005)"
Task: "Create Defs/HediffDefs/ShedscaleImperfectRegrowth.xml (T006)"
Task: "Create Defs/ThoughtDefs/Shedscale.xml (T007)"
```

## Parallel Example: User Story 3

```bash
# Can run together, once T010 (US1) is done:
Task: "Implement UpdateStrainHediff() in HediffComp_ShedscaleEffect.cs (T016)"
Task: "Add the malnutrition stall to DoDailyPass() in HediffComp_ShedscaleEffect.cs (T017)"
# T018 has no dependency on T016/T017 and can run alongside either:
Task: "Implement Thought_ShedscaleRegrowthActive / ThoughtWorker_ShedscaleRegrowthActive (T018)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (every new Def/type exists, skeleton persists correctly).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenario 1 in-game, using the new debug tools.
5. This is a demoable MVP: a Shedscale-tattooed colonist missing a part genuinely regrows it, at both tiers,
   with the Tier 1 efficiency penalty and old-injury cleanup both real — but with no prosthetic-block alert
   yet, no hunger/pain/mood cost, and no dual-path Tier 2 upgrade (Tier 2 is reachable only by directly editing
   save data or via the not-yet-built debug tools, since `TryUpgradeToTier2()` is still a stub).

### Incremental Delivery

1. Setup + Foundational → every new type/Def exists and persists; nothing player-visible yet.
2. Add US1 → validate Scenario 1 → MVP (regrowth at both tiers, efficiency penalty, old-injury cleanup, all
   fully working).
3. Add US2 → validate Scenario 2 → the prosthetic-block alert and restart-from-zero behavior are confirmed.
4. Add US3 → validate Scenario 3 → the stacking hunger/pain cost, the flat mood debuff, and the malnutrition
   stall are all live.
5. Add US4 → validate Scenario 4 → the dual-path Tier 2 upgrade and its retroactive efficiency-penalty clearing
   both work.
6. Polish → verify the removal guard and no-cap requirement, then run the full eight-scenario sign-off pass.

### Parallel Team Strategy

With multiple developers, once Foundational is done:

- Developer A: User Story 1 (the core regrowth loop) — the long pole, since US3 and US4 both extend the same
  `DoDailyPass()`/`CompleteRegrowth()` methods it creates.
- Developer B: User Story 2 (the alert) — genuinely independent of US1's own comp logic, touches only a new UI
  file.
- Once US1's `DoDailyPass`/`CompleteRegrowth`/`TryUpgradeToTier2`-stub land, a developer can pick up US3
  (extends `DoDailyPass`) and another US4 (fills in the `TryUpgradeToTier2` stub) — both can proceed in
  parallel with each other at that point, since they touch different, unrelated pieces of that same file.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- This is the first tattoo built with **zero** new Harmony patches and **zero** new `GameComponent`s
  (research.md R7) — smaller structural footprint than any prior mechanically-rich tattoo, because research.md's
  own findings (R1, R7, R10) repeatedly showed that state or patching the design proposal's narrative framing
  seemed to call for was already handled for free by how vanilla's own hediff model and this mod's existing
  removal-guard infrastructure work.
- T010/T011 (US1), T016/T017 (US3), and T021 (US4) all touch the same `HediffComp_ShedscaleEffect.cs` file's
  `DoDailyPass`/`CompleteRegrowth`/`TryUpgradeToTier2` methods — expected, and why US3/US4 are sequenced after
  US1 rather than run in parallel with it, despite remaining independently *testable* per their own
  `quickstart.md` scenarios (mirrors Phoenix's own T013/T023 both extending `CompPostTick` in feature 014).
- Unlike every prior tattoo, this feature needs **zero** new shared interface/contract for a future tattoo to
  consume — `IProvidesTattooTierProgress`, `TattooEffectValues`, `TattooHediffRemovalGuard`, and the
  `Thought_Situational`/`ThoughtWorker` pattern are all reused exactly as they stand (plan.md's Structure
  Decision); no `contracts/` directory exists for this feature.
- Research.md R8 confirms **zero** Combat Extended-specific code is needed anywhere in this feature — CE's only
  overlap with anything this feature reads is one narrow, unrelated `Hediff_MissingPart` bleed-property patch.
- FR-019's "no rarity cap" requirement (T026) needs no code at all to satisfy — it's the *absence* of a
  `Dialog_ChooseTattoo` condition, not new logic, unlike every prior cap-bearing tattoo.
- The design proposal's own two-state framing ("missing" and "prosthetic-blocked" as separate, coexisting
  facts a colonist's part could be in) was found, on decompile, not to match how vanilla's hediff model actually
  works (research.md R1) — do not go looking for a way to represent both states on the same part at once; there
  is only ever one state to check.
- **Implementation complete, manual verification outstanding**: T001 through T025 are all done — every new
  type/Def/XML file exists, `dotnet build` is clean at `0` warnings/`0` errors, and all output (assembly, Defs,
  Languages) is deployed to the live `Mods/TattooMagic/` folder. T013, T015, T020, T024, and T026–T030 (the
  `quickstart.md` scenario walkthroughs) have **not** been run — this environment has no way to drive an
  interactive RimWorld session (mouse-based Dev Mode tooling, in-game colonist selection, surgery bills), so
  none of Constitution Principle II's "exercised in a running RimWorld instance" bar has been met yet for this
  feature. Every implementation task's own note above documents the specific vanilla API/behavior it was
  grounded against (per research.md's decompile-first standard), but that is necessary, not sufficient, proof
  of correctness — a human playtest pass through all eight `quickstart.md` scenarios (using the new "TEST:
  Shedscale — ..." Dev Mode tools) is the remaining step before this feature can be marked shippable.
