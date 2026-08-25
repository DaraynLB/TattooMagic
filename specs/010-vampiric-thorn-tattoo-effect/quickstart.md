# Quickstart: Validating the Vampiric Thorn Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature
(research.md R7). Unlike Serpent's Eye, this feature's defining behavior does **not** depend on Combat Extended
(research.md R6) — the CE-loaded pass below is the standing "loads cleanly and behaves correctly either way"
check, not a load-bearing behavioral scenario.

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode), God Mode available for Scenario 6.
- Feature 001 already built and working (the ritual station) — use it to apply Vampiric Thorn, or use Dev Mode's
  "Add hediff" tool to grant `TattooMagic_Hediff_VampiricThorn` directly to a test pawn for faster iteration.
- A way to injure pawns on demand (Dev Mode damage tools, or a real spar/fight) — the lifesteal has nothing to
  heal unless the wearer already has an open wound.
- A way to force a landed melee hit and, separately, a killing blow — Dev Mode's damage tools on a low-HP hostile
  or animal work well for reliably triggering a kill on demand.

## Scenario 1 — Tier 1 per-hit lifesteal (User Story 1 / SC-001)

1. Apply Vampiric Thorn to a colonist, then injure that same colonist (Dev Mode damage tool or real combat) so
   there's something for the lifesteal to heal.
2. Note the colonist's worst open wound's severity on the Health tab.
3. Have the colonist land a melee hit on another pawn (spar, Dev Mode-forced attack, or real combat) that deals
   damage.
4. **Expected**: immediately after the hit resolves, the colonist's own worst open wound's severity measurably
   decreases. No error in the debug log.

## Scenario 2 — No effect on a miss, a fully-blocked hit, or when struck instead of striking (Edge Cases / FR-009)

1. With Vampiric Thorn applied and the wearer injured, have the wearer's melee attack miss or be fully
   deflected/blocked (zero damage dealt) — repeat attacks against a well-armored target until one is observed, or
   use Dev Mode to confirm a zero-damage hit.
2. **Expected**: no change to the wearer's wound severity, and the progression counter (Scenario 4) does not
   increment for that swing.
3. Have a *different* pawn land a melee hit on the Vampiric-Thorn-tattooed pawn instead.
4. **Expected**: no lifesteal healing occurs on the tattooed pawn from being struck (FR-009) — Vampiric Thorn only
   reacts to the wearer's own landed attacks, not attacks landed on the wearer.

## Scenario 3 — No effect when uninjured, and no effect without the tattoo (Edge Cases / User Story 1 Acceptance Scenario 2, FR-008)

1. With Vampiric Thorn applied to a colonist currently at full health with no open injuries, have them land a
   melee hit that deals damage.
2. **Expected**: no error in the debug log, and no observable change (nothing to heal) — this is expected
   behavior, not a bug.
3. Confirm an untattooed colonist (including one with other tattoos/hediffs) shows none of Vampiric Thorn's
   lifesteal behavior regardless of how much melee combat they're in (FR-008).

## Scenario 4 — Progression counter and automatic Tier 1 → Tier 2 upgrade (User Story 2 / SC-002, SC-003)

1. With Vampiric Thorn applied, have the wearer land repeated melee hits that deal damage (spar or real combat),
   inspecting the progression counter via Dev Mode as you go.
2. **Expected**: the counter increments by exactly one per landed, damage-dealing hit — not on misses, not on
   fully-blocked hits, not on hits landed by other pawns on the wearer.
3. Continue until the Tier 2 threshold is reached.
4. **Expected**: Vampiric Thorn upgrades to Tier 2 automatically — no new ritual, no ingredient spend, no slot
   change.
5. Injure the Tier 2 wearer again and have them land another melee hit, comparing the resulting heal against
   Scenario 1's Tier 1 result on an otherwise-identical wound.
6. **Expected**: the Tier 2 per-hit lifesteal is measurably larger than Tier 1's (SC-004).

