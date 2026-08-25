# Feature Specification: Vampiric Thorn Tattoo Effect (Sixth Passive Tattoo, Melee Lifesteal Slice)

**Feature Branch**: `010-vampiric-thorn-tattoo-effect`

**Created**: 2026-08-15

**Status**: Draft

**Input**: User description: "Vampiric Thorn tattoo effect — the sixth passive tattoo and final melee-focused passive from the PRD §6 starter roster (item #10). Per docs/PRD.md: a passive tattoo whose Tier 1 effect is that melee attacks lifesteal a small amount of HP back to the wearer; its progression counter is melee hits landed; Tier 2 upgrades to a higher lifesteal percentage plus a brief heal-over-time on a killing blow. Follows the same passive-tattoo shape already proven twice (Frost Sigil, feature 002; Serpent's Eye, feature 003) — a permanent, always-on stat/effect contribution while the tattoo is applied, gated by tier, using the existing IProvidesTattooStatOffset/StatPart_TattooEffectOffset reuse point from feature 002 wherever the effect maps onto an ordinary StatDef, plus whatever additional mechanism is needed for the parts that don't (the lifesteal-on-hit and heal-over-time-on-kill effects are not simple StatDef offsets, so this will need research into how to hook into melee-damage-dealt resolution — likely a Harmony patch, similar in spirit to how Bloodrune's self-heal or other triggered tattoos' effects were implemented, but as a passive/always-active hook on the wearer's own melee attacks rather than an activatable ability)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Vampiric Thorn heals its wearer a little with every melee hit landed (Priority: P1)

