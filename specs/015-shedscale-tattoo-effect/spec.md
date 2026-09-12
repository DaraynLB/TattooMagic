# Feature Specification: Shedscale Tattoo Effect

**Feature Branch**: `015-shedscale-tattoo-effect`

**Created**: 2026-09-12

**Status**: Draft

**Input**: User description: "Shedscale tattoo effect — a reptile-themed regrowth tattoo, following the same pattern as the other 14 tattoos in this mod. Full finalized design is written up in docs/proposals/2026-08-31-shedscale-tattoo.md: automatic, per-part regrowth of missing body parts over a tiered wait period (7 days Tier 1 / 4 days Tier 2), with no one-at-a-time restriction; an installed prosthetic/peg leg/bionic (including bionic organs) blocks the timer from starting at all rather than banking partial progress, cleared by uninstalling it; Tier 1 regrowth comes back at reduced efficiency (75-85%) as a permanent condition, fully removed by reaching Tier 2 both going forward and retroactively on already-regrown parts; stacking hunger and pain per concurrently-regrowing part, stalled by malnutrition; a flat (non-stacking) mood debuff while any regrowth is active; regrowing a part clears its old permanent injuries as the upside; Tier 2 is reached permanently on whichever comes first of a placeholder 30 days worn or 3 parts regrown; and no hard colony-wide rarity cap, unlike Phoenix."

## Clarifications

### Session 2026-09-12

- Q: If a colonist's Shedscale tattoo is removed while a body part is mid-regrowth, what happens to that part's in-progress regrowth? → A: The regrowth immediately halts and the part stays missing indefinitely; only applying a new Shedscale tattoo (which starts the full duration fresh) can resume it.
- Q: If a colonist's Shedscale tattoo is removed and a new one is later applied before Tier 2 was ever reached, does the Tier 2 progress clock (days worn / parts regrown) reset to zero along with it? → A: Yes, it resets to zero — the clock belongs to the current tattoo instance, not a colonist-lifetime stat.
- Q: What form should the "regrowth is blocked by an installed prosthetic/bionic" warning take? → A: A persistent alert in RimWorld's top-right alert list, active every day the block continues.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A missing limb quietly grows back (Priority: P1)

A colonist wearing a Shedscale tattoo is missing a hand, an eye, or some other eligible body part. The player doesn't do anything — no gizmo, no ability to trigger — and days later the part is simply back, as good as new and free of whatever old scars or injuries it used to carry.

**Why this priority**: This is the entire reason the tattoo exists — the core automatic regrowth loop the customer asked for by name, explicitly patterned on how Phoenix's revival works without player input. Nothing else in this spec matters if this doesn't work.

**Independent Test**: Apply a Shedscale tattoo to a colonist missing an eligible part (with no prosthetic installed on it), wait the tier's regrowth duration, and observe the part return on its own, with any pre-existing permanent injuries on that part gone.

**Acceptance Scenarios**:

1. **Given** a colonist wearing a Tier 1 Shedscale tattoo is missing an eligible external part (e.g. a hand) with nothing installed on it, **When** 7 days elapse, **Then** the part returns automatically with no player action, at reduced (75-85%) efficiency.
2. **Given** a Tier 2 wearer is missing the same kind of part, **When** 4 days elapse, **Then** the part returns automatically at full efficiency, with no penalty.
3. **Given** a colonist is missing an internal organ (e.g. a kidney) and wears a Tier 1 tattoo, **When** any amount of time passes, **Then** the organ does not regrow — Tier 1 only covers external limbs and extremities (fingers, toes, hands, feet, arms, legs, ears, nose, jaw, eyes).
4. **Given** that same colonist reaches Tier 2, **When** the organ becomes eligible under Tier 2's broader coverage, **Then** its regrowth timer starts and completes in 4 days like any other part.
5. **Given** a part that regrows had old permanent injuries on it before it was lost (an old scar, a badly set shoulder, a cataract), **When** regrowth completes, **Then** those permanent injuries are gone — the new part is a clean slate.
6. **Given** a colonist is missing their brain, spine, neck, torso, or pelvis, **When** the system evaluates eligible parts, **Then** none of those parts are ever considered — losing them means death, which this tattoo does not address.
7. **Given** a colonist has a present-but-scarred limb (not actually missing), **When** the system evaluates eligible parts, **Then** that limb is never touched — Shedscale only acts on parts that are genuinely gone, never as a voluntary "reroll" on a damaged-but-present part.
8. **Given** a colonist is missing more than one eligible part at once, **When** regrowth is evaluated, **Then** every eligible missing part starts regrowing simultaneously — there is no one-at-a-time restriction.

