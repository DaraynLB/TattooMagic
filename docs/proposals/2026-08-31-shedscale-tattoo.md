# TattooMagic — Shedscale Tattoo Proposal (Draft, looking for your sign-off)

This is an updated version of this proposal based on your follow-up notes — automatic regrowth over a set number of days instead of a triggered ability with a cooldown, a prosthetic warning that blocks the regrowth timer from starting at all (rather than blocking an instant click), stacking hunger/pain debuffs per limb currently regrowing at once instead of escalating over a single regrowth, and a new mood debuff for the sheer unpleasantness of watching a body part regrow. Here's the full picture as it stands now.

## The idea

**Shedscale** is a tattoo that regrows a colonist's missing body parts — a hand, an arm, a leg, an eye, a lost kidney — automatically, over time, the same way Phoenix automatically works toward reviving a dead colonist rather than needing the player to trigger anything.

The theme is reptilian regeneration — a lizard regrowing a lost tail. That image drives the main drawback (see "The cost" below): what grows back isn't quite as good as what was lost, at least at first.

It pairs naturally with Phoenix. Phoenix brings a dead colonist back; whatever got hacked off in the fight that killed them, Shedscale quietly regrows over the following days without the player having to do anything.

## How it works

No gizmo, no player-triggered ability. The moment a colonist with this tattoo is missing a body part, and that part has no prosthetic or bionic on it, Shedscale automatically starts regrowing it.

- **Tier 1:** takes 7 days per part.
- **Tier 2:** takes 4 days per part.
- When the timer completes, the part is simply back — no player action needed.
- If the colonist is missing **more than one** eligible part, they can all be regrowing **at the same time** — there's no one-at-a-time restriction. That's a deliberate choice on the player's part in practice, though: letting several regrow at once means taking on several stacks of the cost below simultaneously (see "The cost"), so a player who wants to manage that impact can choose to fit a prosthetic to some parts and let the others regrow, staggering the load instead of taking it all at once.

## The prosthetic rule

Regrowth won't start at all on a part that currently has an installed prosthetic, peg leg, or bionic.

- A **warning** flags this — the colonist has something to regrow, but it's blocked by an installed replacement.
- The regrowth timer doesn't tick at all while blocked — no partial credit banked in the background.
- Uninstalling the prosthetic (a normal surgery bill) clears the block, and the full duration — 7 or 4 days depending on tier — starts fresh from that point.

The same rule covers **bionic organs**: a colonist with a bionic lung can't regrow their natural lung until the bionic comes out.

## What it can regrow

Tier-gated, so the upgraded version genuinely does more:

| | **Tier 1** | **Tier 2** |
|---|---|---|
| Body parts | External limbs and extremities — fingers, toes, hands, feet, arms, legs, plus ears, nose, jaw, eyes | Everything from Tier 1, **plus internal organs** — a missing kidney or lung |
| Regrowth time | 7 days per part (placeholder) | 4 days per part (placeholder) |
| Regrowth quality | Comes back at reduced efficiency — see below | Comes back whole, no penalty |

**Never** the brain, spine, neck, torso, or pelvis. Those parts can't be "missing" on a living colonist anyway — losing them means death, which is Phoenix's department, not this tattoo's.

Only **actually-missing** parts. You can't voluntarily amputate a scarred-but-present limb just to regrow a clean one — Shedscale only acts on what's genuinely gone.

## The cost

**Imperfect regrowth (Tier 1 only).** A regrown part comes back weaker than the original — placeholder 75–85% efficiency — as a permanent condition on that part. This is the lizard-tail flavor: what grows back is cartilage, not bone. Reaching **Tier 2 removes this** — not just for future regrowths, but it also clears the penalty from parts the colonist already regrew at Tier 1. Same "fresh start on hitting Tier 2" idea we used for Phoenix.

**Stacking hunger and pain, one stack per active regrowth.** Regrowing a limb is a real biological effort, and it shows for as long as that part is actively regrowing: raised hunger rate and mild pain. If a colonist has two parts regrowing at once, they carry two stacks of this — regrowing everything at once is rougher than staggering it. A **malnourished colonist's regrowth stalls** until they've eaten enough to recover.

**A mood debuff.** Watching your own body part regrow is not a pleasant thing to witness. A colonist with an active regrowth in progress carries a mood penalty for as long as it's happening.

## The upside

Beyond just getting the colonist whole again, regrowing a part **clears the permanent injuries on it** — old scars, a badly set shoulder, a cataract in a regrown eye. The regrown part is brand new. That's the reason a player would ever trade away a perfectly good archotech arm: not for the raw stats, but for a clean slate on that limb.

## Reaching Tier 2

The tattoo upgrades itself in place, like every other tattoo in the roster. Shedscale reaches Tier 2 on whichever of these comes **first**:

- The colonist has worn it for a stretch of days — placeholder 30.
- **Or** the colonist has regrown 3 parts with it.

So a colonist who loses limbs often earns the upgrade fast through use; one who rarely needs it still gets there eventually just by carrying it. Once reached, Tier 2 is permanent.

## Rarity

**No hard colony cap** — unlike Phoenix. Losing a limb is already something you can recover from with prosthetics, so a regrowth tattoo isn't game-warping the way resurrection is. Shedscale stays rare through the cost of its ritual ingredients, not a hard limit on how many colonists can have it.

## Assumptions we're running with (flag any you disagree with)

- **No cap on simultaneous regrowths** — every eligible missing part starts regrowing right away, with the stacking hunger/pain cost as the natural brake, rather than the game forcing one-at-a-time.
- **The mood debuff doesn't stack per limb** — one flat penalty while at least one regrowth is active, rather than getting worse with more parts regrowing at once (unlike hunger/pain, which do stack). Flag if you pictured it stacking too.
- **Bionic organs follow the same uninstall-first rule** as prosthetic limbs.
- All numbers here (regrowth days, efficiency penalty, Tier 2 day count, hunger rate, ingredient cost) are **placeholders**, same as the rest of the roster — real values come in the balance pass, not now.

## Next steps

Once you've signed off on this — or told us what to change — we'll write it up as a full spec (`specs/015-shedscale-tattoo-effect/`) and build it against placeholder numbers, the same process as the other twelve tattoos, then get it in front of you working.
