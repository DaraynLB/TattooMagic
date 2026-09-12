# Quickstart: Validating the Shedscale Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run
these after implementation, before marking this feature complete. No automated test suite exists for this
feature (no automated `Verse`/`RimWorld` test harness across features 001–014 either).

**Testing-methodology note**: like Phoenix (feature 014), this feature's core timers are genuinely **days
long** (7/4-day regrowths, a days-long Tier 2 progression clock). Unlike Phoenix, nothing here spans multiple
colonists or faction state — every scenario is single-pawn and needs no `GameComponent` sweep to force.
`Source/Debug/DebugAction_TestShedscaleRegrowth.cs` adds permanent Dev Mode tools under the "TattooMagic"
category, mirroring feature 013/014's own precedent of building dedicated debug tooling for a mechanic too
slow to hand-test:

- **"TEST: Shedscale — force daily pass now"** — runs `HediffComp_ShedscaleEffect.DoDailyPass()` on the
  targeted pawn immediately, bypassing the once-per-day self-gate, so a single click advances every tracked
  regrowth (and the days-worn counter) by exactly one day.
- **"TEST: Shedscale — complete all active regrowths now"** — for each entry in the targeted pawn's
  `activeRegrowths`, jumps its elapsed-days value straight to the tier duration and re-runs the completion
  check, so every in-progress regrowth finishes on the spot without waiting out 7/4 real days.
- **"TEST: Shedscale — set days-worn counter"** / **"...set parts-regrown counter"** — directly set
  `daysWornCounter`/`partsRegrownCounter` on the targeted pawn's comp (re-checking the Tier 2 thresholds
  immediately), mirroring Phoenix's "set days-alive counter" tool.

Also confirm **Options → Dev Mode** is enabled for every scenario below, and that God Mode is available where
noted (fast limb loss via damage tools, removal-guard bypass).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6. Dev Mode enabled; God Mode available where noted.
- Feature 001 already built and working (the ritual station) — use it to apply Shedscale normally, or use Dev
  Mode's "Add hediff" tool to grant `TattooMagic_Hediff_Shedscale` directly to a test pawn for faster setup
  (either path exercises the same `HediffWithComps`/`HediffComp_ShedscaleEffect` attachment).
- A way to remove a body part on a living colonist quickly for setup: Dev Mode's damage tools (deal enough
  damage to destroy a part outright) or the ordinary "Remove body part" surgery bill (`Recipe_RemoveBodyPart`)
  for a controlled, specific part.
- A way to install a prosthetic/bionic quickly: a normal medical bill (simple prosthetic requires no research;
  keep a spare on hand or use Dev Mode to spawn one) plus a doctor to perform it, or Dev Mode's "complete
  current job instantly" to skip the real work-amount wait.
- A way to inspect a pawn's Shedscale tier/day-counter/parts-regrown-counter and their current
  `activeRegrowths` (Dev Mode inspect panel / the health tab, which will show the Strain and
  ImperfectRegrowth support hediffs directly once present).

## Scenario 1 — A missing limb quietly grows back, at both tiers (User Story 1 / SC-001, SC-004)

1. Apply Shedscale to a colonist (ritual station or Dev Mode "Add hediff"). Confirm they start at Tier 1.
2. Remove one eligible external part (e.g. a hand) via the "Remove body part" bill, with nothing installed
   afterward. Click **"TEST: Shedscale — force daily pass now"** once. **Expected**: the health tab shows the
   hand now present in `activeRegrowths` (inspect panel) with 1 elapsed day, and the wound/removal itself
   leaves no other lingering effect yet.
3. Click **"TEST: Shedscale — complete all active regrowths now"**. **Expected**: the hand is back on the
   pawn, and a new `TattooMagic_Hediff_ShedscaleImperfectRegrowth` hediff appears on that specific hand
   (visible on the health tab), reducing its Manipulation contribution — Tier 1's reduced-efficiency penalty
   (FR-012).