---

### User Story 2 - Prosthetics and bionics block regrowth until removed (Priority: P2)

A colonist missing a leg has a peg leg installed on the stump. The player sees a warning that regrowth is available but blocked, and knows that pulling the peg leg (via a normal surgery bill) is what it takes to let the tattoo start regrowing that leg from scratch.

**Why this priority**: Without this rule, Shedscale would compete directly with, rather than sit alongside, the game's existing prosthetics/bionics economy — this gating is core to how the tattoo is meant to coexist with them, not an edge-case afterthought.

**Independent Test**: Install a prosthetic on a missing eligible part of a Shedscale-tattooed colonist, confirm a warning appears and no regrowth timer progresses no matter how long is waited, then remove the prosthetic via a surgery bill and confirm the full tier duration starts fresh from that point.

**Acceptance Scenarios**:

1. **Given** a colonist is missing an eligible part that currently has a prosthetic, peg leg, or bionic installed, **When** the system evaluates regrowth eligibility, **Then** regrowth does not start on that part and a persistent alert appears in the alert list flagging that regrowth exists but is blocked by an installed replacement, remaining present every day the block continues.
2. **Given** a part's regrowth is blocked by an installed replacement, **When** any amount of time passes while it remains installed, **Then** no progress is banked toward that part's regrowth timer — resuming later always starts from zero, never from partial credit.
3. **Given** a bionic organ (e.g. a bionic lung) is installed in place of a missing natural organ, **When** the system evaluates regrowth eligibility for Tier 2 wearers, **Then** the same block rule applies — the bionic organ must be uninstalled before natural regrowth can start.
4. **Given** an installed prosthetic/bionic blocking regrowth is removed via a normal surgery bill, **When** removal completes, **Then** the full tier duration (7 or 4 days) begins counting down fresh from that moment.

---

### User Story 3 - Living with an active regrowth (Priority: P3)

A colonist with two limbs regrowing at once is visibly worse off for it — hungrier, in some pain, and in a foul mood from watching their own body regrow — giving the player a real reason to consider fitting a prosthetic to one part and letting only the other regrow, rather than taking on the full cost of both at once.

**Why this priority**: This is the tattoo's balancing cost mechanism — the thing that keeps "regrow everything for free" from being a strictly dominant, zero-downside choice. It matters, but the tattoo is still recognizably itself without it if not yet built.

**Independent Test**: Have a colonist with two parts regrowing simultaneously, and confirm they carry two stacks of the hunger/pain effect (versus one stack for a single regrowth) plus exactly one flat mood penalty, and that a malnourished colonist's regrowth timers stop advancing until fed.

**Acceptance Scenarios**:

1. **Given** a colonist has exactly one part actively regrowing, **When** the cost effects are evaluated, **Then** they carry one stack of the raised-hunger/mild-pain effect and one instance of the regrowth mood debuff.
2. **Given** a colonist has two or more parts actively regrowing at the same time, **When** the cost effects are evaluated, **Then** they carry one hunger/pain stack per concurrently-regrowing part, but still only one instance of the mood debuff (the mood penalty does not stack per part).
3. **Given** a colonist with active regrowth becomes malnourished, **When** malnutrition is present, **Then** all of that colonist's in-progress regrowth timers stop advancing until they have eaten enough to recover from malnutrition.
4. **Given** a colonist's last active regrowth completes or is otherwise no longer in progress, **When** that happens, **Then** the corresponding hunger/pain stack and the mood debuff (once no regrowths remain) are removed.

---

### User Story 4 - Reaching Tier 2 (Priority: P4)

