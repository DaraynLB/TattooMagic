# Quickstart: Validating the Ironskin Glyph Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature
(research.md R6).

**Testing-methodology caveat carried over from feature 011 (research.md R3)**: RimWorld Dev Mode's "Apply damage"
tool constructs damage with no instigator, which causes Combat Extended to skip its entire armor pipeline outright
for that specific hit (a hard bypass, confirmed by decompiling `ArmorUtilityCE.GetAfterArmorDamage`'s shared
entry-gate check — the same one feature 011's R9 found for its own Flame/Burn testing). Real Sharp/Blunt weapon and
melee damage always carries a `Weapon`/`Instigator` and is unaffected. This means the debug tool **cannot** validate
this feature's own CE damage-reduction claim on its own when Combat Extended is loaded — see Scenario 2 for what to
use instead.

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6. For Scenario 8's CE pass, Combat Extended must also be enabled in the mod list (not
  just present in the Workshop cache).
- Dev Mode enabled (Options → Dev Mode), God Mode available for Scenario 6.
- Feature 001 already built and working (the ritual station) — use it to apply Ironskin Glyph, or use Dev Mode's
  "Add hediff" tool to grant `TattooMagic_Hediff_IronskinGlyph` directly to a test pawn for faster iteration. **Do
  not use "Add hediff" to test damage-reaction behavior** — it injects a hediff directly and never touches
  `Pawn.TakeDamage`, so it exercises nothing this feature adds; it's only useful for granting the tattoo itself.
- A way to deal Sharp/Blunt damage to a pawn on demand: Dev Mode's built-in **"Apply damage" tool**
  (`Verse.DebugTools_Health.Options_ApplyDamage`) lists every `DamageDef` in the game by name — select e.g. `Bullet`
  or `Stab` (sharp) or `Blunt` (blunt), then click the target pawn to deal damage of that exact type via the real
  `Pawn.TakeDamage` → `PreApplyDamage` → `PostApplyDamage` pipeline, the same path this feature's effect hooks.
  Conveniently, this tool's own `DamageInfo` carries no instigator by default, which means every trial you run this
  way also exercises the "no pawn instigator" case for free. **Caveat (research.md R3)**: that same no-instigator
  property makes this tool unusable for Scenario 2's damage-reduction check specifically when Combat Extended is
  loaded — see that scenario for why and what to use instead.
- To reliably observe the Tier 2 full-negate roll (Scenario 4) without dozens of manual clicks: since
  `fullNegateChanceTier2` is a placeholder balance value anyway (PRD §9), temporarily edit it in
  `Defs/HediffDefs/Tattoos/IronskinGlyph.xml` to something high (e.g. `0.9`) for that one test pass, rebuild,
  confirm the behavior, then set it back to its shipped placeholder afterward.
- A way to inspect the pawn's "Armor - Sharp" and "Armor - Blunt" stats (the Health/Stats tab) and Ironskin Glyph's
  progression counter/tier (Dev Mode inspect panel).

## Scenario 1 — Tier 1 armor-rating bonus (User Story 1 / SC-001)

