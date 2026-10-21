# Feature Specification: Soul Tether Tattoo Effect

**Feature Branch**: `017-soul-tether-tattoo-effect`

**Created**: 2026-09-19

**Status**: Draft

**Input**: User description: "Soul Tether tattoo effect (017-soul-tether-tattoo-effect). Source of truth: docs/proposals/2026-09-12-soul-tether-tattoo.md (customer-approved, 2026-09-19). Summary: a two-tier triggered tattoo. Cast it and it yanks one chosen friendly colonist — or their corpse — straight to the caster, regardless of status (downed, bleeding out, immobile, or dead). Single-target only at both tiers; Tier 2 does not become an AoE pull. On arrival, the target gets a flat bonus to both tend speed and tend quality for a placeholder XML-tunable duration. Cooldown before recast, XML-tunable. Tier 2 increases the maximum pull range and grants a bigger tend buff than Tier 1. Customer wants the pull-and-apply-effect logic built as a reusable internal component (target filter + pull + effect application) so a future 'inverse' tattoo (pulling an enemy toward the caster with a debuff instead) can reuse the same plumbing. This is the second of a batch of 4 newly approved tattoos (Bastion Ward, Soul Tether, Stillshroud, Thunderclap Sigil) to be built, per docs/PRD.md §10 and the project's build order. Follow the same conventions as the existing 16 tattoo specs in specs/ (e.g. specs/004-guardians-call-tattoo-effect for the triggered gizmo/cooldown/progression-counter pattern, specs/016-bastion-ward-tattoo-effect for structure) and the mod's established two-tier tattoo model described in docs/PRD.md §5.4."

## Clarifications

### Session 2026-09-19

- Q: The original proposal/spec left the pull as an instant relocation with no visual/audio feedback (this mod has never used RimWorld's Mote/Fleck VFX system at all). The customer asked what a "hand reaching out and grabbing the pawn, pulling them back" would take, and specifically wants it to feel fast — "snatching someone from the jaws of defeat," not a slow float — while still reading as a deliberate pull rather than an instant blink. → A: Reuse vanilla's own proven Skip/Teleport psycast VFX+SFX (`RimWorld.CompAbilityEffect_Teleport`, decompiled from the installed game assembly) rather than building custom motes/art from scratch: a brief energy-tether line (`FleckDefOf.LineEMP`) between caster and target plus a "vanish" flash/sound (`FleckDefOf.PsycastSkipFlashEntry`, `SoundDefOf.Psycast_Skip_Entry`) play immediately at cast confirmation; the actual relocation and tend-buff grant are deferred by a short, XML-tunable window (`pullEffectDurationTicks`, customer-approved default 15-30 ticks — 0.25-0.5 seconds at normal game speed, not 15-30 seconds) before an "arrival" flash/sound (`FleckDefOf.PsycastSkipInnerExit`/`PsycastSkipOuterRingExit`, `SoundDefOf.Psycast_Skip_Exit`) plays at the destination and the target actually appears. No new art or custom rendering was needed — every asset used is already shipped with the base game. A true "pawn's own sprite visibly slides across the ground" animation (requiring a Harmony patch into pawn-rendering internals, a first for this mod) was considered and explicitly deferred as a much larger, higher-risk scope jump with no precedent to build on; this flash-and-snap approach was judged to already deliver the "fast, deliberate yank" feel the customer asked for.
- Q: The reused vanilla Skip/Teleport assets have their own native look (electric blue "psychic skip" flashes and an EMP-style line). The customer wants Soul Tether's effect to feel angelic/holy instead (referencing World of Warcraft's "Life Grip" as a tonal reference point), not arcane/psychic. → A: Tint every reused Fleck a warm gold-white (`HolyTetherColor`, placeholder RGB `(1, 0.92, 0.65)`) via `FleckCreationData.instanceColor` (confirmed, decompiled: `Verse.FleckStatic` reads `creationData.instanceColor ?? Color.white` and applies it at draw time) rather than leaving the assets in their native blue. This required switching from the `FleckMaker.Static`/`ConnectingLine` convenience wrappers (which don't expose a color parameter) to building `FleckCreationData` directly. Placeholder aesthetic choice, not a locked design — mirrors Bastion Ward's own ground-overlay `FillColor` precedent (a private code constant, not XML-exposed, since this is cosmetic rather than a gameplay balance number) — the exact shade can be adjusted after live in-game viewing.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pulling a downed ally to safety (Priority: P1)

A player applies the Soul Tether tattoo to a colonist (via the existing ritual station). In combat, that colonist has an activatable ability (a gizmo, like Guardian's Call's). Activating it lets the player choose a single friendly colonist anywhere within range — including one who is downed, bleeding out, or otherwise immobile — as the target. The moment it resolves, that colonist is instantly moved to a cell next to the caster, and immediately gains a flat, temporary bonus to both tend speed and tend quality, making it easier to patch them up once they're back at the caster's side.

**Why this priority**: This is the entire point of the tattoo — a "get someone out of danger and treat them" rescue tool, which is the specific effect the customer asked for. Every other story in this spec depends on this core targeting-and-pull behavior existing first.

**Independent Test**: Can be fully tested by applying Soul Tether to a colonist, provoking a separate colonist into being downed at range, activating the ability targeting the downed colonist, and confirming they instantly appear next to the caster with the tend speed/quality buff active.

**Acceptance Scenarios**:

1. **Given** a pawn with Soul Tether applied, **When** the player activates the ability and selects a single friendly colonist within range as the target, **Then** that colonist is instantly relocated to a walkable cell adjacent to the caster.
2. **Given** the chosen target is downed, bleeding, or otherwise incapacitated, **When** the pull resolves, **Then** the target is still successfully moved next to the caster — incapacitation does not block the pull.
3. **Given** the pull has just resolved, **When** the target arrives at the caster's side, **Then** the target immediately gains a flat, temporary bonus to both tend speed and tend quality (placeholder duration and magnitude, XML-tunable), not a multiplier on the treating colonist's Medicine skill.
4. **Given** the chosen target is farther from the caster than the ability's current maximum pull range, **When** the player attempts to target them, **Then** the game prevents selecting that target, consistent with how other range-limited tattoo abilities already behave (e.g. Bastion Ward's max cast range).
5. **Given** no walkable cell exists adjacent to the caster, **When** the pull resolves, **Then** the system places the target at the nearest walkable cell it can find near the caster rather than failing the cast or crashing.

