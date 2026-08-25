# TattooMagic — Progress Update, August 21

## What we're building

TattooMagic is a RimWorld mod that turns tattoos into a real source of power for your colonists. Instead of being purely cosmetic, a magic tattoo is something a colonist earns and grows into: applied at a dedicated ritual station, it can grant a passive buff (like extra armor or resistance to fire) or a usable ability with its own cooldown (like a short teleport or a battlefield taunt). There are 11 tattoos planned at launch, each one gets stronger the more it's used, and colonists start with room for one tattoo but can earn a couple more slots over time. The mod is being built to work whether or not you're also running Combat Extended.

## What we got working today

**Ironskin Glyph** is live — a passive tattoo that makes a colonist tougher against physical damage of any kind: bullets, blades, fists, shrapnel. While worn, it raises their armor rating and quietly reduces how much of that damage actually gets through. Absorb enough hits and it upgrades in place to a stronger version: more armor, a bigger cut off every hit, and a real chance to shrug a hit off entirely and take nothing at all.

Your new artwork for it came in clean this time — transparent background already in good shape, no touch-up needed beyond trimming the "made with AI" badge off the corner and resizing it down to match how the other tattoo icons look.

The real story today is that we didn't get caught out twice by the same problem. Back on the 16th, Ember Ward's fire-damage reduction turned out to be silently doing nothing under Combat Extended, because its armor math only checks a colonist's own built-in resistances for creatures with natural hide or plating — never for ordinary humans. Before writing a line of Ironskin Glyph's code this time, we went back into Combat Extended's own math specifically to check whether that same gap existed for ordinary weapon and melee damage too. It did, in the same place, for the same reason. Knowing that going in meant we could build the fix in from day one instead of discovering it the hard way during testing, the way we had to last time.

That paid off. With Combat Extended running, a live fight showed the reduction firing on every single qualifying hit — an exact, consistent percentage cut each time, not something that needed dozens of trials to see a trend. Without Combat Extended, though, a couple of hits in a row looked unchanged, and you flagged it as a possible miss. It wasn't — that's expected behavior, not a bug: with Combat Extended we step in and guarantee the cut ourselves, but without it we're just handing RimWorld's own armor system a slightly better number and letting its own dice decide whether that hit gets reduced at all. At the small placeholder strength we're testing with, "no visible effect" is actually the most likely outcome on any single hit — the tattoo is genuinely working, just through two different mechanisms depending on what else is installed, exactly as intended.

We also caught the trickiest possible timing case live, mid-fight: a single hit that pushed the counter past the upgrade threshold, immediately used the new stronger tier's numbers, and won the full-negate roll, all in the same swing, with no hiccups. Removing the tattoo through cheats (the only way it's supposed to come off) shut off every part of the effect immediately, and an unauthorized removal attempt was correctly refused. A colonist's progress survived a save and reload without resetting. And in a multi-colonist battle run alongside several other tattoos, Guardian's Call's battlefield taunt — probably the single most delicate thing this mod does — kept working perfectly the whole time.

## What's not in yet

1 of 11 tattoos is still inert — Starlight Ward, a passive buff like this one. Numbers are still placeholders across the board, same as every tattoo so far — Ironskin Glyph's armor bonus, damage reduction, full-negate chance, and how many hits it takes to reach its stronger tier are all deliberately small test values, not tuned for real balance yet.