4. Use **"set days-worn counter"** or **"set parts-regrown counter"** (Scenario 4) to push the same colonist
   to Tier 2, then repeat Steps 2–3 with a *different* eligible part. **Expected**: the regrowth completes in
   4 simulated days instead of 7 (confirm via elapsed-days progress after repeated "force daily pass now"
   clicks), and **no** `ShedscaleImperfectRegrowth` hediff appears this time (FR-012's Tier 2 exemption).
5. Remove an internal organ (Kidney or Lung) from a **Tier 1** wearer via surgery. **Expected**: it never
   enters `activeRegrowths` regardless of how many daily passes are forced — Tier 1 doesn't cover organs
   (FR-004). Now push the same colonist to Tier 2 (Scenario 4) and force another daily pass. **Expected**: the
   organ now enters `activeRegrowths` and completes in 4 days like any other part (FR-005).
6. Before removing a part, inflict a separate, unrelated permanent injury on it (a scar from an old wound, or
   Dev Mode's "old injury" tool) if possible, then remove that same part and let it fully regrow (Steps 2–3).
   **Expected**: the old permanent injury is gone once regrowth completes (`RestorePart`'s own cleanup,
   research.md R2, FR-007) — the health tab shows a clean part, not the part plus its old scar.
7. Remove two or three eligible parts on one colonist in the same session, then force one daily pass.
   **Expected**: every eligible missing part appears in `activeRegrowths` simultaneously — no one-at-a-time
   restriction (FR-002).

## Scenario 2 — Prosthetics and bionics block regrowth until removed (User Story 2 / SC-003)

1. Remove an eligible part from a Shedscale-tattooed colonist and immediately install a prosthetic/bionic on
   it (a normal medical bill) before forcing any daily pass. Then force several daily passes.
   **Expected**: the part never enters `activeRegrowths` at any point (research.md R1 — a part can never be
   simultaneously missing and installed) and **"Alert: Shedscale regrowth blocked"** (or its authored label)
   appears in the top-right alert list, naming this colonist.
2. Leave the prosthetic installed for several forced daily passes. **Expected**: the alert remains present
   every time it's checked, and the health tab shows no partial regrowth progress anywhere for that part —
   there is no hidden counter ticking underneath the installed replacement (FR-010).
3. Remove the bionic/prosthetic via its own "Remove: [prosthetic]" surgery bill. **Expected**: the part
   becomes genuinely missing again (a fresh `Hediff_MissingPart`, confirmed via the health tab), the alert for
   this colonist clears on its next check (assuming no other blocked part remains), and forcing a daily pass
   now shows the part entering `activeRegrowths` at 0 elapsed days — the full tier duration restarting from
   scratch, not resuming (FR-011).
4. Repeat Steps 1–3 with a bionic organ (Kidney or Lung) on a Tier 2 wearer instead of an external limb.
   **Expected**: identical blocking/alert/restart behavior (FR-008's "same rule covers bionic organs").

## Scenario 3 — Living with an active regrowth: stacking hunger/pain and the flat mood debuff (User Story 3 / SC-005)

1. With a colonist carrying exactly one active regrowth (Scenario 1, Step 2), inspect their health tab.
   **Expected**: `TattooMagic_Hediff_ShedscaleStrain` is present at severity 1, and the Shedscale mood thought
   is present in their mood tab (one instance).
2. Remove a second eligible part on the same colonist and force a daily pass so both are tracked.
   **Expected**: the Strain hediff's severity is now 2 (visibly higher hunger-rate/pain values on its tooltip
   than Step 1), but the mood tab still shows only **one** instance of the Shedscale mood thought — it does
   not stack per part (FR-015).
3. Induce malnutrition on this colonist (withhold food, or Dev Mode's hunger tools) while both regrowths are
   still in progress, then force a daily pass. **Expected**: neither tracked part's elapsed-days value
   advances that day (confirm via the inspect panel before/after) — malnutrition stalls all of this colonist's
   regrowth timers at once (FR-014). Feed them back to `Fed`/`Hungry` and force another daily pass.
   **Expected**: both timers resume advancing normally.
4. Let both regrowths complete (Scenario 1, Step 3-equivalent). **Expected**: the Strain hediff disappears
   entirely once the second-to-last and then the last regrowth completes, and the mood thought disappears once
   the count reaches zero.

## Scenario 4 — Reaching Tier 2 by either path, with retroactive efficiency clearing (User Story 4 / SC-006)

1. Apply Shedscale to a fresh colonist. Use **"set days-worn counter"** to set it to one below the
   `tier2DaysWornThreshold` placeholder, then force one more daily pass. **Expected**: the tattoo upgrades to
   Tier 2 automatically (no ritual, no item) — confirm via the inspect panel or `TattooTierProgressUtility
   .DescribeTier`'s UI display.
2. On a fresh Tier 1 colonist, regrow 2 parts fully (Scenario 1), each leaving a `ShedscaleImperfectRegrowth`
   hediff behind. Use **"set parts-regrown counter"** to set it to one below the `tier2PartsRegrownThreshold`
   placeholder, then regrow a 3rd part to completion normally. **Expected**: the moment that 3rd regrowth
   completes, the tattoo upgrades to Tier 2 immediately (FR-016's "whichever comes first"), **and** the two
   `ShedscaleImperfectRegrowth` hediffs from the earlier Tier 1 regrowths are both gone from the health tab in
   that same moment (FR-018's retroactive clear) — confirm the 3rd part's own regrowth also carries no
   efficiency penalty despite having started under Tier 1 (it completes *after* the Tier 2 upgrade check, so
   apply order matters — verify `CompleteRegrowth`'s own `partsRegrownCounter++`/`TryUpgradeToTier2()`
   ordering in data-model.md produces the intended outcome for this exact part too, not just the two earlier
   ones).
3. Confirm the upgrade is permanent: kill and revive the colonist if a Phoenix tattoo is also present, or
   simply save/reload (Scenario 6), and confirm `tier == 2` still holds with no path back to Tier 1.

## Scenario 5 — No rarity cap (User Story/Assumption — no dedicated user story, but an explicit spec requirement, FR-019 / SC-007)

1. Apply Shedscale to every free colonist on the map (as many as available — at least 4–5 for a meaningful
   check). **Expected**: `Dialog_ChooseTattoo` never withholds Shedscale as an option for any colonist
   regardless of how many others already carry it — contrast directly with Phoenix's 3-tattoo cap behavior
   (feature 014 quickstart Scenario 2), confirming this feature introduces no analogous gate anywhere.

## Scenario 6 — Removal guard, and removal resetting all in-progress state (research.md R10)

1. Apply Shedscale to a colonist. With Dev Mode enabled but **God Mode off**, confirm there is no direct
   delete/remove control for the Shedscale hediff on the health tab (same protection every prior tattoo
   already has, `TattooHediffRemovalGuard`, feature 001 — no Shedscale-specific configuration needed).
2. Get a colonist partway through one regrowth (some elapsed days, not yet complete) and partway through Tier
   2 progress (a nonzero `daysWornCounter`/`partsRegrownCounter`, still Tier 1). Enable **God Mode** and remove
   the Shedscale hediff via the health tab. **Expected**: the missing part remains missing (regrowth simply
   stopped, per the spec's clarified removal behavior) and the Strain hediff (if present) is removed from the
   pawn in the same moment (`CompPostPostRemoved`, data-model.md).
3. Re-apply a fresh Shedscale tattoo to the same colonist (ritual station or Dev Mode "Add hediff") while that
   same part is still missing. Force a daily pass. **Expected**: the part starts a **brand-new** regrowth at 0
   elapsed days (not resumed from wherever it left off), and the new tattoo's own `daysWornCounter`/
   `partsRegrownCounter` both start at 0 — no memory of the previous tattoo instance's progress carries over,
   matching the spec's Clarifications exactly.

## Scenario 7 — Persistence across save/reload (Constitution Principle V)

1. With a Shedscale-tattooed colonist mid-regrowth on one part (a known elapsed-days value beforehand) and
   partway through Tier 2 progress, save and reload. **Expected**: `activeRegrowths` still shows the same
   part with the same elapsed-days value, and `daysWornCounter`/`partsRegrownCounter` are unchanged —
   forcing a daily pass afterward continues counting from exactly where it left off.
2. Do the same with a colonist carrying an active Strain hediff (2+ concurrent regrowths) and the Shedscale
   mood thought. **Expected**: both are still present, at the same severity/state, immediately after reload
   with no need to force anything.
3. Save and reload with a colonist carrying one or more permanent `ShedscaleImperfectRegrowth` hediffs from
   completed Tier 1 regrowths. **Expected**: they persist unchanged, and reaching Tier 2 afterward still
   correctly clears all of them (re-run the relevant part of Scenario 4 post-reload).

## Scenario 8 — No regressions; zero errors with Combat Extended present (Constitution Principle I/II)

1. With Shedscale applied alongside a few other tattoos on the same colonist (e.g. Phoenix, Frost Sigil),
   confirm the other tattoos' own behaviors are unaffected by Shedscale's presence — a light re-check, not a
   full re-run of every prior feature's own quickstart. In particular, confirm a Phoenix-revived colonist who
   is also missing parts from the killing blow correctly starts Shedscale regrowth on those parts once alive
   again (the two tattoos' documented conceptual pairing, spec.md Assumptions — no direct code interaction
   required, just both operating correctly on the same living pawn).
2. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with it
   present (research.md R8 predicts zero CE-specific behavior difference anywhere in this feature, and this
   feature adds no Harmony patches at all to conflict with anything). Re-check Scenario 1 and Scenario 2 once
   under CE specifically, since CE does touch `Hediff_MissingPart` in one narrow, unrelated way (research.md
   R8) — confirm missing-part detection and regrowth completion still behave identically.
3. Confirm the mod loads cleanly and Scenarios 1–7 behave identically with Royalty and/or Ideology **not**
   installed (nothing in this feature is DLC-gated).

## Pass/fail

All eight scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to
be considered verified, per Constitution Principle II. Shedscale is the 15th tattoo and the first built with
zero new Harmony patches (research.md R7) — Scenario 8's cross-tattoo check is this feature's own regression
contribution, not a full re-verification of every tattoo shipped so far.
