# Phase 1 Data Model: Vampiric Thorn Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain and feature 002's `TattooEffectValues` /
`TattooTierProgress`, both reused unmodified (research.md R1). Adds one new reuse point
(`IOnMeleeHitLandedTattooEffect`, research.md R2-R4) as an additive change to the existing shared
`Patch_Pawn_PostApplyDamage_TattooOnHit`, and one new shared healing helper (`TattooHealingUtility`, research.md
R5) that both this feature and Bloodrune's existing comp now call.

## IOnMeleeHitLandedTattooEffect (new interface)

The melee-attacker-side reuse point (research.md R2-R4), the melee counterpart to feature 003's
`IOnRangedHitLandedTattooEffect`.

| Member | Notes |
|---|---|
| `void OnMeleeHitLanded(Thing target, DamageInfo dinfo, float damageDealt, bool killedTarget)` | Implemented by any `HediffComp` that reacts to its wearer's own melee attack landing on something; consumed by the updated `Patch_Pawn_PostApplyDamage_TattooOnHit`'s new second clause inside the existing melee branch. `killedTarget` is `true` only when this specific hit was the one that killed `target` (research.md R4); `target` is always a `Pawn` in practice for this dispatch site, typed as `Thing` for signature symmetry with `IOnRangedHitLandedTattooEffect` (research.md R3). |

**Guarantee**: Called at most once per qualifying landed melee hit, after `IOnMeleeHitTattooEffect.OnMeleeHitTaken`
has already been dispatched to the struck pawn's own comps for the same hit (both fire from the same postfix,
same guard). Never called for a miss, a fully-absorbed (zero-damage) hit, a ranged hit, or a hit whose instigator
isn't the pawn holding this comp. `killedTarget` reflects `target.Dead` read synchronously after
`Pawn.PostApplyDamage`'s own internal death-transition call chain has already completed (research.md R4) — no
polling or delay is involved.

**What is NOT guaranteed**: call order between multiple tattoos' comps implementing this interface on the same
pawn, same caveat as every existing reuse point in this contract family.

## TattooHealingUtility (new static helper, `Source/Effects/`)

The shared wound-healing approach this feature and Bloodrune's existing effect both use (research.md R5) —
satisfies FR-014's "reuse... rather than a second, parallel mechanism."

| Member | Notes |
|---|---|
| `static float HealWorstInjury(Pawn pawn, float amount)` | Heals the pawn's currently most-severe open, non-permanent `Hediff_Injury` (`Severity > 0f`) by up to `amount`, rolling any leftover onto the next-most-severe injury within the same call, repeating until `amount` is exhausted or there is nothing left to heal. Returns the amount actually applied (may be less than `amount`). No-ops and returns `0f` (not an error) if the pawn has no open injuries. The `Severity > 0f` selection guard is load-bearing, not cosmetic: without it, a fully-closed injury (not yet removed from `hediffSet` — that happens on a later health tick, not synchronously inside `Heal()`) could be re-selected forever with a `healAmount` of `0`, hanging the game (found live during T008 testing, research.md R5 hotfix note). |

`HediffComp_BloodruneEffect.HealTick()` is updated to call `TattooHealingUtility.HealWorstInjury(Pawn,
healPerInterval)` instead of its own inline scan; its own persisted fields, `CompPostTick` cadence, and external
behavior are otherwise unchanged (research.md R5).

## HediffCompProperties_VampiricThornEffect / HediffComp_VampiricThornEffect

Attached to `TattooMagic_Hediff_VampiricThorn` (the existing stub Hediff from feature 001, currently with no
`<comps>` block at all). Composes a `TattooTierProgress` (feature 002, unmodified) and implements the new
`IOnMeleeHitLandedTattooEffect`.

| Field (XML, on the props class) | Type | Notes |
|---|---|---|
| `lifestealAmountTier1` / `lifestealAmountTier2` | float | HP healed instantly on every qualifying landed melee hit (FR-001/FR-004), via `TattooHealingUtility.HealWorstInjury` |
| `postKillHealAmountTier2` | float | Tier 2 only (FR-005) — total additional HP restored over the post-kill recovery window, on top of that hit's own per-hit lifesteal; no Tier 1 equivalent |
| `postKillHealDurationTicksTier2` | int | Tier 2 only — how long, from the killing blow, the post-kill recovery window remains open |
| `postKillHealIntervalTicks` | int | How often, in ticks, a portion of the post-kill total is applied while the window is open — a mechanism constant, not a tiered magnitude, same shape as Bloodrune's `healIntervalTicks` |
| `tier2Threshold` | int | Qualifying-event count (melee hits landed) needed to promote Tier 1 → Tier 2 |

