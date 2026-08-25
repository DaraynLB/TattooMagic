# Feature Specification: Ironskin Glyph Tattoo Effect (Eighth Passive Tattoo, Physical Armor Slice)

**Feature Branch**: `012-ironskin-glyph-tattoo-effect`

**Created**: 2026-08-16

**Status**: Draft

**Input**: User description: "Ironskin Glyph tattoo effect — a passive tattoo granting increased armor rating (PRD
§6 row 5). Defs/TattooDefs/IronskinGlyph.xml is already scaffolded with placeholder ingredients/workAmount and
appliedHediff TattooMagic_Hediff_IronskinGlyph, but no HediffComp yet exists. Tier 1 (base): +armor rating, mapped
to CE armor stats when CE is active per the PRD. Progression counter (per PRD §5.4, passive-tattoo type):
increments on hits absorbed while equipped. Tier 2 (upgraded, in place, no new ritual/slot): further armor
increase, plus a small chance to fully negate a hit entirely. Exact tier-2 threshold and numeric tuning are
placeholders pending the PRD §9 balance pass, same as every other shipped tattoo. Important context from feature
011 (Ember Ward, the mod's other armor-stat-offset tattoo, for Heat damage): its ongoing stat-offset contribution
turned out to silently do nothing under Combat Extended specifically, because CE's own ambient-damage armor
formula only reads a pawn's own armor-category stat when the hit body part belongs to a body-part group ordinary
humans never satisfy — fixed with a direct, CE-gated damage-amount reduction alongside the vanilla stat-offset
path. Ironskin Glyph is the mod's Sharp/Blunt-damage counterpart to that, and per the PRD explicitly needs to 'map
to CE armor stats when CE is active' as a first-class requirement — planning must verify, by inspecting Combat
Extended's own code, whether the direct-hit/non-ambient CE armor path (used for Sharp/Blunt damage from weapons
and melee) has the same or a different gap, rather than assuming feature 011's exact finding transfers unchanged."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ironskin Glyph makes its wearer harder to hurt in a fight (Priority: P1)

A player applies the Ironskin Glyph tattoo to a colonist (via the existing ritual station). From that point on,
that colonist runs measurably tougher than before, and whenever they take physical damage — a knife, a fist, a
bullet, an explosion fragment — the damage they actually take is reduced compared to an otherwise-identical
untattooed pawn.

**Why this priority**: This is the entire point of the feature — turning Ironskin Glyph from an inert stub Hediff
into a tattoo that does something, mirroring what features 002 and 011 did for their own tattoos. It's the
smallest slice that proves this tattoo's Tier 1 effect works, and every other story here depends on it.

**Independent Test**: Can be fully tested by applying Ironskin Glyph to a colonist, comparing their armor stats
against an untattooed pawn, then exposing both to an identical physical-damage source and comparing the damage
each actually takes.

**Acceptance Scenarios**:

1. **Given** a pawn with Ironskin Glyph applied, **When** the pawn's armor-rating stats are inspected, **Then**
   they reflect the tattoo's Tier 1 bonus, and revert to normal if the tattoo were ever absent.
2. **Given** a pawn with Ironskin Glyph applied takes physical (sharp or blunt) damage, **When** that damage is
   resolved, **Then** the amount actually applied is reduced compared to an identical hit against an untattooed
   pawn.
3. **Given** a pawn with Ironskin Glyph applied takes damage from a non-physical source (fire/burn, toxic, etc.),
   **When** that damage is resolved, **Then** no reduction from Ironskin Glyph applies and the hit resolves
   normally.
4. **Given** a pawn without Ironskin Glyph applied, **When** they take physical damage, **Then** no armor bonus
   or damage reduction from this tattoo is present, regardless of what other tattoos or effects they may have.

---

### User Story 2 - Ironskin Glyph grows stronger the more hits its wearer absorbs (Priority: P2)

As an Ironskin Glyph-tattooed colonist keeps absorbing physical hits, the tattoo tracks how many qualifying hits
have occurred and automatically upgrades itself to a stronger Tier 2 version once enough have happened — with no
extra ritual, ingredients, or tattoo slot required. At Tier 2, armor and damage reduction are stronger than
Tier 1, and a hit now has a chance to be fully negated (zero damage taken from it).

**Why this priority**: This is the PRD §5.4 tiering mechanic, mirroring User Story 2 of every prior passive
tattoo. It only makes sense once Tier 1's effect (User Story 1) exists and can be measured, so it's the natural
second slice.

