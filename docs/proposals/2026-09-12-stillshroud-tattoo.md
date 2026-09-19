# TattooMagic — Stillshroud Tattoo Proposal (Draft, looking for your sign-off)

This is the write-up from our back-and-forth on the stasis idea — full invulnerability with no healing either, a duration measured in days instead of seconds so there's actually time to carry the target back to base, a real drawback now baked in so it can't be used to cheese damage for free, and Tier 2 landed as a second independent charge. Here's the full picture, plus the name you've approved, and how it's meant to pair with Soul Tether.

## The idea

**Stillshroud** freezes a single target in time. While active, the target takes zero damage from anything and can't be healed or tended either — they simply stop, for better or worse, until it ends. The intended play pattern is pairing this with Soul Tether: cast Stillshroud on a dying colonist to take them off the clock entirely, then use Soul Tether (or just carry them) back to base, unfreeze them once they're safe, and get them into surgery there.

## How it works

- **Single target only** — no AoE version.
- **Full invulnerability while active** — immune to everything, including things like fire or an explosion on their tile, not just direct attacks.
- **No healing or tending either**, in either direction — the fantasy is "frozen in time," so the target can't be made better *or* worse while it holds.
- **Can still be carried/rescued** to another location while frozen — that's the whole point, since the target is meant to be moved back to base during this window.
- **Duration:** placeholder 3 days, XML-tunable — long enough to genuinely carry someone home rather than just buy a few seconds.
- **Cancelable early** — the caster (or player) can end it manually the moment the target's back at base, rather than being locked into the full duration every time.

## The drawback: cryosleep sickness

Per your note — without some real cost, players would just use this to freeze anyone taking damage and dodge the mechanic entirely. Rather than inventing a new debuff from scratch, we're reusing a hediff the game already has: **Cryptosleep Sickness**, the same aftereffect vanilla applies when a colonist comes out of a cryptosleep casket. It already does exactly what you're describing — nausea, dizziness, reduced consciousness/moving/manipulation — and it clears on its own after a few days, no extra system needed on our end.

- Applied the moment Stillshroud ends (whether it runs out naturally or gets canceled early).
- Same vanilla hediff, same natural fade-out — nothing custom to tune here beyond however severe we set it.

## Tier 2 — a second, independent charge

Tier 1 has one charge: cast it, then wait out the cooldown before casting again. Tier 2 gives the caster **two charges**, and each charge recharges on its own independent timer rather than sharing one cooldown pool.

Concretely, with a 60-minute cooldown per charge:

- Cast on Colonist 1 at t=0 → that charge starts its own 60-minute recharge, back at t=60.
- The *other* charge was never touched, so it's sitting fully available the whole time.
- Cast on Colonist 2 at t=60, using the now-recharged first charge → it starts recharging again, back at t=120.
- The second charge is still fully available (it was never spent), so the caster can immediately cast on Colonist 3 right then too — no need to wait for both charges to line up before either one can fire again.

So at any given moment, "how many casts do I have right now" is just "how many of the two charges aren't currently mid-recharge" — never a shared, both-or-nothing cooldown.

Tier 2 doesn't change how the freeze itself works — same duration, same cryosleep sickness — it purely gives the caster a second independent charge to work with.

## Assumptions we're running with (flag any you disagree with)

- Duration and cooldown are **placeholders**, tunable in XML without a rebuild.
- We're assuming Stillshroud has its own cooldown separate from Soul Tether's, since they're two different tattoos that could in principle be worn by two different colonists, not just used together by the same one. Flag it if you pictured these as a linked pair that must be on the same colonist.
- Applying vanilla's Cryptosleep Sickness at a flat severity regardless of how long the freeze lasted, rather than scaling it with freeze duration. Flag it if you'd rather a longer freeze leave a worse sickness.
- Tier 2's two charges share the **same per-charge cooldown length** as Tier 1's single charge (e.g. both recharge on the same 60-minute placeholder) — Tier 2 adds a second independent charge, not a faster recharge. Flag it if you pictured Tier 2 also recharging faster on top of the extra charge.

## Name

**Stillshroud** — approved.

## Next steps

Once you've signed off — or told us what to change — we'll write it up as a full spec (`specs/018-stillshroud-tattoo-effect/`) and build it against placeholder numbers, then get it in front of you working.
