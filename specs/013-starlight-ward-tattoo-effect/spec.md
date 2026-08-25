# Feature Specification: Starlight Ward Tattoo Effect (Ninth Passive Tattoo, Mental Resilience Slice)

**Feature Branch**: `013-starlight-ward-tattoo-effect`

**Created**: 2026-08-21

**Status**: Draft

**Input**: User description: "Starlight Ward tattoo effect — a passive tattoo granting mental-break resistance and
psychic-sensitivity resistance (PRD §6 row 9), the last of the mod's 11-tattoo v1 roster to get its effect
implemented. Defs/TattooDefs/StarlightWard.xml is already scaffolded with placeholder ingredients/workAmount and
appliedHediff TattooMagic_Hediff_StarlightWard; the HediffDef exists (Defs/HediffDefs/Tattoos/StarlightWard.xml)
but its description says 'Effect not yet implemented — this feature only covers application,' and no HediffComp
exists yet. Tier 1 (base): mental-break resistance plus psychic-sensitivity resistance. Progression counter (per
PRD §5.4, passive-tattoo type): increments on 'mental-break risk events resisted while equipped.' Tier 2 (upgraded,
in place, no new ritual/slot): further resistance to both, plus a small passive mood buff. Exact tier-2 threshold
and numeric tuning are placeholders pending the PRD §9 balance pass, same as every other shipped tattoo.

Important context distinguishing this from every prior passive tattoo (002 Frost Sigil, 011 Ember Ward, 012
Ironskin Glyph): those all hook a damage-resolution reuse point to detect their qualifying event. Starlight Ward's
qualifying event is a mental-break roll, not a damage instance — vanilla RimWorld's mental break system uses a
mood-vs-threshold check with its own timing/roll logic, not a simple pass/fail per tick. Planning must investigate
vanilla's actual mental-break-check code path to determine how to apply resistance through the existing stat-offset
contract versus whether a new contract is needed, and what 'a mental-break risk event resisted' concretely means as
a detectable hook point, since naively incrementing on every non-break tick would make the counter increment
constantly regardless of whether the tattoo did anything. Also confirm whether Royalty/Ideology-specific mental
break or psychic mechanics affect PsychicSensitivity's relevance.

This feature covers exactly one tattoo's gameplay effect, Starlight Ward — the last one in the PRD §6 roster. After
this ships, all 11 tattoos have functioning effects."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Starlight Ward makes its wearer psychologically tougher (Priority: P1)

A player applies the Starlight Ward tattoo to a colonist (via the existing ritual station). From that point on,
that colonist is measurably more resistant to mental breaks than an otherwise-identical untattooed pawn under the
same mood conditions, and is measurably less susceptible to psychic-sensitivity-based effects.

**Why this priority**: This is the entire point of the feature — turning Starlight Ward from an inert stub Hediff
into a tattoo that does something, mirroring what features 002, 011, and 012 did for their own tattoos. It's the
smallest slice that proves this tattoo's Tier 1 effect works, and every other story here depends on it.

**Independent Test**: Can be fully tested by applying Starlight Ward to a colonist, comparing their mental-break
threshold and psychic-sensitivity stats against an untattooed pawn, then driving both pawns' mood down under
identical conditions and comparing mental-break outcomes.

**Acceptance Scenarios**:

1. **Given** a pawn with Starlight Ward applied, **When** the pawn's mental-break-related and psychic-sensitivity
   stats are inspected, **Then** they reflect the tattoo's Tier 1 bonus, and revert to normal if the tattoo were
   ever absent.
2. **Given** two otherwise-identical pawns at the same mood level, one with Starlight Ward applied and one without,
   **When** both are placed under conditions that risk a mental break, **Then** the tattooed pawn is measurably less
   likely to break than the untattooed pawn.
3. **Given** a pawn with Starlight Ward applied, **When** a psychic-sensitivity-scaled effect would apply to them,
   **Then** its impact is reduced relative to an identical untattooed pawn.
4. **Given** a pawn without Starlight Ward applied, **When** their mood or psychic-sensitivity-related stats are
   inspected, **Then** no bonus from this tattoo is present, regardless of what other tattoos or effects they may
   have.

---

### User Story 2 - Starlight Ward grows stronger the more mental-break risk its wearer resists (Priority: P2)

