# Feature Specification: Tattoo-Mastery XP Track & Slot Unlocking

**Feature Branch**: `006-tattoo-mastery-xp-track`

**Created**: 2026-08-09

**Status**: Draft

**Input**: User description: "Tattoo-mastery XP track and slot unlocking. Per PRD.md §5.1: every pawn starts with 1 tattoo slot (already implemented as HediffComp_TattooTracker.slotCapacity, currently hardcoded to 1). Add a hidden per-pawn tattoo-mastery XP stat, distinct from vanilla skills, that gains XP when the pawn's active (triggered) tattoo abilities are used in combat (Guardian's Call, Stormlash today; more triggered tattoos coming). Two XP thresholds gate unlocking slot 2 and slot 3 (3 total slots max). Exact threshold numbers are placeholders pending balance testing (consistent with how other tattoo numbers have been placeholders in prior slices) but the mechanism must be real: crossing a threshold should grow slotCapacity in place, be visible to the player (e.g. a message/notification and something inspectable on the pawn), and persist through save/load. This is a foundational systems slice (not a new tattoo), following slices 001-005 (ritual station, Frost Sigil, Serpent's Eye, Guardian's Call, Stormlash) which currently cap every pawn at 1 slot regardless of this track."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A pawn earns a second tattoo slot by using their triggered tattoo abilities (Priority: P1)

