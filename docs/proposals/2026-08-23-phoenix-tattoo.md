# TattooMagic — Phoenix Tattoo Proposal (Draft, looking for your feedback)

This is an updated version of this proposal based on your notes — burns instead of scars, the tattoo staying equipped through death, a wait-and-retry mechanic instead of an instant one-shot, a new passive bleed-cauterizing ability, cremation as a guaranteed-return option, and a hard cap on how many colonists can ever have this at once. Here's the full picture as it stands now.

## The idea

A tattoo that brings a colonist back from actual death. Unlike our first draft, the tattoo itself is never used up — it stays equipped for life, and just goes to work automatically whenever its wearer dies.

## Keeping it rare: only 3 per colony

Phoenix isn't meant to be something every colonist eventually has — we're proposing a hard cap of **3 Phoenix tattoos active across your whole faction at a time** (all your bases combined, not just one map), so it stays a rare, deliberate choice rather than something everyone ends up wearing.

- A tattoo counts against the cap the moment it's applied, and keeps counting even while its wearer is currently dead and waiting out a revival attempt — the corpse might still come back, so it still holds the slot.
- A slot frees up only when a Phoenix tattoo is genuinely gone for good: its wearer is permanently lost (body destroyed or fully decayed with no cremation save), or it's successfully removed through the future removal ritual — a failed removal attempt leaves it stuck forever, per that proposal, so it keeps counting in that case.
- If a colonist carrying a Phoenix tattoo leaves the colony for any reason — captured, defects, sent away — the tattoo is automatically stripped from them the moment they go. That closes an obvious loophole: banishing someone on purpose to free up a slot doesn't work, since they lose the tattoo the instant they leave instead of taking it with them.
- If a new colonist joins already wearing a Phoenix tattoo (a rescue, a quest reward, a wanderer) while the faction's already at 3, they're allowed to keep it as a temporary 4th — but no further Phoenix tattoos can be applied anywhere in the faction until the count drops back to 3 naturally.
- The ritual station simply won't offer Phoenix as an option once the cap is reached, the same way it wouldn't offer a tattoo a colonist has no free slot for.

## How it triggers

Actual death — not downed, not bleeding out. The body has to still have an intact head and not be destroyed, which matches how resurrection already works elsewhere in RimWorld (the Resurrector Mech Serum uses the same basic gate), so this will feel familiar rather than like a new rule players have to learn from scratch.

## The wait

Once a wearer dies, the tattoo doesn't fire instantly — it counts down. Tier 1 takes 5 days before the first attempt; Tier 2 only takes 3. No ritual station visit, no player action required — it just goes off on its own once the timer's up.

## The revival roll

Whether that first attempt actually succeeds depends on how decomposed the body is by then — a fresher corpse has better odds. If it fails, the colonist stays dead for now, but the tattoo doesn't give up: it tries again one day later, and keeps trying once a day after that. If the body just sits there decomposing naturally, each retry's odds get a little worse than the last — but never mathematically hit zero, right up until the body has either been destroyed outright or has fully decayed away, at which point it's gone for good either way. Keep the body somewhere cold enough to stop decomposition, though, and the odds stop sliding too — repeated attempts at a steady chance will, given enough tries, eventually catch. That's a real strategic choice we like: rush a burial and you risk losing them forever; get them into a freezer and ride it out, and it's just a matter of time and burns (see below).

## The cost — every attempt, not just the ones that work

Every attempt leaves a burn on the body, success or failure. Severe at Tier 1, lighter at Tier 2. These stack, so a colonist who needed several tries — or who's been revived several times over their life — visibly accumulates scarring. That's the "battle-worn" look we wanted, just built from attempts now instead of reapplying the tattoo.

Every attempt also advances a hidden counter toward **Paralytic Abasia**, a temporary near-total-paralysis debuff. The first two times in a colonist's life it doesn't apply at all. From the third attempt on, it does — and since a corpse obviously can't be paralyzed, a string of failed attempts banks silently and the debuff lands in full the moment a revival actually succeeds. Once it starts, it lasts 2 days the first time, and one day longer each time after that.