As a Starlight Ward-tattooed colonist keeps having their mental-break risk reduced by the tattoo, the tattoo tracks
how many qualifying events have occurred and automatically upgrades itself to a stronger Tier 2 version once enough
have happened — with no extra ritual, ingredients, or tattoo slot required. At Tier 2, mental-break and
psychic-sensitivity resistance are stronger than Tier 1, and the wearer also gains a small, always-on mood buff.

**Why this priority**: This is the PRD §5.4 tiering mechanic, mirroring User Story 2 of every prior passive tattoo.
It only makes sense once Tier 1's effect (User Story 1) exists and can be measured, so it's the natural second
slice.

**Independent Test**: Can be tested independently by putting a Starlight Ward-tattooed pawn through enough
mental-break-risk situations to reach the threshold, then confirming the tattoo's effect strengthens in place
without any player action beyond survival, and separately confirming the Tier 2 mood buff is present where it was
absent at Tier 1.

**Acceptance Scenarios**:

1. **Given** Tier 1 Starlight Ward applied with its progression counter below threshold, **When** the wearer's
   mental-break risk is evaluated and the tattoo's resistance makes a difference to the outcome, **Then** the
   counter increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering event resolves, **Then**
   Starlight Ward upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to the pawn's
   slot usage.
3. **Given** Starlight Ward is at Tier 2, **When** its mental-break resistance and psychic-sensitivity resistance
   are compared to Tier 1, **Then** both are measurably stronger, and a small passive mood buff is present where it
   was not at Tier 1.
4. **Given** Starlight Ward is at Tier 1 (not yet upgraded), **When** the wearer's mood is inspected, **Then** no
   mood buff from this tattoo applies — that bonus is Tier 2-exclusive per PRD §6.
5. **Given** Starlight Ward has already reached Tier 2, **When** further qualifying events occur, **Then** the
   tattoo continues functioning at Tier 2 with no errors and no further tier change.

---

### User Story 3 - The effect reuses existing infrastructure wherever it genuinely applies (Priority: P3)

Having shipped Starlight Ward's effect, a developer picking up future work can reuse the existing value accessor,
tier-progression tracker, and stat-offset/removal-guard contracts without modifying Starlight Ward's own effect
logic — and, because Starlight Ward is the first passive tattoo whose qualifying event isn't a damage instance, any
genuinely new reuse point this feature needs to detect "mental-break risk resisted" is itself built generically
enough for a hypothetical future tattoo to reuse, not hard-coded to Starlight Ward.

**Why this priority**: This is the same "establish/confirm the pattern" rationale every prior passive-tattoo
feature had for its own tattoo. It matters less than Starlight Ward actually working (User Stories 1-2) because
there's nothing to confirm reusable until Tier 1/Tier 2 work correctly for this tattoo.

**Scope note**: Unlike Ember Ward (feature 011), which added a new "damage taken by type" reuse point that later
tattoos (012) directly reused, Starlight Ward is expected to need a new reuse point of its own kind — a
mental-break-risk detection hook — since no existing infrastructure covers a non-damage-based passive event. This
story exists to make sure that new hook is built as a generic contract, not to avoid building it.

**Independent Test**: Can be tested by reviewing Starlight Ward's own comp and any new shared hook it introduces,
confirming the existing value accessor, tier tracker, and removal guard are reused unmodified, and confirming any
new detection hook is named/scoped generically rather than referencing Starlight Ward by name.

**Acceptance Scenarios**:

1. **Given** the value accessor, tier-progression tracker, and removal-guard contracts features 002/003 already
   established, **When** Starlight Ward needs a tiered progression counter and tunable numeric values, **Then** it
   reuses both directly, with no modification to existing code.
2. **Given** no existing reuse point detects non-damage-based passive events, **When** Starlight Ward needs to
   detect "a mental-break risk event resisted," **Then** planning designs a new, generically-named hook for this
   purpose rather than embedding Starlight-Ward-specific logic into vanilla's mental-break system directly.

---

### Edge Cases

- What happens if the wearer's mood never comes close to mental-break risk while Starlight Ward is equipped (e.g. a
  consistently happy colonist)? No progression-counter increments occur in this case — there is nothing for the
  tattoo's resistance to have meaningfully affected, mirroring every other passive tattoo's "only consequential
  events count" convention (features 002/011/012).
- What happens if the wearer is downed, dies, or already has an active mental break in progress? No further
  processing is required beyond whatever resistance already applied to that evaluation; there's no meaningful
  "after the fact" state to update once a break is already underway or the pawn is no longer able to break.
