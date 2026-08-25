# Quickstart: Validating the Stormlash Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run
these after implementation, before marking this feature complete. No automated test suite exists for this
feature (research.md R7). **Both Scenario 4 (non-CE Frost Sigil immunity) and Scenario 5 (CE suppression-slow
immunity) are required, not optional** — they're the two concrete proofs behind this feature's Tier 2 "genuine
immunity" claim (research.md R4, R5), and the CE pass is also this feature's confirmation that
`IProvidesTattooGizmo`/`Patch_Pawn_GetGizmos_TattooGizmos` (feature 004) truly needs no CE-specific handling for
a second consumer.

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode), God Mode available for Scenario 7.
- Feature 001 already built and working (the ritual station) — use it to apply Stormlash, or use Dev Mode's
  "Add hediff" tool to grant `TattooMagic_Hediff_Stormlash` directly to a test pawn for faster iteration.
- For Scenario 4, also apply (or Dev-Mode-grant) Frost Sigil (`TattooMagic_Hediff_FrostSigil`) to a *second*,
  separate test pawn — Frost Sigil's slow is inflicted on whoever melee-attacks its wearer, so the Stormlash
  pawn needs to be the one throwing the punch.
- **Combat Extended installed and enabled** for Scenario 5 (package `CETeam.CombatExtended`). If genuinely
  unavailable, that gap must be called out explicitly rather than the feature being marked done regardless —
  this feature's own SC-001/SC-007 require the CE pass.

## Scenario 1 — Gizmo, activation, and cooldown, non-CE (User Story 1 / SC-001, SC-003)

