# Feature Specification: Bastion Ward Tattoo Effect

**Feature Branch**: `016-bastion-ward-tattoo-effect`

**Created**: 2026-09-18

**Status**: Draft

**Input**: User description: "Bastion Ward tattoo effect (016-bastion-ward-tattoo-effect). Source of truth: docs/proposals/2026-09-12-bastion-ward-tattoo.md (customer-approved, 2026-09-18). Summary: a two-tier triggered tattoo. Cast it and it drops a ground-fixed protective zone (does NOT follow the caster) that stays in place until it expires. Any ally standing inside takes zero damage from both melee and ranged attacks for the ward's duration (full negation, not a depleting HP shield pool — unlike the existing Guardian's Call shield wall). Placeholder duration ~8 seconds, XML-tunable. On cast, any enemy currently in melee combat with an ally inside the radius is forcibly ejected/knocked out of the zone. The caster needs a cooldown before recasting (placeholder value, XML-tunable) so it can't be chained back-to-back. Tier 2: larger radius, plus a small healing buff applied to allies standing inside on top of the damage negation. This is the first of a batch of 4 newly approved tattoos (Bastion Ward, Soul Tether, Stillshroud, Thunderclap Sigil) to be built, per docs/PRD.md §10 and the project's build order. Follow the same conventions as the existing 15 tattoo specs in specs/ (e.g. specs/004-guardians-call-tattoo-effect for the triggered gizmo/cooldown/progression-counter pattern, specs/015-shedscale-tattoo-effect for structure) and the mod's established two-tier tattoo model described in docs/PRD.md §5.4."

## Clarifications

### Session 2026-09-18

- Q: Should there be a maximum distance from the caster within which the player can place the ward, or can it be cast anywhere on the map? → A: Capped, XML-tunable range — mirrors Wraithstep's own per-tier range convention.
- Q: If no valid, walkable cell exists outside the new ward's radius to eject an attacking hostile to (e.g. it's boxed in by walls or clutter), what should happen to that hostile? → A: Leave it in place, un-ejected — it is still fully protected against via the damage negation either way; no crash risk, no far-away teleport surprise.
- Q: Should there be an in-game visual indicator showing which ground is currently warded, beyond the debug log? → A: Yes — a tinted ground overlay covering the ward's actual protected cells, using vanilla's own field-highlighting look (no new art asset needed); added as FR-005a after the original spec omitted any visual requirement.
- Q: After live playtesting showed an ejected hostile freely walking back into an active ward, should the ward keep repelling hostiles for its whole duration, or stay a one-time shove at the moment of casting (the original design)? → A: Considered a continuous repel, then reverted back to the original one-time shove at the moment of casting — a hostile may freely walk back in afterward; the damage negation (FR-003) alone is enough, since it means the hostile can never actually land a hit either way.

### Session 2026-09-19

- Q: Live testing of FR-006's rescue-eject against multi-attacker brawls (6+ simultaneous hostiles) needed two rounds of detection-signal fixes (`mindState.meleeThreat`, then an `enemyTarget` + touch-range fallback) to reliably catch the whole scrum, making it by far the hardest-to-verify part of this feature. Given that complexity, should the rescue-eject ship as mandatory core behavior, or as an opt-in toggle? → A: Opt-in toggle, off by default. First built as a mod-wide `TattooMagicSettings` checkbox, then moved (same session) to an XML-tunable field instead — `HediffCompProperties_BastionWardEffect.enableRescueEject`, default `false` in `Defs/HediffDefs/Tattoos/BastionWard.xml` — matching every other per-tattoo knob on this comp rather than a separate settings UI. The ward's core targeting/zone/damage-negation/tiering-and-healing behavior (User Stories 1 and 3) is complete and independently valuable regardless; FR-006/FR-007/FR-008 remain accurate descriptions of the behavior *when the field is `true`*.
- Q: Should Bastion Ward's damage negation extend to environmental/explosive damage (fire, an explosion), or stay attack-damage-only as originally scoped? → A: Full invulnerability — extend to all damage, including environmental/explosive. Discovered mid-session that the actual implementation (`GameComponent_BastionWardZones.TryAbsorb`) had never filtered by damage type at all (it would have already absorbed fire/explosion damage same as an attack); a fix to add that filtering was drafted but not yet shipped when the customer confirmed the broader, no-filtering behavior is what's actually wanted — the fix was reverted rather than applied, so no code change was needed for this outcome, only the spec catching up to what already worked. This is closer to Stillshroud's own explicit full invulnerability than this feature's original scoping.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Dropping a safe zone stops incoming damage cold (Priority: P1)

