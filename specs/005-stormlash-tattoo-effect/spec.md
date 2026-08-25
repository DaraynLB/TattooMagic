# Feature Specification: Stormlash Tattoo Effect (Second Triggered Ability + Movement-Slow Immunity Slice)

**Feature Branch**: `005-stormlash-tattoo-effect`

**Created**: 2026-08-08

**Status**: Draft

**Input**: User description: "Implement Stormlash, tattoo #4 from docs/PRD.md's starter roster (§6): a triggered tattoo granting a temporary move speed + attack speed boost when activated (gizmo + cooldown, reusing the IProvidesTattooGizmo / Patch_Pawn_GetGizmos_TattooGizmos reuse point established by feature 004 / Guardian's Call — this is the second triggered tattoo, so it should validate that reuse point works for a genuinely different consumer, not just Guardian's Call's own files). Progression counter increments once per activation (§5.4, same pattern as Guardian's Call). At Tier 2 the speed boost is bigger, and the pawn also gains brief immunity to movement-slow effects during the buff window — including Combat Extended's suppression-slow, per §7's CE integration requirement. All exact numeric values (speed bonus, duration, cooldown, Tier 2 threshold) are placeholders pending the PRD §9 balance pass, same convention as every tattoo shipped so far."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Stormlash makes its wearer faster and hits harder-and-faster on demand (Priority: P1)

A player applies the Stormlash tattoo to a colonist (via the existing ritual station). In combat or while repositioning, that colonist has an activatable ability (a gizmo, like RimWorld's other activatable abilities and like Guardian's Call). Activating it grants the tattooed pawn a temporary boost to both movement speed and attack speed, letting them close distance, kite, or land more attacks over a fixed window. After the boost's duration ends, the pawn's speed and attack rate return to normal, and the ability goes on cooldown before it can be used again.

**Why this priority**: This is the entire point of the feature — the base Tier 1 effect the tattoo exists to deliver. It's the smallest slice that proves the ability actually boosts speed, and every other story here depends on it existing first.

**Independent Test**: Can be fully tested by applying Stormlash to a colonist, activating the ability, and observing a measurable increase to that pawn's movement speed and attack rate for the ability's duration, followed by a return to baseline and a cooldown period — in both a non-CE game and a CE-loaded game.

**Acceptance Scenarios**:

1. **Given** a pawn with Stormlash applied, **When** the player activates the ability, **Then** that pawn's movement speed and attack speed both measurably increase for the ability's duration, in both a non-CE game and a CE-loaded game.
2. **Given** the boost's duration has elapsed, **When** it expires, **Then** the pawn's movement speed and attack speed return to their normal (non-buffed) values with no lingering effect.
3. **Given** the ability was just activated, **When** the player looks at the gizmo, **Then** it shows the ability is on cooldown and cannot be activated again until the cooldown expires.
4. **Given** a pawn without Stormlash applied, **When** combat or movement occurs, **Then** no Stormlash gizmo appears for that pawn and its speed/attack rate are unaffected, regardless of what other tattoos or effects that pawn may have.

---

### User Story 2 - Stormlash grows stronger the more it's used, and shrugs off being slowed at Tier 2 (Priority: P2)

As a Stormlash-tattooed colonist keeps using the ability, the tattoo tracks how many times it's been activated and automatically upgrades itself to a stronger Tier 2 version once enough uses have accumulated — with no extra ritual, ingredients, or tattoo slot required. At Tier 2, activating the ability grants a bigger speed boost than Tier 1, and additionally makes the wearer briefly immune to movement-slowing effects for the duration of the boost — including Combat Extended's suppression-slow when CE is loaded — so the pawn can't be neutralized mid-charge by the very effects a faster pawn is most likely to run into.

**Why this priority**: This is the PRD §5.4 tiering mechanic, mirroring the equivalent user story in every tattoo shipped so far. It only makes sense once Tier 1's boost (User Story 1) exists and can be observed, so it's the natural second slice — and the Tier 2 slow-immunity is what makes the bigger boost reliable rather than something a single enemy slow effect can cancel out.

**Independent Test**: Can be tested independently by activating a Stormlash-tattooed pawn's ability enough times to reach the threshold, then confirming the tattoo's effect strengthens in place — the speed boost is bigger, and while it's active the pawn is unaffected by an applied movement-slow effect (including CE's suppression-slow in a CE-loaded game), where the same effect would slow an otherwise-identical unbuffed or Tier 1 pawn.

**Acceptance Scenarios**:

