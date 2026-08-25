# Feature Specification: Tattoo Ritual Station & Applying a Tattoo

**Feature Branch**: `001-tattoo-ritual-station`

**Created**: 2026-08-05

**Status**: Draft

**Input**: User description: "Tattoo ritual station + applying a tattoo — the core loop: build the workbench, consume ingredients, run the Artistic skill check, apply a tattoo to a pawn's slot."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Successfully apply a tattoo to a colonist (Priority: P1)

A player builds the tattoo ritual station, assigns a colonist to perform the
ritual on a chosen recipient, and selects one of the 11 starter tattoos. The
required ingredients are consumed, the performing colonist's skill is
checked, and on success the recipient permanently gains the tattoo, which now
occupies one of their tattoo slots.

**Why this priority**: This is the entire core loop of the mod — without it,
no tattoo can ever be applied and nothing else in the mod (progression,
abilities, buffs) can happen. It is the minimum viable slice.

**Independent Test**: Can be fully tested by building the station, queuing a
ritual with a skilled colonist and sufficient ingredients, and confirming the
recipient ends up with the selected tattoo occupying a slot. Delivers value
on its own even before slot-progression or individual tattoo effects exist.

**Acceptance Scenarios**:

1. **Given** a built ritual station, a performing colonist, a recipient pawn
   with at least one free tattoo slot, and sufficient ingredients available,
   **When** the player queues a ritual to apply a specific tattoo to the
   recipient, **Then** the ingredients are consumed and the ritual begins.
2. **Given** a ritual is underway and resolves successfully, **When** the
   ritual completes, **Then** the recipient permanently has that tattoo
   applied, occupying one of their tattoo slots, and the tattoo is visible on
   the recipient going forward.
3. **Given** a recipient with no tattoos yet, **When** a tattoo is
   successfully applied, **Then** the recipient's remaining free slot count
   decreases by exactly one.

---

### User Story 2 - Ritual fails due to low performer skill (mishap, not a hard block) (Priority: P2)

A player attempts a ritual with a colonist whose Artistic skill is low. The
ritual is still permitted to start, but carries a real chance of failure
(mishap) instead of guaranteeing success or being blocked outright.

**Why this priority**: RimWorld's surgery-style conventions (which this
mechanic deliberately mirrors) always allow the attempt while scaling risk by
skill, rather than gating on a hard skill floor. Getting failure behavior
right is essential so players correctly weigh who performs the ritual, but it
depends on User Story 1's success path already existing.

**Independent Test**: Can be tested independently by assigning a
low-Artistic-skill colonist to perform a ritual repeatedly and confirming
some attempts fail (mishap) while the recipient never ends up in a broken or
partially-applied state, and by assigning a high-skill colonist and
confirming failures become rare.

**Acceptance Scenarios**:

1. **Given** a performing colonist with low Artistic skill, **When** a ritual
   is queued, **Then** the game does not block the ritual from starting
   solely on the basis of low skill.
2. **Given** a ritual resolves as a mishap, **When** the ritual completes,
   **Then** the recipient does NOT gain the selected tattoo and does not have
   a slot consumed, and the ingredients used for the attempt are not
   recovered.
3. **Given** two otherwise-identical rituals, one performed by a low-skill
   colonist and one by a high-skill colonist, **When** many attempts are
   compared, **Then** the high-skill colonist's attempts succeed measurably
   more often.

---

### User Story 3 - Ritual is blocked when the recipient has no free tattoo slots (Priority: P3)

A player attempts to apply a tattoo to a pawn who already has a tattoo
occupying every slot they've unlocked so far. The game prevents the ritual
from starting rather than allowing an over-capacity application.

**Why this priority**: Slot limits are what make the progression system
(a separate feature) meaningful, but enforcing the limit at the point of
application is this feature's responsibility. It matters less than the
happy path and failure path because in early play most pawns simply won't
have hit their slot cap yet.

**Independent Test**: Can be tested independently by giving a pawn tattoos
up to their current slot capacity, then attempting one more ritual on that
pawn and confirming it cannot be started.

**Acceptance Scenarios**:

1. **Given** a recipient pawn with zero free tattoo slots, **When** a player
   attempts to queue a ritual targeting that pawn, **Then** the ritual cannot
   be started and the player is shown why.
2. **Given** a recipient pawn with at least one free slot, **When** a ritual
   targeting them is queued, **Then** the ritual is allowed to start
   normally.

---

### Edge Cases

- What happens if the performing colonist is interrupted (drafted, downed,
  mental break, colony under attack) mid-ritual? The ritual MUST cancel
  without consuming ingredients that haven't yet been committed and without
  applying a partial tattoo to the recipient.
- What happens if a player tries to apply a tattoo the recipient already has
  applied? The system MUST prevent selecting a duplicate tattoo for a
  recipient who already has it, since duplicates have no defined effect.
  Getting a stronger version of an owned tattoo happens through a separate
  tiering-progression feature, not by re-applying it.
- What happens if ingredients are available when the ritual is queued but
  are consumed/hauled away by something else before the ritual starts? The
  ritual MUST wait for ingredients to be available, consistent with standard
  crafting-bill behavior, rather than starting and failing partway.
