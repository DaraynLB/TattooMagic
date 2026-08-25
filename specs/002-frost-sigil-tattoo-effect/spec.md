# Feature Specification: Frost Sigil Tattoo Effect (Effect Pattern Vertical Slice)

**Feature Branch**: `002-frost-sigil-tattoo-effect`

**Created**: 2026-08-06

**Status**: Draft

**Input**: User description: "Wire up real tattoo effects — vertical slice for ONE tattoo (Frost Sigil) to establish the pattern before doing the remaining 10. Implement Frost Sigil's Tier 1 passive cold resistance + on-hit melee slow, its Tier 1→Tier 2 progression counter per PRD §5.4, and plug it into the existing appliedHediff contract from feature 001 — as a clean, reusable pattern later features can reuse per-tattoo."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Frost Sigil actually protects its wearer (Priority: P1)

A player applies the Frost Sigil tattoo to a colonist (via the feature 001
ritual station, already built). From that point on, the colonist runs
measurably colder-hardy than before, and melee attackers who strike the
colonist have a chance to be slowed by the sigil's frost.

**Why this priority**: This is the entire point of the feature — turning
Frost Sigil from an inert stub Hediff into a tattoo that does something. It's
the smallest slice that proves a tattoo's PRD-described Tier 1 effect can
actually run in-game, and every other prioritized story in this feature
depends on it existing first.

**Independent Test**: Can be fully tested by applying Frost Sigil to a
colonist, comparing their cold-resistance stat against an untattooed pawn,
then having that colonist take repeated melee hits and observing attackers
occasionally slowed.

**Acceptance Scenarios**:

1. **Given** a pawn with Frost Sigil applied, **When** the pawn's
   cold-resistance stat is inspected, **Then** it reflects the tattoo's Tier 1
   bonus, and reverts to normal if the tattoo were ever absent.
2. **Given** a pawn with Frost Sigil applied is struck by a melee attacker,
   **When** the slow chance procs, **Then** the attacker's move speed is
   reduced for a limited duration.
3. **Given** a pawn with Frost Sigil applied is struck by a melee attacker,
   **When** the slow chance does not proc, **Then** the attacker is
   unaffected and combat continues normally.
4. **Given** a pawn without Frost Sigil applied, **When** they are struck in
   melee, **Then** no slow effect occurs and no cold-resistance bonus is
   present, regardless of what other tattoos or effects they may have.

---

### User Story 2 - Frost Sigil grows stronger the more it's used (Priority: P2)

As a Frost Sigil-tattooed colonist keeps fighting and the sigil's slow
repeatedly triggers on melee attackers, the tattoo tracks how often that
happens and automatically upgrades itself to a stronger Tier 2 version once
enough qualifying hits have occurred — with no extra ritual, ingredients, or
tattoo slot required.

**Why this priority**: This is the PRD §5.4 tiering mechanic that makes a
tattoo "keep paying off" instead of being apply-once-and-forget. It only
makes sense once Tier 1's effect (User Story 1) already exists and can be
triggered, so it is the natural second slice.

**Independent Test**: Can be tested independently by repeatedly triggering
melee hits with the slow proc against a Frost Sigil-tattooed pawn until the
threshold is reached, then confirming the tattoo's effect strengthens in
place without any player action beyond fighting.

**Acceptance Scenarios**:

1. **Given** Tier 1 Frost Sigil applied with its progression counter below
   threshold, **When** a melee hit against the wearer causes the slow to
   proc, **Then** the counter increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When**
   the triggering event resolves, **Then** Frost Sigil upgrades to Tier 2 in
   place, with no new ritual, no ingredient cost, and no change to the
   pawn's slot usage.
3. **Given** Frost Sigil is at Tier 2, **When** its cold resistance and slow
   proc are compared to Tier 1, **Then** both are measurably stronger, and a
   rare chance to briefly freeze the attacker is present where it was not at
   Tier 1.
4. **Given** Frost Sigil has already reached Tier 2, **When** further
   qualifying events occur, **Then** the tattoo continues functioning at
   Tier 2 with no errors and no further tier change.

---

### User Story 3 - The effect can be reused for the next *passive* tattoo without rework (Priority: P3)