A colonist who keeps losing limbs earns Shedscale's upgrade quickly through hard use, while a colonist who rarely needs it still gets there eventually just by wearing the tattoo long enough — and once reached, every part that colonist regrew at the weaker tier retroactively loses its efficiency penalty too.

**Why this priority**: An important long-term progression payoff consistent with every other tattoo's tier system, but it builds on top of User Stories 1-3 rather than gating them — the tattoo functions at Tier 1 without this ever resolving.

**Independent Test**: Advance a Shedscale-tattooed colonist to the earlier of the two Tier 2 thresholds (30 days worn, or 3 parts regrown) and confirm the upgrade applies permanently, all future regrowths come back at full efficiency, and any parts already regrown at Tier 1 lose their efficiency penalty retroactively.

**Acceptance Scenarios**:

1. **Given** a colonist has worn a Shedscale tattoo for 30 days without yet regrowing 3 parts, **When** the 30-day threshold is reached, **Then** the tattoo permanently upgrades to Tier 2.
2. **Given** a colonist has regrown 3 parts with the tattoo before reaching 30 days worn, **When** the 3rd part finishes regrowing, **Then** the tattoo permanently upgrades to Tier 2 immediately, without waiting for the day threshold.
3. **Given** a colonist reaches Tier 2 by either path, **When** the upgrade applies, **Then** it is permanent and does not downgrade for any reason.
4. **Given** a colonist already has one or more parts carrying the Tier 1 reduced-efficiency penalty at the moment they reach Tier 2, **When** the upgrade applies, **Then** the efficiency penalty is cleared from those already-regrown parts as well as from all future regrowths.

---

### Edge Cases

- What happens if a colonist regrows a part, then loses it again later? The part is once again "genuinely missing" and becomes eligible for another full regrowth cycle at the wearer's current tier, subject to the same prosthetic-block rule as any other missing part.
- What happens if a colonist's body is mid-revival via a Phoenix tattoo and also missing parts that Shedscale would regrow? Regrowth is only evaluated against parts missing on a living colonist; a dead-and-pending-revival colonist has no living body for Shedscale to act on until Phoenix's revival resolves.
- What happens if a colonist has zero parts actively regrowing but was carrying leftover Tier 1 efficiency penalties from past regrowths? Those penalties persist indefinitely until either that specific part is lost and regrown again, or the colonist reaches Tier 2 (which clears all of them retroactively).
- What happens if a prosthetic is installed on a part in the middle of that part's regrowth countdown (not just before it starts)? Installing a replacement stops the timer immediately at whatever point it is at, and per the "no banked progress" rule (User Story 2, Scenario 2), a later removal restarts the full duration from zero rather than resuming.
- What happens to the malnutrition-based stall if a colonist has some regrowths and becomes malnourished, then a new part becomes eligible for regrowth while still malnourished? The newly-eligible part does not begin counting down either, since malnutrition stalls regrowth generally rather than only the ones already in progress.
- What happens if a colonist's Shedscale tattoo is removed while a body part is mid-regrowth? The regrowth halts immediately and the part remains missing indefinitely; resuming it requires a newly-applied Shedscale tattoo, which restarts that part's full tier duration from zero — no partial progress carries over, consistent with the prosthetic-block rule's no-banked-progress behavior (User Story 2, Scenario 2).
- What happens to a colonist's Tier 2 progress (days worn, parts regrown) if their pre-Tier-2 Shedscale tattoo is removed and a new one is later applied? The clock resets to zero along with the tattoo — it is a property of the current tattoo instance, not a colonist-lifetime stat, so a fresh tattoo always starts its own progress from scratch.

## Requirements *(mandatory)*

### Functional Requirements

**Trigger and eligibility**

