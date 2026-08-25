# Feature Specification: Ember Ward Tattoo Effect (Seventh Passive Tattoo, Burn Resistance Slice)

**Feature Branch**: `011-ember-ward-tattoo-effect`

**Created**: 2026-08-16

**Status**: Draft

**Input**: User description: "Ember Ward tattoo effect — a passive tattoo granting heat resistance and burn-damage
protection (PRD §6 row 1). Defs/TattooDefs/EmberWard.xml is already scaffolded with placeholder ingredients/
workAmount and appliedHediff TattooMagic_Hediff_EmberWard, but no HediffComp yet exists. Tier 1 (base): +heat
resistance, small reduction to fire/burn damage taken. Progression counter (per PRD §5.4, passive-tattoo type):
increments on instances of fire/burn damage absorbed while equipped. Tier 2 (upgraded, in place, no new ritual/
slot): further burn damage reduction, plus a chance to fully ignore a burn instance entirely. Exact tier-2
threshold and numeric tuning are placeholders pending the PRD §9 balance pass, same as the other 8 already-
implemented tattoos. Follow the same pattern as the previously implemented passive tattoos (Frost Sigil feature
002, Vampiric Thorn feature 010) for HediffComp structure and tier-progress wiring."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ember Ward makes its wearer harder to burn (Priority: P1)

A player applies the Ember Ward tattoo to a colonist (via the existing ritual station). From that point on, that
colonist runs measurably more heat-hardy than before, and whenever they take fire/burn damage — from a burning
building, a flamethrower, a fire-breathing incident, or simply standing too close to a blaze — the damage they
actually take is reduced compared to an otherwise-identical untattooed pawn.

**Why this priority**: This is the entire point of the feature — turning Ember Ward from an inert stub Hediff
into a tattoo that does something, mirroring what features 002, 003, and 010 did for their own tattoos. It's the
smallest slice that proves this tattoo's Tier 1 effect works, and every other story here depends on it.

**Independent Test**: Can be fully tested by applying Ember Ward to a colonist, comparing their heat-resistance
stat against an untattooed pawn, then exposing both to an identical fire/burn source and comparing the damage
each actually takes.

**Acceptance Scenarios**:

1. **Given** a pawn with Ember Ward applied, **When** the pawn's heat-resistance stat is inspected, **Then** it
   reflects the tattoo's Tier 1 bonus, and reverts to normal if the tattoo were ever absent.
2. **Given** a pawn with Ember Ward applied takes fire/burn damage, **When** that damage is resolved, **Then**
   the amount actually applied is reduced compared to an identical hit against an untattooed pawn.
3. **Given** a pawn with Ember Ward applied takes damage from a non-fire/burn source (blunt, sharp, etc.),
   **When** that damage is resolved, **Then** no reduction from Ember Ward applies and the hit resolves normally.
4. **Given** a pawn without Ember Ward applied, **When** they take fire/burn damage, **Then** no heat-resistance
   bonus or damage reduction from this tattoo is present, regardless of what other tattoos or effects they may
   have.

---

### User Story 2 - Ember Ward grows stronger the more burn damage its wearer survives (Priority: P2)

As an Ember Ward-tattooed colonist keeps absorbing fire/burn damage, the tattoo tracks how many qualifying
instances have occurred and automatically upgrades itself to a stronger Tier 2 version once enough have happened
— with no extra ritual, ingredients, or tattoo slot required. At Tier 2, burn damage reduction is stronger than
Tier 1, and a burn instance now has a chance to be fully ignored (zero damage taken from that instance).

**Why this priority**: This is the PRD §5.4 tiering mechanic, mirroring User Story 2 of every prior passive
tattoo. It only makes sense once Tier 1's effect (User Story 1) exists and can be measured, so it's the natural
second slice.

**Independent Test**: Can be tested independently by exposing an Ember Ward-tattooed pawn to enough fire/burn
damage instances to reach the threshold, then confirming the tattoo's effect strengthens in place without any
player action beyond survival, and separately confirming the full-ignore chance is observable across enough
trials at Tier 2.

**Acceptance Scenarios**:

1. **Given** Tier 1 Ember Ward applied with its progression counter below threshold, **When** the wearer takes
   fire/burn damage that is at least partially absorbed by the tattoo's reduction, **Then** the counter
   increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering instance resolves,
   **Then** Ember Ward upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to the
   pawn's slot usage.
