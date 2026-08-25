# Quickstart: Validating the Bloodrune Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature
(research.md precedent from features 001–006).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode), God Mode available for Scenario 6.
- Feature 001 already built and working (the ritual station) — use it to apply Bloodrune, or use Dev Mode's "Add
  hediff" tool to grant `TattooMagic_Hediff_Bloodrune` directly to a test pawn for faster iteration.
- A way to injure a test pawn on demand for Scenarios 1 and 3 (Dev Mode's damage tools, or letting a hostile land
  a hit) — the heal burst needs an actual open injury to have a visible effect.

## Scenario 1 — Gizmo, activation, healing, and cooldown (User Story 1 / SC-001, SC-002)

1. Apply Bloodrune to a colonist, then injure them (Dev Mode damage tool or real combat) so there's something to
   heal.
2. Select the colonist. **Expected**: a "Bloodrune" gizmo appears, enabled.
3. Note the colonist's current injury severity/health percentage on the Health tab.
4. Activate the gizmo. **Expected**: over the next several seconds, the injury's severity measurably decreases
   (health percentage improves) in more than one visible step, not all at once in a single frame; the gizmo
   immediately shows disabled with a cooldown reason (remaining time visible in its tooltip/label).
5. Attempt to activate again while on cooldown. **Expected**: nothing happens (gizmo is disabled, not clickable)
   — no error in the debug log.
6. Let the burst's duration fully elapse. **Expected**: healing stops; health reflects whatever was restored
   during the burst.
7. Wait for the cooldown to expire. **Expected**: gizmo becomes enabled again.

## Scenario 2 — No effect without the tattoo, and no error when there's nothing to heal (Edge Cases / FR-008)

1. Confirm an untattooed colonist (including one with other tattoos/hediffs) shows no Bloodrune gizmo and is
   unaffected by anything Bloodrune-related.
2. Apply (or Dev-Mode-grant) Bloodrune to a colonist who is currently at full health with no injuries. Activate
   the ability. **Expected**: the gizmo still goes on cooldown (the ability triggers normally) but nothing visibly
   heals, and no error appears in the debug log (spec User Story 1, Acceptance Scenario 5).

## Scenario 3 — Progression counter, Tier 2 upgrade, and Tier 2's pain reduction (User Story 2 / SC-003, SC-004, SC-005, SC-006)

1. With Bloodrune at Tier 1, activate the ability repeatedly (waiting out cooldowns, or using Dev Mode to
   reduce/bypass cooldown for faster iteration) until the Tier 2 threshold is reached.
2. **Expected**: the progression counter increments by exactly one per activation; the tattoo upgrades to Tier 2
   in place with no new ritual, ingredient spend, or slot change.
3. Injure the pawn again, activate the ability at Tier 2, and compare the heal against Scenario 1's Tier 1
   result. **Expected**: Tier 2's heal is measurably stronger and/or longer-lasting.
4. While that Tier 2 burst is active, open the Health tab and compare the pawn's pain percentage against an
   otherwise-identical pawn (or the same pawn's own pre-activation pain level) with equivalent injuries.
   **Expected**: pain is measurably lower while the burst is active.
5. Let the burst's duration elapse. **Expected**: pain returns to its normal (unreduced) level for the pawn's
   remaining injuries, with no lingering reduction.

## Scenario 4 — Coexists correctly with Guardian's Call and Stormlash, validating the reuse point for a third consumer (User Story 3 / SC-009, SC-010)

1. Apply Bloodrune alongside Guardian's Call and/or Stormlash on the same colonist (Dev Mode-grant is fine for
   this check).
2. Select the colonist. **Expected**: every applicable ability's gizmo appears independently.
3. Activate Bloodrune. **Expected**: the other tattoo(s)' gizmo/cooldown state is unaffected.
4. Activate one of the other triggered tattoos. **Expected**: Bloodrune's gizmo/cooldown state is unaffected.
5. Confirm no code change was needed in `HediffComp_GuardiansCallEffect.cs`, `HediffComp_StormlashEffect.cs`, or
   `Patch_Pawn_GetGizmos_TattooGizmos.cs` to make this work — a code-review confirmation, not an in-game one.

## Scenario 5 — Bloodrune automatically feeds the tattoo-mastery XP track (User Story 3 / SC-011, feature 006 regression check)

1. Note the colonist's current tattoo-mastery progress (feature 006's `[TattooMagic] ... tattoo-mastery
   progress:` debug log line, or the ritual station dialog's slot-usage label).
2. Activate Bloodrune once.
3. **Expected**: that pawn's tattoo-mastery progress increases by exactly one, the same way it would for
   Guardian's Call or Stormlash — confirming feature 006's gizmo-wrap hook needed zero Bloodrune-specific code to
   pick this tattoo up.

## Scenario 6 — Removal guard and save/reload persistence (Edge Cases / FR-014, FR-015, SC-007, SC-012)

1. With a Bloodrune-tattooed pawn at a known tier and progression-counter value, save and reload. **Expected**:
   tier and counter are unchanged immediately after reload compared to immediately before saving.
2. With Dev Mode enabled and **God Mode off**, confirm the tattoo hediff has no delete/remove control on the
   health tab (matches vanilla's own behavior, per feature 003's existing guard).
3. Enable **God Mode**, remove the hediff via the health tab. **Expected**: removal succeeds and the gizmo, any
   active heal burst, and any active pain reduction all stop immediately (FR-014).
4. Re-apply the tattoo, disable God Mode, and attempt a direct `pawn.health.RemoveHediff(...)` call on it (via a
   Dev Mode debug action or throwaway test patch, if available). **Expected**: the hediff remains — the removal
   call has no effect, no error logged (FR-015, reusing feature 003's existing guard with zero
   Bloodrune-specific configuration).

## Scenario 7 — No regressions, zero errors (Constitution Principle I/II)

1. Re-confirm Guardian's Call's and Stormlash's own gizmos, cooldowns, and tier progression are unaffected by
   this feature (a light re-check, not a full re-run of features 004/005's own quickstarts).
2. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with it
   present — this feature is expected to need no CE-specific behavior at all (research.md R4), so this is the
   standing "loads cleanly either way" check, not a CE-specific behavioral scenario like Stormlash's Scenario 5.

## Pass/fail

All seven scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to be
considered verified, per Constitution Principle II.
