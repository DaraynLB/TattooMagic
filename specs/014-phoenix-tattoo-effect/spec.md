# Feature Specification: Phoenix Tattoo Effect

**Feature Branch**: `014-phoenix-tattoo-effect`

**Created**: 2026-08-23

**Status**: Draft

**Input**: User description: "Phoenix tattoo effect — a Phoenix-themed magic tattoo, following the same pattern as the other 13 tattoos in this mod. Full finalized design is written up in docs/proposals/2026-08-23-phoenix-tattoo.md: faction-wide 3-tattoo rarity cap; automatic resurrection on actual death after a tiered wait period, with a decomposition-based success chance that retries daily and never truly reaches zero; a burn injury and a Paralytic Abasia debuff as the cost of every attempt; Tier 2 reached by staying alive a set number of days, which resets on revival but is otherwise permanent and also resets the Abasia counter; a crematorium-cremation exception that guarantees the next revival at the cost of any un-stripped gear; and an independent passive ability that can auto-cauterize severe bleeding wounds before they become fatal."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A colonist cheats death (Priority: P1)

A colonist wearing a Phoenix tattoo dies in combat. The player doesn't need to do anything special — days later, without any ritual or item use, that colonist gets back up on their own, carrying a fresh burn from the ordeal.

**Why this priority**: This is the entire reason the tattoo exists — it's the feature the customer asked for by name. Nothing else in this spec matters if this core loop doesn't work.

**Independent Test**: Apply a Phoenix tattoo to a colonist, kill them with their body left intact and unburied, and observe that they automatically return to life after the tier's wait period, bearing a new burn injury.

**Acceptance Scenarios**:

1. **Given** a colonist wearing a Tier 1 Phoenix tattoo dies with their head and body intact, **When** the tier's wait period (5 days) elapses, **Then** the game attempts a revival roll based on the corpse's current decomposition and, on success, the colonist returns to life bearing a new severe burn injury.
2. **Given** a Tier 2 wearer dies under the same conditions, **When** 3 days elapse, **Then** the first revival attempt occurs (3 days sooner than Tier 1), and any resulting burn is the lighter Tier 2 severity.
3. **Given** a revival attempt fails, **When** one more day passes, **Then** the tattoo attempts revival again, and this repeats daily until it succeeds or the body is destroyed/fully decayed.
4. **Given** the corpse is being kept somewhere cold enough to halt decomposition, **When** repeated daily attempts occur at an unchanging chance, **Then** the colonist is still expected to eventually revive, since the odds never permanently favor failure.
5. **Given** a colonist has already been revived twice in their life, **When** a third revival attempt succeeds, **Then** they also gain the Paralytic Abasia debuff (2 days), which lasts one day longer on each subsequent occurrence.
6. **Given** a colonist has stayed alive for the required number of consecutive days while wearing the tattoo, **When** that threshold is reached, **Then** the tattoo permanently upgrades to Tier 2 (shorter wait, lighter burn) and the Paralytic Abasia counter resets to a fresh start.
7. **Given** a wearer's body is destroyed outright, or is left to fully decay away, **When** either occurs, **Then** revival becomes impossible for that colonist forever, unless the cremation exception (User Story 3) applies.

---

### User Story 2 - Keeping Phoenix rare across the colony (Priority: P2)

The player can't just tattoo every colonist with Phoenix — only 3 can be active across their whole faction (all bases combined) at any time, so choosing who gets it is a meaningful, scarce decision.

**Why this priority**: Without this cap, Phoenix trivializes permadeath for the whole colony rather than being a rare, deliberate choice for a few colonists — this is a core balance requirement, not a nice-to-have.

**Independent Test**: With 3 colonists already wearing Phoenix tattoos, attempt to apply a 4th at the ritual station and confirm it is unavailable; then free a slot (by permanent loss, successful removal, or a wearer leaving the colony) and confirm a 4th application becomes possible again.

**Acceptance Scenarios**:

