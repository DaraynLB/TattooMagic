# Quickstart: Validating the Guardian's Call Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run
these after implementation, before marking this feature complete. No automated test suite exists for this
feature (research.md R7). **The Combat Extended pass (Scenario 5) is not optional** — it is the second of the
two independently-verified paths Constitution Principle IV requires for any targeting-AI interception, and this
is the first tattoo in the mod that touches targeting AI at all.

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode), God Mode available for Scenario 6.
- A save/map where you can spawn multiple hostile pawns near a colonist (Dev Mode: spawn pawns / force a raid).
- Feature 001 already built and working (the ritual station) — use it to apply Guardian's Call, or use Dev
  Mode's "Add hediff" tool to grant `TattooMagic_Hediff_GuardiansCall` directly to a test pawn for faster
  iteration.
- **Combat Extended installed and enabled** for Scenario 5 (package `CETeam.CombatExtended`). If genuinely
  unavailable, that gap must be called out explicitly rather than the feature being marked done regardless —
  this feature's own SC-001/SC-010 require the CE pass.
- Before Scenario 5, temporarily add the `Log.Message` probes described in research.md R5 to
  `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt`'s postfix (per this project's proactive-
  debug-logging practice) so you can see in the dev console whether it fires for CE-controlled hostiles.
  Remove them again (or gate behind `Prefs.DevMode`) before considering the feature done.

## Scenario 1 — Gizmo, activation, and cooldown, non-CE (User Story 1 / SC-001, SC-003)

1. With Combat Extended **disabled**, apply Guardian's Call to a colonist.
2. Select the colonist. **Expected**: a "Guardian's Call" gizmo appears, enabled.
3. Spawn 2-3 hostile pawns within taunt range but not yet engaged, then activate the gizmo.
4. **Expected**: hostiles' current/next target selection redirects to the tattooed colonist; the gizmo
   immediately shows disabled with a cooldown reason (remaining time visible in its tooltip/label).
5. Attempt to activate again while on cooldown. **Expected**: nothing happens (gizmo is disabled, not
   clickable) — no error in the debug log.
6. Wait for the cooldown to expire. **Expected**: gizmo becomes enabled again.

## Scenario 2 — Taunt duration expiry and live re-evaluation (User Story 1 / SC-002; Edge Cases)

1. Activate Guardian's Call with hostiles nearby (as Scenario 1) and let the taunt duration fully elapse
   without the colonist being downed/killed.
2. **Expected**: once the duration ends, hostiles resume normal targeting (no continued bias toward the
   previously-taunted colonist) on their next targeting decision.
3. Separately: activate the ability, then introduce a *new* hostile that spawns/arrives within taunt range
   partway through the still-active duration.
4. **Expected**: the new arrival is also biased toward the tattooed colonist for the remainder of the
   duration — confirming the taunt is a live, standing check (research.md R4), not a one-time snapshot applied
   only to hostiles present at activation.

## Scenario 3 — Taunt ends immediately if the tattooed pawn is downed/killed/loses the hediff (Edge Cases)

1. Activate Guardian's Call, then down or kill the tattooed pawn (or, in Dev Mode with God Mode, remove the
   hediff) while the taunt duration is still active.
2. **Expected**: hostiles no longer favor that pawn on their next targeting decision — no lingering fixation
   on a downed/dead/no-longer-tattooed pawn, and no error in the debug log.

## Scenario 4 — Progression counter, Tier 2 upgrade, and armor buff, non-CE (User Story 2 / SC-004, SC-005, SC-006)

1. With Guardian's Call at Tier 1, activate the ability repeatedly (waiting out cooldowns, or using Dev Mode to
   reduce/bypass cooldown for faster iteration) until the Tier 2 threshold is reached.
2. **Expected**: the progression counter increments by exactly one per activation (not per hostile taunted);
   the tattoo upgrades to Tier 2 in place with no new ritual, ingredient spend, or slot change.
3. At Tier 2, open the colonist's Stats tab *before* activating. **Expected**: `Armor - Sharp`/`Armor - Blunt`/
   `Armor - Heat` show their normal (non-buffed) values.
4. Activate the ability. **Expected**: the same armor stats now show a measurably higher value (the "Tattoo
   effects" `StatPart` explanation line should mention the offset) for the duration of that activation, while
   the taunt itself still applies exactly as it did at Tier 1.
5. Wait for the duration to elapse. **Expected**: the armor stats return to their pre-buff values with no
   lingering offset — re-check the Stats tab explanation to confirm the "Tattoo effects" line is gone or back
   to zero for the Guardian's Call contribution.

## Scenario 5 — Taunt correctly redirects targeting under Combat Extended (User Story 1 / SC-001, SC-010 — **required**)

1. Enable Combat Extended. Apply Guardian's Call to a colonist (or grant the hediff directly).
2. Spawn CE-equipped hostile pawns (ranged and, separately, melee) within taunt range and activate the
   ability.
3. Watch the dev console for the `Log.Message` probes added per Prerequisites. **Expected**: the probe fires
   for the CE-controlled hostiles' targeting decisions, confirming
   `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt` is actually being reached under CE (research.md
   R5).
4. **Expected outcome A**: hostiles redirect to the tattooed colonist exactly as in Scenario 1 — this confirms
   research.md R5's "Outcome A" (the existing vanilla-path postfix already governs CE pawns); no second patch
   is needed, and this scenario alone satisfies Constitution Principle IV's "verified together when CE is
   loaded" requirement.
5. **If instead the probe does not fire for CE pawns, or they do not redirect**: this confirms research.md
   R5's "Outcome B" — implement `Patch_CE_AttackTargetFinder_GuardiansCallTaunt` per
   `contracts/two-path-targeting-contract.md` §2, then repeat this scenario until it passes. The feature is
   **not** done until one of the two outcomes is confirmed working in a CE-loaded game, not merely assumed from
   the vanilla pass.
6. Confirm zero red Harmony/mod errors in the debug log throughout, in both the ranged and melee cases.

## Scenario 6 — No effect without the tattoo; removal guard; save/reload persistence (edge cases / FR-009, FR-014, FR-015, SC-007, SC-011)

1. Confirm an untattooed colonist (including one with other tattoos/hediffs) shows no Guardian's Call gizmo and
   is never treated as a taunt source.
2. With a Guardian's Call-tattooed pawn at a known tier and progression-counter value, save and reload.
   **Expected**: tier and counter are unchanged immediately after reload compared to immediately before saving.
3. With Dev Mode enabled and **God Mode off**, confirm the tattoo hediff has no delete/remove control on the
   health tab (matches vanilla's own behavior, per feature 003's existing guard).
4. Enable **God Mode**, remove the hediff via the health tab. **Expected**: removal succeeds and the gizmo,
   any active taunt, and any active buff all stop immediately (FR-014).
5. Re-apply the tattoo, disable God Mode, and attempt a direct `pawn.health.RemoveHediff(...)` call on it (via
   a Dev Mode debug action or throwaway test patch, if available). **Expected**: the hediff remains — the
   removal call has no effect, no error logged (FR-015, reusing feature 003's existing guard with zero
   Guardian's-Call-specific configuration).

## Pass/fail

Scenarios 1–4 and 6 must match their "Expected" outcome with zero red Harmony/mod errors for this feature's
non-CE behavior to be considered verified. Scenario 5 must **also** pass, confirming one of its two documented
outcomes, before this feature can be marked done — per Constitution Principle IV, a single vanilla-only pass is
not sufficient for a targeting-AI feature. If Combat Extended is genuinely unavailable in the test environment,
that gap must be called out explicitly rather than the feature being marked done regardless.
