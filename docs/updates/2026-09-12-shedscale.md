# TattooMagic — Progress Update, September 12

## What we're building

TattooMagic is a RimWorld mod that turns tattoos into a real source of power for your colonists. Instead of being purely cosmetic, a magic tattoo is something a colonist earns and grows into: applied at a dedicated ritual station, it can grant a passive buff or a usable ability with its own cooldown. **Shedscale** is the newest one — a reptile-themed tattoo that automatically regrows a colonist's missing body parts over time, the same hands-off way Phoenix works toward reviving a dead colonist. This update covers building it from scratch and a full day of hands-on testing that took it all the way to feature-complete.

## What we tested today

**Automatic regrowth, at both tiers.** Lost a hand, a leg, an eye — didn't matter, no ritual or player action needed. Confirmed a missing part genuinely grows back on its own after the wait, that several missing parts can all be regrowing at the same time with no one-at-a-time limit, and that a regrown part comes back weaker at first (Tier 1) but perfectly whole once the tattoo has proven itself (Tier 2) — including internal organs, which only become regrowable once you reach that point. We also confirmed the regrowth genuinely gives a clean slate: an old scar or lingering injury on a part is gone once it grows back.

**Prosthetics block it — and we now warn you.** Fitting a colonist with a prosthetic or bionic stops that part from ever regrowing naturally, for as long as it's installed, with zero partial progress hiding underneath. We added a persistent warning that flags exactly which colonists have this happening, and confirmed removing the prosthetic lets the tattoo start that part over fresh, not pick up where anything left off.

**The cost of regrowing.** A colonist with something actively regrowing runs hungrier and carries some pain — worse the more parts are regrowing at once — plus a flat mood hit from the sheer unpleasantness of watching your own body do this, which doesn't get worse no matter how many parts are involved at the same time. We also confirmed a malnourished colonist's regrowth simply pauses until they've eaten enough to recover, rather than continuing to progress while they're starving.

**Reaching the upgrade, both ways.** The tattoo permanently upgrades itself once a colonist has either worn it long enough or regrown enough parts with it, whichever happens first — we isolated and confirmed each path separately so we knew neither was accidentally masking the other. Reaching the upgrade also reaches back and cleans up the "weaker regrowth" penalty from anything already regrown at the earlier stage, not just future ones.

**No colony-wide limit.** Unlike Phoenix, this tattoo isn't capped — we had ten colonists wearing it at once and confirmed the game still offers it to an eleventh without any pushback.

**Persistence and compatibility.** Confirmed a colonist's progress, active regrowths, and everything else about this tattoo survive a save and reload correctly. Also confirmed the mod behaves identically with Combat Extended fully installed and with it completely removed, and ran additional regression testing across the rest of the tattoo roster with no issues found.

## Issues we found (and fixed)

- One of our own testing tools didn't refresh a colonist's "actively regrowing" status after force-finishing a regrowth, which could make it look like the discomfort from regrowing was lingering after it was actually done. Fixed — cosmetic to testing only, not something that could happen in normal play.
- The biggest find: installing or removing a full prosthetic *arm* or *leg* attaches at the shoulder or hip, not at the arm or leg itself — which meant both our new warning and the regrowth check itself were quietly missing this, the single most common real case of fitting someone with a full limb replacement. Fixed once, in the one place both checks share, so a full-limb prosthetic is now recognized correctly everywhere.
- Bumped the visibility of the new prosthetic warning after review — it's now a more prominent alert, not an easy-to-miss default one.

None of these were visible without deliberate, hands-on testing, which is exactly what today was for.

## Where this leaves Shedscale

The mechanics are done. Every part of the design has now been confirmed working in-game: automatic regrowth at both tiers including organs, the prosthetic-block warning and its "start over fresh" behavior, the stacking hunger-and-pain cost alongside the flat mood penalty, the malnutrition pause, both paths to the permanent upgrade and its retroactive cleanup, the lack of any colony-wide limit, full save-and-reload safety, and clean coexistence with the rest of the roster and with Combat Extended both present and absent. The tattoo's artwork has also been added. No test-only shortcuts were left behind — everything ran through permanent testing tools rather than temporarily changed numbers, so there was nothing to put back afterward, and the mod rebuilds with zero errors.

What's left is balance, not mechanics. Like the rest of the roster, Shedscale is still running on placeholder numbers — the regrowth time per tier, how much weaker an early regrowth is, the day/part-count thresholds for the upgrade, and the hunger-and-pain scaling are all still placeholders. Tuning that for how it should actually feel in a real playthrough is the next step, alongside the same balance pass planned for everything else.

Shedscale — the 15th tattoo — is feature-complete.