Having shipped Frost Sigil's effect, a developer picking up the next
**passive** tattoo (per PRD §6: Ember Ward, Ironskin Glyph, Serpent's Eye,
Starlight Ward, or Vampiric Thorn) can give it a passive stat bonus, an
on-hit/on-event proc, and/or a combat-event-driven tiered progression
counter by supplying that tattoo's own data and configuration — without
modifying Frost Sigil's own effect logic.

**Why this priority**: This is the stated purpose of doing Frost Sigil
first — establishing a reusable pattern. It matters less than Frost Sigil
actually working (User Stories 1-2) because until Tier 1/Tier 2 work
correctly for one tattoo, there is no proven pattern to reuse yet.

**Scope note**: PRD §5.4 splits the 11 tattoos into two families with
different mechanics: **passive** tattoos (reactive stat bonuses/procs,
counter driven by combat events) and **triggered** tattoos (player-activated
gizmo abilities with a cooldown, counter driven by activation count). Frost
Sigil is a passive tattoo, so this feature only proves out — and this story
only claims reuse for — the *passive* half of that split. Triggered/gizmo
tattoos (Bloodrune, Stormlash, Wraithstep, Berserker's Mark, Guardian's
Call) need a materially different pattern (ability gizmo UI, cooldown
management, activation-count counter) that this feature does not build or
validate. That pattern is intentionally left for a separate future feature
using a triggered tattoo as its own vertical slice — see Assumptions.

**Independent Test**: Can be tested by implementing a second *passive*
tattoo's stat-bonus and/or on-event-proc effect using the shared
infrastructure this feature establishes, and confirming no changes to Frost
Sigil-specific code were required to do so.

**Acceptance Scenarios**:

1. **Given** the effect infrastructure this feature establishes, **When** a
   future *passive* tattoo needs a passive stat bonus and/or an on-hit/
   on-event proc, **Then** it can be built from that same shared
   infrastructure rather than a one-off implementation copied and modified
   from Frost Sigil.
2. **Given** the tier-progression tracking this feature establishes, **When**
   a future *passive* tattoo needs its own Tier 1→Tier 2 counter driven by a
   combat event (per PRD §5.4), **Then** it can reuse the same tracking
   mechanism with its own counter definition and threshold, without
   duplicating Frost Sigil's counter logic.
3. **Given** a future *triggered* tattoo (gizmo + cooldown + activation-count
   counter), **When** it is implemented, **Then** it is understood to require
   its own separate pattern, not this feature's passive-effect
   infrastructure — this feature makes no reuse claim for that case.

---

### Edge Cases

- What happens if the melee attacker striking the tattooed pawn is not a
  humanlike (e.g. an animal or mechanoid)? The slow proc and progression
  counter MUST still apply — PRD §6 says "a melee attacker" generically,
  with no pawn-type restriction.
- What happens if the tattooed pawn is downed or dies from the same blow
  that would have triggered the slow proc? No slow effect or counter
  increment is required in this case, since there is no meaningful "after
  the hit" state to apply it in.
- What happens if the melee attacker is already affected by a movement-slow
  effect (from this or another source) when Frost Sigil's slow procs again?
  The system MUST handle a repeat/overlapping slow without erroring;
  refreshing or stacking duration is an implementation-level choice, not a
  behavior this feature's success depends on.
- What happens if damage against the tattooed pawn comes from a non-melee
  source (ranged weapon, explosion, fire, etc.)? The slow proc and
  progression counter MUST NOT trigger — PRD §6 explicitly scopes this to
  melee attackers.
- What happens to the progression counter and tier state across a save and
  reload? Both MUST persist unchanged.
- What happens if some other system removes the Frost Sigil hediff from the
  pawn (even though feature 001 provides no in-game way to do this)? The
  cold-resistance bonus and slow proc MUST stop applying immediately once
  the hediff is gone.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Frost Sigil applied (at either tier), the
  system MUST grant that pawn a passive cold-resistance bonus that applies
  continuously for as long as the tattoo is present.
- **FR-002**: When a pawn with Frost Sigil applied is struck by a melee
  attacker, the system MUST roll a chance to apply a temporary movement-speed
  slow to that attacker.