All fields above are read exclusively through `TattooEffectValues.Get(...)` at point of use (FR-012); the XML
value is only ever consulted as that call's `xmlDefault` argument.

| Field (runtime, on the comp) | Type | Persistence | Notes |
|---|---|---|---|
| `tierState` | `TattooTierProgress` | `ExposeData()` from `CompExposeData()` | `tier` is `1` or `2`; `progressionCounter` counts qualifying "melee hit landed" events (every landed hit, not gated by any proc chance — Vampiric Thorn has none) |
| `postKillWindowEndTick` | int | `Scribe_Values.Look` | Tier 2 only — game tick after which the current post-kill recovery window closes; `0`/default when no window is active |
| `postKillHealRemaining` | float | `Scribe_Values.Look` | HP still to be applied from the current post-kill window's total heal budget |
| `nextPostKillHealTick` | int | `Scribe_Values.Look` | Game tick at which the next periodic post-kill heal application occurs, while a window is active |

**Behavior**:
- `OnMeleeHitLanded(Thing target, DamageInfo dinfo, float damageDealt, bool killedTarget)`:
  1. If `Pawn == null || Pawn.Dead`, return immediately (edge case: the wearer died in the same resolution as
     their own attack — nothing further applies).
  2. Instant per-hit lifesteal (FR-001): `TattooHealingUtility.HealWorstInjury(Pawn, lifestealAmount)`, where
     `lifestealAmount` is the tier-appropriate accessor-resolved value. No-ops silently (FR-010) if the wearer has
     no open injuries — `TattooHealingUtility` already handles this.
  3. `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)` (FR-002/FR-003) —
     every qualifying landed hit counts, at either tier.
  4. If `tierState.tier >= 2 && killedTarget` (FR-005): (re)start the post-kill recovery window — set
     `postKillWindowEndTick = now + duration`, `postKillHealRemaining = postKillHealAmountTier2`,
     `nextPostKillHealTick = now`, all read via the accessor. This unconditionally **overwrites** any
     already-running window rather than stacking (FR-006) — the same refresh-not-stack shape Bloodrune's/Guardian's
     Call's/Stormlash's own time-boxed state already uses.
  5. At Tier 1, `killedTarget` is read but never acts on the post-kill window fields (FR-005's "Tier 1 MUST NOT
     grant this additional post-kill recovery").
- `CompPostTick(ref float severityAdjustment)`: no-ops once `Find.TickManager.TicksGame >= postKillWindowEndTick`;
  otherwise, once `TicksGame >= nextPostKillHealTick && postKillHealRemaining > 0f`, calls
  `TattooHealingUtility.HealWorstInjury(Pawn, Mathf.Min(postKillHealRemaining, healPerInterval))`, decrements
  `postKillHealRemaining` by the amount actually applied, and advances `nextPostKillHealTick` by
  `Props.postKillHealIntervalTicks` — identical shape to `HediffComp_BloodruneEffect.CompPostTick`/`HealTick`.
- `CompExposeData()`: `base.CompExposeData()`, then `tierState.ExposeData()` and the three `Scribe_Values.Look`
  calls above (FR-007).

**Validation rules**:
- `tierState.tier` is never anything other than `1` or `2`; `tierState.progressionCounter` never decreases and
  continues incrementing harmlessly past the Tier 2 threshold (feature 002 precedent, unmodified).
- The comp only acts (FR-008) while attached to a live, applied `TattooMagic_Hediff_VampiricThorn` — removing the
  hediff removes the comp with it, satisfying FR-015 for free via normal Hediff lifecycle (same as every prior
  tattoo); the existing `TattooHediffRemovalGuard`/`Patch_HealthTracker_RemoveHediff_ProtectTattoos` (feature 003
  FR-015) already covers it via the generic `appliedHediff` registry, no Vampiric-Thorn-specific configuration
  needed.
- `OnMeleeHitLanded` is never invoked for misses, fully-deflected (zero-damage) hits, or hits landed by anyone
  other than this comp's own pawn — enforced by the shared patch's guard clauses (FR-009), not by this comp.
- A second killing blow while `postKillWindowEndTick` is still in the future overwrites the window's fields
  outright (step 4 above) rather than stacking two windows (FR-006).

### XML: `Defs/HediffDefs/Tattoos/VampiricThorn.xml` (updated)

`hediffClass` stays `HediffWithComps` (no custom `Hediff` subclass needed — unlike Bloodrune's `PainOffset`
override, nothing here needs a mechanism outside ordinary comp behavior); adds a `<comps>` entry:

```xml
<hediffClass>HediffWithComps</hediffClass>
...
<!-- All numeric values below are placeholders pending the PRD §9 balance pass. They are read
     exclusively through TattooEffectValues at point of use (FR-012), small/fast-to-test rather
     than "plausible-looking" per feature 006 research.md R8 precedent. -->
<comps>
  <li Class="TattooMagic.HediffCompProperties_VampiricThornEffect">
    <lifestealAmountTier1>2</lifestealAmountTier1>
    <lifestealAmountTier2>4</lifestealAmountTier2>
    <postKillHealAmountTier2>10</postKillHealAmountTier2>
    <postKillHealDurationTicksTier2>180</postKillHealDurationTicksTier2>
    <postKillHealIntervalTicks>30</postKillHealIntervalTicks>
    <tier2Threshold>15</tier2Threshold>
  </li>
</comps>
```

All values placeholders pending PRD §9's balance pass (Constitution Principle III) — carried by the XML comment
above, matching the top-of-file comment every previously shipped tattoo's `HediffDef` XML carries without
exception (`FrostSigil.xml`, `SerpentsEye.xml`, `Bloodrune.xml`, `BerserkersMark.xml`, `Wraithstep.xml`), not just
noted in this doc's prose.

## Updated: Patch_Pawn_PostApplyDamage_TattooOnHit (feature 002 file, additive change only)

Adds a second dispatch clause inside the existing melee branch, alongside the existing, untouched
`DispatchMeleeHit` call:

| Branch | Condition | Dispatches to | Interface |
|---|---|---|---|
| Existing (melee, struck pawn, unmodified) | `dinfo.Tool != null` | `__instance`'s (struck pawn's) comps | `IOnMeleeHitTattooEffect.OnMeleeHitTaken` |
| New (melee, attacker) | `dinfo.Tool != null` (same condition, second clause) | `dinfo.Instigator`'s (attacking pawn's) comps, when it `is Pawn` | `IOnMeleeHitLandedTattooEffect.OnMeleeHitLanded`, with `killedTarget = __instance.Dead` |
| Existing (ranged, unmodified) | `dinfo.Tool == null` | `dinfo.Instigator`'s (attacking pawn's) comps, when it `is Pawn` | `IOnRangedHitLandedTattooEffect.OnRangedHitLanded` |

