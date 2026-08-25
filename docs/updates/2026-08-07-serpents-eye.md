# TattooMagic — Progress Update, August 7

## What we're building

TattooMagic is a RimWorld mod that turns tattoos into a real source of power for your colonists. Instead of being purely cosmetic, a magic tattoo is something a colonist earns and grows into: applied at a dedicated ritual station, it can grant a passive buff (like extra armor or resistance to fire) or a usable ability with its own cooldown (like a short teleport or a battlefield taunt). There are 11 tattoos planned at launch, each one gets stronger the more it's used, and colonists start with room for one tattoo but can earn a couple more slots over time. The mod is being built to work whether or not you're also running Combat Extended.

## What we got working today

**Serpent's Eye** is live. A colonist wearing it shoots more accurately than before — and if you're also running Combat Extended, their weapon sways and kicks less while aiming too, on top of the accuracy bonus. Land enough shots with it on and the tattoo grows stronger on its own: better accuracy, steadier aim, and (under Combat Extended) their ammunition starts hitting noticeably harder. No extra ritual needed for the upgrade — same as Frost Sigil, it just happens as the colonist keeps fighting.

This one was the first tattoo that actually had to behave differently depending on whether Combat Extended is installed — CE changes what "shooting accuracy" even means under the hood, so the tattoo has to know which version of the game it's talking to and grant the right kind of bonus either way. We tested both configurations directly: applied the tattoo with Combat Extended off, then again with it on, and confirmed the accuracy bonus, the sway/recoil reduction, the ranged-hit counter, the automatic upgrade, and the ammo-effectiveness bonus all worked correctly in both, with the ammo bonus confirmed to only ever affect the tattooed colonist's own shots, never leaking onto anyone else using the same ammunition. We also saved and reloaded to confirm none of that progress gets lost.

We also closed a gap you flagged after Frost Sigil shipped: previously, nothing stopped some other mod's generic "clear all hediffs" tool from stripping a tattoo off a colonist outright. That's fixed now — an applied tattoo can no longer be removed except through this mod's own ritual system (Developer Mode's own removal tooling still works fine for testing). This applies to every tattoo already in the game, including Frost Sigil, not just Serpent's Eye.

## What's not in yet

9 of the 11 tattoos are still inert — applying them works, but they don't grant anything yet. The system for earning extra tattoo slots also still isn't in.