- **FR-003**: The system MUST track a per-pawn, per-tattoo progression
  counter for Frost Sigil that increments each time the slow effect actually
  procs on an attacker, per the counter defined in PRD §5.4/§6 ("melee hits
  taken where the slow procs").
- **FR-004**: When Frost Sigil's progression counter reaches its Tier 2
  threshold, the system MUST automatically upgrade the tattoo to Tier 2 in
  place, without requiring a new ritual, additional ingredients, or any
  change to the pawn's tattoo-slot usage.
- **FR-005**: At Tier 2, Frost Sigil MUST provide a higher cold-resistance
  bonus and a higher slow chance and/or magnitude than at Tier 1, plus a rare
  chance to briefly freeze (immobilize) the attacker, per PRD §6.
- **FR-006**: The progression counter's current value and the tattoo's
  current tier MUST persist correctly across a game save and reload.
- **FR-007**: Cold resistance and the slow-on-hit proc MUST NOT apply to any
  pawn that does not currently have Frost Sigil applied.
- **FR-008**: The system MUST NOT trigger the slow proc or increment the
  progression counter for non-melee damage sources (ranged, explosive, fire,
  or other non-melee-attack damage).
- **FR-009**: Frost Sigil's effect MUST be implemented through the existing
  `appliedHediff` contract established by feature 001 (the `TattooDef` →
  `HediffDef` → Hediff/HediffComp chain already guaranteed to exist and be
  granted/removed correctly) rather than a new or parallel mechanism for
  granting or tracking tattoo state.
- **FR-010**: The effect implementation MUST be structured as reusable
  building blocks — a passive stat-bonus effect, an on-hit/on-event-proc
  effect, and combat-event-driven tier-progression counter tracking — such
  that a future **passive** tattoo (per PRD §6/§5.4) needing any of these can
  be added via that tattoo's own data/configuration, without modifying Frost
  Sigil's own effect code. This requirement does NOT extend to triggered/
  gizmo-ability tattoos (activation-based, with a cooldown and a
  use-count progression counter) — that is a distinct pattern out of scope
  for this feature; see Assumptions.
- **FR-011**: If the Frost Sigil hediff is removed from a pawn by any means,
  the system MUST stop applying its cold-resistance bonus and slow proc
  immediately, with no lingering effect.
- **FR-012**: Every tunable numeric value driving Frost Sigil's effect
  (cold-resistance amount, slow chance/magnitude, freeze chance/duration,
  and the Tier 1→Tier 2 progression threshold, for both tiers) MUST be read
  through a single override-capable value accessor shared by all tattoo
  effects, rather than being read directly from XML/Def fields at each use
  site. The accessor MUST return the tattoo's XML-defined value when no
  override is present. This feature does NOT need to provide a way to
  actually set an override (no in-game settings UI) — per PRD §10, a
  player-facing settings menu for tuning these values remains explicitly
  out of scope — but every future value-configuration entry point (whether
  a settings UI or something else) MUST be able to plug into this feature's
  accessor without any tattoo's effect code changing.

### Key Entities *(include if feature involves data)*

- **Frost Sigil Tattoo Effect**: The runtime gameplay behavior granted while
  a pawn has the Frost Sigil hediff — the Tier 1 cold-resistance bonus plus
  on-hit slow proc, and the stronger Tier 2 variant of both plus the rare
  freeze chance.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying
  events (for Frost Sigil: melee hits taken where the slow procs) that drives
  the automatic Tier 1 → Tier 2 upgrade once a threshold is reached.
- **Effect Pattern (shared infrastructure)**: The reusable structure this
  feature establishes — for passive stat-bonus effects, on-hit/on-event-proc
  effects, and combat-event-driven tier-progression tracking — that later
  features will reuse to wire up the remaining **passive** tattoos (Ember
  Ward, Ironskin Glyph, Serpent's Eye, Starlight Ward, Vampiric Thorn)
  without re-deriving the approach from scratch. It does NOT cover the
  triggered-ability pattern (gizmo, cooldown, activation-count counter)
  needed for Bloodrune, Stormlash, Wraithstep, Berserker's Mark, and
  Guardian's Call — that pattern is established by a separate future
  feature.
- **Tunable Value Accessor**: The single shared lookup point through which
  every tattoo effect (starting with Frost Sigil) reads its numeric balance
  values, returning an override if one has been set and the tattoo's
  XML-defined value otherwise. This feature is the first consumer and
  establishes the accessor itself; it does not provide any way to set an
  override, since a player-facing settings UI (PRD §10) remains out of
  scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pawn with Frost Sigil applied shows a measurable
  cold-resistance increase compared to an otherwise-identical pawn without
  it, verifiable via the pawn's in-game stat readout.
- **SC-002**: Across repeated melee hits against a Frost Sigil-tattooed pawn,
  attackers are slowed at a rate consistent with a real, non-zero,
  non-guaranteed proc chance, distinguishable from "never happens" or
  "always happens" over enough trials.
- **SC-003**: A tattooed pawn's Frost Sigil automatically upgrades from
  Tier 1 to Tier 2 once the designated number of qualifying events occurs,
  with zero additional player action required (no new ritual, no ingredient
  spend, no slot change).
- **SC-004**: After upgrading to Tier 2, the same pawn's cold resistance and
  slow proc are measurably stronger than they were at Tier 1, and the rare
  freeze effect is observed to occur across enough trials.
- **SC-005**: A Frost Sigil-tattooed pawn's progression counter and current
  tier are unchanged immediately after a save/reload compared to immediately
  before it.
- **SC-006**: A second *passive* tattoo's stat-bonus and/or on-hit/on-event-
  proc effect, implemented in a later feature, requires no modification to
  any of Frost Sigil's own effect code — confirmed by review at that time.
  (This criterion applies only to passive tattoos; triggered/gizmo tattoos
  are explicitly out of scope for this reuse claim — see Assumptions.)
- **SC-007**: A future player-facing settings UI for tuning tattoo balance
  values (PRD §10, out of scope for this feature) can be built entirely by
  plugging into this feature's value accessor, with zero changes required
  to Frost Sigil's or any other tattoo's effect code — confirmed by review
  at that time.

## Assumptions

- "Chance to slow a melee attacker on hit" (PRD §6) means the tattooed pawn
  is the one being struck by a melee attacker; the tattoo reactively
  punishes attackers striking its wearer, not the wearer's own attacks.
- Exact numeric values — the cold-resistance offset, the slow chance and
  magnitude, the Tier 2 freeze chance/duration, and the Tier 1→Tier 2
  progression-counter threshold — are placeholder/balance-pass numbers per
  PRD §9 ("Per-tattoo Tier 1 → Tier 2 thresholds ... needs a full balance
  pass"). This feature only needs the mechanism to work correctly for
  whatever values are configured, not final tuned numbers.
- "Freeze" at Tier 2 is interpreted as a short hard-crowd-control effect
  (e.g., a brief stun/immobilize) on the attacker, consistent with vanilla
  RimWorld conventions for short CC effects; the exact mechanic is an
  implementation-level decision, not fixed by this feature.
- Per PRD §7, Frost Sigil is not the tattoo the PRD flags for CE
  ammo/loadout-system integration (that candidate is Serpent's Eye). This
  feature only needs to not error or misbehave when Combat Extended is
  loaded or absent — it does not need dedicated CE-specific tuning for
  Frost Sigil's stats.
- This feature covers exactly one tattoo, Frost Sigil. The other 10 tattoos
  remain inert stub Hediffs after this feature ships; wiring up their
  individual effects is out of scope here.
- The reusable pattern this feature establishes (passive stat-bonus effect,
  on-hit/on-event proc, combat-event-driven tier counter) only covers the
  **passive** tattoo family per PRD §5.4/§6 (Ember Ward, Ironskin Glyph,
  Serpent's Eye, Starlight Ward, Vampiric Thorn — 5 tattoos remaining after
  this one). It deliberately does not attempt to also cover the
  **triggered** tattoo family (Bloodrune, Stormlash, Wraithstep, Berserker's
  Mark, Guardian's Call), which are player-activated on-demand abilities
  needing a gizmo UI, cooldown management, and a use-count progression
  counter instead of a combat-event counter — a materially different
  pattern. The next planned vertical slice after this feature is expected to
  pick one triggered tattoo and establish that on-demand-ability pattern the
  same way this feature establishes the passive one; it is not part of this
  feature's scope or success criteria.
- The feature 001 ritual/application flow (recipe, skill check, slot
  enforcement) is unaffected by this feature; this feature only adds
  gameplay effect behavior to a tattoo that can already be applied.
- Per PRD §10, a player-facing in-game settings menu for tuning tattoo
  balance values is explicitly deferred, not v1 scope. This feature draws a
  line between that deferred UI and the value-accessor indirection it sits
  behind (FR-012): the accessor is cheap to build now and expensive to
  retrofit once several tattoos' effect code exists reading values
  directly, so it's in scope here; the settings window/UI itself gains
  nothing from being built early (it grows incrementally as each tattoo
  ships regardless of start date), so it stays deferred per the PRD.