- What happens if the recipient pawn is not a free colonist (e.g. a
  prisoner, slave, guest, or hostile)? Out of scope for this feature — see
  Assumptions.
- What happens if no colonist on the map currently has any Artistic skill at
  all (skill level 0)? The ritual MUST still be attemptable — a skill of 0
  is simply the low end of the same success-chance scale, not a separate
  blocked state.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a dedicated tattoo ritual station, separate
  from vanilla crafting/medical stations, at which tattoo application takes
  place.
- **FR-002**: System MUST let the player choose, for a ritual, both a
  recipient pawn and which one of the 11 starter tattoos to apply.
- **FR-003**: System MUST require a defined set of ingredient materials to be
  available and consumed in order for a ritual to be performed.
- **FR-004**: System MUST require a performing colonist, distinct from the
  recipient, whose Artistic skill level determines the ritual's chance of
  success.
- **FR-005**: System MUST resolve every ritual attempt as exactly one of two
  outcomes: success (tattoo applied) or mishap (tattoo not applied), and MUST
  NOT block an attempt outright purely for having low performer skill.
- **FR-006**: System MUST make success probability increase with the
  performing colonist's Artistic skill level, so more skilled performers
  fail less often.
- **FR-007**: On success, system MUST permanently apply the chosen tattoo to
  the recipient and MUST reduce the recipient's free tattoo-slot count by
  one.
- **FR-008**: On mishap, system MUST NOT apply the tattoo, MUST NOT change
  the recipient's slot count, and MUST NOT refund the ingredients already
  consumed for the attempt.
- **FR-009**: System MUST prevent a ritual from starting when the recipient
  has zero free tattoo slots, and MUST communicate to the player why the
  ritual cannot start.
- **FR-010**: System MUST prevent a recipient from ever having the same
  tattoo applied more than once at a time.
- **FR-011**: Once applied, a tattoo MUST be permanent — the system MUST NOT
  provide any way to remove or undo an applied tattoo in this feature.
- **FR-012**: System MUST leave a partially-completed ritual with no lasting
  effect on the recipient if it is interrupted before resolving (no partial
  tattoo, no partial slot consumption).

### Key Entities *(include if feature involves data)*

- **Tattoo Ritual Station**: The dedicated workbench where tattoo
  application happens; defines what ingredients and performer conditions a
  ritual requires.
- **Tattoo Definition**: One of the 11 starter tattoos available for
  selection at the station; this feature only concerns each tattoo's
  identity and ritual recipe, not the gameplay effect it grants once
  applied (covered by a separate feature).
- **Ritual Attempt**: A single in-progress or resolved instance of applying
  a specific tattoo to a specific recipient by a specific performer; resolves
  to success or mishap.
- **Applied Tattoo (pawn record)**: The record, kept per recipient pawn, of
  which tattoo(s) currently occupy which of their tattoo slots.
- **Tattoo Slot**: A recipient pawn's limited capacity for simultaneously
  applied tattoos; this feature enforces the current capacity but does not
  determine how that capacity grows (see the separate slot-progression
  feature).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A player can go from "station not yet built" to "a colonist has
  a tattoo applied" in a single uninterrupted play session, with zero errors
  shown in the dev console.
- **SC-002**: Across repeated attempts, a high-Artistic-skill performer
  succeeds at applying a tattoo measurably more often than a low-skill
  performer, confirming skill visibly affects outcomes.
- **SC-003**: In 100% of tested attempts, a recipient pawn with zero free
  tattoo slots cannot have a ritual started against them.
- **SC-004**: In 100% of tested attempts, no recipient pawn ends up with the
  same tattoo applied twice, and no in-game action removes an already-applied
  tattoo.
- **SC-005**: In 100% of tested interruption scenarios (performer drafted
  away, downed, or the colony is attacked mid-ritual), the recipient is left
  with no partial tattoo and no partial slot consumption.

## Assumptions

- Only free colonists are eligible recipients and performers in this
  feature; prisoners, slaves, guests, and hostile pawns are out of scope for
  v1 (no PRD guidance suggests otherwise).
- On a mishap, the only consequence is that the ingredients are lost and the
  tattoo is not applied — no additional injury, illness, or mood penalty is
  required by this feature. Exact mishap severity is a balance-tuning
  question (PRD §9) that can be adjusted later without changing this
  feature's scope.
- The exact ingredient recipe and numeric skill-check thresholds per tattoo
  are placeholders pending a dedicated balance pass (PRD §9); this feature
  only requires that a recipe and a skill-scaled success chance exist and be
  enforced, not their final tuned values.
- A pawn's current tattoo-slot capacity (starting at 1, growing via a
  separate XP-driven progression feature) is treated as an existing input
  this feature reads and enforces, not something this feature implements.
- What each of the 11 tattoos actually does once applied (passive buff or
  triggered ability, and its Tier 1/Tier 2 behavior) is out of scope here;
  this feature only covers getting a tattoo from "selected" to "applied and
  occupying a slot."