**Independent Test**: Can be tested independently by exposing an Ironskin Glyph-tattooed pawn to enough physical
hits to reach the threshold, then confirming the tattoo's effect strengthens in place without any player action
beyond survival, and separately confirming the full-negate chance is observable across enough trials at Tier 2.

**Acceptance Scenarios**:

1. **Given** Tier 1 Ironskin Glyph applied with its progression counter below threshold, **When** the wearer
   takes physical damage that is at least partially absorbed by the tattoo's armor, **Then** the counter
   increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering hit resolves,
   **Then** Ironskin Glyph upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to
   the pawn's slot usage.
3. **Given** Ironskin Glyph is at Tier 2, **When** its armor rating and damage reduction are compared to Tier 1,
   **Then** both are measurably stronger, and a chance to fully negate a hit (take zero damage from it) is
   present where it was not at Tier 1.
4. **Given** Ironskin Glyph is at Tier 1 (not yet upgraded), **When** the wearer takes physical damage, **Then**
   no full-negate chance applies — that bonus is Tier 2-exclusive per PRD §6.
5. **Given** Ironskin Glyph has already reached Tier 2, **When** further qualifying physical hits occur, **Then**
   the tattoo continues functioning at Tier 2 with no errors and no further tier change.

---

### User Story 3 - The effect reuses existing infrastructure without needing a new reuse point (Priority: P3)

