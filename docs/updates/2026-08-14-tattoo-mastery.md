# TattooMagic — Progress Update, August 14

## What we're building

TattooMagic is a RimWorld mod that turns tattoos into a real source of power for your colonists. Instead of being purely cosmetic, a magic tattoo is something a colonist earns and grows into: applied at a dedicated ritual station, it can grant a passive buff (like extra armor or resistance to fire) or a usable ability with its own cooldown (like a short teleport or a battlefield taunt). There are 11 tattoos planned at launch, each one gets stronger the more it's used, and colonists start with room for one tattoo but can earn a couple more slots over time. The mod is being built to work whether or not you're also running Combat Extended.

## What we got working today

Every colonist has been stuck at one tattoo slot since the very first build — the ritual station could apply a tattoo, but nothing let anyone ever earn room for a second or third. That's live now: colonists build up hidden "tattoo mastery" just by getting real use out of the tattoos they already have, and once they've built up enough, their slot count grows on its own — no ritual, no ingredients, just a message on screen telling you it happened. Keep using Guardian's Call or Stormlash and a colonist will earn their second slot, then eventually their third, entirely through normal play. The ritual station's tattoo picker now also shows a colonist's current slot count up front, so you can see at a glance how many they've got and how many are filled.

You asked a good question partway through testing this — what happens to a colonist who only ever gets a passive tattoo like Frost Sigil, since they never click an ability button at all? Turned out the honest answer at that point was "nothing, they're stuck at one slot forever," which wasn't a good place to leave things given roughly half the tattoo roster is passive. So we extended the mastery system to cover that too: a passive tattoo doing its thing in combat now builds mastery progress the same way clicking an active ability does, so every colonist has a real path to more slots regardless of which kind of tattoo they end up with.

We also caught and fixed a data-integrity issue while stress-testing the new slot-count display: if a tattoo ever ended up on a colonist through anything other than the ritual station itself, the game's own bookkeeping didn't know about it, which could have let the ritual station be talked into granting a duplicate of a tattoo a colonist already had. That's now fixed — the ritual station always double-checks a colonist's actual tattoos before granting a new one, so this can't happen no matter how a tattoo got there.

## What's not in yet

7 of the 11 tattoos are still inert — applying them works, but they don't grant anything yet. Numbers are still a placeholder story across the board: how much mastery progress it takes to earn slot 2 and slot 3 hasn't been tuned, and neither has how fast a passive tattoo builds that progress compared to an active one — right now a passive tattoo in a real fight can rack up progress considerably faster than clicking an on-cooldown ability, which is on our list for the real balance pass once more tattoos are in and we can weigh everything against each other properly.