---

### User Story 2 - Recovering a dead colonist's body (Priority: P2)

A colonist has already died and become a corpse out in the field. The player activates Soul Tether and selects that corpse as the target instead of a living pawn. The corpse is instantly pulled to the caster's location the same way a living target would be, letting the player recover a body without a manual hauling trip through hostile territory.

**Why this priority**: This is an explicitly called-out capability distinct from User Story 1 — pulling a corpse has no tend-buff effect and exercises a different target type, but is core to the tattoo's identity as described in the approved proposal ("if a colonist has already died and become a corpse, Soul Tether can still pull the body").

**Independent Test**: Can be tested independently by letting a colonist die and become a corpse, then activating Soul Tether targeting that corpse, and confirming the corpse is relocated next to the caster.

**Acceptance Scenarios**:

1. **Given** a deceased former colonist's corpse is present on the map within range, **When** the player activates Soul Tether and selects that corpse as the target, **Then** the corpse is instantly relocated to a walkable cell adjacent to the caster.
2. **Given** a corpse has just been pulled, **When** the pull resolves, **Then** no tend speed/quality buff is applied (a corpse cannot be tended), and no error or crash occurs.
3. **Given** a hostile faction's corpse (not a former colonist) is present within range, **When** the player attempts to target it, **Then** the game prevents selecting that target — only friendly-faction pawns and their corpses are valid targets.

---

### User Story 3 - Soul Tether grows stronger the more it's used (Priority: P3)

As a Soul Tether-tattooed colonist keeps activating the ability, the tattoo tracks how many times it's been used and automatically upgrades itself to a stronger Tier 2 version once enough uses have accumulated — with no extra ritual, ingredients, or tattoo slot required. At Tier 2, the maximum pull range is longer and the tend buff applied on arrival is stronger, but the ability remains strictly single-target — Tier 2 never becomes an area pull.

