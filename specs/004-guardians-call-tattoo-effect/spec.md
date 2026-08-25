# Feature Specification: Guardian's Call Tattoo Effect (First Triggered Ability + Two-Path AI Targeting Slice)

**Feature Branch**: `004-guardians-call-tattoo-effect`

**Created**: 2026-08-07

**Status**: Draft

**Input**: User description: "Wire up the Guardian's Call tattoo effect — the first triggered (gizmo + cooldown) tattoo, and the tattoo the customer originally asked for. Tier 1: activating the ability forces nearby hostile pawns to prioritize attacking this pawn for a duration, then the ability goes on cooldown (PRD §6, #11). Tier 2 (auto-upgrade once the usage-count threshold is reached, same 'times activated' progression counter pattern as other triggered tattoos per PRD §5.4): same taunt effect, plus a temporary HP/armor buffer ('shield wall') for the taunt's duration. This is the first tattoo needing genuinely new infrastructure beyond what features 002 (Frost Sigil) and 003 (Serpent's Eye) established: (1) a triggered-ability pattern (gizmo, cooldown, activation) that doesn't exist in the codebase yet; (2) AI-targeting interception, which per PRD §7 and Constitution Principle IV ('Two-Path Combat Patching') requires two independently-verified Harmony patch paths — one for vanilla's AttackTargetFinder-style hostile-target-selection logic, one for Combat Extended's equivalent override. This is the only tattoo in the full 11-tattoo roster that touches targeting AI. Explicitly out of scope: PRD §5.1's tattoo-mastery XP track (pawn-wide slot-unlock system) — deferred to its own future feature. This slice is scoped to just Guardian's Call's own taunt ability and its own per-tattoo tier-progression counter, reusing feature 002/003's existing passive-effect infrastructure wherever the Tier 2 buff or CE-awareness needs it, without modifying those files' existing behavior for Frost Sigil or Serpent's Eye."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Guardian's Call makes its wearer the target everyone attacks (Priority: P1)

A player applies the Guardian's Call tattoo to a colonist (via the existing ritual station). In combat, that colonist has an activatable ability (a gizmo, like RimWorld's other activatable abilities). Activating it forces nearby hostile pawns to prioritize attacking the tattooed colonist for a limited duration, pulling attention away from the rest of the colony — directly addressing the "raiders ignore my tank and focus down someone squishier" problem the tattoo exists to solve. After the duration ends, hostiles return to choosing targets normally, and the ability goes on cooldown before it can be used again.

**Why this priority**: This is the entire point of the feature, and the specific tattoo the customer asked for by name. It's the smallest slice that proves the ability actually taunts, and every other story here depends on it existing first.

**Independent Test**: Can be fully tested by applying Guardian's Call to a colonist, provoking a fight with multiple hostiles nearby, activating the ability, and observing that hostiles redirect their attacks to the tattooed colonist for the duration, then resume normal targeting once it ends — in both a non-CE game and a CE-loaded game.

**Acceptance Scenarios**:

1. **Given** a pawn with Guardian's Call applied and multiple hostiles nearby, **When** the player activates the ability, **Then** those hostiles prioritize attacking the tattooed pawn for the ability's duration, in both a non-CE game and a CE-loaded game.
2. **Given** the taunt's duration has elapsed, **When** hostiles next choose a target, **Then** they resume normal target selection (no longer specifically biased toward the tattooed pawn).
3. **Given** the ability was just activated, **When** the player looks at the gizmo, **Then** it shows the ability is on cooldown and cannot be activated again until the cooldown expires.
4. **Given** a pawn without Guardian's Call applied, **When** combat occurs nearby, **Then** no taunt gizmo appears for that pawn and hostile targeting is unaffected, regardless of what other tattoos or effects that pawn may have.
5. **Given** Combat Extended is loaded, **When** the ability is activated, **Then** the taunt still redirects hostile targeting correctly, verified independently of the non-CE code path (per Constitution Principle IV).

---

### User Story 2 - Guardian's Call grows stronger the more it's used (Priority: P2)

As a Guardian's Call-tattooed colonist keeps using the ability in combat, the tattoo tracks how many times it's been activated and automatically upgrades itself to a stronger Tier 2 version once enough uses have accumulated — with no extra ritual, ingredients, or tattoo slot required. At Tier 2, activating the ability still taunts nearby hostiles, and additionally grants the tattooed pawn a temporary defensive buffer for the taunt's duration, making it survivable to actually hold that attention.

**Why this priority**: This is the PRD §5.4 tiering mechanic, mirroring User Story 2 of features 002 and 003. It only makes sense once Tier 1's taunt (User Story 1) exists and can be observed, so it's the natural second slice — and Tier 2's defensive buffer is what makes tanking the taunted attacks survivable rather than just heroic.