A player applies the Bastion Ward tattoo to a colonist (via the existing ritual station). In combat, that colonist has an activatable ability (a gizmo, like Guardian's Call's). Activating it drops a fixed protective zone on the ground at a targeted location. Any ally-faction pawn standing inside that zone takes zero damage from melee or ranged attacks — not reduced damage, not a depleting shield pool that eventually breaks, but complete negation — for as long as the ward holds. The zone stays exactly where it was cast; it does not follow the caster or anyone else around.

**Why this priority**: This is the entire point of the tattoo and the specific effect the customer asked for — a "make a stand here" tool that turns one spot into genuine safety. Every other story in this spec depends on this core zone-and-negation behavior existing first.

**Independent Test**: Can be fully tested by applying Bastion Ward to a colonist, activating the ability at a location with allies standing on it, and confirming those allies take zero damage from both melee and ranged attacks for the ward's duration while a pawn standing just outside the zone still takes normal damage — in both a non-CE game and a CE-loaded game.

**Acceptance Scenarios**:

1. **Given** a pawn with Bastion Ward applied, **When** the player activates the ability targeting a location with allies nearby, **Then** a protective zone appears fixed at that location and remains there without moving, even if the caster subsequently moves away.
2. **Given** an ally-faction pawn is standing inside an active ward, **When** a hostile pawn attacks them with a melee weapon, **Then** the attack resolves as normal (hit/miss roll still happens) but deals zero damage to the defender.
3. **Given** an ally-faction pawn is standing inside an active ward, **When** a hostile pawn attacks them with a ranged weapon, **Then** the same full negation applies — zero damage gets through.
4. **Given** an ally-faction pawn is standing just outside the ward's radius, **When** they are attacked, **Then** they take normal damage — the ward only protects pawns actually inside its radius.
5. **Given** the ward's duration has elapsed, **When** it expires, **Then** the zone disappears and any pawns previously inside it are once again fully vulnerable to damage.
6. **Given** Combat Extended is loaded, **When** an ally inside an active ward is attacked, **Then** the damage negation still applies correctly, verified independently of the non-CE code path (per Constitution Principle I).

---

### User Story 2 - Casting the ward rescues allies already being meleed (Priority: P2)

A fight is already underway and a colonist is being meleed by one or more hostiles. The player activates Bastion Ward centered on that colonist. The moment the ward goes up, any hostile currently in melee with an ally inside the new zone is forcibly shoved out of it — so the ward doesn't just fail to stop an attack that was already mid-swing, it actively clears the zone of the attacker at the moment it forms.

**Why this priority**: Without this, a player dropping the ward on an ally who's already being meleed would still watch that attacker get one more "free" hit in from inside the supposedly safe zone before the negation and positioning catch up — undermining the whole "cast it to make a stand" use case the tattoo exists for. It's a smaller, secondary slice on top of User Story 1's core negation.

**Independent Test**: Can be tested by provoking a hostile into melee combat with a colonist, then activating Bastion Ward centered on that colonist, and confirming the attacking hostile is immediately displaced outside the ward's radius at the moment of casting.

**Acceptance Scenarios**:

1. **Given** a hostile pawn is currently in melee combat with an ally-faction pawn, **When** the player casts Bastion Ward such that the ally ends up inside the new ward's radius, **Then** the attacking hostile is immediately displaced to a position outside the ward's radius as part of the cast.
2. **Given** multiple hostiles are simultaneously meleeing allies who all end up inside the new ward's radius, **When** the ward is cast, **Then** every one of those hostiles is displaced out of the zone, not just one.
3. **Given** a hostile is attacking an ally at range (not in melee) at the moment of casting, **When** the ward goes up around that ally, **Then** the ranged attacker is not forcibly displaced — only melee attackers actively engaged at the moment of casting are ejected; the ranged attacker's subsequent shots are simply negated per User Story 1.
4. **Given** an ejected hostile chooses to walk back into the ward's radius after being displaced, **When** it re-enters and attacks an ally inside, **Then** it is not ejected a second time — the displacement is a one-time reaction to the moment of casting, not a standing barrier that continuously excludes enemies — but its attacks still deal zero damage per User Story 1.

---

### User Story 3 - Bastion Ward grows stronger the more it's used (Priority: P3)

