# Feature Specification: Bloodrune Tattoo Effect (Third Triggered Ability Slice)

**Feature Branch**: `007-bloodrune-tattoo-effect`

**Created**: 2026-08-14

**Status**: Draft

**Input**: User description: "Bloodrune tattoo effect. Per PRD.md §6 roster item 3: Triggered tattoo — a short burst of lifesteal / self-heal over a few seconds when activated, on its own cooldown, following the exact same 'click an ability, get a temporary effect, go on cooldown' shape already proven by Guardian's Call (feature 004) and Stormlash (feature 005): implements IProvidesTattooGizmo for its ability button, uses TattooTierProgress for its own 2-tier progression counter (Tier 1 -> Tier 2 on 'times activated' per PRD §6), and reads its tunable numbers (heal amount, duration, cooldown, tier-2 threshold) through TattooEffectValues under its own HediffDef scope key, matching every prior tattoo's convention. Tier 1 (base): a short burst of self-healing (or lifesteal-style HP restoration) over a few seconds after activation. Tier 2 (upgraded, per PRD §6): longer duration and/or a stronger heal, plus a slight reduction to pain while the effect is active. Placeholder numeric values are expected and fine, exact balance is a later pass. This is the third triggered-ability tattoo, and automatically participates in feature 006's tattoo-mastery XP track with zero additional code."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bloodrune heals its wearer in a burst when activated (Priority: P1)