1. **Given** Tier 1 Stormlash applied with its progression counter below threshold, **When** the player activates the ability, **Then** the counter increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering activation resolves, **Then** Stormlash upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to the pawn's slot usage.
3. **Given** Stormlash is at Tier 2, **When** the wearer activates the ability, **Then** the movement/attack-speed boost is measurably larger than Tier 1's, for the same or a configured duration.
4. **Given** a Tier 2 Stormlash boost is active, **When** a movement-slow effect would otherwise apply to the wearer (including Combat Extended's suppression-slow, in a CE-loaded game), **Then** the wearer's movement speed is unaffected by it for as long as the boost remains active.
5. **Given** the Tier 2 boost's duration has elapsed, **When** it expires, **Then** the pawn is once again susceptible to movement-slow effects as normal, with no lingering immunity.

---

### User Story 3 - The gizmo/cooldown reuse point proves itself on a second, unrelated consumer (Priority: P3)

Having shipped Guardian's Call as the first triggered tattoo, Stormlash is the second. A developer (or reviewer) confirms that the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` gizmo-contribution mechanism feature 004 introduced works correctly for a genuinely different kind of ability — a self-buff with no targeting or taunt behavior at all — without any change to Guardian's Call's own files, and that a pawn with both tattoos applied gets two independent gizmos that each work correctly.

**Why this priority**: Matters less than the boost itself actually working (User Stories 1-2), but it is the specific thing the customer asked this feature to prove: that feature 004's reuse point is real reusable infrastructure and not something that only happens to work for its original, single consumer.

**Independent Test**: Can be tested by reviewing that Stormlash's own comp implements `IProvidesTattooGizmo` and is picked up by the existing shared patch with zero modifications to Guardian's Call's files, and by applying both Guardian's Call and Stormlash to the same pawn and confirming both gizmos appear and function independently (activating one does not affect the other's cooldown or availability).

**Acceptance Scenarios**:

1. **Given** the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` reuse point feature 004 established, **When** Stormlash's own comp implements it, **Then** Stormlash's gizmo appears and works correctly with no modification to Guardian's Call's comp, patch, or any other feature-004 file.
2. **Given** a pawn with both Guardian's Call and Stormlash applied, **When** the player views that pawn's gizmo row, **Then** both abilities' gizmos appear independently, each showing its own correct cooldown/availability state.
3. **Given** that same pawn, **When** the player activates one ability, **Then** the other ability's cooldown and availability are unaffected.

---

### Edge Cases

- What happens if the tattooed pawn is downed, killed, or otherwise removed from play while the boost is still active? The boost simply stops mattering (a downed/dead pawn isn't moving or attacking); no special cleanup beyond what already happens when a hediff's owning pawn stops existing is required.
- What happens if the ability is activated again while still on cooldown? The gizmo itself prevents this (RimWorld's standard cooldown-gizmo behavior disables activation until the cooldown expires), matching Guardian's Call's precedent — the underlying boost logic does not need to separately guard against it.
- What happens if the ability is activated again while a previous activation's boost window is still running (e.g. cooldown shorter than duration)? The new activation simply restarts the boost/cooldown timers from that moment (refresh, not stack) — there is no stacking of multiple simultaneous Stormlash boosts on the same pawn, consistent with Guardian's Call's single-instance-per-pawn taunt/buff state.
- Does Tier 2's movement-slow immunity apply to the pawn's own baseline movement-speed factors (injuries, pain, encumbrance/carry weight, terrain movement cost)? No — immunity applies only to externally-inflicted movement-slowing effects (e.g. a hostile's slow-on-hit effect, CE's suppression-slow), not to the pawn's intrinsic movement-speed calculation, which continues to apply normally during the boost.
- What happens to the progression counter and tier state across a save and reload? Both MUST persist unchanged, exactly as required of every tattoo shipped so far.
- What happens if some other system removes the Stormlash hediff from the pawn? All of its effects (the gizmo, any active speed boost, any active Tier 2 slow immunity) MUST stop applying immediately once the hediff is gone, and the existing cross-tattoo removal guard (feature 003, FR-015) already prevents anything but this mod's own sanctioned path or Developer Mode/God Mode from removing it in the first place.
- What happens if Combat Extended is loaded, then unloaded (or vice versa) between sessions on the same save? The boost must correctly resolve against whichever stat set (vanilla or CE) matches CE's current presence, and Tier 2's slow-immunity must correctly no-op against CE's suppression-slow when CE is absent, without throwing or corrupting saved tier/counter state either way.
- What happens if a pawn has both Stormlash and some other tattoo that also modifies movement speed (e.g. a future tattoo with its own speed offset)? Stormlash's boost stacks additively with any other tattoo's speed-stat contribution via the same shared summation mechanism every other tattoo's stat offset already uses, rather than overriding or suppressing it.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Stormlash applied (at either tier), the system MUST grant that pawn an activatable ability (gizmo) that, when used, grants that pawn a temporary increase to both movement speed and attack speed for a fixed duration.
- **FR-002**: After the ability is activated, it MUST become unavailable for a fixed cooldown period before it can be activated again, with the remaining cooldown visible to the player via the gizmo.
- **FR-003**: The movement-speed and attack-speed boosts MUST resolve correctly against vanilla movement/combat-speed stats in a non-CE game, and against Combat Extended's equivalent replacement stats in a CE-loaded game, per PRD §7.
- **FR-004**: The system MUST track a per-pawn, per-tattoo progression counter for Stormlash that increments each time the wearer activates the ability, per the "times activated" counter defined for triggered tattoos (PRD §5.4).
- **FR-005**: When Stormlash's progression counter reaches its Tier 2 threshold, the system MUST automatically upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or any change to the pawn's tattoo-slot usage.
- **FR-006**: At Tier 2, the movement-speed and attack-speed boost magnitudes MUST be larger than Tier 1's, using the same activation mechanism and gizmo.
- **FR-007**: At Tier 2 only, for the duration of an active boost, the wearer MUST be immune to externally-inflicted movement-slowing effects, including Combat Extended's suppression-slow when CE is loaded, without affecting the pawn's intrinsic movement-speed factors (injuries, pain, encumbrance, terrain).
- **FR-008**: Stormlash's speed-stat boosts MUST target the same movement- and attack-speed-related StatDef(s) — vanilla, and Combat Extended's equivalent(s) when CE is loaded — that any other tattoo's own speed bonus would use, so it stacks additively with any other simultaneously-applied tattoo's speed bonus via the existing shared stat-offset summation mechanism, without Stormlash needing special-case knowledge of any other specific tattoo.
- **FR-009**: The progression counter's current value and the tattoo's current tier MUST persist correctly across a game save and reload.
- **FR-010**: None of Stormlash's effects (gizmo, speed/attack-speed boost, Tier 2 slow immunity) MUST apply to any pawn that does not currently have Stormlash applied.
- **FR-011**: Stormlash's effect MUST be implemented through the existing `appliedHediff` contract established by feature 001 and reused by features 002-004 (the `TattooDef` → `HediffDef` → Hediff/HediffComp chain), rather than a new or parallel mechanism for granting or tracking tattoo state.
- **FR-012**: Every tunable numeric value driving Stormlash's effect (speed-boost magnitude, attack-speed-boost magnitude, boost duration, cooldown length, and the Tier 2 threshold) MUST be read through feature 002's `TattooEffectValues` accessor rather than read directly from XML/Def fields at each use site, consistent with the equivalent requirement for every tattoo shipped so far.
- **FR-013**: The system MUST reuse feature 002's existing tier-progression tracker (`TattooTierProgress`) without modification, and MUST reuse feature 003's `CombatExtendedInterop` CE-detection convention for any CE-specific branching this feature needs, rather than re-implementing CE detection inline.
- **FR-014**: Stormlash's ability gizmo MUST be contributed via the existing `IProvidesTattooGizmo` interface and picked up by the existing shared `Patch_Pawn_GetGizmos_TattooGizmos` patch established by feature 004, with no modification to Guardian's Call's own comp, patch, or targeting files.
- **FR-015**: A pawn with both Guardian's Call and Stormlash applied MUST see both abilities' gizmos independently, with each ability's activation, cooldown, and effect state fully independent of the other's.
- **FR-016**: If the Stormlash hediff is removed from a pawn by any means, the system MUST stop applying all of its effects immediately, with no lingering gizmo, boost, or slow immunity.
- **FR-017**: The Stormlash hediff MUST be protected by the existing cross-tattoo removal guard (feature 003, FR-015) with no additional per-tattoo configuration required, since that guard already covers every `TattooMagicDef.appliedHediff` generically.

### Key Entities *(include if feature involves data)*

- **Stormlash Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Stormlash hediff — the activatable movement-speed/attack-speed boost and its cooldown at Tier 1, with a larger boost plus temporary immunity to externally-inflicted movement-slow effects (including CE's suppression-slow) for the boost's duration at Tier 2.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying events (for Stormlash: number of times the ability has been activated) that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is reached — reusing feature 002's tracker.
- **Movement-Slow Immunity**: The Tier 2-only mechanism that suppresses externally-inflicted movement-speed reductions on the wearer for the duration of an active Stormlash boost, including Combat Extended's suppression-slow when CE is loaded.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When a pawn with Stormlash activates the ability, that pawn's movement speed and attack speed both measurably increase for the ability's duration, in both a non-CE game and a CE-loaded game.
- **SC-002**: After the boost's duration elapses, the pawn's movement speed and attack speed measurably return to their pre-activation baseline.
- **SC-003**: The ability cannot be re-activated until its cooldown period has elapsed, verified across repeated activation attempts.
- **SC-004**: A Stormlash-tattooed pawn's progression counter increments only on the wearer's own ability activations, verified across repeated trials.
- **SC-005**: A tattooed pawn's Stormlash automatically upgrades from Tier 1 to Tier 2 once the designated number of activations occurs, with zero additional player action required (no new ritual, no ingredient spend, no slot change).
- **SC-006**: After upgrading to Tier 2, activating the ability grants a measurably larger movement/attack-speed boost than Tier 1's.
- **SC-007**: While a Tier 2 boost is active, an applied movement-slow effect (including Combat Extended's suppression-slow in a CE-loaded game) measurably fails to reduce the wearer's movement speed, where the same effect measurably would reduce an otherwise-identical unbuffed or Tier 1 pawn's movement speed.
- **SC-008**: A Stormlash-tattooed pawn's progression counter and current tier are unchanged immediately after a save/reload compared to immediately before it.
- **SC-009**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat Extended, with zero errors in the RimWorld dev console in either configuration.
- **SC-010**: Stormlash's gizmo is implemented and functions correctly using the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` reuse point from feature 004 with zero modifications to any of Guardian's Call's own files, confirming the reuse point works for a second, genuinely different consumer.
- **SC-011**: A pawn with both Guardian's Call and Stormlash applied has both gizmos function correctly and independently of one another.
- **SC-012**: An attempt to remove the Stormlash hediff through RimWorld's normal hediff-removal API, from any caller other than this mod's own sanctioned path, fails to remove it, consistent with feature 003's existing cross-tattoo removal guard.

## Assumptions

- Speed-boost magnitude (Tier 1 and Tier 2), attack-speed-boost magnitude (Tier 1 and Tier 2), boost duration, cooldown length, and the Tier 1→Tier 2 activation-count threshold are placeholder/balance-pass numbers per PRD §9, exactly as every other tattoo's numeric values have been so far. This feature only needs the mechanism to work correctly for whatever values are configured, not final tuned numbers.
- "Attack speed" (PRD §6) is interpreted as a reduction to the time between attacks/shots — the vanilla stat(s) governing melee and ranged attack cadence, and Combat Extended's equivalent cadence-related stat(s) when CE is loaded — mirroring how other CE-aware tattoos (Ironskin Glyph, Serpent's Eye) map a vanilla concept onto CE's replacement stats rather than a vanilla-only interpretation.
- "Immunity to movement-slow effects" (Tier 2) is interpreted as immunity to externally-inflicted movement-speed-reducing effects — hediffs or mechanics applied to the pawn by something other than the pawn's own baseline state, including Combat Extended's suppression-slow — not as a change to the pawn's intrinsic movement-speed calculation from injuries, pain, encumbrance, or terrain, which continue to apply normally during the boost.
- Per PRD §7's wording ("including Combat Extended's suppression-slow"), the immunity is scoped to the movement-speed-reduction component of CE's suppression mechanic specifically, not to suppression as a whole (e.g. any accuracy or other non-movement effects CE's suppression may also apply, if any, are unaffected).
- A second activation while a previous boost window is still running refreshes (restarts) that boost and its cooldown rather than stacking multiple simultaneous boosts, consistent with Guardian's Call's existing single-instance-per-pawn precedent for its own taunt/buff state.
- This feature covers exactly one tattoo's gameplay effect, Stormlash. The remaining triggered tattoos (Bloodrune, Wraithstep, Berserker's Mark) are out of scope here; wiring up their individual effects is future work that reuses the gizmo/cooldown pattern this feature helps validate.
- PRD §5.1's tattoo-mastery XP track (pawn-wide slot unlocks) remains explicitly out of scope, per the same product decision made for feature 004.
- The value-accessor-driven, override-ready design established by features 002-004 continues to apply here; this feature still does not build a player-facing settings UI (PRD §10 remains deferred).
- The feature 001 ritual/application flow (recipe, skill check, slot enforcement) is unaffected by this feature; this feature only adds gameplay effect behavior to a tattoo that can already be applied.