As a Bastion Ward-tattooed colonist keeps activating the ability in combat, the tattoo tracks how many times it's been used and automatically upgrades itself to a stronger Tier 2 version once enough uses have accumulated — with no extra ritual, ingredients, or tattoo slot required. At Tier 2, the ward covers a larger area and also gives allies standing inside it a small ongoing heal on top of the unchanged damage negation.

**Why this priority**: This is the PRD §5.4 tiering mechanic every triggered tattoo in the roster follows. It only matters once Tier 1's core negation (User Story 1) and rescue-eject behavior (User Story 2) already work, so it's the natural third slice.

**Independent Test**: Can be tested independently by activating a Bastion Ward-tattooed pawn's ability enough times to reach the threshold, then confirming the ward's radius is measurably larger on the next cast and allies standing inside a subsequent ward visibly regain health over time in addition to taking zero damage.

**Acceptance Scenarios**:

1. **Given** Tier 1 Bastion Ward applied with its progression counter below threshold, **When** the player activates the ability, **Then** the counter increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering activation resolves, **Then** Bastion Ward upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to the pawn's slot usage.
3. **Given** Bastion Ward is at Tier 2, **When** the wearer activates the ability, **Then** the resulting ward's radius is larger than a Tier 1 ward's, and allies standing inside it receive a small healing effect in addition to the unchanged full damage negation.
4. **Given** a Tier 2 ward's duration elapses, **When** it expires, **Then** both the damage negation and the healing effect stop applying, exactly as Tier 1's negation stops at expiry.

---

### Edge Cases