- What happens if the wearer has other mods, traits, or hediffs that already modify mental-break thresholds or
  psychic sensitivity (e.g. Nerves of Steel, Psychically Hypersensitive)? Starlight Ward's bonus stacks additively
  with those, the same way every other tattoo's stat-offset bonus stacks with unrelated traits/hediffs today.
- What happens to the progression counter and tier state across a save and reload? Both MUST persist unchanged.
- What happens if some other system removes the Starlight Ward hediff from the pawn? The mental-break resistance,
  psychic-sensitivity resistance, and (at Tier 2) mood buff MUST stop applying immediately once the hediff is gone;
  the existing cross-tattoo removal guard (feature 003, FR-015) already prevents anything but this mod's own
  sanctioned path or Developer Mode/God Mode from removing it in the first place, with no Starlight-Ward-specific
  configuration required.
- What happens if Royalty and/or Ideology are not installed? Mental-break resistance and psychic-sensitivity
  resistance MUST still function using core-game mechanics; the tattoo MUST NOT require either DLC.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Starlight Ward applied (at either tier), the system MUST grant that pawn a passive
  reduction in mental-break likelihood that applies continuously for as long as the tattoo is present.
- **FR-002**: While a pawn has Starlight Ward applied (at either tier), the system MUST grant that pawn a passive
  reduction in psychic sensitivity that applies continuously for as long as the tattoo is present.
- **FR-003**: The system MUST track a per-pawn, per-tattoo progression counter for Starlight Ward that increments
  each time the wearer's mental-break risk is evaluated and Starlight Ward's resistance meaningfully affects the
  outcome, per the counter defined in PRD §5.4/§6 ("mental-break risk events resisted while equipped").
- **FR-004**: When Starlight Ward's progression counter reaches its Tier 2 threshold, the system MUST automatically
  upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or any change to
  the pawn's tattoo-slot usage.
- **FR-005**: At Tier 2, Starlight Ward MUST provide stronger mental-break resistance and stronger psychic-
  sensitivity resistance than at Tier 1, plus a small, always-on passive mood buff, per PRD §6.
- **FR-006**: At Tier 1, the passive mood buff MUST NOT apply — that bonus is Tier 2-exclusive per PRD §6.
- **FR-007**: The progression counter's current value and the tattoo's current tier MUST persist correctly across a
  game save and reload.
- **FR-008**: The mental-break resistance, psychic-sensitivity resistance, and (at Tier 2) mood buff MUST NOT apply
  to any pawn that does not currently have Starlight Ward applied.
- **FR-009**: The system MUST NOT increment the progression counter for a mood dip or event that never placed the
  wearer at meaningful risk of a mental break (i.e. the tattoo's resistance had nothing consequential to affect).
- **FR-010**: Starlight Ward's effect MUST be implemented through the existing `appliedHediff` contract established
  by feature 001 and reused by every tattoo shipped so far (the `TattooDef` → `HediffDef` → Hediff/HediffComp
  chain), rather than a new or parallel mechanism for granting or tracking tattoo state.
- **FR-011**: Every tunable numeric value driving Starlight Ward's effect (the Tier 1 and Tier 2 resistance amounts,
  the Tier 2 mood buff, and the Tier 1→Tier 2 progression threshold) MUST be read through feature 002's
  `TattooEffectValues` accessor rather than read directly from XML/Def fields at each use site, consistent with
  every tattoo shipped so far.
- **FR-012**: The system MUST reuse the existing value accessor, tier-progression tracker, and hediff removal guard
  (features 002/003) without modification. Because no existing reuse point detects a non-damage-based passive
  event, this feature MUST introduce a new, generically-scoped detection mechanism for "mental-break risk resisted"
  rather than one hard-coded to Starlight Ward, consistent with how feature 011 introduced its own new reuse point
  generically.
- **FR-013**: If the Starlight Ward hediff is removed from a pawn by any means, the system MUST stop applying all
  of its effects (mental-break resistance, psychic-sensitivity resistance, mood buff) immediately, with no
  lingering effect.
- **FR-014**: Starlight Ward's effects MUST function correctly regardless of whether the Royalty and/or Ideology
  DLCs are installed, using only core-game mental-break and psychic-sensitivity mechanics.

### Key Entities *(include if feature involves data)*

- **Starlight Ward Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Starlight Ward
  hediff — the Tier 1 mental-break and psychic-sensitivity resistance, and the stronger Tier 2 variant plus the
  passive mood buff.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying events (for Starlight Ward: mental-break
  risk events resisted) that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is reached — the same
  kind of counter every prior passive tattoo established, reusing their tracker.
