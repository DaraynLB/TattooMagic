# Phase 1 Data Model: Stormlash Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain, feature 002/003's `TattooEffectValues` /
`TattooTierProgress` / `IProvidesTattooStatOffset` / `CombatExtendedInterop`, and feature 004's
`IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos`, all reused unmodified (spec FR-011/FR-013/FR-014).
`StatPart_TattooEffectOffset` (feature 002) gains one small, still-generic extension (see below) rather than
being reused unmodified.

## Entities

### `HediffCompProperties_StormlashEffect`

XML-facing properties on `Defs/HediffDefs/Tattoos/Stormlash.xml`'s `HediffWithComps` comp list — every field
here is only ever read as an `xmlDefault` fallback through `TattooEffectValues.Get`, never directly at a
decision point (FR-012).

| Field | Type | Meaning |
|---|---|---|
| `moveSpeedOffsetTier1` / `moveSpeedOffsetTier2` | `float` | Flat `MoveSpeed` offset granted for the boost's duration. |
| `attackSpeedBonusTier1` / `attackSpeedBonusTier2` | `float` | Positive user-facing magnitude (e.g. `0.15` = 15% faster); negated internally when offsetting `MeleeCooldownFactor`/`RangedCooldownFactor` (research.md R2). |
| `boostDurationTicksTier1` / `boostDurationTicksTier2` | `int` | How long, from activation, the speed/attack-speed boost (and, Tier 2 only, the slow immunity) remain active. |
| `cooldownDurationTicksTier1` / `cooldownDurationTicksTier2` | `int` | Ticks after activation before the gizmo can be used again. |
| `tier2Threshold` | `int` | Activation count at which Tier 1 auto-upgrades to Tier 2 (PRD §5.4 pattern, same shape as `HediffCompProperties_GuardiansCallEffect.tier2Threshold`). |
| `abilityIconPath` | `string` | Texture path for the gizmo icon; falls back to a placeholder if unset, same as Guardian's Call's `abilityIconPath`. |

Tier 1 and Tier 2 fields are independent tunables, matching the "same effect, stronger at Tier 2" shape every
prior tiered tattoo uses.

### `HediffComp_StormlashEffect`

The new tattoo-specific comp, attached via `TattooMagic_Hediff_Stormlash`'s `<comps>` list. Implements
`IProvidesTattooGizmo` (reused, feature 004), `IProvidesTattooStatOffset` (reused, feature 002), and
`IGrantsStatOffsetImmunity` (new, research.md R5).

| Field | Type | Persistence | Meaning |
|---|---|---|---|
| `tierState` | `TattooTierProgress` (reused, unmodified) | `ExposeData()` from `CompExposeData()` | Tier + activation-count progression, identical mechanism to every prior tiered tattoo. |
| `cooldownEndTick` | `int` | `Scribe_Values.Look` | Game tick after which the gizmo is usable again. |
| `boostEndTick` | `int` | `Scribe_Values.Look` | Game tick after which this activation's speed/attack-speed boost and (Tier 2) slow immunity both end. |

Behavior:

- `GetGizmo()` → builds and returns one `Command_Action` each call (icon, label "Stormlash", `action =>
  TryActivate()`, `disabled`/`disabledReason` computed live from `cooldownEndTick`) — same shape as
  `HediffComp_GuardiansCallEffect.GetGizmo()`.
- `TryActivate()` → no-ops if still on cooldown (defensive; the gizmo itself should already prevent this per
  Edge Cases); otherwise sets `cooldownEndTick`/`boostEndTick` from accessor-resolved values for the comp's
  current tier and calls `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)`
  exactly once per activation (FR-004). Unlike Guardian's Call, there is no registry to update and no
  target-search side effect — the entire effect is self-contained stat state on the wearer's own comp.
- `GetStatOffset(StatDef stat)` → returns `0f` if `Find.TickManager.TicksGame >= boostEndTick` (no active
  boost); otherwise returns the accessor-resolved, tier-appropriate positive offset for `StatDefOf.MoveSpeed`,
  or the accessor-resolved, tier-appropriate **negated** magnitude for `StatDefOf.MeleeCooldownFactor` /
  `StatDefOf.RangedCooldownFactor`; `0f` for any other stat (FR-001, FR-006, FR-008).
- `BlocksNegativeOffsets(StatDef stat)` → `true` only when `stat == StatDefOf.MoveSpeed` **and**
  `tierState.tier >= 2` **and** `Find.TickManager.TicksGame < boostEndTick`; `false` otherwise, including at
  Tier 1 and at Tier 2 between activations (FR-007).
- `CompExposeData()` → `base.CompExposeData()`, then `tierState.ExposeData()` and the two `Scribe_Values.Look`
  calls above (FR-009).

