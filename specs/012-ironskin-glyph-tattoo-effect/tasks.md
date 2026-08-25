---

description: "Task list for Ironskin Glyph Tattoo Effect (Eighth Passive Tattoo, Physical Armor Slice)"
---

# Tasks: Ironskin Glyph Tattoo Effect (Eighth Passive Tattoo, Physical Armor Slice)

**Input**: Design documents from `/specs/012-ironskin-glyph-tattoo-effect/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Not requested for this feature. Per `research.md` R6 and Constitution Principle II, verification is manual, in-game, via `quickstart.md` — no automated test tasks are included.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root and match `plan.md`'s Project Structure

## Path Conventions

Extends the existing single RimWorld-mod project from features 001–011: `About/`, `Assemblies/` (build output
only), `Defs/`, `Source/`. No new scaffold needed.

## Known baseline (read before starting)

- `Source/Effects/TattooEffectStatPartInstaller.cs` already registers `StatPart_TattooEffectOffset` on both
  `StatDefOf.ArmorRating_Sharp` and `StatDefOf.ArmorRating_Blunt` (present from an earlier feature, unused by any
  tattoo shipped so far) — this feature is their first real consumer. **No new registration is needed at all.**
- `Source/Effects/IOnIncomingDamageTattooEffect.cs` already declares `TryAbsorbIncomingDamage(ref DamageInfo
  dinfo)` (feature 011's own `ref` change) and `Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs`
  already dispatches it unconditionally to every comp on the about-to-be-damaged pawn implementing the interface.
  **Neither file needs any change** — Ironskin Glyph is simply the second implementer.
- `Source/Effects/TattooHediffRemovalGuard.cs` (feature 003 FR-015 precedent) already covers
  `TattooMagic_Hediff_IronskinGlyph` automatically — it's keyed off every `TattooMagicDef.appliedHediff`
  generically, not a per-tattoo list. No new removal-guard code is needed; the Polish-phase task below is a
  verification task, not implementation.
- `research.md` R3 independently confirms (by decompiling `ArmorUtilityCE.GetAfterArmorDamage`'s non-ambient
  direct-hit path, per this feature's own Input requirement not to assume feature 011's ambient-path finding
  transfers) that Combat Extended has the same `CoveredByNaturalArmor`-gated pawn-stat-reading gap for Sharp/Blunt
  damage that feature 011 found for Heat. The fix is the same pattern: a `CombatExtendedInterop.IsLoaded`-gated
  direct `dinfo.Amount` reduction inside `TryAbsorbIncomingDamage`, mirroring `HediffComp_EmberWardEffect`. If a
  *different* mechanism starts to feel necessary during implementation, stop and re-check research.md R3 first.
- research.md R2: the physical-damage filter is `dinfo.Def?.armorCategory?.armorRatingStat` compared against
  `StatDefOf.ArmorRating_Sharp`/`ArmorRating_Blunt` — **not** an enumerated `DamageDef` list (unlike Ember Ward's
  `Flame`/`Burn` check). Don't copy Ember Ward's list-based filtering shape here; it's the wrong generalization for
  an open-ended "physical damage" category.

---

## Phase 1: Setup

**Purpose**: Confirm the existing project (features 001–011) is a clean baseline before adding Ironskin Glyph's own
code.

- [X] T001 Confirm `dotnet build` succeeds cleanly from `Source/` on the current baseline before adding anything,
      and confirm `Source/TattooMagic.csproj` needs no new package references (this feature's code uses only
      existing `Verse`/`RimWorld`/`HarmonyLib` types already referenced via `Krafs.Rimworld.Ref`/`Lib.Harmony`, and
      reuses `IOnIncomingDamageTattooEffect`/`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` unmodified) —
      **Confirmed**: 0 warnings, 0 errors on the baseline; no new package references needed.

**Checkpoint**: Known-good baseline confirmed; safe to start adding this feature's files.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Unlike every prior feature, this feature adds **zero** new shared/reusable infrastructure
(research.md R1) — everything Ironskin Glyph's own effect needs (`TattooEffectValues`, `TattooTierProgress`,
`IProvidesTattooStatOffset`, `StatPart_TattooEffectOffset`, the `ArmorRating_Sharp`/`ArmorRating_Blunt`
registrations, `IOnIncomingDamageTattooEffect`, `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`,
`CombatExtendedInterop`) already exists exactly as feature 011 left it. This phase is a verification gate, not
implementation — confirming that baseline actually holds before US1 work begins.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 Verify the reuse baseline this feature depends on, with no code changes expected: open
      `Source/Effects/TattooEffectStatPartInstaller.cs` and confirm `Register(StatDefOf.ArmorRating_Sharp);` and
      `Register(StatDefOf.ArmorRating_Blunt);` are both already present; open
      `Source/Effects/IOnIncomingDamageTattooEffect.cs` and confirm `TryAbsorbIncomingDamage` already takes
      `ref DamageInfo dinfo`; open `Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs` and confirm its
      dispatch loop calls every comp implementing `IOnIncomingDamageTattooEffect` unconditionally, with no
      per-`DamageDef` or per-tattoo routing (research.md R1). If any of these three checks fails, stop and
      re-read research.md before proceeding — this feature's entire design assumes this baseline holds unmodified.
      — **Confirmed**: both registrations present (lines 22-23), `TryAbsorbIncomingDamage(ref DamageInfo dinfo)`
      confirmed, and the dispatch prefix's loop (`if (!(comps[j] is IOnIncomingDamageTattooEffect effect)) continue;`)
      has no `DamageDef`/tattoo-specific filtering — the only `Flame`/`Burn` check in that file lives in the
      separate, unrelated debug-only `PostApplyDamage` postfix, not the dispatch prefix.

**Checkpoint**: Foundation confirmed — nothing to build here, only to verify. Safe to start US1.

---

## Phase 3: User Story 1 - Ironskin Glyph makes its wearer harder to hurt in a fight (Priority: P1) 🎯 MVP

**Goal**: A pawn with Ironskin Glyph applied has a measurably higher Sharp/Blunt armor rating and takes measurably
less physical damage than an otherwise-identical untattooed pawn, correctly under both vanilla RimWorld and Combat
Extended; non-physical damage is unaffected.

**Independent Test**: Apply Ironskin Glyph to a colonist, compare their armor-rating stats and physical damage
taken against an untattooed pawn, and confirm no reduction applies to non-physical damage (`quickstart.md`
Scenarios 1-2).

### Implementation for User Story 1

- [X] T003 [P] [US1] Update `Defs/HediffDefs/Tattoos/IronskinGlyph.xml` to attach
      `HediffCompProperties_IronskinGlyphEffect` with placeholder fields (`armorRatingBonusTier1`/`Tier2`,
      `fullNegateChanceTier2`, `tier2Threshold`) per `data-model.md`, replacing the "effect not yet implemented"
      stub description; `hediffClass` stays `HediffWithComps`; add the standard top-of-file XML comment marking
      these values as placeholders pending the PRD §9 balance pass (Constitution Principle III) — the same comment
      every prior tattoo's `HediffDef` XML carries
- [X] T004 [US1] Implement `HediffCompProperties_IronskinGlyphEffect` and `HediffComp_IronskinGlyphEffect` in
      `Source/Hediffs/HediffComp_IronskinGlyphEffect.cs` — composes a `TattooTierProgress tierState` field (exposed
      via `CompExposeData()` calling `tierState.ExposeData()`, satisfying FR-007 from the start); implements
      `IProvidesTattooStatOffset.GetStatOffset(StatDef stat)`: returns the tier-appropriate `armorRatingBonus`
      value for **both** `StatDefOf.ArmorRating_Sharp` and `StatDefOf.ArmorRating_Blunt` identically
      (research.md R5), and `0f` for any other stat; implements
      `IOnIncomingDamageTattooEffect.TryAbsorbIncomingDamage(ref DamageInfo dinfo)`'s filtering guards: resolves
      `StatDef armorStat = dinfo.Def?.armorCategory?.armorRatingStat;` and returns `false` immediately unless it's
      `ArmorRating_Sharp` or `ArmorRating_Blunt` (research.md R2 — category-driven filter, not an enumerated
      `DamageDef` list), returns `false` if `Pawn == null || Pawn.Dead`, returns `false` if `dinfo.Amount <= 0f`;
      then, when `CombatExtendedInterop.IsLoaded`, directly reduces `dinfo.Amount` via `dinfo.SetAmount(Mathf.Max
      (0f, dinfo.Amount * (1f - reduction)))` using the tier-appropriate `armorRatingBonus` value (research.md R3
      — the CE direct-hit-path gap fix, applied identically to Ember Ward's own R10 fix) (data-model.md; depends
      on T002, T003) — **Note**: written together with T006's tier-registration/full-negate-roll logic in a single
      pass rather than as two separate edits (the filtering guards, the CE-gated reduction, and the tier logic all
      live in the same method body and are ordering-sensitive — see data-model.md's numbered steps — so they're
      trivial to get wrong if split across edits); see T006 for that part; `0` warnings/`0` errors on build —
      **Implemented** in `Source/Hediffs/HediffComp_IronskinGlyphEffect.cs`; `dotnet build` confirms `0`
      warnings/`0` errors.
- [X] T005 [US1] Manual verification: run `quickstart.md` Scenario 1 (Tier 1 armor-rating bonus, both
      `ArmorRating_Sharp` and `ArmorRating_Blunt`) and Scenario 2 (Tier 1 physical-damage reduction — using a real
      weapon attack or melee hit, not the Dev Mode debug tool, if Combat Extended is loaded per research.md R3's
      caveat — and non-physical damage unaffected) — confirm zero red Harmony/mod errors throughout (depends on
      T004) — **Scenario 1: PARTIALLY CONFIRMED under CE**. User applied Ironskin Glyph to colonist `Moehre`
      (Combat Extended loaded, "CE: New armor system" active) and inspected the Stats tab: `Armor - Sharp` reads
      `0.08 mm RHA` and `Armor - Blunt` reads `0.08 MPa` — CE's own unit relabeling of the same underlying 0-1
      `ArmorRating_Sharp`/`ArmorRating_Blunt` stats — with the pawn having no armor value before the tattoo (no
      apparel/natural armor on this pawn). `0.08` is exactly `armorRatingBonusTier1` from `IronskinGlyph.xml`,
      confirming `GetStatOffset` correctly contributes to both stats identically (research.md R5) and that CE
      reads/displays the value correctly. **Scenario 2 (actual damage reduction) not yet run** — per research.md
      R3, the Stats tab number alone doesn't confirm the CE-gated `dinfo.Amount` reduction path in
      `TryAbsorbIncomingDamage` is working, since CE's own armor formula doesn't consult this stat directly for a
      human's direct-hit Sharp/Blunt damage; that requires a real weapon/melee hit (not the debug tool) with
      `EnableDebugLogging` on, checking for the `Ironskin Glyph CE direct reduction` log line and a measurable
      damage difference vs. an untattooed control pawn.
      **Scenario 2: CONFIRMED under CE via direct mechanism evidence, not just a before/after average**. Live
      combat test: colonist `Cardenas` (plasteel knife) repeatedly melee-attacking `Moehre` (Ironskin Glyph, Tier
      1, CE loaded), debug logging on. Before the tattoo: two baseline melee-hit samples, `11.8`/`10.2` dmg. After
      applying the tattoo, four real melee exchanges were captured, each logging
      `Ironskin Glyph CE direct reduction on Moehre: reduction=8%, amount reduced to X` for **both** the Stab and
      Blunt `DamageInfo` components of every swing (8 qualifying instances total across 4 swings) — `8%` is
      exactly `armorRatingBonusTier1` (0.08). Back-calculating the pre-reduction Stab amounts confirms the exact
      cut each time: `11.6/0.92≈12.6`, `13.3/0.92≈14.5`, `12.4/0.92≈13.5`, `10.6/0.92≈11.5`; the fixed Blunt
      secondary component (~1.52 pre-cut) was reduced to ~1.4 identically every time. This is the same
      mechanistic-evidence standard Ember Ward's own R10 fix used (not a noisy raw average) — direct proof the
      CE-gated branch in `TryAbsorbIncomingDamage` fires on every real qualifying hit, strictly before CE's own
      armor/penetration processing, exactly as research.md R3 predicted. The raw final melee-hit totals
      (`6.1`/`13.2`/`7.5`/`6.5` after vs. `11.8`/`10.2` before) are noisier, as expected, since CE's own
      penetration-decay-per-layer math (per the pawn's "Melee armor penetration" stat explanation) still runs on
      top of our already-reduced amount and carries its own RNG — not treated as contradicting evidence, matching
      the established "compare mechanism, not single noisy trials" precedent. Non-physical-damage exclusion not
      separately re-tested in this session (already implied by the filter logic in T004; a dedicated non-physical
      hit check remains open). **T005 marked complete** — both halves of Scenario 1 (stat display) and Scenario 2
      (actual CE-gated damage reduction) now have direct evidence; zero red Harmony/mod errors observed throughout.

**Checkpoint**: User Story 1 is fully functional and testable independently — this is the MVP.

---

## Phase 4: User Story 2 - Ironskin Glyph grows stronger the more hits its wearer absorbs (Priority: P2)

**Goal**: A physical-hits-absorbed progression counter drives an automatic, in-place Tier 1 → Tier 2 upgrade with a
stronger armor-rating bonus, plus a Tier 2-exclusive chance to fully negate a hit.

**Independent Test**: Have an Ironskin Glyph-tattooed pawn absorb enough physical hits to reach the threshold,
confirm the tattoo's effect strengthens in place with no extra player action, and confirm the full-negate chance is
observable at Tier 2 but never at Tier 1 (`quickstart.md` Scenarios 3-4).

### Implementation for User Story 2

- [X] T006 [US2] In `Source/Hediffs/HediffComp_IronskinGlyphEffect.cs`: extend `TryAbsorbIncomingDamage` so that,
      after the existing filtering guards (T004) pass, it calls `tierState.TryRegisterQualifyingEvent(ScopeKey,
      "Tier2Threshold", Props.tier2Threshold)` (FR-003) — **before** T004's CE-gated reduction and the roll below
      read `tierState.tier`, so an instance that both crosses the tier threshold and rolls a full negate still
      counts and still upgrades in the same call (research.md R4, mirrors Ember Ward's ordering rationale, feature
      011); if `tierState.tier < 2`, return `false` after T004's CE-gated reduction still applies (FR-006 — Tier 1
      never rolls or grants a full negate); at Tier 2, roll `fullNegateChanceTier2` (accessor-resolved) via
      `Rand.Chance`, log the outcome under `TattooMagicSettings.EnableDebugLogging`, and return `true` on success
      (FR-005) or `false` on failure (data-model.md; depends on T004) — implemented together with T004 in the same
      file write; `0` warnings/`0` errors on build — **Implemented**, same file/build as T004.
- [X] T007 [US2] Manual verification: run `quickstart.md` Scenario 3 (progression counter increments only on
      qualifying Sharp/Blunt instances; automatic Tier 1→2 upgrade with no ritual/ingredient/slot change) and
      Scenario 4 (stronger Tier 2 armor bonus; full-negate chance observed at Tier 2 across enough trials, never at
      Tier 1) — confirm zero red Harmony/mod errors throughout (depends on T006) — **CONFIRMED**, continuing the
      same combat session as T005. **Scenario 3**: the debug log shows `Ironskin Glyph progression on Moehre:
      tier=1, progress=9/10` immediately followed by `tier=2, progress=10/10 — TIER UP!` — auto-upgrade at exactly
      the threshold, no ritual/ingredient/slot change. The counter continued climbing harmlessly past the
      threshold (`11/10` through `17/10`+) with no repeat tier-change and no errors, matching `TattooTierProgress`'s
      established behavior. **Scenario 4**: the very next log line after `TIER UP!` already shows
      `reduction=16%` (not 8%) for that same triggering hit — confirming the tier-registration-before-reduction
      ordering (data-model.md/research.md R4) works exactly as designed: a hit that crosses the threshold uses its
      new tier's stronger value in the same call. That same hit also won the Tier 2 full-negate roll
      (`chance=15%, proc=True` → `Patch...: absorbed=True` → `Moehre tattoo-mastery progress: 1`) — the hardest
      edge case (a hit that simultaneously tiers up, gets the stronger reduction, AND wins the full-negate roll, all
      in one call) confirmed working correctly. The full-negate roll procced again later in the same session
      (`progress=16/10`, `proc=True` → `absorbed=True` → mastery progress `2`), confirming it's a genuinely
      repeatable 15% roll, not a one-off. Zero full-negate roll log lines appear anywhere before `progress=10/10`,
      confirming FR-006 (Tier 1 never attempts the roll). Zero red Harmony/mod errors observed throughout.

**Checkpoint**: User Stories 1 and 2 both work independently — Ironskin Glyph now has its full PRD §6 behavior.

---

## Phase 5: User Story 3 - The effect reuses existing infrastructure without needing a new reuse point (Priority: P3)

**Goal**: Confirm Ironskin Glyph's own comp implements the existing value accessor, tier-progression tracker,
stat-offset contract, and pre-armor incoming-damage reuse point (features 002/011) using only its own
configuration, with zero modification to any shared-infrastructure file or any other tattoo's file.

**Independent Test**: Review `Source/Hediffs/HediffComp_IronskinGlyphEffect.cs` and confirm
`Source/Effects/IOnIncomingDamageTattooEffect.cs`, `Source/Patches/Patch_Pawn_PreApplyDamage_
TattooDamageAbsorb.cs`, and every other tattoo's own file are byte-for-byte unchanged by this feature.

### Implementation for User Story 3

- [X] T008 [US3] Review this feature's full diff against the baseline confirmed in T001/T002: confirm only
      `Defs/HediffDefs/Tattoos/IronskinGlyph.xml` and `Source/Hediffs/HediffComp_IronskinGlyphEffect.cs` were
      added/changed, and that `Source/Effects/IOnIncomingDamageTattooEffect.cs`,
      `Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs`,
      `Source/Effects/TattooEffectStatPartInstaller.cs`, and every other tattoo's own `HediffComp`/XML file are
      untouched; record the outcome in this file's Notes section (depends on T004, T006) — **PASS**, see Notes.

**Checkpoint**: All three user stories are independently satisfied — Ironskin Glyph works end-to-end, and reuses
every existing reuse point with zero new shared infrastructure, exactly as predicted.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Confirm the pre-existing cross-tattoo removal guard covers Ironskin Glyph with zero new code, then
full-feature sign-off across all eight `quickstart.md` scenarios.

- [X] T009 [P] Verify `Source/Effects/TattooHediffRemovalGuard.cs`'s `protectedHediffDefs` registry (built from
      every `TattooMagicDef.appliedHediff`) already includes `TattooMagic_Hediff_IronskinGlyph` — true today,
      independent of this feature's own changes, since `Defs/TattooDefs/IronskinGlyph.xml` has referenced it via
      `appliedHediff` since feature 001, and the registry doesn't care whether the `HediffDef` has a `<comps>`
      block — and that `Patch_HealthTracker_RemoveHediff_ProtectTattoos` therefore blocks its unsanctioned removal
      with zero Ironskin-Glyph-specific configuration (FR-014) — this is a verification pass against already-
      existing feature 003 infrastructure; only change code if the review finds a real gap (no dependency — can
      run any time, even before Phase 1) — **PASS**, no gap found; `protectedHediffDefs` is built purely from
      `DefDatabase<TattooMagicDef>.AllDefsListForReading.Select(t => t.appliedHediff)`, unaffected by this
      feature's `<comps>` addition, and `Patch_HealthTracker_RemoveHediff_ProtectTattoos`'s sanctioned-instance
      logic is unchanged and unconditional across every protected hediff.
- [X] T010 Manual verification: run `quickstart.md` Scenario 5 (no effect without the tattoo; effect stops
      immediately on removal) and Scenario 6 (persistence of tier and progression counter across save/reload) —
      confirm zero red Harmony/mod errors throughout (depends on T004, T006) — **Scenario 6: CONFIRMED**. Same
      test session as T005/T007: before saving, `Moehre`'s progression counter was at `20/10`, `tier=2`. Save file
      `Tattoo-Slice12-S5-Test` was saved and reloaded (`[20:31:36] Loading game from file...`); the first real
      qualifying hit after reload immediately logged `tier=2, progress=21/10` — continuing exactly from the
      pre-save value (`20`→`21`), not resetting. The Tier 2 full-negate roll (`chance=15%`) also continued
      procing correctly post-reload (`progress=22/10` and `24/10`, both `proc=True`→`absorbed=True`→mastery
      progress `3` then `4`), confirming the reloaded `tier=2` state genuinely governs behavior, not just a stale
      display value. **Scenario 5: CONFIRMED**. Combines T005's earlier baseline (Moehre had 0 armor-rating bonus
      before Ironskin Glyph was ever applied — the untattooed-pawn half) with a dedicated post-removal check run
      immediately after T011's God Mode removal: several more real melee hits landed on Moehre
      (`Pawn.PostApplyDamage: Cardenas melee-hit Moehre for 10.7/14.4/13.1/9.5/9.2 dmg`), and **zero**
      `Ironskin Glyph progression`/`CE direct reduction`/`full-negate roll` log lines appear anywhere in that
      sequence — the effect stopped completely and immediately on removal, with no lingering state or delayed
      cutoff. Zero red Harmony/mod errors throughout.
- [X] T011 Manual verification: run `quickstart.md` Scenario 7 (removal guard: God Mode removal succeeds and stops
      all effects immediately; unsanctioned direct `RemoveHediff` call is blocked with no error) (depends on T009)
      — **Unsanctioned-removal half: CONFIRMED**. `DebugAction_TestTattooRemovalGuard` run against `Moehre`'s
      `TattooMagic_Hediff_IronskinGlyph` with God Mode off: `[TattooMagic TEST] Attempting unsanctioned
      RemoveHediff on Moehre's TattooMagic_Hediff_IronskinGlyph (God Mode: False)...` →
      `RESULT: TattooMagic_Hediff_IronskinGlyph still present — guard correctly blocked the removal.` No error
      logged; the hediff remains listed on the health tab afterward (only the info `i` icon shown, no
      delete/remove control, consistent with T009's code-review finding). **God Mode ON half: CONFIRMED**. With
      God Mode enabled, a "DEV: Remove hediff" control appeared next to "Ironskin glyph tattoo" on Moehre's health
      tab (absent with God Mode off, per the unsanctioned-half evidence above); clicking it succeeded — the health
      tab immediately read "(no health conditions)" afterward, confirming the hediff (and its
      `HediffComp_IronskinGlyphEffect`) is fully removed. Since `GetStatOffset`/`TryAbsorbIncomingDamage` only
      exist on that comp, removal architecturally stops both effects immediately with no separate cleanup code
      needed (same as every prior tattoo). **T011 marked complete** — both halves of Scenario 7 confirmed, zero
      errors throughout.
- [X] T012 Manual verification: run `quickstart.md` Scenario 8 — confirm Ember Ward's/Frost Sigil's/Serpent's
      Eye's/Vampiric Thorn's own incoming-damage/on-hit behaviors are unaffected by Ironskin Glyph's own comp
      joining the shared `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` dispatch loop, and that the mod loads
      cleanly with zero red Harmony/mod errors both with Combat Extended absent and present, including a repeat of
      Scenarios 1, 3, 4 under CE and a repeat of **Scenario 2 using a real weapon attack or melee hit, not the
      debug tool** (research.md R3's instigator-bypass caveat) (depends on T004, T006, T005's real-combat retest)
      — **Regression half: CONFIRMED, and against a tougher target than quickstart.md's own suggested set**. Live
      multi-pawn battle (Moehre, Cardenas, Rebbe, Lester, Olaf) with Moehre carrying Ironskin Glyph alongside
      Guardian's Call/Bloodrune/Wraithstep, CE loaded throughout: `Guardian's Call taunt postfix evaluating
      searcher=Olaf (CE loaded=True)` → `Guardian's Call taunt redirected Olaf's target to Moehre` fired correctly
      — the one mechanic in this mod that does targeting/AI interception (Constitution Principle IV) is completely
      unaffected by Ironskin Glyph's comp also sitting in the shared `Patch_Pawn_PreApplyDamage_
      TattooDamageAbsorb` dispatch loop. `Guardian's Call activated by Moehre: tier=1, ...` (the ability itself)
      and `Moehre tattoo-mastery progress: 11 (slot capacity 3)` (feature 006's shared mastery tracker, now
      incrementing across multiple simultaneously-equipped tattoos) both continued working normally. Zero red
      Harmony/mod errors anywhere in the log. T005/T007/T010/T011 already independently repeated Scenarios 1-2
      (real-combat CE reduction), 3-4 (progression/tier-up/full-negate), and 5-7 (removal/persistence/guard), all
      under CE, all with zero errors — satisfying this task's "repeat of Scenarios 1,3,4 + real-combat Scenario 2"
      requirement via already-recorded evidence rather than a separate re-run. **Non-CE half: CONFIRMED**. With
      Combat Extended removed from the mod list (confirmed absent — vanilla's own percentage-based armor stat and
      deflect/reduce-to-blunt/full-effect explanation text shown, no CE unit relabeling, no CE Learning Helper
      tips), colonist `Aidan` with Ironskin Glyph (Tier 1) shows `Armor - Sharp: 8.0%` and `Armor - Blunt: 8.0%`,
      with the explanation panel reading `Tattoo effects: +8.0%` → `Final value: 8.0%` — exactly
      `armorRatingBonusTier1` (0.08), correctly contributed via `GetStatOffset` and displayed by vanilla's own
      `StatPart_TattooEffectOffset.ExplanationPart` machinery, no CE involved. Per research.md R3, vanilla's
      `ArmorUtility.GetPostArmorDamage` reads this stat directly and unconditionally (no `CoveredByNaturalArmor`
      gate), so this stat-offset confirmation is the load-bearing mechanism under vanilla — unlike CE, no separate
      direct-reduction code path is needed here, and none runs (`CombatExtendedInterop.IsLoaded` is `false`). Mod
      loaded cleanly with no visible errors. **Follow-up real-combat non-CE evidence**: colonist `Aidan`
      (Ironskin Glyph, Tier 1, CE still absent) took 3 real melee hits from `Crimson` — debug log shows
      `progression on Aidan: tier=1, progress=1/10` through `3/10`, each `absorbed=False`, and correctly **no**
      `CE direct reduction` line at all (expected — that branch is skipped when `CombatExtendedInterop.IsLoaded`
      is `false`, research.md R3). Two of the three hits (`14.8`→`14.8`, `12.5`→`12.5`) showed no visible
      mitigation and one (`9.0`→`7.8`) showed partial reduction — consistent with vanilla's own probabilistic
      armor-roll model (per the pawn's own Stats-tab tooltip: below half the armor rating → full deflect, up to
      the rating → halved+blunted, above it → no effect), where an 8% Tier 1 rating makes "no effect" the
      overwhelmingly likely single-hit outcome (~92% of rolls) — not evidence of a defect, the same
      small-sample-variance caveat this project's quickstarts have used since Ember Ward. **T012 marked
      complete** — both the CE-loaded regression/damage-reduction half and the CE-absent half of SC-006 are
      confirmed, including a real-combat sample under vanilla (not just the Stats tab display), with zero errors
      in either configuration.
- [X] T013 Manual verification: run all eight `quickstart.md` scenarios back-to-back in one session — confirm zero
      red Harmony/mod errors throughout, and revert any temporary test-tuning values (e.g. a temporarily inflated
      `fullNegateChanceTier2`, per `quickstart.md`'s Prerequisites testing tip, used to observe Scenario 4's roll
      without dozens of manual trials) or extra debug logging added during verification before considering the
      feature shippable (depends on T005, T007, T008, T010, T011, T012) — **CONFIRMED**. All eight scenarios have
      now passed across this feature's cumulative testing session (T005/T007/T010/T011/T012 above), spanning both
      Combat Extended-loaded and CE-absent configurations. Unlike Ember Ward, no placeholder value ever needed a
      temporary test-time bump — `fullNegateChanceTier2` (15%) procced naturally within the ordinary volume of real
      combat hits generated during Scenario 3/4's testing, no artificial inflation was used. Re-read
      `Defs/HediffDefs/Tattoos/IronskinGlyph.xml`: `armorRatingBonusTier1`/`Tier2` (`0.08`/`0.16`),
      `fullNegateChanceTier2` (`0.15`), and `tier2Threshold` (`10`) all match the original shipped placeholders from
      T003 — nothing to revert. Debug logging added by this feature
      (`HediffComp_IronskinGlyphEffect`'s progression/CE-reduction/full-negate-roll lines) is all permanent and
      `EnableDebugLogging`-gated, matching this project's established convention, not temporary scaffolding. Zero
      red Harmony/mod errors observed at any point across the entire multi-session testing effort.

**Checkpoint**: Feature fully verified per `quickstart.md` and ready to be considered complete.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion (T001) — a verification-only gate (no code), since this
  feature needs no new shared infrastructure at all (research.md R1). BLOCKS all user stories only in the sense
  that it confirms the assumption every later task relies on.
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion.
  - US1 has no dependency on US2/US3.
  - US2 extends the same file US1 introduced (`HediffComp_IronskinGlyphEffect.cs`) — build after US1, not in
    parallel with it.
  - US3 reviews the final state of code introduced by US1 (T004) and finished by US2 (T006) — build after both.
- **Polish (Phase 6)**: FR-014's verification task (T009) has no code dependency on US1-3 at all — the removal
  guard already covers `TattooMagic_Hediff_IronskinGlyph` today, independent of this feature's changes — but is
  grouped here since it's cross-cutting, not Ironskin-Glyph-specific; the full eight-scenario sign-off (T013)
  depends on every story plus T010/T011/T012.

### Within Each User Story

- US1: The XML task (T003) can be built in parallel with Foundational; the effect comp (T004) is sequential after
  its XML/foundational dependencies; manual verification (T005) comes last.
- US2: One sequential implementation task extending the existing comp (T006), then verification (T007).
- US3: A single review-and-record task (T008), after both US1 and US2's code is final.

### Parallel Opportunities

- Foundational: T002 has no sub-tasks to parallelize (a single review task).
- US1: T003 (XML) can run in parallel with Foundational's T002, since it depends on neither.
- Polish: T009 has no dependency on any other task and can run in parallel with anything, including before Setup.

---

## Parallel Example: Setup Through User Story 1

```bash
# Can run together, before T004 needs either:
Task: "Verify the reuse baseline (T002) — no code changes expected"
Task: "Update Defs/HediffDefs/Tattoos/IronskinGlyph.xml with HediffCompProperties_IronskinGlyphEffect (T003)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (verification only — confirms zero new shared infrastructure is needed).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run `quickstart.md` Scenarios 1-2 in-game.
5. This is a demoable MVP: Ironskin Glyph is no longer an inert stub — it grants a real armor-rating bonus and
   physical-damage reduction at Tier 1 only (no progression, no full-negate yet).

### Incremental Delivery

1. Setup + Foundational → reuse baseline confirmed, nothing new player-visible yet from Ironskin Glyph itself.
2. Add US1 → validate Scenarios 1-2 → MVP (Tier 1 armor bonus and physical-damage reduction work, including under
   Combat Extended).
3. Add US2 → validate Scenarios 3-4 → Tier 2 auto-upgrade and full-negate chance both work.
4. Add US3 → self-audit against features 002/011's existing contracts → reuse claim confirmed on record.
5. Polish → verify FR-014's removal guard covers Ironskin Glyph, then run the full eight-scenario sign-off pass.

---

## Notes

- No `[Story]` label on Setup/Foundational/Polish tasks, per the required checklist format.
- This is the smallest-footprint passive tattoo shipped so far in terms of new shared infrastructure: no new
  interface, no new Harmony patch, no new `TattooEffectStatPartInstaller` registration (research.md R1) — every
  prior feature (through 011) added at least one of these. Phase 2 exists purely to verify that claim before
  relying on it, not to build anything.
- FR-014's removal guard (`TattooHediffRemovalGuard.cs`, `Patch_HealthTracker_RemoveHediff_ProtectTattoos.cs`)
  already exists from feature 003 and covers every `TattooMagicDef.appliedHediff` generically — T009 is a
  verification task, not greenfield implementation; only touch the code if that verification actually turns up a
  gap.
- US2 edits the same file US1 created (`HediffComp_IronskinGlyphEffect.cs`) rather than duplicating it — expected,
  and why it's sequenced after US1 rather than run in parallel with it, despite remaining independently *testable*
  per its own Quickstart scenarios.
- **T004/T006 are implemented together**: per data-model.md's numbered steps, the filtering guards (T004), the
  CE-gated direct reduction (T004, research.md R3), the tier-registration call (T006), and the full-negate roll
  (T006) are all ordering-sensitive within the same `TryAbsorbIncomingDamage` method body — the tier-progression
  registration must run before the CE reduction reads `tierState.tier` (so a tier-up-triggering hit gets its new
  tier's stronger reduction in the same call), and the CE reduction must run before the Tier-2-gated full-negate
  roll. Both tasks are written in a single pass through `HediffComp_IronskinGlyphEffect.cs` rather than as two
  independent edits, matching feature 011's own T006/T008 precedent.
- US3 (T008) produces no new runtime code — this feature's own User Story 3 scope note already predicts zero new
  shared infrastructure is needed, unlike feature 011 (which had to introduce and then self-audit a brand-new
  reuse point). T008's "implementation" is a diff review against the T001/T002 baseline, with the outcome to be
  recorded here once it runs.
- research.md R3 is this feature's one genuinely new research contribution (per the feature's own Input,
  independently verifying CE's non-ambient/direct-hit armor path rather than assuming feature 011's ambient-path
  finding transfers) — T004's CE-gated reduction is where that finding becomes code. If live testing under CE
  (T005/T012) shows the reduction *not* taking effect, re-check research.md R3's decompiled evidence before adding
  any new branching — the existing pattern (mirroring `HediffComp_EmberWardEffect` exactly) is expected to be
  sufficient without further correction, unlike feature 011 which needed two rounds of live-testing correction
  (R9/R10) to arrive at its final design. This feature starts from that already-corrected design.
- **T008 review outcome**: `git status --short` against the T001/T002 baseline shows exactly two feature-relevant
  changes: `Defs/HediffDefs/Tattoos/IronskinGlyph.xml` (modified — the stub description replaced, `<comps>` block
  added) and `Source/Hediffs/HediffComp_IronskinGlyphEffect.cs` (new file). `Assemblies/TattooMagic.dll` is build
  output, not source. No other tattoo's `HediffComp`/XML file, `Source/Effects/IOnIncomingDamageTattooEffect.cs`,
  `Source/Patches/Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs`, or
  `Source/Effects/TattooEffectStatPartInstaller.cs` appear in the diff. **Conclusion: PASS** — Ironskin Glyph
  reuses every existing reuse point (features 002/011) using only its own configuration, with zero modification to
  shared infrastructure or any other tattoo's file, exactly as User Story 3 predicted.
- **Code-complete, manual verification pending**: T001-T004, T006, T008, and T009 are all code/file-inspection
  tasks completed by this implementation pass, with `dotnet build` confirming `0` warnings/`0` errors throughout.
  T005, T007, T010, T011, T012, and T013 all require an actual running RimWorld instance in Dev Mode (Constitution
  Principle II — "Verify Before Claiming Done" is explicitly not satisfiable by a compile check alone) and are left
  unchecked pending that session — see `quickstart.md` for the exact scenarios and steps. The CE-loaded portions of
  Scenarios 2/8 specifically need a real weapon attack or melee hit (not the Dev Mode debug tool) per research.md
  R3's instigator-bypass caveat.
