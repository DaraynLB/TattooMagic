# TattooMagic — Thunderclap Sigil Tattoo Proposal (Draft, looking for your sign-off)

This is the write-up from our back-and-forth on the stun idea — reusing RimWorld's existing native stun state instead of building a custom one, and Tier 2 adding an AoE on top of the same duration rather than trading duration for radius. Updated per your latest note: instead of overriding stun-immunity outright, Thunderclap now simply can't target anything flagged as a boss. Here's the full picture, plus the name you've approved.

## The idea

**Thunderclap Sigil** stops an enemy cold — whatever they're doing, healing, attacking, running, they stop doing it. Tier 1 hits one target; Tier 2 turns it into an area effect that stops everyone hostile in the radius at once.

## How it works

- **Reuses RimWorld's existing native stunned state** — the same mechanic vanilla uses (e.g. EMP effects) — rather than a custom "frozen mid-action" state. Simpler to build, and behaves the way players already expect a stun to behave.
- **Can't be used on anything flagged as a boss.** You flagged overriding "unstunnable" outright as too game-breaking — good catch, and it turns out vanilla already gives us the right tool for this. Certain vanilla pawns (Biotech's War Queen, Diabolus, and Apocriton; Anomaly's Dreadmeld, among others) carry an `isBoss` flag on their pawn kind specifically to mark them as boss-tier encounters. We'll gate Thunderclap on that flag directly: a boss-flagged pawn simply can't be selected as a target for either tier, the same way vanilla already restricts some of its own abilities from targeting bosses (there's an existing `canTargetBosses` switch abilities can set, and it defaults to off). No custom "is this a boss" logic needed — we piggyback on the flag the game already uses for this exact purpose.
- Against everything else (i.e. every pawn not flagged as a boss), Thunderclap still works even on pawns that would otherwise resist a stun — that part of the original design is unchanged, it's just bosses specifically that are now off-limits.
- **Duration:** same placeholder value at both tiers — Tier 2 doesn't shorten the stun to pay for the wider radius. XML-tunable.
- **Cooldown:** placeholder, XML-tunable.

## Tier 1 — single target

Targeted stun on one enemy.

## Tier 2 — area of effect

Same stun, same duration, now applied to every hostile inside a radius around the target point. **Allies caught in the same radius are excluded** — it only affects enemies. **Any boss-flagged pawn caught in the radius is skipped too**, same as Tier 1.

## Assumptions we're running with (flag any you disagree with)

- Duration, cooldown, and Tier 2's radius are all **placeholders**, tunable in XML without a rebuild.
- "Boss" means whatever the game itself already marks with `isBoss` on the pawn kind — we're not introducing our own separate definition of "boss." Flag it if there's a specific tough-but-not-technically-`isBoss` enemy you'd also want excluded.

## Name

**Thunderclap Sigil** — approved.

## Next steps

Once you've signed off — or told us what to change — we'll write it up as a full spec (`specs/019-thunderclap-sigil-tattoo-effect/`) and build it against placeholder numbers, then get it in front of you working.
