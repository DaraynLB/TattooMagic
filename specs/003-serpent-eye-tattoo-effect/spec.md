# Feature Specification: Serpent's Eye Tattoo Effect (Second Passive Tattoo, CE-Aware Slice)

**Feature Branch**: `003-serpent-eye-tattoo-effect`

**Created**: 2026-08-06

**Status**: Draft

**Input**: User description: "Wire up the Serpent's Eye tattoo effect — the second passive tattoo, built on the reusable passive-tattoo-effect infrastructure established by feature 002 (Frost Sigil). Tier 1: + accuracy, reduced weapon sway/recoil (CE-aware). Progression counter: ranged hits landed. Tier 2: further accuracy/sway reduction, plus a bonus to CE ammo effectiveness. This is the PRD §7 candidate for exercising real Combat-Extended-aware stat branching, and needs a new 'ranged hit landed' reuse point complementing feature 002's 'melee hit taken' one."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Serpent's Eye makes its wearer a better shot (Priority: P1)

A player applies the Serpent's Eye tattoo to a colonist (via the existing ritual station). From that point on,
the colonist shoots more accurately than before. If the player also has Combat Extended loaded, the colonist's
weapon additionally sways and recoils less than it otherwise would.

**Why this priority**: This is the entire point of the feature — turning Serpent's Eye from an inert stub
Hediff into a tattoo that does something, mirroring what feature 002 did for Frost Sigil. It's the smallest
slice that proves this tattoo's Tier 1 effect works, and every other story here depends on it.

**Independent Test**: Can be fully tested by applying Serpent's Eye to a colonist, comparing their accuracy
stat against an untattooed colonist, then (with Combat Extended loaded) comparing sway/recoil-related stats
the same way.

**Acceptance Scenarios**:

1. **Given** a pawn with Serpent's Eye applied, **When** their accuracy stat is inspected, **Then** it
   reflects the tattoo's Tier 1 bonus, in both a non-CE game and a CE-loaded game.
2. **Given** a pawn with Serpent's Eye applied and Combat Extended loaded, **When** their sway/recoil-related
   stats are inspected, **Then** they reflect the tattoo's Tier 1 reduction.
3. **Given** a pawn with Serpent's Eye applied and Combat Extended **not** loaded, **When** their stats are
   inspected, **Then** the accuracy bonus is still present, and no error or broken state results from the
   sway/recoil portion simply not having anything to apply to (see Assumptions).
4. **Given** a pawn without Serpent's Eye applied, **When** their stats are inspected, **Then** none of these
   bonuses are present, regardless of what other tattoos or effects they may have.

---

### User Story 2 - Serpent's Eye grows stronger the more its wearer hits their target (Priority: P2)

As a Serpent's Eye-tattooed colonist keeps landing ranged shots, the tattoo tracks how many have connected and
automatically upgrades itself to a stronger Tier 2 version once enough have landed — with no extra ritual,
ingredients, or tattoo slot required. With Combat Extended loaded, Tier 2 also grants a bonus to the
effectiveness of the wearer's currently loaded ammunition.

**Why this priority**: This is the PRD §5.4 tiering mechanic, mirroring User Story 2 of feature 002. It only
makes sense once Tier 1's effect (User Story 1) exists and can be measured, so it's the natural second slice.

**Independent Test**: Can be tested independently by having a Serpent's Eye-tattooed pawn land enough ranged
hits to reach the threshold, then confirming the tattoo's effect strengthens in place without any player
action beyond fighting.

**Acceptance Scenarios**:

1. **Given** Tier 1 Serpent's Eye applied with its progression counter below threshold, **When** a ranged
   shot fired by the wearer lands on a target, **Then** the counter increments by one.
2. **Given** a ranged shot fired by the wearer misses or is fired by someone else at the wearer, **When** that
   event resolves, **Then** the counter does NOT increment — only the wearer's own landed shots count.
3. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering event resolves,
   **Then** Serpent's Eye upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change
   to the pawn's slot usage.
4. **Given** Serpent's Eye is at Tier 2 and Combat Extended is loaded, **When** the wearer's currently loaded
   ammunition is inspected, **Then** its effectiveness is measurably boosted compared to Tier 1 or an
   untattooed pawn using the same ammunition.
5. **Given** Serpent's Eye is at Tier 2 and Combat Extended is **not** loaded, **When** the wearer is
   inspected, **Then** accuracy and (if applicable) sway/recoil are still measurably stronger than Tier 1, and
   the absence of the CE-only ammo bonus causes no error.

---

### User Story 3 - The effect can be reused for the next tattoo, including one needing real CE branching (Priority: P3)