A player applies the Bloodrune tattoo to a colonist (via the existing ritual station). That colonist gains an activatable ability (a gizmo, like Guardian's Call and Stormlash). Activating it restores a portion of the wearer's health gradually over a short window, giving a wounded colonist a way to patch themselves up mid-fight or right after one, without needing a doctor or medicine on hand. After the burst ends, the ability goes on cooldown before it can be used again.

**Why this priority**: This is the entire point of the feature — the base Tier 1 effect the tattoo exists to deliver. It's the smallest slice that proves the ability actually restores health, and every other story here depends on it existing first.

**Independent Test**: Can be fully tested by applying Bloodrune to an injured colonist, activating the ability, and observing that colonist's health measurably improve over the burst's duration, followed by the ability becoming unavailable for a cooldown period.

**Acceptance Scenarios**:

1. **Given** an injured pawn with Bloodrune applied, **When** the player activates the ability, **Then** that pawn's health measurably improves over the burst's duration.
2. **Given** the heal burst's duration has elapsed, **When** it ends, **Then** no further healing occurs from that activation, and the pawn's health simply reflects whatever was restored.
3. **Given** the ability was just activated, **When** the player looks at the gizmo, **Then** it shows the ability is on cooldown and cannot be activated again until the cooldown expires.
4. **Given** a pawn without Bloodrune applied, **When** any amount of time passes or combat occurs, **Then** no Bloodrune gizmo appears for that pawn and its health is unaffected by this feature, regardless of what other tattoos or effects that pawn may have.
5. **Given** a pawn with Bloodrune applied but currently at full health with no injuries, **When** the player activates the ability, **Then** the ability still triggers (cooldown starts) but has nothing to heal, with no error.

---

### User Story 2 - Bloodrune grows stronger the more it's used, and eases pain while it works (Priority: P2)

As a Bloodrune-tattooed colonist keeps using the ability, the tattoo tracks how many times it's been activated and automatically upgrades itself to a stronger Tier 2 version once enough uses have accumulated — with no extra ritual, ingredients, or tattoo slot required. At Tier 2, activating the ability delivers a longer and/or stronger heal than Tier 1, and the wearer also feels less pain for as long as that heal burst is active.

**Why this priority**: This is the PRD §5.4 tiering mechanic, mirroring the equivalent user story in every tattoo shipped so far. It only makes sense once Tier 1's heal (User Story 1) exists and can be observed, so it's the natural second slice.

**Independent Test**: Can be tested independently by activating a Bloodrune-tattooed pawn's ability enough times to reach the threshold, then confirming the tattoo's effect strengthens in place — the heal is bigger and/or lasts longer, and while a burst is active the pawn's pain sensation is measurably reduced compared to an otherwise-identical unbuffed or Tier 1 pawn with the same injuries.

**Acceptance Scenarios**:

1. **Given** Tier 1 Bloodrune applied with its progression counter below threshold, **When** the player activates the ability, **Then** the counter increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering activation resolves, **Then** Bloodrune upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to the pawn's slot usage.
3. **Given** Bloodrune is at Tier 2, **When** the wearer activates the ability, **Then** the heal burst is measurably stronger and/or longer-lasting than Tier 1's.
4. **Given** a Tier 2 Bloodrune heal burst is active, **When** the player compares the wearer's pain level to an otherwise-identical pawn with the same injuries and no active burst, **Then** the Tier 2 pawn's pain is measurably lower for as long as the burst remains active.
5. **Given** a Tier 2 heal burst's duration has elapsed, **When** it ends, **Then** the pawn's pain returns to its normal (unreduced) level with no lingering effect.

---

### User Story 3 - The triggered-ability reuse point proves itself on a third consumer, and feeds mastery progress for free (Priority: P3)

Having shipped Guardian's Call and Stormlash as the first two triggered tattoos, and having since shipped the pawn-wide tattoo-mastery XP track (feature 006), Bloodrune is the third triggered ability and the first one built after that track existed. A developer (or reviewer) confirms that the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` gizmo-contribution mechanism (feature 004) works correctly for a third, still-different kind of ability — a self-heal with no targeting, taunt, or speed behavior at all — without any change to Guardian's Call's or Stormlash's own files, and that activating Bloodrune automatically counts toward that pawn's tattoo-mastery progress (feature 006) with no Bloodrune-specific code required to make that happen.

**Why this priority**: Matters less than the heal itself working (User Stories 1-2), but it's what confirms two separate pieces of shared infrastructure — the gizmo reuse point and the mastery track — both generalize to a genuinely new tattoo without needing to be touched.

**Independent Test**: Can be tested by reviewing that Bloodrune's own comp implements `IProvidesTattooGizmo` and is picked up by the existing shared patch with zero modifications to Guardian's Call's or Stormlash's own files, and by activating Bloodrune on a pawn and confirming that pawn's tattoo-mastery progress (feature 006) increases by one, the same way it would for Guardian's Call or Stormlash.

**Acceptance Scenarios**:

1. **Given** the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` reuse point, **When** Bloodrune's own comp implements it, **Then** Bloodrune's gizmo appears and works correctly with no modification to Guardian's Call's or Stormlash's comp, patch, or any other prior feature's file.
2. **Given** a pawn with Bloodrune and at least one other triggered tattoo applied, **When** the player views that pawn's gizmo row, **Then** every ability's gizmo appears independently, each showing its own correct cooldown/availability state.
3. **Given** a pawn with Bloodrune applied, **When** the wearer activates it, **Then** that pawn's pawn-wide tattoo-mastery progress (feature 006) increases by exactly one, with no Bloodrune-specific code added to feature 006's tracker.

---

### Edge Cases

- What happens if the tattooed pawn is downed, killed, or otherwise removed from play while a heal burst is still active? The burst simply stops mattering (nothing left to heal or a pawn no longer being tracked); no special cleanup beyond what already happens when a hediff's owning pawn stops existing is required.
- What happens if the ability is activated again while still on cooldown? The gizmo itself prevents this (RimWorld's standard cooldown-gizmo behavior disables activation until the cooldown expires), matching Guardian's Call's and Stormlash's precedent.
- What happens if the ability is activated again while a previous activation's heal burst is still running (e.g. cooldown shorter than duration)? The new activation restarts the burst and cooldown timers from that moment (refresh, not stack) — no stacking of multiple simultaneous Bloodrune bursts on the same pawn, consistent with Guardian's Call's and Stormlash's single-instance-per-pawn precedent.
- What happens if the wearer has no injuries (or is already at full, unharmed health) when the burst triggers? The burst still starts and the ability still goes on cooldown; it simply has nothing to restore, with no error (User Story 1, Acceptance Scenario 5).
- Does the heal restore only HP loss from injuries, or does it also address other health complications (e.g. blood loss, toxic buildup, disease)? Out of scope for this feature to address beyond ordinary injury healing — this feature restores injury-based health loss the way RimWorld's own healing mechanisms do; it is not a cure for diseases, addictions, or other non-injury conditions.
- What happens to the progression counter and tier state across a save and reload? Both MUST persist unchanged, exactly as required of every tattoo shipped so far.
- What happens if some other system removes the Bloodrune hediff from the pawn? All of its effects (the gizmo, any active heal burst, any active Tier 2 pain reduction) MUST stop applying immediately once the hediff is gone, and the existing cross-tattoo removal guard (feature 003, FR-015) already prevents anything but this mod's own sanctioned path or Developer Mode/God Mode from removing it in the first place.
- Does this tattoo need any Combat Extended-specific interaction? Unlike Ironskin Glyph, Serpent's Eye, or Stormlash, healing and pain are not stats Combat Extended replaces or remaps with its own equivalents — this is expected to be a case where Constitution Principle I is not applicable (like Frost Sigil's core slow effect), to be confirmed during planning rather than assumed final here.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Bloodrune applied (at either tier), the system MUST grant that pawn an activatable ability (gizmo) that, when used, restores a portion of that pawn's health gradually over a fixed duration.
- **FR-002**: After the ability is activated, it MUST become unavailable for a fixed cooldown period before it can be activated again, with the remaining cooldown visible to the player via the gizmo.
- **FR-003**: The system MUST track a per-pawn, per-tattoo progression counter for Bloodrune that increments each time the wearer activates the ability, per the "times activated" counter defined for triggered tattoos (PRD §5.4).
- **FR-004**: When Bloodrune's progression counter reaches its Tier 2 threshold, the system MUST automatically upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or any change to the pawn's tattoo-slot usage.
- **FR-005**: At Tier 2, the heal burst's magnitude and/or duration MUST be greater than Tier 1's, using the same activation mechanism and gizmo.
- **FR-006**: At Tier 2 only, for the duration of an active heal burst, the wearer's pain MUST be reduced below what it would otherwise be from the same injuries, returning to normal once the burst ends.
- **FR-007**: The progression counter's current value and the tattoo's current tier MUST persist correctly across a game save and reload.
- **FR-008**: None of Bloodrune's effects (gizmo, heal burst, Tier 2 pain reduction) MUST apply to any pawn that does not currently have Bloodrune applied.
- **FR-009**: Bloodrune's effect MUST be implemented through the existing `appliedHediff` contract established by feature 001 and reused by features 002-005 (the `TattooDef` → `HediffDef` → Hediff/HediffComp chain), rather than a new or parallel mechanism for granting or tracking tattoo state.
- **FR-010**: Every tunable numeric value driving Bloodrune's effect (heal amount/rate, burst duration, cooldown length, pain-reduction magnitude, and the Tier 2 threshold) MUST be read through feature 002's `TattooEffectValues` accessor rather than read directly from XML/Def fields at each use site, consistent with the equivalent requirement for every tattoo shipped so far.
- **FR-011**: The system MUST reuse feature 002's existing tier-progression tracker (`TattooTierProgress`) without modification for Bloodrune's own tier state.
- **FR-012**: Bloodrune's ability gizmo MUST be contributed via the existing `IProvidesTattooGizmo` interface and picked up by the existing shared `Patch_Pawn_GetGizmos_TattooGizmos` patch established by feature 004, with no modification to Guardian's Call's or Stormlash's own comp, patch, or targeting files.
- **FR-013**: A pawn with Bloodrune and any other triggered tattoo(s) applied MUST see every ability's gizmo independently, with each ability's activation, cooldown, and effect state fully independent of the others.
- **FR-014**: If the Bloodrune hediff is removed from a pawn by any means, the system MUST stop applying all of its effects immediately, with no lingering gizmo, heal burst, or pain reduction.
- **FR-015**: The Bloodrune hediff MUST be protected by the existing cross-tattoo removal guard (feature 003, FR-015) with no additional per-tattoo configuration required, since that guard already covers every `TattooMagicDef.appliedHediff` generically.
- **FR-016**: Activating Bloodrune's ability MUST automatically increase the wearer's pawn-wide tattoo-mastery progress (feature 006) by one qualifying event, with no Bloodrune-specific code added to feature 006's own files, since that track's gizmo-wrap hook (feature 006, research.md R1) already covers every `IProvidesTattooGizmo` implementer generically.

### Key Entities *(include if feature involves data)*

- **Bloodrune Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Bloodrune hediff — the activatable heal burst and its cooldown at Tier 1, with a stronger/longer burst plus temporary pain reduction for the burst's duration at Tier 2.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying events (for Bloodrune: number of times the ability has been activated) that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is reached — reusing feature 002's tracker.
- **Heal Burst / Pain Reduction Window**: The temporary state, running for a fixed duration after activation, during which health is gradually restored (both tiers) and pain is reduced (Tier 2 only).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When an injured pawn with Bloodrune activates the ability, that pawn's health measurably improves over the burst's duration.
- **SC-002**: The ability cannot be re-activated until its cooldown period has elapsed, verified across repeated activation attempts.
- **SC-003**: A Bloodrune-tattooed pawn's progression counter increments only on the wearer's own ability activations, verified across repeated trials.
- **SC-004**: A tattooed pawn's Bloodrune automatically upgrades from Tier 1 to Tier 2 once the designated number of activations occurs, with zero additional player action required (no new ritual, no ingredient spend, no slot change).
- **SC-005**: After upgrading to Tier 2, activating the ability delivers a measurably stronger and/or longer-lasting heal than Tier 1's.
- **SC-006**: While a Tier 2 heal burst is active, the wearer's pain is measurably lower than an otherwise-identical pawn with the same injuries and no active burst, returning to normal once the burst ends.
- **SC-007**: A Bloodrune-tattooed pawn's progression counter and current tier are unchanged immediately after a save/reload compared to immediately before it.
- **SC-008**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat Extended, with zero errors in the RimWorld dev console in either configuration.
- **SC-009**: Bloodrune's gizmo is implemented and functions correctly using the `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos` reuse point with zero modifications to any of Guardian's Call's or Stormlash's own files.
- **SC-010**: A pawn with Bloodrune and at least one other triggered tattoo applied has every gizmo function correctly and independently of the others.
- **SC-011**: Activating Bloodrune increases the wearer's pawn-wide tattoo-mastery progress (feature 006) by exactly one, with zero Bloodrune-specific code added to feature 006's own files.
- **SC-012**: An attempt to remove the Bloodrune hediff through RimWorld's normal hediff-removal API, from any caller other than this mod's own sanctioned path, fails to remove it, consistent with feature 003's existing cross-tattoo removal guard.

## Assumptions

- "Lifesteal / self-heal" (PRD §6) is interpreted as a self-targeted heal-over-time burst triggered on activation, not contingent on the wearer dealing damage to a target — consistent with the tattoo's progression counter being "times activated" (PRD §5.4) rather than a hit-based counter like Frost Sigil's or Serpent's Eye's. "Lifesteal" is read as flavor framing for the effect, not a literal drain-HP-from-an-enemy mechanic.
- The heal burst restores ordinary injury-based health loss (RimWorld's standard healing mechanism), not blood loss, disease, toxic buildup, addictions, or other non-injury health complications — out of scope per Edge Cases.
- Tier 2's pain reduction is scoped to the active heal-burst window only, matching the temporary-buff-during-active-window shape already used by Guardian's Call's Tier 2 armor bonus and Stormlash's Tier 2 slow immunity, rather than being a permanent or always-on reduction.
- Healing and pain are not stats Combat Extended replaces or remaps the way it does armor, accuracy, or movement speed — this feature is expected to have no CE-specific branching needed for its core effect, similar to Frost Sigil's core slow effect being CE-agnostic; this is confirmed (not assumed final) during planning's Constitution Check.
- Heal amount/rate, burst duration, cooldown length, pain-reduction magnitude, and the Tier 1→Tier 2 activation-count threshold are placeholder/balance-pass numbers per PRD §9, exactly as every other tattoo's numeric values have been so far, and small/fast-to-test values are preferred over "plausible-looking" ones per feature 006's research.md R8 precedent. This feature only needs the mechanism to work correctly for whatever values are configured, not final tuned numbers.
- A second activation while a previous heal burst is still running refreshes (restarts) that burst and its cooldown rather than stacking multiple simultaneous bursts, consistent with Guardian's Call's and Stormlash's existing single-instance-per-pawn precedent.
- This feature covers exactly one tattoo's gameplay effect, Bloodrune. The remaining tattoos (Ember Ward, Ironskin Glyph, Wraithstep, Berserker's Mark, Starlight Ward, Vampiric Thorn) are out of scope here; wiring up their individual effects is future work.
- Feature 006's tattoo-mastery XP track requires no changes for this feature — Bloodrune's activations are picked up automatically through the existing gizmo-wrap hook, per feature 006's own FR-015/FR-017 reuse guarantee.
- The value-accessor-driven, override-ready design established by features 002-006 continues to apply here; this feature still does not build a player-facing settings UI (PRD §10 remains deferred).
- The feature 001 ritual/application flow (recipe, skill check, slot enforcement) is unaffected by this feature; this feature only adds gameplay effect behavior to a tattoo that can already be applied.
