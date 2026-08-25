# Feature Specification: Berserker's Mark Tattoo Effect (Fifth and Final Triggered Ability Slice)

**Feature Branch**: `009-berserkers-mark-tattoo-effect`

**Created**: 2026-08-15

**Status**: Draft

**Input**: User description: "Berserker's Mark tattoo effect — feature 009. Triggered ability tattoo (same category as Bloodrune, Stormlash, Wraithstep, Guardian's Call): activating it grants a temporary melee damage boost and raises the pawn's pain threshold, at the cost of reduced defense, for a short duration, then goes on cooldown. Tier 2 (reached via times-activated progression, same mastery pattern as the other triggered tattoos) gives a bigger melee damage boost and reduces (but does not remove) the defense penalty. Must work with and without Combat Extended loaded, consistent with how Stormlash/Serpent's Eye handle CE-aware stats."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Berserker's Mark trades defense for melee damage and staying power on demand (Priority: P1)

A player applies the Berserker's Mark tattoo to a colonist (via the existing ritual station). That colonist gains an activatable ability (a gizmo, like Guardian's Call, Stormlash, Bloodrune, and Wraithstep). Activating it grants the tattooed pawn a temporary boost to melee damage and a higher pain threshold (letting them keep fighting through injuries that would otherwise down them), while also temporarily lowering that pawn's defense — a genuine risk/reward trade rather than a pure buff. After the window ends, all three effects revert and the ability goes on cooldown before it can be used again.

**Why this priority**: This is the entire point of the feature — the base Tier 1 effect the tattoo exists to deliver. It's the smallest slice that proves the ability actually trades defense for melee power and staying power, and every other story here depends on it existing first.

**Independent Test**: Can be fully tested by applying Berserker's Mark to a colonist, activating the ability, and observing a measurable increase to that pawn's melee damage output and pain threshold alongside a measurable decrease to that pawn's defense, all for the ability's duration, followed by a full reversion to baseline and a cooldown period — in both a non-CE game and a CE-loaded game.

**Acceptance Scenarios**:

1. **Given** a pawn with Berserker's Mark applied, **When** the player activates the ability, **Then** that pawn's melee damage and pain threshold both measurably increase and that pawn's defense measurably decreases, all for the ability's duration, in both a non-CE game and a CE-loaded game.
2. **Given** the boost's duration has elapsed, **When** it expires, **Then** the pawn's melee damage, pain threshold, and defense all return to their normal (unbuffed) values with no lingering effect.
3. **Given** the ability was just activated, **When** the player looks at the gizmo, **Then** it shows the ability is on cooldown and cannot be activated again until the cooldown expires.
4. **Given** a pawn without Berserker's Mark applied, **When** combat occurs, **Then** no Berserker's Mark gizmo appears for that pawn and its melee damage, pain threshold, and defense are unaffected, regardless of what other tattoos or effects that pawn may have.

---

### User Story 2 - Berserker's Mark grows stronger the more it's used, and the risk eases at Tier 2 (Priority: P2)

As a Berserker's Mark-tattooed colonist keeps using the ability, the tattoo tracks how many times it's been activated and automatically upgrades itself to a stronger Tier 2 version once enough uses have accumulated — with no extra ritual, ingredients, or tattoo slot required. At Tier 2, activating the ability grants a bigger melee damage boost than Tier 1, and the defense penalty that comes with it is smaller than Tier 1's — better risk/reward, not risk-free.

**Why this priority**: This is the PRD §5.4 tiering mechanic, mirroring the equivalent user story in every tattoo shipped so far. It only makes sense once Tier 1's trade-off (User Story 1) exists and can be observed, so it's the natural second slice.

**Independent Test**: Can be tested independently by activating a Berserker's Mark-tattooed pawn's ability enough times to reach the threshold, then confirming the tattoo's effect strengthens in place — the melee damage boost is bigger, and the defense reduction applied during an active window is smaller in magnitude than Tier 1's, for an otherwise-identical pawn.