Having shipped Ironskin Glyph's effect, a developer picking up a future tattoo can reuse the existing value
accessor, tier-progression tracker, stat-offset contract, and (if this tattoo's own hit needs it) the pre-armor
incoming-damage reuse point feature 011 added — without modifying Ironskin Glyph's own effect logic.

**Why this priority**: This is the same "establish/confirm the pattern" rationale every prior passive-tattoo
feature had for its own tattoo. It matters less than Ironskin Glyph actually working (User Stories 1-2) because
there's nothing to confirm reusable until Tier 1/Tier 2 work correctly for this tattoo.

**Scope note**: Unlike Ember Ward (feature 011), which had to add a brand-new "damage taken by type" reuse point,
Ironskin Glyph is expected to need no new shared infrastructure at all — it's the second consumer of everything
feature 011 already built (`IProvidesTattooStatOffset` against `ArmorRating_Sharp`/`ArmorRating_Blunt`, and
`IOnIncomingDamageTattooEffect` for the same reasons feature 011 needed it: guaranteeing a real reduction under
Combat Extended and supporting the Tier 2 full-negate roll). This story exists to confirm that expectation holds,
not to design something new.

**Independent Test**: Can be tested by reviewing Ironskin Glyph's own comp and confirming it implements the
existing contracts using only its own configuration, without any change to
`Source/Effects/IOnIncomingDamageTattooEffect.cs`, `Source/Patches/Patch_Pawn_PreApplyDamage_
TattooDamageAbsorb.cs`, or any other tattoo's file.

**Acceptance Scenarios**:

1. **Given** the value accessor, tier-progression tracker, and stat-offset contract features 002/011 already
   established, **When** Ironskin Glyph needs a tiered progression counter and an armor-rating stat bonus,
   **Then** it reuses both directly, with no modification to existing code.
2. **Given** the pre-armor incoming-damage reuse point feature 011 added, **When** Ironskin Glyph needs a
   guaranteed-correct reduction under Combat Extended and a Tier 2 full-negate roll, **Then** it reuses that
   point with its own `DamageDef`/stat configuration, without duplicating or modifying Ember Ward's own comp.

---

### Edge Cases

- What happens if the physical damage would have been fully absorbed by armor or another effect before Ironskin
  Glyph's own reduction applies (net damage dealt is already zero)? No progression-counter increment is required
  in this case, mirroring Ember Ward's "only successful/consequential events count" convention (feature 011).
- What happens if the wearer is downed or dies from the same hit that would have triggered the reduction or
  counter increment? No further processing is required beyond whatever damage reduction already applied to that
  instance; there's no meaningful "after the hit" state to update on a dead pawn.
- What happens if the wearer takes multiple simultaneous physical hits in the same tick (e.g. a shotgun blast
  with multiple pellets, or a melee flurry)? Each instance is evaluated independently — each gets its own
  reduction, its own (at Tier 2) full-negate roll, and its own counter increment.
- What happens if a Tier 2 full-negate roll succeeds? The instance is prevented from dealing damage entirely, but
  the progression counter still increments — reaching Tier 2 doesn't stop the counter from being tracked, it just
  no longer changes the tattoo's tier.
- What happens if damage against the tattooed pawn comes from a non-physical source (fire/burn, toxic, etc.)?
  Ironskin Glyph's reduction, full-negate chance, and progression counter MUST NOT trigger — PRD §6 scopes this
  to physical ("armor rating") damage, not every damage category.
- What happens to the progression counter and tier state across a save and reload? Both MUST persist unchanged.
- What happens if some other system removes the Ironskin Glyph hediff from the pawn? The armor bonus, damage
  reduction, and full-negate chance MUST stop applying immediately once the hediff is gone; the existing
  cross-tattoo removal guard (feature 003, FR-015) already prevents anything but this mod's own sanctioned path
  or Developer Mode/God Mode from removing it in the first place, with no Ironskin-Glyph-specific configuration
  required.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Ironskin Glyph applied (at either tier), the system MUST grant that pawn a passive
  armor-rating bonus, covering both sharp and blunt physical damage categories, that applies continuously for as
  long as the tattoo is present.
- **FR-002**: While a pawn has Ironskin Glyph applied, the system MUST reduce the amount of physical (sharp or
  blunt) damage actually applied to that pawn whenever they take damage of those types, and this reduction MUST
  resolve correctly whether Combat Extended is loaded or absent (per PRD §7's "mapped to CE armor stats when CE
  is active" requirement).
- **FR-003**: The system MUST track a per-pawn, per-tattoo progression counter for Ironskin Glyph that increments
  each time the wearer takes a physical damage instance that Ironskin Glyph's reduction actually applies to, per
  the counter defined in PRD §5.4/§6 ("hits absorbed while equipped").
- **FR-004**: When Ironskin Glyph's progression counter reaches its Tier 2 threshold, the system MUST
  automatically upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or
  any change to the pawn's tattoo-slot usage.
- **FR-005**: At Tier 2, Ironskin Glyph MUST provide a higher armor-rating bonus and stronger physical-damage
  reduction than at Tier 1, plus a chance to fully negate a hit (reduce the damage it deals to zero), per PRD §6.
- **FR-006**: At Tier 1, the full-negate chance MUST NOT apply — that bonus is Tier 2-exclusive per PRD §6.
- **FR-007**: The progression counter's current value and the tattoo's current tier MUST persist correctly across
  a game save and reload.
- **FR-008**: The armor bonus, physical-damage reduction, and full-negate chance MUST NOT apply to any pawn that
  does not currently have Ironskin Glyph applied.
- **FR-009**: The system MUST NOT apply Ironskin Glyph's reduction/full-negate effects or increment the
  progression counter for damage that is not sharp or blunt type.
- **FR-010**: The system MUST NOT increment the progression counter for a physical damage instance that resolves
  to zero net damage before Ironskin Glyph's own reduction would apply (e.g. fully absorbed by armor or another
  effect first).
- **FR-011**: Ironskin Glyph's effect MUST be implemented through the existing `appliedHediff` contract
  established by feature 001 and reused by every tattoo shipped so far (the `TattooDef` → `HediffDef` →
  Hediff/HediffComp chain), rather than a new or parallel mechanism for granting or tracking tattoo state.
- **FR-012**: Every tunable numeric value driving Ironskin Glyph's effect (the Tier 1 and Tier 2 armor-rating
  amounts, the Tier 2 full-negate chance, and the Tier 1→Tier 2 progression threshold) MUST be read through
  feature 002's `TattooEffectValues` accessor rather than read directly from XML/Def fields at each use site,
  consistent with every tattoo shipped so far.
- **FR-013**: The system MUST reuse the existing value accessor, tier-progression tracker, stat-offset contract,
  and pre-armor incoming-damage reuse point (features 002/011) without modification. This feature is not expected
  to need any new shared infrastructure — see User Story 3's scope note — but if a genuine gap is found during
  planning, any new reuse point added MUST be similarly generic, not Ironskin-Glyph-specific.
- **FR-014**: If the Ironskin Glyph hediff is removed from a pawn by any means, the system MUST stop applying all
  of its effects (armor bonus, damage reduction, full-negate chance) immediately, with no lingering effect.
