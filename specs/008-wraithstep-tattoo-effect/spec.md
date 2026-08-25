# Feature Specification: Wraithstep Tattoo Effect (Fourth Triggered Ability + First Cell-Targeted Ability Slice)

**Feature Branch**: `008-wraithstep-tattoo-effect`

**Created**: 2026-08-14

**Status**: Draft

**Input**: User description: "Wraithstep tattoo effect — triggered ability, short-range blink/teleport for escape/repositioning. Tier 1: short-range blink on a cooldown. Tier 2 (after a use-count threshold, TBD): longer blink range, plus a brief untargetable window immediately after blinking. Follows the same triggered-tattoo pattern established by Bloodrune (007), Stormlash (005), and Guardian's Call (004) — gizmo + cooldown, tier-up via times-activated counter, must remain functional both without Combat Extended and with CE loaded (movement/positioning must not conflict with CE's own systems)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Wraithstep instantly relocates its wearer to a chosen nearby spot (Priority: P1)

A player applies the Wraithstep tattoo to a colonist (via the existing ritual station). That colonist gains an activatable ability (a gizmo, like Guardian's Call, Stormlash, and Bloodrune) that, unlike those three, asks the player to pick a destination on the map before it fires. Activating the ability and choosing a valid nearby spot instantly relocates the wearer there — letting a colonist dodge out of a bad spot, close distance, or escape a surrounded position without needing to walk there. After the blink resolves, the ability goes on cooldown before it can be used again.

**Why this priority**: This is the entire point of the feature — the base Tier 1 effect the tattoo exists to deliver. It's the smallest slice that proves the ability actually relocates the wearer, and every other story here depends on it existing first.

**Independent Test**: Can be fully tested by applying Wraithstep to a colonist, activating the ability, choosing a valid destination cell within range, and observing the colonist instantly appear at that cell, followed by the ability becoming unavailable for a cooldown period.

**Acceptance Scenarios**:

1. **Given** a pawn with Wraithstep applied, **When** the player activates the ability and selects a valid destination cell within range, **Then** that pawn is instantly relocated to the chosen cell.
2. **Given** the player has activated the ability, **When** they attempt to select a destination cell beyond the ability's range, an unwalkable cell, an unexplored/fogged cell, or a cell already occupied by another pawn, **Then** that cell cannot be confirmed as the destination and the ability does not fire or start its cooldown.
3. **Given** the player has activated the ability, **When** they aim the targeter across the map, **Then** the ability's current maximum range is visibly indicated, and hovering an invalid cell shows the player why it can't be selected — mirroring the range-ring-plus-reject-reason feedback vanilla's own targeted abilities (e.g. Psycasts) already give.
4. **Given** the ability was just successfully activated, **When** the player looks at the gizmo, **Then** it shows the ability is on cooldown and cannot be activated again until the cooldown expires.
5. **Given** a pawn without Wraithstep applied, **When** any amount of time passes or combat occurs, **Then** no Wraithstep gizmo appears for that pawn and its position is unaffected by this feature, regardless of what other tattoos or effects that pawn may have.
6. **Given** a pawn with Wraithstep applied is downed or otherwise unable to act, **When** the player views that pawn's gizmos, **Then** the Wraithstep ability is unavailable, consistent with vanilla's own handling of movement-dependent abilities for incapacitated pawns.

---

### User Story 2 - Wraithstep grows stronger the more it's used, and covers its wearer's landing at Tier 2 (Priority: P2)

As a Wraithstep-tattooed colonist keeps using the ability, the tattoo tracks how many times it's been activated and automatically upgrades itself to a stronger Tier 2 version once enough uses have accumulated — with no extra ritual, ingredients, or tattoo slot required. At Tier 2, the wearer can blink farther than at Tier 1, and immediately after landing, hostiles are briefly unable to pick the wearer as a target — covering the vulnerable instant after a repositioning play where an enemy would otherwise capitalize on the wearer's new, possibly exposed position.

**Why this priority**: This is the PRD §5.4 tiering mechanic, mirroring the equivalent user story in every tattoo shipped so far. It only makes sense once Tier 1's blink (User Story 1) exists and can be observed, so it's the natural second slice — and the Tier 2 untargetable window is what makes the longer-range blink safe to use aggressively rather than just a bigger version of the same risk.

**Independent Test**: Can be tested independently by activating a Wraithstep-tattooed pawn's ability enough times to reach the threshold, then confirming the tattoo's effect strengthens in place — the maximum selectable blink range is larger, and for a brief window immediately after landing, an otherwise-eligible hostile's targeting search does not select the wearer, where the same search would select an otherwise-identical unbuffed or Tier 1 pawn in the same spot.

**Acceptance Scenarios**:

1. **Given** Tier 1 Wraithstep applied with its progression counter below threshold, **When** the player successfully activates the ability, **Then** the counter increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering activation resolves, **Then** Wraithstep upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to the pawn's slot usage.
3. **Given** Wraithstep is at Tier 2, **When** the player selects a destination cell, **Then** cells farther away than Tier 1's maximum range (up to Tier 2's larger maximum) are selectable.
4. **Given** a Tier 2 Wraithstep blink has just resolved, **When** a hostile pawn's targeting search runs while the wearer is otherwise a legal target, **Then** that search does not select the wearer for a brief window, in both a non-CE game and a CE-loaded game.
5. **Given** the Tier 2 untargetable window has elapsed, **When** a hostile pawn's targeting search next runs, **Then** the wearer is once again a selectable target as normal, with no lingering effect.

