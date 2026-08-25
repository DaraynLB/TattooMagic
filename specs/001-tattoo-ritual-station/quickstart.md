# Quickstart: Validating the Tattoo Ritual Station

Manual, in-game validation steps satisfying Constitution Principle II
("Verify Before Claiming Done"). Run these after implementation, before
marking this feature complete. No automated test suite exists for this
feature (see `research.md` R5).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in
  `Assemblies/`, per the `rimworld-modding` toolchain.
- Mod enabled in RimWorld 1.6 (Harmony present; Combat Extended not required
  for this feature).
- Dev Mode enabled (Options → Dev Mode).
- A Dev quicktest map (or any save) with at least two colonist pawns.

## Scenario 1 — Happy path (User Story 1 / SC-001)

1. Use the architect menu to build a Tattoo Ritual Station; wait for
   construction to complete.
2. Use Dev Mode tools to set one colonist's Artistic skill high (e.g. 15+).
   This is the performer.
3. Ensure the required ritual ingredients (per the chosen `TattooDef`) are
   present in the colony's stockpile.
4. Queue a ritual at the station targeting a second colonist (the
   recipient) with a specific tattoo selected.
5. Let the job run to completion.
6. **Expected**: Ingredients are consumed; the recipient gains the tattoo's
   Hediff (visible on their Health tab); the recipient's free tattoo-slot
   count decreases by one; the debug log shows no red Harmony/mod errors.

## Scenario 2 — Low-skill mishap (User Story 2 / SC-002)

1. Repeat Scenario 1, but set the performer's Artistic skill to `0`.
2. Run several ritual attempts (skill checks are probabilistic — a single
   attempt isn't proof either way).
3. **Expected**: Some attempts resolve as a mishap — the recipient does NOT
   gain the tattoo, their slot count is unchanged, and the ingredients used
   for that attempt are not returned. No console errors on any outcome.
4. Repeat with Artistic skill at `20` and confirm the success rate is
   visibly higher than at skill `0`, confirming SC-002.

## Scenario 3 — Slot limit enforcement (User Story 3 / SC-003)

1. Take a recipient pawn already at their current slot capacity (default 1
   — apply one tattoo successfully first via Scenario 1, or use Dev Mode to
   set `slotCapacity`/`appliedTattoos` directly if a debug action exists).
2. Attempt to queue a second ritual targeting the same pawn.
3. **Expected**: The ritual cannot be started; the game communicates why
   (per FR-009). No partial state changes on the recipient.

## Scenario 4 — Duplicate tattoo prevention (edge case / SC-004)

1. Take a recipient who already has a specific tattoo applied (and has a
   free slot available for a second, different tattoo).
2. Attempt to select the *same* tattoo again for that recipient.
3. **Expected**: That tattoo is not selectable/queueable for a recipient who
   already has it.

## Scenario 5 — Interruption mid-ritual (edge case / SC-005)

1. Start a ritual (Scenario 1 setup).
2. While `InProgress`, forcibly interrupt the performer — draft them away,
   or trigger a raid/mental break.
3. **Expected**: The job cancels; the recipient ends up with no tattoo and
   no slot consumed (state as if the ritual never started); no leftover
   partial Hediff or inconsistent tracker state. Re-queuing the same ritual
   afterward behaves like a fresh attempt.

## Pass/fail

All five scenarios must match their "Expected" outcome, with zero red
Harmony/mod errors in the debug log throughout, for this feature to be
considered verified per Constitution Principle II.
