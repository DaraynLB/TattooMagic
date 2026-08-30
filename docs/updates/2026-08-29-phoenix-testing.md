# TattooMagic — Progress Update, August 29

## What we're building

TattooMagic is a RimWorld mod that turns tattoos into a real source of power for your colonists. Instead of being purely cosmetic, a magic tattoo is something a colonist earns and grows into: applied at a dedicated ritual station, it can grant a passive buff or a usable ability with its own cooldown. Phoenix — the tattoo about cheating death itself — is the newest and most ambitious one yet, and this update covers a full day of hands-on testing that took it from "the basics work" to the whole design confirmed in-game and the feature complete.

## What we tested today

**The 3-colonist limit, every angle.** Applying a 4th is correctly blocked, a colonist who's dead and waiting to come back still counts, coming back successfully still counts, permanently losing a wearer frees their spot, a new recruit who already has the tattoo is allowed to keep it as a temporary exception without letting anyone else sneak in past the limit, and leaving the colony (captured, recruited away, etc.) frees the slot the instant it happens.

**Tier 2 and the paralysis debuff.** Staying alive long enough upgrades the tattoo in place — no ritual needed. We confirmed the 3rd time a colonist comes back from death, they're hit with a temporary near-total paralysis as a real cost for cheating death that many times, that a 4th time makes the paralysis last even longer, and that a failed attempt never triggers it — only an actual successful comeback does, even though every attempt (success or fail) counts toward the total.

**The passive bleeding-protection ability, fully.** Confirmed it correctly ignores minor scratches, targets only the single worst wound when a colonist has more than one bleeding injury at once (leaving the rest for normal medical care), always prioritizes a severed limb over an ordinary wound no matter how bad that wound looks, and — in a very deliberate side-by-side test — that the upgraded Tier 2 version procs dramatically more often than the base version while leaving an identical burn behind either way, exactly as designed.

**The tattoo can't be cheesed off, and losing it does the right thing.** Confirmed there's no ordinary way to strip the tattoo — it takes cheat-level tools — and that using those tools to remove it from a living colonist correctly frees up their colony slot.

**Persistence and compatibility.** Confirmed nothing about a pending revival, a guaranteed cremation, tier progress, or the colony-wide count gets lost or duplicated across a save and reload — this got exercised constantly throughout the whole day, not just as a one-off check. Also confirmed the mod loads and plays cleanly with Combat Extended both installed and completely removed, and with the optional Royalty and Ideology expansions absent. Phoenix was tested sharing a single colonist with both Guardian's Call and Vampiric Thorn at once — including that colonist dying, coming back, and then landing melee hits afterward — with every tattoo's own behavior working normally and no interference in either direction.

## Issues we found (and fixed)

- Two related bugs where the game's internal bookkeeping for "who currently wears this tattoo" kept incorrectly re-adding and re-removing certain colonists over and over — a corpse that was already gone for good, and a colonist destroyed outright through a cheat tool. Neither was visible in normal play, just log clutter, but both are fixed.
- The tattoo's wait-before-reviving time couldn't be set to anything less than a full day, which would have made it awkward to fine-tune later. Fixed so it can be set to any amount of time, including part of a day.
- Once a tattoo ritual was queued up for a colonist, there was no way to back out of it if you changed your mind and gave that colonist a different order — they'd stay stuck "waiting" forever. Added a proper cancel option.
- The bleeding-protection passive could throw an error in the specific case where it tried to treat a limb that had already been severed entirely. Fixed.
- Removing the tattoo from a living colonist through cheat-level tools wasn't actually freeing up their colony slot afterward, even though the removal itself worked. Fixed — removal now correctly opens the slot back up immediately.

None of these were visible in ordinary play before today — all six turned up specifically because of deliberate, thorough testing, which is exactly what today was for.

## Where this leaves Phoenix

The mechanics are done. Every part of the design has now been confirmed working in-game: coming back from real death at both tattoo tiers, the daily retry and the decomposition-based odds, the deliberate-cremation guaranteed comeback, the escalating paralysis cost, the 3-colonist colony limit from every angle, the passive bleeding protection, the tattoo surviving death and resisting removal, full save-and-reload safety, and clean coexistence with the other tattoos and with Combat Extended. The handful of test-only tuning values we'd dialed down to make testing fast (mainly the revive wait, temporarily cut to half a day) have been put back to their real placeholder settings, and the mod rebuilds with zero errors.

What's left is balance, not mechanics. Like the rest of the roster, Phoenix is still running on placeholder numbers — the real revive wait is a full 5 days at Tier 1 and 3 at Tier 2, the success odds, the burn severity, the paralysis duration, the days-alive threshold for the Tier 2 upgrade, and so on. Tuning all of that for how it should actually feel in a real playthrough is the next step, and happens alongside the same balance pass planned for every other tattoo.

Phoenix — the 14th tattoo — is feature-complete.