### `IGrantsStatOffsetImmunity` (new reuse point, `Source/Effects/`)

```csharp
public interface IGrantsStatOffsetImmunity
{
    bool BlocksNegativeOffsets(StatDef stat);
}
```

Implemented by any `HediffComp` that wants to veto *other* comps' negative contributions to a specific stat
while some condition of its own holds. Does not affect its own contribution (still governed by
`IProvidesTattooStatOffset`), or any positive contribution from any comp. See
`contracts/stat-offset-immunity-contract.md` §1.

### `StatPart_TattooEffectOffset` (feature 002, updated)

`GetOffset(StatRequest req)` gains a small, still-generic two-pass shape:

1. Walk the pawn's hediff comps once to determine whether *any* comp implementing `IGrantsStatOffsetImmunity`
   currently returns `true` from `BlocksNegativeOffsets(parentStat)`.
2. Walk the pawn's `IProvidesTattooStatOffset` comps (as before) to sum contributions to `parentStat` — but if
   step 1 found an active veto, clamp each individual comp's own `GetStatOffset(parentStat)` to
   `Mathf.Max(0f, offset)` before adding, instead of summing the raw value.

No change to `ExplanationPart` beyond it continuing to reflect whatever `GetOffset` returns. The class remains
tattoo-agnostic — it never references `HediffComp_StormlashEffect` or `HediffComp_FrostSigilEffect`/
`HediffComp_FrostSigilSlow` by name, only the two interfaces. See `contracts/stat-offset-immunity-contract.md`
§2.

### `TattooEffectStatPartInstaller` (feature 002, updated)

Two new `Register(...)` calls, alongside the existing six: `StatDefOf.MeleeCooldownFactor`,
`StatDefOf.RangedCooldownFactor` (research.md R2). No change to any existing registration.

### `Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity` (new, CE-only, `Source/Patches/`)

Reflection-based, imperatively-applied patch following `Patch_CE_ProjectileImpact_TattooAmmoBonus`'s convention
(feature 003), gated behind `CombatExtendedInterop.IsLoaded`. Postfixes
`CombatExtended.CompSuppressable`'s `IsCrouchWalking` property getter (`AccessTools.PropertyGetter`); forces
`__result = false` when the comp's owning pawn currently has an active Tier 2 Stormlash boost (same "is there a
Tier 2 comp with an unexpired `boostEndTick`" check `HediffComp_StormlashEffect` itself uses). See research.md
R4 and `contracts/stat-offset-immunity-contract.md` §3 for why this — rather than a bigger `MoveSpeed`
offset — is required for genuine immunity, and for the exact CE internals this patch targets.

### XML: `Defs/HediffDefs/Tattoos/Stormlash.xml` (updated)

Adds a `<comps>` entry:

```xml
<li Class="TattooMagic.HediffCompProperties_StormlashEffect">
  <moveSpeedOffsetTier1>1.5</moveSpeedOffsetTier1>
  <moveSpeedOffsetTier2>2.5</moveSpeedOffsetTier2>
  <attackSpeedBonusTier1>0.15</attackSpeedBonusTier1>
  <attackSpeedBonusTier2>0.3</attackSpeedBonusTier2>
  <boostDurationTicksTier1>300</boostDurationTicksTier1>
  <boostDurationTicksTier2>300</boostDurationTicksTier2>
  <cooldownDurationTicksTier1>1800</cooldownDurationTicksTier1>
  <cooldownDurationTicksTier2>1800</cooldownDurationTicksTier2>
  <tier2Threshold>15</tier2Threshold>
</li>
```

All values placeholders pending PRD §9's balance pass (Constitution Principle III), consistent with every
tattoo shipped so far.

## State Transitions

Same tier-transition shape as every prior tiered tattoo (`TattooTierProgress`, unmodified): `tier` starts at
`1`, `progressionCounter` increments once per ability activation, flips to `tier = 2` exactly once when
`progressionCounter` reaches the accessor-resolved `Tier2Threshold` — idempotent, deterministic, Scribe-backed
(Constitution Principle V).

New to this feature: a per-activation, time-boxed window (`boostEndTick`) independent of tier state, re-derived
fresh from the comp's *current* tier every time `TryActivate()` runs — so an activation made just before the
Tier 1→2 upgrade uses Tier 1's numbers, and the next activation (post-upgrade) uses Tier 2's, with no
retroactive change to an already-running boost (same shape as Guardian's Call's `tauntEndTick`, feature 004).
`BlocksNegativeOffsets` and the CE `IsCrouchWalking` postfix both derive their answer from this same tier +
`boostEndTick` state, so there is no separate immunity-specific state to persist or desync.
