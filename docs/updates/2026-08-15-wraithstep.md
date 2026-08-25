# TattooMagic — Progress Update, August 15

## What we're building

TattooMagic is a RimWorld mod that turns tattoos into a real source of power for your colonists. Instead of being purely cosmetic, a magic tattoo is something a colonist earns and grows into: applied at a dedicated ritual station, it can grant a passive buff (like extra armor or resistance to fire) or a usable ability with its own cooldown (like a short teleport or a battlefield taunt). There are 11 tattoos planned at launch, each one gets stronger the more it's used, and colonists start with room for one tattoo but can earn a couple more slots over time. The mod is being built to work whether or not you're also running Combat Extended.

## What we got working today

**Wraithstep** is live — the fourth ability-style tattoo, after Guardian's Call, Stormlash, and Bloodrune. This one works differently from the others in a way that shaped a lot of today: instead of just clicking a button and something happening, Wraithstep asks you to pick where the colonist goes. Click it, a range circle appears around the colonist, and clicking anywhere inside it teleports them there instantly — including straight through a wall or a mountain, since the whole point is reaching somewhere a normal walk couldn't. Cells outside range, unwalkable ground, an occupied tile, or anywhere still under fog of war are all clearly rejected with a reason shown right at the cursor, the same way vanilla RimWorld already handles targeting for thrown items or aimed weapons.

At tier two — reached by using it enough times — the range gets longer, and landing gives the colonist a brief window where hostiles can't target them at all. That part took the bulk of today's testing, and it's worth telling the full story rather than just the happy ending. Our first pass looked right in a quick check, but you kept pushing on it with real encounters instead of taking that at face value, and that's what actually surfaced the problem: a hostile that already had eyes on the colonist before the teleport — mid-fight, already swinging — could still land a hit or two even during the "can't be targeted" window. The protection was only stopping a hostile from *choosing* the colonist fresh; it wasn't doing anything about a lock a hostile already had. We rebuilt it so the protection now actively breaks an existing lock the instant the window opens, not just blocks new ones — and this time deliberately tested both cases (a hostile that's never noticed the colonist at all, and one that's already mid-fight) until both held up clean.

You also caught something we'd have otherwise shipped broken: a downed colonist could still use Wraithstep. Every other ability already correctly greys out while a colonist is down; this one just never got that check. Fixed, and confirmed live — the ability now shows disabled with a clear reason the moment a colonist goes down.

Since you're running Combat Extended, we made a point of testing the "can't be targeted" protection against an actual CE-armed hostile with a gun, not just melee — CE handles ranged aiming through its own separate system, and it turned out the earlier testing that day had only ever exercised melee attackers. That gun-toting test came back clean too. And as usual, Wraithstep's own artwork needed the same background-and-watermark cleanup Bloodrune's did before it would blend into the ability bar properly.

Last thing, at your request: TattooMagic now has its own entry in RimWorld's Mod Options screen, with a single checkbox for detailed debug logging. It's off by default, so a normal game stays quiet — but if something ever needs troubleshooting again, turning it on doesn't require touching RimWorld's own Developer Mode at all.

## What's not in yet

5 of the 11 tattoos are still inert — applying them works, but they don't grant anything yet. Numbers are still placeholders across the board, same as every tattoo so far — Wraithstep's range, cooldown, and how many uses it takes to reach tier two are all deliberately small test values, not tuned for real balance.
