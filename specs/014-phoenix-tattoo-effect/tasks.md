---

description: "Task list for Phoenix Tattoo Effect"
---

# Tasks: Phoenix Tattoo Effect

**Input**: Design documents from `/specs/014-phoenix-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included, consistent with every prior tattoo feature.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–013: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

- This is the 14th tattoo, and the first to touch death, corpses, or faction-wide state at all — nothing in
  `Source/Effects/`, `Source/Hediffs/`, or `Source/Patches/` today has any precedent for this feature's core
  mechanics. Every file this feature creates is genuinely new; no existing tattoo's file needs modification
  except `Source/UI/Dialog_ChooseTattoo.cs` (one additive condition, US2) and `Source/Defs/TattooMagicDefOf.cs`
  (three new entries, Foundational).
- **No custom `Hediff` subclass is needed.** `TattooMagic_Hediff_Phoenix` stays a plain `HediffWithComps`, unlike
  the closest vanilla precedent (`Hediff_DeathRefusal`, which overrides `Notify_PawnDied`/
  `Notify_PawnCorpseDestroyed`). The wait/retry timer is driven entirely by `GameComponent_PhoenixRegistry`'s own
  `GameComponentTick()` sweep — a deliberate deviation, not an oversight (research.md R3): a normal `HediffComp`
  goes silent the instant its pawn dies (`Pawn_HealthTracker.HealthTick()`/`HealthTickInterval()` both
  early-return `if (Dead)`), and the vanilla `Corpse.TickRare()`-hook pattern cannot survive the cremation
  exception destroying the `Corpse` `Thing` mid-wait. **Do not add `Notify_PawnDied`/`Notify_PawnCorpseDestroyed`
  overrides or a `Corpse.TickRare` patch** — if either starts to feel necessary during implementation, stop and
  re-read research.md R3 first.
- **`GameComponent_PhoenixRegistry` needs zero manual registration code.** RimWorld's own `Game.FillComponents()`
  already auto-discovers every non-abstract `GameComponent` subclass by reflection during `Game.ExposeData()` and
  instantiates it if not already present (research.md R11) — do not add a `Game.FinalizeInit` Harmony patch or
  any other wiring to add this component to the game.
- **`HediffComp_PhoenixEffect` does not compose the shared `TattooTierProgress` class.** It implements
  `IProvidesTattooTierProgress` directly with its own `tier`/`daysAliveCounter` fields, because Phoenix's own
  progression counter (days alive) resets to `0` on every revival — semantically different from every other
  tiered tattoo's monotonically-increasing counter, which `TattooTierProgress.TryRegisterQualifyingEvent`
  assumes. Do not try to force-fit `TattooTierProgress` here.
- **`ResurrectionUtility.TryResurrect(pawn, ...)` silently fails to spawn the pawn if `pawn.Corpse` is already
  `null`** (research.md R4, a real decompile-confirmed gap, not hypothetical) — this only matters for the
  cremation path (US3), since cremation destroys the `Corpse` `Thing` before the guaranteed revival later
  resolves. The natural revival path (US1) never hits this, since a live `pawn.Corpse` is a precondition for
  every non-cremation attempt in the first place. Capture `corpse.PositionHeld`/`corpse.MapHeld` at cremation
  time and fall back to an explicit `GenSpawn.Spawn(...)` only in that path — do not add the fallback-spawn logic
  to the natural-revival path, it isn't needed there and would just be dead code.
- **Burial ends revival, same as outright destruction** (spec's own Assumptions section, explicitly flagged there
  as an unconfirmed working assumption — carried forward here unchanged, not re-litigated). A buried corpse is
  **not** a destroyed `Thing` in vanilla (`Building_Grave` holds it live in its own `ThingOwner`) — the
  permanent-loss check needs an explicit `corpse.ParentHolder is Building_Grave` condition alongside the
  destroyed/dessicated checks (research.md R6), or a buried corpse will be missed.
- **Cauterization stops bleeding via `Hediff.Tended(1f, 1f)`, never via `Severity` reduction** — vanilla's own
  `Gene_Clotting` (Biotech) is the direct precedent for this exact call shape (research.md R8). Reducing
  `Severity` is what `TattooHealingUtility.HealWorstInjury` (Vampiric Thorn) already does for a conceptually
  different purpose (healing) and doesn't work for `Hediff_MissingPart` bleeds (a missing limb has no "severity"
  to heal) — do not reuse that utility or that approach here.
- **Cauterization burns always use `burnSeverityTier1`, regardless of the wearer's own tier** (FR-029) — this is
  the one place Tier 2 does *not* get a stronger numeric effect, only a higher proc chance. Do not scale the
  cauterize burn severity by tier.
- **`HediffComp_Disappears.ticksToDisappear`/`disappearsAfterTicks` are plain public fields**, confirmed by
  direct decompile (research.md R10) — set both directly after `AddHediff`, computed per-occurrence from
  `abasiaBaseDurationDays + (abasiaOccurrenceCount - 1) * abasiaDurationIncrementDays` (evaluated *after*
  incrementing `abasiaOccurrenceCount` for the current occurrence). No custom `HediffComp` is needed for
  Paralytic Abasia's duration.
- Every numeric this feature introduces — wait days, the decomposition-vs-chance curve, burn severities, Abasia
  duration/increment, the Tier-2 day threshold, cauterize proc chances, the trivial-bleed floor, and the faction
  cap itself — lives on `HediffCompProperties_PhoenixEffect`'s XML fields and is read exclusively through
  `TattooEffectValues.Get(...)` at point of use (Constitution Principle III), exactly like every prior tattoo.
  The decomposition curve is a `SimpleCurve` (mirrors `TattooMagicDef.successCurveOverride`'s existing
  convention) whose last control point sits at a small nonzero Y value, so FR-013's "approaches but never
  reaches zero" falls out of `SimpleCurve.Evaluate`'s own clamp-to-last-point behavior with no separate
  floor-clamping code.
- `Source/Effects/TattooHediffRemovalGuard.cs` (feature 003) already covers `TattooMagic_Hediff_Phoenix`
  automatically once `Defs/TattooDefs/Phoenix.xml` declares it as `appliedHediff` — it's keyed off every
  `TattooMagicDef.appliedHediff` generically, not a per-tattoo list. No new removal-guard code is needed; the
  Polish-phase task below is a verification task, not implementation.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–013) is a clean baseline before adding Phoenix's own
code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline before adding
      anything, and confirm `Source/TattooMagic.csproj` needs no new package references — this feature's code
      uses only existing `Verse`/`RimWorld`/`HarmonyLib` types already referenced via `Krafs.Rimworld.Ref`/
      `Lib.Harmony` (research.md). — **Confirmed**: baseline built with `0` warnings/`0` errors; no new
      package references were needed anywhere in this feature.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Get every one of Phoenix's new types, XML Defs, and persisted fields into existence — every user
story needs some slice of this before it can deliver anything player-visible. Unlike any prior feature's
Foundational phase, this one is substantial: Phoenix is the first tattoo needing a `GameComponent`, and the
first whose core comp has no shared `TattooTierProgress`/damage-event interface to lean on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Add three new entries to `Source/Defs/TattooMagicDefOf.cs`: `TattooMagic_Phoenix`
      (`TattooMagicDef`), `TattooMagic_Hediff_Phoenix` (`HediffDef`), `TattooMagic_Hediff_ParalyticAbasia`
      (`HediffDef`) — mirrors every existing entry's shape (data-model.md). — **Implemented**.
- [X] T003 [P] Create `Defs/TattooDefs/Phoenix.xml`: the recipe-side `TattooMagicDef`
      (`tattooType = Passive` — no activatable gizmo, `appliedHediff = TattooMagic_Hediff_Phoenix`, standard
      `ingredients`/`workAmount` shape matching every prior tattoo's recipe XML) (data-model.md, plan.md Project
      Structure). — **Implemented** in `Defs/TattooDefs/Phoenix.xml` (well-formed XML confirmed). References an
      `iconPath` of `UI/Tattoos/Phoenix` with no texture file yet — falls back to `BaseContent.BadTex`
      (`TattooMagicDef.Icon`'s own existing behavior); adding the actual artwork is out of this feature's own
      functional scope, same as several prior tattoos shipped their effect before their icon.
- [X] T004 [P] Create `Defs/HediffDefs/Tattoos/Phoenix.xml`: `TattooMagic_Hediff_Phoenix`, `hediffClass =
      HediffWithComps`, `<comps>` carrying `TattooMagic.HediffCompProperties_PhoenixEffect` with every
      placeholder tunable from data-model.md's field table (`waitDaysTier1`/`Tier2`, `decompositionChanceCurve`
      as a `SimpleCurve` with a small nonzero final-point Y, `burnSeverityTier1`/`Tier2`,
      `abasiaBaseDurationDays`, `abasiaDurationIncrementDays`, `tier2DaysAliveThreshold`,
      `cauterizeChanceTier1`/`Tier2`, `minimumQualifyingBleedRate`, `factionCap`), under the standard
      top-of-file XML comment marking these as placeholders pending this spec's own Assumptions/PRD §9 balance
      pass (Constitution Principle III). — **Implemented**. Curve points: `(0, 0.9) (1, 0.6) (3, 0.3) (7, 0.1)
      (14, 0.02)` — confirmed via `python -m xml.dom.minidom` that the file is well-formed, and the exact
      `<points><li>(x, y)</li></points>` shape was verified against real vanilla Core XML
      (`FactionDefs/Factions_Misc.xml`) before writing, not guessed.
- [X] T005 [P] Create `Defs/HediffDefs/ParalyticAbasia.xml`: `TattooMagic_Hediff_ParalyticAbasia`, sits directly
      under `Defs/HediffDefs/` (not `Tattoos/`, mirroring `TattooTracker.xml`/`FrostSigilSlow.xml`'s existing
      support-hediff placement) — `hediffClass = HediffWithComps`, `<stages>` applying severe `capMods`
      (near-zero `Moving`/`Manipulation`/`Consciousness`, "near-total paralysis" per the spec), one comp:
      vanilla's own `HediffCompProperties_Disappears` (data-model.md, research.md R10). No duration value in the
      XML itself — duration is set programmatically per occurrence (US1). — **Implemented**: `capMods` use
      `setMax` (a hard cap, not an additive `offset`) — `Moving`/`Manipulation` capped at `0.05`, `Consciousness`
      at `0.3` — verified against vanilla's own `Anesthetic` hediff (`Hediffs_Global_Misc.xml`) as the real
      `capMods`/`setMax` XML precedent for "render near-helpless," rather than guessed syntax.
- [ ] T006 Implement the `HediffCompProperties_PhoenixEffect`/`HediffComp_PhoenixEffect` skeleton in
      `Source/Hediffs/HediffComp_PhoenixEffect.cs`: every runtime field from data-model.md's table (`tier`,
      `daysAliveCounter`, `nextDaysAliveCheckTick`, `revivalAttemptCount`, `abasiaOccurrenceCount`,
      `reviveAttemptTick`, `cremationGuaranteed`, `cremationFallbackPos`, `cremationFallbackMap`,
      `nextCauterizeCheckTick`); `CompExposeData()` persisting all of them (`Scribe_Values`/
      `Scribe_References` as appropriate); `IProvidesTattooTierProgress` implemented directly off `tier`/
      `daysAliveCounter` (no composed `TattooTierProgress` — see Known Baseline); `CompPostMake()` calling
      `Current.Game?.GetComponent<GameComponent_PhoenixRegistry>()?.Register(Pawn)` (data-model.md). Leave
      `CompPostTick` as a `base.CompPostTick(...)`-only stub for now — US1 and US4 fill in its two independent
      self-gated checks later. Depends on T002, T004. — **Implemented** in `Source/Hediffs/
      HediffComp_PhoenixEffect.cs` — written in one complete pass together with T013/T023's own logic rather than
      literally left as a stub first (same file, no separate build/verify checkpoint needed between the skeleton
      and its two `CompPostTick` behaviors).
- [X] T007 Implement the `GameComponent_PhoenixRegistry` skeleton in `Source/Effects/
      GameComponent_PhoenixRegistry.cs`: `registeredWearers` (`List<Pawn>`), the `(Game game)` constructor,
      `Register(Pawn)`/`Unregister(Pawn)` (idempotent), `IsAtCap` (reads `factionCap` via `TattooEffectValues`),
      `ExposeData()` (`Scribe_Collections.Look(..., LookMode.Reference)` + the `PostLoadInit` null-guard), and a
      small static accessor (e.g. `Get()` wrapping `Current.Game?.GetComponent<GameComponent_PhoenixRegistry>
      ()`) for other call sites to use (data-model.md, research.md R11). Leave `GameComponentTick()`'s sweep body
      empty for now (self-gate field only) — US1 fills in the actual sweep logic. Depends on T002. —
      **Implemented**, written in one pass together with T012's sweep body. Decompile-confirmed during
      implementation (not assumed) that RimWorld's own `Game.FillComponents()` auto-discovers every non-abstract
      `GameComponent` subclass by reflection and instantiates it — no `Game.FinalizeInit` patch or any other
      registration code was added, per research.md R11.
- [X] T008 [P] Implement `PhoenixRevivalUtility.ApplyBurn(Pawn pawn, BodyPartRecord part, float severity)` in
      `Source/Effects/PhoenixRevivalUtility.cs`: `HediffMaker.MakeHediff(HediffDefOf.Burn, pawn, part)` → set
      `.Severity` → `pawn.health.AddHediff(hediff)` (research.md R9 — reuses vanilla's own `Burn` `HediffDef`,
      relies on vanilla's existing injury-merge logic for stacking, no custom `HediffDef`). Declare (but leave
      unimplemented/`throw new NotImplementedException()`) `TryResolveAttempt` and
      `RegisterAttemptAndMaybeApplyAbasia` — US1 implements both. No dependency on T006/T007 — `ApplyBurn` only
      needs `Pawn`/`BodyPartRecord`/`float`, not any Phoenix-specific type. — **Implemented**, written in one pass
      together with T010/T011 (no intermediate `NotImplementedException` stub was actually committed). **Correction
      found during implementation**: `HediffDefOf.Burn` does not exist — vanilla's own curated `HediffDefOf`
      class has no static field for the `Burn` `HediffDef` (confirmed by decompile), only its bare `defName`
      `"Burn"` in `Hediffs_Local_Injuries.xml`. Resolved once via `DefDatabase<HediffDef>.GetNamed("Burn")` inside
      a `[StaticConstructorOnStartup]` static constructor (mirrors `CombatExtendedInterop`'s own def-lookup-at-
      startup pattern), not a per-call `DefDatabase` lookup and not a (nonexistent) `HediffDefOf.Burn` reference.
      **Updated post-implementation** (see T010's own note): `ApplyBurn` now also marks the created hediff
      permanent immediately via its `HediffComp_GetsPermanent` comp (vanilla's `Burn` def already carries one),
      fixing a real bug where `ResurrectionUtility.TryResurrect`'s own cleanup pass silently deleted a
      non-permanent burn applied just before it, on a successful revival.
- [X] T009 Confirm `dotnet build` succeeds with `0` warnings/`0` errors on the Foundational skeleton (depends on
      T002–T008). — **Confirmed**: `0` warnings/`0` errors after two missing `using RimWorld;` directives
      (`GenDate` lives in `RimWorld`, not `Verse`) were added to `HediffComp_PhoenixEffect.cs` and
      `GameComponent_PhoenixRegistry.cs`. Build output and the new/updated `Defs/**` confirmed deployed to the
      live `Mods/TattooMagic/` folder via the existing `PublishToModsFolder` target.

**Checkpoint**: Foundational ready — every new type/Def exists and persists correctly (even though most of them
do nothing yet); user story implementation can now begin.

---

## Phase 3: User Story 1 - A colonist cheats death (Priority: P1) 🎯 MVP

**Goal**: A colonist wearing Phoenix who actually dies with an intact, unburied body automatically returns to
life after the tier's wait period, rolling a decomposition-based chance daily until it succeeds or the body is
permanently lost — bearing a fresh burn on every attempt, with an escalating Paralytic Abasia debuff from the
3rd lifetime attempt onward, and a separate days-alive clock that permanently upgrades the tattoo to Tier 2.

**Independent Test**: Apply Phoenix to a colonist, kill them with their body left intact and unburied, and
observe that they automatically return to life after the tier's wait period, bearing a new burn injury
(`quickstart.md` Scenario 1).

### Implementation for User Story 1

- [X] T010 [US1] Implement `PhoenixRevivalUtility.TryResolveAttempt(Pawn pawn, HediffComp_PhoenixEffect comp)`
      in `Source/Effects/PhoenixRevivalUtility.cs` (research.md R1/R2/R6, data-model.md): when **not**
      `comp.cremationGuaranteed`, re-check the permanent-loss conditions (corpse null/`Destroyed`/`Dessicated`/
      `ParentHolder is Building_Grave`, plus the FR-008 head/brain-intact gate via `pawn.health.hediffSet
      .GetBrain() != null`) — on any failure, clear `reviveAttemptTick`, unregister from
      `GameComponent_PhoenixRegistry`, return `false` with no burn/no attempt counted; otherwise evaluate
      `Props.decompositionChanceCurve.Evaluate(rotProgressDays)` and `Rand.Chance(...)`. Always call `ApplyBurn`
      at the tier-appropriate severity (success or failure, FR-015) and
      `RegisterAttemptAndMaybeApplyAbasia(pawn, comp, succeeded)`. On failure, reschedule
      `reviveAttemptTick = TicksGame + GenDate.TicksPerDay` (FR-012). On success, call
      `ResurrectionUtility.TryResurrect(pawn, ...)`, reset `daysAliveCounter`/`nextDaysAliveCheckTick` to their
      initial values (FR-019), and clear `reviveAttemptTick`/`cremationGuaranteed`/both fallback fields. Depends
      on T008. — **Implemented** in `Source/Effects/PhoenixRevivalUtility.cs` (`IsPermanentlyLost` factored out
      as its own public method, also consumed by T012's sweep). The revival burn is applied to
      `pawn.RaceProps.body.corePart` (the Torso for humans) — the spec doesn't pin a specific body part for
      revival burns (unlike cauterize burns, which always target the actual bleeding part), and `corePart` is
      the vanilla-standard "generic whole-body location" choice, decompile-confirmed as a real public
      `BodyDef` field. **Real bug found via live in-game testing (post-implementation) and fixed**: a
      successful revival left no burn visible at all — `ResurrectionUtility.TryResurrect`'s own
      `Notify_Resurrected` call unconditionally strips every non-permanent, curable `Hediff_Injury` (vanilla
      `Burn` included) as part of "waking up," which silently deleted the just-applied burn on the very same
      tick. Fixed in T008's `ApplyBurn` — see that task's own note and research.md R9's corrected write-up.
- [X] T011 [US1] Implement `PhoenixRevivalUtility.RegisterAttemptAndMaybeApplyAbasia(Pawn pawn,
      HediffComp_PhoenixEffect comp, bool succeeded)` in the same file: always `comp.revivalAttemptCount++`
      (FR-016); if `succeeded && comp.revivalAttemptCount >= 3` (FR-017), increment
      `comp.abasiaOccurrenceCount`, then `HediffMaker.MakeHediff(TattooMagicDefOf.
      TattooMagic_Hediff_ParalyticAbasia, pawn)` → `pawn.health.AddHediff(...)` → fetch its
      `HediffComp_Disappears` → set both `ticksToDisappear`/`disappearsAfterTicks` to
      `(Props.abasiaBaseDurationDays + (comp.abasiaOccurrenceCount - 1) * Props.abasiaDurationIncrementDays) *
      GenDate.TicksPerDay` (FR-018, research.md R10). Depends on T010 (same file/method group). — **Implemented**.
      Uses vanilla's own `HediffComp_Disappears.SetDuration(int ticks)` convenience method (decompile-confirmed
      it sets both fields in one call) rather than setting `ticksToDisappear`/`disappearsAfterTicks` separately
      — the exact same call shape `HediffComp_FrostSigilEffect.GrantSlow` already uses for its own
      programmatically-timed hediff, found and reused rather than reinvented.
- [X] T012 [US1] Implement `GameComponent_PhoenixRegistry.GameComponentTick()`'s sweep body in `Source/Effects/
      GameComponent_PhoenixRegistry.cs`: self-gated to roughly once per 2,000 ticks; for each pawn in
      `registeredWearers`: skip if not `Dead`; if `Dead` and `reviveAttemptTick == 0`, this is a newly-observed
      death — check the FR-008 head/brain/not-destroyed precondition immediately, and either schedule
      `reviveAttemptTick = TicksGame + waitDays(comp.tier) * GenDate.TicksPerDay` or, if already ineligible,
      unregister immediately with no wait scheduled; if `reviveAttemptTick != 0` and `TicksGame >=
      reviveAttemptTick`, call `PhoenixRevivalUtility.TryResolveAttempt`; if `reviveAttemptTick != 0`,
      `TicksGame < reviveAttemptTick`, and **not** `cremationGuaranteed`, re-check the permanent-loss
      conditions every sweep (not only at the scheduled attempt) so a corpse destroyed/buried mid-wait is
      unregistered promptly (research.md R3/R6, data-model.md). Depends on T007, T010. — **Implemented**,
      iterating `registeredWearers` backwards by index so in-loop `RemoveAt`/`Unregister` calls are safe. Also
      added a Dev-Mode-only `ForceSweepNow()` method (resets the self-gate, then calls `GameComponentTick()`
      directly) for T014's debug tooling, not part of the spec's own shipped behavior. **Real bug found via live
      in-game testing and fixed**: a registered wearer whose own `Pawn` Thing was destroyed *while still alive*
      (Dev Mode's "T: Destroy" used directly on a living colonist, bypassing `Kill()`/the corpse/death pathway
      entirely — `pawn.Dead` never becomes `true`) was permanently stuck occupying a cap slot forever, since the
      sweep only ever evaluated `Dead` wearers. Fixed by adding a `pawn.Destroyed` check ahead of the `Dead`
      branch — `Thing.Destroyed` is `true` immediately once `Destroy()` runs regardless of the pawn's living/dead
      state, and is unaffected by the already-handled, separate case of a *dead* wearer's own `Corpse` being
      destroyed (destroying a `Corpse` doesn't itself destroy the `Pawn` object it wraps). **Third real bug found
      via live testing, after a save/reload specifically**: `registeredWearers` came back from a reload
      undercounting a dead, mid-wait wearer (2 instead of the actual 3, confirmed via the "force registry sweep
      now" debug tool's own count log), silently letting the cap be bypassed. Root cause not fully pinned down —
      `Scribe_Collections.Look(..., LookMode.Reference)` should in principle resolve a tracked `Pawn` regardless
      of `Spawned`/`Dead` state — but fixed the symptom with a `SyncFromActualHediffs()` self-heal
      (`GameComponent_PhoenixRegistry`), mirroring `HediffComp_TattooTracker.SyncAppliedTattoosFromActualHediffs`'s
      own established precedent (feature 006). Deliberately does **not** rely on `PawnsFinder`'s "AliveOrDead"
      enumerations for the dead half of the scan — decompile-confirmed `MapPawns.AllPawnsUnspawned` explicitly
      filters out any `Dead` pawn, so a corpse sitting on a home map (not yet in `WorldPawns`) would still be
      missed; every map's own `Corpse` things (`ThingRequestGroup.Corpse`) are scanned directly instead. Called
      both from the sweep itself (self-heals periodically with no player action) and from `Dialog_ChooseTattoo`
      (self-heals the instant the cap is actually checked, same trigger point `HediffComp_TattooTracker`'s own
      sync already uses).
- [X] T013 [US1] In `Source/Hediffs/HediffComp_PhoenixEffect.cs`, add the days-alive advance to `CompPostTick`:
      guard `if (Pawn == null || Pawn.Dead) return;` before this feature's own checks; self-gated (once per
      in-game day via `nextDaysAliveCheckTick`) `daysAliveCounter++`; if `tier == 1 && daysAliveCounter >=
      TattooEffectValues.Get(ScopeKey, "Tier2DaysAliveThreshold", Props.tier2DaysAliveThreshold)`, set
      `tier = 2` and reset both `revivalAttemptCount` and `abasiaOccurrenceCount` to `0` in the same step
      (FR-019/FR-019a). Depends on T006. — **Implemented**. The tier-2-threshold check was factored into its own
      `TryUpgradeToTier2()` method rather than inlined directly in the day-counter tick, so T014's "set
      days-alive counter" debug tool (which sets `daysAliveCounter` directly rather than incrementing it) can
      re-run the exact same upgrade check without either double-incrementing the counter or duplicating the
      threshold logic — a real ordering bug (calling the increment-and-check method after directly setting the
      counter would have made the tool's own self-gate immediately block it) was caught and fixed during this
      same implementation pass, before it ever shipped.
- [X] T014 [P] [US1] Add Dev Mode debug tools in `Source/Debug/DebugAction_TestPhoenixRevival.cs` per
      `quickstart.md`: "TEST: Phoenix — kill pawn now", "TEST: Phoenix — force registry sweep now", "TEST:
      Phoenix — set corpse rot progress", "TEST: Phoenix — force pending attempt to resolve now", "TEST:
      Phoenix — set days-alive counter", "TEST: Phoenix — set lifetime attempt counter" — mirrors feature 013's
      own deterministic-debug-tool precedent for a mechanic too slow to hand-test. Depends on T012, T013 (calls
      into the sweep and the comp's own fields) but is its own new file — no shared state with T013 itself. —
      **Implemented**, all six tools present. **Implementation-time adjustment from the plan**: "set corpse rot
      progress" targets a `Corpse` under the mouse cursor via `DebugActionType.ToolMap` (reading
      `UI.MouseCell()`), not `ToolMapForPawns` — decompiling vanilla's own `ToolMap`/`ToolMapForPawns` dev-tool
      dispatch during implementation showed `ToolMapForPawns` iterates spawned `Pawn` objects specifically, which
      a dead pawn no longer is once their `Corpse` replaces them on the map; the "force pending attempt to
      resolve now" tool (which also needs to target a dead pawn's corpse) uses the same `ToolMap`-plus-
      `UI.MouseCell()` pattern for the same reason. The numeric-value tools ("set corpse rot progress", "set
      days-alive counter", "set lifetime attempt counter") use vanilla's own `Verse.Dialog_Slider` for value
      entry (the same dev-tool-numeric-input mechanism vanilla itself uses, e.g. for its "set age" tool) rather
      than free-text entry.
- [X] T015 [US1] Manual verification: run `quickstart.md` Scenario 1 (natural revival at both tiers with daily
      retry; permanent loss on outright destruction or full decay) using the new debug tools — confirm zero red
      Harmony/mod errors throughout (depends on T010–T014). — **PASS**, live-verified 2026-08-28/29 across
      multiple colonists (Von, Estelle, Adams): natural decomposition-chance revival succeeded and failed
      correctly (failures reschedule exactly one day later per FR-012), Tier 1 and Tier 2 wait timing both
      confirmed, a burn applied on every attempt regardless of outcome, and permanent loss correctly triggered
      on outright corpse destruction, a missing brain, and a destroyed-while-still-alive pawn (a real gap found
      and fixed in the registry sweep itself — see Notes). `quickstart.md` Scenario 4 (Tier 2 progression and
      Paralytic Abasia, also User Story 1, no dedicated task number of its own) was verified in the same pass:
      a 3rd lifetime successful attempt applied Abasia at the base 2-day duration, a 4th escalated it to 3 days
      (refreshing the existing hediff in place rather than stacking a second copy), and a failed attempt
      advanced the lifetime counter without ever applying Abasia. Zero red Harmony/mod errors throughout.

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Keeping Phoenix rare across the colony (Priority: P2)

**Goal**: No more than 3 Phoenix tattoos can be simultaneously active across the player's whole faction — the
ritual station stops offering it once the cap is reached, a pending-revival wearer still counts, a freed slot
(permanent loss, successful removal) reopens the option, and leaving the faction strips the tattoo automatically
while a rescued/recruited pawn who already has it is allowed to keep it as a temporary 4th.

**Independent Test**: With 3 colonists already wearing Phoenix tattoos, attempt to apply a 4th at the ritual
station and confirm it is unavailable; then free a slot and confirm a 4th application becomes possible again
(`quickstart.md` Scenario 2).

### Implementation for User Story 2

- [X] T016 [P] [US2] Update `Source/UI/Dialog_ChooseTattoo.cs`: add one additive condition in the existing
      tattoo-listing loop, alongside the existing `recipientTracker.HasTattoo(tattoo)` skip — `continue` when
      `tattoo == TattooMagicDefOf.TattooMagic_Phoenix && GameComponent_PhoenixRegistry.Get()?.IsAtCap == true`
      (FR-006, data-model.md). No other change to this file. Depends on T007. — **Implemented**; no other line
      in this file was touched.
- [X] T017 [P] [US2] Implement `Patch_Pawn_SetFaction_PhoenixRegistryUpdate.cs` in `Source/Patches/`: a postfix
      on `Verse.Pawn.SetFaction` — when the pawn carries `TattooMagic_Hediff_Phoenix` and is leaving
      `Faction.OfPlayer`, strip it via `TattooHediffRemovalGuard.RemoveTattooHediff` and call
      `GameComponent_PhoenixRegistry.Get()?.Unregister(pawn)` (FR-004 — covers recruit-away/defect/exile/
      enslave, research.md R7); when the pawn already carries the hediff and is joining `Faction.OfPlayer`, call
      `Register(pawn)` (allowed even over cap, FR-005). Factor the strip-and-unregister logic into one small
      shared static helper this and T018 both call. Depends on T007. — **Implemented**. Capturing the pawn's
      *previous* faction needs a Harmony `Prefix`/`Postfix` pair with `out Faction __state` (a postfix alone
      only ever sees the *new* faction, already applied by the time it runs) — standard Harmony state-passing,
      matching this project's own established patching style elsewhere. The shared helper
      (`PhoenixFactionLeaveUtility.StripAndUnregister`) is a top-level static class in this same file, referenced
      directly by T018's patch (same namespace) rather than nested inside the patch class.
- [X] T018 [P] [US2] Implement `Patch_PawnGuestTracker_SetGuestStatus_PhoenixCaptured.cs` in `Source/Patches/`: a
      postfix on `RimWorld.Pawn_GuestTracker.SetGuestStatus` covering plain capture-as-prisoner, which
      `SetFaction` alone doesn't see (research.md R7) — calls the same shared strip/unregister helper T017
      introduces. Depends on T007, T017 (shares T017's helper). — **Implemented**. `Pawn_GuestTracker.pawn` is a
      *private* field with no public accessor (decompile-confirmed) — read via a cached `AccessTools.Field(...)`
      reflection lookup, the same `HarmonyLib.AccessTools` reflection technique this codebase's own CE-interop
      patches already use (`Patch_CE_ProjectileImpact_TattooAmmoBonus.cs`), not a new pattern. Guards on
      `pawn.Faction == Faction.OfPlayer && newHost != Faction.OfPlayer` so only "our own colonist captured by
      someone else" triggers the strip — not the reverse case of the player capturing an enemy.
- [X] T019 [US2] Manual verification: run `quickstart.md` Scenario 2 (cap enforcement across application,
      pending-revival, freed slots, join-while-over-cap, and faction-leave stripping) — confirm zero red
      Harmony/mod errors throughout (depends on T016–T018). — **PASS**, live-verified 2026-08-28: all six steps
      confirmed — a 4th application correctly blocked at 3/3 with the cap tooltip shown; a dead-but-pending
      wearer still counts; a resolved/successful revival still counts; freeing slots (down to 2) correctly
      re-opened the option; a pawn recruited while already carrying the hediff was allowed to keep it as a
      temporary 4th via the real `Pawn.SetFaction` code path, without opening a 5th slot for anyone else; and
      leaving the faction correctly auto-stripped the hediff and freed the slot immediately. Zero red
      Harmony/mod errors throughout.

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Guaranteeing a comeback through cremation (Priority: P3)

**Goal**: Deliberately cremating a Phoenix-tattooed corpse via the crematorium's own bill guarantees the next
scheduled revival attempt succeeds, bypassing the decomposition-based chance — without shortening the wait, and
at the cost of any gear left on the corpse.

**Independent Test**: Let a Phoenix-tattooed corpse decompose to a point where its natural revival chance would
be very low, cremate it via the crematorium building's bill, and confirm the next scheduled attempt succeeds
regardless of decomposition (`quickstart.md` Scenario 3).

### Implementation for User Story 3

- [X] T020 [US3] Implement `Patch_RecipeWorker_ConsumeIngredient_PhoenixCremation.cs` in `Source/Patches/`: a
      prefix on `Verse.RecipeWorker.ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)` — when
      `recipe.defName == "CremateCorpse"` and `ingredient is Corpse corpse` whose `InnerPawn` carries
      `TattooMagic_Hediff_Phoenix`, set `cremationGuaranteed = true` and capture `cremationFallbackPos =
      corpse.PositionHeld` / `cremationFallbackMap = corpse.MapHeld` on that pawn's `HediffComp_PhoenixEffect`
      **before** the base method's `ingredient.Destroy()` runs (research.md R4/R5). Depends on T006. —
      **Implemented**. Confirmed by decompiling `Corpse.Destroy()`/`PostCorpseDestroy` during this
      implementation pass (not merely assumed from research.md) that it unconditionally calls
      `pawn.apparel?.DestroyAll()` (→ `wornApparel.ClearAndDestroyContents`, a real destroy, not a drop) for
      *any* corpse destruction — independently confirming FR-023 needs no code of its own here.
- [X] T021 [US3] Update `PhoenixRevivalUtility.TryResolveAttempt` (`Source/Effects/PhoenixRevivalUtility.cs`,
      T010) to add the cremation-guaranteed branch: when `comp.cremationGuaranteed`, skip both the permanent-loss
      precondition checks and the decomposition roll entirely and always succeed (FR-020); after calling
      `ResurrectionUtility.TryResurrect(pawn, ...)`, if `!pawn.Spawned && comp.cremationFallbackMap != null`,
      explicitly call `GenSpawn.Spawn(pawn, comp.cremationFallbackPos, comp.cremationFallbackMap)` as a fallback
      for vanilla's own gap when `pawn.Corpse` no longer exists (research.md R4). Depends on T010, T020. —
      **Implemented together with T010** (both branches were written in the same pass in
      `PhoenixRevivalUtility.TryResolveAttempt`, since the cremation branch is a conditional fork inside the
      same method, not a separate one added afterward).
- [X] T022 [US3] Manual verification: run `quickstart.md` Scenario 3 (guaranteed revival regardless of
      decomposition; wait not shortened; gear on the corpse lost; counts toward the Abasia counter; a negative
      control confirming ordinary fire/destruction does **not** guarantee revival) — confirm zero red Harmony/
      mod errors throughout (depends on T020, T021). — **PASS**, live-verified 2026-08-28: a deliberately
      cremated corpse's next attempt succeeded regardless of decomposition, `cremationGuaranteed` was confirmed
      set on the comp before destruction, all apparel on the corpse was destroyed (inherent vanilla bill
      behavior, no new code), and the attempt counted toward the lifetime/Abasia counter like any other. The
      negative control was independently confirmed earlier via Scenario 1's own permanent-loss cases (ordinary
      destruction never sets `cremationGuaranteed`). Zero red Harmony/mod errors throughout.

**Checkpoint**: User Stories 1, 2, and 3 all work independently.

---

## Phase 6: User Story 4 - A tattoo that quietly staunches bleeding (Priority: P4)

**Goal**: Independent of the death/revival mechanic entirely, a Phoenix-tattooed colonist has a passive chance
to auto-cauterize a severe (non-trivial) bleeding wound before it becomes fatal, always prioritizing a bleeding
severed/missing limb over any other wound, with Tier 2 only improving the proc chance, never the burn severity.

**Independent Test**: Inflict a severe bleeding wound on a Phoenix-tattooed colonist and observe that, over
repeated trials, the wound is sometimes automatically cauterized without any change to the colonist's revival
wait timer or cap status (`quickstart.md` Scenario 5).

### Implementation for User Story 4

- [X] T023 [US4] In `Source/Hediffs/HediffComp_PhoenixEffect.cs`, add the self-gated cauterize check to
      `CompPostTick` (independent of T013's days-alive block — same method, no shared state, FR-025): self-gated
      to roughly once per real second via `nextCauterizeCheckTick`; scan `Pawn.health.hediffSet.hediffs` for
      `Hediff_MissingPart` bleeds (always top priority) and `Hediff_Injury` bleeds with `BleedRate >=
      TattooEffectValues.Get(ScopeKey, "MinimumQualifyingBleedRate", Props.minimumQualifyingBleedRate)`
      (FR-026/FR-027, research.md R8); if a qualifying candidate exists, roll the tier-appropriate
      `cauterizeChanceTier1`/`Tier2`; on success, call `.Tended(1f, 1f)` on every qualifying bleed sharing the
      target's exact `BodyPartRecord` (FR-028) and call `PhoenixRevivalUtility.ApplyBurn(Pawn, targetPart,
      TattooEffectValues.Get(ScopeKey, "BurnSeverityTier1", Props.burnSeverityTier1))` — **always** Tier 1
      severity regardless of the wearer's own tier (FR-029, see Known Baseline). Depends on T006, T008, T013. —
      **Implemented** as `CheckCauterize()`/`FindCauterizeTarget()`, called from the same `CompPostTick` T013
      extends. `Hediff.Tended(float quality, float maxQuality, int batchPosition = 0)` and both
      `Hediff_Injury.BleedRate`/`Hediff_MissingPart.BleedRate` returning exactly `0f` once tended were
      decompile-confirmed during this pass, alongside vanilla's own `Gene_Clotting` (Biotech) as a direct,
      already-shipped precedent for "periodically `Tended()` every bleeding hediff found" — the same call shape
      reused here, not reinvented.
- [X] T024 [US4] Manual verification: run `quickstart.md` Scenario 5 (proc only on non-trivial bleeds;
      most-severe-body-part targeting; severed-limb absolute priority; Tier 2 higher proc chance with identical
      burn severity; zero interaction with revival-mechanic state) — confirm zero red Harmony/mod errors
      throughout (depends on T023). — **PASS**, live-verified 2026-08-28/29, all six quickstart steps: a single
      real wound was detected, rolled, and cauterized with zero change to `daysAliveCounter`/`reviveAttemptTick`;
      a trivial wound was proven excluded by inverting `minimumQualifyingBleedRate` to an unreachable value (the
      dev tools available can't produce a wound small enough to fail the real placeholder floor directly, so the
      exclusion check itself was verified instead — same logical guarantee, more reliable than hunting for a
      naturally-small wound); two simultaneous wounds on different parts correctly resulted in only the current
      top candidate being treated per proc, the other left untouched; a severed-limb bleed was correctly
      prioritized over a competing higher-severity ordinary wound; and a direct Tier 1 (2%) vs Tier 2 (98%)
      side-by-side comparison confirmed Tier 2 procs dramatically more often while both burns came out at an
      identical severity (`18.0`, always `burnSeverityTier1` regardless of wearer tier, per FR-029). One real
      bug found and fixed during this pass — see Notes (missing-limb burn placement). Zero red Harmony/mod
      errors throughout after the fix.

**Checkpoint**: All four user stories are independently functional — Phoenix has its full spec.md behavior.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Confirm the pre-existing cross-tattoo removal guard covers Phoenix with zero new code, then
full-feature sign-off across all eight `quickstart.md` scenarios.

- [X] T025 [P] Verify `Source/Effects/TattooHediffRemovalGuard.cs`'s `protectedHediffDefs` registry (built from
      every `TattooMagicDef.appliedHediff`) already includes `TattooMagic_Hediff_Phoenix` — true automatically
      once T003 exists, independent of any of this feature's own runtime code — and that
      `Patch_HealthTracker_RemoveHediff_ProtectTattoos` therefore blocks its unsanctioned removal with zero
      Phoenix-specific configuration (FR-009). Verification-only — only change code if the review finds a real
      gap (no dependency — can run any time once T003 exists). — **PASS**, no gap found (code review only, no
      live game needed for this one — same as feature 013's own T010): `Defs/TattooDefs/Phoenix.xml` declares
      `<appliedHediff>TattooMagic_Hediff_Phoenix</appliedHediff>`, and `protectedHediffDefs` is built purely from
      `DefDatabase<TattooMagicDef>.AllDefsListForReading.Select(t => t.appliedHediff)`, unaffected by anything
      this feature added — no code change made.
- [X] T026 Manual verification: run `quickstart.md` Scenario 6 (removal guard blocks unsanctioned removal; God
      Mode removal succeeds and frees the cap slot; the tattoo survives death and multiple failed attempts
      unconsumed) — confirm zero red Harmony/mod errors throughout (depends on T015, T019, T022). — **PASS**,
      live-verified 2026-08-29 using a dedicated debug tool that directly attempts an unsanctioned
      `RemoveHediff` in code (stronger evidence than just checking the health tab's delete button): correctly
      blocked with God Mode off, correctly succeeded with God Mode on. The tattoo was independently confirmed
      to survive death and multiple failed attempts unconsumed throughout every Scenario 1/4 cycle. One real bug
      found and fixed during this pass — see Notes (a God-Mode-removed living wearer's cap slot never freed).
      Zero red Harmony/mod errors throughout after the fix.
- [X] T027 Manual verification: run `quickstart.md` Scenario 7 (persistence across save/reload — mid-wait,
      mid-cremation-guarantee, mid-Tier-2-progression, and the faction count at exactly 3/3) — confirm zero red
      Harmony/mod errors throughout (depends on T015, T019, T022, T024). — **PASS**. Rather than one dedicated
      save/reload pass, this was exercised continuously and repeatedly across the entire 2026-08-28/29 testing
      arc, reloading the same save many times over: `reviveAttemptTick` (mid-wait), `cremationGuaranteed`
      (confirmed surviving multiple reloads days apart — this is in fact how Estelle's cremation-guarantee flag
      was first discovered to be set correctly at all), `revivalAttemptCount`/`abasiaOccurrenceCount`, and the
      registry's own self-healed wearer count all correctly resumed every time. A colonist sitting mid-Tier-2-
      progression was separately confirmed to survive a reload. Zero red Harmony/mod errors throughout.
- [X] T028 Manual verification: run `quickstart.md` Scenario 8 (cross-tattoo regression check; zero errors with
      Combat Extended present and absent — research.md R12 predicts no CE-specific behavior difference anywhere
      in this feature; zero errors with Royalty/Ideology absent) — confirm zero red Harmony/mod errors
      throughout (depends on T015, T019, T022, T024). — **Verified, 2026-08-29**: confirmed zero red
      Harmony/mod errors with Combat Extended present (the entire testing arc ran with it active) and,
      separately, with it fully absent (a fresh save/colonist with no CE installed ran the full kill → schedule
      → fail → retry → succeed revival cycle cleanly) — research.md R12's "zero CE-specific behavior difference"
      prediction holds both ways. Guardian's Call and Phoenix confirmed coexisting cleanly on the same colonist
      (independent cooldowns/progress, Phoenix's revival/Abasia cycle unaffected). Vampiric Thorn's
      `IOnMeleeHitLandedTattooEffect` interaction confirmed once the shared test colonist (Galina) landed a
      melee hit — `HediffComp_VampiricThornEffect.OnMeleeHitLanded` fired via `DispatchMeleeHitLanded` with
      correct lifesteal/mastery credit, on a pawn who had already been through a full Phoenix death→revival
      cycle in the same session (no interference either direction). Royalty/Ideology-absent also confirmed
      clean.
- [X] T029 Manual verification: run all eight `quickstart.md` scenarios back-to-back in one session, revert any
      temporary test-tuning values (e.g. a temporarily raised `cauterizeChanceTier1`/`Tier2` for Scenario 5,
      per `quickstart.md`'s own testing note) or extra debug logging added during verification, and confirm zero
      red Harmony/mod errors throughout before considering the feature shippable (depends on T025–T028). With
      this task complete, Phoenix — the 14th tattoo — is fully verified end-to-end. — **Complete, 2026-08-29**.
      All temp-tuning values reverted to real placeholders: `cauterizeChanceTier1`/`Tier2` and
      `minimumQualifyingBleedRate` (`0.15`/`0.35`/`0.15`) earlier, and `waitDaysTier1`/`Tier2` restored to
      `5`/`3` (from the `0.5`/`0.25` fast-testing values) in `Defs/HediffDefs/Tattoos/Phoenix.xml` —
      `dotnet build` clean (`0`/`0`), redeploy to the live `Mods/TattooMagic/` folder confirmed. All
      temporary debug trace logging (the cremation-patch diagnostic) removed; the standard
      `EnableDebugLogging`-gated `[TattooMagic DEBUG]` lines are the established proactive-logging pattern and
      stay. All eight quickstart scenarios confirmed passing with zero red Harmony/mod errors. Phoenix — the
      14th tattoo — is fully verified end-to-end. Mechanics are final; numeric balance still pending PRD §9.

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001). BLOCKS all user stories — none of the four
  can deliver anything player-visible without the new Defs, the comp/GameComponent skeletons, and `ApplyBurn`
  existing first.
- **User Stories (Phase 3–6)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3/US4 and is the recommended build order's first stop (the MVP).
  - US2 depends only on the Foundational `GameComponent_PhoenixRegistry` skeleton (T007) — it does **not**
    depend on US1's revival logic being implemented, and could in principle be built before or in parallel with
    US1's own tasks, though verifying it (T019) is more convincing once US1's kill/revive tooling (T014) exists.
  - US3 extends `PhoenixRevivalUtility.TryResolveAttempt`, the same method US1 (T010) creates — build after US1,
    not in parallel with it.
  - US4 extends `HediffComp_PhoenixEffect.CompPostTick`, the same method US1 (T013) extends, and calls
    `PhoenixRevivalUtility.ApplyBurn` (T008) — independent of US1's revival-roll/Abasia/cap logic in every other
    respect, but sequenced after T013 to avoid two tasks editing the same method concurrently.
- **Polish (Phase 7)**: T025 has no code dependency on any user story — the removal guard already covers
  `TattooMagic_Hediff_Phoenix` the moment T003's XML exists — but is grouped here since it's cross-cutting, not
  Phoenix-specific. The full sign-off tasks (T026–T029) depend on every story's own implementation and
  verification tasks.

### Within Each User Story

- US1: T010 and T011 share one file/method group (sequential); T012 (the sweep) depends on T007 and T010; T013
  (days-alive) only depends on T006 and can run in parallel with T010–T012; T014 (debug tools) depends on T012/
  T013 but is its own file; T015 (verification) comes last.
- US2: T016, T017, and T018 all depend only on the Foundational T007 and touch three different files — T017 and
  T018 share one small helper (T018 sequenced just after T017 for that reason); T016 is fully independent of
  both. T019 (verification) comes last.
- US3: T020 depends on T006; T021 depends on T010 (extends the same method) and T020 (needs the flag/fields
  T020 sets); T022 (verification) comes last.
- US4: T023 depends on T006, T008, and T013 (extends the same `CompPostTick` method T013 already touched); T024
  (verification) comes last.

### Parallel Opportunities

- Foundational: T002, T003, T004, T005 are four independent files and can all run in parallel; T008 has no
  dependency on T006/T007 and can run in parallel with both.
- US1: T013 (days-alive) can run in parallel with T010–T012 (the revival-roll/sweep chain) — different methods,
  no shared state until T014 needs both.
- US2: T016, T017, and T018 (once T017's helper exists) can all run in parallel — three different files.
- Polish: T025 has no dependency on any user-story task and can run in parallel with anything, including before
  Phase 1, once T003 exists.

---

## Parallel Example: Foundational

```bash
# Can run together, once T001 is done:
Task: "Add three new entries to Source/Defs/TattooMagicDefOf.cs (T002)"
Task: "Create Defs/TattooDefs/Phoenix.xml (T003)"
Task: "Create Defs/HediffDefs/Tattoos/Phoenix.xml (T004)"
Task: "Create Defs/HediffDefs/ParalyticAbasia.xml (T005)"
Task: "Implement PhoenixRevivalUtility.ApplyBurn in Source/Effects/PhoenixRevivalUtility.cs (T008)"
```

## Parallel Example: User Story 2

```bash
# Can run together, once T007 is done:
Task: "Add the Phoenix cap-check condition to Source/UI/Dialog_ChooseTattoo.cs (T016)"
Task: "Implement Patch_Pawn_SetFaction_PhoenixRegistryUpdate.cs (T017)"
# T018 follows T017 (shares its strip/unregister helper), but is still a distinct file:
Task: "Implement Patch_PawnGuestTracker_SetGuestStatus_PhoenixCaptured.cs (T018)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (every new Def/type exists, skeleton persists correctly).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenario 1 in-game, using the new debug tools.
5. This is a demoable MVP: a Phoenix-tattooed colonist who dies genuinely comes back, with real burns, real
   Tier 2 progression, and real Paralytic Abasia — but with no rarity cap enforced yet, no cremation shortcut,
   and no cauterize passive.

### Incremental Delivery

1. Setup + Foundational → every new type/Def exists and persists; nothing player-visible yet.
2. Add US1 → validate Scenario 1 → MVP (the core revival loop, both tiers, Abasia escalation, all fully working).
3. Add US2 → validate Scenario 2 → the 3-tattoo cap is enforced faction-wide, including join/leave/capture.
4. Add US3 → validate Scenario 3 → deliberate cremation guarantees the next attempt, at the cost of gear.
5. Add US4 → validate Scenario 5 → the independent passive cauterize ability works, fully decoupled from
   everything above it.
6. Polish → verify the removal guard covers Phoenix, then run the full eight-scenario sign-off pass.

### Parallel Team Strategy

With multiple developers, once Foundational is done:

- Developer A: User Story 1 (the core revival loop) — the long pole, since US3 and US4 both extend files it
  creates.
- Developer B: User Story 2 (the faction cap) — genuinely independent of US1's own revival logic.
- Once US1's `TryResolveAttempt`/`CompPostTick` land, a developer can pick up US3 (extends
  `TryResolveAttempt`) and another US4 (extends `CompPostTick`) — both can proceed in parallel with each other
  at that point, since they touch different, unrelated additions to those same methods.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- This is the first tattoo whose core mechanic needs a `GameComponent` at all (T007) — this mod's first
  faction-wide (non-per-pawn) persisted state — and the first to deliberately *not* reuse the shared
  `TattooTierProgress` class despite being visibly tiered, because its progression counter's reset-on-revival
  semantics don't fit that class's monotonic-counter contract (Known Baseline, data-model.md).
- T010/T011 (US1) and T021 (US3) all extend the same `PhoenixRevivalUtility.TryResolveAttempt`/
  `RegisterAttemptAndMaybeApplyAbasia` file — expected, and why US3 is sequenced after US1 rather than run in
  parallel with it, despite remaining independently *testable* per its own `quickstart.md` scenario.
- T013 (US1) and T023 (US4) both extend `HediffComp_PhoenixEffect.CompPostTick` — two independent self-gated
  checks in the same method, sharing no state (FR-025's explicit "no shared cooldown" requirement) — sequenced
  T013-then-T023 to avoid two tasks concurrently editing the same method, not because US4 depends on US1's
  actual revival behavior.
- Unlike every prior tattoo, this feature needs **zero** new shared interface/contract for a future tattoo to
  consume — `IProvidesTattooTierProgress`, `TattooEffectValues`, and `TattooHediffRemovalGuard` are reused
  exactly as they stand (plan.md's Structure Decision); no `contracts/` directory exists for this feature.
- Research.md R12 confirms **zero** Combat Extended-specific code is needed anywhere in this feature — the one
  genuine shared surface (CE's own `BleedRate` stabilization postfix) already composes correctly with T023's
  `Tended()` call with no branch needed.
- FR-009's removal guard (`TattooHediffRemovalGuard.cs`, `Patch_HealthTracker_RemoveHediff_ProtectTattoos.cs`)
  already exists from feature 003 and covers every `TattooMagicDef.appliedHediff` generically — T025 is a
  verification task, not greenfield implementation; only touch the code if that verification actually turns up
  a gap.
- The design proposal's own assumption that "the Resurrector Mech Serum uses the same [head/brain] gate" as
  Phoenix is expected to need was found, on decompile, to be incorrect (research.md R1) — FR-008's head/brain
  precondition (T010, T012) is genuinely new code with no vanilla behavior to delegate to; do not go looking for
  a shortcut through `ResurrectionUtility`/`CompTargetable` for it.
- **Implementation complete, manual verification substantially complete**: T001 through T025 are all
  done. Live manual verification across two sessions (2026-08-28/29) confirmed T015, T019, T022, T024, T026,
  and T027 (Scenarios 1, 2, 3, 4, 5, 6, 7 — see each task's own PASS note for specifics). T028 (Scenario 8) is
  partially verified (Combat Extended present/absent, one cross-tattoo pairing) with two specific gaps
  remaining (Vampiric Thorn's on-landed-hit interaction, Royalty/Ideology absence). T029 (full sign-off) is
  blocked on those same two gaps plus reverting `waitDaysTier1`/`Tier2` from their fast-testing values.
  `dotnet build` has stayed at `0` warnings/`0` errors throughout, including after every fix made during live
  testing (see the bug list below), and every fix has been deployed to and re-verified against the live
  `Mods/TattooMagic/` folder.
- **Corrections found and fixed during this implementation pass** (none required a design change — all were
  caught before `dotnet build` first succeeded or during this session's own code-level review, not discovered
  live in-game): `HediffDefOf.Burn` doesn't exist (T008, resolved via a cached `DefDatabase<HediffDef>
  .GetNamed("Burn")` lookup instead); `GenDate` lives in the `RimWorld` namespace, not `Verse` (two missing
  `using` directives, T009); the days-alive Tier 2 upgrade check needed factoring into its own
  `TryUpgradeToTier2()` method so the "set days-alive counter" debug tool could reuse it correctly without
  double-counting (T013); `Pawn_GuestTracker.pawn` is a private field needing `AccessTools.Field` reflection to
  read (T018); vanilla's own `ToolMapForPawns` dev-tool targeting doesn't reach dead pawns' corpses, so the two
  corpse-targeting debug tools use `ToolMap` + `UI.MouseCell()` instead (T014).
- **Real bugs found and fixed during live manual verification (2026-08-28/29)** — distinct from the list above,
  each one confirmed live in-game, rebuilt, redeployed, and re-verified, not just caught at compile time:
  1. `GameComponent_PhoenixRegistry`'s self-heal (`TryRegisterIfCarrier`) re-added an already permanently-lost
     wearer every single sweep, only for the main loop to immediately purge them again — an endless add/remove
     churn for the rest of the save's life, not a one-time transition. Fixed by mirroring the main loop's own
     `IsPermanentlyLost`/`cremationGuaranteed` guard in the self-heal too.
  2. The same self-heal method had no equivalent guard for a pawn destroyed outright while still alive (Dev
     Mode's "T: Destroy" on a living pawn) — same churn shape, different precondition. Fixed with a matching
     `!pawn.Dead && pawn.Destroyed` check mirroring the main loop's own.
  3. `waitDaysTier1`/`waitDaysTier2` were declared as `int`, silently preventing any sub-day wait value from
     ever working via XML (RimWorld logs a parse error and falls back to the hardcoded default) — a real
     usability bug for any player who wants to tune this, not just a testing inconvenience. Changed both to
     `float` and fixed the consuming math in `GameComponent_PhoenixRegistry` to stop truncating before
     multiplying by `GenDate.TicksPerDay`.
  4. `Dialog_ChooseTattoo.TryQueueRitual` force-starts a wait job on the ritual recipient with no way to cancel
     it if the player manually redirects that colonist afterward — they'd stay stuck "pending" forever, spamming
     `WorkGiver_TattooRitual`'s own debug log on every other colonist. Fixed by adding a "Cancel ritual" gizmo
     (`Patch_Pawn_GetGizmos_TattooGizmos`) that clears `pendingRitualTattoo`; both job drivers already `FailOn`
     that going null, so no other cleanup code was needed.
  5. `HediffComp_PhoenixEffect.CheckCauterize` tried to burn the exact same `BodyPartRecord` a severed-limb
     cauterize target had just been removed from — RimWorld's health system correctly refuses to attach a new
     Hediff to a missing part, logging a red error. Fixed by burning the missing part's parent (the stump)
     instead, falling back to the pawn's own core part if there's no parent at all.
  6. `GameComponent_PhoenixRegistry.GameComponentTick`'s per-wearer loop unconditionally skipped any living
     pawn, so a living wearer who had the Phoenix hediff removed outright (God Mode's health-tab delete button,
     or a direct debug `RemoveHediff` call with God Mode on) was never reconsidered and stayed registered
     forever, permanently occupying a cap slot in violation of FR-003. Fixed by checking whether a living pawn
     still actually carries the hediff before skipping them, unregistering immediately if not.
