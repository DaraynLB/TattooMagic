# Quickstart: Validating the Wraithstep Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature
(research.md precedent from features 001–007).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode), God Mode available for Scenario 7.
- Feature 001 already built and working (the ritual station) — use it to apply Wraithstep, or use Dev Mode's "Add
  hediff" tool to grant `TattooMagic_Hediff_Wraithstep` directly to a test pawn for faster iteration.
- An open, partially-explored map area with some unwalkable terrain (a wall, deep water, or similar) and some
  unexplored/fogged tiles nearby, to exercise Scenario 2's rejection cases.
- At least one hostile pawn reachable for Scenarios 5-6 (a raid, a manhunter, or a Dev Mode-spawned hostile).

## Scenario 1 — Gizmo, targeting, successful blink, and cooldown (User Story 1 / SC-001, SC-004)

1. Apply Wraithstep to a colonist. Select them. **Expected**: a "Wraithstep" gizmo appears, enabled.
2. Click the gizmo. **Expected**: the cursor enters targeting mode (matches vanilla's own targeted-ability feel —
   e.g. aiming a thrown item).
3. Click a valid, walkable, explored, unoccupied cell within range. **Expected**: the colonist instantly appears at
   that cell; the gizmo immediately shows disabled with a cooldown reason (remaining time visible).
4. Attempt to activate again while on cooldown. **Expected**: nothing happens (gizmo disabled, not clickable) — no
   error in the debug log.
5. Wait for the cooldown to expire. **Expected**: gizmo becomes enabled again.

## Scenario 2 — Range indicator, reject reasons, and invalid destinations (User Story 1 / SC-002, SC-003)

1. Click the gizmo again. **Expected**: a visible range ring appears centered on the colonist matching the
   ability's current maximum range.
2. Hover a cell beyond that ring. **Expected**: the cell is shown as unselectable and a reason is displayed (e.g.
   "out of range"); clicking it does nothing (no blink, no cooldown started, no error).
3. Hover an unwalkable cell within range (a wall, deep water). **Expected**: shown as unselectable with a reason;
   clicking it does nothing.
4. Hover a fogged/unexplored cell within range. **Expected**: shown as unselectable with a reason; clicking it does
   nothing.
5. Move another pawn onto a cell within range, then hover that cell. **Expected**: shown as unselectable (occupied)
   with a reason; clicking it does nothing.
6. Press Escape (or right-click) to cancel targeting entirely. **Expected**: targeting ends, no blink occurs, the
   gizmo remains enabled (cooldown never started), and — per Scenario 4 below — no progression counter or mastery
   credit was granted for this cancelled attempt.

## Scenario 3 — No effect without the tattoo, and downed pawns can't use it (Edge Cases / FR-011)

1. Confirm an untattooed colonist (including one with other tattoos/hediffs) shows no Wraithstep gizmo and their
   position is never affected by anything Wraithstep-related.
2. Down a Wraithstep-tattooed colonist (Dev Mode). **Expected**: the Wraithstep gizmo is unavailable/disabled,
   consistent with vanilla's own handling of movement-dependent abilities for incapacitated pawns.

## Scenario 4 — Progression counter, Tier 2 upgrade, longer range, and the untargetable window (User Story 2 / SC-005, SC-006, SC-007, SC-008)

1. With Wraithstep at Tier 1, perform several **successful** blinks (Scenario 1) and confirm the progression
   counter (debug log or an added temporary `Log.Message`) increments by exactly one per successful blink.
2. Perform a **cancelled** targeting attempt (Scenario 2, step 6) and confirm the counter does **not** increment.
3. Repeat successful blinks until the Tier 2 threshold is reached. **Expected**: the tattoo upgrades to Tier 2 in
   place — no new ritual, ingredient spend, or slot change.
4. Open the targeter again at Tier 2. **Expected**: the range ring is visibly larger than Tier 1's, and a cell
   beyond Tier 1's old maximum (but within Tier 2's) is now selectable.
5. With at least one hostile pawn nearby and hostile to the tattooed pawn, blink at Tier 2 so the hostile could
   otherwise select the wearer as a target. **Expected**: for the configured window immediately after landing, the
   hostile's targeting does not lock onto the wearer (observe via the hostile's behavior, or temporary `Log.Message`
   probes in the targeting-exclusion Prefix per this project's proactive-debug-logging practice); once the window
   elapses, the hostile can target the wearer normally again.
