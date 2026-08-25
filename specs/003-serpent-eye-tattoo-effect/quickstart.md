# Quickstart: Validating the Serpent's Eye Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature (see
`research.md` R6). Unlike Frost Sigil, **the Combat Extended pass below is not optional/best-effort** — this
feature's defining behaviors (FR-002, FR-006) only exist under CE, so Scenarios 4–6 are load-bearing, not a bonus
check.

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode).
- A save/map with at least two colonists, a ranged weapon, and access to Dev Mode tools (spawn items, set
  hediffs, force combat).
- Feature 001 already built and working (the ritual station) — use it to actually apply Serpent's Eye, or use
  Dev Mode's "Add hediff" tool to grant `TattooMagic_Hediff_SerpentsEye` directly to a test pawn for faster
  iteration.
- **Combat Extended installed and enabled** for Scenarios 4–6 (package `CETeam.CombatExtended`). If genuinely
  unavailable in your test environment, that gap must be called out explicitly per Constitution Principle II —
  but note this feature's own SC-008 treats the CE pass as required, not best-effort, so completion should
  normally wait for it rather than ship without it.

## Scenario 1 — Passive accuracy bonus, non-CE (User Story 1 / SC-001)

1. With Combat Extended **disabled**, apply Serpent's Eye to a colonist.
2. Open the colonist's Stats tab and inspect **Shooting Accuracy (Pawn)** (`ShootingAccuracyPawn`); note the
   value.
3. Compare against an otherwise-identical colonist without Serpent's Eye.
4. **Expected**: The tattooed colonist's shooting accuracy is measurably higher, with zero red errors in the
   debug log.

## Scenario 2 — Ranged-hit-landed counter, non-CE (User Story 2 / SC-003)

1. With Serpent's Eye applied to colonist A, have them fire on a target repeatedly (Dev Mode: spawn a hostile at
   range, or set up a turkey shoot) until you can distinguish hits from misses.
2. **Expected**: The progression counter (inspect via Dev Mode if available) increments only on shots that
   actually connect and deal damage — not on misses, not on shots fired *at* colonist A by someone else. No
   errors on any shot.
3. Continue until the Tier 2 threshold is reached.
4. **Expected**: Serpent's Eye upgrades to Tier 2 automatically — no new ritual, no ingredient spend, no slot
   change. Re-check the Stats tab: `ShootingAccuracyPawn` should be visibly higher than at Tier 1.

## Scenario 3 — No effect without the tattoo, and save/reload persistence (edge cases / SC-006)

1. Confirm an untattooed colonist shows none of Serpent's Eye's bonuses regardless of other tattoos/hediffs
   present (FR-008).
2. With a Serpent's Eye-tattooed pawn at a known tier and counter value, save and reload.
3. **Expected**: Tier and counter are unchanged immediately after reload compared to immediately before saving.

## Scenario 4 — Accuracy and sway/recoil, CE-loaded (User Story 1 / SC-001, SC-002 — **required**)

1. Enable Combat Extended. Apply Serpent's Eye to a colonist.
2. Inspect **Aiming Accuracy** (`AimingAccuracy`, CE-only) on the tattooed colonist vs. an untattooed one.
3. **Expected**: `AimingAccuracy` is measurably higher on the tattooed colonist — this is where CE's accuracy
   bonus actually lives (research.md R4), not `ShootingAccuracyPawn`.
4. Inspect **Weapon Handling** (CE's relabeled `ShootingAccuracyPawn` — same stat, different meaning under CE)
   on both colonists.
5. **Expected**: Weapon Handling is also measurably higher on the tattooed colonist (this is Serpent's Eye's
   sway/recoil reduction under CE, per research.md R4 — confirm the in-game description still reads as CE's
   sway/recoil formula, not vanilla's accuracy description, to make sure you're reading the right stat's
   current meaning).
6. Fire a CE-loaded weapon with and without Serpent's Eye applied and visually/numerically compare sway buildup
   during aiming, and recoil kick per shot, if CE's UI exposes either directly.

## Scenario 5 — Tier 2 CE ammo-effectiveness bonus (User Story 2 / SC-005 — **required**)

1. With Combat Extended enabled, get a Serpent's Eye-tattooed colonist to Tier 2 (repeat Scenario 2's landed-hit
   grind with CE loaded — this also re-confirms the ranged-hit-landed counter fires correctly under CE's own
   ballistics pipeline, per research.md R2's stated assumption).
2. Have the Tier 2 colonist fire a CE ammo-using weapon at a target and compare the damage dealt per shot (or
   the target's HP loss) against an otherwise-identical Tier 1 or untattooed colonist firing the *same* weapon
   loaded with the *same* ammo type.
3. **Expected**: The Tier 2 colonist's shots deal measurably more damage — the ammo-effectiveness bonus
   (research.md R5) applying only to that pawn's own fired projectiles.
4. Have a different, non-tattooed colonist fire the *same ammo stack/type* immediately afterward.
5. **Expected**: The non-tattooed colonist's damage is unaffected — confirming the bonus is scoped to the
   individual fired projectile instance and does not leak into the shared `AmmoDef`/weapon data (research.md
   R5's core safety claim). No errors in the debug log from the reflection-based patch in either case.

## Scenario 6 — CE absent: sway/recoil and ammo bonus cleanly no-op (edge case / SC-002, SC-005 — **required**)

1. With Combat Extended **disabled**, repeat a quick version of Scenario 1 (accuracy bonus present) and confirm
   there is no `AimingAccuracy` stat to inspect at all (CE-only, doesn't exist) and no crash/error from the mod
   attempting to resolve it.
2. Confirm the mod's startup log shows no attempt to patch `CombatExtended.ProjectileCE.Impact` (the reflection
   patch must not even try to resolve the CE type when CE is absent, per research.md R5's guard).
3. **Expected**: Zero red errors in the debug log at startup and throughout play — the CE-only portions of the
   effect are simply absent, not broken (Constitution Principle I).

## Scenario 7 — Tattoo hediff removal is blocked except via God Mode (FR-015 / SC-009 — required, non-CE)

1. Apply Serpent's Eye (or Frost Sigil) to a colonist. With Dev Mode enabled but **God Mode off**, open the
   colonist's health tab and confirm there is no delete/remove control shown for the tattoo hediff at all
   (this matches vanilla's own behavior — the control only appears under God Mode — so this step is mostly
   confirming nothing about this feature changed that).
2. Enable **God Mode** (Dev Mode → God mode). Reopen the health tab and use its delete control on the tattoo
   hediff.
3. **Expected**: The hediff is removed successfully, and its bonuses stop immediately (FR-014) — God Mode's
   override is not blocked.
4. Re-apply the tattoo. With God Mode back off, use a Dev Mode debug action (or a throwaway test patch/mod, if
   available in your environment) that calls `pawn.health.RemoveHediff(...)` directly on the tattoo hediff,
   simulating an unrelated mod attempting the same thing.
5. **Expected**: The hediff remains — the removal call has no effect, and no error appears in the debug log.

## Pass/fail

Scenarios 1–3 and 7 must match their "Expected" outcome with zero red Harmony/mod errors for this feature's
non-CE behavior to be considered verified. Scenarios 4–6 must **also** pass — per SC-008, this is the first
passive tattoo where the CE-loaded pass is required for the feature's own defining behavior, not an optional
compatibility check. If Combat Extended is genuinely unavailable in the test environment, that gap must be
called out explicitly rather than the feature being marked done regardless. Scenario 7 (FR-015) is required
regardless of CE's availability, since it's CE-independent.