---

### User Story 3 - The gizmo reuse point proves itself on a fourth consumer, the targeting-exclusion convention gets its first user, and mastery progress accrues for free (Priority: P3)

Having shipped Guardian's Call, Stormlash, and Bloodrune as the first three triggered tattoos, and having since shipped the pawn-wide tattoo-mastery XP track (feature 006), Wraithstep is the fourth triggered ability and the first one that requires the player to pick a destination rather than firing on the wearer alone. A developer (or reviewer) confirms that the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` gizmo-contribution mechanism (feature 004) works correctly for a fourth, still-different kind of ability without any change to the three prior tattoos' own files; that Wraithstep's Tier 2 untargetable window reuses Guardian's Call's two-path AI-targeting convention (feature 004) in its inverted form — excluding the wearer from target selection instead of forcing selection toward a taunter — without duplicating that convention's vanilla/CE patch structure; and that activating Wraithstep automatically counts toward that pawn's tattoo-mastery progress (feature 006) with no Wraithstep-specific code required to make that happen.

**Why this priority**: Matters less than the blink itself working (User Stories 1-2), but it's what confirms three separate pieces of shared infrastructure — the gizmo reuse point, the two-path targeting convention, and the mastery track — all generalize to a genuinely new tattoo without needing to be touched or duplicated from scratch.

**Independent Test**: Can be tested by reviewing that Wraithstep's own comp implements `IProvidesTattooGizmo` and is picked up by the existing shared patch with zero modifications to Guardian's Call's, Stormlash's, or Bloodrune's own files; by reviewing that its Tier 2 targeting-exclusion follows the two-path (vanilla + CE) convention documented in feature 004's `two-path-targeting-contract.md` without altering Guardian's Call's own taunt patches or registry; and by activating Wraithstep on a pawn and confirming that pawn's tattoo-mastery progress (feature 006) increases by one, the same way it would for any other triggered tattoo.

**Acceptance Scenarios**:

1. **Given** the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` reuse point, **When** Wraithstep's own comp implements it, **Then** Wraithstep's gizmo appears and works correctly with no modification to Guardian's Call's, Stormlash's, or Bloodrune's comp, patch, or targeting files.
2. **Given** a pawn with Wraithstep and at least one other triggered tattoo applied, **When** the player views that pawn's gizmo row, **Then** every ability's gizmo appears independently, each showing its own correct cooldown/availability state.
3. **Given** Wraithstep's Tier 2 targeting exclusion, **When** it is implemented, **Then** it follows the vanilla-postfix-plus-CE-verification-or-second-patch structure documented in feature 004's two-path targeting contract, without modifying `GuardiansCallTauntRegistry` or Guardian's Call's own patches.
4. **Given** a pawn with Wraithstep applied, **When** the wearer successfully activates it, **Then** that pawn's pawn-wide tattoo-mastery progress (feature 006) increases by exactly one, with no Wraithstep-specific code added to feature 006's tracker.

---

### Edge Cases

- What happens if the player cancels destination selection after activating the ability (e.g. presses escape) instead of confirming a cell? The activation is abandoned entirely — no blink occurs, the cooldown does not start, and the progression counter does not increment, consistent with vanilla's own targeted-ability cancellation behavior.
- What happens if the chosen destination cell is valid at the moment of targeting but becomes invalid (occupied, destroyed floor, etc.) before the blink resolves? Out of scope for this feature to guard against beyond what vanilla's own instant-cast targeted abilities already handle — targeting and resolution are treated as effectively simultaneous, matching vanilla's own instant-effect ability precedent.
- Does the blink need line-of-sight or an unobstructed path between origin and destination? No — the destination cell itself must be valid (walkable, unfogged, unoccupied, within range), but intervening walls, doors, or terrain between origin and destination do not block the teleport; that is the point of a blink, consistent with vanilla's own Psycast-style blink abilities.
- What happens if the tattooed pawn is downed, killed, or otherwise removed from play immediately after blinking, before a Tier 2 untargetable window expires? The window simply stops mattering (a downed/dead pawn isn't being targeted meaningfully); no special cleanup beyond what already happens when a hediff's owning pawn stops existing is required.
- What happens if the ability is activated again while still on cooldown? The gizmo itself prevents this (RimWorld's standard cooldown-gizmo behavior disables activation until the cooldown expires), matching the precedent of every triggered tattoo shipped so far.
- Does the Tier 2 untargetable window interrupt an attack a hostile already committed to before the blink (e.g. a bullet already in flight, a melee swing already underway)? No — the window only prevents hostiles from newly selecting the wearer as a target during that window; it does not retroactively cancel or redirect an attack already committed before the blink occurred.
- Does the Tier 2 untargetable window make the wearer undetectable/invisible (e.g. removed from enemy vision, unable to be seen)? No — it is scoped strictly to attack-target selection, not to vision, detection, or any other awareness mechanic; the wearer remains fully visible and can still be seen, just not selected as a new attack target for the window's duration.
- What happens to the progression counter and tier state across a save and reload? Both MUST persist unchanged, exactly as required of every tattoo shipped so far.
- What happens if some other system removes the Wraithstep hediff from the pawn? All of its effects (the gizmo, the ability to blink, any active Tier 2 untargetable window) MUST stop applying immediately once the hediff is gone, and the existing cross-tattoo removal guard (feature 003, FR-015) already prevents anything but this mod's own sanctioned path or Developer Mode/God Mode from removing it in the first place.
- What happens if Combat Extended is loaded, then unloaded (or vice versa) between sessions on the same save? The Tier 2 targeting exclusion must correctly resolve against whichever targeting code path (vanilla or CE) is active, without throwing or corrupting saved tier/counter state either way.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Wraithstep applied (at either tier), the system MUST grant that pawn an activatable ability (gizmo) that, when used, prompts the player to select a destination cell and, upon a valid selection, instantly relocates that pawn to the chosen cell.
- **FR-002**: A destination cell MUST be considered valid only if it is walkable, unfogged/explored, unoccupied by another pawn, and within the ability's current maximum range; an invalid or unconfirmed selection MUST NOT trigger a relocation, start the cooldown, or increment the progression counter.
- **FR-003**: While the player is choosing a destination, the system MUST visibly indicate the ability's current maximum range and MUST show the player a reason when hovering a cell that fails FR-002's validity check, consistent with vanilla's own targeted-ability targeting feedback (e.g. Psycast range rings and reject-reason tooltips).
- **FR-004**: The relocation MUST NOT be blocked by intervening walls, doors, or terrain between the wearer's origin and the chosen destination cell — only the destination cell's own validity (FR-002) gates the blink.
- **FR-005**: After the ability successfully fires, it MUST become unavailable for a fixed cooldown period before it can be activated again, with the remaining cooldown visible to the player via the gizmo.
- **FR-006**: The system MUST track a per-pawn, per-tattoo progression counter for Wraithstep that increments each time the wearer successfully activates the ability (a completed blink, not a cancelled or invalid targeting attempt), per the "times activated" counter defined for triggered tattoos (PRD §5.4).
- **FR-007**: When Wraithstep's progression counter reaches its Tier 2 threshold, the system MUST automatically upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or any change to the pawn's tattoo-slot usage.
- **FR-008**: At Tier 2, the ability's maximum selectable blink range MUST be greater than Tier 1's, using the same activation mechanism and gizmo.
- **FR-009**: At Tier 2 only, immediately after a blink resolves, the wearer MUST be excluded from hostile AI's attack-target selection for a fixed duration, in both a non-CE game and a CE-loaded game, without interrupting any attack a hostile already committed to before the blink and without affecting the wearer's visibility/detectability by any other mechanic.
- **FR-010**: The progression counter's current value and the tattoo's current tier MUST persist correctly across a game save and reload.
- **FR-011**: None of Wraithstep's effects (gizmo, blink, Tier 2 targeting exclusion) MUST apply to any pawn that does not currently have Wraithstep applied.
- **FR-012**: Wraithstep's effect MUST be implemented through the existing `appliedHediff` contract established by feature 001 and reused by features 002-007 (the `TattooDef` → `HediffDef` → Hediff/HediffComp chain), rather than a new or parallel mechanism for granting or tracking tattoo state.
- **FR-013**: Every tunable numeric value driving Wraithstep's effect (Tier 1 range, Tier 2 range, cooldown length, Tier 2 untargetable-window duration, and the Tier 2 threshold) MUST be read through feature 002's `TattooEffectValues` accessor rather than read directly from XML/Def fields at each use site, consistent with the equivalent requirement for every tattoo shipped so far.
- **FR-014**: The system MUST reuse feature 002's existing tier-progression tracker (`TattooTierProgress`) without modification for Wraithstep's own tier state.
- **FR-015**: Wraithstep's ability gizmo MUST be contributed via the existing `IProvidesTattooGizmo` interface and picked up by the existing shared `Patch_Pawn_GetGizmos_TattooGizmos` patch established by feature 004, with no modification to Guardian's Call's, Stormlash's, or Bloodrune's own comp, patch, or targeting files.
- **FR-016**: Wraithstep's Tier 2 targeting exclusion MUST follow feature 004's two-path AI-targeting convention (a vanilla patch plus a verified-or-separately-patched Combat Extended path, per Constitution Principle IV) without modifying `GuardiansCallTauntRegistry` or any of Guardian's Call's own patches.
- **FR-017**: A pawn with Wraithstep and any other triggered tattoo(s) applied MUST see every ability's gizmo independently, with each ability's activation, cooldown, and effect state fully independent of the others.
- **FR-018**: If the Wraithstep hediff is removed from a pawn by any means, the system MUST stop applying all of its effects immediately, with no lingering gizmo, blink availability, or Tier 2 targeting exclusion.
- **FR-019**: The Wraithstep hediff MUST be protected by the existing cross-tattoo removal guard (feature 003, FR-015) with no additional per-tattoo configuration required, since that guard already covers every `TattooMagicDef.appliedHediff` generically.
- **FR-020**: Successfully activating Wraithstep's ability MUST automatically increase the wearer's pawn-wide tattoo-mastery progress (feature 006) by one qualifying event, with no Wraithstep-specific code added to feature 006's own files, since that track's gizmo-wrap hook (feature 006, research.md R1) already covers every `IProvidesTattooGizmo` implementer generically.

### Key Entities *(include if feature involves data)*

- **Wraithstep Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Wraithstep hediff — the activatable, player-targeted blink and its cooldown at Tier 1, with a longer maximum range plus a temporary post-blink targeting exclusion at Tier 2.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying events (for Wraithstep: number of times the ability has been successfully activated, i.e. resulted in an actual blink) that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is reached — reusing feature 002's tracker.
- **Destination Cell Targeting**: The player-driven selection of a valid map cell (walkable, unfogged, unoccupied, within range) that the ability requires before it will fire, with a visible range indicator and per-cell reject reasons shown while choosing — the first cell-targeted (rather than self-targeted or taunt-registry-driven) ability among this mod's triggered tattoos.
- **Post-Blink Untargetable Window (Tier 2 only)**: The temporary state, running for a fixed duration immediately after a Tier 2 blink resolves, during which hostile AI's attack-target search excludes the wearer — the first user of feature 004's two-path AI-targeting convention in its inverted (exclusion rather than forced-selection) form.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When a pawn with Wraithstep activates the ability and selects a valid destination cell within range, that pawn is instantly relocated to the chosen cell.
- **SC-002**: An attempt to select a destination cell that is out of range, unwalkable, unexplored, or occupied fails to trigger a relocation and does not start the cooldown.
- **SC-003**: While choosing a destination, the player can see the ability's current maximum range and, when hovering an invalid cell, a reason it can't be selected.
- **SC-004**: The ability cannot be re-activated until its cooldown period has elapsed, verified across repeated activation attempts.
- **SC-005**: A Wraithstep-tattooed pawn's progression counter increments only on the wearer's own successfully-resolved blinks, verified across repeated trials, and does not increment on cancelled or invalid targeting attempts.
- **SC-006**: A tattooed pawn's Wraithstep automatically upgrades from Tier 1 to Tier 2 once the designated number of successful activations occurs, with zero additional player action required (no new ritual, no ingredient spend, no slot change).
- **SC-007**: After upgrading to Tier 2, the maximum selectable blink range is measurably larger than Tier 1's.
- **SC-008**: Immediately after a Tier 2 blink resolves, an otherwise-eligible hostile's attack-target search fails to select the wearer for the window's duration, in both a non-CE game and a CE-loaded game, and correctly resumes selecting the wearer once the window elapses.
- **SC-009**: A Wraithstep-tattooed pawn's progression counter and current tier are unchanged immediately after a save/reload compared to immediately before it.
- **SC-010**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat Extended, with zero errors in the RimWorld dev console in either configuration.
- **SC-011**: Wraithstep's gizmo is implemented and functions correctly using the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` reuse point with zero modifications to any of Guardian's Call's, Stormlash's, or Bloodrune's own files.
- **SC-012**: Wraithstep's Tier 2 targeting exclusion is implemented using feature 004's two-path targeting convention with zero modifications to `GuardiansCallTauntRegistry` or Guardian's Call's own patches.
- **SC-013**: A pawn with Wraithstep and at least one other triggered tattoo applied has every gizmo function correctly and independently of the others.
- **SC-014**: Activating Wraithstep increases the wearer's pawn-wide tattoo-mastery progress (feature 006) by exactly one, with zero Wraithstep-specific code added to feature 006's own files.
- **SC-015**: An attempt to remove the Wraithstep hediff through RimWorld's normal hediff-removal API, from any caller other than this mod's own sanctioned path, fails to remove it, consistent with feature 003's existing cross-tattoo removal guard.