3. **Given** Ember Ward is at Tier 2, **When** its heat resistance and burn-damage reduction are compared to
   Tier 1, **Then** both are measurably stronger, and a chance to fully ignore a burn instance (take zero damage
   from it) is present where it was not at Tier 1.
4. **Given** Ember Ward is at Tier 1 (not yet upgraded), **When** the wearer takes fire/burn damage, **Then** no
   full-ignore chance applies — that bonus is Tier 2-exclusive per PRD §6.
5. **Given** Ember Ward has already reached Tier 2, **When** further qualifying fire/burn damage occurs, **Then**
   the tattoo continues functioning at Tier 2 with no errors and no further tier change.

---

### User Story 3 - The effect reuses existing infrastructure, and adds the one reuse point that's genuinely missing (Priority: P3)

Having shipped Ember Ward's effect, a developer picking up a future tattoo can reuse the existing value accessor
and tier-progression tracker (feature 002), and whatever this feature adds for "the wearer absorbed fire/burn
damage" — without modifying Ember Ward's own effect logic.

**Why this priority**: This is the same "establish the pattern" rationale every prior passive-tattoo feature had
for its own tattoo. It matters less than Ember Ward actually working (User Stories 1-2) because there's nothing
proven to reuse until Tier 1/Tier 2 work correctly for this tattoo.

**Scope note**: The existing reuse points cover "this pawn was struck in melee" (feature 002), "this pawn's own
ranged shot landed" (feature 003), and "this pawn's own melee attack landed" (feature 010). None of them are
scoped to a specific damage *type* — Ember Ward is the first tattoo whose trigger is "damage of a particular kind
(fire/burn) was taken," regardless of whether it arrived via melee, ranged, or an environmental source (e.g. a
burning building). This feature is expected to add that reuse point, keyed generically enough that a future
tattoo built around a different damage type (e.g. a cold- or explosive-themed effect) could reuse the same
mechanism with its own DamageDef configuration, rather than a one-off "burn damage" special case.

**Independent Test**: Can be tested by reviewing whatever new reuse point this feature adds and confirming a
hypothetical future tattoo needing "my wearer took damage of DamageDef X" could adopt it via its own data/
configuration, without editing Ember Ward-specific code.

**Acceptance Scenarios**:

1. **Given** the value accessor and tier-progression tracker features 002/010 already established, **When**
   Ember Ward needs a tiered progression counter, **Then** it reuses that directly, with no modification to
   existing code.
2. **Given** a new reuse point this feature adds for "the wearer took damage of a given DamageDef," **When** a
   future tattoo needs the same kind of hook for a different damage type, **Then** it can reuse that point with
   its own configuration, without duplicating Ember Ward's fire/burn-specific detection logic.

---

### Edge Cases

- What happens if the fire/burn damage would have been fully absorbed by armor or another effect before Ember
  Ward's reduction applies (net damage dealt is already zero)? No progression-counter increment is required in
  this case, since there is no meaningful "damage instance absorbed" to count — mirroring Vampiric Thorn's
  "only successful/consequential events count" convention (feature 010).
- What happens if the wearer is downed or dies from the same burn instance that would have triggered the
  reduction or counter increment? No further processing is required beyond whatever damage reduction already
  applied to that instance; there's no meaningful "after the hit" state to update on a dead pawn.
- What happens if the wearer takes multiple simultaneous fire/burn damage instances in the same tick (e.g.
  standing in a large fire while also hit by a flamethrower)? Each instance is evaluated independently — each
  gets its own reduction roll, its own (at Tier 2) full-ignore roll, and its own counter increment.
- What happens if a Tier 2 full-ignore roll succeeds? The instance is prevented from dealing damage entirely,
  but the progression counter still increments — reaching Tier 2 doesn't stop the counter from being tracked, it
  just no longer changes the tattoo's tier.
- What happens if damage against the tattooed pawn comes from a non-fire/burn source? Ember Ward's reduction,
  full-ignore chance, and progression counter MUST NOT trigger — PRD §6 explicitly scopes this to fire/burn
  damage.
