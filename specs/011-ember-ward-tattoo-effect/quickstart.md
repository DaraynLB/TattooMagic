# Quickstart: Validating the Ember Ward Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature
(research.md R7).

**Important correction found during live testing (research.md R9)**: RimWorld Dev Mode's "Apply damage" tool
constructs damage with no instigator, which causes Combat Extended to skip its entire armor pipeline outright
(a hard bypass, confirmed by decompiling `ArmorUtilityCE.GetAfterArmorDamage`) — **not** a small/negligible
effect, a complete skip, verified by testing an artificially huge `ArmorRating_Heat` value and seeing zero change
in damage dealt. This means the debug tool **cannot** validate Scenario 2's damage-reduction claim while CE is
loaded — it will show no reduction regardless of whether the underlying mechanism works, because CE never even
looks at the stat for this kind of hit. Real fire damage (`RimWorld.Fire.DoFireDamage`) and real weapon attacks
always carry a non-null instigator and are not affected by this bypass, so the debug tool remains valid for
vanilla-only (no CE) runs of Scenario 2, and for every other scenario in this file regardless of CE (none of the
others depend on CE's armor pipeline).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6. For Scenario 8's CE pass, Combat Extended must also be enabled in the mod list (not
  just present in the Workshop cache).
- Dev Mode enabled (Options → Dev Mode), God Mode available for Scenario 6.
- Feature 001 already built and working (the ritual station) — use it to apply Ember Ward, or use Dev Mode's
  "Add hediff" tool to grant `TattooMagic_Hediff_EmberWard` directly to a test pawn for faster iteration. **Do not
  use "Add hediff" to test damage-reaction behavior** — it injects a hediff directly and never touches
  `Pawn.TakeDamage`, so it exercises nothing this feature adds; it's only useful for granting the tattoo itself.
- A way to deal fire/burn damage to a pawn on demand: Dev Mode's built-in **"Apply damage" tool**
  (`Verse.DebugTools_Health.Options_ApplyDamage`, confirmed by decompiling RimWorld 1.6's `Assembly-CSharp.dll`)
  lists every `DamageDef` in the game by name — select `Flame` or `Burn`, then click the target pawn to deal 5
  damage of that exact type via the real `Pawn.TakeDamage` → `PreApplyDamage` → `PostApplyDamage` pipeline, the
  same path this feature's new patch hooks. Conveniently, this tool's own `DamageInfo` carries no instigator by
  default, which means every trial you run this way also exercises the "no pawn instigator" case (research.md R4
  — the reason the existing on-hit patch couldn't be reused) for free, without needing to arrange an actual
  environmental fire. **Caveat (research.md R9)**: that same no-instigator property makes this tool unusable for
  Scenario 2's damage-reduction check specifically when Combat Extended is loaded — see that scenario for why and
  what to use instead.
- To reliably observe the Tier 2 full-ignore roll (Scenario 4) without dozens of manual clicks: since
  `fullIgnoreChanceTier2` is a placeholder balance value anyway (PRD §9), temporarily edit it in
  `Defs/HediffDefs/Tattoos/EmberWard.xml` to something high (e.g. `0.9`) for that one test pass, rebuild, confirm
  the behavior, then set it back to its shipped placeholder afterward.
- A way to inspect the pawn's `ArmorRating - Heat` and `Comfortable Temperature Range` stats (the Health/Stats tab)
  and Ember Ward's progression counter/tier (Dev Mode inspect panel).

## Scenario 1 — Tier 1 heat resistance (User Story 1 / SC-001)

1. Apply Ember Ward to a colonist.
2. Open the colonist's Stats tab and inspect "Comfortable Temperature Range" (max end).
3. **Expected**: the max comfortable temperature is measurably higher than an otherwise-identical untattooed
   pawn's, with "Tattoo effects: +X" visible in the stat's explanation breakdown (reusing
   `StatPart_TattooEffectOffset.ExplanationPart`, feature 002).

## Scenario 2 — Tier 1 burn-damage reduction (User Story 1 / SC-002)

**CE caveat (research.md R9)**: with Combat Extended loaded, steps 2-3 below MUST use real fire or a real
weapon attack, not the Dev Mode "Apply damage" tool — that tool's instigator-less `DamageInfo` triggers a
complete CE armor-pipeline bypass unrelated to whether Ember Ward works, producing a false "no reduction" result
regardless of the tattoo. Without CE loaded, the debug tool is fine for this scenario (vanilla's
`ArmorUtility.GetPostArmorDamage`, research.md R3, has no such instigator gate).

1. With Ember Ward applied, inspect the colonist's "Armor - Heat" stat and confirm it's higher than an
   otherwise-identical untattooed pawn's, with the same "Tattoo effects: +X" explanation line. Note: Combat
   Extended replaces the Stats tab's combat section with its own curated stat list and may not display
   `Armor - Heat` as a row at all when CE is loaded — this is a UI limitation, not evidence the stat is
   missing; treat step 2's damage-comparison result as the authoritative check when CE is active.
2. Deal an identical `Flame` or `Burn` damage instance to the tattooed colonist and to an untattooed control
   pawn, same amount, same real source (both from the same fire, or both from the same attacking weapon —
   **not** two separately-triggered Dev Mode debug-tool clicks if CE is loaded, per the caveat above). Without
   CE, the debug tool is fine for this step.
3. **Expected**: across repeated trials, the tattooed pawn takes measurably less average damage than the control
   pawn from otherwise-identical fire/burn hits (armor-roll variance means single trials can match by chance —
   compare trends over several hits, not one).
4. Deal a non-fire/burn damage instance (e.g. `Blunt`) to the tattooed colonist — the debug tool is fine here
   regardless of CE, since this step only needs to confirm the *absence* of an effect, which an armor-pipeline
   bypass can't produce a false positive for.
5. **Expected**: no Ember Ward-attributable reduction — the "Armor - Heat" offset simply doesn't apply to a
   damage type outside its own armor category, which is vanilla's own existing behavior, not something this
   feature needs to special-case.

## Scenario 3 — Progression counter and automatic Tier 1 → Tier 2 upgrade (User Story 2 / SC-003)

1. With Ember Ward applied, deal repeated `Flame`/`Burn` damage instances to the wearer (Dev Mode damage tool),
   inspecting the progression counter via Dev Mode as you go.
2. **Expected**: the counter increments by one per qualifying instance (any fire/burn hit that reaches the comp
   with a nonzero incoming amount — research.md R6); a damage instance already at zero amount before reaching the
   comp does not increment it.
3. Continue until the Tier 2 threshold is reached.
4. **Expected**: Ember Ward upgrades to Tier 2 automatically — no new ritual, no ingredient spend, no slot change.

## Scenario 4 — Tier 2 stronger reduction and full-ignore chance (User Story 2 / SC-004)

1. With Ember Ward at Tier 2, re-inspect "Comfortable Temperature Range" and "Armor - Heat" and confirm both are
   higher than their Tier 1 values from Scenarios 1–2.
2. At Tier 1 (a fresh test pawn, or before reaching the threshold), deal a long series of `Flame`/`Burn` hits and
   confirm the pawn's health log never shows a hit reduced all the way to exactly zero damage *by Ember Ward's own
   full-ignore roll specifically* (ordinary armor deflection can still occasionally zero a hit on its own — the
   distinction here is that Ember Ward's Tier 2-exclusive roll never fires at Tier 1, per FR-006, though vanilla
   armor's own inherent chance-to-deflect is a separate, pre-existing mechanic this feature doesn't suppress).
3. At Tier 2, deal a long series of `Flame`/`Burn` hits (enough trials for a low-probability event — dozens, given
   the small placeholder chance per research.md R8).
4. **Expected**: at least one instance is fully absorbed (zero damage, no wound at all), and the Dev Mode debug
   log (`TattooMagicSettings.EnableDebugLogging`) shows `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` reporting
   an absorbed instance for that pawn.

## Scenario 5 — No effect without the tattoo, and effect stops immediately on removal (Edge Cases / FR-008/FR-014)

1. Confirm an untattooed colonist (including one with other tattoos/hediffs) shows none of Ember Ward's heat
   resistance, armor offset, or full-ignore behavior, regardless of fire exposure.
2. With Ember Ward applied and at a known tier/counter value, remove the hediff via Dev Mode/God Mode's health
   tab.
3. **Expected**: immediately after removal, the pawn's "Comfortable Temperature Range" and "Armor - Heat" stats
   revert to their pre-tattoo values, and no further full-ignore behavior occurs on subsequent fire exposure.

## Scenario 6 — Persistence across save/reload (Edge Cases / SC-005)

1. With an Ember Ward-tattooed pawn at a known tier and progression-counter value, save and reload.
2. **Expected**: tier and counter are unchanged immediately after reload compared to immediately before saving,
   and the stat offsets (Scenarios 1/2/4) still apply correctly post-reload.

## Scenario 7 — Removal guard (FR-014, reusing feature 003's existing guard)

1. Apply Ember Ward to a colonist. With Dev Mode enabled but **God Mode off**, confirm there is no delete/remove
   control for the tattoo hediff on the health tab.
2. Enable **God Mode**, remove the hediff via the health tab. **Expected**: removal succeeds and all of Ember
   Ward's effects stop immediately (Scenario 5).
3. Re-apply the tattoo, disable God Mode, and attempt a direct `pawn.health.RemoveHediff(...)` call on it (via
   `DebugAction_TestTattooRemovalGuard` or an equivalent Dev Mode tool, per feature 003 precedent). **Expected**:
   the hediff remains — the removal call has no effect, no error logged.

## Scenario 8 — No regressions to existing on-hit consumers; zero errors with Combat Extended present (Constitution Principle I/II, User Story 3, SC-006)

1. With Ember Ward, Frost Sigil, Serpent's Eye, and Vampiric Thorn all applied to the same colonist (or across a
   small squad), confirm Frost Sigil's/Serpent's Eye's/Vampiric Thorn's own melee/ranged on-hit behaviors are
   unaffected by this feature's new `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` prefix — a light re-check, not
   a full re-run of features 002/003/010's own quickstarts.
2. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with it
   present, including a repeat of Scenarios 1, 3, and 4 under CE (all confirmed CE-safe live — research.md R9's
   debug-log evidence) and a repeat of **Scenario 2 using real fire or a real weapon attack, not the debug tool**
   (research.md R9's instigator-bypass caveat) to get a valid CE-loaded reading on the ongoing damage-reduction
   claim specifically.

## Pass/fail

All eight scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to be
considered verified, per Constitution Principle II. Scenario 8's CE pass is required for SC-006 (loads/behaves
correctly with CE present). Research.md R3 established CE's armor pipeline resolves `ArmorRating_Heat` the same
way vanilla does *when that pipeline actually runs*; R9 found live that it doesn't run at all for
instigator-less debug-tool damage, which is a testing-methodology caveat (Scenario 2 must use real fire/weapon
damage under CE), not a defect in the feature itself — the stat offset was directly confirmed correct and live
via debug logging (R9) independent of this gap.