**Why this priority**: This is the PRD §5.4 tiering mechanic every triggered tattoo in the roster follows. It only matters once Tier 1's core pull behavior (User Story 1) already works, so it's the natural third slice.

**Independent Test**: Can be tested independently by activating a Soul Tether-tattooed pawn's ability enough times to reach the threshold, then confirming the maximum pull range is measurably larger on the next cast and the tend buff applied is measurably stronger than at Tier 1.

**Acceptance Scenarios**:

1. **Given** Tier 1 Soul Tether applied with its progression counter below threshold, **When** the player activates the ability, **Then** the counter increments by one.
2. **Given** the progression counter reaches its Tier 2 threshold, **When** the triggering activation resolves, **Then** Soul Tether upgrades to Tier 2 in place, with no new ritual, no ingredient cost, and no change to the pawn's slot usage.
3. **Given** Soul Tether is at Tier 2, **When** the wearer activates the ability, **Then** the maximum pull range is longer than at Tier 1, the resulting tend buff is stronger than at Tier 1, and the ability still only ever pulls the single chosen target — never an area around it.
4. **Given** Tier 2's tend buff is applied to a pulled target, **When** it expires, **Then** the bonus to tend speed and quality stops applying, exactly as Tier 1's buff stops at expiry.

---

### Edge Cases

- What happens if the caster targets themselves? The ability's targeting excludes the caster — Soul Tether pulls another pawn or corpse to the caster, not the caster to a location.
- What happens if the target is already standing adjacent to the caster when the ability is activated? The pull still resolves (the target may not visibly move far, or at all, if already adjacent) and the tend buff is still applied — there is no minimum-distance requirement.
- What happens if the target pawn is on a caravan, in a different map, or otherwise not present on the caster's current map? It is not a valid target — Soul Tether only pulls pawns/corpses that are actually spawned on the caster's current map.
- What happens if the chosen target becomes hostile (e.g. a mental break turning them into a manhunter, or a prisoner who was never friendly) between when targeting starts and when the ability resolves? Targeting is restricted to friendly-faction pawns/corpses at the moment of activation; the existing targeting validity re-check (standard ability-targeting behavior) prevents completing a cast against a target that is no longer valid.
- What happens to an in-progress job, mental state, or downed/bleeding status the target had before being pulled? None of it is altered by the pull itself — Soul Tether only relocates the pawn and applies the tend buff; any existing conditions continue exactly as they were, just at the new location.
- What happens if the caster re-targets and casts again while the previous cast's cooldown hasn't elapsed? The ability's own cooldown (User Story 1) prevents this until the cooldown from the prior activation has elapsed.
- What happens to the tend buff's remaining duration and the progression counter/tier across a save and reload while the buff is still active? Both MUST persist unchanged, exactly as required of every other tattoo's per-instance state (Constitution Principle V).
- What happens if the Soul Tether hediff is removed from the caster after a pull has already resolved and the target's tend buff is still active? The already-applied tend buff continues running for its remaining duration, unaffected — it is a hediff on the target, independent of the caster's own tattoo once applied.

## Requirements *(mandatory)*

### Functional Requirements

**Casting and targeting**

- **FR-001**: While a pawn has Soul Tether applied (at either tier), the system MUST grant that pawn an activatable, pawn-targeted ability (gizmo) that, when used, lets the player choose a single friendly-faction pawn or a friendly-faction pawn's corpse as the target.
- **FR-002**: The targetable pawn/corpse MUST be limited to within a maximum range of the caster (placeholder value per tier, XML-tunable) — the player cannot pull a target an unlimited distance away.
- **FR-003**: The ability MUST accept a target regardless of the target's health/mobility status — downed, bleeding out, immobile, or otherwise incapacitated targets are all valid, provided they are still a living pawn or already a corpse.
- **FR-004**: The ability MUST NOT allow the caster to target themselves.
- **FR-005**: The ability MUST NOT allow targeting a pawn or corpse belonging to a non-friendly faction.
- **FR-006**: The ability MUST only allow targeting a pawn or corpse that is currently spawned on the caster's own map.

**Pull resolution**

