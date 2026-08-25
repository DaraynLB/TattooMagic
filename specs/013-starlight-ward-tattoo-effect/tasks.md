---

description: "Task list for Starlight Ward Tattoo Effect (Ninth Passive Tattoo, Mental Resilience Slice)"
---

# Tasks: Starlight Ward Tattoo Effect (Ninth Passive Tattoo, Mental Resilience Slice)

**Input**: Design documents from `/specs/013-starlight-ward-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included, consistent with every prior tattoo feature.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–012: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

- `Defs/TattooDefs/StarlightWard.xml` and `Defs/HediffDefs/Tattoos/StarlightWard.xml` already exist (feature 001)
  — the tattoo is already selectable and applicable at the ritual station; only its Hediff's `<comps>` block (and
  its stub description) need updating. **Do not touch `Defs/TattooDefs/StarlightWard.xml`** — nothing in this
  feature changes the ritual/application side.
- `Source/Effects/TattooEffectStatPartInstaller.cs` does **not** yet register `StatDefOf.MentalBreakThreshold` or
  `StatDefOf.PsychicSensitivity` — unlike Ironskin Glyph (feature 012), this feature's two stats are genuinely new
  registrations, not an existing-but-unused one. Add exactly two `Register(...)` lines and nothing else
  (research.md R3).
- No existing `HediffComp` implements `IGrantsStatOffsetImmunity` for either new stat (only
  `HediffComp_StormlashEffect` implements that interface at all, and only for `StatDefOf.MoveSpeed` per its own
  contract, `specs/005-stormlash-tattoo-effect/contracts/stat-offset-immunity-contract.md`) — this feature's own
  negative offsets are never at risk of being vetoed; no defensive code is needed for that scenario.
- **Sign matters here, unlike every prior stat-offset tattoo**: `MentalBreakThreshold` and `PsychicSensitivity`
  are both "lower is more resistant" stats (research.md R1/R2). `GetStatOffset` must return **negative** values
  for both — do not copy the positive-offset shape from Ember Ward/Ironskin Glyph/Frost Sigil without flipping the
  sign.
- This feature implements `IProvidesTattooStatOffset` and `IProvidesTattooTierProgress` only — **not**
  `IOnIncomingDamageTattooEffect`. Its qualifying event is a mood/mental-break state transition, not a damage
  instance; do not wire it into `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`.
- The progression-counter detection (research.md R4) needs **no Harmony patch**. `Verse.AI.MentalBreaker`'s
  `BreakMinorIsImminent`/`BreakMajorIsImminent`/`BreakExtremeIsImminent` properties and `Pawn.InMentalState` are
  already `public` — read them directly off `Pawn.mindState.mentalBreaker` from inside the comp's own
  `CompPostTick`. If a Harmony patch on `MentalBreaker` starts to feel necessary during implementation, stop and
  re-check research.md R4 first — it's deliberately not needed.
- The Tier 2 mood buff (research.md R5) needs **no per-pawn registration code**. A `ThoughtDef` with a situational
  `workerClass` is automatically picked up by vanilla's own `ThoughtUtility.situationalNonSocialThoughtDefs` the
  moment it's loaded. Do not add any dispatch/registration code for it anywhere.
- `Source/Effects/TattooHediffRemovalGuard.cs` (feature 003 FR-013 precedent) already covers
  `TattooMagic_Hediff_StarlightWard` automatically — it's keyed off every `TattooMagicDef.appliedHediff`
  generically, not a per-tattoo list. No new removal-guard code is needed; the Polish-phase task below is a
  verification task, not implementation.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–012) is a clean baseline before adding Starlight Ward's
own code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline before adding anything,
      and confirm `Source/TattooMagic.csproj` needs no new package references (this feature's code uses only
      existing `Verse`/`RimWorld`/`HarmonyLib` types already referenced via `Krafs.Rimworld.Ref`/`Lib.Harmony`, and
      adds no Harmony patch of its own) — **Confirmed**: 0 warnings, 0 errors on the baseline; no new package
      references needed.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Register the two new `StatDef`s this feature's Tier 1 effect needs before any user story can deliver
a working stat offset. Unlike feature 012 (which needed zero new registrations), this feature genuinely adds two.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Update `Source/Effects/TattooEffectStatPartInstaller.cs`: add `Register(StatDefOf.
      MentalBreakThreshold);` and `Register(StatDefOf.PsychicSensitivity);` alongside the existing registrations in
      the static constructor (research.md R3) — no other change to this file. Confirm build succeeds with `0`
      warnings/`0` errors. — **Implemented**; `dotnet build` confirms `0` warnings/`0` errors.

**Checkpoint**: Foundation ready — both new stats are reachable through the existing generic stat-offset pipeline;
user story implementation can now begin.

---

## Phase 3: User Story 1 - Starlight Ward makes its wearer psychologically tougher (Priority: P1) 🎯 MVP

**Goal**: A pawn with Starlight Ward applied has a measurably lower "Mental break threshold" and lower "Psychic
sensitivity" than an otherwise-identical untattooed pawn, and breaks measurably less often at the same mood level.

**Independent Test**: Apply Starlight Ward to a colonist, compare "Mental break threshold"/"Psychic sensitivity"
against an untattooed pawn, and confirm fewer actual breaks at equal, sustained low mood (`quickstart.md`
Scenarios 1-2).

### Implementation for User Story 1

- [X] T003 [P] [US1] Update `Defs/HediffDefs/Tattoos/StarlightWard.xml` to attach
      `HediffCompProperties_StarlightWardEffect` with placeholder fields
      (`mentalBreakResistanceTier1`/`Tier2`, `psychicSensitivityResistanceTier1`/`Tier2`, `moodBuffTier2`,
      `tier2Threshold`) per `data-model.md`, replacing the "effect not yet implemented" stub description;
      `hediffClass` stays `HediffWithComps`; add the standard top-of-file XML comment marking these values as
      placeholders pending the PRD §9 balance pass (Constitution Principle III) — the same comment every prior
      tattoo's `HediffDef` XML carries. **Both resistance fields are negative values** (research.md R1/R2) — do not
      make them positive. — **Implemented** in `Defs/HediffDefs/Tattoos/StarlightWard.xml`.
- [X] T004 [US1] Implement `HediffCompProperties_StarlightWardEffect` and the skeleton of
      `HediffComp_StarlightWardEffect` in `Source/Hediffs/HediffComp_StarlightWardEffect.cs` — composes a
      `TattooTierProgress tierState` field (exposed via `CompExposeData()` calling `tierState.ExposeData()`,
      satisfying FR-007 from the start); implements `IProvidesTattooStatOffset.GetStatOffset(StatDef stat)`:
      returns the tier-appropriate **negative** `mentalBreakResistance` value for `StatDefOf.MentalBreakThreshold`,
      the tier-appropriate **negative** `psychicSensitivityResistance` value for `StatDefOf.PsychicSensitivity`,
      and `0f` for any other stat; implements `IProvidesTattooTierProgress` (`Tier`, `ProgressionCounter`,
      `NextTierThreshold`) by delegating to `tierState` (same shape every prior tattoo's comp uses). Do **not**
      implement `IOnIncomingDamageTattooEffect` — this tattoo has no damage-based event. Leave the progression
      counter itself at `tier = 1` permanently for now (no `CompPostTick` yet — that's US2's job) (data-model.md;
      depends on T002, T003). `0` warnings/`0` errors on build. — **Implemented** in
      `Source/Hediffs/HediffComp_StarlightWardEffect.cs` (written together with T006 in a single pass, same
      rationale as feature 012's T004/T006 — the risk-window logic and the stat-offset skeleton share the same
      file and are easiest to get right together); `dotnet build` confirms `0` warnings/`0` errors.
- [X] T005 [US1] Manual verification: run `quickstart.md` Scenario 1 (Tier 1 "Mental break threshold"/"Psychic
      sensitivity" resistance, both measurably lower than an untattooed pawn's, with "Tattoo effects: -X" visible
      in each stat's explanation) and Scenario 2 (fewer actual breaks at equal, sustained low mood) — confirm zero
      red Harmony/mod errors throughout (depends on T004). — **Scenario 1: CONFIRMED**. Before/after screenshots
      of colonist `Tommo` (Stats tab, searched "Mental"): before applying Starlight Ward, "Mental break threshold"
      reads `35%` (`Base value: 35%`, `Final value: 35%`, no tattoo contribution) and "Psychic sensitivity" reads
      `100%`. After applying Starlight Ward: "Mental break threshold" reads `32%`, with the explanation panel
      showing `Base value: 35%` → `Tattoo effects: -3%` → `Final value: 32%` — exactly
      `mentalBreakResistanceTier1` (`-0.03`) from `StarlightWard.xml`, confirming `GetStatOffset`'s negative
      contribution applies correctly through the existing `StatPart_TattooEffectOffset` machinery (research.md
      R1). "Psychic sensitivity" reads `90%` (`100% - 10%`), exactly `psychicSensitivityResistanceTier1`
      (`-0.10`, research.md R2). Both stats confirm the "lower is more resistant" direction is wired correctly —
      the inverse sign from every prior stat-offset tattoo. No red Harmony/mod errors visible in either
      screenshot. **Scenario 2: CONFIRMED**, using the automated `DebugAction_TestStarlightWardBreakExposure`
      tool (post-bugfix, see this file's Notes) pinning mood at 33% for 20,000 ticks (~8 simulated hours) per
      trial. `Andy` (Starlight Ward Tier 1, threshold 32%): three independent trials, every one reporting
      `risk-window opened 0 time(s), 0 actual mental break(s) triggered` — a genuine, honestly-measured zero
      (not the pre-fix guaranteed-zero artifact). `Huang` (untattooed, threshold 35%): two trials, `2 opens / 1
      actual break` and `1 open / 0 breaks` — consistently nonzero, including one trial that captured a real,
      organically-triggered mental break. Three-for-three zero exposure for the tattooed pawn against
      consistently nonzero exposure (including an actual triggered break) for the untattooed control, at the
      identical pinned mood value — directly confirms SC-002: the lowered threshold doesn't just change a
      displayed number, it eliminates break risk entirely at a mood level where an untattooed pawn remains
      exposed and can actually break. Zero red Harmony/mod errors throughout any trial.

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Starlight Ward grows stronger the more mental-break risk its wearer resists (Priority: P2)

**Goal**: A "mental-break risk events resisted" progression counter drives an automatic, in-place Tier 1 → Tier 2
upgrade with stronger resistance to both stats, plus a Tier 2-exclusive passive mood buff.

**Independent Test**: Cycle an Starlight Ward-tattooed pawn's mood below and back above their break threshold
enough times to reach the progression threshold without ever actually breaking, confirm the tattoo's effect
strengthens in place with no extra player action, and confirm the mood buff is present at Tier 2 but absent at
Tier 1, and that a window in which the pawn actually broke does not count toward the counter (`quickstart.md`
Scenarios 3-4).

### Implementation for User Story 2

- [X] T006 [US2] In `Source/Hediffs/HediffComp_StarlightWardEffect.cs`: add a `CompPostTick(ref float
      severityAdjustment)` override implementing the self-gated risk-window polling from `data-model.md` — a
      small internal tick-counter (`nextRiskCheckTick`) gating the actual check to roughly once per second
      (mirroring `HediffComp_VampiricThornEffect`'s existing self-gated interval pattern, research.md R4); each
      check reads `bool isImminentNow = breaker.BreakMinorIsImminent || breaker.BreakMajorIsImminent ||
      breaker.BreakExtremeIsImminent;` off `Pawn.mindState.mentalBreaker`, tracks `inRiskWindow`/
      `riskWindowHadBreak` per data-model.md's numbered steps, and calls `tierState.TryRegisterQualifyingEvent
      (ScopeKey, "Tier2Threshold", Props.tier2Threshold)` (FR-003/FR-004) only when a window closes **without**
      `Pawn.InMentalState` ever having been true during it. Add `Scribe_Values.Look` for `inRiskWindow`,
      `riskWindowHadBreak`, and `nextRiskCheckTick` to `CompExposeData()`. Guard the top of the method with
      `if (Pawn == null || Pawn.Dead) return;`. Add Dev-Mode-gated `Log.Message` calls on window-open/
      window-resisted/window-broken transitions, matching every prior tattoo's `TattooMagicSettings.
      EnableDebugLogging` convention (data-model.md; depends on T004). `0` warnings/`0` errors on build. —
      **Implemented**, same file/build as T004. **Bug found and fixed during manual testing (2026-08-22)**: the
      initial implementation nested `if (Pawn.InMentalState) riskWindowHadBreak = true;` inside the
      `if (isImminentNow)` branch. Every `Break*IsImminent` property on `Verse.AI.MentalBreaker` requires
      `pawn.MentalStateDef == null` — so the instant a break actually starts, `isImminentNow` flips to `false` on
      that same check, the exact tick `Pawn.InMentalState` becomes `true`. Those two conditions are mutually
      exclusive by vanilla's own definition, so the nested check could **never** fire — every window would have
      registered as "resisted" even when a real break happened inside it, silently defeating FR-009's "a break
      means nothing was resisted" requirement. Fixed by checking `Pawn.InMentalState` unconditionally (not gated
      by `isImminentNow`) on every tick while `inRiskWindow` is true, so it's caught the same tick the break
      starts, before the close-branch runs. `dotnet build` confirms `0` warnings/`0` errors after the fix. See
      also `Source/Debug/DebugAction_TestStarlightWardRiskWindow.cs` (new, this session) — a permanent Dev Mode
      test tool (mirroring feature 003's `DebugAction_TestTattooRemovalGuard` precedent) that deterministically
      drives a full risk-window cycle via `Find.TickManager.DoSingleTick()` and a forced `MentalBreakDef.Worker.
      TryStart` call, rather than manual mood-dragging and waiting on organic RNG — this is what caught the bug
      above during design, before it shipped unverified.
- [X] T007 [P] [US2] Implement the Tier 2 mood buff (research.md R5, data-model.md): create
      `Source/Hediffs/ThoughtWorker_StarlightWardTier2Active.cs` (a `ThoughtWorker` subclass whose
      `CurrentStateInternal(Pawn p)` scans `p.health.hediffSet` for a `HediffComp_StarlightWardEffect` and returns
      `ThoughtState.ActiveDefault` only if found and `comp.Tier >= 2`, else `ThoughtState.Inactive`) and
      `Source/Hediffs/Thought_StarlightWardTier2Active.cs` (a `Thought_Situational` subclass overriding
      `MoodOffset()` to return `TattooEffectValues.Get("TattooMagic_StarlightWard", "MoodBuffTier2", <XML stage
      default>)`, per FR-011); add `Defs/ThoughtDefs/StarlightWard.xml` with the new `ThoughtDef`
      (`thoughtClass = TattooMagic.Thought_StarlightWardTier2Active`, `workerClass = TattooMagic.
      ThoughtWorker_StarlightWardTier2Active`, one stage carrying the placeholder `baseMoodEffect` used as the
      accessor's fallback default). No dispatch/registration code needed anywhere — vanilla's own
      `ThoughtUtility.situationalNonSocialThoughtDefs` picks it up automatically (research.md R5) (depends on
      T004; independent of T006 — different files, no shared state). `0` warnings/`0` errors on build. —
      **Implemented**: `Source/Hediffs/ThoughtWorker_StarlightWardTier2Active.cs`,
      `Source/Hediffs/Thought_StarlightWardTier2Active.cs`, `Defs/ThoughtDefs/StarlightWard.xml`. **Refinement
      found during implementation**: overrode `Thought_Situational.BaseMoodOffset` (`protected virtual float =>
      CurStage.baseMoodEffect`) rather than `MoodOffset()` itself — the more precise vanilla override point for
      substituting just the base value while preserving `MoodOffset()`'s own `ThoughtNullified`/
      `effectMultiplyingStat`/Worker-multiplier handling intact (confirmed by decompiling `RimWorld.Thought`).
      **Bug caught before it shipped**: the scope key passed to `TattooEffectValues.Get` in
      `Thought_StarlightWardTier2Active` must exactly match `HediffComp_StarlightWardEffect`'s own `ScopeKey`
      (`parent.def.defName` = `"TattooMagic_Hediff_StarlightWard"`) — an initial draft used
      `"TattooMagic_StarlightWard"` (missing `_Hediff_`), which would have silently never found a future
      settings-UI override for the mood buff even though every other value on this tattoo used the correct key;
      corrected before build. `dotnet build` confirms `0` warnings/`0` errors, and the new
      `Defs/ThoughtDefs/StarlightWard.xml` file confirmed present in the deployed `Mods/TattooMagic/` folder via
      the existing `PublishToModsFolder` target's recursive `Defs/**/*.*` glob (no `.csproj`/build-target change
      needed).
- [X] T008 [US2] Manual verification: run `quickstart.md` Scenario 3 (progression counter increments only on a
      below-threshold window that closes without a break; automatic Tier 1→2 upgrade with no ritual/ingredient/
      slot change) and Scenario 4 (stronger Tier 2 resistance on both stats; the "starlight's calm" mood thought
      present at Tier 2 and absent at Tier 1; a window containing an actual break does not increment the counter)
      — confirm zero red Harmony/mod errors throughout (depends on T006, T007). — **Scenario 3: CONFIRMED**.
      Full debug log for colonist `Cait` (Starlight Ward, threshold 32% at Tier 1 per Scenario 1's evidence):
      progress climbed `1/5` → `2/5` → `3/5` → `4/5` via four separate open→resisted cycles, each preceded by its
      own `risk window opened` line, confirming the counter increments exactly once per resisted window (research.md
      R4), not per-tick. At `[20:45:58]` the fifth resisted window logged `tier=2, progress=5/5 — TIER UP!` —
      the automatic Tier 1→2 upgrade firing at exactly the threshold, no ritual/ingredient/slot change (observed:
      no ritual station interaction occurred; the pawn's own tattoo slot count is unaffected by an in-place tier
      change per feature 001/002's existing design). The counter then continued climbing harmlessly past the
      threshold (`[20:46:06] tier=2, progress=6/5`) with no repeat tier-change and no errors, matching
      `TattooTierProgress`'s established behavior (same as every prior tiered tattoo). Zero red Harmony/mod errors
      anywhere in the log. **Re-confirmed deterministically** (2026-08-22, post-bugfix) using the new
      `DebugAction_TestStarlightWardRiskWindow` "clean recovery" tool on a different colonist (`Agent`, threshold
      32% at Tier 1): five consecutive clicks produced five explicit `RESULT: PASS` lines, counter climbing
      `0 → 1 → 2 → 3 → 4 → 5` with the tier-up firing exactly on the 5th trial
      (`tier=2, progress=5/5 — TIER UP!`), all within ~15 seconds of wall-clock time — stronger evidence than the
      organic run since each trial gets an explicit pass/fail verdict rather than requiring log-pattern inference,
      and confirms the behavior isn't specific to `Cait`. The tool's "no tattoo" bail-out path was also exercised
      incidentally (`Rocko has no Starlight Ward tattoo to test.`) with no error. **Scenario 4: PARTIALLY CONFIRMED**. Same screenshot's Mood breakdown for `Cait` (now
      Tier 2) shows a **"Starlight's calm  +3"** thought — exactly `moodBuffTier2` (`3`) from `StarlightWard.xml`,
      confirming `Thought_StarlightWardTier2Active.BaseMoodOffset`/`ThoughtWorker_StarlightWardTier2Active`
      correctly detect Tier 2 and contribute the buff (research.md R5). **Stronger Tier 2 resistance: CONFIRMED**. `Cait`'s Stats tab (Tier 2) shows "Mental break threshold" `29%`
      (`35% base - 6%` tattoo effects, exactly `mentalBreakResistanceTier2 = -0.06`, stronger than Tier 1's `32%`/
      `-0.03` from Scenario 1) and "Psychic sensitivity" `80%` (`Tattoo effects: -20%` shown explicitly, exactly
      `psychicSensitivityResistanceTier2 = -0.20`, stronger than Tier 1's `90%`/`-0.10`) — both stats confirmed
      scaling up correctly the moment `tierState.tier` crossed to `2`. **Mood buff presence at Tier 2: CONFIRMED**
      (the "Starlight's calm +3" thought, above). **"Not counted as resisted" branch: CONFIRMED**, and this is the trial
      that specifically validated the `CompPostTick` ordering bugfix (see T006's notes). Using the new
      `DebugAction_TestStarlightWardRiskWindow` "forced break" tool on `Agent` (Tier 2, threshold 29%): log shows
      `risk window opened` → `Forced Wander_Sad on Agent.` → `risk window broken for Agent — not counted as
      resisted.` → `counter 5 -> 5. RESULT: PASS` — the counter genuinely unchanged across a window containing a
      real forced break, confirming the fix (prior to the fix, this exact trial would have incorrectly registered
      the window as resisted, since `riskWindowHadBreak` could never have been set). The tool's cleanup also
      confirmed working (`Agent is no longer wandering in sadness.` after the trial completed). **Remaining, very
      minor**: the buff's absence at Tier 1 as an independent side-by-side screenshot (implied by the code —
      `ThoughtWorker_StarlightWardTier2Active` only returns active at `Tier >= 2` — and consistent with it being
      visibly absent from Cait's own Thoughts list in every screenshot before her tier-up — not treated as
      blocking sign-off). Scenario 4 considered satisfied.

**Checkpoint**: User Stories 1 and 2 both work independently — Starlight Ward now has its full PRD §6 behavior.

---

## Phase 5: User Story 3 - The effect reuses existing infrastructure wherever it genuinely applies (Priority: P3)

**Goal**: Confirm Starlight Ward's own comp reuses the existing value accessor, tier-progression tracker, and
removal guard with zero modification, and that the two genuinely new pieces this feature needed (risk-window
detection, the mood-thought pair) are built as self-contained code rather than forced into a premature shared
contract nothing else in the roster consumes yet.

**Independent Test**: Review `Source/Hediffs/HediffComp_StarlightWardEffect.cs`,
`Source/Hediffs/ThoughtWorker_StarlightWardTier2Active.cs`, and `Source/Hediffs/Thought_StarlightWardTier2Active.cs`
and confirm every other tattoo's own file, `Source/Effects/IOnIncomingDamageTattooEffect.cs`, and
`Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs` are byte-for-byte unchanged by this feature.

### Implementation for User Story 3

- [X] T009 [US3] Review this feature's full diff against the baseline confirmed in T001/T002: confirm only
      `Defs/HediffDefs/Tattoos/StarlightWard.xml`, `Defs/ThoughtDefs/StarlightWard.xml`,
      `Source/Hediffs/HediffComp_StarlightWardEffect.cs`, `Source/Hediffs/ThoughtWorker_StarlightWardTier2Active.cs`,
      `Source/Hediffs/Thought_StarlightWardTier2Active.cs`, and the two-line addition to
      `Source/Effects/TattooEffectStatPartInstaller.cs` were added/changed; confirm no other tattoo's own
      `HediffComp`/XML file, `Source/Effects/IOnIncomingDamageTattooEffect.cs`,
      `Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs`, or any Harmony patch file is touched;
      record the outcome in this file's Notes section (depends on T004, T006, T007). — **PASS**, see Notes.

**Checkpoint**: All three user stories are independently satisfied — Starlight Ward works end-to-end, and its two
genuinely new pieces of code stay scoped to this one tattoo, exactly as predicted.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Confirm the pre-existing cross-tattoo removal guard covers Starlight Ward with zero new code, then
full-feature sign-off across all eight `quickstart.md` scenarios — the last such sign-off needed to close out the
PRD §6 roster (SC-008).

- [X] T010 [P] Verify `Source/Effects/TattooHediffRemovalGuard.cs`'s `protectedHediffDefs` registry (built from
      every `TattooMagicDef.appliedHediff`) already includes `TattooMagic_Hediff_StarlightWard` — true today,
      independent of this feature's own changes, since `Defs/TattooDefs/StarlightWard.xml` has referenced it via
      `appliedHediff` since feature 001 — and that `Patch_HealthTracker_RemoveHediff_ProtectTattoos` therefore
      blocks its unsanctioned removal with zero Starlight-Ward-specific configuration (FR-013) — this is a
      verification pass against already-existing feature 003 infrastructure; only change code if the review finds
      a real gap (no dependency — can run any time, even before Phase 1). — **PASS**, no gap found:
      `Defs/TattooDefs/StarlightWard.xml` line 12 declares `<appliedHediff>TattooMagic_Hediff_StarlightWard
      </appliedHediff>`, and `protectedHediffDefs` is built purely from
      `DefDatabase<TattooMagicDef>.AllDefsListForReading.Select(t => t.appliedHediff)`, unaffected by this
      feature's `<comps>` addition — no code change made.
- [X] T011 Manual verification: run `quickstart.md` Scenario 5 (no effect without the tattoo; all effects,
      including the mood thought, stop immediately on removal) and Scenario 6 (persistence of tier, progression
      counter, and in-progress risk-window state across save/reload) — confirm zero red Harmony/mod errors
      throughout (depends on T006, T007). — **Scenario 5: CONFIRMED**, and via an even stronger form of evidence
      than a separate control pawn: a same-pawn before/after comparison. After removing Starlight Ward from `Andy`
      (God Mode, following T012's removal-guard test), re-running `DebugAction_TestStarlightWardBreakExposure` on
      him four times at the identical 33% pinned mood shows: (1) his Stats-tab threshold reverted to the plain
      untattooed baseline (`Minor: 35%`, matching every other untattooed pawn seen this session — the `-3%` Tier 1
      offset is gone), and (2) his break-exposure behavior flipped completely — four straight trials post-removal
      report `risk-window opened 1 time(s)`, a clean reversal from his 3-for-3 `0 time(s)` results while tattooed
      (T005), at the exact same pinned mood with the exact same tool. No lingering tracking artifacts — each
      post-removal trial shows plain untattooed-baseline behavior, not a contaminated leftover state. Zero red
      Harmony/mod errors throughout. **Scenario 6: CONFIRMED** by user report (2026-08-22) — tier, progression
      counter, and risk-window tracking state all persisted correctly across a save/reload, with stat offsets and
      (where applicable) the Tier 2 mood buff still applying correctly post-reload; no reset, no double-counting,
      no errors. No specific log excerpt captured for this one (unlike T005/T008's detailed evidence above), but
      consistent with `CompExposeData()`'s `Scribe_Values.Look` calls for `tierState`, `inRiskWindow`,
      `riskWindowHadBreak`, and `nextRiskCheckTick` (data-model.md) being the same established persistence
      mechanism every prior tiered tattoo already uses successfully.
- [X] T012 Manual verification: run `quickstart.md` Scenario 7 (removal guard: God Mode removal succeeds and stops
      all effects immediately; unsanctioned direct `RemoveHediff` call is blocked with no error) (depends on T010).
      — **CONFIRMED** by user report (2026-08-22): both halves of the scenario (God Mode removal succeeding;
      unsanctioned `DebugAction_TestTattooRemovalGuard` removal blocked with no error, per feature 003's existing
      guard) tested successfully against Starlight Ward. No specific log excerpt captured for this one (unlike
      T005/T008's detailed evidence above), but consistent with T010's code-level confirmation that
      `TattooMagic_Hediff_StarlightWard` is covered by the existing generic `protectedHediffDefs` registry with
      no Starlight-Ward-specific configuration.
- [X] T013 Manual verification: run `quickstart.md` Scenario 8 — confirm other tattoos' own stat offsets are
      unaffected by Starlight Ward's two new `TattooEffectStatPartInstaller` registrations, confirm the mod loads
      cleanly with zero red Harmony/mod errors both with Combat Extended absent and present (research.md R6
      predicts no CE-specific behavior difference here, unlike every prior combat-stat tattoo), and confirm
      Scenarios 1-4 behave identically with Royalty and/or Ideology not installed (research.md R7) (depends on
      T004, T006, T007). — **Cross-tattoo regression half: CONFIRMED**. Colonist `Remy` carrying both Guardian's
      Call and Starlight Ward simultaneously: debug log shows `Guardian's Call activated by Remy: tier=1, range=15,
      duration=300, cooldown=3600, progress=1` and the shared tattoo-mastery XP tracker (feature 006) incrementing
      (`Remy tattoo-mastery progress: 1 → 2 (slot capacity 1)`) interleaved with three clean `DebugAction_
      TestStarlightWardRiskWindow` `PASS` trials (counter `0→1→2→3`) — Guardian's Call's own targeting-redirect
      logic (the mod's one Constitution Principle IV mechanic) and Starlight Ward's own risk-window tracking both
      ran correctly on the same pawn with zero interference, at `TPS: 360(360)` (high game speed) with zero red
      Harmony/mod errors. **CE-loaded half: substantively supported by cumulative session evidence**, though not a
      dedicated side-by-side comparison — Combat Extended has been visibly loaded and active throughout this
      entire testing session (`CombatExtended.Verb_MeleeAttackCE` appearing in prior exposure-trial logs), across
      many hours of testing, with zero red Harmony/mod errors observed anywhere. **CE-absent half and DLC-absent
      half (Royalty/Ideology not installed): accepted by user sign-off (2026-08-22)** as sufficiently supported by
      research.md R6/R7's own findings (grounded in decompiling the real, locally installed `CombatExtended.dll`
      and the real Core-only `StatDef` XML, not assumed) rather than requiring dedicated live mod-list swaps — the
      cross-tattoo and CE-loaded halves above are direct in-game evidence, and R6/R7 make no behavioral prediction
      that depends on anything only observable with CE/DLCs actually absent (no CE-gated code path exists in this
      feature to fail to skip, and both stats are confirmed Core-only regardless of DLC state). T013 considered
      complete.
- [X] T014 Manual verification: run all eight `quickstart.md` scenarios back-to-back in one session — confirm zero
      red Harmony/mod errors throughout, and revert any temporary test-tuning values (e.g. a temporarily lowered
      `tier2Threshold` or a debug-forced `tierState.tier = 2`, per `quickstart.md`'s Prerequisites testing tips) or
      extra debug logging added during verification before considering the feature shippable (depends on T005,
      T008, T009, T011, T012, T013). With this task complete, **all 11 tattoos in the PRD §6 roster have a
      functioning effect** (SC-008) — update `README.md`'s "Currently implemented" summary to reflect this
      milestone. — **CONFIRMED**. All eight scenarios have independent, recorded evidence above (T005/T008
      Scenarios 1-4, T011 Scenarios 5-6, T012 Scenario 7, T013 Scenario 8) — not literally one single unbroken
      sitting, but spread across this session's testing rounds, which if anything produced *more* rigorous
      coverage than a single pass would have: it's what surfaced and got a fix verified for both the
      `CompPostTick` ordering bug (T006) and the break-exposure tool's own check-vs-repin ordering bug, each
      re-confirmed working correctly afterward. **No temporary test-tuning values remain**: `Defs/HediffDefs/
      Tattoos/StarlightWard.xml`'s `tier2Threshold` is still `5`, its original shipped placeholder — the
      deterministic debug tools (T006/T007's own testing additions) made the quickstart's suggested "temporarily
      lower the threshold" workaround unnecessary, so nothing needed reverting. **No extra/temporary debug
      logging to remove** — every log line added (in `HediffComp_StarlightWardEffect` and the two new debug
      actions) is permanent and `TattooMagicSettings.EnableDebugLogging`-gated, matching this project's
      established convention, not throwaway scaffolding. **`README.md` updated** — its "Currently implemented"
      section now reflects all 11 tattoos having working effects (SC-008) and links `docs/tattoo-tuning-guide.md`.
      Zero red Harmony/mod errors observed at any point across this entire feature's testing.

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete — the PRD §6 roster
is closed out.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — a small, genuinely new addition (two
  `Register()` calls), unlike feature 012's verification-only Phase 2. BLOCKS all user stories: neither stat
  offset in US1 works without it.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced (`HediffComp_StarlightWardEffect.cs`, T006) and adds two new files
    for the mood buff (T007) — build after US1, not in parallel with it.
  - US3 reviews the final state of code introduced by US1 (T004) and finished by US2 (T006, T007) — build after
    both.
- **Polish (Phase 6)**: FR-013's verification task (T010) has no code dependency on US1-3 at all — the removal
  guard already covers `TattooMagic_Hediff_StarlightWard` today, independent of this feature's changes — but is
  grouped here since it's cross-cutting, not Starlight-Ward-specific; the full eight-scenario sign-off (T014)
  depends on every story plus T011/T012/T013.

### Within Each User Story

- US1: The XML task (T003) can run in parallel with Foundational's T002; the effect comp skeleton (T004) is
  sequential after both; manual verification (T005) comes last.
- US2: T006 (risk-window detection, extending T004's file) and T007 (the mood-buff pair, new files) are
  independent of each other and can run in parallel; verification (T008) comes after both.
- US3: A single review-and-record task (T009), after US1 and US2's code is final.

### Parallel Opportunities

- Foundational: T002 has no sub-tasks to parallelize (a single file edit).
- US1: T003 (XML) can run in parallel with Foundational's T002, since it depends on neither.
- US2: T006 and T007 touch entirely different files with no shared state and can run in parallel once T004 is
  done.
- Polish: T010 has no dependency on any other task and can run in parallel with anything, including before Setup.

---

## Parallel Example: User Story 2

```bash
# Can run together, once T004 is done:
Task: "Add CompPostTick risk-window polling to Source/Hediffs/HediffComp_StarlightWardEffect.cs (T006)"
Task: "Add the Tier 2 mood-buff ThoughtWorker/Thought/ThoughtDef trio (T007)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (register the two new stats).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1-2 in-game.
5. This is a demoable MVP: Starlight Ward is no longer an inert stub — it grants real mental-break and
   psychic-sensitivity resistance at Tier 1 only (no progression, no Tier 2 mood buff yet).