- **FR-001**: System MUST automatically start regrowing any eligible body part the instant it is both genuinely missing (not merely damaged, scarred, or otherwise present-but-injured) and has no installed prosthetic, peg leg, or bionic, requiring no player-triggered action.
- **FR-002**: System MUST allow every eligible missing part on a colonist to regrow simultaneously, with no one-at-a-time restriction.
- **FR-003**: System MUST NEVER consider the brain, spine, neck, torso, or pelvis eligible for regrowth under any circumstance.
- **FR-004**: System MUST restrict Tier 1 eligibility to external limbs and extremities — fingers, toes, hands, feet, arms, legs, ears, nose, jaw, and eyes.
- **FR-005**: System MUST extend Tier 2 eligibility to everything covered by Tier 1 plus internal organs (e.g. kidneys, lungs).
- **FR-006**: System MUST complete Tier 1 regrowth in 7 days per part and Tier 2 regrowth in 4 days per part (placeholder values).
- **FR-007**: On regrowth completion, system MUST clear all pre-existing permanent injuries that were recorded on that specific body part (old scars, prior surgical damage, etc.), returning a clean part.

**Prosthetic and bionic block rule**

- **FR-008**: System MUST prevent a missing part's regrowth timer from starting at all while that part has an installed prosthetic, peg leg, or bionic (including bionic organs).
- **FR-009**: System MUST surface a persistent alert (in RimWorld's standard alert list) indicating regrowth is available but currently blocked by an installed replacement, for any such part, remaining active every day the block continues rather than a one-time notification.
- **FR-010**: System MUST NOT bank any partial regrowth progress while a part is blocked — removing the blocking replacement always restarts the full tier duration from zero.
- **FR-011**: System MUST treat a normal surgery bill to uninstall the blocking prosthetic/bionic as the sole way to clear the block and allow the timer to begin.

**Cost of active regrowth**

- **FR-012**: System MUST apply a permanent reduced-efficiency condition (placeholder 75-85%) to any part regrown while the wearer is Tier 1, and MUST NOT apply this penalty to parts regrown while the wearer is Tier 2.
- **FR-013**: System MUST apply one stack of a raised-hunger/mild-pain effect per body part currently and actively regrowing on a given colonist, such that two simultaneous regrowths produce two stacks.
- **FR-014**: System MUST stall progress on all of a colonist's in-progress regrowth timers while that colonist is malnourished, resuming only once malnutrition is resolved.
- **FR-015**: System MUST apply exactly one instance of a regrowth mood debuff whenever a colonist has at least one part actively regrowing, regardless of how many parts are regrowing at once, and MUST remove it once no regrowths remain in progress.

**Tier 2 progression**

- **FR-016**: System MUST permanently upgrade a Shedscale tattoo to Tier 2 the moment either of two conditions is first met: the tattoo has been worn for a placeholder 30 consecutive days, or the wearer has completed regrowing 3 parts with it — whichever occurs first.
- **FR-017**: System MUST treat the Tier 2 upgrade as permanent and non-reversible once triggered.
- **FR-018**: Upon reaching Tier 2, system MUST retroactively clear the reduced-efficiency condition (FR-012) from every part the wearer had already regrown at Tier 1, in addition to preventing the penalty on all future regrowths.

**Rarity**

- **FR-019**: System MUST NOT impose any hard cap on the number of colonists in a faction who may simultaneously wear a Shedscale tattoo, unlike tattoos that do carry such a cap.

**Skill independence**

- **FR-020**: None of Shedscale's own automatic mechanics (regrowth trigger, timer, efficiency penalty, cost effects, Tier 2 progression) MUST be influenced by any colonist's skill level, including the applying artist's — this is distinct from the standard ritual-station application skill check (spec 001), which still governs getting the tattoo applied in the first place.

**Removal behavior**

- **FR-021**: System MUST halt an in-progress regrowth immediately and permanently if the Shedscale tattoo is removed from that colonist before that part's regrowth completes, leaving the part missing until a newly-applied Shedscale tattoo restarts that part's full tier duration from zero.
- **FR-021a**: System MUST reset a colonist's Tier 2 progress clock (days worn and parts-regrown counts) to zero if their Shedscale tattoo is removed before Tier 2 was reached and a new Shedscale tattoo is later applied — Tier 2 progress belongs to the current tattoo instance, not a colonist-lifetime stat.

### Key Entities

- **Shedscale Tattoo**: The tattoo instance itself — tracks its tier (1 or 2), and is never consumed by its own abilities.
- **Regrowth State**: Per-body-part tracking of an in-progress regrowth — elapsed/remaining time toward the tier's duration, and whether it is currently blocked by an installed prosthetic/bionic. Owned by the Shedscale tattoo itself; it does not survive that tattoo's removal, so a removed-then-reapplied tattoo always restarts affected parts from zero.
- **Reduced-Efficiency Condition**: A permanent, per-part hediff applied to Tier 1 regrowths, cleared either by that specific part being lost and regrown again post-Tier 2, or retroactively across all such parts the moment the wearer reaches Tier 2.
- **Regrowth Hunger/Pain Stack**: A per-colonist stacking effect with one stack per concurrently-active regrowth, tied 1:1 to how many parts are currently regrowing.
- **Regrowth Mood Debuff**: A flat, non-stacking per-colonist mood penalty present whenever at least one regrowth is active.
- **Tier 2 Progress Clock**: Tracking of consecutive days worn and cumulative parts regrown for the current Shedscale tattoo instance, used to determine whichever Tier 2 condition is met first. Owned by that tattoo instance — it resets to zero if the tattoo is removed and a new one applied before Tier 2 was reached.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A colonist wearing a Shedscale tattoo who is missing an eligible part with nothing installed on it reliably regrows that part without any player action, observed consistently across repeated in-game tests at both tiers.
- **SC-002**: A colonist with multiple eligible missing parts at once has all of them regrowing simultaneously in every observed test, never gated to one at a time.
- **SC-003**: Installing a prosthetic or bionic on a missing part reliably blocks its regrowth timer indefinitely in every observed test, and removing it reliably restarts the full duration from zero rather than resuming.
- **SC-004**: A part regrown at Tier 1 is observably weaker (reduced efficiency) than a part regrown at Tier 2, and every pre-existing permanent injury on a regrown part is gone immediately after regrowth completes.
- **SC-005**: A colonist with two or more simultaneous regrowths is observably worse off (more hunger/pain stacks) than one with a single regrowth, while carrying the same single mood penalty regardless of count.
- **SC-006**: A colonist reliably reaches Tier 2 upon meeting either the days-worn or parts-regrown threshold, whichever comes first, and every part previously regrown at Tier 1 loses its efficiency penalty at that same moment.
- **SC-007**: No point in play allows more than the colony's actual population to simultaneously wear Shedscale tattoos — i.e., no artificial faction-wide cap is ever enforced, unlike Phoenix's 3-tattoo cap.
- **SC-008**: Every numeric balance value introduced by this feature (regrowth days per tier, efficiency-penalty range, Tier 2 day/part thresholds, hunger rate) exists as a discoverable, tunable value rather than an unlabeled constant, consistent with this project's data-driven balance principle.

## Assumptions

- All numeric values in this spec (7/4-day regrowth durations, 75-85% efficiency range, 30-day/3-part Tier 2 thresholds, hunger/pain magnitude) are placeholders per the customer-approved proposal, to be resolved in a dedicated balance pass rather than fixed here — ship with clearly-labeled placeholder values per Constitution Principle III.
- The regrowth mood debuff is a single flat penalty that does not stack or intensify with additional concurrent regrowths, per the proposal's explicit assumption; the hunger/pain effect is the only cost that scales with concurrent-regrowth count.
- Bionic organs are gated by the exact same install/uninstall block rule as external prosthetics and peg legs — there is no separate rule for internal replacements.
- "Missing" reuses RimWorld's own existing concept of a genuinely absent body part (as used by prosthetic/bionic installation eligibility) rather than a new custom absence model, for consistency with how the base game already tracks lost limbs and organs.
- Shedscale pairs with the existing Phoenix tattoo (spec 014) conceptually — Phoenix's revival restores a dead colonist to life, and Shedscale then regrows whatever that colonist was missing at the time — but this spec does not require any direct code-level interaction between the two tattoos; each operates independently on a living colonist's current missing-parts state.
- No colony-wide or faction-wide rarity cap applies to Shedscale; any scarcity is intended to come from ritual-ingredient cost (out of scope for this spec, per the ritual-station application feature, spec 001) rather than a hard limit enforced by this feature.
