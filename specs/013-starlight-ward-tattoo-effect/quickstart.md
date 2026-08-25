# Quickstart: Validating the Starlight Ward Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature (no
automated `Verse`/`RimWorld` test harness across features 001–012 either).

**Testing-methodology note**: unlike every prior passive tattoo, this feature's qualifying event depends on the
pawn's **mood**, not a single damage instance, so it can't be triggered with one debug-tool click the way a damage
instance can. Two testing paths exist, and which one to use depends on what a given scenario is actually checking:

- **Deterministic path (preferred for Scenarios 3 and 4's counting logic)**: `Source/Debug/
  DebugAction_TestStarlightWardRiskWindow.cs` adds two permanent Dev Mode tools under the "TattooMagic" category —
  **"TEST: Starlight Ward risk window (clean recovery, should count)"** and **"TEST: Starlight Ward risk window
  (forced break, should NOT count)"**. Click either on a tattooed pawn (`ToolMapForPawns`) and it drives one full
  risk-window cycle end-to-end in well under a second of wall-clock time — forcing Mood directly via `Need.
  CurLevel`'s public setter, fast-forwarding game ticks via `Find.TickManager.DoSingleTick()` (no real-time
  waiting, no mood drift to fight), optionally forcing a real `MentalBreakDef` via the same mechanism vanilla's own
  "Mental break..." tool uses, and logging a clear `PASS`/`FAIL` comparing the progression counter before/after. It
  restores the pawn's original mood and ends any mental state it forced when done. Use this to build progression
  toward the Tier 2 threshold (repeat the "clean recovery" trial) and to verify the break-exclusion logic (the
  "forced break" trial) — this is what caught a real ordering bug in the counting logic during this feature's own
  implementation testing (2026-08-22), which manual dragging alone had left ambiguous.
- **Automated exposure path (preferred for Scenario 2)**: `Source/Debug/
  DebugAction_TestStarlightWardBreakExposure.cs` adds **"TEST: Break exposure at pinned mood (33%, ~8h)"** under
  the same "TattooMagic" Dev Mode category. It pins the targeted pawn's Mood to an exact value (`Need.CurLevel`'s
  public setter) and **re-pins it on every single simulated tick** while fast-forwarding via `Find.TickManager.
  DoSingleTick()` — so unlike manually dragging the Needs-tab bar (which only offers coarse ±10% steps and drifts
  the instant you stop watching it), the pinned value genuinely cannot drift, and no real-time waiting is needed at
  all. It reports how many times the pawn's own break-risk window opened and how many actual mental breaks fired
  over the trial. Run it once on the tattooed pawn and once on an untattooed control at the same pinned mood value
  and compare: **33% specifically sits between a typical untattooed pawn's Minor threshold (35%) and a Tier
  1/Tier 2 Starlight Ward pawn's own lowered threshold (32%/29%)** — at that value the untattooed pawn should show
  a nonzero window-open count while the tattooed pawn shows zero, which is a much sharper, faster, and more
  decisive result than sampling for an actual rare break to fire (see the tool's own log output for why
  `breaksObserved` can legitimately read `0` even on a fully-working trial).
- Also confirm **Options → Dev Mode → "Random mental states"** is enabled wherever any scenario needs an organic
  break to actually have a chance of firing — `Verse.DebugSettings.enableRandomMentalStates`, confirmed in
  `MentalBreakerTickInterval`'s own early-out.

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode), God Mode available for Scenario 6.
- Feature 001 already built and working (the ritual station) — use it to apply Starlight Ward, or use Dev Mode's
  "Add hediff" tool to grant `TattooMagic_Hediff_StarlightWard` directly to a test pawn for faster iteration.
- A way to inspect the pawn's "Mental break threshold" and "Psychic sensitivity" stats (Health/Stats tab),
  Starlight Ward's progression counter/tier (Dev Mode inspect panel), and the pawn's current Mood (Needs tab,
  draggable in Dev Mode).
- To reliably observe a Tier 1 → Tier 2 upgrade (Scenario 3) without dozens of manual mood-cycling passes: click
  **"TEST: Starlight Ward risk window (clean recovery, should count)"** repeatedly (once per point of
  `tier2Threshold`, `5` by default) — each click is one full, deterministic dip-and-recover cycle. Alternatively,
  since `tier2Threshold` is a placeholder balance value anyway (PRD §9), temporarily edit it in
  `Defs/HediffDefs/Tattoos/StarlightWard.xml` to something small (e.g. `2`) for that one test pass, rebuild,
  confirm the behavior, then set it back to its shipped placeholder afterward.
- To reliably observe the Tier 2 full mood buff (Scenario 4) without waiting on real progression: use the "Add
  hediff" tool, then Dev Mode's field-editing tools (or a temporary debug action) to set the hediff's own comp
  `tierState.tier` directly to `2`, exactly as any other tattoo's own quickstart would do to fast-forward to Tier
  2 for isolated testing.

## Scenario 1 — Tier 1 mental-break and psychic-sensitivity resistance (User Story 1 / SC-001)

1. Apply Starlight Ward to a colonist.
2. Open the colonist's Stats tab and inspect "Mental break threshold" and "Psychic sensitivity".
3. **Expected**: "Mental break threshold" is measurably **lower** than an otherwise-identical untattooed pawn's
   (lower is the resistant direction, research.md R1), and "Psychic sensitivity" is measurably lower too
   (research.md R2) — both with "Tattoo effects: -X" visible in each stat's explanation breakdown (reusing
   `StatPart_TattooEffectOffset.ExplanationPart`, feature 002).

## Scenario 2 — Tier 1 fewer actual breaks at equal mood (User Story 1 / SC-002)

**Preferred**: with **Random mental states** enabled, click **"TEST: Break exposure at pinned mood (33%, ~8h)"**
on the Starlight Ward-tattooed pawn, then on an untattooed control pawn. Compare the two log lines' `risk-window
opened N time(s)` counts.

**Manual alternative**: drag both pawns' Mood down to the same value between the two pawns' thresholds and leave
both there for a few in-game hours, re-dragging as needed if it drifts.

1. **Expected**: at the 33% pinned value (between a typical untattooed pawn's 35% Minor threshold and a
   Starlight-Ward pawn's own lowered threshold), the untattooed control shows a **nonzero** `risk-window opened`
   count while the tattooed pawn shows **zero** — the tattooed pawn's mood never actually crosses below their own
   (lower) threshold at that pinned value, so their break risk isn't just *lower*, it's fully absent at that
   specific mood level. This is a direct, mechanical consequence of Scenario 1's lower effective threshold, not a
   separately-coded "block the break" mechanism.
2. **If you want to additionally observe an actual triggered break** (not just risk exposure) for the untattooed
   pawn specifically: re-run the tool with the pinned value lowered further into that pawn's Major or Extreme
   threshold band (their own MTB odds improve sharply there, research.md R4's underlying `MentalBreaker` findings)
   — expect this to take several trials/a longer duration even then, since break timing is still genuinely
   probabilistic; `windowOpens` (Scenario 2's actual pass/fail signal) doesn't require this at all.

## Scenario 3 — Progression counter and automatic Tier 1 → Tier 2 upgrade (User Story 2 / SC-003)

**Preferred**: with Starlight Ward applied, click **"TEST: Starlight Ward risk window (clean recovery, should
count)"** on the pawn, once per point of `tier2Threshold`, checking the debug log's `PASS`/`FAIL` and the
progression counter after each click.

**Manual alternative**: with **Random mental states disabled** (so mood-recovery windows close cleanly without an
organic break interfering) and Starlight Ward applied, drag the pawn's Mood below their (tattoo-adjusted)
minor-break threshold, hold it there briefly, then drag it back above threshold. Repeat several times, inspecting
the progression counter via Dev Mode between cycles.

1. **Expected**: the counter increments by one each time a below-threshold window closes without a mental break
   having occurred during it (research.md R4) — i.e. once per "dip and recover" cycle, not once per tick spent
   below threshold. Every clean-recovery debug-tool trial should log `PASS`.
2. Continue cycling until the Tier 2 threshold is reached (see Prerequisites for temporarily lowering it, if using
   the manual path).
3. **Expected**: Starlight Ward upgrades to Tier 2 automatically — no new ritual, no ingredient spend, no slot
   change.

## Scenario 4 — Tier 2 stronger resistance, the mood buff, and a break correctly not counting as resisted (User Story 2 / SC-004)

1. With Starlight Ward at Tier 2, re-inspect "Mental break threshold"/"Psychic sensitivity" (Scenario 1's stats)
   and confirm both are more resistant than their Tier 1 values.
2. Open the pawn's mood breakdown (the "i" tooltip on the Mood need). **Expected**: a "starlight's calm" thought
   is present, contributing the Tier 2 mood buff — and confirm this same thought is **absent** on a Tier 1 (or
   untattooed) pawn's mood breakdown (FR-006).
3. **Preferred**: click **"TEST: Starlight Ward risk window (forced break, should NOT count)"** on the pawn.
   **Expected**: the debug log shows the risk window ending as "broken" and reports `PASS` — the progression
   counter is unchanged before/after the trial (research.md R4) — a break happening means nothing was resisted.
   **Manual alternative**: with **Random mental states enabled**, drag mood below threshold and either wait for an
   organic break or force one via Dev Mode's "Mental break..." tool while mood is still below threshold, then
   check the debug log and the progression counter the same way.

## Scenario 5 — No effect without the tattoo, and effect stops immediately on removal (Edge Cases / FR-008/FR-013)

1. Confirm an untattooed colonist (including one with other tattoos/hediffs) shows none of Starlight Ward's
   threshold/sensitivity bonus or Tier 2 mood buff, regardless of mood.
2. With Starlight Ward applied and at a known tier/counter value, remove the hediff via Dev Mode/God Mode's health
   tab.
3. **Expected**: immediately after removal, the pawn's "Mental break threshold"/"Psychic sensitivity" stats revert
   to their pre-tattoo values, the Tier 2 mood buff thought (if present) disappears on the next mood recompute, and
   no further risk-window tracking occurs for that pawn.

## Scenario 6 — Persistence across save/reload (Edge Cases / SC-005)

1. With a Starlight Ward-tattooed pawn at a known tier and progression-counter value (including mid-risk-window, if
   convenient to arrange), save and reload.
2. **Expected**: tier and counter are unchanged immediately after reload compared to immediately before saving, the
   stat offsets (Scenario 1) and Tier 2 mood buff (Scenario 4, if applicable) still apply correctly post-reload,
   and an in-progress risk window resumes being tracked correctly rather than erroring or double-counting.

## Scenario 7 — Removal guard (FR-013, reusing feature 003's existing guard)

1. Apply Starlight Ward to a colonist. With Dev Mode enabled but **God Mode off**, confirm there is no
   delete/remove control for the tattoo hediff on the health tab.
2. Enable **God Mode**, remove the hediff via the health tab. **Expected**: removal succeeds and all of Starlight
   Ward's effects stop immediately (Scenario 5).
3. Re-apply the tattoo, disable God Mode, and attempt a direct `pawn.health.RemoveHediff(...)` call on it (via
   `DebugAction_TestTattooRemovalGuard` or an equivalent Dev Mode tool, per feature 003 precedent). **Expected**:
   the hediff remains — the removal call has no effect, no error logged.

## Scenario 8 — No regressions; zero errors with Combat Extended present; DLC-independence (Constitution Principle I/II, SC-006/FR-014)

1. With Starlight Ward applied alongside a few other tattoos on the same colonist (e.g. Frost Sigil, Ember Ward),
   confirm the other tattoos' own stat offsets and behaviors are unaffected by Starlight Ward's own
   `StatPart_TattooEffectOffset` registrations for two new stats — a light re-check, not a full re-run of every
   prior feature's own quickstart.
2. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with it
   present (research.md R6 predicts zero CE interaction for this feature specifically — no CE-loaded behavior
   difference is expected in Scenarios 1-4, unlike every prior combat-stat tattoo).
3. Confirm the mod loads cleanly and Scenarios 1-4 behave identically with Royalty and/or Ideology **not**
   installed (research.md R7 — both stats are Core-only).

## Pass/fail

All eight scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to be
considered verified, per Constitution Principle II. With this feature complete, all 11 tattoos in the PRD §6
roster have a functioning effect (SC-008) — Scenario 8's cross-tattoo check is this feature's contribution to that
overall milestone, not a full regression suite for every tattoo shipped so far.