## Scenario 5 — Tier 2 post-kill recovery window (User Story 2 / SC-005)

1. With Vampiric Thorn at Tier 2 and the wearer injured, have the wearer land a melee killing blow on a target
   (Dev Mode: reduce a hostile/animal's HP low enough that the wearer's next hit kills it).
2. **Expected**: the immediate per-hit lifesteal applies as usual (Scenario 1 shape), **and** over the following
   several seconds the wearer's wound continues healing further, beyond that single hit's own lifesteal amount.
3. Repeat with Vampiric Thorn still at **Tier 1** (a fresh test pawn, or before reaching the threshold).
4. **Expected**: only the ordinary Tier 1 per-hit lifesteal applies to the killing blow — no extra post-kill
   healing window (FR-005's Tier 1 exclusion).
5. Back at Tier 2, land a second killing blow while an earlier kill's recovery window is still running (chain two
   kills in quick succession).
6. **Expected**: the window's remaining duration/heal budget resets to a fresh window rather than two windows
   running (healing) simultaneously — observable as the total extra healing not exceeding what one window's worth
   should produce, and the window's end point visibly extending from the second kill rather than the first
   (FR-006).

## Scenario 6 — Persistence across save/reload (Edge Cases / SC-006)

1. With a Vampiric Thorn-tattooed pawn at a known tier and progression-counter value, save and reload.
2. **Expected**: tier and counter are unchanged immediately after reload compared to immediately before saving.
3. Repeat with a Tier 2 pawn mid-post-kill-recovery-window (land a killing blow, then save/reload before the
   window closes).
4. **Expected**: the post-kill recovery window is still active after reload, for approximately its remaining
   duration, and continues healing — not reset, not doubled, not lost.

## Scenario 7 — Removal guard (FR-015 / SC-009, reusing feature 003's existing guard)

1. Apply Vampiric Thorn to a colonist. With Dev Mode enabled but **God Mode off**, confirm there is no delete/
   remove control for the tattoo hediff on the health tab.
2. Enable **God Mode**, remove the hediff via the health tab. **Expected**: removal succeeds and all of Vampiric
   Thorn's effects (per-hit lifesteal, any active post-kill window) stop immediately.
3. Re-apply the tattoo, disable God Mode, and attempt a direct `pawn.health.RemoveHediff(...)` call on it (via
   `DebugAction_TestTattooRemovalGuard` or an equivalent Dev Mode tool). **Expected**: the hediff remains — the
   removal call has no effect, no error logged. This exercises the existing cross-tattoo guard with zero
   Vampiric-Thorn-specific configuration, per feature 003 FR-015.

## Scenario 8 — No regressions to existing on-hit consumers, zero errors either way with Combat Extended (Constitution Principle I/II, User Story 3)

1. With Vampiric Thorn, Frost Sigil, and Serpent's Eye all applied to the same colonist (or across a small squad),
   confirm Frost Sigil's melee-taken slow-on-hit and Serpent's Eye's ranged-hit-landed accuracy/counter behaviors
   are both unaffected by this feature's new dispatch clause in `Patch_Pawn_PostApplyDamage_TattooOnHit` — a light
   re-check, not a full re-run of features 002/003's own quickstarts.
2. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with it
   present (research.md R6 — no CE-specific behavior is expected from this feature at all), including a repeat of
   Scenario 1 under CE to confirm the melee-hit-landed dispatch and the lifesteal heal both still work correctly
   when CE's own melee/health interactions are active.

## Pass/fail

All eight scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to be
considered verified, per Constitution Principle II. Scenario 8's CE pass is required for SC-008 (loads/behaves
correctly with CE present) but, per research.md R6, is not expected to surface any CE-specific behavioral
difference — a clean pass confirms the "no CE branching needed" finding rather than exercising a CE-only code
path the way Serpent's Eye's or Ironskin Glyph's own CE scenarios do.