**Acceptance Scenarios**:

1. **Given** Tier 1 Berserker's Mark applied with its progression counter below threshold, **When** the player activates the ability, **Then** the counter increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering activation resolves, **Then** Berserker's Mark upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to the pawn's slot usage.
3. **Given** Berserker's Mark is at Tier 2, **When** the wearer activates the ability, **Then** the melee damage boost is measurably larger than Tier 1's, for the same or a configured duration.
4. **Given** Berserker's Mark is at Tier 2, **When** the wearer activates the ability, **Then** the defense reduction applied is measurably smaller in magnitude than Tier 1's defense reduction, while still being a genuine reduction (never zero).
5. **Given** a Tier 2 boost's duration has elapsed, **When** it expires, **Then** the pawn's melee damage, pain threshold, and defense all return to normal with no lingering effect.

---

### User Story 3 - The triggered-ability reuse point closes out the full roster of five (Priority: P3)

Having shipped Guardian's Call, Stormlash, Bloodrune, and Wraithstep as the first four triggered tattoos, Berserker's Mark is the fifth and — per the PRD §6 starter roster — the last triggered ability the mod ships. A developer (or reviewer) confirms that the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` gizmo-contribution mechanism (feature 004) works correctly for this final consumer — a self-buff/self-debuff combination with no targeting behavior at all — without any change to any prior triggered tattoo's own files, that activating it automatically counts toward the pawn's tattoo-mastery progress (feature 006) with no Berserker's Mark-specific code required, and that a pawn wearing several triggered tattoos at once (including Berserker's Mark) has every gizmo work independently.

**Why this priority**: Matters less than the trade-off itself working (User Stories 1-2), but it's what confirms the shared gizmo and mastery infrastructure generalizes across all five triggered tattoos the PRD calls for, completing that half of the starter roster.

**Independent Test**: Can be tested by reviewing that Berserker's Mark's own comp implements `IProvidesTattooGizmo` and is picked up by the existing shared patch with zero modifications to any other triggered tattoo's files, and by applying Berserker's Mark alongside at least one other triggered tattoo on the same pawn and confirming every gizmo appears and functions independently, and that activating Berserker's Mark increases that pawn's tattoo-mastery progress (feature 006) by one.

**Acceptance Scenarios**:

1. **Given** the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` reuse point, **When** Berserker's Mark's own comp implements it, **Then** its gizmo appears and works correctly with no modification to Guardian's Call's, Stormlash's, Bloodrune's, or Wraithstep's own comp, patch, or targeting files.
2. **Given** a pawn with Berserker's Mark and at least one other triggered tattoo applied, **When** the player views that pawn's gizmo row, **Then** every ability's gizmo appears independently, each showing its own correct cooldown/availability state, and activating one does not affect any other's cooldown or availability.
3. **Given** a pawn with Berserker's Mark applied, **When** the wearer activates it, **Then** that pawn's pawn-wide tattoo-mastery progress (feature 006) increases by exactly one, with no Berserker's Mark-specific code added to feature 006's tracker.

---

### Edge Cases

