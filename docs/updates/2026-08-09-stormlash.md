# TattooMagic — Progress Update, August 9

## What we're building

TattooMagic is a RimWorld mod that turns tattoos into a real source of power for your colonists. Instead of being purely cosmetic, a magic tattoo is something a colonist earns and grows into: applied at a dedicated ritual station, it can grant a passive buff (like extra armor or resistance to fire) or a usable ability with its own cooldown (like a short teleport or a battlefield taunt). There are 11 tattoos planned at launch, each one gets stronger the more it's used, and colonists start with room for one tattoo but can earn a couple more slots over time. The mod is being built to work whether or not you're also running Combat Extended.

## What we got working today

**Stormlash** is live — the second tattoo you actively trigger rather than a passive always-on effect, following Guardian's Call. Click the ability and the colonist gets a temporary burst of speed: they move faster and attack faster, then it goes on cooldown. Use it enough and it grows stronger in place — a bigger speed boost, plus something new: for the duration of that boost, the colonist becomes genuinely immune to anything that would slow their movement, whether that's another tattoo's slowing effect or, if you're running Combat Extended, the movement penalty from being pinned down under suppressive fire. That last part matters for a "charge in fast" tattoo — without it, the very act of running into a firefight would suppress and slow the colonist right back down, defeating the point.

Because this is only the second ability-style tattoo, a good chunk of today was making sure the "click a button, get a temporary effect, go on cooldown" machinery we built for Guardian's Call genuinely works for a second, completely different ability — not just something that happened to work once. It did: Stormlash's button shows up and works correctly alongside Guardian's Call's on a colonist with both, with no changes needed to Guardian's Call's own code at all.

The immunity claim took real, hands-on testing to actually nail down, and you were a big part of that today. We didn't just check that the numbers looked bigger — we specifically proved the slow gets *cancelled out entirely*, not just outweighed by the speed boost. That meant deliberately getting a colonist slowed by another tattoo's effect while Stormlash was active and confirming the slow had zero effect at all, and separately putting a colonist under real suppressive fire from actual hostiles with Combat Extended running and confirming the same thing there. Both held up.

Along the way we also ran into a genuinely tricky bug: the speed boost worked fine, but the ranged-attack-speed half of it silently didn't apply at all, even though the code looked identical to the melee half that worked. It turned out to be a subtle quirk in how RimWorld itself decides which stats are worth recalculating live versus caching permanently for performance — Stormlash happened to be the first tattoo to touch a stat that fell into the "cache it forever" bucket. Tracked it down by digging into the game's own compiled code rather than guessing, and fixed it at the source so it can't quietly bite any future tattoo the same way. We also added always-on (but hidden from normal play) diagnostic logging for combat events, at your request, so tracking down anything like this in the future should go a lot faster than it did today.

Separately, we also fixed a log-spam issue you flagged from the ritual station — it was writing debug messages to the game log constantly, even outside of Developer Mode. That's now fixed for good.

Stormlash also has its own custom ability icon now, made from the artwork you provided, instead of a placeholder.

## What's not in yet

7 of the 11 tattoos are still inert — applying them works, but they don't grant anything yet. The system for earning extra tattoo slots also still isn't in. And as always, the exact numbers (speed bonus, duration, cooldown, how many uses it takes to reach tier 2) are still placeholders — nothing's tuned for real balance yet.
