# TattooMagic — Soul Tether Tattoo Proposal (Draft, looking for your sign-off)

This is the write-up from our back-and-forth on the pull ability — single-target only at both tiers, pulls corpses as well as living colonists, and a flat tend speed/quality buff instead of a skill multiplier. Here's the full picture, plus the name we'd like to use if you're happy with it.

## The idea

**Soul Tether** yanks one chosen friendly colonist — or their corpse — straight to the caster, no matter what state they're in: downed, bleeding out, immobile, or dead. It's a battlefield rescue tool: get someone (or someone's body) out of danger and into position to be tended, immediately.

## How it works

- **Single target, chosen by the caster** — not an AoE pull. Tier 2 doesn't change this; see below.
- **Works regardless of status**, including on an actual corpse, not just a downed-but-living pawn. If a colonist has already died and become a corpse, Soul Tether can still pull the body to the caster.
- **On arrival, the target gets a tend buff** — a flat bonus to both tend speed and tend quality, lasting a placeholder duration after the pull (XML-tunable). The idea is "make it possible to heal them better and faster" once they're back at the caster's side.
- **Cooldown** before the caster can use it again, XML-tunable.

## Tier 2

- Same single-target behavior — **not** an AoE pull.
- Increases the maximum range the caster can pull a colonist from, so Tier 2 reaches farther across the map, but still only grabs the one chosen target.
- Bigger tend buff than Tier 1.

## Forward-looking architecture note

You mentioned wanting the target and the applied buff to be swappable, since a future "inverse" version — pulling an *enemy* toward the caster instead, with a debuff instead of a tend buff — is likely to come up later. We'll build Soul Tether's pull-and-apply-effect logic as a reusable piece internally so that a future enemy-pull tattoo can reuse the same plumbing with a different target filter and a different effect, rather than being written from scratch. That's an implementation detail on our end and doesn't change anything about how Soul Tether itself plays.

## Assumptions we're running with (flag any you disagree with)

- The tend buff is a **flat bonus** to tend speed and quality, not a multiplier on the treating colonist's Medicine skill — simpler to reason about and tune. Flag it if you pictured it working off the doctor's skill instead.
- Pull range, cooldown, and tend buff duration/magnitude for both tiers are all **placeholders**, tunable in XML without a rebuild.

## Name

**Soul Tether** — approved.

## Next steps

Once you've signed off — or told us what to change — we'll write it up as a full spec (`specs/017-soul-tether-tattoo-effect/`) and build it against placeholder numbers, then get it in front of you working.