1. **Given** exactly 3 Phoenix tattoos are currently active across the player's faction, **When** a player attempts to apply a 4th at the ritual station, **Then** Phoenix is not offered as an option.
2. **Given** one of the 3 wearers is currently dead and awaiting a pending revival attempt, **When** the cap is checked, **Then** that pending tattoo still counts as one of the 3 active slots.
3. **Given** a wearer's tattoo is gone for good (permanent loss of the wearer, or a successful tattoo-removal ritual), **When** that happens, **Then** their slot frees up and a new Phoenix application becomes available again.
4. **Given** a wearer's tattoo-removal attempt fails (per the separate removal-ritual feature), **When** that failure occurs, **Then** the tattoo remains permanently stuck on the colonist and continues to count against the cap.
5. **Given** a colonist wearing a Phoenix tattoo leaves the colony for any reason (captured, defects, sent away), **When** they leave, **Then** the tattoo is automatically stripped from them at that moment, freeing their slot.
6. **Given** the faction is already at the 3-tattoo cap, **When** a new colonist joins already wearing a Phoenix tattoo (rescue, quest reward, wanderer), **Then** they are allowed to keep it as a temporary 4th, but no further Phoenix tattoos can be applied anywhere in the faction until the count naturally drops back to 3.

---

### User Story 3 - Guaranteeing a comeback through cremation (Priority: P3)

Instead of leaving a body's fate to chance, a player can deliberately cremate a Phoenix-tattooed corpse to guarantee its owner comes back once the wait is up — echoing the phoenix rising from its own ashes.

**Why this priority**: A meaningful, thematic enhancement to the core revival loop (User Story 1) that gives players deliberate control over an otherwise-random outcome, at a real cost (lost gear). It depends on User Story 1 existing but is independently observable and testable on its own.

**Independent Test**: Let a Phoenix-tattooed corpse decompose to a point where its natural revival chance would be very low, cremate it via the crematorium building's bill, and confirm the next scheduled attempt succeeds regardless of decomposition.

**Acceptance Scenarios**:

1. **Given** a Phoenix-tattooed corpse has decomposed heavily, **When** it is cremated via the crematorium building's bill, **Then** the next scheduled revival attempt succeeds automatically, bypassing the normal decomposition-based chance roll.
2. **Given** a corpse is destroyed by fire some other way (a burning building, a raid, a wildfire), **When** that happens, **Then** it does NOT count as cremation and does not guarantee revival — it is treated as ordinary body destruction (permanent loss, per User Story 1).
3. **Given** a corpse is cremated partway through its tier's wait period, **When** cremation occurs, **Then** the wait timer is not shortened — the guaranteed success still only applies once the tier's usual wait has elapsed.
4. **Given** gear was left on the corpse at the time of cremation, **When** the guaranteed revival occurs, **Then** that gear is permanently lost and only the colonist (with their stats intact) returns.
5. **Given** a cremation-triggered revival succeeds, **When** it resolves, **Then** it counts as one attempt toward the Paralytic Abasia counter exactly like any other revival attempt.

---

### User Story 4 - A tattoo that quietly staunches bleeding (Priority: P4)

Independent of the death-and-revival mechanic entirely, a Phoenix-tattooed colonist has a passive chance to auto-cauterize a severe bleeding wound before it becomes fatal — a quiet safety net that can prevent the need for the main ability to ever trigger.

**Why this priority**: A valuable passive layer that adds survivability value even for a colonist who never actually dies, but it is fully self-contained and does not block or depend on User Stories 1-3.

**Independent Test**: Inflict a severe bleeding wound on a Phoenix-tattooed colonist and observe that, over repeated trials, the wound is sometimes automatically cauterized (stopping the bleed and leaving a burn) without any change to the colonist's revival wait timer or cap status.

**Acceptance Scenarios**:

1. **Given** a Phoenix-tattooed colonist takes a severe bleeding wound, **When** the wound deals blood-loss damage, **Then** there is a passive chance it is automatically cauterized, stopping that bleed and leaving a burn injury.
2. **Given** the wound is trivial (a scratch), **When** it deals damage, **Then** it is never eligible for auto-cauterization.
3. **Given** the colonist has bleeding wounds on more than one body part at once, **When** the passive ability procs, **Then** it targets whichever single wound is most severe, cauterizing every qualifying bleed on that one body part while leaving other body parts' bleeds untouched.
4. **Given** the colonist has a severed or missing limb that is bleeding, **When** any other bleeding wound exists elsewhere on the body at the same time, **Then** the severed-limb bleed always takes priority for targeting.
5. **Given** the colonist is wearing a Tier 2 Phoenix tattoo rather than Tier 1, **When** the passive chance is evaluated, **Then** the proc chance is higher, but the resulting burn's severity is unchanged from Tier 1.
6. **Given** the passive ability procs or the main revival ability fires, **When** either occurs, **Then** neither affects the other's cooldown, timer, or state in any way.

---

### Edge Cases

- What happens if a Phoenix-tattooed corpse mid-wait is buried, or destroyed by a method other than the crematorium bill (e.g. fed to a chemical reprocessor, eaten by animals)? Current working assumption: this ends the possibility of revival the same way outright destruction does — see Assumptions.
- What happens if a colonist dies while already carrying a partially-progressed Paralytic Abasia counter and is then successfully cremated-revived — does the cremation-triggered attempt still bank/trigger the counter the same as a natural attempt? Yes — cremation only replaces the chance roll, not any of the surrounding cost mechanics (User Story 3, Scenario 5).
- What happens to Tier 2 "days alive" progress if a colonist is captured or leaves the colony before dying? Since leaving strips the tattoo entirely, any in-progress Tier 2 clock is moot — the tattoo and its progress are gone.
- What happens if two separate bleeding wounds on different body parts are exactly tied in severity? The system needs a deterministic tie-break (e.g. most recently inflicted, or a stable ordering by body part) — left as an implementation detail, not a player-facing behavior difference.
- What happens if a raid or map event destroys a corpse that was mid-wait for a natural (non-cremation) revival? Treated as outright body destruction — permanent loss, per User Story 1 Scenario 7.

## Requirements *(mandatory)*

### Functional Requirements

**Rarity cap**

- **FR-001**: System MUST prevent more than 3 Phoenix tattoos from being simultaneously active across a single player faction (all bases/settlements combined).
- **FR-002**: System MUST count a Phoenix tattoo against the cap both while its wearer is alive and equipped, and while its wearer is dead and has a pending revival attempt outstanding.
- **FR-003**: System MUST free a cap slot only when a Phoenix tattoo is permanently gone — the wearer is permanently lost (destroyed/decayed body with no successful cremation save), or the tattoo is successfully removed via the separate tattoo-removal feature. A failed removal attempt MUST NOT free the slot.
- **FR-004**: System MUST automatically strip a Phoenix tattoo from any colonist who leaves the player's faction for any reason (captured, defection, sent away), freeing their cap slot at that moment.
- **FR-005**: System MUST allow a new colonist who joins already wearing a Phoenix tattoo to keep it even if doing so exceeds the cap, while preventing any further Phoenix tattoos from being applied anywhere in the faction until the count naturally returns to 3 or fewer.
- **FR-006**: The ritual station MUST NOT offer Phoenix as an applicable tattoo option whenever the faction is at or above the cap.

**Trigger, wait, and revival roll**

- **FR-007**: System MUST trigger Phoenix's revival sequence only on the wearer's actual death, not on being downed or bleeding out.
- **FR-008**: System MUST require the wearer's head/brain to be intact and the body to not be destroyed as a precondition for any revival attempt to ever succeed.
- **FR-009**: The Phoenix tattoo MUST remain equipped on its wearer through death and revival and MUST NOT be consumed by any revival attempt, success or failure.
- **FR-010**: System MUST wait 5 days at Tier 1 (3 days at Tier 2) after death before making the first revival attempt, with no player action required to trigger the attempt.
- **FR-011**: System MUST calculate each revival attempt's success chance based on the corpse's current decomposition state at the time of the attempt, with fresher corpses having better odds.
- **FR-012**: On a failed revival attempt, system MUST retry automatically once per day until success, permanent body destruction, or full decay occurs.
- **FR-013**: System MUST ensure the revival success chance approaches but never mathematically reaches zero purely from decomposition, and MUST keep the chance unchanged between attempts for a corpse whose decomposition is halted (e.g. kept frozen).
- **FR-014**: System MUST permanently disable further revival attempts for a wearer once their body is destroyed outright or has fully decayed away, except where the cremation exception (FR-020 through FR-024) applies.