- What happens if the tattooed pawn is downed, killed, or otherwise removed from play while the boost is still active? The boost simply stops mattering (a downed/dead pawn isn't fighting); no special cleanup beyond what already happens when a hediff's owning pawn stops existing is required.
- What happens if the ability is activated again while still on cooldown? The gizmo itself prevents this (RimWorld's standard cooldown-gizmo behavior disables activation until the cooldown expires), matching every prior triggered tattoo's precedent.
- What happens if the ability is activated again while a previous activation's window is still running (e.g. cooldown shorter than duration)? The new activation restarts the boost and cooldown timers from that moment (refresh, not stack) — no stacking of multiple simultaneous Berserker's Mark windows on the same pawn, consistent with every prior triggered tattoo's single-instance-per-pawn precedent.
- What happens when the pain-threshold boost expires while the pawn's current pain level is above the pawn's normal (unboosted) threshold? The pawn is evaluated against the reverted, lower threshold the moment the boost ends, the same way RimWorld's own pain-shock mechanism continuously evaluates any pawn — if that means the pawn goes down right as the window closes, that is the intended risk of using the ability while badly hurt, not a bug to be masked.
- Can activating Berserker's Mark ever down the wearer outright (e.g. if the defense reduction alone, or some other simultaneous effect, would drop the pawn)? No — the defense reduction affects incoming-hit resolution, not the pawn's current health or pain directly; the ability itself never directly downs or damages its own wearer.
- What happens if a pawn has both Berserker's Mark and Ironskin Glyph (a passive tattoo that raises defense) applied at once? Berserker's Mark's temporary defense reduction stacks additively against Ironskin Glyph's (or any other tattoo's) defense-related stat contribution via the same shared stat-offset summation mechanism every other tattoo's stat offset already uses, rather than overriding or suppressing it — a well-armored pawn using Berserker's Mark still ends up less defended than they would be without it, just not from an undefended baseline.
- What happens to the progression counter and tier state across a save and reload? Both MUST persist unchanged, exactly as required of every tattoo shipped so far.
- What happens if some other system removes the Berserker's Mark hediff from the pawn? All of its effects (the gizmo, any active damage boost, pain-threshold increase, or defense reduction) MUST stop applying immediately once the hediff is gone, and the existing cross-tattoo removal guard (feature 003, FR-015) already prevents anything but this mod's own sanctioned path or Developer Mode/God Mode from removing it in the first place.
- What happens if Combat Extended is loaded, then unloaded (or vice versa) between sessions on the same save? The melee damage boost and defense reduction must correctly resolve against whichever stat set (vanilla or CE) matches CE's current presence, without throwing or corrupting saved tier/counter state either way.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Berserker's Mark applied (at either tier), the system MUST grant that pawn an activatable ability (gizmo) that, when used, grants that pawn a temporary increase to melee damage and pain threshold, together with a temporary decrease to defense, all for a fixed duration.
- **FR-002**: After the ability is activated, it MUST become unavailable for a fixed cooldown period before it can be activated again, with the remaining cooldown visible to the player via the gizmo.
- **FR-003**: The melee damage boost and defense reduction MUST resolve correctly against vanilla combat stats in a non-CE game, and against Combat Extended's equivalent replacement stats in a CE-loaded game, per PRD §7 and Constitution Principle I.
- **FR-004**: The system MUST track a per-pawn, per-tattoo progression counter for Berserker's Mark that increments each time the wearer activates the ability, per the "times activated" counter defined for triggered tattoos (PRD §5.4).
- **FR-005**: When Berserker's Mark's progression counter reaches its Tier 2 threshold, the system MUST automatically upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or any change to the pawn's tattoo-slot usage.
- **FR-006**: At Tier 2, the melee damage boost magnitude MUST be larger than Tier 1's, using the same activation mechanism and gizmo.
- **FR-007**: At Tier 2, the defense-reduction magnitude MUST be smaller than Tier 1's while remaining a genuine, non-zero reduction — Tier 2 improves the trade-off, it does not remove it.
- **FR-008**: The pain-threshold increase MUST apply for the same active window as the melee damage boost and defense reduction, reverting to the pawn's normal threshold the instant the window ends, at both tiers.
- **FR-009**: Berserker's Mark's melee-damage and defense stat contributions MUST target the same StatDef(s) — vanilla, and Combat Extended's equivalent(s) when CE is loaded — that any other tattoo's own melee-damage or defense-related bonus would use, so it stacks additively with any other simultaneously-applied tattoo's contribution via the existing shared stat-offset summation mechanism, without Berserker's Mark needing special-case knowledge of any other specific tattoo (e.g. Ironskin Glyph).
- **FR-010**: The progression counter's current value and the tattoo's current tier MUST persist correctly across a game save and reload.
- **FR-011**: None of Berserker's Mark's effects (gizmo, melee damage boost, pain-threshold increase, defense reduction) MUST apply to any pawn that does not currently have Berserker's Mark applied.
- **FR-012**: Berserker's Mark's effect MUST be implemented through the existing `appliedHediff` contract established by feature 001 and reused by features 002-008 (the `TattooDef` → `HediffDef` → Hediff/HediffComp chain), rather than a new or parallel mechanism for granting or tracking tattoo state.
- **FR-013**: Every tunable numeric value driving Berserker's Mark's effect (melee-damage-boost magnitude at each tier, pain-threshold-increase magnitude, defense-reduction magnitude at each tier, boost duration, cooldown length, and the Tier 2 threshold) MUST be read through feature 002's `TattooEffectValues` accessor rather than read directly from XML/Def fields at each use site, consistent with the equivalent requirement for every tattoo shipped so far.
- **FR-014**: The system MUST reuse feature 002's existing tier-progression tracker (`TattooTierProgress`) without modification, and MUST reuse feature 003's `CombatExtendedInterop` CE-detection convention for any CE-specific branching this feature needs, rather than re-implementing CE detection inline.
- **FR-015**: Berserker's Mark's ability gizmo MUST be contributed via the existing `IProvidesTattooGizmo` interface and picked up by the existing shared `Patch_Pawn_GetGizmos_TattooGizmos` patch, with no modification to any other triggered tattoo's own comp, patch, or targeting files.
- **FR-016**: A pawn with Berserker's Mark and any other triggered tattoo(s) applied MUST see every ability's gizmo independently, with each ability's activation, cooldown, and effect state fully independent of the others.
- **FR-017**: If the Berserker's Mark hediff is removed from a pawn by any means, the system MUST stop applying all of its effects immediately, with no lingering gizmo, damage boost, pain-threshold increase, or defense reduction.
- **FR-018**: The Berserker's Mark hediff MUST be protected by the existing cross-tattoo removal guard (feature 003, FR-015) with no additional per-tattoo configuration required, since that guard already covers every `TattooMagicDef.appliedHediff` generically.
- **FR-019**: Activating Berserker's Mark's ability MUST automatically increase the wearer's pawn-wide tattoo-mastery progress (feature 006) by one qualifying event, with no Berserker's Mark-specific code added to feature 006's own files, since that track's gizmo-wrap hook already covers every `IProvidesTattooGizmo` implementer generically.

### Key Entities *(include if feature involves data)*

- **Berserker's Mark Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Berserker's Mark hediff — an activatable window (Tier 1 and Tier 2) that simultaneously boosts melee damage and pain threshold while reducing defense, with a bigger damage boost and a smaller defense penalty at Tier 2.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying events (for Berserker's Mark: number of times the ability has been activated) that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is reached — reusing feature 002's tracker.
- **Risk/Reward Window**: The temporary state, running for a fixed duration after activation, during which melee damage and pain threshold are increased and defense is decreased together, all reverting in unison when the window ends.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When a pawn with Berserker's Mark activates the ability, that pawn's melee damage and pain threshold both measurably increase and that pawn's defense measurably decreases, all for the ability's duration, in both a non-CE game and a CE-loaded game.
- **SC-002**: After the window's duration elapses, the pawn's melee damage, pain threshold, and defense measurably return to their pre-activation baseline.
- **SC-003**: The ability cannot be re-activated until its cooldown period has elapsed, verified across repeated activation attempts.
- **SC-004**: A Berserker's Mark-tattooed pawn's progression counter increments only on the wearer's own ability activations, verified across repeated trials.
- **SC-005**: A tattooed pawn's Berserker's Mark automatically upgrades from Tier 1 to Tier 2 once the designated number of activations occurs, with zero additional player action required (no new ritual, no ingredient spend, no slot change).
- **SC-006**: After upgrading to Tier 2, activating the ability grants a measurably larger melee damage boost, and applies a measurably smaller (but still non-zero) defense reduction, compared to Tier 1's.
- **SC-007**: A Berserker's Mark-tattooed pawn's progression counter and current tier are unchanged immediately after a save/reload compared to immediately before it.
- **SC-008**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat Extended, with zero errors in the RimWorld dev console in either configuration.
- **SC-009**: Berserker's Mark's gizmo is implemented and functions correctly using the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` reuse point with zero modifications to any other triggered tattoo's own files.
- **SC-010**: A pawn with Berserker's Mark and at least one other triggered tattoo applied has every gizmo function correctly and independently of the others.
- **SC-011**: Activating Berserker's Mark increases the wearer's pawn-wide tattoo-mastery progress (feature 006) by exactly one, with zero Berserker's Mark-specific code added to feature 006's own files.
- **SC-012**: An attempt to remove the Berserker's Mark hediff through RimWorld's normal hediff-removal API, from any caller other than this mod's own sanctioned path, fails to remove it, consistent with feature 003's existing cross-tattoo removal guard.

## Assumptions

- "Defense down" (PRD §6) is interpreted as a temporary reduction to the wearer's armor rating — the vanilla armor stats, and Combat Extended's equivalent armor/penetration-resistance stats when CE is loaded — the direct inverse of Ironskin Glyph's armor boost (PRD §6 roster item 5), since that is the most natural reading of a berserker trading protection for offense. Confirming the exact StatDef mapping (and whether any CE-specific "defense" concept beyond armor applies) is deferred to planning's Constitution Check, consistent with how Stormlash's "attack speed" and Bloodrune's CE-applicability were resolved during their own planning rather than in the spec itself.
- "Pain threshold" (PRD §6) refers to RimWorld's existing pain-shock mechanism (the pain level at which a pawn becomes incapable of acting) — raising it temporarily lets the wearer keep fighting through injuries that would otherwise down them. Like Bloodrune's pain-reduction effect, this is expected to be CE-agnostic (CE does not replace or remap this stat), to be confirmed rather than assumed final during planning.
- "Melee damage" boost (PRD §6) is interpreted as a multiplier/offset to the wearer's outgoing melee damage — the vanilla melee-damage-factor stat, and Combat Extended's equivalent melee damage-related stat(s) when CE is loaded — mirroring how other CE-aware tattoos (Ironskin Glyph, Serpent's Eye, Stormlash) map a vanilla combat concept onto CE's replacement stats.
- A second activation while a previous window is still running refreshes (restarts) that window and its cooldown rather than stacking multiple simultaneous windows, consistent with every prior triggered tattoo's existing single-instance-per-pawn precedent.
- Melee-damage-boost magnitude (Tier 1 and Tier 2), pain-threshold-increase magnitude, defense-reduction magnitude (Tier 1 and Tier 2), window duration, cooldown length, and the Tier 1→Tier 2 activation-count threshold are placeholder/balance-pass numbers per PRD §9, exactly as every other tattoo's numeric values have been so far, and small/fast-to-test values are preferred over "plausible-looking" ones per feature 006's research.md R8 precedent. This feature only needs the mechanism to work correctly for whatever values are configured, not final tuned numbers.
- This feature covers exactly one tattoo's gameplay effect, Berserker's Mark — the fifth and final triggered tattoo in the PRD §6 starter roster. The remaining tattoos (Ember Ward, Ironskin Glyph, Starlight Ward, Vampiric Thorn) are all passive and out of scope here; wiring up their individual effects is future work.
- Feature 006's tattoo-mastery XP track requires no changes for this feature — Berserker's Mark's activations are picked up automatically through the existing gizmo-wrap hook, per feature 006's own reuse guarantee already exercised by Stormlash, Bloodrune, and Wraithstep.
- The value-accessor-driven, override-ready design established by features 002-008 continues to apply here; this feature still does not build a player-facing settings UI (PRD §10 remains deferred) beyond the existing debug-logging toggle (feature added 2026-08-15).
- The feature 001 ritual/application flow (recipe, skill check, slot enforcement) is unaffected by this feature; this feature only adds gameplay effect behavior to a tattoo that can already be applied.
