# TattooMagic — Progress Update, August 14

## What we're building

TattooMagic is a RimWorld mod that turns tattoos into a real source of power for your colonists. Instead of being purely cosmetic, a magic tattoo is something a colonist earns and grows into: applied at a dedicated ritual station, it can grant a passive buff (like extra armor or resistance to fire) or a usable ability with its own cooldown (like a short teleport or a battlefield taunt). There are 11 tattoos planned at launch, each one gets stronger the more it's used, and colonists start with room for one tattoo but can earn a couple more slots over time. The mod is being built to work whether or not you're also running Combat Extended.

## What we got working today

**Bloodrune** is live — the third ability colonists can actively trigger, after Guardian's Call and Stormlash. Click it and the colonist opens a short burst of self-healing, patching up their worst wound first before moving on to lesser ones, then the ability goes on cooldown. Use it enough and it grows stronger in place: a bigger, longer-lasting burst, plus something new — for as long as that burst is running, the colonist also feels less pain from whatever injuries they've still got. We put it through its paces on a colonist who'd taken a real beating: pain went from Severe to None, several wounds closed outright, and one capacity that had dropped low enough to make her "incapable of walking" recovered enough to lift that restriction — all from the tattoo alone, no doctor involved.

You caught something worth fixing properly rather than waving through: we'd already confirmed Bloodrune's removal is blocked the same way every other tattoo's is, but that check only ever exercised the button in the health panel — which itself doesn't even appear without Developer Mode's "God Mode" toggled on. That's a different question from "does the underlying protection hold if something reaches in through a different door," which is really the scenario the protection exists for in the first place. So we built a small, permanent testing tool that calls the removal directly, sidestepping that button entirely, and confirmed the protection holds up there too. That tool isn't specific to Bloodrune — it'll work for testing removal protection on any tattoo we add from here on, so this kind of check gets faster for everything still to come, not just today's tattoo.

We also tracked down why your Bloodrune artwork was showing up with a white square behind it instead of blending into the ability bar like Guardian's Call and Stormlash's icons do. Turned out to be a quirk in how the image-editing step saved the file — some extra metadata the game's engine doesn't handle well — not anything wrong with the art itself. Fixed at the file level; the icon now sits in the toolbar exactly like the other two.

## What's not in yet

6 of the 11 tattoos are still inert — applying them works, but they don't grant anything yet. Numbers are still a placeholder story across the board: how much healing Bloodrune restores, how long its burst lasts, and how many uses it takes to reach its stronger tier are all deliberately small test values, not tuned for real balance yet. That's still on the list for the dedicated balance pass once more tattoos are in and we can weigh them against each other properly.
