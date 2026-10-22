# Quickstart: Validating the Soul Tether Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature
(unchanged precedent from features 001-016 — `Verse`/`RimWorld` game types aren't practically runnable outside the
game process).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `1.6/Assemblies/` and `1.7/Assemblies/` (dual
  build, since 2026-10-21).
- Mod enabled (launched via RimSort) in RimWorld 1.7, the primary test target. Smoke-test a load on 1.6 before
  release.
- Dev Mode enabled; use Dev Mode's "Add hediff" tool to grant `TattooMagic_Hediff_SoulTether` directly to a test
  pawn for faster iteration than the ritual station.
- At least 2 colonist/ally pawns available and controllable via Dev Mode (one to cast, one to be pulled), plus a
  way to down a pawn (Dev Mode "Down") and kill one (Dev Mode "Kill", leaving a corpse) without ending the game.
- A hostile-faction pawn (and, separately, that hostile's own corpse via killing it) available to confirm they are
  rejected as targets.
- `TattooMagicSettings.EnableDebugLogging` enabled to observe activation, pull, and tend-buff-grant `Log.Message`
  output.
- A second RimWorld instance/save with Combat Extended loaded, for Scenario 5.

## Scenario 1 — Pulling a downed ally applies the tend buff (User Story 1 / SC-001, SC-002, SC-004)

1. Apply Soul Tether to a colonist. Select them. **Expected**: a "Soul Tether" gizmo appears, enabled.
2. Click the gizmo. **Expected**: the cursor enters targeting mode with a range ring around the caster matching
   Tier 1's max range.
3. Hover the caster themselves. **Expected**: not selectable (a caster cannot target itself).
4. Hover a hostile-faction pawn. **Expected**: not selectable, with a "not friendly"-style reason.
5. Down a separate colonist (Dev Mode) at a distance within Tier 1's range and hover them. **Expected**: selectable,
   with no reject reason shown despite being downed.
6. Move (or spawn) a friendly colonist beyond Tier 1's max range and hover them. **Expected**: not selectable, with
   a "too far away"-style reason; confirm this at both Tier 1 and (after reaching Tier 2 in Scenario 3) Tier 2
   range values.
7. Click the downed colonist from step 5. **Expected** (FR-007a, research.md R9): a brief energy-tether line
   flashes between caster and target and a "vanish" flash/sound plays at the target's position immediately;
   roughly a third of a second later (not instant, not a noticeable delay — 15-30 ticks), an "arrival" flash/sound
   plays at the destination and the colonist actually appears there (debug log confirms both the initial activation
   and the later "pull resolved" message), immediately gaining a hediff granting the flat tend speed/quality bonus,
   visible in their health tab with a remaining-duration countdown. The whole sequence should read as a quick,
   deliberate snatch, not an instant teleport and not a sluggish float.
8. Have a doctor tend the pulled colonist's injuries. **Expected**: the resulting tend quality is measurably higher,
   and the tend job completes measurably faster, than an equivalent tend on a pawn without the buff (compare two
   otherwise-identical tends, with and without an active buff, same doctor/medicine).
9. Wait for the tend buff's duration to elapse. **Expected**: the hediff disappears and subsequent tends on that
   pawn return to normal quality/speed.

## Scenario 2 — Pulling a corpse (User Story 2)

1. Kill a colonist (Dev Mode "Kill") away from the caster, leaving a corpse, within range.
2. Click the Soul Tether gizmo and hover the corpse. **Expected**: selectable (no reject reason).
3. Kill a hostile-faction pawn nearby instead, leaving a hostile corpse, and hover it. **Expected**: NOT
   selectable, with a "not friendly"-style reason — confirming research.md R2's corpse-faction check works
   correctly (a naive `GenHostility.HostileTo` check on the bare corpse would have wrongly allowed this).
3b. Kill a factionless or neutral pawn (e.g. Dev Mode "Spawn Pawn\Drifter") within range and hover its corpse.
   **Expected**: NOT selectable, "Not friendly.": only colony/ally pawns are valid (spec Assumptions; issue #5).
4. Click the friendly corpse from step 1. **Expected**: the same quick snatch as Scenario 1 step 7 (vanish flash,
   then arrival about 20 ticks later), landing it in a cell adjacent to the caster;
   no tend-buff hediff is granted (there is nothing to grant it to in a way that matters — confirm no error/crash
   and no hediff appears "on" the corpse).

## Scenario 3 — Cooldown and Tier 2 upgrade (User Story 1 / SC-005; User Story 3 / SC-006, SC-007)

1. Immediately after Scenario 1's pull, click the gizmo again. **Expected**: gizmo is disabled with a visible
   remaining-cooldown reason; no new pull occurs.
2. Wait for the cooldown to expire, then activate the ability (a full successful pull) repeatedly until the Tier 2
   threshold is reached. **Expected**: the progression counter increments once per successful pull (debug log), and
   the tattoo upgrades to Tier 2 in place — no new ritual, ingredient spend, or slot change.
3. At Tier 2, repeat Scenario 1 step 6 (range check). **Expected**: the max range is now measurably longer than
   Tier 1's.
4. At Tier 2, pull a downed ally and tend them. **Expected**: the resulting tend quality/speed bonus is measurably
   stronger than Tier 1's own bonus (Scenario 1 step 8), while the pull itself still affects only the one chosen
   target — confirm no other nearby friendly pawn is relocated or buffed by the same cast.

## Scenario 4 — Edge cases (Edge Cases)

1. Attempt to target a friendly pawn currently off-map (e.g. in a caravan, if testable, or simply confirm the
   targeter's own candidate list never includes an unspawned pawn). **Expected**: not targetable.
2. Target a friendly pawn standing already adjacent to the caster. **Expected**: the pull still resolves (no error,
   no requirement that the target be far away) and the tend buff is still granted.
3. Pull a friendly pawn who has an in-progress job or an existing mental state / bleeding status. **Expected**:
   after the pull, that job/mental state/bleeding status is unchanged except for position — confirm the pawn simply
   continues (or resumes) whatever they were doing at the new location.
4. Remove the caster's Soul Tether hediff (Dev Mode) after a pull's tend buff is already active on some other pawn.
   **Expected**: the gizmo disappears from the caster and further casts are prevented, but the already-granted tend
   buff on the other pawn is unaffected and continues to its natural expiry (FR-024).
5. Cast Soul Tether twice in a row on the same already-buffed target once cooldown allows. **Expected**: the buff
   is refreshed to the current tier's full magnitude/duration rather than stacking a second hediff instance.

## Scenario 5 — Save/reload persistence and Combat Extended (Edge Cases, SC-008, SC-009)

1. With a Soul-Tether-tattooed pawn at a known tier/progression-counter value, and a separate pawn carrying an
   active tend buff with known remaining duration, save and reload. **Expected**: both the caster's tier/counter and
   the buffed pawn's remaining tend-buff duration are unchanged immediately after reload compared to immediately
   before saving.
2. Load Combat Extended. Repeat Scenario 1 end-to-end (pull a downed ally, confirm the tend buff applies to a CE
   game's own tend flow if CE modifies tending at all — if CE does not touch `TendUtility`/`JobDriver_TendPatient`,
   confirm behavior is identical to the non-CE case). **Expected**: no errors; tend buff applies the same way.
3. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with it
   present, and that every other triggered tattoo's own gizmo/cooldown/tier behavior is unaffected by this
   feature's two new patches (`Patch_TendUtility_CalculateBaseTendQuality_SoulTetherBuff`,
   `StatPart_SoulTetherTendSpeedBuff` registered on `MedicalTendSpeed`) — a light re-check that tending an
   un-buffed pawn (the overwhelming majority case) still produces vanilla-identical quality/speed, not a full
   re-run of every other tattoo's own quickstart.

## Pass/fail

All five scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to be
considered verified, per Constitution Principle II.