A player applies the Vampiric Thorn tattoo to a colonist (via the existing ritual station). From that point on,
every time that colonist lands a melee attack that deals damage — in a fistfight, with a knife, in a brawl
started by someone else, against a hostile or even an ally — the colonist recovers a small amount of their own
health, mirroring a wound closing slightly. A colonist with no injuries of their own simply doesn't visibly
benefit from a given hit (there's nothing to heal), which is expected, not a bug.

**Why this priority**: This is the entire point of the feature — turning Vampiric Thorn from an inert stub
Hediff into a tattoo that does something, mirroring what features 002 and 003 did for Frost Sigil and Serpent's
Eye. It's the smallest slice that proves this tattoo's Tier 1 effect works, and every other story here depends
on it.

**Independent Test**: Can be fully tested by applying Vampiric Thorn to an injured colonist, having them land a
melee hit (e.g. via Dev Mode-assisted combat or a spar), and observing a measurable reduction in the severity of
their own worst open wound immediately after the hit resolves.

**Acceptance Scenarios**:

1. **Given** an injured pawn with Vampiric Thorn applied, **When** that pawn lands a melee attack that deals
   damage, **Then** the pawn's own most severe currently-open wound measurably heals by a small amount
   immediately after the hit resolves.
2. **Given** an uninjured pawn with Vampiric Thorn applied, **When** that pawn lands a melee attack that deals
   damage, **Then** no error occurs and nothing observably changes, since there is no wound for the lifesteal to
   heal.
3. **Given** a pawn with Vampiric Thorn applied, **When** that pawn's melee attack misses, is fully
   deflected/blocked (zero damage dealt), or the pawn is instead the one being struck, **Then** no lifesteal
   healing occurs.
4. **Given** a pawn without Vampiric Thorn applied, **When** combat occurs, **Then** none of this lifesteal
   behavior applies to that pawn, regardless of what other tattoos or effects it may have.

---

### User Story 2 - Vampiric Thorn grows stronger the more its wearer lands melee hits, and finishing a foe grants extra recovery (Priority: P2)

As a Vampiric Thorn-tattooed colonist keeps landing melee hits, the tattoo tracks how many have connected and
automatically upgrades itself to a stronger Tier 2 version once enough have landed — with no extra ritual,
ingredients, or tattoo slot required. At Tier 2, each landed hit heals a larger amount than at Tier 1, and
striking the killing blow on a target in melee grants the wearer a brief additional period of accelerated
recovery beyond the immediate per-hit lifesteal.

**Why this priority**: This is the PRD §5.4 tiering mechanic, mirroring User Story 2 of features 002 and 003. It
only makes sense once Tier 1's effect (User Story 1) exists and can be measured, so it's the natural second
slice.

**Independent Test**: Can be tested independently by having a Vampiric Thorn-tattooed pawn land enough melee
hits to reach the threshold, then confirming the tattoo's effect strengthens in place, and separately by having
that pawn land a killing blow and confirming the extra recovery window follows it.

**Acceptance Scenarios**:

1. **Given** Tier 1 Vampiric Thorn applied with its progression counter below threshold, **When** a melee attack
   by the wearer lands on a target, **Then** the counter increments by one.
2. **Given** a melee attack by the wearer misses or is fired by someone else at the wearer, **When** that event
   resolves, **Then** the counter does NOT increment — only the wearer's own landed melee hits count.
3. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering hit resolves, **Then**
   Vampiric Thorn upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to the
   pawn's slot usage.
4. **Given** Vampiric Thorn is at Tier 2, **When** the wearer lands a melee hit, **Then** the resulting lifesteal
   heal is measurably larger than an otherwise-identical Tier 1 hit would produce.
5. **Given** Vampiric Thorn is at Tier 2, **When** the wearer's melee attack kills its target, **Then** the
   wearer additionally recovers health over a brief period following the kill, beyond that single hit's own
   lifesteal amount.
6. **Given** Vampiric Thorn is at Tier 1 (not yet upgraded), **When** the wearer's melee attack kills its
   target, **Then** only the normal Tier 1 per-hit lifesteal applies — no extra post-kill recovery window,
   since that bonus is Tier 2-exclusive per PRD §6.

---

### User Story 3 - The effect reuses existing infrastructure, and adds the one reuse point that's genuinely missing (Priority: P3)

Having shipped Vampiric Thorn's effect, a developer picking up a future tattoo can reuse feature 002's original
infrastructure (the value accessor, the tier-progression tracker) and whatever this feature adds for "the
wearer's own melee attack landed" and "the wearer's own melee attack killed its target" — without modifying
Vampiric Thorn's own effect logic.

**Why this priority**: This is the same "establish the pattern" rationale features 002 and 003 had for their own
tattoos. It matters less than Vampiric Thorn actually working (User Stories 1-2) because there's nothing proven
to reuse until Tier 1/Tier 2 work correctly for this tattoo.

**Scope note**: Feature 002 established a reuse point for "this pawn was hit in melee" (the struck pawn's own
perspective). Feature 003 established the mirror-image reuse point for "this pawn's own ranged shot landed" (the
attacker's perspective). Neither covers "this pawn's own melee attack landed" (the attacker's perspective, for a
melee hit specifically) — that combination is new to this feature, since no prior tattoo needed it. This feature
is expected to add that one new reuse point, plus whatever's needed for "the wearer's own melee attack was a
killing blow," reusing everything else (the value accessor, the tier tracker, and the existing wound-healing
approach already proven by a prior triggered tattoo's own self-heal effect) rather than inventing parallel
mechanisms.

**Independent Test**: Can be tested by reviewing whatever new reuse point(s) this feature adds and confirming a
hypothetical future tattoo needing "my own melee hit landed" or "my own melee attack killed something" could
adopt them via its own data/configuration, without editing Vampiric Thorn-specific code.

**Acceptance Scenarios**:

1. **Given** the value accessor and tier-progression tracker features 002/003 already established, **When**
   Vampiric Thorn needs a tiered progression counter, **Then** it reuses that directly, with no modification to
   existing code.
2. **Given** a new reuse point this feature adds for "the wearer's own melee attack landed," **When** a future
   tattoo needs the same kind of counter (e.g. a tattoo themed around melee combat), **Then** it can reuse that
   point with its own configuration, without duplicating Vampiric Thorn's detection logic.
3. **Given** the wound-healing approach a prior triggered tattoo's own self-heal effect already established,
   **When** Vampiric Thorn needs to heal the wearer's own injuries, **Then** it reuses that same underlying
   approach rather than inventing a second, parallel way to heal a pawn.

---

### Edge Cases

- What happens if the wearer's melee attack hits a non-hostile target (a colony ally, an animal, a sparring
  partner)? The lifesteal still applies and the counter still increments — PRD §6 says "melee attacks" and
  "melee hits landed" generically, with no target-relationship restriction, mirroring how Frost Sigil's and
  Serpent's Eye's counters also don't care who the target is.