- What happens to the progression counter and tier state across a save and reload? Both MUST persist unchanged.
- What happens if some other system removes the Ember Ward hediff from the pawn? The heat-resistance bonus,
  damage reduction, and full-ignore chance MUST stop applying immediately once the hediff is gone; the existing
  cross-tattoo removal guard (feature 003, FR-015) already prevents anything but this mod's own sanctioned path
  or Developer Mode/God Mode from removing it in the first place, with no Ember-Ward-specific configuration
  required.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Ember Ward applied (at either tier), the system MUST grant that pawn a passive
  heat-resistance bonus that applies continuously for as long as the tattoo is present.
- **FR-002**: While a pawn has Ember Ward applied, the system MUST reduce the amount of fire/burn damage actually
  applied to that pawn whenever they take damage of that type.
- **FR-003**: The system MUST track a per-pawn, per-tattoo progression counter for Ember Ward that increments
  each time the wearer takes a fire/burn damage instance that Ember Ward's reduction actually applies to, per the
  counter defined in PRD §5.4/§6 ("instances of fire/burn damage absorbed").
- **FR-004**: When Ember Ward's progression counter reaches its Tier 2 threshold, the system MUST automatically
  upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or any change to
  the pawn's tattoo-slot usage.
- **FR-005**: At Tier 2, Ember Ward MUST provide a higher heat-resistance bonus and stronger fire/burn damage
  reduction than at Tier 1, plus a chance to fully ignore a burn instance (reduce the damage it deals to zero),
  per PRD §6.
- **FR-006**: At Tier 1, the full-ignore chance MUST NOT apply — that bonus is Tier 2-exclusive per PRD §6.
- **FR-007**: The progression counter's current value and the tattoo's current tier MUST persist correctly
  across a game save and reload.
- **FR-008**: Heat resistance, burn-damage reduction, and the full-ignore chance MUST NOT apply to any pawn that
  does not currently have Ember Ward applied.
- **FR-009**: The system MUST NOT apply Ember Ward's reduction/full-ignore effects or increment the progression
  counter for damage that is not fire/burn type.
- **FR-010**: The system MUST NOT increment the progression counter for a fire/burn damage instance that resolves
  to zero net damage before Ember Ward's own reduction would apply (e.g. fully absorbed by armor or another
  effect first).
- **FR-011**: Ember Ward's effect MUST be implemented through the existing `appliedHediff` contract established
  by feature 001 and reused by every tattoo shipped so far (the `TattooDef` → `HediffDef` → Hediff/HediffComp
  chain), rather than a new or parallel mechanism for granting or tracking tattoo state.
- **FR-012**: Every tunable numeric value driving Ember Ward's effect (the Tier 1 and Tier 2 heat-resistance
  amounts, the Tier 1 and Tier 2 burn-damage reduction amounts, the Tier 2 full-ignore chance, and the Tier
  1→Tier 2 progression threshold) MUST be read through feature 002's `TattooEffectValues` accessor rather than
  read directly from XML/Def fields at each use site, consistent with every tattoo shipped so far.
- **FR-013**: The system MUST reuse the existing value accessor and tier-progression tracker (feature 002)
  without modification. Where existing reuse points (the stat-offset contract, the on-melee-hit-taken contract,
  the on-ranged-hit-landed contract, the on-own-melee-hit-landed contract) do not cover what Ember Ward needs —
  specifically, reacting to the wearer taking damage of a specific DamageDef (fire/burn) regardless of how it
  arrived — the system MUST add a new, similarly generic reuse point rather than hardcoding Ember-Ward-specific
  detection logic into a one-off location.
- **FR-014**: If the Ember Ward hediff is removed from a pawn by any means, the system MUST stop applying all of
  its effects (heat resistance, damage reduction, full-ignore chance) immediately, with no lingering effect.
- **FR-015**: Ember Ward's damage-reduction effect MUST resolve correctly whether Combat Extended is loaded or
  absent. This MUST be confirmed against CE's own loaded content during planning rather than assumed final —
  CE may route damage-reduction resolution (e.g. its own armor/damage pipeline) differently from vanilla for
  fire/burn damage specifically.

### Key Entities *(include if feature involves data)*

- **Ember Ward Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Ember Ward hediff —
  the Tier 1 heat-resistance bonus plus burn-damage reduction, and the stronger Tier 2 variant plus the
  full-ignore chance.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying events (for Ember Ward: fire/burn
  damage instances absorbed) that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is reached — the
  same kind of counter every prior passive tattoo established, reusing their tracker.