Having shipped Serpent's Eye's effect, a developer picking up the next passive tattoo can reuse both feature
002's original infrastructure (the value accessor, the stat-offset contract, the tier-progression tracker) and
whatever this feature adds for "a wearer's own ranged shot landed" and for CE-aware stat branching — without
modifying Serpent's Eye's own effect logic.

**Why this priority**: This is the same "establish the pattern" rationale feature 002 had for Frost Sigil,
now extended to two things Frost Sigil's slice never needed: reacting to the wearer's own successful action
(rather than an attack against them), and genuinely branching behavior on whether Combat Extended is loaded.
It matters less than Serpent's Eye actually working (User Stories 1-2) because there's nothing proven to reuse
until Tier 1/Tier 2 work correctly for this tattoo.

**Scope note**: Feature 002 already established reuse points for a passive stat bonus and for "wearer was hit
in melee." This feature is expected to add at least one new reuse point — "wearer's own ranged shot landed" —
and to establish (or confirm, if feature 002's existing points already suffice) a convention for CE-aware stat
branching that a future tattoo can follow. It does not redesign feature 002's existing infrastructure for its
own sake.

**Independent Test**: Can be tested by reviewing whatever new reuse points this feature adds and confirming a
hypothetical future tattoo needing "my own ranged/melee hit landed" or "a CE-only stat bonus" could adopt them
via its own data/configuration, without editing Serpent's Eye-specific code.

**Acceptance Scenarios**:

1. **Given** the value accessor, stat-offset contract, and tier-progression tracker feature 002 already
   established, **When** Serpent's Eye needs a passive stat bonus and a tiered progression counter, **Then**
   it reuses those directly, with no modification to feature 002's code.
2. **Given** a new reuse point this feature adds for "the wearer's own ranged shot landed," **When** a future
   tattoo needs the same kind of counter (e.g. a tattoo themed around ranged combat), **Then** it can reuse
   that point with its own configuration, without duplicating Serpent's Eye's detection logic.
3. **Given** the CE-branching approach this feature establishes, **When** a future tattoo needs a bonus that
   only makes sense with Combat Extended loaded, **Then** it can follow the same convention rather than
   inventing a new one.

---

### Edge Cases

- What happens if the wearer's ranged shot hits a non-hostile target (e.g. a stray shot hitting a colony
  ally, or hunting an animal)? The counter still increments — PRD §6 says "ranged hits landed" generically,
  with no target-relationship restriction, mirroring how Frost Sigil's melee counter also doesn't care who
  the attacker is.
- What happens if the wearer's shot is deflected/blocked entirely (zero damage dealt)? It MUST NOT count as a
  landed hit and MUST NOT increment the counter, mirroring Frost Sigil's "only successful/consequential
  events count" convention.
- What happens if the wearer is downed or dies before a shot they fired resolves? No counter increment is
  required in that case, since there's no meaningful pawn state to update it on.
- What happens to the progression counter and tier state across a save and reload? Both MUST persist
  unchanged, exactly as required of Frost Sigil.
- What happens if Combat Extended is loaded, then unloaded (or vice versa) between sessions on the same save?
  The non-CE-applicable portions of the effect (sway/recoil reduction, CE ammo bonus) simply stop or start
  applying based on current CE presence; this MUST NOT throw or corrupt saved tier/counter state either way.
- What happens if some other system removes the Serpent's Eye hediff from the pawn (even though no in-game
  way to do this exists yet)? All of its bonuses MUST stop applying immediately once the hediff is gone,
  mirroring Frost Sigil's FR-011.
- What happens if code outside this mod (another mod's generic hediff-clearing tool, a "cleanse" ability from
  an unrelated mod) attempts to remove a tattoo hediff via RimWorld's normal removal API? The attempt MUST be
  blocked — this applies to every applied tattoo, not just Serpent's Eye, since it was raised as a
  customer-identified gap after Frost Sigil (feature 002) had already shipped without it.
- What happens when a developer or tester uses RimWorld's Developer Mode / God Mode tools to delete a tattoo
  hediff directly from the health tab, for testing purposes? That deliberate, explicit override MUST still
  succeed — the removal protection above targets unattended/foreign code, not a human tester's explicit
  God Mode action.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Serpent's Eye applied (at either tier), the system MUST grant that pawn a
  passive ranged-accuracy bonus that applies continuously for as long as the tattoo is present, in both
  non-CE and CE-loaded games.
- **FR-002**: While a pawn has Serpent's Eye applied and Combat Extended is loaded, the system MUST also
  grant a passive reduction to that pawn's weapon sway and recoil, continuously for as long as the tattoo is
  present.
- **FR-003**: The system MUST track a per-pawn, per-tattoo progression counter for Serpent's Eye that
  increments each time a ranged shot fired by the wearer lands on a target (deals damage), per the counter
  defined in PRD §5.4/§6 ("ranged hits landed").