- What happens if the wearer's melee attack is deflected/blocked entirely (zero damage dealt)? It MUST NOT
  trigger lifesteal healing and MUST NOT increment the counter, mirroring Serpent's Eye's "only
  successful/consequential events count" convention.
- What happens if the wearer has no open injuries at the moment a qualifying hit lands? The lifesteal simply has
  nothing to heal and produces no observable change — this is expected behavior, not an error, not a "wasted"
  proc that should be deferred or banked for later.
- What happens if the wearer's lifesteal-worthy hit would heal more than their worst wound's remaining severity?
  Mirroring the existing wound-healing approach this feature reuses, any leftover healing amount rolls onto the
  wearer's next-most-severe open wound rather than being lost, the same way the reused mechanism already handles
  a wound closing mid-heal.
- What happens if the wearer's melee attack simultaneously deals the killing blow AND is itself a normal landed
  hit? Both the Tier 1/Tier 2 per-hit lifesteal and (at Tier 2) the post-kill recovery window apply together —
  the kill bonus is additional recovery, not a replacement for the ordinary per-hit lifesteal.
- What happens if the wearer lands a second killing blow while an earlier kill's post-kill recovery window (Tier
  2) is still running? The window refreshes/restarts from the new kill rather than stacking two simultaneous
  recovery windows, consistent with every existing triggered tattoo's "refresh, not stack" precedent for
  time-boxed effects on the same pawn.
- What happens to the progression counter, tier state, and any in-progress post-kill recovery window across a
  save and reload? All of it MUST persist unchanged, exactly as required of every prior tattoo.
- What happens if some other system removes the Vampiric Thorn hediff from the pawn? All of its effects (lifesteal
  on hit, the post-kill recovery window if Tier 2) MUST stop applying immediately once the hediff is gone; the
  existing cross-tattoo removal guard (feature 003, FR-015) already prevents anything but this mod's own
  sanctioned path or Developer Mode/God Mode from removing it in the first place, with no Vampiric-Thorn-specific
  configuration required.
- What happens if the wearer is downed or dies in the same tick their melee attack resolves (e.g. a mutual
  killing blow)? No lifesteal or kill-bonus healing is required to apply to a dead pawn — there's no meaningful
  pawn state left to update.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Vampiric Thorn applied (at either tier), the system MUST heal a small amount of
  that pawn's own most-severe currently-open injury every time a melee attack made by that pawn lands on a
  target and deals damage.
- **FR-002**: The system MUST track a per-pawn, per-tattoo progression counter for Vampiric Thorn that increments
  each time a melee attack made by the wearer lands on a target (deals damage), per the counter defined in PRD
  §5.4/§6 ("melee hits landed").
- **FR-003**: When Vampiric Thorn's progression counter reaches its Tier 2 threshold, the system MUST
  automatically upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or
  any change to the pawn's tattoo-slot usage.
- **FR-004**: At Tier 2, the per-hit lifesteal amount MUST be larger than Tier 1's, per PRD §6.
- **FR-005**: At Tier 2 only, when a melee attack made by the wearer kills its target, the system MUST grant the
  wearer additional healing over a brief period following the kill, on top of that hit's own per-hit lifesteal.
  Tier 1 MUST NOT grant this additional post-kill recovery.
