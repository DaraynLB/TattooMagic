# Quickstart: Validating the Tattoo-Mastery XP Track & Slot Unlocking

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature
(research.md R7).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode) — both `slot2Threshold`/`slot3Threshold` ship as small, fast-to-reach
  placeholder values (`3`/`8`) specifically so real activations are enough for most scenarios below without
  needing Dev Mode workarounds (research.md R8); the `Prefs.DevMode`-gated "[DEV] +N tattoo mastery progress"
  button in the ritual station's tattoo-selection dialog (research.md R9) is used for Scenario 8 specifically,
  and can speed up any other scenario if needed. God Mode available if needed.
- Feature 001 (ritual station) working, to apply tattoos. Feature 002 (Frost Sigil) and/or feature 003 (Serpent's
  Eye), feature 004 (Guardian's Call), and/or feature 005 (Stormlash) applied to a test colonist — at least one
  tattoo (triggered or passive, revised 2026-08-12/research.md R10) is required to generate mastery progress at
  all.

## Scenario 1 — Activating a triggered ability accrues pawn-wide mastery progress (User Story 1 / SC-001)

1. Apply Guardian's Call to a colonist with slot capacity 1 (the default).
2. Activate the Guardian's Call gizmo several times (waiting out or Dev-Mode-bypassing its cooldown each time).
3. **Expected**: each activation is counted — confirm via a temporary Dev Mode inspection (e.g. a debug log line
   or the Dev Mode inspector) that the pawn's tracked mastery progress increases by exactly one per activation,
   with zero increase from anything else (waiting, taking damage, other pawns' activations).

## Scenario 2 — First threshold grants slot 2 automatically (User Story 1 / SC-002)

1. Continuing from Scenario 1, keep activating Guardian's Call until mastery progress reaches the slot-2
   threshold (`Defs/HediffDefs/TattooTracker.xml`'s `slot2Threshold`, or a lowered `TattooEffectValues` override
   for faster iteration).
2. **Expected**: the instant the threshold is crossed, the pawn's slot capacity becomes 2 with no ritual,
   ingredient spend, or other player action — and an on-screen message names the pawn and the new slot count
   (FR-009).
3. Open the ritual station's tattoo-selection dialog for that pawn. **Expected**: the dialog shows the pawn now
   has 2 total slots (1 free, assuming only Guardian's Call is applied), and a second tattoo can be selected and
   queued for the ritual (FR-010, SC-006).

## Scenario 3 — Second threshold grants slot 3, and capacity never exceeds 3 (User Story 2 / SC-003, SC-004)

1. Continuing from Scenario 2, keep activating triggered ability(s) — either the same tattoo repeatedly, or a
   second triggered tattoo (e.g. Stormlash) applied into the pawn's newly-unlocked second slot — until mastery
   progress reaches the slot-3 threshold.
2. **Expected**: slot capacity becomes 3, with its own on-screen message.
3. Keep activating well past the slot-3 threshold. **Expected**: slot capacity stays at 3 — no error, no
   message, mastery progress may keep counting internally but no further slot is ever granted (SC-004).

## Scenario 4 — Progress from two different triggered tattoos shares one pawn-wide total (User Story 2, Acceptance Scenario 3)

1. On a pawn with both Guardian's Call and Stormlash applied (e.g. the pawn from Scenario 3), activate Guardian's
   Call a few times, note the mastery progress value, then activate Stormlash a few times.
2. **Expected**: Stormlash's activations continue incrementing the same total Guardian's Call's activations were
   incrementing — not a separate counter — confirming a single pawn-wide track rather than per-tattoo tracks
   (spec Assumptions).

## Scenario 5 — Passive tattoos contribute too; only a tattoo-less pawn never progresses (Edge Cases / SC-008, revised 2026-08-12)

*(Originally: passive-only pawns never accumulate progress. Reversed after playtesting found that left
passive-only pawns with no path to ever unlock a slot — see spec.md Assumptions, research.md R10. Scenario 5 now
covers both the surviving "truly never progresses" case and the new passive-contribution case.)*

1. On a colonist with **no tattoo applied at all**, let time pass and put the colonist through some combat.
   **Expected**: that colonist's mastery progress never increases and slot capacity remains 1 — there is no
   tattoo comp for any event to dispatch to.
2. On a colonist with **only a passive tattoo applied** (e.g. Frost Sigil), have another pawn melee-attack them a
   few times (Frost Sigil's `OnMeleeHitTaken` is dispatched on the *wearer* taking a hit, not landing one — use
   Dev Mode's melee-attack tools, aimed at this pawn, if needed). **Expected**: mastery progress increases by one
   per dispatched hit, confirmed via the T004 log line, regardless of whether Frost Sigil's own slow proc
   succeeds or fails that hit (its own proc-chance log line, if `Prefs.DevMode`, is a separate, unrelated roll —
   FR-004/R10). Once enough hits land, slot capacity grows exactly as it would from triggered-tattoo activations.

## Scenario 6 — Persists correctly across save/reload (User Story 3 / SC-007)

1. With a pawn at a known, partial mastery-progress value (below the next threshold), save and reload the game.
   **Expected**: mastery progress and slot capacity are unchanged immediately after reload compared to
   immediately before saving.
2. With a pawn who has already unlocked slot 2 or slot 3, save and reload. **Expected**: the pawn retains that
   slot capacity — it does not revert to fewer slots.

## Scenario 7 — Coexists correctly with Guardian's Call and Stormlash's own mechanisms, no regressions

1. On the pawn from Scenario 4, confirm Guardian's Call's own tier-progression counter and Stormlash's own
   tier-progression counter are each still counting correctly and independently of each other and of the new
   pawn-wide mastery counter (i.e. this feature added a new counter, it did not repurpose or interfere with
   either tattoo's existing per-tattoo counter).
2. Confirm both gizmos' cooldown/availability behavior is unchanged from before this feature (activating one
   still does not affect the other's cooldown).
3. Confirm zero red Harmony/mod errors in the debug log throughout every scenario above.

## Scenario 8 — A single jump crosses both thresholds at once (research.md R4, R9 — **required**)

1. On a fresh pawn at `masteryProgress = 0` / `slotCapacity = 1` (Dev-Mode-grant a triggered tattoo if needed
   just so the tracker exists, no activations required), open the ritual station's tattoo-selection dialog and
   use the `"[DEV] +N tattoo mastery progress"` button to add enough progress in one click to clear both
   `slot2Threshold` (`3`) and `slot3Threshold` (`8`) at once (e.g. add `10`).
2. **Expected**: `slotCapacity` jumps straight from 1 to 3, and **two separate** on-screen messages appear (one
   naming 2 slots, one naming 3 slots) — not a single message, and not a silent skip straight to 3 slots with no
   message at all. This is the one scenario that actually exercises `CheckSlotThresholds()`'s `while` loop
   crossing more than one threshold in a single check, which no sequence of real activations can produce (each
   activation only ever adds one point of progress before checking).
3. Continue clicking the debug button. **Expected**: `slotCapacity` stays at 3 with no further messages;
   `masteryProgress` keeps increasing (confirm via the T004 log line) but nothing else changes.

## Pass/fail

All eight scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to be
considered verified, per Constitution Principle II — **Scenario 8 is required, not optional**, since it's the
only way to confirm the multi-crossing branch actually works rather than just being reasoned about (research.md
R4). This feature has no Combat Extended-specific behavior (no combat stat or targeting logic touched), so no
separate CE-loaded pass is required — Constitution Principle I still requires the mod to load cleanly with
Harmony + CE present, which Scenario 7's "zero red errors" check covers if CE happens to be enabled in the test
environment.
