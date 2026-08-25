# TattooMagic — Progress Update, August 8

## Before you install this build

This is the first build we're handing over for you to actually play, not just a status update — so one important thing first: **please make a backup of your save before loading it with this mod enabled.** We've tested it thoroughly on our end, but this is new ground (the first tattoo that actively interferes with enemy AI mid-fight), and neither of us can fully rule out an odd interaction with your specific save or mod list until it's been played for real. If anything looks off, you can drop back to your backup with nothing lost. A save made *before* this mod ever touches your colony is the safest kind to keep on hand.

## What we're building

TattooMagic is a RimWorld mod that turns tattoos into a real source of power for your colonists. Instead of being purely cosmetic, a magic tattoo is something a colonist earns and grows into: applied at a dedicated ritual station, it can grant a passive buff (like extra armor or resistance to fire) or a usable ability with its own cooldown (like a short teleport or a battlefield taunt). There are 11 tattoos planned at launch, each one gets stronger the more it's used, and colonists start with room for one tattoo but can earn a couple more slots over time. The mod is being built to work whether or not you're also running Combat Extended.

## What we got working today

**Guardian's Call** is live — the first tattoo of a different kind than Frost Sigil or Serpent's Eye. Those two are passive, always-on effects; Guardian's Call is something the colonist actively *uses*: click the ability button and every hostile within range is forced to turn and focus their attacks on that colonist for a few seconds, then it goes on cooldown. It's a proper taunt — pull the raiders off your wounded doctor, or your one colonist with a gun, and onto whoever's wearing the tattoo instead. Use it enough and it grows stronger in place, eventually adding a temporary armor boost for as long as the taunt is active.

The interesting part of today's work was making the taunt actually *feel* like a taunt. Our first pass only reliably worked on an enemy that hadn't picked a fight yet — a raider already trading blows with one of your colonists wouldn't necessarily get pulled off them. You flagged that a real taunt should yank someone away mid-swing, not just influence who they pick next time, so we dug into exactly how the game decides who an enemy is currently attacking and fixed it to work instantly, even mid-fight. We confirmed this by watching a raider's actual landed hits switch from one colonist to the tattooed one the moment the ability was used, both with Combat Extended off and with it on — Combat Extended runs its own separate combat logic under the hood, so both had to be checked independently, and both now hold correctly for the ability's whole duration.

Also confirmed today: the ability button only ever shows up on a colonist actually wearing the tattoo, the cooldown genuinely blocks spamming it, the taunt drops instantly if the tattooed colonist goes down or is killed (no lingering fixation on someone who's no longer a threat), the tier-2 upgrade and its armor bonus both showed up and disappeared at exactly the right moments, and progress survives a save and reload. The existing protection against tattoos being stripped off by some other mod's tools carried over automatically too — no extra work needed for that part. Guardian's Call also now has its own custom icon on the ability button instead of a placeholder.

## What's not in yet

8 of the 11 tattoos are still inert — applying them works, but they don't grant anything yet. The system for earning extra tattoo slots also still isn't in. And a heads-up on numbers: the taunt's range, duration, cooldown, how many uses it takes to reach tier 2, and the armor bonus are all still placeholder values — nothing's been tuned for actual balance yet, that pass comes later once more of the tattoos are in and we can weigh them against each other.