## Reaching Tier 2 — and what it rewards

A colonist reaches Tier 2 by staying alive for a stretch of days while wearing the tattoo — that clock resets to zero every time they're revived, so it's really measuring "how long have they stayed out of trouble since the last time," not time since the tattoo went on. Once they hit Tier 2 it's permanent, same as every other tattoo — dying again doesn't knock them back down to Tier 1.

Tier 2 is more than a shorter wait and a lighter burn, too — reaching it also wipes the Paralytic Abasia counter clean. A colonist who's fought their way to Tier 2 earns a genuine fresh start on that escalation, not just a faster, gentler revival.

## What ends it for good

Two things stop this working for a colonist permanently: the body gets destroyed outright, or it's left to fully decay away with no attempt at preservation — which ends the same way in practice. Either way, that colonist is done; no more revivals. There's one deliberate exception to this rule — see below.

## Cremation: burned to ash, guaranteed to return

This is the one case where destroying the body doesn't end things — it's actually the surest way to bring someone back, in keeping with the phoenix's own myth of rising from its own ashes.

- Has to be a deliberate cremation through the crematorium building's bill specifically. A corpse destroyed some other way — a burning building, a raid, a wildfire — doesn't count.
- Once cremated, decomposition stops mattering entirely. However far gone the body was, the next scheduled attempt is guaranteed to succeed instead of rolling the usual decomposition-based chance.
- It still has to wait out the tier's usual timer — 5 days at Tier 1, 3 at Tier 2. Cremating doesn't skip the wait, it just guarantees what happens once the wait is up.
- It still counts as an attempt toward the Paralytic Abasia counter, same as any other revival.
- Any gear left on the body when it's cremated is lost along with it — only the colonist comes back, with their stats intact. Stripping the body first, same as you'd do for an ordinary cremation, is how you keep their gear.

## A second ability: passively cauterizing bleeds

Separate from the revival mechanic above, and never touching that wait timer, the tattoo also gives its wearer a passive chance to auto-cauterize a bleeding wound before it becomes fatal on its own.

- Checked every time a bleeding wound actually deals its blood-loss tick, not just once when the wound is first inflicted — that gives it a real shot at catching a bleed-out in progress, not just a one-time roll at the moment of injury. We'll build it this way first; if testing shows it's a performance problem, the fallback is checking once per wound instead, but we don't expect it to come to that.
- A trivial wound — a scratch — never rolls at all. The chance only applies to bleeding serious enough to actually threaten the colonist.
- If a colonist is bleeding from more than one body part at once, whichever single wound is the most severe decides where the attempt targets — not how many wounds are on a given part. A severed limb bleeding out always takes priority over everything else, regardless of how severe any other wound is.
- On a successful proc, every qualifying bleed on that one targeted body part cauterizes at once. Wounds on other body parts are untouched and still need normal tending, or another lucky proc of their own.
- Tier 2 has a better chance to proc than Tier 1 — same pattern as every other passive tattoo in the roster.
- A successful cauterization still leaves a burn, using the exact same burn system as the revival burns above — it stacks into that same visible scar history. Unlike the revival burns, this one doesn't get lighter at Tier 2; only the proc chance improves, not the mark it leaves.

## Still open on our end

- The exact curve for how revival chance drops with decomposition, and how many days of "staying alive" it takes to reach Tier 2 — both are balance-pass numbers, same as every other tattoo's tuning. We'll land on real values during testing rather than guessing now.
- What happens if a body mid-wait gets buried or cremated before an attempt resolves. Our assumption: that ends it the same as outright destruction, since the body's no longer there to try on. Flag it if you pictured something different — like being able to exhume a grave to try again.

Once we hear back on these, we'll turn this into a real spec the same way we did for the other eleven tattoos and get it in front of you working.