All three share the existing `totalDamageDealt > 0` guard (FR-009) at the top of the postfix. The new clause
credits `TattooTrackerUtility.GetTracker(attacker)?.RegisterMasteryActivation()` once per dispatched notification,
mirroring both existing clauses (feature 006 precedent) — a wearer whose melee hit lands and is also picked up by
this new interface earns one mastery credit for this dispatch, independent of whatever `IOnMeleeHitTattooEffect`
credit the struck pawn's own comps may separately earn.

## Persistence summary (Constitution Principle V)

- `HediffComp_VampiricThornEffect.tierState` (`tier`, `progressionCounter`) — `Scribe_Values`, on the Vampiric
  Thorn hediff, saved/loaded automatically as part of the pawn's health state, identical mechanism to every prior
  tiered tattoo.
- `postKillWindowEndTick`, `postKillHealRemaining`, `nextPostKillHealTick` — `Scribe_Values`, same hediff,
  identical shape to Bloodrune's `burstEndTick`/`healRemaining`/`nextHealTick`; a window in progress at save time
  resumes correctly on load (FR-007), since all three fields are plain absolute-tick/remaining-amount values with
  no derived state to reconstruct.
- `TattooHealingUtility` — stateless; nothing to persist.
- `TattooEffectValues`'s override dictionary — still not persisted by this feature either, same as every prior
  feature.