- **FR-006**: A second killing blow landed while an earlier kill's post-kill recovery window is still running
  MUST refresh (restart) that window rather than stacking multiple simultaneous recovery windows on the same
  pawn.
- **FR-007**: The progression counter's current value, the tattoo's current tier, and any in-progress post-kill
  recovery window MUST persist correctly across a game save and reload.
- **FR-008**: None of Vampiric Thorn's effects (per-hit lifesteal, post-kill recovery) MUST apply to any pawn
  that does not currently have Vampiric Thorn applied.
- **FR-009**: The system MUST NOT trigger lifesteal healing or increment the progression counter for a melee
  attack that misses, is fully deflected/blocked for zero damage, or is landed by anyone other than the Vampiric
  Thorn wearer.
- **FR-010**: If the wearer has no open injuries at the moment a qualifying hit lands, the system MUST simply
  produce no observable healing effect, without error and without banking or deferring the unused amount.
- **FR-011**: Vampiric Thorn's effect MUST be implemented through the existing `appliedHediff` contract
  established by feature 001 and reused by features 002/003 (the `TattooDef` → `HediffDef` → Hediff/HediffComp
  chain), rather than a new or parallel mechanism for granting or tracking tattoo state.
- **FR-012**: Every tunable numeric value driving Vampiric Thorn's effect (the Tier 1 and Tier 2 per-hit
  lifesteal amounts, the Tier 2 post-kill recovery amount and duration, and the Tier 1→Tier 2 progression
  threshold) MUST be read through feature 002's `TattooEffectValues` accessor rather than read directly from
  XML/Def fields at each use site, consistent with every tattoo shipped so far.
- **FR-013**: The system MUST reuse feature 002's existing value accessor and tier-progression tracker without
  modification. Where existing reuse points (the stat-offset contract, the on-melee-hit-taken contract, the
  on-ranged-hit-landed contract) do not cover something Vampiric Thorn needs — specifically, reacting to the
  wearer's own landed melee attack, and to the wearer's own melee attack killing its target — the system MUST add
  new, similarly generic reuse point(s) rather than hardcoding Vampiric-Thorn-specific detection logic into a
  one-off location.
- **FR-014**: The system MUST reuse the existing wound-healing approach a prior triggered tattoo's own self-heal
  effect already established (healing the wearer's own most-severe open injury first, moving to the next once
  fully healed) rather than introducing a second, parallel mechanism for healing a pawn's injuries.
- **FR-015**: If the Vampiric Thorn hediff is removed from a pawn by any means, the system MUST stop applying all
  of its effects immediately, with no lingering per-hit lifesteal or post-kill recovery window.
- **FR-016**: Vampiric Thorn's effect requires no Combat Extended-specific branching — healing a pawn's own
  injury severity is a core health-system concept CE does not replace or reinterpret, mirroring the equivalent
  finding for Bloodrune's own self-heal effect (feature 007). This MUST be confirmed against CE's own loaded
  content during planning rather than assumed final.

### Key Entities *(include if feature involves data)*