A player has a colonist with a triggered tattoo applied (e.g. Guardian's Call or Stormlash). As that colonist activates the ability during play, they accumulate hidden "tattoo mastery" progress. Once enough activations have accumulated, the colonist's number of usable tattoo slots increases from 1 to 2, without any player action beyond normal play — no new ritual, no ingredient cost. The player is told this happened and can see it reflected the next time they open the ritual station for that pawn (an additional free slot is available to fill).

**Why this priority**: This is the entire point of the feature — the base mechanism the PRD describes. Every other story (the second threshold, visibility, persistence) is only meaningful once a slot can actually be earned at all.

**Independent Test**: Can be fully tested by applying a triggered tattoo to a pawn, activating its ability repeatedly, and observing that the pawn's slot capacity increases from 1 to 2 once the first threshold is reached, with a second tattoo then applicable at the ritual station.

**Acceptance Scenarios**:

1. **Given** a pawn with a triggered tattoo applied and slot capacity 1, **When** the pawn activates that tattoo's ability, **Then** the pawn's hidden tattoo-mastery progress increases by one qualifying event.
2. **Given** a pawn's tattoo-mastery progress reaches the slot-2 threshold, **When** the triggering activation resolves, **Then** the pawn's slot capacity increases from 1 to 2 immediately, with no ritual, ingredient cost, or player action required beyond the activation itself.
3. **Given** a pawn whose slot capacity just increased to 2, **When** the player opens the ritual station for that pawn, **Then** a second tattoo can be selected and applied, subject to the same ritual rules as the pawn's first tattoo.
4. **Given** a pawn with no tattoo applied at all, **When** time passes or combat occurs, **Then** that pawn's tattoo-mastery progress does not increase, since there is no tattoo comp for any event to be dispatched to.
5. **Given** a pawn with only a passive tattoo applied (e.g. Frost Sigil, Serpent's Eye), **When** that tattoo's own on-hit event is dispatched (a melee hit taken, a ranged hit landed), **Then** the pawn's tattoo-mastery progress increases by one qualifying event, the same pawn-wide counter triggered tattoos contribute to — revised 2026-08-12 (playtesting finding: a passive-only pawn had no path to ever unlock a second slot); see Assumptions and research.md R10.

---

### User Story 2 - A pawn earns a third and final slot the same way (Priority: P2)

Having already earned a second slot, the same colonist keeps using their triggered tattoo ability (or a second triggered tattoo's ability, once applied). Once a second, higher threshold of accumulated activations is reached, the colonist's slot capacity increases again, from 2 to 3 — the maximum described in the PRD. Further activations beyond this point continue to be tracked but no longer grant additional slots.

**Why this priority**: This completes the progression PRD §5.1 describes (1 → 3 slots via two thresholds). It depends on User Story 1's mechanism existing first, and is a smaller increment once that mechanism is proven.

**Independent Test**: Can be tested independently by continuing to activate a pawn's triggered ability(s) past the slot-2 threshold until the slot-3 threshold is reached, and confirming slot capacity becomes 3, then confirming further activations no longer change slot capacity.

**Acceptance Scenarios**:

1. **Given** a pawn already at slot capacity 2, **When** their tattoo-mastery progress reaches the slot-3 threshold, **Then** their slot capacity increases from 2 to 3 immediately.
2. **Given** a pawn already at slot capacity 3, **When** the pawn activates a triggered tattoo ability again, **Then** their tattoo-mastery progress may continue to be tracked, but slot capacity remains 3 (no further slots are ever granted).
3. **Given** a pawn who has activated triggered abilities from two different tattoos (e.g. both Guardian's Call and Stormlash), **When** each activation occurs, **Then** each contributes to the same single pawn-wide tattoo-mastery progress total, rather than tracking separate progress per tattoo.

---

### User Story 3 - The player is told when a pawn earns a new slot, and it isn't lost on save/reload (Priority: P2)

Slot unlocks happen automatically during play, with no dedicated UI screen for the mastery track — so the player needs some signal that it happened, and needs to trust that the pawn's progress and slot count survive saving and reloading the game like everything else about that pawn.

**Why this priority**: Without visibility, an automatic background system is invisible and confusing ("why does this pawn suddenly have 2 slots?"); without persistence, the whole mechanism is worthless the moment the player reloads. Both are necessary for the feature to be trustworthy, but neither changes the core mechanism from User Stories 1-2, so they're a notch lower priority.

**Independent Test**: Can be tested by triggering a slot unlock and confirming an on-screen notification names the pawn and the new slot count, that the pawn's tattoo-mastery progress and current slot capacity are inspectable (e.g. via the pawn's health/tattoo UI), and that saving and reloading the game leaves both values unchanged.

**Acceptance Scenarios**:

1. **Given** a pawn's tattoo-mastery progress reaches a slot threshold, **When** the slot capacity increases, **Then** the player receives an on-screen notification identifying the pawn and the new total slot count.
2. **Given** a pawn with some amount of tattoo-mastery progress and a given slot capacity, **When** the player inspects that pawn, **Then** the current slot capacity is visible somewhere in the existing tattoo-related UI.
3. **Given** a pawn with partial progress toward a threshold (not yet reached), **When** the game is saved and reloaded, **Then** that pawn's progress total and current slot capacity are unchanged from immediately before the save.
4. **Given** a pawn who has already unlocked slot 2 or slot 3, **When** the game is saved and reloaded, **Then** the pawn retains that slot capacity and does not revert to fewer slots.

---

### Edge Cases

- What happens if a pawn dies or is removed from the game while partway toward a threshold? Their progress simply stops mattering along with the rest of their state; no special cleanup is required beyond what already happens when a pawn's hediffs cease to exist.
- What happens if a pawn has zero tattoos applied at all (no `HediffComp_TattooTracker` activity yet)? Tattoo-mastery progress cannot increase, since there is no triggered ability to activate; slot capacity remains at its starting value of 1.
- What happens if a triggered tattoo's ability is activated but fails to actually take effect for some other reason (e.g. blocked by an edge case in that tattoo's own logic)? Out of scope for this feature to distinguish — this feature counts activations at the same point each triggered tattoo already counts its own per-tattoo tier-progression activations (§5.4), so mastery progress and that tattoo's own tier progress stay consistent with each other.
- What happens if a pawn's slot capacity is already at 3 and somehow gains further tattoo-mastery progress (e.g. from a future third triggered tattoo)? Progress keeps being tracked harmlessly; slot capacity is capped at 3 and never exceeds it.
- What happens to a pawn's already-applied tattoos if slot capacity were ever reduced by some other means? Out of scope — this feature only ever grows slot capacity upward from 1 toward 3, it never reduces it, so existing applied tattoos are never put at risk of exceeding capacity by this feature's own logic.
- Does passive-tattoo use (Frost Sigil, Serpent's Eye) contribute to tattoo-mastery progress? **Yes, revised 2026-08-12** — originally scoped out (only triggered-tattoo ability activations counted), but playtesting surfaced that a pawn who only ever receives passive tattoos would then have *no possible path* to ever unlock slot 2/3, despite the mechanism existing. Now: every dispatched on-hit notification (`IOnMeleeHitTattooEffect.OnMeleeHitTaken`, `IOnRangedHitLandedTattooEffect.OnRangedHitLanded`) also credits one qualifying event to the same pawn-wide `masteryProgress` counter triggered tattoos use — independent of, and in addition to, that passive tattoo's own separate tier-progression counter (§5.4), which keeps its own gating unchanged (e.g. Frost Sigil's tier counter still only advances on a successful slow proc; mastery progress advances on every dispatched hit regardless). See research.md R10.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every pawn MUST start with a tattoo-mastery progress total of zero and a slot capacity of 1, matching current behavior.
- **FR-002**: Each time a pawn activates any triggered tattoo's ability, the system MUST increase that pawn's tattoo-mastery progress by one qualifying event, regardless of which triggered tattoo was activated.
- **FR-003**: Tattoo-mastery progress MUST be tracked once per pawn (not per tattoo), so activations from multiple different triggered tattoos applied to the same pawn all contribute to the same total.
- **FR-004** *(revised 2026-08-12)*: Each time a passive tattoo's on-hit event is dispatched to it (a melee hit taken via `IOnMeleeHitTattooEffect`, a ranged hit landed via `IOnRangedHitLandedTattooEffect`), the system MUST increase the comp-owning pawn's tattoo-mastery progress by one qualifying event, the same pawn-wide total FR-002 feeds — independent of whether that tattoo's own effect internally procs. *(Originally: passive events MUST NOT contribute at all; reversed after playtesting found this left passive-only pawns with no path to unlock any slot — see Assumptions, research.md R10.)*
- **FR-005**: The system MUST define two ascending tattoo-mastery progress thresholds: one that unlocks a pawn's second tattoo slot, and one (higher) that unlocks a pawn's third tattoo slot.
- **FR-006**: When a pawn's tattoo-mastery progress reaches the second-slot threshold, the system MUST immediately increase that pawn's slot capacity from 1 to 2, with no ritual, ingredient cost, or other player action required.
- **FR-007**: When a pawn's tattoo-mastery progress reaches the third-slot threshold, the system MUST immediately increase that pawn's slot capacity from 2 to 3, with no ritual, ingredient cost, or other player action required.
- **FR-008**: A pawn's slot capacity MUST never exceed 3 regardless of how much further tattoo-mastery progress accumulates beyond the third-slot threshold.
- **FR-009**: When a pawn's slot capacity increases, the system MUST display an on-screen notification identifying the pawn and its new slot capacity.
- **FR-010**: A pawn's current slot capacity MUST be visible to the player through the existing tattoo-related UI (the ritual station's tattoo-selection UI established by feature 001, at minimum).
- **FR-011**: A pawn's tattoo-mastery progress total and current slot capacity MUST persist correctly across a game save and reload.
- **FR-012**: The two threshold values (progress required for slot 2, progress required for slot 3) MUST be configurable tunable values rather than hardcoded literals, consistent with every other placeholder numeric value shipped so far (PRD §9), and MUST be read through the existing `TattooEffectValues` accessor convention established by feature 002.
- **FR-013**: This feature MUST reuse the existing `HediffComp_TattooTracker.slotCapacity` field as the single source of truth for a pawn's slot capacity, rather than introducing a second, competing notion of slot count.
- **FR-014**: This feature MUST hook into the existing triggered-tattoo activation path (the `TryActivate` entry point already implemented by Guardian's Call and Stormlash) to register a qualifying event, rather than introducing a separate, parallel way of detecting "the ability was used."
- **FR-015**: Any future triggered tattoo (Bloodrune, Wraithstep, Berserker's Mark) that implements the same triggered-ability activation pattern MUST automatically contribute to tattoo-mastery progress with no per-tattoo opt-in code, the same way Guardian's Call and Stormlash do.
- **FR-016** *(added 2026-08-12)*: This feature MUST hook passive-tattoo contribution into the existing shared `Patch_Pawn_PostApplyDamage_TattooOnHit` dispatch point (feature 002/003) rather than introducing a separate, parallel way of detecting "a passive tattoo did something."
- **FR-017** *(added 2026-08-12)*: Any future passive tattoo that implements `IOnMeleeHitTattooEffect` or `IOnRangedHitLandedTattooEffect` MUST automatically contribute to tattoo-mastery progress with no per-tattoo opt-in code, mirroring FR-015's guarantee for triggered tattoos.

### Key Entities *(include if feature involves data)*

- **Tattoo-Mastery Progress**: A single hidden, per-pawn counter (distinct from vanilla RimWorld skills) that increments once per triggered-tattoo ability activation, regardless of which triggered tattoo was activated. Drives slot unlocking and persists with the pawn.
- **Slot Capacity**: The existing per-pawn value (`HediffComp_TattooTracker.slotCapacity`) representing how many tattoos a pawn may currently have applied at once. This feature is the mechanism that grows it from 1 toward a maximum of 3; it does not change how slot capacity is consumed or enforced at the ritual station.
- **Slot Unlock Thresholds**: Two ascending tattoo-mastery progress values — one gating the 1→2 slot increase, one gating the 2→3 slot increase — configured as tunable placeholder values pending balance testing.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pawn's tattoo-mastery progress increases by exactly one for each triggered-tattoo ability activation, verified across repeated activations and across multiple different triggered tattoos applied to the same pawn.
- **SC-002**: A pawn's slot capacity increases from 1 to 2 automatically and immediately once tattoo-mastery progress reaches the slot-2 threshold, with zero additional player action (no ritual, no ingredient spend).
- **SC-003**: A pawn's slot capacity increases from 2 to 3 automatically and immediately once tattoo-mastery progress reaches the slot-3 threshold, with zero additional player action.
- **SC-004**: A pawn's slot capacity never exceeds 3, verified by continuing to activate triggered abilities well past the slot-3 threshold.
- **SC-005**: Every slot-capacity increase produces an on-screen notification naming the pawn and its new slot count.
- **SC-006**: A pawn's current slot capacity is visible in the ritual station's tattoo-selection UI at all times.
- **SC-007**: A pawn's tattoo-mastery progress total and slot capacity are unchanged immediately after a save/reload compared to immediately before it, whether or not a threshold has been crossed.
- **SC-008** *(revised 2026-08-12)*: A pawn with only passive tattoos applied (no triggered tattoo) still accumulates tattoo-mastery progress from real on-hit combat events (a melee hit taken, a ranged hit landed) and can still unlock slot 2/3 through that alone — it is not permanently stuck at slot capacity 1 the way an entirely tattoo-less pawn is. *(Originally the inverse: passive-only pawns never accumulated progress at all; reversed after playtesting found that left them with no path to unlock any slot.)*
- **SC-009**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat Extended, with zero errors in the RimWorld dev console in either configuration.

## Assumptions

- The two threshold values (slot-2, slot-3) are placeholder/balance-pass numbers per PRD §9, exactly as every other tattoo's numeric values have been so far. This feature only needs the mechanism to work correctly for whatever values are configured, not final tuned numbers.
- "Active tattoo abilities are used" (PRD §5.1) is interpreted as a successful activation of a triggered tattoo's ability gizmo (the same event each triggered tattoo's own `TryActivate` already fires on), not merely the ability becoming available or the gizmo being drawn on screen.
- Tattoo-mastery progress is tracked pawn-wide, not per-tattoo — a pawn with two triggered tattoos applied progresses toward their next slot from either tattoo's use, consistent with the PRD's framing of a single per-pawn "tattoo-mastery XP track" distinct from each tattoo's own individual tier-progression counter (§5.4).
- No dedicated new UI screen is required for the mastery track itself; a notification on unlock plus an existing-UI display of current slot capacity (e.g. the ritual station's tattoo picker) satisfies visibility. A full "XP bar" or similar detailed progress display is out of scope unless requested later.
- *(Superseded 2026-08-12 — see FR-004/FR-016/FR-017, SC-008, research.md R10.)* ~~Because passive tattoos never activate an ability, only triggered tattoos can currently generate tattoo-mastery progress; this is expected given the roster's current state (4 of 11 tattoos shipped, a mix of both types) and is not a gap this feature needs to work around.~~ Passive tattoos now contribute too, via the existing on-hit dispatch patch, once per dispatched notification. This is a deliberately coarser granularity than each passive tattoo's own private tier-progression counter (e.g. Frost Sigil's own counter only advances on a successful proc; mastery progress advances on every dispatched hit regardless of that internal roll) — a genuine balance-pass concern flagged in `docs/PRD.md` §9 alongside the threshold placeholders, not resolved by this feature, since getting a proc-accurate signal out to the shared dispatch point would require touching each passive tattoo's own file, contradicting FR-017's no-opt-in guarantee.
- This feature does not change ritual/application rules (recipe, skill check) established by feature 001 — it only changes how many slots a pawn has available to apply tattoos into.
- This feature does not add tattoo removal/respec (PRD §3 non-goal) or a player-facing settings UI for the threshold values (PRD §10); thresholds remain XML/def-configurable only, consistent with every other tattoo's numeric values so far.