- **FR-007**: When the ability resolves, the system MUST relocate the target (living pawn or corpse) to a walkable cell adjacent to the caster. If no walkable cell is available immediately adjacent, the system MUST place the target at the nearest walkable cell it can find near the caster rather than failing the cast or throwing an error.
- **FR-007a**: The system MUST play a brief visual/audio effect at the moment of activation (at the target's location) and a second effect at the moment the target actually arrives (at the destination), and MUST delay the actual relocation and tend-buff grant by a short, XML-tunable window between the two (session 2026-09-19 Clarifications) — the pull MUST NOT be a silent, effect-free, instant teleport.
- **FR-008**: The pull MUST be single-target only, at both tiers — the system MUST NOT pull, move, or otherwise affect any pawn or corpse other than the one chosen target, regardless of tier.
- **FR-009**: The pull MUST NOT alter any other existing state on the target (jobs, mental state, health conditions, downed/bleeding status) beyond its position.

**Tend buff**

- **FR-010**: When the pulled target is a living pawn, the system MUST immediately apply a temporary effect granting a flat bonus to both tend speed and tend quality, lasting a placeholder duration (XML-tunable). This bonus MUST be a flat addition, not a multiplier tied to the treating colonist's Medicine skill.
- **FR-011**: When the pulled target is a corpse, the system MUST NOT attempt to apply the tend buff, and MUST NOT error or crash as a result.
- **FR-012**: When the tend buff's duration elapses, the system MUST remove it, restoring the target's tend speed and quality to normal.

**Cooldown and progression**

- **FR-013**: After the ability is activated, it MUST become unavailable for a fixed cooldown period (placeholder value, XML-tunable) beginning at the moment of activation, before it can be activated again, with the remaining cooldown visible to the player via the gizmo.
- **FR-014**: The system MUST track a per-pawn, per-tattoo progression counter for Soul Tether that increments each time the wearer activates the ability, per the "times activated" pattern already used by every other triggered tattoo (PRD §5.4; feature 004 precedent).
- **FR-015**: When Soul Tether's progression counter reaches its Tier 2 threshold, the system MUST automatically upgrade the tattoo to Tier 2 in place, without requiring a new ritual, additional ingredients, or any change to the pawn's tattoo-slot usage.

**Tier 2**

- **FR-016**: At Tier 2, the maximum pull range (FR-002) MUST be longer than at Tier 1 (placeholder values, XML-tunable).
- **FR-017**: At Tier 2, the tend buff applied on arrival (FR-010) MUST be stronger than at Tier 1 (placeholder values, XML-tunable).
- **FR-018**: Tier 2 MUST NOT change the ability's single-target nature (FR-008) or its cooldown relative to Tier 1 — only maximum range and tend-buff magnitude differ between tiers.

**Cross-cutting / reuse**

- **FR-019**: The target-selection, pull, and effect-application logic MUST be implemented as an internally reusable component, parameterized by a target filter (e.g. "friendly pawn/corpse") and an arrival effect (e.g. "apply tend buff hediff"), so that a future tattoo pulling a different kind of target (e.g. a hostile pawn) and applying a different arrival effect (e.g. a debuff) can reuse the same pull mechanism without rewriting it. This is an internal implementation detail and MUST NOT change any player-facing behavior described above.
- **FR-020**: Every tunable numeric value driving Soul Tether's effect (max pull range per tier, cooldown length, tend buff duration and magnitude per tier, and the Tier 2 activation-count threshold) MUST be read through the existing `TattooEffectValues` accessor (feature 002) rather than read directly from XML/Def fields at each use site.
- **FR-021**: The system MUST reuse the existing `TattooTierProgress` tracker for tier/counter state, per the same convention used by every other tiered tattoo.
- **FR-022**: The system MUST reuse the existing triggered-ability (gizmo/cooldown/activation) pattern established by feature 004 (Guardian's Call) for Soul Tether's own cast/cooldown behavior, rather than inventing a new one.
- **FR-023**: The progression counter's current value, the tattoo's current tier, and any currently-active target tend buff's remaining duration MUST persist correctly across a game save and reload.
- **FR-024**: If the Soul Tether hediff is removed from a pawn by any means, the system MUST stop granting the gizmo and MUST prevent further casts, but MUST NOT retroactively remove a tend buff that pull had already applied to some other pawn.
- **FR-025**: The Soul Tether hediff MUST be protected by the existing cross-tattoo removal guard (feature 003, FR-015) with no additional per-tattoo configuration required.

### Key Entities

- **Soul Tether Tattoo Effect**: The runtime gameplay behavior granted while a pawn has the Soul Tether hediff — the activatable, pawn-targeted pull ability and its cooldown at Tier 1, plus a longer max range and a stronger tend buff at Tier 2.
- **Pull-and-Apply-Effect Component**: The internally reusable piece of logic that selects a valid target under a given filter, relocates it to the caster, and applies a given arrival effect — built generically enough that a future tattoo can supply a different target filter and arrival effect (e.g. a hostile-pull-and-debuff variant) without rewriting the pull mechanism itself.
- **Tend Buff**: The temporary hediff/effect applied to a successfully pulled living target, granting a flat bonus to tend speed and tend quality for a fixed duration; not applied to corpse targets.
- **Tier Progression Counter**: A per-pawn, per-tattoo count of ability activations that drives the automatic Tier 1 → Tier 2 upgrade once a threshold is reached — reuses feature 002's tracker.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Activating Soul Tether against a valid friendly target within range instantly relocates that target to a cell adjacent to the caster, verified across repeated trials including downed/bleeding/immobile targets.
- **SC-002**: A pulled living target measurably gains the flat tend speed/quality bonus immediately on arrival, and it measurably ends when its duration elapses.
- **SC-003**: A pulled corpse target is relocated to the caster with no tend buff applied and no errors in the RimWorld dev console.
- **SC-004**: A target beyond the ability's current maximum range cannot be selected, verified at both Tier 1 and Tier 2 range values.
- **SC-005**: The ability cannot be reactivated until its cooldown period has elapsed, verified across repeated activation attempts.
- **SC-006**: A Soul Tether-tattooed pawn automatically upgrades from Tier 1 to Tier 2 once the designated number of activations occurs, with zero additional player action required.
- **SC-007**: At Tier 2, the maximum pull range is measurably longer and the applied tend buff is measurably stronger than at Tier 1, while every cast at either tier still affects exactly one target.
- **SC-008**: A Soul Tether-tattooed pawn's progression counter, current tier, and any pulled target's active tend buff remaining duration are unchanged immediately after a save/reload compared to immediately before it.
- **SC-009**: The mod loads cleanly and behaves correctly with Harmony only, and separately with Harmony + Combat Extended, with zero errors in the RimWorld dev console in either configuration.

## Assumptions

- Maximum pull range per tier, cooldown length, tend buff duration, tend buff magnitude per tier, and the pull effect's delay window (FR-007a) are placeholder/balance-pass numbers, to be resolved in a dedicated balance pass rather than fixed here — ship with clearly-labeled placeholder values per Constitution Principle III.
- The pull effect's delay window (FR-007a) is deliberately short (customer-approved 15-30 ticks, i.e. a quarter to half a second at normal game speed) — Soul Tether is a battlefield rescue tool and must read as a fast, deliberate yank, not a slow float or a leisurely animation.
- The tend buff is a flat bonus to tend speed and tend quality, not a multiplier on the treating colonist's Medicine skill, per the approved proposal.
- "Corpse" targeting means an existing `Corpse` thing already present on the map (a pawn that has already died), not a mechanism for reviving the dead — Soul Tether only relocates it.
- Only friendly-faction (colony/ally) pawns and their corpses are valid targets; there is no player-facing option to pull a hostile pawn in this feature. The reusable pull component (FR-019) exists specifically so a future, separately-specified "inverse" tattoo can supply a hostile-pawn target filter and a debuff effect later, without requiring any rework here.
- The pull is single-target at both tiers; Tier 2's range increase does not imply or introduce any area-of-effect behavior.
- This feature covers exactly one tattoo's gameplay effect, Soul Tether. The remaining two tattoos from this batch (Stillshroud, Thunderclap Sigil) are out of scope here and will each get their own future spec, per the project's build order (docs/PRD.md §10).
- The cooldown begins at the moment of activation, not at the tend buff's expiry, matching Guardian's Call's and Bastion Ward's existing cooldown convention.
