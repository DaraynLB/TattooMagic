# TattooMagic — Player's Guide to Editing Tattoo Values

Every number that drives every tattoo in this mod — how strong it is, how often it procs, how fast it upgrades,
how much it costs to apply — lives in plain text files you can open and change yourself. You don't need to know
how to program, and you don't need to rebuild anything. This guide explains where those numbers live, what each
one does, and how to change them safely.

**All numbers in this mod right now are placeholders.** Nothing has been through a real balance pass yet. That
means you can't "break" anything by experimenting — worst case, a tattoo feels too strong, too weak, or too
fast/slow, and you just change the number back or try something else.

## How to edit these files

1. Find your TattooMagic mod folder — wherever RimWorld has it installed (in your Steam Workshop mods, or your
   local `Mods` folder, depending on how you installed it).
2. Inside it, the files you care about live under `Defs\TattooDefs\` and `Defs\HediffDefs\Tattoos\` (plus a
   handful of support files directly under `Defs\HediffDefs\`, and one under `Defs\ThoughtDefs\` for Shedscale's
   mood penalty — each called out alongside the tattoo it belongs to).
3. Open the relevant `.xml` file in a plain text editor — Notepad works fine. Right-click the file → **Open with**
   → **Notepad** if double-clicking opens something else.
4. Find the number you want to change, between its opening and closing tag — e.g. in
   `<tier2Threshold>15</tier2Threshold>`, the `15` is the value; `tier2Threshold` is its name.
5. Change the number, save the file, and **fully close and restart RimWorld** — not just reload a save. RimWorld
   only reads these files once, when it starts up.

### What's safe to change, and what isn't

Every table in this guide lists the fields that are safe, intended tuning knobs — feel free to change any number
in these tables. A few things in each file are **not** balance values and shouldn't be touched unless you know
exactly what you're doing, because changing them can stop the tattoo from working at all:

- `defName` — the tattoo's internal identity. Other files reference this exact name.
- `Class="..."` and `hediffClass` — tells RimWorld which piece of code to run. These must match real code exactly.
- `abilityIconPath` / `iconPath` — which picture shows on the button. Only change this if you're deliberately
  swapping in different artwork.

If you (or an AI agent editing on your behalf) stick to the numbers in this guide's tables, you're on safe
ground.

## Concepts that apply to every tattoo

**Tiers.** Every tattoo starts at Tier 1 the moment it's applied. Each tattoo tracks its own progress counter,
and once that counter reaches its own `tier2Threshold`, the tattoo automatically upgrades to Tier 2 — no new
ritual, no extra cost, no lost slot. What makes the counter go up is different for **triggered** tattoos
(abilities you click) versus **passive** tattoos (always-on effects):

- **Triggered tattoos** (the ones with a clickable ability and a cooldown): the counter goes up once per use,
  every time you activate the ability, whether or not it does anything spectacular that time.
- **Passive tattoos** (always-on, no button to click): the counter goes up on a specific combat event tied to
  that tattoo's own theme — explained individually below, since it's different for each one (e.g. Ironskin Glyph
  counts hits it absorbed; Vampiric Thorn counts melee hits landed).
- **Automatic tattoos** (Phoenix, Shedscale): there's no ability to use and no single triggering event — these
  react entirely on their own to something happening to the wearer (dying, losing a body part), and their own
  path to Tier 2 is explained individually in their own sections below, since neither one uses the simple
  "progress counter reaches a threshold" shape the other two categories share.

**Ticks.** RimWorld's internal clock runs in "ticks" instead of seconds. At normal (1x) game speed, **60 ticks ≈
1 real-world second**. So a `cooldownDurationTicks` of `3600` is about a 1-minute cooldown, and `300` is about 5
seconds. Playing at faster game speeds just makes everything feel quicker in real time — the tick numbers
themselves don't change.

**Chances and percentages.** Any field with "Chance" in its name is a probability between `0` and `1` — `0.15`
means a 15% chance, `1.0` would mean it always happens, `0` would mean it never does.

**Sign direction — read this carefully.** For almost every tattoo, a *bigger* number means a *bigger* bonus —
more resistance, more armor, more damage, a bigger heal. **Starlight Ward is the one exception.** Its resistance
numbers are stored as *negative* values on purpose, because they lower a stat where lower is better (see its own
section below). For every other tattoo, don't overthink it — bigger number, bigger effect.

## Applying a tattoo: ingredients and cost

Separate from the effect itself, each tattoo has its own **`Defs\TattooDefs\<Name>.xml`** file controlling what
it costs to apply at the ritual station — a different file from the one controlling what the tattoo actually
*does* (that's in `Defs\HediffDefs\Tattoos\<Name>.xml`, covered below). The original 11 tattoos all use the exact
same recipe:

| Field | What it controls | Right now |
|---|---|---|
| `workAmount` | How much work the ritual takes to perform. | 600 |
| Ingredient: `MedicineHerbal` count | How many herbal medicine are consumed. | 5 |
| Ingredient: `Silver` count | How much silver is consumed. | 10 |

**Phoenix and Shedscale cost more silver** — 25 and 15 respectively, instead of 10 — reflecting how much rarer
their effects are meant to be. `workAmount` and the herbal medicine count are unchanged for both. Every other
tattoo added in the future should be assumed to set its own cost rather than automatically matching the
original 11.

To change a tattoo's cost, open its file under `Defs\TattooDefs\`, find the `<ingredients>` block, and change the
`<count>` numbers, or the `<workAmount>` value near the top.

## Tattoo slots: how a colonist earns more

Every colonist starts with 1 tattoo slot. They earn 2 more by using triggered-ability tattoos (one "mastery
point" per activation) and, faster, through passive tattoos triggering their own combat events (each qualifying
event — like a landed melee hit while wearing Vampiric Thorn — also adds mastery points, without needing a
cooldown to reset first). These numbers live in **`Defs\HediffDefs\TattooTracker.xml`**:

| Field | What it controls | Right now |
|---|---|---|
| `slot2Threshold` | Mastery points needed to unlock the 2nd tattoo slot. | 3 |
| `slot3Threshold` | Mastery points needed to unlock the 3rd tattoo slot. | 8 |

**Phoenix and Shedscale don't feed this counter at all.** Their own automatic mechanics (revival, cauterizing,
regrowth) aren't a click or a combat event this system watches for, so wearing either one contributes zero
mastery points on its own. A colonist wearing one of these *and* a triggered or passive tattoo still earns slots
normally from that second tattoo's own activity.

---

## The 13 tattoos

Each section below covers one tattoo's file at `Defs\HediffDefs\Tattoos\<Name>.xml`. Phoenix and Shedscale each
also have one or two extra support files, called out in their own sections.

### Frost Sigil (Passive)

Cold resistance, plus a chance to slow down anyone who hits you in melee.

- **Tier 1**: colder tolerance, and each time an attacker lands a melee hit on you, a chance to slow their move
  speed for a few seconds.
- **Tier 2** (after enough successful slows land): stronger cold resistance, a bigger slow chance and a stronger
  slow, plus a small chance to fully freeze the attacker in place for a moment.
- **Progress counter**: only counts when the slow actually procs — not every hit you take.

| Field | What it controls | Right now |
|---|---|---|
| `coldResistanceTier1` / `Tier2` | How many degrees colder you can comfortably tolerate. | 5 / 10 |
| `slowChanceTier1` / `Tier2` | Chance a melee attacker gets slowed when they hit you. | 15% / 30% |
| `slowMoveSpeedOffsetTier1` / `Tier2` | How much slower the attacker moves when slowed (already negative — a *bigger negative* number is a *bigger* slow). | -1.5 / -2.5 |
| `slowDurationTicksTier1` / `Tier2` | How long the slow lasts. | 180 (3s) / 300 (5s) |
| `freezeChanceTier2` | Tier 2 only — chance, on top of a successful slow, to fully freeze the attacker. | 10% |
| `freezeDurationTicksTier2` | How long the freeze lasts. | 60 (1s) |
| `tier2Threshold` | Successful slow-procs needed to reach Tier 2. | 15 |

### Serpent's Eye (Passive)

Ranged accuracy — or, with Combat Extended installed, steadier aim (less sway/recoil) instead, since CE
redefines what that underlying stat means.

- **Tier 1**: a flat accuracy bonus (or sway/recoil reduction under CE).
- **Tier 2** (after enough landed ranged hits): a bigger version of the same bonus, plus — with CE specifically —
  a bonus to how effective your currently-loaded ammo is.
- **Progress counter**: every ranged hit you land counts, no proc chance involved.

| Field | What it controls | Right now |
|---|---|---|
| `accuracyBonusTier1` / `Tier2` | Accuracy bonus without Combat Extended. | 5% / 10% |
| `swayRecoilBonusTier1` / `Tier2` | Sway/recoil reduction with Combat Extended (same slots, different meaning). | 5% / 10% |
| `ceAmmoEffectivenessMultiplierTier2` | Tier 2 + CE only — this is a *multiplier*, not a bonus percentage: `1.15` = +15% ammo effectiveness, `1.0` would mean no change. | 1.15 |
| `tier2Threshold` | Landed ranged hits needed to reach Tier 2. | 15 |

### Guardian's Call (Triggered)

Click to force every hostile nearby to focus-fire you for a few seconds — a taunt for protecting squishier
colonists.

- **Tier 1**: activate, nearby hostiles target you, then it goes on cooldown.
- **Tier 2** (after enough uses): same taunt, plus a temporary armor boost for as long as the taunt lasts.
- **Progress counter**: one point per activation (click), regardless of how many enemies it actually redirects.

| Field | What it controls | Right now |
|---|---|---|
| `tauntRangeTier1` / `Tier2` | How far away (map tiles) an enemy can be and still get pulled in. | 15 / 15 |
| `tauntDurationTicksTier1` / `Tier2` | How long the taunt lasts once activated. | 300 (5s) / 300 (5s) |
| `cooldownDurationTicksTier1` / `Tier2` | Wait time before you can use it again. | 3600 (60s) / 3600 (60s) |
| `armorOffsetTier2` | Tier 2 only — extra armor (Sharp/Blunt/Heat) while the taunt is active. `0.3` = +30%. | 0.3 |
| `tier2Threshold` | Uses needed to reach Tier 2. | 15 |

*Right now Tier 1 and Tier 2 use identical range/duration/cooldown — only the armor bonus is new at Tier 2. If
you want Tier 2 to feel like more of an upgrade, try shortening the Tier 2 cooldown or extending its range.*

### Stormlash (Triggered)

Click for a burst of extra move speed and attack speed.

- **Tier 1**: activate for a temporary speed boost, then cooldown.
- **Tier 2** (after enough uses): a bigger boost, plus brief immunity to anything that would slow you down while
  it's active (including Combat Extended's own suppression-slow).
- **Progress counter**: one point per activation.

| Field | What it controls | Right now |
|---|---|---|
| `moveSpeedOffsetTier1` / `Tier2` | Extra move speed while boosted. | 1.5 / 2.5 |
| `attackSpeedBonusTier1` / `Tier2` | How much faster attacks come out. `0.15` = 15% faster. | 15% / 30% |
| `boostDurationTicksTier1` / `Tier2` | How long the boost lasts. | 300 (5s) / 300 (5s) |
| `cooldownDurationTicksTier1` / `Tier2` | Wait time before reuse. | 1800 (30s) / 1800 (30s) |
| `tier2Threshold` | Uses needed to reach Tier 2. | 15 |

### Ironskin Glyph (Passive)

Straightforward physical armor — tougher against bullets, blades, and blunt impacts alike.

- **Tier 1**: raises armor rating against both Sharp and Blunt damage, so you take less of it.
- **Tier 2** (after enough hits absorbed): stronger armor, plus a chance to fully shrug off a hit and take zero
  damage from it.
- **Progress counter**: physical hits that had something real to absorb (a hit already reduced to zero by
  something else doesn't count).

| Field | What it controls | Right now |
|---|---|---|
| `armorRatingBonusTier1` / `Tier2` | Armor rating added against Sharp and Blunt damage alike. `0.08` = +8%. | 8% / 16% |
| `fullNegateChanceTier2` | Tier 2 only — chance to fully absorb a physical hit (zero damage taken). | 15% |
| `tier2Threshold` | Qualifying hits needed to reach Tier 2. | 10 |

### Ember Ward (Passive)

The fire/heat counterpart to Ironskin Glyph.

- **Tier 1**: raises how much heat you can comfortably tolerate, and reduces burn/fire damage taken.
- **Tier 2** (after enough qualifying burns): stronger of both, plus a chance to fully ignore a burn instance.
- **Progress counter**: fire/burn instances that had something real to reduce.

| Field | What it controls | Right now |
|---|---|---|
| `heatResistanceTier1` / `Tier2` | How many degrees hotter you can comfortably tolerate. | 5 / 10 |
| `armorRatingHeatTier1` / `Tier2` | Armor rating added specifically against Heat/fire damage. | 8% / 16% |
| `fullIgnoreChanceTier2` | Tier 2 only — chance to fully ignore a burn instance. | 15% |
| `tier2Threshold` | Qualifying burn instances needed to reach Tier 2. | 10 |

### Vampiric Thorn (Passive)

Melee attacks heal you a little.

- **Tier 1**: every landed melee hit heals a flat amount off your worst current wound.
- **Tier 2** (after enough landed hits): heals more per hit, plus a short heal-over-time burst any time you land
  a killing blow.
- **Progress counter**: every landed melee hit counts, no proc chance involved.

| Field | What it controls | Right now |
|---|---|---|
| `lifestealAmountTier1` / `Tier2` | HP healed per landed melee hit. | 2 / 4 |
| `postKillHealAmountTier2` | Tier 2 only — total extra healing granted after a killing blow. | 10 |
| `postKillHealDurationTicksTier2` | How long that post-kill healing window lasts. | 180 (3s) |
| `postKillHealIntervalTicks` | How often (in ticks) the post-kill window ticks out its healing. | 30 (0.5s) |
| `tier2Threshold` | Landed melee hits needed to reach Tier 2. | 15 |

### Berserker's Mark (Triggered)

A risk/reward melee cooldown: hit harder and shrug off more pain, at the cost of your armor while it's active.

- **Tier 1**: activate for a temporary melee damage and pain-threshold boost, but your armor drops for the same
  window, then cooldown.
- **Tier 2** (after enough uses): a bigger damage/pain boost, and the armor penalty gets *smaller* (a better
  trade, not free).
- **Progress counter**: one point per activation.

| Field | What it controls | Right now |
|---|---|---|
| `meleeDamageOffsetTier1` / `Tier2` | Melee damage bonus while active. `0.20` = +20%. | 20% / 40% |
| `painThresholdOffsetTier1` / `Tier2` | How much more pain you can take before going down, while active. | 15% / 15% |
| `defenseOffsetTier1` / `Tier2` | The size of the armor penalty while active — this is written as a positive number meaning "how much you give up"; a *smaller* number here is the improvement. | 15% / 8% |
| `boostDurationTicksTier1` / `Tier2` | How long the boosted window lasts. | 180 (3s) / 180 (3s) |
| `cooldownDurationTicksTier1` / `Tier2` | Wait time before reuse. | 1800 (30s) / 1800 (30s) |
| `tier2Threshold` | Uses needed to reach Tier 2. | 15 |

### Bloodrune (Triggered)

A burst of self-healing on demand.

- **Tier 1**: activate to open a short healing burst, then cooldown.
- **Tier 2** (after enough uses): more total healing over a longer burst, plus reduced pain for as long as the
  burst lasts.
- **Progress counter**: one point per activation.

| Field | What it controls | Right now |
|---|---|---|
| `totalHealAmountTier1` / `Tier2` | Total HP healed across the whole burst. | 12 / 24 |
| `burstDurationTicksTier1` / `Tier2` | How long the burst takes to deliver its healing. | 180 (3s) / 300 (5s) |
| `healIntervalTicks` | How often, within the burst, a heal tick fires (applies at both tiers). | 30 (0.5s) |
| `cooldownDurationTicksTier1` / `Tier2` | Wait time before reuse. | 1800 (30s) / 1800 (30s) |
| `painOffsetTier2` | Tier 2 only — reduces pain for the burst's duration. | 15% |
| `tier2Threshold` | Uses needed to reach Tier 2. | 15 |

### Wraithstep (Triggered)

Click, then pick a nearby spot to instantly blink there — an escape/repositioning tool.

- **Tier 1**: activate, choose a destination within range, teleport there, then cooldown.
- **Tier 2** (after enough uses): a longer blink range, plus a brief window right after landing where nothing can
  target you at all.
- **Progress counter**: one point per *successful* blink (cancelling the targeting doesn't count).

| Field | What it controls | Right now |
|---|---|---|
| `rangeTier1` / `Tier2` | How far (map tiles) you can blink. | 6 / 10 |
| `cooldownDurationTicksTier1` / `Tier2` | Wait time before reuse. | 1200 (20s) / 1200 (20s) |
| `untargetableWindowTicksTier2` | Tier 2 only — how long you're untargetable right after landing. | 90 (1.5s) |
| `tier2Threshold` | Successful blinks needed to reach Tier 2. | 15 |

### Starlight Ward (Passive)

Mental resilience — harder to break, less affected by psychic effects.

- **Tier 1**: lowers your mental-break threshold (so you can take a worse mood before risking a breakdown) and
  lowers your psychic sensitivity (less affected by negative psychic effects).
- **Tier 2** (after enough resisted close calls): stronger versions of both, plus a small, always-on mood buff.
- **Progress counter**: counts every time your mood dips into risky territory *and then recovers without an
  actual mental break happening* — not every tick spent unhappy, and a real break resets that particular close
  call without counting it.

| Field | What it controls | Right now |
|---|---|---|
| `mentalBreakResistanceTier1` / `Tier2` | **Written as a negative number on purpose.** Makes the mood-level for mental break risk. To make this tattoo *more* resistant, make the number *more negative* (e.g. `-0.06` is stronger than `-0.03`) — that's the opposite direction from every other tattoo in this guide. | -3% / -6% |
| `psychicSensitivityResistanceTier1` / `Tier2` | Also written negative, same rule as above — more negative means more resistant to psychic effects. | -10% / -20% |
| `moodBuffTier2` | Tier 2 only — a flat, always-on mood bonus. This one is a normal positive number (bigger = better). | +3 |
| `tier2Threshold` | Resisted close calls needed to reach Tier 2. | 5 |

### Phoenix (Automatic)

Cheats death. No ability, no button — the moment a wearer actually dies (not merely downed), the tattoo starts
working toward bringing them back on its own.

- **Tier 1**: after a several-day wait, a daily chance (better the fresher the body) to revive the wearer,
  retrying once a day until it succeeds or the body is destroyed/fully decayed. Every attempt, success or
  failure, leaves a burn.
- **Tier 2** (after staying alive long enough while wearing it): a shorter wait before the first attempt, and a
  lighter burn each time.
- **Progress counter**: simply staying alive, day by day, while wearing the tattoo — it resets to zero every time
  the wearer actually dies and comes back, so reaching Tier 2 means a long unbroken stretch of survival.
- Deliberately cremating a Phoenix-tattooed corpse via a crematorium's own bill guarantees the next attempt
  succeeds, regardless of how decomposed the body already is, at the cost of any gear left on it.
- A colonist's 3rd lifetime revival, and every one after it, comes with a temporary near-total paralysis as the
  price of cheating death that many times — lasting longer with each additional occurrence.
- Only 3 colonists in your whole faction can wear this tattoo at once. It also has an entirely separate passive:
  a chance to auto-cauterize a wearer's worst bleeding wound before it becomes fatal, with no connection to the
  revival mechanic at all.

Its numbers live in **`Defs\HediffDefs\Tattoos\Phoenix.xml`** and one support file,
**`Defs\HediffDefs\ParalyticAbasia.xml`**:

| Field | What it controls | Right now |
|---|---|---|
| `waitDaysTier1` / `Tier2` | Days after death before the first revival attempt. | 5 / 3 |
| `decompositionChanceCurve` | A curve, not a single number — each `<li>(days, chance)</li>` point pairs a corpse age with a success chance. Add, remove, or edit points to reshape the odds over time; RimWorld fills in the curve smoothly between them. | (0, 90%) → (1, 60%) → (3, 30%) → (7, 10%) → (14, 2%) |
| `burnSeverityTier1` / `Tier2` | How severe the burn left by each attempt is. | 18 / 8 |
| `abasiaBaseDurationDays` | How long the paralysis lasts the first time it happens to a colonist. | 2 |
| `abasiaDurationIncrementDays` | How many extra days it lasts each time after that. | 1 |
| `tier2DaysAliveThreshold` | Consecutive days alive (while worn) needed to reach Tier 2. | 30 |
| `cauterizeChanceTier1` / `Tier2` | Passive per-check chance to auto-cauterize a bleeding wound. | 15% / 35% |
| `minimumQualifyingBleedRate` | How severe a bleed has to be before the passive will even consider it — trivial scratches are always ignored regardless of this value. | 0.15 |
| `factionCap` | Maximum colonists who may wear this tattoo across your whole faction at once. | 3 |

`Defs\HediffDefs\ParalyticAbasia.xml`'s `capMods` block controls how disabling the paralysis is — how close to
`0` each affected capacity (Moving, Manipulation, Consciousness) is forced down to while it's active. These are
intentionally severe by design; lower them if you want the paralysis to feel less punishing.

### Shedscale (Automatic)

Automatically regrows a wearer's missing body parts over time — no ability, no button, and no ritual needed a
second time. The moment an eligible part is genuinely missing and nothing artificial has been installed in its
place, the tattoo starts regrowing it on its own.

- **Tier 1**: external limbs and extremities (fingers, toes, hands, feet, arms, legs, ears, nose, jaw, eyes)
  regrow over several days each, coming back at reduced efficiency as a permanent condition on that part.
- **Tier 2** (reached by wearing it long enough, or regrowing enough parts — whichever comes first): regrowth
  also covers internal organs (kidneys, lungs), finishes faster, comes back at full efficiency, and **reaches
  back** to remove the efficiency penalty from every part already regrown at Tier 1, not just future ones.
- **Progress counter**: two independent counters race each other — consecutive days worn, and total parts
  regrown. Whichever hits its own threshold first triggers the permanent upgrade.
- Every eligible missing part regrows at the same time — there's no one-at-a-time limit — but a colonist with
  several parts regrowing at once runs hungrier and carries more pain the more are active simultaneously, plus a
  flat mood penalty (that part doesn't get worse no matter how many parts are involved). Malnutrition pauses all
  of a colonist's regrowth entirely until they've eaten enough to recover.
- Fitting a prosthetic or bionic on an eligible part blocks its regrowth entirely for as long as it's installed,
  with no progress quietly banking underneath — removing the replacement restarts that part's regrowth from
  scratch. There is no colony-wide limit on how many colonists may wear this tattoo.

Its numbers live in **`Defs\HediffDefs\Tattoos\Shedscale.xml`** and two support files,
**`Defs\HediffDefs\ShedscaleStrain.xml`** and **`Defs\HediffDefs\ShedscaleImperfectRegrowth.xml`**:

| Field | What it controls | Right now |
|---|---|---|
| `tier1RegrowthDays` / `tier2RegrowthDays` | Days to regrow one part, per tier. | 7 / 4 |
| `tier1EfficiencyFactor` | The regrown part's health fraction while the Tier 1 penalty applies. `0.80` means it comes back at 80% as good as the original. | 0.80 |
| `tier2DaysWornThreshold` | Consecutive days worn needed to reach Tier 2 via the "worn long enough" path. | 30 |
| `tier2PartsRegrownThreshold` | Lifetime parts regrown needed to reach Tier 2 via the "used enough" path. | 3 |
| `tier1EligiblePartDefs` | The list of body-part types eligible at Tier 1. Add or remove entries to change what can regrow. | Finger, Toe, Hand, Foot, Arm, Leg, Ear, Nose, Jaw, Eye |
| `tier2AdditionalEligiblePartDefs` | Extra body-part types that become eligible once Tier 2 is reached, on top of the Tier 1 list. | Kidney, Lung |

`ShedscaleStrain.xml`'s `<stages>` block controls the hunger/pain cost per number of parts regrowing at once —
each stage's `minSeverity` is how many parts must be active for that stage's `hungerRateFactorOffset`/
`painOffset` to apply. `ShedscaleImperfectRegrowth.xml` has no tunable numbers of its own — its severity is
calculated from `tier1EfficiencyFactor` above at the moment each part finishes regrowing. The flat mood penalty
lives in a third file, `Defs\ThoughtDefs\Shedscale.xml`, as `baseMoodEffect` (currently -4).

---

## Using this guide with an AI agent

If you're not comfortable editing XML yourself, you can hand this whole guide, plus the specific `.xml` file(s)
you want changed, to an AI chat assistant and ask it to make the edit for you. Something like:

> "Here's a guide to TattooMagic's tattoo files, and my current `FrostSigil.xml`. I want Frost Sigil's slow
> chance to be much higher at Tier 1 — closer to 40% instead of 15% — and I want Tier 2 to unlock faster, after
> 8 procs instead of 15. Can you show me the edited file, and explain what changed?"

Because every field in this guide is named, described, and given its current value, the assistant doesn't need
access to the mod's actual code to make a correct, safe edit — just this guide and the file itself. Ask it to
give you the complete file back (not just the changed lines) so you can paste the whole thing back in cleanly.
