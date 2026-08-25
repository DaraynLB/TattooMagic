# Quickstart: Validating the Berserker's Mark Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature
(research.md precedent from features 001–008).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode), God Mode available for Scenario 6.
- Feature 001 already built and working (the ritual station) — use it to apply Berserker's Mark, or use Dev
  Mode's "Add hediff" tool to grant `TattooMagic_Hediff_BerserkersMark` directly to a test pawn for faster
  iteration.
- Turn on the mod's debug-logging checkbox (Mod Options) for Scenarios 1 and 4 — `HediffComp_
  BerserkersMarkEffect` activation logging makes the tier/offset values easy to cross-check without opening the
  Health/Stats tabs for every step.
- A way to compare a pawn's melee damage, pain threshold, and armor rating stats — the vanilla Stats tab (right-
  click a pawn's portrait or open the Health/Character info panel) shows the current, live value of each,
  including the "Tattoo effects" breakdown line `StatPart_TattooEffectOffset` contributes to the tooltip.

## Scenario 1 — Gizmo, activation, and all three effects moving together (User Story 1 / SC-001, SC-002)

1. Apply Berserker's Mark to a colonist.
2. Select the colonist. **Expected**: a "Berserker's Mark" gizmo appears, enabled.
3. Open the Stats tab and note the colonist's current `Melee damage factor`, `Pain shock threshold`, and armor
   rating (Sharp/Blunt/Heat) values.
4. Activate the gizmo. **Expected**: all three stats change immediately — melee damage factor and pain shock
   threshold measurably increase, armor rating measurably decreases (each tooltip's "Tattoo effects" breakdown
   shows the contribution) — and the gizmo immediately shows disabled with a cooldown reason (remaining time
   visible in its tooltip/label).
5. Attempt to activate again while on cooldown. **Expected**: nothing happens (gizmo is disabled, not clickable)
   — no error in the debug log.
6. Let the window's duration fully elapse. **Expected**: all three stats return to their pre-activation values
   with no lingering offset.
7. Wait for the cooldown to expire. **Expected**: gizmo becomes enabled again.

## Scenario 2 — No effect without the tattoo (Edge Cases / FR-011)

1. Confirm an untattooed colonist (including one with other tattoos/hediffs) shows no Berserker's Mark gizmo and
   has unaffected melee damage, pain threshold, and armor rating.

## Scenario 3 — Progression counter and Tier 2's improved risk/reward (User Story 2 / SC-003, SC-004, SC-005, SC-006)

1. With Berserker's Mark at Tier 1, activate the ability repeatedly (waiting out cooldowns, or using Dev Mode to
   reduce/bypass cooldown for faster iteration) until the Tier 2 threshold is reached.
2. **Expected**: the progression counter increments by exactly one per activation; the tattoo upgrades to Tier 2
   in place with no new ritual, ingredient spend, or slot change.
3. Activate the ability at Tier 2 and compare all three stat changes against Scenario 1's Tier 1 result.
   **Expected**: the melee-damage-factor increase is measurably larger than Tier 1's, and the armor-rating
   decrease is measurably smaller in magnitude than Tier 1's — but still a genuine decrease, never zero.
4. Let a Tier 2 window elapse. **Expected**: all three stats return to normal, same as Tier 1.

## Scenario 4 — The pain-threshold-reversion edge case (Edge Cases)

1. Injure a Berserker's Mark-tattooed pawn (Dev Mode damage tool) until their pain level sits just above what
   their *normal* (unboosted) pain shock threshold would be, but below their boosted threshold.
2. Activate Berserker's Mark. **Expected**: the pawn remains conscious/active (their pain is now below the
   boosted threshold).
3. Let the window's duration elapse without letting the pawn's pain level drop further. **Expected**: the moment
   the window closes and the pain-threshold offset reverts, the pawn goes down from pain shock if their current
   pain is still above the normal threshold — this is the intended trade-off (Edge Cases), not a bug. If the
   pawn's pain had already dropped below the normal threshold by the time the window closed (e.g. via healing),
   confirm they simply stay up instead — either outcome is correct, driven entirely by vanilla's own pain-shock
   evaluation reacting to the reverted stat.

## Scenario 5 — Coexists correctly with the other four triggered tattoos, validating the reuse point for the final consumer (User Story 3 / SC-009, SC-010)

1. Apply Berserker's Mark alongside at least one other triggered tattoo (Guardian's Call, Stormlash, Bloodrune,
   or Wraithstep) on the same colonist (Dev Mode-grant is fine for this check).
2. Select the colonist. **Expected**: every applicable ability's gizmo appears independently.
3. Activate Berserker's Mark. **Expected**: the other tattoo(s)' gizmo/cooldown state is unaffected.
4. Activate one of the other triggered tattoos. **Expected**: Berserker's Mark's gizmo/cooldown state is
   unaffected.
5. If the other tattoo applied is Guardian's Call at Tier 2 (its own armor buff) or a future Ironskin Glyph,
   confirm the armor-rating Stats-tab tooltip shows both contributions summed together (a positive one and
   Berserker's Mark's negative one netting out), not one overriding the other — confirms FR-009's stacking
   guarantee live, not just by code review.
6. Confirm no code change was needed in any other tattoo's own `.cs` file, or in `Patch_Pawn_GetGizmos_
   TattooGizmos.cs` or `StatPart_TattooEffectOffset.cs`, to make this work — a code-review confirmation, not an
   in-game one.

## Scenario 6 — Berserker's Mark automatically feeds the tattoo-mastery XP track (User Story 3 / SC-011, feature 006 regression check)

1. Note the colonist's current tattoo-mastery progress (feature 006's `[TattooMagic] ... tattoo-mastery
   progress:` debug log line, or the ritual station dialog's slot-usage label).
2. Activate Berserker's Mark once.
3. **Expected**: that pawn's tattoo-mastery progress increases by exactly one, the same way it would for any
   other triggered tattoo — confirming feature 006's gizmo-wrap hook needed zero Berserker's Mark-specific code
   to pick this tattoo up.

## Scenario 7 — Removal guard and save/reload persistence (Edge Cases / FR-017, FR-018, SC-007, SC-012)

1. With a Berserker's Mark-tattooed pawn at a known tier and progression-counter value, save and reload.
   **Expected**: tier and counter are unchanged immediately after reload compared to immediately before saving.
2. With Dev Mode enabled and **God Mode off**, confirm the tattoo hediff has no delete/remove control on the
   health tab (matches vanilla's own behavior, per feature 003's existing guard).
3. Enable **God Mode**, remove the hediff via the health tab. **Expected**: removal succeeds and the gizmo, and
   any active melee-damage/pain-threshold/defense offsets, all stop immediately (FR-017).
4. Re-apply the tattoo, disable God Mode, and attempt a direct `pawn.health.RemoveHediff(...)` call on it (via a
   Dev Mode debug action or throwaway test patch, if available). **Expected**: the hediff remains — the removal
   call has no effect, no error logged (FR-018, reusing feature 003's existing guard with zero Berserker's
   Mark-specific configuration).

## Scenario 8 — No regressions, zero errors, CE-agnostic as predicted (Constitution Principle I/II)

1. Re-confirm at least one other triggered tattoo's own gizmo, cooldown, and tier progression is unaffected by
   this feature (a light re-check, not a full re-run of its own quickstart).
2. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with it
   present, and that all three of Berserker's Mark's effects (melee damage, pain threshold, defense) apply
   identically in both configurations — this feature is expected to need no CE-specific behavior at all
   (research.md R2/R3), so this is the standing "loads cleanly either way, and behaves the same either way"
   check, not a CE-specific behavioral scenario like Stormlash's.

## Pass/fail

All eight scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to be
considered verified, per Constitution Principle II.