**Independent Test**: Can be tested independently by activating a Guardian's Call-tattooed pawn's ability enough times to reach the threshold, then confirming the tattoo's effect strengthens in place — the taunt still works, and the Tier 2 defensive buffer is now present for the duration of subsequent activations.

**Acceptance Scenarios**:

1. **Given** Tier 1 Guardian's Call applied with its progression counter below threshold, **When** the player activates the ability, **Then** the counter increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering activation resolves, **Then** Guardian's Call upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to the pawn's slot usage.
3. **Given** Guardian's Call is at Tier 2, **When** the wearer activates the ability, **Then** the taunt still applies as before, and the wearer additionally gains a temporary HP/armor buffer for the duration of that activation.
4. **Given** the temporary Tier 2 buffer's duration has elapsed, **When** it expires, **Then** the pawn's defenses return to their normal (non-buffed) values with no lingering effect.

---

### User Story 3 - The ability and targeting patterns can be reused for future tattoos (Priority: P3)

Having shipped Guardian's Call, a developer picking up a future triggered tattoo (Bloodrune, Stormlash, Wraithstep, Berserker's Mark) can reuse the gizmo/cooldown/activation pattern this feature establishes, without duplicating Guardian's Call's own taunt-specific logic. Separately, if any future tattoo ever needs to influence AI targeting again, the two-path (vanilla + Combat Extended) patch convention this feature establishes is there to follow.

**Why this priority**: This is the same "establish the pattern" rationale features 002 and 003 had for their own reusable infrastructure. It matters less than Guardian's Call actually taunting (User Stories 1-2) because there's nothing proven to reuse until the ability itself works correctly, and — unlike the passive-effect reuse points from 002/003 — this feature is not required to prove a second consumer exists yet (no other triggered tattoo ships in this slice).

**Independent Test**: Can be tested by reviewing whatever new reuse points this feature adds and confirming a hypothetical future triggered tattoo could adopt the ability/cooldown pattern via its own data/configuration, and a hypothetical future targeting-related tattoo could follow the two-path patch convention, without editing Guardian's Call's own files.

**Acceptance Scenarios**:

1. **Given** the value accessor and tier-progression tracker features 002/003 already established, **When** Guardian's Call needs a tiered progression counter, **Then** it reuses `TattooTierProgress` directly, with no modification to feature 002/003's code.
2. **Given** the new triggered-ability (gizmo/cooldown/activation) pattern this feature adds, **When** a future triggered tattoo needs the same kind of "click to activate, then cooldown" mechanic, **Then** it can reuse that pattern with its own configuration, without duplicating Guardian's Call's own taunt logic.
3. **Given** the two-path AI-targeting patch convention this feature establishes, **When** a future tattoo needs to influence hostile targeting again, **Then** it can follow the same vanilla+CE dual-patch convention rather than inventing a new one.

---

### Edge Cases