- **FR-015**: Ironskin Glyph's damage-reduction effect MUST resolve correctly whether Combat Extended is loaded
  or absent, confirmed against CE's own loaded content during planning rather than assumed final. Planning MUST
  independently verify (per this feature's Input) whether Combat Extended's non-ambient/direct-hit armor code
  path — the one sharp/blunt weapon and melee damage actually takes, distinct from the ambient/fire-tick path
  feature 011 investigated — has the same, a different, or no gap in reading a pawn's own armor-category stat,
  rather than assuming feature 011's fix transfers unchanged.

### Key Entities *(include if feature involves data)*

- **Ironskin Glyph Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Ironskin Glyph
  hediff — the Tier 1 armor-rating bonus plus physical-damage reduction, and the stronger Tier 2 variant plus the
  full-negate chance.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying events (for Ironskin Glyph: physical
  hits absorbed) that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is reached — the same kind of
  counter every prior passive tattoo established, reusing their tracker.
- **Tattoo Hediff Removal Guard**: Existing cross-tattoo infrastructure (feature 003, FR-015) that already blocks
  Ironskin Glyph's hediff from unsanctioned removal with zero additional configuration needed here.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pawn with Ironskin Glyph applied shows a measurable armor-rating increase compared to an
  otherwise-identical pawn without it, verifiable via the pawn's in-game stat readout.
- **SC-002**: Across repeated physical damage instances against an Ironskin Glyph-tattooed pawn, the damage
  actually applied is measurably lower than against an otherwise-identical untattooed pawn, both with Combat
  Extended absent and with it present, and non-physical damage is unaffected.
- **SC-003**: A tattooed pawn's Ironskin Glyph automatically upgrades from Tier 1 to Tier 2 once the designated
  number of qualifying physical hits occurs, with zero additional player action required (no new ritual, no
  ingredient spend, no slot change).
- **SC-004**: After upgrading to Tier 2, the same pawn's armor rating and physical-damage reduction are
  measurably stronger than they were at Tier 1, and the full-negate effect is observed to occur across enough
  trials; at Tier 1, no full-negate ever occurs.
- **SC-005**: An Ironskin Glyph-tattooed pawn's progression counter and current tier are unchanged immediately
  after a save/reload compared to immediately before it.
- **SC-006**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat
  Extended, with zero errors in either configuration.
- **SC-007**: An attempt to remove the Ironskin Glyph hediff through RimWorld's normal hediff-removal API, from
  any caller other than this mod's own sanctioned path, fails to remove it; the same removal action succeeds
  when performed via Developer Mode/God Mode's health-tab tooling.

## Assumptions

- "Armor rating" (PRD §6, undifferentiated) is interpreted as covering both `ArmorRating_Sharp` and
  `ArmorRating_Blunt` — a generalist toughness effect, unlike Ember Ward's single-damage-category (Heat) scope —
  since Ironskin Glyph's theme ("ironskin") and its PRD description don't scope it to one physical damage type
  over the other, and the roster already has no other tattoo covering general physical armor.
- Exact numeric values — the Tier 1 and Tier 2 armor-rating amounts, the Tier 2 full-negate chance, and the Tier
  1→Tier 2 progression-counter threshold — are placeholder/balance-pass numbers per PRD §9, exactly as every
  tattoo shipped so far.
- "Hits absorbed while equipped" (PRD §6) is interpreted the same way Ember Ward's "instances ... absorbed" is
  (feature 011): a physical damage instance where Ironskin Glyph's own reduction had something nonzero to reduce
  (i.e. the instance wasn't already reduced to zero damage before Ironskin Glyph's contribution).
- This feature covers exactly one tattoo's gameplay effect, Ironskin Glyph. The one remaining passive tattoo
  (Starlight Ward) remains an inert stub Hediff after this feature ships; wiring it up is out of scope here.
- The tattoo hediff removal guard (feature 003, FR-015) already covers every `TattooMagicDef.appliedHediff`
  generically, including Ironskin Glyph, with no further work needed in this feature.
- The value-accessor-driven, override-ready design established by Frost Sigil (feature 002 FR-012) continues to
  apply here; this feature still does not build a player-facing settings UI (PRD §10 remains deferred).
- The feature 001 ritual/application flow (recipe, skill check, slot enforcement) is unaffected by this feature;
  this feature only adds gameplay effect behavior to a tattoo that can already be applied.
- Per PRD §7, Ironskin Glyph is not the tattoo the PRD flags for CE ammo/loadout-system integration (that
  candidate is Serpent's Eye). This feature's CE obligation is specifically that its own armor-rating reduction
  resolves correctly under CE (PRD §7's explicit "mapped to CE armor stats when CE is active" line for this
  tattoo), not broader ammo/loadout tuning.
- Whether Combat Extended's non-ambient (direct-hit) armor code path has the same pawn-stat-reading gap feature
  011 found in its ambient path is an open question this feature's planning phase is expected to resolve by
  inspecting CE's own code, not something this spec assumes an answer to either way.