6. Confirm the wearer remains visible on-screen and interactable by the player throughout the untargetable window
   (it is not stealth/invisibility — Edge Cases).

## Scenario 5 — Interaction with Guardian's Call (targeting-exclusion-contract.md §3)

1. Apply both Guardian's Call and Wraithstep (Tier 2) to the same colonist.
2. Activate Guardian's Call's taunt, then immediately blink with Wraithstep while the taunt is still active.
3. **Expected**: for as long as the post-blink untargetable window is open, hostiles do not lock onto the wearer via
   Guardian's Call's taunt either (the exclusion composes with the taunt's own validator check, research.md R6) —
   once the window elapses, the taunt resumes working normally for its own remaining duration.

## Scenario 6 — Coexists correctly with the other triggered tattoos, validating the reuse point for a fourth consumer (User Story 3 / SC-011)

1. Apply Wraithstep alongside Guardian's Call, Stormlash, and/or Bloodrune on the same colonist (Dev Mode-grant is
   fine for this check).
2. Select the colonist. **Expected**: every applicable ability's gizmo appears independently.
3. Blink with Wraithstep. **Expected**: the other tattoo(s)' gizmo/cooldown state is unaffected.
4. Activate one of the other triggered tattoos. **Expected**: Wraithstep's gizmo/cooldown state is unaffected.
5. Confirm no code change was needed in `HediffComp_GuardiansCallEffect.cs`, `HediffComp_StormlashEffect.cs`, or
   `HediffComp_BloodruneEffect.cs` to make this work — a code-review confirmation, not an in-game one.

## Scenario 7 — Wraithstep feeds the tattoo-mastery XP track only on successful blinks (User Story 3 / SC-014)

1. Note the colonist's current tattoo-mastery progress (feature 006's debug log line, or the ritual station
   dialog's slot-usage label).
2. Perform one **cancelled** targeting attempt. **Expected**: mastery progress is unchanged.
3. Perform one **successful** blink. **Expected**: mastery progress increases by exactly one — the same way it
   would for Guardian's Call, Stormlash, or Bloodrune — confirming the `IHandlesOwnMasteryProgress` opt-out
   (research.md R4) correctly defers credit to the confirmed activation instead of the gizmo click.

## Scenario 8 — Removal guard and save/reload persistence (Edge Cases / FR-017, FR-018, SC-009)

1. With a Wraithstep-tattooed pawn at a known tier and progression-counter value, save and reload. **Expected**:
   tier and counter are unchanged immediately after reload compared to immediately before saving.
2. With Dev Mode enabled and **God Mode off**, confirm the tattoo hediff has no delete/remove control on the health
   tab (matches vanilla's own behavior, per feature 003's existing guard).
3. Enable **God Mode**, remove the hediff via the health tab. **Expected**: removal succeeds and the gizmo and any
   active Tier 2 untargetable window stop immediately (FR-017).
4. Re-apply the tattoo, disable God Mode, and attempt a direct `pawn.health.RemoveHediff(...)` call on it (via a Dev
   Mode debug action or throwaway test patch, if available). **Expected**: the hediff remains — the removal call has
   no effect, no error logged (FR-018, reusing feature 003's existing guard with zero Wraithstep-specific
   configuration).

## Scenario 9 — No regressions, zero errors, both without and with Combat Extended (Constitution Principle I/II, targeting-exclusion-contract.md §4)

1. Re-confirm Guardian's Call's, Stormlash's, and Bloodrune's own gizmos, cooldowns, and tier progression are
   unaffected by this feature (a light re-check, not a full re-run of features 004/005/007's own quickstarts).
2. Repeat Scenario 4, steps 5-6 (the Tier 2 untargetable window) with Combat Extended loaded and a CE-controlled
   hostile nearby. **Expected**: the same exclusion behavior holds; confirm via temporary `Log.Message` probes
   whether the existing Prefix alone covers CE-controlled hostiles or a second CE-specific patch is required
   (research.md R6 / targeting-exclusion-contract.md §4).
3. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with it
   present.

## Pass/fail

All nine scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to be
considered verified, per Constitution Principle II.