## Assumptions

- "Short-range blink/teleport" (PRD §6 roster item 7) is interpreted as a player-directed, cell-targeted ability — the player activates the gizmo, then picks a destination cell within range, mirroring vanilla's own Psycast-style blink abilities — rather than a random-direction or fixed-vector teleport, or an automatic reposition with no player input.
- Destination-cell validity (walkable, unfogged/explored, unoccupied, within range) mirrors vanilla's own targeted-ability targeting conventions; intervening terrain/walls between origin and destination do not block the teleport itself, consistent with "blink" as a through-obstacles repositioning tool rather than a fast-walk.
- The range indicator and reject-reason feedback (FR-003) are expected to be delivered through RimWorld's own existing targeter UI conventions (the same range-ring-plus-reject-reason mechanism vanilla's own targeted Verbs/Psycasts already provide), not a bespoke UI built for this feature — to be confirmed as a feasible reuse point during planning rather than assumed as a new UI component.
- "Untargetable" (Tier 2) is interpreted narrowly as exclusion from hostile AI's *new* attack-target selection only — not stealth/invisibility from vision, and not interruption of an attack a hostile already committed to before the blink — reusing the inverse of Guardian's Call's two-path targeting convention (feature 004) rather than a new detectability mechanic.
- A cancelled or invalid targeting attempt (no destination confirmed, or an invalid cell) does not consume the cooldown or increment the progression counter — only a successfully-resolved blink counts as an activation, consistent with vanilla's own cancellable targeted-ability precedent.
- Downed or otherwise movement-incapacitated pawns cannot use the ability, consistent with vanilla's own gizmo-disabling for movement-dependent abilities — no new incapacitation logic is expected to be required beyond what vanilla already provides, to be confirmed during planning.
- Range (Tier 1 and Tier 2), cooldown length, the Tier 2 untargetable-window duration, and the Tier 1→Tier 2 activation-count threshold are placeholder/balance-pass numbers per PRD §9, exactly as every other tattoo's numeric values have been so far, and small/fast-to-test values are preferred over "plausible-looking" ones per feature 006's research.md R8 precedent. This feature only needs the mechanism to work correctly for whatever values are configured, not final tuned numbers.
- This feature covers exactly one tattoo's gameplay effect, Wraithstep. The remaining tattoos (Ember Ward, Ironskin Glyph, Berserker's Mark, Starlight Ward, Vampiric Thorn) are out of scope here; wiring up their individual effects is future work.
- Feature 006's tattoo-mastery XP track requires no changes for this feature — Wraithstep's activations are picked up automatically through the existing gizmo-wrap hook, per feature 006's own FR-015/FR-017 reuse guarantee.
- The value-accessor-driven, override-ready design established by features 002-007 continues to apply here; this feature still does not build a player-facing settings UI (PRD §10 remains deferred).
- The feature 001 ritual/application flow (recipe, skill check, slot enforcement) is unaffected by this feature; this feature only adds gameplay effect behavior to a tattoo that can already be applied.
