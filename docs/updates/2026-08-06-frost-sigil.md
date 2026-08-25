# TattooMagic — Progress Update, August 6

## What we're building

TattooMagic is a RimWorld mod that turns tattoos into a real source of power for your colonists. Instead of being purely cosmetic, a magic tattoo is something a colonist earns and grows into: applied at a dedicated ritual station, it can grant a passive buff (like extra armor or resistance to fire) or a usable ability with its own cooldown (like a short teleport or a battlefield taunt). There are 11 tattoos planned at launch, each one gets stronger the more it's used, and colonists start with room for one tattoo but can earn a couple more slots over time. The mod is being built to work whether or not you're also running Combat Extended.

## What we got working today

Last time, we got tattoos *applying* to colonists. Today we made the first one actually *do* something: **Frost Sigil** is live. A colonist wearing it runs colder-hardy than normal, and any attacker who lands a melee hit on them has a chance to get slowed down by the frost — punishing whoever's dumb enough to get in close. Fight enough with it on and the sigil grows stronger on its own: better cold resistance, a better chance to slow attackers, and eventually a rare chance to freeze an attacker solid for a few seconds. No extra ritual needed for the upgrade — it just happens as the colonist keeps fighting.

We tested this extensively in a live colony today: applied the tattoo, picked fights to trigger the slow, watched it upgrade to its stronger tier, confirmed the freeze actually locks an attacker in place, and saved and reloaded the game to make sure none of that progress gets lost. Along the way we also found and fixed a real bug — the slow effect was only triggering against armed attackers, so an unarmed brawler's fists wouldn't ever trigger it. That's fixed now; any melee attacker counts.

Just as importantly, Frost Sigil wasn't built as a one-off. We built it alongside some shared plumbing that the other tattoos can now reuse — things like "grant a stat bonus while a tattoo is worn" and "react when the wearer gets hit" — so the next tattoo shouldn't take nearly as long to wire up as this first one did.

We've also already scoped out **Serpent's Eye** (better accuracy, and if you're running Combat Extended, steadier aim and better ammo effectiveness too) as the next tattoo to build — that's planned, not built yet.

## What's not in yet

9 of the 11 tattoos are still inert — applying them works, but they don't grant anything yet. Serpent's Eye is next up. The system for earning extra tattoo slots also still isn't in.