- What happens to an already-active ward if the caster who cast it is downed, killed, or has the Bastion Ward tattoo removed afterward? The ward continues running for the rest of its duration unaffected — it is explicitly ground-fixed and independent of the caster once cast, not a caster-following or caster-sustained effect.
- What happens if the caster re-targets and casts a new ward while a previous one they created is still active? The ability's own cooldown (User Story 1) prevents this until the cooldown from the prior activation has elapsed; the cooldown begins at the moment of casting, not at the previous ward's expiry.
- What happens if two separate Bastion Ward zones (e.g. from two different tattooed colonists) overlap? A pawn standing in the overlap is simply protected — no special stacking rule is needed since "inside at least one active ward" is sufficient for negation (and, if either overlapping ward is Tier 2, that ward's own heal effect applies to anyone inside its own radius).
- What happens if an ally already standing inside an active ward becomes hostile (e.g. a mental break) or a hostile pawn is captured and becomes friendly while inside? The ward's protection (and Tier 2 heal) is evaluated by current faction relationship each time it would apply, not locked in at the moment a pawn first entered the zone.
- What happens if a non-combat source of damage (e.g. fire spreading across the ground, an explosion) affects a tile inside the ward? It is also negated — Bastion Ward grants full invulnerability to any damage instance reaching a protected pawn while inside the zone, not attack-damage-only. (See Assumptions and Clarifications, session 2026-09-19 — this reverses the feature's original scoping.)
- What happens to the ward's remaining duration, position, and tier-at-cast across a save and reload while it is still active? All three MUST persist unchanged, exactly as required of every other tattoo's per-instance state (Constitution Principle V), so an active ward continues correctly after loading a save.
- What happens if a melee attacker that should be ejected per FR-006 has no valid walkable cell outside the radius to be moved to (e.g. boxed in by walls or clutter)? It is left in its current position rather than displaced elsewhere or blocking the cast — its own attacks against allies inside the ward are still fully negated regardless.

## Requirements *(mandatory)*

### Functional Requirements

**Casting and the zone itself**

- **FR-001**: While a pawn has Bastion Ward applied (at either tier), the system MUST grant that pawn an activatable, ground-targeted ability (gizmo) that, when used, spawns a protective zone fixed at the targeted location.
- **FR-001a**: The targetable location MUST be limited to within a maximum distance of the caster (placeholder value per tier, XML-tunable) — the player cannot place the ward an unlimited distance away.
- **FR-002**: The ward zone MUST remain fixed at its cast location for its entire duration — it MUST NOT follow the caster or reposition itself once spawned.
- **FR-003**: For the ward's duration (placeholder ~8 seconds, XML-tunable), the system MUST fully negate any damage instance — melee, ranged, environmental, or explosive alike — dealt to any ally-faction pawn standing inside the ward's radius at the moment that damage would apply — zero damage, not a partial reduction and not a depletable absorption pool. (Widened from an original melee/ranged-attack-only scope; see Assumptions and Clarifications, session 2026-09-19.)
- **FR-004**: A pawn standing outside the ward's radius MUST NOT receive any damage negation from it, regardless of proximity.
- **FR-005**: When the ward's duration elapses, the system MUST remove the zone and its damage-negation (and, at Tier 2, healing) effects immediately.
- **FR-005a**: While a ward is active, the system MUST render a visible ground indicator covering the ward's actual radius, so the player can see which cells are currently protected without consulting anything outside the game (placeholder visual treatment, tunable independently of gameplay balance). The indicator MUST disappear immediately when the ward expires (FR-005), matching the mechanic exactly rather than lagging or persisting after protection has actually ended.

**Rescue-eject on cast**

- **FR-006**: At the moment of casting, the system MUST displace any hostile pawn that is currently engaged in melee combat with an ally-faction pawn who ends up standing inside the new ward's radius, moving that hostile to a walkable position outside the radius. If no valid walkable cell outside the radius can be found for a given hostile, the system MUST leave that hostile in its current position rather than displacing it elsewhere or failing the cast — its attacks against allies inside the ward are still fully negated per FR-003 either way, so the ward's core protection is unaffected even when the shove itself can't happen.
- **FR-007**: The displacement in FR-006 MUST apply only at the instant of casting — the system MUST NOT continue ejecting or otherwise excluding hostile pawns that enter the ward's radius after it has already been cast.
- **FR-008**: The displacement in FR-006 MUST NOT apply to a hostile pawn that is attacking at range rather than in melee, even if that pawn ends up inside the ward's radius at the moment of casting.

**Cooldown and progression**

- **FR-009**: After the ability is activated, it MUST become unavailable for a fixed cooldown period (placeholder value, XML-tunable) beginning at the moment of activation, before it can be activated again, with the remaining cooldown visible to the player via the gizmo.
- **FR-010**: The system MUST track a per-pawn, per-tattoo progression counter for Bastion Ward that increments each time the wearer activates the ability, per the "times activated" pattern already used by every other triggered tattoo (PRD §5.4; feature 004 precedent).
- **FR-011**: When Bastion Ward's progression counter reaches its Tier 2 threshold, the system MUST automatically upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or any change to the pawn's tattoo-slot usage.

**Tier 2**

- **FR-012**: At Tier 2, a cast ward's radius MUST be larger than a Tier 1 ward's radius (placeholder values, XML-tunable).
- **FR-013**: At Tier 2, in addition to the unchanged full damage negation, any ally-faction pawn standing inside an active ward MUST receive a small healing effect for as long as they remain inside it.
- **FR-014**: Tier 2 MUST NOT change the ward's duration or its cooldown relative to Tier 1 — only radius and the added healing effect differ between tiers.

**Cross-cutting / reuse**

- **FR-015**: The damage-negation effect (FR-003) MUST function correctly whether Combat Extended is loaded or not, and MUST NOT throw or silently no-op in either configuration, per Constitution Principle I.
- **FR-016**: Every tunable numeric value driving Bastion Ward's effect (duration, radius per tier, max cast range per tier, cooldown length, the Tier 2 activation-count threshold, and the Tier 2 heal magnitude) MUST be read through the existing `TattooEffectValues` accessor (feature 002) rather than read directly from XML/Def fields at each use site.
- **FR-017**: The system MUST reuse the existing `TattooTierProgress` tracker for tier/counter state and the existing `CombatExtendedInterop` CE-detection convention for any CE-specific branching this feature needs, rather than re-implementing either.
- **FR-018**: The system MUST reuse the existing triggered-ability (gizmo/cooldown/activation) pattern established by feature 004 (Guardian's Call) for Bastion Ward's own cast/cooldown behavior, rather than inventing a new one.
- **FR-019**: The progression counter's current value, the tattoo's current tier, and any currently-active ward's remaining duration, position, and tier-at-cast MUST persist correctly across a game save and reload.
- **FR-020**: If the Bastion Ward hediff is removed from a pawn by any means, the system MUST stop granting the gizmo and MUST prevent further casts, but MUST NOT retroactively end a ward that pawn had already cast and which is still independently active (per FR-002's caster-independence).
- **FR-021**: The Bastion Ward hediff MUST be protected by the existing cross-tattoo removal guard (feature 003, FR-015) with no additional per-tattoo configuration required.

### Key Entities

- **Bastion Ward Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Bastion Ward hediff — the activatable, ground-targeted cast ability and its cooldown at Tier 1, plus a larger radius and an added healing effect for anyone inside at Tier 2.
- **Ward Zone**: The spawned, ground-fixed effect a cast creates — tracks its position, radius, remaining duration, and the tier it was cast at (which determines its radius and whether it heals). Independent of its caster once spawned; unaffected by the caster's death, downing, or later loss of the tattoo.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of ability activations that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is reached — reuses feature 002's tracker.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An ally-faction pawn standing inside an active Bastion Ward takes measurably zero damage from melee and ranged attacks for the ward's full duration, verified across repeated trials in both a non-CE game and a CE-loaded game.
- **SC-002**: A pawn standing outside an active ward's radius takes normal, unreduced damage, confirming the negation is strictly zone-bound.
- **SC-003**: Casting a ward on a location where an ally is already being meleed measurably displaces the attacking hostile(s) out of the new zone's radius as part of the cast, verified across repeated trials.
- **SC-004**: The ability cannot be reactivated until its cooldown period has elapsed, verified across repeated activation attempts.
- **SC-005**: A Bastion Ward-tattooed pawn automatically upgrades from Tier 1 to Tier 2 once the designated number of activations occurs, with zero additional player action required.
- **SC-006**: A Tier 2 ward's radius is measurably larger than a Tier 1 ward's, and allies standing inside a Tier 2 ward measurably regain health over time in addition to the unchanged full damage negation.
- **SC-007**: An already-active ward continues to negate damage (and heal, if Tier 2) correctly for the remainder of its duration even after its caster is downed, killed, or loses the tattoo.
- **SC-008**: A Bastion Ward-tattooed pawn's progression counter, current tier, and any active ward's remaining duration/position/tier are unchanged immediately after a save/reload compared to immediately before it.
- **SC-009**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat Extended, with zero errors in the RimWorld dev console in either configuration.
- **SC-010**: A player can visually identify exactly which cells an active ward currently protects, without opening the dev console or any other out-of-game tool, and that indicator disappears in the same instant the ward's protection actually ends.

## Assumptions

- Duration (~8 seconds), radius per tier, max cast range per tier, cooldown length, the Tier 1→Tier 2 activation-count threshold, and the Tier 2 heal magnitude are placeholder/balance-pass numbers, to be resolved in a dedicated balance pass rather than fixed here — ship with clearly-labeled placeholder values per Constitution Principle III.
- The max cast range (FR-001a) is a separate placeholder from the zone's own radius — one bounds how far from the caster the player can place the ward's center, the other bounds the protected area once placed.
- The rescue-eject in User Story 2 is a one-time reaction evaluated only at the instant of casting; it is not a standing "no enemies allowed" exclusion zone for the ward's whole duration. A continuous-repel version was prototyped and playtested, then deliberately reverted back to this one-time-shove design — the damage negation (FR-003) alone already guarantees a re-entering hostile can never land a hit, so a standing barrier was judged not worth the added behavior complexity.
- Bastion Ward's damage negation is full invulnerability to any damage instance reaching a protected pawn while inside the zone — melee, ranged, environmental, and explosive alike (fire, an explosion). This reverses the feature's original scoping, which had read the proposal's "prevent melee and ranged attacks coming in" wording as attack-damage-only and explicitly flagged fire/explosion as out of scope; the customer confirmed during live testing (session 2026-09-19) that the broader behavior — closer to Stillshroud's own explicit full invulnerability — is what's wanted, before any code change actually shipped the narrower attack-only reading.
- The ward protects any ally-faction pawn standing inside it, including the caster themselves if they remain within the radius after casting — there is no special exclusion for the caster.
- The cooldown (FR-009) begins at the moment of activation, not at the ward's expiry, matching Guardian's Call's existing cooldown convention (feature 004).
- Tier 2 does not change the ward's duration, only its radius and the added heal — per the proposal, which calls out radius and healing as the only Tier 2 changes.
- This feature covers exactly one tattoo's gameplay effect, Bastion Ward. The remaining three tattoos from this batch (Soul Tether, Stillshroud, Thunderclap Sigil) are out of scope here and will each get their own future spec, per the project's build order (docs/PRD.md §10).
- FR-005a's ground indicator's exact visual treatment (color, opacity, style) is a placeholder aesthetic choice, not a locked-down design — a tinted field overlay was chosen specifically because it needs no new art asset and reuses a look already familiar from vanilla's own area-highlighting UI, not because a more elaborate animated effect (e.g. a custom shimmering-barrier mote) was rejected outright. A richer visual can replace it later without changing FR-005a's actual requirement (a live, accurate indicator of protected cells) or touching the underlying mechanic at all.