- **Vampiric Thorn Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Vampiric Thorn
  hediff — the Tier 1 per-hit lifesteal, and the stronger Tier 2 variant plus the post-kill recovery window.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying events (for Vampiric Thorn: the
  wearer's own melee hits that land) that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is
  reached — the same kind of counter features 002/003 established, reusing their tracker.
- **"Own Melee Hit Landed" Reuse Point**: The new, tattoo-agnostic detection mechanism this feature adds for
  "this pawn's own melee attack just landed on a target" — the melee counterpart to feature 003's "own ranged hit
  landed" reuse point, intended for reuse by future melee-themed tattoo effects.
- **Post-Kill Recovery Window**: The Tier 2-exclusive, time-boxed state that grants additional healing over a
  brief period after the wearer's melee attack kills its target — refreshed (not stacked) by a subsequent kill
  while still active.
- **Tattoo Hediff Removal Guard**: Existing cross-tattoo infrastructure (feature 003, FR-015) that already blocks
  Vampiric Thorn's hediff from unsanctioned removal with zero additional configuration needed here.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An injured pawn with Vampiric Thorn applied shows a measurable reduction in their own worst open
  wound's severity immediately after landing a melee hit that deals damage.
- **SC-002**: A Vampiric Thorn-tattooed pawn's progression counter increments only on the wearer's own landed
  melee hits — not on misses, not on fully-deflected hits, and not on hits landed by other pawns — verified
  across repeated trials.
- **SC-003**: A tattooed pawn's Vampiric Thorn automatically upgrades from Tier 1 to Tier 2 once the designated
  number of landed melee hits occurs, with zero additional player action required (no new ritual, no ingredient
  spend, no slot change).
- **SC-004**: After upgrading to Tier 2, the same pawn's per-hit lifesteal is measurably stronger than at Tier 1.
- **SC-005**: At Tier 2, a killing blow landed by the wearer produces measurably more total healing over the
  following period than an otherwise-identical non-killing hit; at Tier 1, a killing blow produces no more
  healing than an ordinary landed hit.
- **SC-006**: A Vampiric Thorn-tattooed pawn's progression counter, current tier, and any in-progress post-kill
  recovery window are unchanged immediately after a save/reload compared to immediately before it.
- **SC-007**: A future tattoo needing to react to "the wearer's own melee attack landing" can be implemented
  using the reuse point this feature adds, without modifying any of Vampiric Thorn's own effect code.
- **SC-008**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat
  Extended, with zero errors in either configuration.
- **SC-009**: An attempt to remove the Vampiric Thorn hediff through RimWorld's normal hediff-removal API, from
  any caller other than this mod's own sanctioned path, fails to remove it; the same removal action succeeds when
  performed via Developer Mode/God Mode's health-tab tooling.

## Assumptions

- "Lifesteal a small amount of HP" is interpreted the same way Bloodrune's (feature 007) self-heal effect already
  interprets "healing" for this mod — RimWorld has no single scalar "HP" value to add to directly; instead, the
  wearer's own most-severe currently-open `Hediff_Injury` is healed by some amount, moving to the next-most-severe
  once one closes, exactly mirroring the wound-healing approach that effect already established and proved
  CE-agnostic.
- Exact numeric values — the Tier 1 and Tier 2 per-hit lifesteal amounts, the Tier 2 post-kill recovery amount and
  duration, and the Tier 1→Tier 2 progression-counter threshold — are placeholder/balance-pass numbers per PRD
  §9, exactly as every tattoo shipped so far.
- "Melee hits landed" is interpreted as any of the wearer's own melee attacks (unarmed or weapon-based) that deal
  damage to a target, mirroring Frost Sigil's and Serpent's Eye's "only successful/consequential events count"
  convention.
- "A killing blow" is interpreted as the specific melee attack instance that directly causes its target's death,
  not a downed state or a later, unrelated death from an existing wound.
- This feature covers exactly one tattoo's *gameplay effect*, Vampiric Thorn. The other 3 remaining passive
  tattoos (Ember Ward, Ironskin Glyph, Starlight Ward) remain inert stub Hediffs after this feature ships; wiring
  up their individual effects is out of scope here.
- The tattoo hediff removal guard (feature 003, FR-015) already covers every `TattooMagicDef.appliedHediff`
  generically, including Vampiric Thorn, with no further work needed in this feature.
- The value-accessor-driven, override-ready design established by Frost Sigil (feature 002 FR-012) continues to
  apply here; this feature still does not build a player-facing settings UI (PRD §10 remains deferred).
- The feature 001 ritual/application flow (recipe, skill check, slot enforcement) is unaffected by this feature;
  this feature only adds gameplay effect behavior to a tattoo that can already be applied.