1. With Combat Extended **disabled**, apply Stormlash to a colonist.
2. Select the colonist. **Expected**: a "Stormlash" gizmo appears, enabled.
3. Open the Stats tab and note the colonist's baseline `Move speed`, `Melee cooldown`, and `Ranged cooldown
   multiplier` values.
4. Activate the gizmo. **Expected**: `Move speed` measurably increases and both cooldown-factor stats measurably
   decrease (the "Tattoo effects" `StatPart` explanation line should mention the offset on each); the gizmo
   immediately shows disabled with a cooldown reason (remaining time visible in its tooltip/label).
5. Attempt to activate again while on cooldown. **Expected**: nothing happens (gizmo is disabled, not
   clickable) — no error in the debug log.
6. Wait for the boost duration to elapse. **Expected**: all three stats return to their pre-activation baseline,
   with the "Tattoo effects" explanation line gone or back to zero for Stormlash's contribution.
7. Wait for the cooldown to expire. **Expected**: gizmo becomes enabled again.

## Scenario 2 — No effect without the tattoo (Edge Cases / FR-010)

1. Confirm an untattooed colonist (including one with other tattoos/hediffs) shows no Stormlash gizmo and its
   `Move speed`/cooldown-factor stats are unaffected by anything Stormlash-related.

## Scenario 3 — Progression counter and Tier 2 upgrade, non-CE (User Story 2 / SC-004, SC-005, SC-006)

1. With Stormlash at Tier 1, activate the ability repeatedly (waiting out cooldowns, or using Dev Mode to
   reduce/bypass cooldown for faster iteration) until the Tier 2 threshold is reached.
2. **Expected**: the progression counter increments by exactly one per activation; the tattoo upgrades to Tier 2
   in place with no new ritual, ingredient spend, or slot change.
3. At Tier 2, activate the ability and compare the boosted `Move speed`/cooldown-factor values against
   Scenario 1's Tier 1 boosted values. **Expected**: Tier 2's boost is measurably larger.

## Scenario 4 — Tier 2 is genuinely immune to this mod's own slow effect, non-CE (User Story 2 / SC-007; research.md R5 — **required**)

1. With Combat Extended **disabled** and Stormlash at Tier 2, activate the boost, then have the Stormlash pawn
   melee-attack the separate Frost-Sigil-tattooed pawn from Prerequisites until Frost Sigil's slow proc lands on
   the Stormlash pawn (check the Stormlash pawn's hediff list for `TattooMagic_Hediff_FrostSigilSlow`; raise
   Frost Sigil's proc chance via Dev Mode/`TattooEffectValues` override for faster iteration if needed).
2. **Expected**: once `TattooMagic_Hediff_FrostSigilSlow` is present on the Stormlash pawn, its `Move speed`
   stat shows **no reduction** relative to its Stormlash-Tier-2-boosted-only value from Scenario 3 — the "Tattoo
   effects" explanation line should show only Stormlash's own positive offset, with Frost Sigil's negative
   contribution clamped out, not merely outweighed.
3. Let the Stormlash boost expire while the Frost Sigil slow hediff is still active. **Expected**: `Move speed`
   now drops to reflect Frost Sigil's slow normally (confirming the veto is scoped to the active boost window,
   not a permanent effect).

## Scenario 5 — Tier 2 is genuinely immune to Combat Extended's suppression-slow (User Story 2 / SC-007; research.md R4 — **required**)

1. Enable Combat Extended. With Stormlash at Tier 1 (not yet Tier 2), put the tattooed colonist under sustained
   suppressive fire from hostiles (drafted, holding position, not fleeing to cover) until CE's suppression
   causes crouch-walking (check `Move speed` on the Stats tab — it should show a reduced value with a CE
   "Crouch walking: x67%" explanation line).
2. **Expected (Tier 1 baseline)**: `Move speed` is reduced by suppression as normal — Tier 1 has no slow
   immunity.
3. Upgrade (or Dev-Mode-set) the same colonist to Tier 2, repeat the suppression scenario, and activate the
   Stormlash boost while under active suppression/crouch-walking.
4. **Expected**: `Move speed` shows **no** suppression-crouch-walking reduction for the duration of the boost —
   the CE "Crouch walking: x67%" explanation line should not appear (or should show no net effect) while the
   boost is active, even though the colonist remains suppressed by every other measure (mental-break risk,
   cover-seeking eligibility, sway, hunkering all continue to behave normally — only the movement-speed
   consequence is neutralized, per FR-007's scope).
5. Let the boost expire while suppression is still ongoing. **Expected**: the crouch-walking `Move speed`
   reduction reasserts itself immediately.
6. Confirm zero red Harmony/mod errors in the debug log throughout.

## Scenario 6 — Coexists correctly with Guardian's Call, validating the reuse point for a second consumer (User Story 3 / SC-010, SC-011)

1. Apply both Guardian's Call and Stormlash to the same colonist.
2. Select the colonist. **Expected**: both "Guardian's Call" and "Stormlash" gizmos appear independently.
3. Activate Stormlash. **Expected**: Guardian's Call's gizmo/cooldown state is unaffected (still shows its own,
   independent cooldown/availability).
4. Activate Guardian's Call. **Expected**: Stormlash's gizmo/cooldown state is unaffected.
5. Confirm no code change was needed in any feature-004 file (`HediffComp_GuardiansCallEffect.cs`,
   `Patch_Pawn_GetGizmos_TattooGizmos.cs`, `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs`) to
   make this work — a code-review confirmation, not an in-game one.

## Scenario 7 — Removal guard and save/reload persistence (Edge Cases / FR-016, FR-017, SC-008, SC-012)

1. With a Stormlash-tattooed pawn at a known tier and progression-counter value, save and reload. **Expected**:
   tier and counter are unchanged immediately after reload compared to immediately before saving.
2. With Dev Mode enabled and **God Mode off**, confirm the tattoo hediff has no delete/remove control on the
   health tab (matches vanilla's own behavior, per feature 003's existing guard).
3. Enable **God Mode**, remove the hediff via the health tab. **Expected**: removal succeeds and the gizmo, any
   active boost, and any active slow immunity all stop immediately (FR-016).
4. Re-apply the tattoo, disable God Mode, and attempt a direct `pawn.health.RemoveHediff(...)` call on it (via a
   Dev Mode debug action or throwaway test patch, if available). **Expected**: the hediff remains — the removal
   call has no effect, no error logged (FR-017, reusing feature 003's existing guard with zero
   Stormlash-specific configuration).

## Pass/fail

Scenarios 1–4, 6, and 7 must match their "Expected" outcome with zero red Harmony/mod errors for this feature's
non-CE behavior to be considered verified. Scenario 5 must **also** pass, confirming genuine (not partial)
suppression-slow immunity, before this feature can be marked done — per Constitution Principle I/II, a
vanilla-only pass is not sufficient for a feature whose Tier 2 claim explicitly includes CE behavior. If Combat
Extended is genuinely unavailable in the test environment, that gap must be called out explicitly rather than
the feature being marked done regardless.