- **FR-004**: When Serpent's Eye's progression counter reaches its Tier 2 threshold, the system MUST
  automatically upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional
  ingredients, or any change to the pawn's tattoo-slot usage.
- **FR-005**: At Tier 2, Serpent's Eye MUST provide a higher accuracy bonus, and (when Combat Extended is
  loaded) a higher sway/recoil reduction, than at Tier 1, per PRD §6.
- **FR-006**: At Tier 2, while Combat Extended is loaded, Serpent's Eye MUST additionally grant a bonus to
  the effectiveness of the wearer's currently loaded ammunition, per PRD §6/§7.
- **FR-007**: The progression counter's current value and the tattoo's current tier MUST persist correctly
  across a game save and reload.
- **FR-008**: None of Serpent's Eye's bonuses MUST apply to any pawn that does not currently have Serpent's
  Eye applied.
- **FR-009**: The system MUST NOT increment the progression counter for a shot that misses, is fully
  deflected/blocks for zero damage, or is fired by anyone other than the Serpent's Eye wearer.
- **FR-010**: Serpent's Eye's effect MUST be implemented through the existing `appliedHediff` contract
  established by feature 001 and reused by feature 002 (the `TattooDef` → `HediffDef` → Hediff/HediffComp
  chain), rather than a new or parallel mechanism for granting or tracking tattoo state.
- **FR-011**: Every tunable numeric value driving Serpent's Eye's effect (accuracy amounts, sway/recoil
  reduction amounts, the CE ammo-effectiveness bonus, and the Tier 1→Tier 2 progression threshold, for both
  tiers) MUST be read through feature 002's `TattooEffectValues` accessor rather than read directly from
  XML/Def fields at each use site, consistent with FR-012 of feature 002.
- **FR-012**: The system MUST reuse feature 002's existing value accessor and tier-progression tracker
  without modification. Where feature 002's existing reuse points (the stat-offset contract, the
  on-melee-hit-taken contract) do not cover something Serpent's Eye needs — specifically, reacting to the
  wearer's own landed ranged shot — the system MUST add a new, similarly generic reuse point rather than
  hardcoding Serpent's Eye-specific detection logic into a one-off location.
- **FR-013**: Any code path that reads or modifies a stat Combat Extended replaces, or that grants a
  CE-exclusive bonus (sway/recoil reduction, ammo effectiveness), MUST detect CE's presence at runtime and
  branch accordingly: it MUST apply correctly when CE is loaded, and MUST simply not apply (not throw, not
  silently corrupt other behavior) when CE is absent — per Constitution Principle I.
- **FR-014**: If the Serpent's Eye hediff is removed from a pawn by any means, the system MUST stop applying
  all of its bonuses immediately, with no lingering effect.
- **FR-015**: The system MUST prevent any applied tattoo hediff (not Serpent's Eye alone — this applies
  uniformly to every tattoo, including Frost Sigil retroactively) from being removed via RimWorld's normal
  hediff-removal API by any caller other than this mod's own explicitly-sanctioned removal path, while MUST
  NOT block a developer/tester's deliberate use of Developer Mode / God Mode's own hediff-removal tooling.
  This is customer-identified, cross-cutting hardening surfaced during this feature's work, not a
  Serpent's-Eye-specific behavior; feature 002's own spec is not retroactively amended, but this requirement's
  protection covers its hediff too.

### Key Entities *(include if feature involves data)*