1. Apply Ironskin Glyph to a colonist.
2. Open the colonist's Stats tab and inspect "Armor - Sharp" and "Armor - Blunt".
3. **Expected**: both values are measurably higher than an otherwise-identical untattooed pawn's, with "Tattoo
   effects: +X" visible in each stat's explanation breakdown (reusing `StatPart_TattooEffectOffset.
   ExplanationPart`, feature 002).

## Scenario 2 — Tier 1 physical-damage reduction (User Story 1 / SC-002)

**CE caveat (research.md R3)**: with Combat Extended loaded, steps 2-3 below MUST use a real weapon attack or
melee hit (a colonist actually shooting/stabbing the test pawn, or an equivalent real combat event), not the Dev
Mode "Apply damage" tool — that tool's instigator-less `DamageInfo` triggers a complete CE armor-pipeline bypass
unrelated to whether Ironskin Glyph works, producing a false "no reduction" result regardless of the tattoo.
Without CE loaded, the debug tool is fine for this scenario (vanilla's `ArmorUtility.GetPostArmorDamage` has no
such instigator gate).

1. With Ironskin Glyph applied, confirm "Armor - Sharp"/"Armor - Blunt" are higher than an otherwise-identical
   untattooed pawn's (Scenario 1). Note: Combat Extended replaces the Stats tab's combat section with its own
   curated stat list and may not display these rows at all when CE is loaded — this is a UI limitation, not
   evidence the stat is missing; treat step 2's damage-comparison result as the authoritative check when CE is
   active.
2. Deal an identical Sharp (e.g. `Bullet` or `Stab`) damage instance to the tattooed colonist and to an untattooed
   control pawn, same amount, same real source — **not** two separately-triggered Dev Mode debug-tool clicks if
   CE is loaded, per the caveat above. Without CE, the debug tool is fine for this step. Repeat separately for
   Blunt damage (e.g. `Blunt`).
3. **Expected**: across repeated trials, the tattooed pawn takes measurably less average damage than the control
   pawn from otherwise-identical Sharp/Blunt hits (armor-roll variance means single trials can match by chance —
   compare trends over several hits, not one), for both damage categories.
4. Deal a non-physical damage instance (e.g. `Flame` or `Burn`) to the tattooed colonist — the debug tool is fine
   here regardless of CE, since this step only needs to confirm the *absence* of an effect, which an
   armor-pipeline bypass can't produce a false positive for.
5. **Expected**: no Ironskin Glyph-attributable reduction — the armor-rating offsets simply don't apply to a
   damage type outside their own armor category, which is vanilla's own existing behavior, not something this
   feature needs to special-case.

## Scenario 3 — Progression counter and automatic Tier 1 → Tier 2 upgrade (User Story 2 / SC-003)

1. With Ironskin Glyph applied, deal repeated Sharp/Blunt damage instances to the wearer (Dev Mode damage tool),
   inspecting the progression counter via Dev Mode as you go.
2. **Expected**: the counter increments by one per qualifying instance (any Sharp/Blunt hit that reaches the comp
   with a nonzero incoming amount — research.md R4); a damage instance already at zero amount before reaching the
   comp does not increment it; a non-physical instance (e.g. `Flame`) does not increment it either.
3. Continue until the Tier 2 threshold is reached.
4. **Expected**: Ironskin Glyph upgrades to Tier 2 automatically — no new ritual, no ingredient spend, no slot
   change.

## Scenario 4 — Tier 2 stronger reduction and full-negate chance (User Story 2 / SC-004)

1. With Ironskin Glyph at Tier 2, re-inspect "Armor - Sharp"/"Armor - Blunt" and confirm both are higher than
   their Tier 1 values from Scenario 1.
2. At Tier 1 (a fresh test pawn, or before reaching the threshold), deal a long series of Sharp/Blunt hits and
   confirm the pawn's health log never shows a hit reduced all the way to exactly zero damage *by Ironskin Glyph's
   own full-negate roll specifically* (ordinary armor deflection can still occasionally zero a hit on its own —
   the distinction here is that Ironskin Glyph's Tier 2-exclusive roll never fires at Tier 1, per FR-006, though
   vanilla armor's own inherent chance-to-deflect is a separate, pre-existing mechanic this feature doesn't
   suppress).
3. At Tier 2, deal a long series of Sharp/Blunt hits (enough trials for a low-probability event — dozens, given
   the small placeholder chance per research.md R7).
4. **Expected**: at least one instance is fully absorbed (zero damage, no wound at all), and the Dev Mode debug
   log (`TattooMagicSettings.EnableDebugLogging`) shows `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` reporting
   an absorbed instance for that pawn.

## Scenario 5 — No effect without the tattoo, and effect stops immediately on removal (Edge Cases / FR-008/FR-014)

1. Confirm an untattooed colonist (including one with other tattoos/hediffs) shows none of Ironskin Glyph's armor
   bonus, damage reduction, or full-negate behavior, regardless of physical-damage exposure.
2. With Ironskin Glyph applied and at a known tier/counter value, remove the hediff via Dev Mode/God Mode's health
   tab.
3. **Expected**: immediately after removal, the pawn's "Armor - Sharp"/"Armor - Blunt" stats revert to their
   pre-tattoo values, and no further full-negate behavior occurs on subsequent physical-damage exposure.

## Scenario 6 — Persistence across save/reload (Edge Cases / SC-005)

1. With an Ironskin Glyph-tattooed pawn at a known tier and progression-counter value, save and reload.
2. **Expected**: tier and counter are unchanged immediately after reload compared to immediately before saving,
   and the stat offsets (Scenarios 1/4) still apply correctly post-reload.

## Scenario 7 — Removal guard (FR-014, reusing feature 003's existing guard)

1. Apply Ironskin Glyph to a colonist. With Dev Mode enabled but **God Mode off**, confirm there is no
   delete/remove control for the tattoo hediff on the health tab.
2. Enable **God Mode**, remove the hediff via the health tab. **Expected**: removal succeeds and all of Ironskin
   Glyph's effects stop immediately (Scenario 5).
3. Re-apply the tattoo, disable God Mode, and attempt a direct `pawn.health.RemoveHediff(...)` call on it (via
   `DebugAction_TestTattooRemovalGuard` or an equivalent Dev Mode tool, per feature 003 precedent). **Expected**:
   the hediff remains — the removal call has no effect, no error logged.

## Scenario 8 — No regressions to existing on-hit/incoming-damage consumers; zero errors with Combat Extended
present (Constitution Principle I/II, User Story 3, SC-006)

1. With Ironskin Glyph, Ember Ward, Frost Sigil, Serpent's Eye, and Vampiric Thorn all applied to the same
   colonist (or across a small squad), confirm the other tattoos' own on-hit/incoming-damage behaviors are
   unaffected by Ironskin Glyph's own comp being dispatched through the shared
   `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` prefix alongside Ember Ward's — a light re-check, not a full
   re-run of features 002/003/010/011's own quickstarts.
2. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with it
   present, including a repeat of Scenarios 1, 3, and 4 under CE and a repeat of **Scenario 2 using a real weapon
   attack or melee hit, not the debug tool** (research.md R3's instigator-bypass caveat) to get a valid CE-loaded
   reading on the damage-reduction claim specifically.

## Pass/fail

All eight scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to be
considered verified, per Constitution Principle II. Scenario 8's CE pass is required for SC-006 (loads/behaves
correctly with CE present). Research.md R3 independently confirmed (by decompiling CE's own direct-hit code path,
not by assuming feature 011's ambient-path finding transfers) that Combat Extended's armor pipeline has the same
pawn-stat-reading gap for Sharp/Blunt damage that feature 011 found for Heat — the fix (a CE-gated direct
`dinfo.Amount` reduction) is applied the same way and is expected to need no further correction, but Scenario 8's
CE-loaded pass with real combat damage remains the authoritative verification, not an assumption.
