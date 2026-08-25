# Quickstart: Validating the Frost Sigil Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run
these after implementation, before marking this feature complete. No automated test suite exists for this
feature (see `research.md` R7).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode).
- A save/map with at least two colonists and access to Dev Mode tools (spawn items, set hediffs, force
  combat).
- Feature 001 already built and working (the ritual station) — use it to actually apply Frost Sigil, or use
  Dev Mode's "Add hediff" tool to grant `TattooMagic_Hediff_FrostSigil` directly to a test pawn if you want
  to skip the ritual for faster iteration.

## Scenario 1 — Passive cold resistance (User Story 1 / SC-001)

1. Apply Frost Sigil to a colonist (via the ritual station, or Dev Mode hediff-add).
2. Open the colonist's Stats tab and inspect **Comfortable Temperature Range** (backed by
   `ComfyTemperatureMin`); note the value.
3. Compare against an otherwise-identical colonist without Frost Sigil.
4. **Expected**: The tattooed colonist's comfortable-cold threshold is measurably lower (better cold
   tolerance) than the untattooed colonist's, with zero red errors in the debug log.

## Scenario 2 — On-hit slow proc (User Story 1 / SC-002)

1. With Frost Sigil applied to colonist A, trigger repeated melee attacks against them from another pawn
   (Dev Mode: spawn a hostile, or use `Tools → Damage` iteratively, or a caged/downed sparring partner if
   available — anything that produces real melee `DamageInfo` hits, not `Tools → Damage`'s direct HP
   subtraction which does not go through `Pawn.PostApplyDamage` with a populated `Weapon`).
2. Run at least 20–30 hits and track how often the attacker visibly slows (check the attacker's Stats tab
   for a temporary `MoveSpeed` reduction, or observe movement).
3. **Expected**: The attacker is slowed on a clear subset of hits — not 0%, not 100% — consistent with a
   real proc chance. No errors in the debug log on any hit, proc or not.

## Scenario 3 — Tier 1 → Tier 2 auto-upgrade (User Story 2 / SC-003, SC-004)

1. Continue triggering melee hits against the Frost Sigil-tattooed colonist (Scenario 2 setup) until enough
   procs have landed to reach the Tier 2 threshold (check `progressionCounter` via Dev Mode inspection if
   available, or just run a large batch of hits).
2. **Expected**: At the threshold, Frost Sigil upgrades to Tier 2 automatically — no new ritual, no
   ingredient spend, no slot change (recipient's applied-tattoo count is unchanged throughout).
3. Re-check the Stats tab: cold resistance and slow-proc rate/magnitude should both be visibly stronger than
   in Scenarios 1–2.
4. Continue triggering hits past the threshold and confirm the rare freeze (attacker briefly stunned) is
   observed over enough trials, and that no further tier change or error occurs once at Tier 2.

## Scenario 4 — Save/reload persistence (edge case / SC-005)

1. With a Frost Sigil-tattooed pawn at a known tier and `progressionCounter` value, save the game.
2. Reload the save.
3. **Expected**: Tier and counter are unchanged immediately after reload compared to immediately before
   saving.

## Scenario 5 — Non-melee damage does not trigger the effect (edge case)

1. Apply ranged, explosive, or fire damage to the Frost Sigil-tattooed pawn (e.g. shoot them, or use Dev
   Mode fire/explosion tools).
2. **Expected**: No slow proc on the damage source (there usually isn't an "attacker pawn" to slow anyway
   for explosions/fire), and the progression counter does not increment. No errors.

## Scenario 6 — Combat Extended loaded (research.md R2 — required if CE is available)

1. Repeat Scenarios 1–3 with Combat Extended enabled alongside this mod.
2. **Expected**: Identical outcomes to the non-CE scenarios — cold resistance applies, the slow proc still
   fires off CE-resolved melee hits, and the tier upgrade still occurs. This specifically confirms the
   `Pawn.PostApplyDamage` patch point (research.md R2) is not silently bypassed by CE's melee resolution, per
   Constitution Principle I.
3. If CE is not installed in your test environment, this scenario cannot be completed — note that
   explicitly rather than assuming pass, consistent with Constitution Principle II ("done" requires it to
   actually run, not be assumed).

## Pass/fail

Scenarios 1–5 must match their "Expected" outcome, with zero red Harmony/mod errors in the debug log
throughout, for this feature to be considered verified per Constitution Principle II. Scenario 6 must also
pass if Combat Extended is available in the test environment; if it genuinely isn't available, that gap must
be called out explicitly rather than silently skipped.