- **Serpent's Eye Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Serpent's Eye
  hediff — the Tier 1 accuracy bonus (plus CE-only sway/recoil reduction), and the stronger Tier 2 variant
  plus the CE-only ammo-effectiveness bonus.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying events (for Serpent's Eye: the
  wearer's own ranged shots that land) that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is
  reached — the same kind of counter feature 002 established for Frost Sigil, reusing its tracker.
- **"Own Ranged Hit Landed" Reuse Point**: The new, tattoo-agnostic detection mechanism this feature adds
  for "this pawn's own ranged attack just landed on a target" — the counterpart to feature 002's
  "this pawn was hit in melee" reuse point, intended for reuse by future ranged-themed tattoo effects.
- **CE-Branching Convention**: The pattern this feature establishes (or confirms, if adequate infrastructure
  already exists) for a tattoo effect to correctly apply a bonus only when Combat Extended is loaded,
  without erroring or misbehaving when it's absent.
- **Tattoo Hediff Removal Guard**: Cross-tattoo infrastructure (FR-015) that blocks any applied tattoo hediff
  from being removed except through this mod's own sanctioned removal path or an explicit Developer Mode/God
  Mode override — keyed generically off every `TattooMagicDef.appliedHediff` (feature 001's registry), so it
  covers all 11 tattoos, not just Serpent's Eye, without per-tattoo configuration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pawn with Serpent's Eye applied shows a measurably higher accuracy stat than an
  otherwise-identical pawn without it, in both a non-CE game and a CE-loaded game.
- **SC-002**: In a CE-loaded game, a pawn with Serpent's Eye applied shows measurably reduced sway/recoil
  compared to an otherwise-identical pawn without it; in a non-CE game, no error results from this portion of
  the effect having nothing to apply to.
- **SC-003**: A Serpent's Eye-tattooed pawn's progression counter increments only on the wearer's own landed
  ranged shots — not on misses, not on fully-deflected shots, and not on shots fired by other pawns —
  verified across repeated trials.
- **SC-004**: A tattooed pawn's Serpent's Eye automatically upgrades from Tier 1 to Tier 2 once the
  designated number of landed shots occurs, with zero additional player action required (no new ritual, no
  ingredient spend, no slot change).
- **SC-005**: After upgrading to Tier 2, the same pawn's accuracy (and, under CE, sway/recoil reduction) are
  measurably stronger than at Tier 1, and — under CE — the wearer's loaded ammunition shows a measurable
  effectiveness bonus not present at Tier 1 or on an untattooed pawn.
- **SC-006**: A Serpent's Eye-tattooed pawn's progression counter and current tier are unchanged immediately
  after a save/reload compared to immediately before it.
- **SC-007**: A future tattoo needing to react to "the wearer's own ranged (or similarly-themed) attack
  landing" can be implemented using the reuse point this feature adds, without modifying any of Serpent's
  Eye's own effect code.
- **SC-008**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony +
  Combat Extended, with zero errors in the RimWorld dev console in either configuration — the first passive
  tattoo to actually demonstrate this dual-mode requirement in practice (Frost Sigil's own stats never
  needed CE branching).
- **SC-009**: An attempt to remove any applied tattoo hediff (Serpent's Eye or Frost Sigil) through RimWorld's
  normal hediff-removal API, from any caller other than this mod's own sanctioned path, fails to remove it;
  the same removal action succeeds when performed via Developer Mode/God Mode's health-tab tooling.

## Assumptions

- **"Weapon sway/recoil" has no vanilla RimWorld equivalent** — sway and recoil are Combat Extended-specific
  mechanics; vanilla RimWorld's ranged combat model doesn't include them at all. This means Tier 1/Tier 2's
  sway/recoil reduction is inherently a CE-only portion of the effect: in a non-CE game there is nothing for
  it to apply to, which is expected/correct behavior (not a bug, not a "silently no-op" violation of
  Constitution Principle I, since there is no vanilla equivalent stat being ignored) rather than something
  requiring a vanilla fallback.
- **The Tier 2 "CE ammo effectiveness" bonus is likewise CE-exclusive** — vanilla RimWorld has no ammo-type
  system for it to apply to. Same reasoning as above: absent without CE is correct, not a gap.
- Exact numeric values — the accuracy bonus, the sway/recoil reduction amount, the ammo-effectiveness bonus,
  and the Tier 1→Tier 2 progression-counter threshold — are placeholder/balance-pass numbers per PRD §9,
  exactly as Frost Sigil's were. This feature only needs the mechanism to work correctly for whatever values
  are configured, not final tuned numbers.
- "Ranged hits landed" is interpreted as any of the wearer's own ranged-weapon shots (fired by the wearer,
  not thrown melee weapons or turret-assisted fire unless the wearer is the one directly firing) that deal
  damage to a target, mirroring Frost Sigil's "only successful/consequential events count" convention from
  feature 002.
- This feature covers exactly one tattoo's *gameplay effect*, Serpent's Eye. The other 3 remaining passive
  tattoos (Ember Ward, Ironskin Glyph, Starlight Ward) remain inert stub Hediffs after this feature ships;
  wiring up their individual effects is out of scope here. FR-015's removal guard is the one exception to
  this single-tattoo scoping — it is deliberately generic across every `TattooMagicDef.appliedHediff` and
  therefore already covers all 11 tattoos (including the 3 still-inert stubs) with no further work once they
  eventually ship their own effects.
- Per PRD §7, Serpent's Eye (or a dedicated ammo-focused tattoo) is explicitly the PRD's own candidate for
  demonstrating CE ammo/loadout integration — this feature is not inventing that requirement, it's fulfilling
  an already-identified PRD gap that Frost Sigil's slice didn't touch.
- The value-accessor-driven, override-ready design established by Frost Sigil (feature 002 FR-012) continues
  to apply here; this feature still does not build a player-facing settings UI (PRD §10 remains deferred).
- The feature 001 ritual/application flow (recipe, skill check, slot enforcement) is unaffected by this
  feature; this feature only adds gameplay effect behavior to a tattoo that can already be applied.
