# TattooMagic — Progress Update, September 19

## What we're building

**Bastion Ward** is the first of the four new tattoos you signed off on back on September 12 (Bastion Ward, Soul Tether, Stillshroud, Thunderclap Sigil). It's a new kind of ability for this mod: instead of buffing the wearer themselves, activating it drops a protective zone on the ground at a location you pick. That zone stays exactly where it lands — it doesn't follow the caster around — and anyone allied standing inside it is completely protected for the ward's duration. This update covers building it from scratch and a full day of hands-on testing that took it all the way to feature-complete.

## What we tested today

**Full protection, the whole duration.** Activating the ability opens a targeting mode with a live preview showing exactly how big the zone will be and how far away you're allowed to place it; cells that are too far or still fogged are clearly rejected. Once placed, we confirmed the zone stays fixed even as the caster walks away, that every ally standing inside — including non-human allies like a colony animal — takes zero damage from repeated melee and ranged hits for the ward's full duration, that a pawn standing just outside the radius takes normal damage, and that protection and the ground indicator both disappear in the same instant the ward expires, with no lag either direction.

**A scope decision, made mid-testing.** The original plan had Bastion Ward block melee and ranged attacks only, leaving fire and explosions to still land — matching how the proposal was originally worded. While confirming that exclusion actually worked, we found the underlying check had no such filter at all yet, so a decision was needed either way. You confirmed you'd rather have full invulnerability instead — any damage negated, fire and explosions included, closer to how Stillshroud is planned to work — so that's what shipped.

**Cooldown and Tier 2.** Confirmed the ability correctly refuses to re-activate while on cooldown, and that reaching the tier-up threshold automatically upgrades the ward in place — no new ritual, no extra cost — with a visibly larger zone and, at Tier 2, a periodic heal for anyone standing inside on top of the same full negation. Both the negation and the healing stop together, exactly at expiry.

**Rescuing allies already under attack — and where that got complicated.** The original design also had casting the ward shove any enemy caught mid-swing on an ally out of the new zone. A one-on-one test of this worked immediately. Staging something messier — a real brawl with six or more hostiles all actively fighting at once — showed it was only ever catching one or two of them, not the whole fight. We tracked that down, fixed the underlying detection so it now correctly clears an entire brawl in one cast, and confirmed it doesn't affect ranged attackers (they're negated, not shoved) or hostiles who haven't actually started fighting yet. Given how much work it took to get that reliable, you asked to make this whole behavior optional rather than something every player gets by default — it now ships as an off-by-default setting in the tattoo's own file, so the ward's core protection works fully either way, and turning the rescue on is a deliberate choice.

**Everything else.** Confirmed an already-active ward keeps working correctly if its caster is downed, killed, or loses the tattoo mid-duration; that two overlapping wards both protect an ally standing in the overlap; that protection updates immediately if a pawn's faction relationship changes mid-ward (a mental break, a capture); that a ward's exact position, remaining duration, and tier survive a save and reload untouched; that the mod's existing tattoo-removal protection covers this one automatically; and that everything above holds identically whether Combat Extended is loaded or not.

## Issues we found (and fixed)

- The ground indicator showing which cells are currently warded didn't render at all at first (too subtle to notice), then rendered as solid magenta once we made it more visible — Unity's own "something's wrong with this material" fallback color. Root-caused by checking exactly how the game's own built-in effects draw a similar ground highlight, then matching that same approach. Fixed and confirmed showing the intended translucent gold tint.
- The biggest find: the "rescue an ally being meleed" check only reliably caught whichever single attacker happened to be mid-swing at the exact instant you clicked to confirm the cast — invisible in a one-on-one test, but it meant a real brawl only had one or two of its attackers shoved out, not the whole fight. Fixed by broadening what counts as "currently attacking" for this check; a fresh six-plus-hostile brawl confirmed it now clears the whole scrum in one cast.

None of these were visible without deliberate, hands-on testing that pushed past the simplest case, which is exactly what today was for.

## Where this leaves Bastion Ward

The mechanics are done. Every part of the design has now been confirmed working in-game: the targeting preview and its reject reasons, the ground-fixed zone and its full-invulnerability protection (melee, ranged, fire, and explosions alike), the cooldown, the Tier 2 upgrade with its larger radius and periodic healing, the optional rescue-eject behavior (off by default, confirmed working when turned on), and every edge case around caster death, overlapping wards, faction changes, save/reload, and Combat Extended. The tattoo's artwork has also been added. The mod rebuilds with zero errors.

What's left is balance, not mechanics. Like the rest of the roster, Bastion Ward is still running on placeholder numbers — the zone's duration and radius per tier, the max cast range per tier, the cooldown, the Tier 2 threshold, and the Tier 2 heal amount are all still placeholders. Tuning that for how it should actually feel in a real playthrough is the next step, alongside the same balance pass planned for everything else.

Bastion Ward — the 14th tattoo, and the first of the new batch — is feature-complete. Soul Tether is next.
