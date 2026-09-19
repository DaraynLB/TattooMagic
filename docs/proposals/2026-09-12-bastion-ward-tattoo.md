# TattooMagic — Bastion Ward Tattoo Proposal (Draft, looking for your sign-off)

This is the write-up from our back-and-forth on the party-barrier idea — ground-fixed placement, full damage negation instead of a depleting shield pool, and enemies caught meleeing an ally inside the zone getting forcibly shoved out the moment it goes up. Here's the full picture, plus the name we'd like to use if you're happy with it.

## The idea

**Bastion Ward** drops a fixed protective zone on the ground. Any ally standing inside takes zero damage — melee or ranged — for as long as the ward holds. It's a "make a stand here" tool: cast it before or during a fight to turn one spot into a safe zone for whoever's in it.

## How it works

- **Ground-targeted, not caster-following.** Once cast, the ward is fixed in place until it expires — it doesn't move with the caster or anyone else.
- **Full damage negation**, not a depleting HP pool. Nothing gets through while the ward holds — no partial soak, no "shield breaks after enough hits."
- **Duration:** placeholder ~8 seconds, XML-tunable.
- **Blocks both melee and ranged** attacks aimed at anyone standing inside.
- **On cast, forcibly ejects any enemy currently in melee with an ally inside the radius** — if you drop the ward on a fight already in progress, attackers get shoved clear instead of getting a free hit in from inside the safe zone.
- **Cooldown after the ward ends** (or after it forces enemies out) before the caster can use it again — back-to-back casting would be too strong. Placeholder value, XML-tunable.

## Tier 2

- Larger radius.
- Adds a small healing buff to allies standing inside, on top of the damage negation.

## Assumptions we're running with (flag any you disagree with)

- The forced-eject only applies to enemies actively **meleeing** an ally at the moment of cast — an enemy just standing or shooting from inside the radius (if that's even possible before we lock down ranged-block behavior) isn't specifically called out as needing removal, but we'll revisit if testing shows that's needed too.
- Duration, radius, cooldown, and the Tier 2 heal amount are all **placeholders** — real values come in the balance pass, same as the rest of the roster, but all of them live in XML so you can tune them yourself without a rebuild.

## Name

**Bastion Ward** — approved.

## Next steps

Once you've signed off — or told us what to change — we'll write it up as a full spec (`specs/016-bastion-ward-tattoo-effect/`) and build it against placeholder numbers, then get it in front of you working.