- **Mental-Break-Risk-Resisted Event**: A new kind of qualifying event, distinct from every prior passive tattoo's
  damage-instance-based event, representing an occasion where the wearer's mental-break risk was evaluated and
  Starlight Ward's resistance made a meaningful difference to the outcome.
- **Tattoo Hediff Removal Guard**: Existing cross-tattoo infrastructure (feature 003, FR-015) that already blocks
  Starlight Ward's hediff from unsanctioned removal with zero additional configuration needed here.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pawn with Starlight Ward applied shows a measurable mental-break-resistance increase and psychic-
  sensitivity decrease compared to an otherwise-identical pawn without it, verifiable via the pawn's in-game stat
  readout.
- **SC-002**: Across repeated mental-break-risk situations, a Starlight Ward-tattooed pawn breaks measurably less
  often than an otherwise-identical untattooed pawn at the same mood level.
- **SC-003**: A tattooed pawn's Starlight Ward automatically upgrades from Tier 1 to Tier 2 once the designated
  number of qualifying resisted-risk events occurs, with zero additional player action required (no new ritual, no
  ingredient spend, no slot change).
- **SC-004**: After upgrading to Tier 2, the same pawn's mental-break and psychic-sensitivity resistance are
  measurably stronger than they were at Tier 1, and a small passive mood buff is present that was absent at Tier 1.
- **SC-005**: A Starlight Ward-tattooed pawn's progression counter and current tier are unchanged immediately after
  a save/reload compared to immediately before it.
- **SC-006**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat
  Extended, with zero errors in either configuration.
- **SC-007**: An attempt to remove the Starlight Ward hediff through RimWorld's normal hediff-removal API, from any
  caller other than this mod's own sanctioned path, fails to remove it; the same removal action succeeds when
  performed via Developer Mode/God Mode's health-tab tooling.
- **SC-008**: With this feature complete, every one of the 11 tattoos in the PRD §6 roster has a functioning
  gameplay effect (Tier 1 and Tier 2), with no tattoo remaining as an inert stub.

## Assumptions

- "Mental-break risk events resisted while equipped" (PRD §6) is interpreted as: an occasion when the wearer's
  mental state was evaluated for a possible mental break and Starlight Ward's resistance made a meaningful
  difference to that evaluation — mirroring the "the tattoo's own contribution had something consequential to
  affect" convention established by Ember Ward and Ironskin Glyph (features 011/012), applied to mental-break risk
  rather than incoming damage, rather than incrementing on every tick regardless of whether the tattoo did
  anything.
- Exact numeric values — the Tier 1 and Tier 2 resistance amounts, the Tier 2 mood buff, and the Tier 1→Tier 2
  progression-counter threshold — are placeholder/balance-pass numbers per PRD §9, exactly as every tattoo shipped
  so far.
- Psychic sensitivity is treated as a core-game mechanic (not gated behind the Royalty or Ideology DLCs) — core
  RimWorld already includes traits (e.g. Psychically Deaf, Psychically Hypersensitive) that modify it — so no
  DLC-conditional behavior is required for this feature.
- This feature covers exactly one tattoo's gameplay effect, Starlight Ward — the last one in the PRD §6 roster.
  After this feature ships, all 11 tattoos have functioning Tier 1/Tier 2 effects and no further "wire up a stub
  tattoo" feature remains in the v1 scope (PRD §2).
- Tier 2's "small passive mood buff" (PRD §6) is a flat, always-on mood offset while at Tier 2, distinct from and
  additive with the mental-break/psychic-sensitivity resistance, consistent with the PRD's "further resistance ...
  plus a small passive mood buff" wording.
- The tattoo hediff removal guard (feature 003, FR-015) already covers every `TattooMagicDef.appliedHediff`
  generically, including Starlight Ward, with no further work needed in this feature.
- The value-accessor-driven, override-ready design established by Frost Sigil (feature 002 FR-012) continues to
  apply here; this feature still does not build a player-facing settings UI (PRD §10 remains deferred).
- The feature 001 ritual/application flow (recipe, skill check, slot enforcement) is unaffected by this feature;
  this feature only adds gameplay effect behavior to a tattoo that can already be applied.
- Whether a new reuse point/contract is needed to detect "mental-break risk resisted," and exactly how it should
  hook into vanilla's mental-break system, is an open technical question this feature's planning phase resolves by
  investigating vanilla's actual code path, not something this spec assumes an answer to either way.