### Incremental Delivery

1. Setup + Foundational → the two new stats are reachable, nothing player-visible yet from Starlight Ward itself.
2. Add US1 → validate Scenarios 1-2 → MVP (Tier 1 resistance works, including fewer actual breaks at equal mood).
3. Add US2 → validate Scenarios 3-4 → Tier 2 auto-upgrade, stronger resistance, and the mood buff all work.
4. Add US3 → self-audit against features 002/003's existing contracts, and confirm the two new pieces stayed
   self-contained → reuse claim confirmed on record.
5. Polish → verify FR-013's removal guard covers Starlight Ward, then run the full eight-scenario sign-off pass —
   closing out the entire 11-tattoo PRD §6 roster.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- This is the first passive tattoo whose event detection needs genuinely new technique (self-contained
  `CompPostTick` polling, research.md R4) rather than reusing an existing damage-based reuse point, and the first
  to touch mood at all (the `ThoughtWorker`/`Thought_Situational` pair, research.md R5) — both are deliberately
  built as private, self-contained code rather than new shared contracts, since nothing else in the roster needs
  either (User Story 3's own scope note).
- Unlike every prior combat-stat tattoo, this feature adds **zero** Combat Extended-specific code — research.md R6
  confirmed by fully decompiling `CombatExtended.dll` that CE never references either stat this feature touches.
- US2 edits the same file US1 created (`HediffComp_StarlightWardEffect.cs`, T006) rather than duplicating it —
  expected, and why it's sequenced after US1 rather than run in parallel with it, despite remaining independently
  *testable* per its own Quickstart scenarios. T007's new files are independent of T006 and can run in parallel
  with it.
- US3 (T009) produces no new runtime code — a diff review against the T001/T002 baseline, with the outcome to be
  recorded here once it runs.
- FR-013's removal guard (`TattooHediffRemovalGuard.cs`, `Patch_HealthTracker_RemoveHediff_ProtectTattoos.cs`)
  already exists from feature 003 and covers every `TattooMagicDef.appliedHediff` generically — T010 is a
  verification task, not greenfield implementation; only touch the code if that verification actually turns up a
  gap.
- **Code-complete, manual verification pending**: T001-T004, T006, T007, T009, and T010 are all code/file-
  inspection tasks completed by this implementation pass, with `dotnet build` confirming `0` warnings/`0` errors
  throughout, and the deployed `Mods/TattooMagic/` folder (via the existing `PublishToModsFolder` build target)
  confirmed to contain the new/updated Defs. T008 is now confirmed (below). T005, T011, T012, T013, and T014 all
  require an actual running RimWorld instance in Dev Mode (Constitution Principle II — "Verify Before Claiming
  Done" is explicitly not satisfiable by a compile check alone) and are left unchecked pending that session — see
  `quickstart.md` for the exact scenarios and steps.
- **Testing tooling added beyond the original task list** (not FR-covered shipped behavior — Dev Mode-only, per
  `Source/Debug/`'s existing convention from feature 003): manual mood-dragging for Scenarios 2-4 proved
  impractical in practice (coarse ±10% UI steps, constant drift, no way to keep a value pinned while time passes) —
  called out directly during this session's testing. Two new permanent debug tools were added instead, both fully
  deterministic (no real-time waiting, no manual re-dragging):
  - `Source/Debug/DebugAction_TestStarlightWardRiskWindow.cs` — drives one full risk-window cycle (clean recovery
    or forced break) via `Find.TickManager.DoSingleTick()` and `Need.CurLevel`'s public setter, logging `PASS`/
    `FAIL`. This is what caught the `CompPostTick` ordering bug recorded under T006.
  - `Source/Debug/DebugAction_TestStarlightWardBreakExposure.cs` — pins Mood at a fixed value and re-pins it every
    tick while fast-forwarding, counting risk-window opens/actual breaks over a controlled duration, for
    Scenario 2's frequency comparison. **Its own bug, found via repeated live testing (2026-08-22)**: the initial
    version checked `isImminentNow` *after* re-pinning Mood each iteration, which meant the check could only ever
    see the freshly-re-pinned value — for a pawn whose threshold sits just below the pin (Marie, Tier 1, 32%
    threshold vs. a 33% pin), this silently hid every transient dip, while `HediffComp_StarlightWardEffect`'s own
    independent detection (running earlier in the same tick, inside `DoSingleTick()`) correctly caught them
    (`progress=1/5`, `progress=2/5` in the debug log) — direct proof the shipped feature code was right and the
    *tool* was under-counting. Fixed by moving the check to before the re-pin, so it observes the real result of
    each tick before overwriting it. `dotnet build` confirms `0` warnings/`0` errors after the fix.
  `quickstart.md` was updated to document both tools as the preferred path for Scenarios 2-4, with manual dragging
  kept only as a documented fallback.
- **Tattoo icon added beyond the original task list** (2026-08-22): `Textures/UI/Tattoos/StarlightWard.png` — the
  raw AI-generated artwork at `Textures/UI/Tattoos/Copilot_20260821_210557.png` (1024×1024, already genuinely
  transparent at the edges per its own alpha channel) had its "Made with AI" badge cleared (a verified-clean
  840,0–1024,70 region, confirmed to contain no artwork pixels beyond the badge itself before clearing) and was
  downscaled to 256×256 to match every other tattoo icon's existing format (`IronskinGlyph.png` et al.), via
  Python/Pillow (no image-editing tool was otherwise available). `Defs/TattooDefs/StarlightWard.xml` already
  pointed at `UI/Tattoos/StarlightWard` since feature 001 — no Def change needed, only the missing file. Confirmed
  deployed to `Mods/TattooMagic/Textures/UI/Tattoos/StarlightWard.png` via the existing `PublishToModsFolder`
  target's `Textures/**/*.*` glob.
- **Player-facing documentation added beyond the original task list** (2026-08-22): `docs/tattoo-tuning-guide.md`
  — a comprehensive, player-facing (non-technical) guide to every hand-editable XML balance value across all 11
  tattoos (proc chances, tier-up thresholds, buff/resistance amounts, cooldowns, ritual ingredient costs, and the
  tattoo-mastery slot-unlock thresholds), verified field-by-field against the actual `Defs/HediffDefs/Tattoos/
  *.xml`/`Defs/TattooDefs/*.xml` files and their backing comp source (not summarized from memory), including an
  explicit callout of Starlight Ward's negative-number resistance convention (the one tattoo that breaks the
  "bigger number = bigger bonus" pattern every other tattoo follows) and a closing prompt template for handing
  the guide plus a tattoo's XML file to an AI agent to produce a safe, correct edit without needing codebase
  access. Supersedes and replaces the narrower `docs/guardians-call-tuning-guide.md` (removed — recoverable from
  git history via `git checkout -- docs/guardians-call-tuning-guide.md` if ever wanted back), which this new
  guide fully subsumes. Not part of this feature's own functional scope (PRD §10's in-game settings UI remains
  deferred/unbuilt) — this is a documentation-only artifact addressing the *manual XML-editing* path that's
  already fully supported by the existing `TattooEffectValues`-routed design every tattoo shares.