**Cost of each attempt**

- **FR-015**: System MUST apply a burn injury to the corpse/wearer on every revival attempt, success or failure alike — severe severity at Tier 1, lighter severity at Tier 2 — and these burns MUST stack across repeated attempts and across a wearer's lifetime revivals.
- **FR-016**: System MUST advance a per-colonist Paralytic Abasia counter by one on every revival attempt, success or failure.
- **FR-017**: System MUST apply the Paralytic Abasia debuff starting from a colonist's 3rd lifetime revival attempt onward (the 1st and 2nd never trigger it), applying the accumulated cost at the moment a revival actually succeeds (a failed attempt cannot visibly debuff a corpse, but still advances the counter).
- **FR-018**: System MUST set Paralytic Abasia's duration to 2 days on its first occurrence for a given colonist, increasing by 1 additional day on each subsequent occurrence.

**Tier 2 progression**

- **FR-019**: System MUST track consecutive days a wearer has stayed alive while equipped with the Phoenix tattoo, resetting this count to zero on every revival, and MUST upgrade the tattoo to Tier 2 permanently once a configured day threshold is reached — this upgrade MUST NOT be reversed by any later death.
- **FR-019a**: System MUST reset the Paralytic Abasia counter (both the count toward the next debuff occurrence and the duration-escalation schedule) to a fresh start at the moment a wearer reaches Tier 2.

**Cremation exception**

- **FR-020**: System MUST guarantee the next scheduled revival attempt succeeds, bypassing the decomposition-based chance entirely, when a Phoenix-tattooed corpse is cremated via the crematorium building's bill specifically.
- **FR-021**: System MUST NOT treat corpse destruction by any other means (structure fires, raids, wildfires, other destruction methods) as cremation for this purpose — those remain ordinary permanent body destruction.
- **FR-022**: System MUST still require the tier's full wait period to elapse after cremation before the guaranteed revival occurs — cremation guarantees the outcome, not an earlier timing.
- **FR-023**: System MUST permanently destroy any gear/apparel left on a corpse at the time of cremation, returning only the revived colonist and their stats.
- **FR-024**: A cremation-guaranteed revival MUST still count as one attempt toward the Paralytic Abasia counter (FR-016 through FR-018) and apply the tier-appropriate burn (FR-015), exactly like a natural revival attempt.

**Passive bleed cauterization**

- **FR-025**: System MUST give a Phoenix-tattooed colonist a passive chance to automatically cauterize a bleeding wound, evaluated independently of and without any interaction with the death/revival mechanic (FR-007 through FR-024) — no shared cooldown or shared state between the two.
- **FR-026**: System MUST exclude trivial (scratch-level) bleeding wounds from ever being eligible for auto-cauterization; only bleeding severe enough to meaningfully threaten the colonist is eligible.
- **FR-027**: When a colonist has bleeding wounds on more than one body part simultaneously, system MUST target the single most severe individual wound's body part for the cauterization attempt, except that a bleeding severed/missing limb MUST always take absolute priority over any other wound regardless of relative severity.
- **FR-028**: On a successful cauterization proc, system MUST stop every qualifying bleeding wound on the targeted body part simultaneously, leaving bleeds on other body parts unaffected.
- **FR-029**: System MUST give Tier 2 wearers a higher cauterization proc chance than Tier 1 wearers, while keeping the resulting burn's severity identical regardless of tier.
- **FR-030**: A successful cauterization MUST leave a burn injury using the same burn-injury system as revival burns (FR-015), stacking into the same shared scar history.

