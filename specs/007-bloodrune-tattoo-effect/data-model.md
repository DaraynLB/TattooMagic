# Phase 1 Data Model: Bloodrune Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain, feature 002's `TattooEffectValues` /
`TattooTierProgress`, and feature 004's `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos`, all reused
unmodified (spec FR-009/FR-011/FR-012). Feature 006's tattoo-mastery gizmo-wrap hook requires no change either —
it already wraps every `Command_Action` collected from any `IProvidesTattooGizmo` comp, including this one, the
moment `HediffComp_BloodruneEffect` starts returning one (spec FR-016). No existing shared file beyond the
already-inert `Bloodrune.xml` Defs is modified.

## Entities

### `HediffCompProperties_BloodruneEffect`

XML-facing properties on `Defs/HediffDefs/Tattoos/Bloodrune.xml`'s comp list — every field here is only ever read
as an `xmlDefault` fallback through `TattooEffectValues.Get`, never directly at a decision point (FR-010).

| Field | Type | Meaning |
|---|---|---|
| `totalHealAmountTier1` / `totalHealAmountTier2` | `float` | Total HP restored across the whole burst, distributed over its duration (research.md R2). |
| `burstDurationTicksTier1` / `burstDurationTicksTier2` | `int` | How long, from activation, healing continues (and, Tier 2 only, pain reduction remains active). |
| `healIntervalTicks` | `int` | How often, in ticks, a portion of the total heal amount is applied during an active burst (research.md R2) — not tiered, since it's a mechanism constant, not a magnitude. |
| `cooldownDurationTicksTier1` / `cooldownDurationTicksTier2` | `int` | Ticks after activation before the gizmo can be used again. |
| `painOffsetTier2` | `float` | Tier-2-only pain reduction magnitude (positive user-facing value; applied as a negative `PainOffset`, research.md R3). No Tier 1 equivalent — Tier 1 grants no pain reduction at all (spec FR-006). |
| `tier2Threshold` | `int` | Activation count at which Tier 1 auto-upgrades to Tier 2 (PRD §5.4 pattern, same shape as every prior tiered tattoo's own threshold field). |
| `abilityIconPath` | `string` | Texture path for the gizmo icon; falls back to a placeholder if unset, same as Guardian's Call's/Stormlash's `abilityIconPath`. |

Tier 1 and Tier 2 magnitude/duration fields are independent tunables, matching the "same effect, stronger at
Tier 2" shape every prior tiered tattoo uses.

### `HediffComp_BloodruneEffect`

The new tattoo-specific comp, attached via `TattooMagic_Hediff_Bloodrune`'s `<comps>` list. Implements
`IProvidesTattooGizmo` (reused, feature 004).

| Field | Type | Persistence | Meaning |
|---|---|---|---|
| `tierState` | `TattooTierProgress` (reused, unmodified) | `ExposeData()` from `CompExposeData()` | Tier + activation-count progression, identical mechanism to every prior tiered tattoo. |
| `cooldownEndTick` | `int` | `Scribe_Values.Look` | Game tick after which the gizmo is usable again. |
| `burstEndTick` | `int` | `Scribe_Values.Look` | Game tick after which this activation's heal burst and (Tier 2) pain reduction both end. |
| `nextHealTick` | `int` | `Scribe_Values.Look` | Game tick at which the next periodic heal application occurs, while a burst is active. |
| `healRemaining` | `float` | `Scribe_Values.Look` | HP still to be applied from the current burst's total heal budget, decremented as `HealTick()` applies portions of it. |

Behavior:

- `GetGizmo()` → builds and returns one `Command_Action` each call (icon, label "Bloodrune", `action =>
  TryActivate()`, `disabled`/`disabledReason` computed live from `cooldownEndTick`) — same shape as
  `HediffComp_GuardiansCallEffect.GetGizmo()`/`HediffComp_StormlashEffect.GetGizmo()`.
- `TryActivate()` → no-ops if still on cooldown (defensive; the gizmo itself should already prevent this per
  Edge Cases); otherwise sets `cooldownEndTick`/`burstEndTick` from accessor-resolved values for the comp's
  current tier, resets `healRemaining` to the tier-appropriate `TotalHealAmount`, schedules `nextHealTick`, and
  calls `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)` exactly once
  per activation (FR-003). Like Stormlash, there is no registry to update and no target-search side effect —
  the entire effect is self-contained state on the wearer's own comp.
- `CompPostTick(ref float severityAdjustment)` → no-ops once `Find.TickManager.TicksGame >= burstEndTick`;
  otherwise, once `TicksGame >= nextHealTick` and `healRemaining > 0f`, calls `HealTick()` and advances
  `nextHealTick` by `HealIntervalTicks` (research.md R2).
- `HealTick()` → finds the wearer's currently most-severe open `Hediff_Injury` (via `Pawn.health.hediffSet`);
  if none exists, does nothing this interval (User Story 1, Acceptance Scenario 5); otherwise calls that
  injury's own `Heal(float)` with `Mathf.Min(healRemaining, healPerInterval)` and decrements `healRemaining` by
  the same amount, moving to the next-most-severe injury once one is fully closed.
- `CurrentPainOffset` (`float`, computed property, not persisted) → returns the accessor-resolved
  `PainOffsetTier2` value, negated, only when `tierState.tier >= 2 && Find.TickManager.TicksGame <
  burstEndTick`; `0f` otherwise (FR-006). Read by `Hediff_BloodruneEffect.PainOffset`.
- `CompExposeData()` → `base.CompExposeData()`, then `tierState.ExposeData()` and the `Scribe_Values.Look` calls
  above (FR-007).

### `Hediff_BloodruneEffect` (new, `Source/Hediffs/`)

```csharp
public class Hediff_BloodruneEffect : HediffWithComps
{
    public override float PainOffset => TryGetComp<HediffComp_BloodruneEffect>()?.CurrentPainOffset ?? 0f;
}
```

The `hediffClass` for `TattooMagic_Hediff_Bloodrune`, replacing plain `HediffWithComps`. The only reason this
subclass exists is to override `PainOffset` — every other behavior (gizmo, cooldown, tier state) is unchanged
comp behavior identical to how Guardian's Call's and Stormlash's plain `HediffWithComps` Hediffs work. See
`contracts/pain-offset-contract.md` §1.

### XML: `Defs/HediffDefs/Tattoos/Bloodrune.xml` (updated)

`hediffClass` changes from `HediffWithComps` to `TattooMagic.Hediff_BloodruneEffect`; adds a `<comps>` entry:

```xml
<hediffClass>TattooMagic.Hediff_BloodruneEffect</hediffClass>
...
<comps>
  <li Class="TattooMagic.HediffCompProperties_BloodruneEffect">
    <totalHealAmountTier1>12</totalHealAmountTier1>
    <totalHealAmountTier2>24</totalHealAmountTier2>
    <burstDurationTicksTier1>180</burstDurationTicksTier1>
    <burstDurationTicksTier2>300</burstDurationTicksTier2>
    <healIntervalTicks>30</healIntervalTicks>
    <cooldownDurationTicksTier1>1800</cooldownDurationTicksTier1>
    <cooldownDurationTicksTier2>1800</cooldownDurationTicksTier2>
    <painOffsetTier2>0.15</painOffsetTier2>
    <tier2Threshold>15</tier2Threshold>
    <abilityIconPath>UI/Commands/Bloodrune</abilityIconPath>
  </li>
</comps>
```

All values placeholders pending PRD §9's balance pass (Constitution Principle III), consistent with every tattoo
shipped so far; small/fast-to-test rather than "plausible-looking" per feature 006 research.md R8 precedent.

## State Transitions

Same tier-transition shape as every prior tiered tattoo (`TattooTierProgress`, unmodified): `tier` starts at
`1`, `progressionCounter` increments once per ability activation, flips to `tier = 2` exactly once when
`progressionCounter` reaches the accessor-resolved `Tier2Threshold` — idempotent, deterministic, Scribe-backed
(Constitution Principle V).

New to this feature: a per-activation, time-boxed heal-burst window (`burstEndTick`, paired with `healRemaining`
draining over successive `nextHealTick` intervals) independent of tier state, re-derived fresh from the comp's
*current* tier every time `TryActivate()` runs — so an activation made just before the Tier 1→2 upgrade uses
Tier 1's numbers, and the next activation (post-upgrade) uses Tier 2's, with no retroactive change to an
already-running burst (same shape as Guardian's Call's `tauntEndTick`/Stormlash's `boostEndTick`). A second
activation before a prior burst finishes overwrites `burstEndTick`/`healRemaining`/`nextHealTick` outright
(refresh, not stack), consistent with those same two tattoos' precedent. `CurrentPainOffset` derives its answer
from this same tier + `burstEndTick` state, so there is no separate persisted state for the pain mechanism.