- **"Damage Taken By Type" Reuse Point**: The new, tattoo-agnostic detection mechanism this feature adds for
  "this pawn took damage of a specific DamageDef," scoped to fire/burn for Ember Ward but generic enough for a
  future tattoo themed around a different damage type to reuse with its own configuration.
- **Tattoo Hediff Removal Guard**: Existing cross-tattoo infrastructure (feature 003, FR-015) that already blocks
  Ember Ward's hediff from unsanctioned removal with zero additional configuration needed here.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pawn with Ember Ward applied shows a measurable heat-resistance increase compared to an
  otherwise-identical pawn without it, verifiable via the pawn's in-game stat readout.
- **SC-002**: Across repeated fire/burn damage instances against an Ember Ward-tattooed pawn, the damage actually
  applied is measurably lower than against an otherwise-identical untattooed pawn, and non-fire/burn damage is
  unaffected.
- **SC-003**: A tattooed pawn's Ember Ward automatically upgrades from Tier 1 to Tier 2 once the designated
  number of qualifying fire/burn damage instances occurs, with zero additional player action required (no new
  ritual, no ingredient spend, no slot change).
- **SC-004**: After upgrading to Tier 2, the same pawn's heat resistance and burn-damage reduction are measurably
  stronger than they were at Tier 1, and the full-ignore effect is observed to occur across enough trials; at
  Tier 1, no full-ignore ever occurs.
- **SC-005**: An Ember Ward-tattooed pawn's progression counter and current tier are unchanged immediately after
  a save/reload compared to immediately before it.
- **SC-006**: A future tattoo needing to react to "the wearer took damage of a specific type" can be implemented
  using the reuse point this feature adds, without modifying any of Ember Ward's own effect code.
- **SC-007**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat
  Extended, with zero errors in either configuration.
- **SC-008**: An attempt to remove the Ember Ward hediff through RimWorld's normal hediff-removal API, from any
  caller other than this mod's own sanctioned path, fails to remove it; the same removal action succeeds when
  performed via Developer Mode/God Mode's health-tab tooling.

## Assumptions

- "Fire/burn damage" is interpreted as RimWorld's `DamageDefOf.Flame` (and any other damage def flagged as a
  burn-type source, if RimWorld 1.6 distinguishes more than one) — the same damage category vanilla uses for
  fire, flamethrowers, and burning-building/environmental fire damage.
- Exact numeric values — the Tier 1 and Tier 2 heat-resistance amounts, the Tier 1 and Tier 2 burn-damage
  reduction amounts, the Tier 2 full-ignore chance, and the Tier 1→Tier 2 progression-counter threshold — are
  placeholder/balance-pass numbers per PRD §9, exactly as every tattoo shipped so far.
- "Instances of fire/burn damage absorbed" (PRD §6) is interpreted as: a fire/burn damage instance where Ember
  Ward's own reduction had something nonzero to reduce (i.e. the instance wasn't already reduced to zero damage
  before Ember Ward's contribution), mirroring how other passive tattoos only count consequential events.
- This feature covers exactly one tattoo's gameplay effect, Ember Ward. The other 2 remaining passive tattoos
  (Ironskin Glyph, Starlight Ward) remain inert stub Hediffs after this feature ships; wiring up their individual
  effects is out of scope here.
- The tattoo hediff removal guard (feature 003, FR-015) already covers every `TattooMagicDef.appliedHediff`
  generically, including Ember Ward, with no further work needed in this feature.
- The value-accessor-driven, override-ready design established by Frost Sigil (feature 002 FR-012) continues to
  apply here; this feature still does not build a player-facing settings UI (PRD §10 remains deferred).
- The feature 001 ritual/application flow (recipe, skill check, slot enforcement) is unaffected by this feature;
  this feature only adds gameplay effect behavior to a tattoo that can already be applied.
- Per PRD §7, Ember Ward is not the tattoo the PRD flags for CE ammo/loadout-system integration (that candidate
  is Serpent's Eye). This feature only needs its fire/burn damage reduction to resolve correctly against
  whichever damage pipeline (vanilla or CE) is active — it does not need dedicated CE-specific ammo/loadout
  tuning.