**Skill independence**

- **FR-031**: None of Phoenix's own passive or triggered mechanics (revival roll, cauterization proc, Paralytic Abasia) MUST be influenced by any colonist's skill level, including the applying artist's — this is distinct from the standard ritual-station application skill check (spec 001), which still governs getting the tattoo applied in the first place.

### Key Entities

- **Phoenix Tattoo**: The tattoo instance itself — tracks its tier (1 or 2), and is never consumed by its own abilities.
- **Revival State**: Per-corpse tracking of a pending Phoenix revival — current wait/retry timer, cumulative decomposition-based chance, and whether a cremation guarantee has been applied.
- **Paralytic Abasia Counter**: Per-colonist lifetime count of revival attempts, used to determine whether the debuff applies and how long it lasts on its next occurrence.
- **Paralytic Abasia (debuff)**: A new temporary hediff causing near-total paralysis, with escalating duration per occurrence.
- **Tier 2 Progress Clock**: Per-colonist count of consecutive days alive while wearing the tattoo, reset on every revival, driving the permanent Tier 1 → Tier 2 upgrade.
- **Faction Phoenix Registry**: Faction-wide count of active Phoenix tattoos (including pending-revival ones), used to enforce the 3-tattoo cap and gate ritual-station availability.
- **Burn Injury**: The shared injury type applied by both revival attempts and successful cauterizations, stacking over a colonist's lifetime.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A colonist wearing a Phoenix tattoo who dies with an intact, unburied body reliably returns to life without any player action, observed consistently across repeated in-game tests at both tiers.
- **SC-002**: At no point during play can more than 3 colonists in a single faction simultaneously hold an active Phoenix tattoo, verified across application attempts, pending-revival deaths, and colonists leaving/rejoining the colony.
- **SC-003**: A corpse cremated via the crematorium revives at its next scheduled attempt in effectively 100% of test cases, regardless of how decomposed it was beforehand.
- **SC-004**: A colonist's 3rd-and-later lifetime revivals are observably harder on them than their first two — the Paralytic Abasia debuff is visibly present and its duration measurably increases each additional occurrence, until a Tier 2 upgrade resets it.
- **SC-005**: The passive cauterization ability is observed to save colonists from bleed-out death in playtesting without ever altering the timing or outcome of a separate, ongoing revival sequence for the same or a different colonist.
- **SC-006**: Every numeric balance value introduced by this feature (decomposition-to-chance curve, Tier 2 day threshold, cauterization proc chances) exists as a discoverable, tunable value rather than an unlabeled constant, consistent with this project's data-driven balance principle.

## Assumptions

- A body mid-wait that is buried, or destroyed by any method other than the crematorium's own bill (chemical reprocessing, animal scavenging, etc.), is treated the same as outright body destruction — permanent loss, no revival. This has been communicated to the customer as our working assumption but is not yet explicitly confirmed; flag for confirmation before final tuning.
- The exact revival-chance-vs-decomposition curve is a balance-pass value, not fixed by this spec — ship with a clearly-labeled placeholder curve per Constitution Principle III, to be tuned later.
- The exact number of consecutive "days alive" required to reach Tier 2 is likewise a balance-pass placeholder, not fixed here.
- "Destroyed" body state, decomposition stages, and corpse-eligibility gating reuse RimWorld's own existing corpse/resurrection-style concepts where possible, for player familiarity, rather than inventing a fully custom decay model.
- Paralytic Abasia is a new custom hediff built for this feature; it is not assumed to reuse any existing vanilla or DLC hediff.
- The separate tattoo-removal ritual referenced by the rarity-cap rules (FR-003) is out of scope for this spec — only its effect on cap accounting is assumed here, not its own implementation.
- Auto-cauterization's "check every bleed-damage tick" approach is the intended first implementation; falling back to a per-wound-application check is an acceptable implementation-time adjustment if profiling shows a real performance cost, without changing this spec's player-facing behavior requirements.
