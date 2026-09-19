# Quickstart: Validating the Bastion Ward Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run these
after implementation, before marking this feature complete. No automated test suite exists for this feature
(unchanged precedent from features 001–015 — `Verse`/`RimWorld` game types aren't practically runnable outside the
game process).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6.
- Dev Mode enabled (Options → Dev Mode); use Dev Mode's "Add hediff" tool to grant `TattooMagic_Hediff_BastionWard`
  directly to a test pawn for faster iteration than the ritual station.
- At least 2-3 colonist/ally pawns and 1-2 hostile pawns available and controllable via Dev Mode spawning, on a map
  with open ground for placing a ward.
- `TattooMagicSettings.EnableDebugLogging` enabled (per this project's proactive-debug-logging practice) to observe
  zone registration/expiry, absorb checks, heal ticks, and ejection decisions via `Log.Message`.
- A second RimWorld instance/save with Combat Extended loaded, for Scenario 6.

## Scenario 1 — Casting drops a fixed zone that fully negates melee and ranged damage (User Story 1 / SC-001, SC-002)

1. Apply Bastion Ward to a colonist. Select them. **Expected**: a "Bastion Ward" gizmo appears, enabled.
2. Click the gizmo. **Expected**: the cursor enters targeting mode with a radius ring preview at the cursor
   matching Tier 1's radius.
3. Hover a cell beyond Tier 1's max cast range (FR-001a). **Expected**: the cell is shown as unselectable with a
   "too far away"-style reason; clicking it does nothing (no zone spawns, no cooldown starts).
4. Hover a fogged/unexplored cell within range. **Expected**: shown as unselectable with a reason; clicking it does
   nothing.
5. Click a valid ground location, within range, with 1-2 ally pawns standing on it. **Expected**: the zone
   registers (debug log confirms), and it does not move even after the caster subsequently walks away. A
   translucent gold tint appears over exactly the ward's radius (FR-005a) — visually confirm the tinted cells
   match the radius the targeting preview showed in step 2, with no dev console/log needed to tell the ward is up.
6. Have a hostile melee-attack an ally standing inside the zone. **Expected**: the attack roll still happens (a
   hit/miss resolves normally) but the defender takes zero damage (debug log confirms `TryAbsorb` returned `true`);
   no wound appears.
7. Have a hostile ranged-attack (or Dev Mode "Damage" a specific instance on) an ally standing inside the zone.
   **Expected**: same full negation.
8. Move an ally to a cell clearly outside the zone's radius and repeat step 6/7 against them. **Expected**: normal,
   unreduced damage — confirming the negation is strictly zone-bound (SC-002).
9. Wait for the ward's duration to elapse. **Expected**: the zone is removed (debug log confirms expiry); a pawn
   who was previously protected and remains on that spot now takes normal damage; the gold tint disappears in the
   same instant, not lagging behind or lingering after protection actually ends (SC-010).

## Scenario 2 — Cooldown prevents re-casting (User Story 1 / SC-004)

1. Immediately after Scenario 1's cast, click the gizmo again. **Expected**: gizmo is disabled with a visible
   remaining-cooldown reason; no new zone spawns.
2. Wait for the cooldown to expire. **Expected**: gizmo becomes enabled again and a new cast succeeds.

## Scenario 3 — Casting on an already-melee'd ally ejects the attacker (User Story 2 / SC-003)

1. Provoke a hostile into melee combat with a colonist (let it close and start attacking).
2. While the hostile is actively meleeing that colonist, cast Bastion Ward centered so the colonist ends up inside
   the new zone's radius. **Expected**: the moment the zone registers, the attacking hostile is immediately
   repositioned outside the zone's radius (debug log confirms the ejection and the destination cell chosen); the
   hostile does not get one more "free" hit in from inside the zone.
3. Repeat with two hostiles simultaneously meleeing two different allies who both end up inside the same new zone.
   **Expected**: both hostiles are ejected, not just one.
4. Provoke a hostile into a **ranged** attack on an ally (keep it at range, not adjacent), then cast Bastion Ward
   around that ally. **Expected**: the ranged attacker is NOT displaced — only its subsequent shots are negated per
   Scenario 1.
5. Let an ejected hostile from step 2 walk back into the zone and attack an ally inside. **Expected**: it is not
   ejected a second time (the displacement is one-time, at the moment of casting only); its attack still deals zero
   damage per Scenario 1.
6. Build (or find) a walled-in nook with no free cell outside a would-be ward's radius, provoke a hostile into
   melee with a colonist inside it, then cast Bastion Ward so no valid displacement cell exists (debug log confirms
   `TryFindRandomCellNear` failing for that hostile). **Expected**: the hostile is left in its current position (no
   position/teleport change, no error/crash, cast completes normally); its own attacks against the ally still deal
   zero damage per Scenario 1, satisfying FR-006's fallback.

## Scenario 4 — Tier 2 upgrade: larger radius plus healing (User Story 3 / SC-005, SC-006)

1. With Bastion Ward at Tier 1 and its progression counter below threshold, activate the ability (a full successful
   cast, per Scenario 1) several times and confirm the counter increments by exactly one per activation (debug log
   or a temporary `Log.Message`).
2. Continue until the Tier 2 threshold is reached. **Expected**: the tattoo upgrades to Tier 2 in place — no new
   ritual, ingredient spend, or slot change.
3. Cast Bastion Ward again at Tier 2. **Expected**: the radius preview and the resulting zone are visibly larger
   than a Tier 1 zone's.
4. With an ally standing inside the Tier 2 zone and carrying an existing wound (Dev Mode "Damage" them first, not
   inside the zone, to leave a healable injury), wait through a few heal intervals. **Expected**: the wound's
   severity visibly decreases over time (debug log confirms periodic `HealWorstInjury` calls) in addition to the
   ally still taking zero damage from any attack.
5. Let a Tier 2 ward expire. **Expected**: both the damage negation and the healing stop applying immediately —
   identical to Tier 1's negation stopping at expiry.

## Scenario 5 — Edge cases: caster-independence, overlapping wards, faction changes (Edge Cases)

1. Cast a ward, then down or kill the caster (Dev Mode) while the ward is still active. **Expected**: the ward
   continues negating damage (and healing, if Tier 2) for allies inside it, unaffected by the caster's state.
2. With God Mode, remove the Bastion Ward hediff from the caster entirely while a ward they previously cast is
   still active. **Expected**: the gizmo disappears from that pawn and further casts are prevented, but the
   already-active ward is untouched and continues to its natural expiry (FR-020).
3. Cast two overlapping wards from two different Bastion-Ward-tattooed colonists (or the same one twice, if
   cooldown allows via Dev Mode). Stand an ally in the overlap. **Expected**: the ally is protected (one zone
   covering them is sufficient); if either overlapping ward is Tier 2, its own heal applies to anyone inside its
   own radius.
4. With an ally standing inside an active ward, trigger a mental break that turns them hostile (Dev Mode), or
   capture a hostile pawn standing inside a ward so it becomes a prisoner/colonist. **Expected**: protection
   updates immediately to match the pawn's new faction relationship — a now-hostile former ally stops being
   protected; a newly-friendly former hostile starts being protected — without needing to re-cast the ward.
5. Confirm a fire or explosion affecting a tile inside an active ward does NOT damage a pawn standing there — full
   invulnerability per spec Assumptions (session 2026-09-19 reversal; Bastion Ward negates all damage inside the
   ward, not attack-only).

## Scenario 6 — Save/reload persistence and Combat Extended (Edge Cases, SC-008, SC-009, research.md R5's open verification item)

1. With an active ward (known position, remaining duration, and tier-at-cast) and a Bastion-Ward-tattooed pawn at a
   known tier/progression-counter value, save and reload. **Expected**: the ward's position/radius/remaining
   duration/heal behavior, and the pawn's tier/counter, are all unchanged immediately after reload compared to
   immediately before saving.
2. Load Combat Extended. Repeat Scenario 1 (damage negation) with a CE-controlled hostile attacking an ally inside
   a ward, both melee and ranged. **Expected**: full negation holds identically to the non-CE case.
3. With CE loaded, repeat Scenario 3 (rescue-eject) with a CE hostile in melee combat with an ally. **Expected**:
   the hostile is still detected and ejected. If it is NOT (i.e. CE's own melee verb does not populate
   `mindState.meleeThreat` the way vanilla's does), this confirms research.md R5's flagged open question — apply
   the documented fallback detection (`enemyTarget == ally` + touch-range distance check) and re-verify.
4. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with it
   present, and that Guardian's Call/Wraithstep/every other triggered tattoo's own gizmo/cooldown/tier behavior is
   unaffected by this feature's changes to `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs` (a light re-check, not
   a full re-run of their own quickstarts).

## Pass/fail

All six scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to be
considered verified, per Constitution Principle II.