- What happens if the tattooed pawn is downed, killed, or otherwise removed from combat while the taunt duration is still active? The taunt effect ends immediately for that pawn (there is no one left to redirect attacks toward); hostiles resume normal targeting rather than continuing to fixate on a downed or dead pawn.
- What happens if no hostiles are within range when the ability is activated? The ability still activates and goes on cooldown as normal — activation is not refunded or blocked by the absence of a valid target, consistent with how RimWorld's other activatable abilities behave.
- What happens to a hostile that enters taunt range partway through the duration, after having already chosen a different target? It is still affected — the taunt is a standing bias in effect for its full duration, not a one-time redirect applied only to hostiles in range at the instant of activation (matching the PRD's "prioritize attacking this pawn for a duration" wording, which describes an ongoing state, not a single snapshot event).
- What happens if two different pawns both have active Guardian's Call taunts in effect at the same time? Each hostile pawn's own targeting decision resolves independently and may favor either taunting pawn; this feature does not define a stacking or priority rule between simultaneous taunts, since the PRD does not call for one and it is not needed for the core taunt-a-tank use case.
- What happens if the ability is activated again while still on cooldown? The gizmo itself prevents this (RimWorld's standard cooldown-gizmo behavior disables activation until the cooldown expires) — this is not a state the underlying taunt/targeting logic needs to separately guard against.
- What happens to the progression counter and tier state across a save and reload? Both MUST persist unchanged, exactly as required of Frost Sigil and Serpent's Eye.
- What happens if some other system removes the Guardian's Call hediff from the pawn? All of its effects (the gizmo, any active taunt, any active Tier 2 buffer) MUST stop applying immediately once the hediff is gone, and the existing cross-tattoo removal guard (FR-015, feature 003) already prevents anything but this mod's own sanctioned path or Developer Mode/God Mode from removing it in the first place.
- What happens if Combat Extended is loaded, then unloaded (or vice versa) between sessions on the same save? The taunt must correctly use whichever targeting patch path (vanilla or CE) matches CE's current presence; this MUST NOT throw or corrupt saved tier/counter state either way.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While a pawn has Guardian's Call applied (at either tier), the system MUST grant that pawn an activatable ability (gizmo) that, when used, forces hostile pawns within range to prioritize attacking the tattooed pawn for a fixed duration.
- **FR-002**: After the ability is activated, it MUST become unavailable for a fixed cooldown period before it can be activated again, with the remaining cooldown visible to the player via the gizmo.
- **FR-003**: The taunt effect MUST correctly redirect hostile targeting in both a non-CE game and a CE-loaded game, via two independently-verified Harmony patch paths (Constitution Principle IV) — one for vanilla's hostile-target-selection logic, one for Combat Extended's equivalent.
- **FR-004**: The system MUST track a per-pawn, per-tattoo progression counter for Guardian's Call that increments each time the wearer activates the ability, per the "times activated" counter defined for triggered tattoos (PRD §5.4).
- **FR-005**: When Guardian's Call's progression counter reaches its Tier 2 threshold, the system MUST automatically upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or any change to the pawn's tattoo-slot usage.
- **FR-006**: At Tier 2, activating the ability MUST additionally grant the wearer a temporary increase to their armor-rating stat(s) for the duration of that activation, on top of the unchanged taunt effect, using the same passive stat-offset contract (`IProvidesTattooStatOffset`) features 002/003 already established, rather than a new damage-absorption mechanic.
- **FR-007**: Guardian's Call's Tier 2 armor buff MUST target the same armor-related StatDef(s) — vanilla, and Combat Extended's equivalent(s) when CE is loaded — that any other tattoo's own armor bonus would use, so that it stacks additively with any other simultaneously-applied tattoo's armor bonus (e.g. a future Ironskin Glyph) via the existing shared `StatPart_TattooEffectOffset` summation mechanism, without Guardian's Call needing special-case knowledge of any other specific tattoo.
- **FR-008**: The progression counter's current value and the tattoo's current tier MUST persist correctly across a game save and reload.
- **FR-009**: None of Guardian's Call's effects (gizmo, taunt, Tier 2 buffer) MUST apply to any pawn that does not currently have Guardian's Call applied.
- **FR-010**: Guardian's Call's effect MUST be implemented through the existing `appliedHediff` contract established by feature 001 and reused by features 002/003 (the `TattooDef` → `HediffDef` → Hediff/HediffComp chain), rather than a new or parallel mechanism for granting or tracking tattoo state.
- **FR-011**: Every tunable numeric value driving Guardian's Call's effect (taunt duration, taunt range, cooldown length, the Tier 2 threshold, and the Tier 2 buff's magnitude/duration) MUST be read through feature 002's `TattooEffectValues` accessor rather than read directly from XML/Def fields at each use site, consistent with FR-011/FR-012 of features 002/003.
- **FR-012**: The system MUST reuse feature 002's existing tier-progression tracker (`TattooTierProgress`) without modification, and MUST reuse feature 003's `CombatExtendedInterop` CE-detection convention for any CE-specific branching this feature needs, rather than re-implementing CE detection inline.
- **FR-013**: The taunt-targeting patches MUST detect Combat Extended's presence at runtime and apply the correct patch path accordingly: the vanilla-targeting patch when CE is absent, and both the vanilla-targeting-equivalent and CE's own targeting patch verified together when CE is loaded, per Constitution Principle IV — neither path may throw or silently no-op when its corresponding configuration is active.
- **FR-014**: If the Guardian's Call hediff is removed from a pawn by any means, the system MUST stop applying all of its effects immediately, with no lingering gizmo, taunt, or buff.
- **FR-015**: The Guardian's Call hediff MUST be protected by the existing cross-tattoo removal guard (feature 003, FR-015) with no additional per-tattoo configuration required, since that guard already covers every `TattooMagicDef.appliedHediff` generically.

### Key Entities *(include if feature involves data)*

- **Guardian's Call Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Guardian's Call hediff — the activatable taunt ability and its cooldown at Tier 1, plus a temporary armor-stat increase (via the shared passive stat-offset contract, stacking additively with any other tattoo's armor bonus) for the duration of each activation at Tier 2.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of qualifying events (for Guardian's Call: number of times the ability has been activated) that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is reached — reusing feature 002's tracker.
- **Triggered-Ability Reuse Point**: The new, tattoo-agnostic gizmo/cooldown/activation mechanism this feature adds for "a tattoo grants an activatable ability with a cooldown," intended for reuse by future triggered-tattoo effects (Bloodrune, Stormlash, Wraithstep, Berserker's Mark).
- **Two-Path AI-Targeting Convention**: The pattern this feature establishes for correctly influencing hostile-pawn targeting decisions in both a vanilla game and a Combat-Extended-loaded game, per Constitution Principle IV.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When a pawn with Guardian's Call activates the ability, hostile pawns within taunt range measurably redirect their attacks toward that pawn for the ability's duration, in both a non-CE game and a CE-loaded game.
- **SC-002**: After the taunt's duration elapses, hostile pawns resume normal target selection with no lingering bias toward the previously-taunted pawn.
- **SC-003**: The ability cannot be re-activated until its cooldown period has elapsed, verified across repeated activation attempts.
- **SC-004**: A Guardian's Call-tattooed pawn's progression counter increments only on the wearer's own ability activations, verified across repeated trials.
- **SC-005**: A tattooed pawn's Guardian's Call automatically upgrades from Tier 1 to Tier 2 once the designated number of activations occurs, with zero additional player action required (no new ritual, no ingredient spend, no slot change).
- **SC-006**: After upgrading to Tier 2, activating the ability grants a measurably higher armor-rating stat in addition to the unchanged taunt effect, and that increase measurably expires at the end of the activation's duration.
- **SC-007**: A Guardian's Call-tattooed pawn's progression counter and current tier are unchanged immediately after a save/reload compared to immediately before it.
- **SC-008**: A future triggered tattoo needing "an activatable ability with a cooldown" can be implemented using the reuse point this feature adds, without modifying any of Guardian's Call's own effect code.
- **SC-009**: A future tattoo needing to influence AI targeting can follow the two-path (vanilla + CE) patch convention this feature establishes, without inventing a new approach from scratch.
- **SC-010**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat Extended, with zero errors in the RimWorld dev console in either configuration.
- **SC-011**: An attempt to remove the Guardian's Call hediff through RimWorld's normal hediff-removal API, from any caller other than this mod's own sanctioned path, fails to remove it, consistent with feature 003's existing cross-tattoo removal guard.
- **SC-012**: The Tier 2 armor buff is confirmed (by the same kind of code-level review SC-008/SC-009 rely on, since no second armor-offsetting tattoo exists yet to test live) to route through the existing generic, per-stat `StatPart_TattooEffectOffset` summation — not a Guardian's-Call-specific override — so it is structurally guaranteed to add together with any other current or future tattoo's bonus to the same armor stat(s), rather than overriding or suppressing it.

## Assumptions

- **Tier 2's "temporary HP/armor buffer" mechanism** (resolved): a temporary increase to the pawn's armor-rating stat(s), via the existing `IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset` contract with a time-limited grant — not a new damage-absorption "shield" mechanic. Chosen specifically so it stacks additively with a future Ironskin Glyph's own (permanent) armor bonus via the same shared summation mechanism, since the customer explicitly wants that synergy for building a genuinely tanky pawn (2026-08-07 discussion). Because Ironskin Glyph doesn't exist yet, this feature must register its buff against whichever armor-related StatDef(s) (vanilla, and CE's equivalent when loaded) a future Ironskin Glyph would also target, so the stacking happens automatically without either tattoo needing to know about the other — see FR-006/FR-007.
- Taunt range, taunt duration, cooldown length, the Tier 1→Tier 2 activation-count threshold, and the Tier 2 buffer's own magnitude/duration are placeholder/balance-pass numbers per PRD §9, exactly as every other tattoo's numeric values have been so far. This feature only needs the mechanism to work correctly for whatever values are configured, not final tuned numbers.
- "Nearby hostile pawns" (PRD §6) is interpreted as hostile pawns within a tunable radius of the tattooed pawn at the time their own targeting decision is (re-)evaluated, not a one-time snapshot at the moment of activation — see Edge Cases.
- The taunt affects a hostile's target selection regardless of whether that hostile is a melee or ranged attacker — PRD's "forces nearby hostiles to prioritize attacking this pawn" is unqualified, mirroring how Frost Sigil's "melee attacker" wasn't restricted to armed attackers only (feature 002 precedent).
- This feature covers exactly one tattoo's gameplay effect, Guardian's Call. The remaining triggered tattoos (Bloodrune, Stormlash, Wraithstep, Berserker's Mark) are out of scope here; wiring up their individual effects is future work that reuses the ability/cooldown pattern this feature establishes.
- PRD §5.1's tattoo-mastery XP track (pawn-wide slot unlocks) is explicitly out of scope for this feature, per the customer/product decision to defer it to its own future feature rather than bundle it with Guardian's Call.
- The value-accessor-driven, override-ready design established by features 002/003 continues to apply here; this feature still does not build a player-facing settings UI (PRD §10 remains deferred).
- The feature 001 ritual/application flow (recipe, skill check, slot enforcement) is unaffected by this feature; this feature only adds gameplay effect behavior to a tattoo that can already be applied.
