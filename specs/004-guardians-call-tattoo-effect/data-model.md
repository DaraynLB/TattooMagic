# Phase 1 Data Model: Guardian's Call Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain and feature 002/003's
`TattooEffectValues` / `TattooTierProgress` / `IProvidesTattooStatOffset` / `StatPart_TattooEffectOffset` /
`CombatExtendedInterop`, all reused unmodified (spec FR-010/FR-012).

## Entities

### `HediffCompProperties_GuardiansCallEffect`

XML-facing properties on `Defs/HediffDefs/Tattoos/GuardiansCall.xml`'s `HediffWithComps` comp list — every
field here is only ever read as a `xmlDefault` fallback through `TattooEffectValues.Get`, never directly at a
decision point (FR-011).

| Field | Type | Meaning |
|---|---|---|
| `tauntRangeTier1` / `tauntRangeTier2` | `float` | Radius (cells) within which hostiles are subject to the taunt bias. |
| `tauntDurationTicksTier1` / `tauntDurationTicksTier2` | `int` | How long, from activation, the taunt bias and (Tier 2) the armor buff remain active. |
| `cooldownDurationTicksTier1` / `cooldownDurationTicksTier2` | `int` | Ticks after activation before the gizmo can be used again. |
| `tier2Threshold` | `int` | Activation count at which Tier 1 auto-upgrades to Tier 2 (PRD §5.4 pattern, same shape as `HediffCompProperties_FrostSigilEffect.tier2Threshold`). |
| `armorOffsetTier2` | `float` | Flat offset applied to `ArmorRating_Sharp`/`Blunt`/`Heat` while Tier 2's buff window is open (R6). |
| `abilityIconPath` | `string` | Texture path for the gizmo icon; falls back to a placeholder if unset. |

Tier 1 has no armor offset (Tier 1 has no buff at all, per spec — only Tier 2 grants one); Tier 1 fields for
range/duration/cooldown are independent tunables from Tier 2's, matching the "same effect, stronger at Tier 2"
shape Frost Sigil/Serpent's Eye already use for their own tier1/tier2 field pairs.

### `HediffComp_GuardiansCallEffect`

The new tattoo-specific comp, attached via `TattooMagic_Hediff_GuardiansCall`'s `<comps>` list. Implements
`IProvidesTattooGizmo` (new, R1) and `IProvidesTattooStatOffset` (reused, R6).

| Field | Type | Persistence | Meaning |
|---|---|---|---|
| `tierState` | `TattooTierProgress` (reused, unmodified) | `ExposeData()` from `CompExposeData()` | Tier + activation-count progression, identical mechanism to Frost Sigil/Serpent's Eye. |
| `cooldownEndTick` | `int` | `Scribe_Values.Look` | Game tick after which the gizmo is usable again. |
| `tauntEndTick` | `int` | `Scribe_Values.Look` | Game tick after which this activation's taunt bias and (Tier 2) armor buff both end. |

Behavior:

- `GetGizmo()` → builds and returns one `Command_Action` each call (icon, label "Guardian's Call", `action =>
  TryActivate()`, `disabled`/`disabledReason` computed live from `cooldownEndTick`).
- `TryActivate()` → no-ops if still on cooldown (defensive; the gizmo itself should already prevent this per
  Edge Cases); otherwise sets `cooldownEndTick`/`tauntEndTick` from accessor-resolved values for the comp's
  current tier, registers itself with `GuardiansCallTauntRegistry` (R3), and calls
  `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)` exactly once per
  activation (FR-004).
- `GetStatOffset(StatDef stat)` → returns the accessor-resolved `armorOffsetTier2` for
  `ArmorRating_Sharp`/`Blunt`/`Heat` only while `tierState.tier >= 2` **and** `Find.TickManager.TicksGame <
  tauntEndTick`; `0f` otherwise (including at Tier 1, and at Tier 2 between activations) — FR-006, Acceptance
  Scenario 4.
- `CompExposeData()` → `base.CompExposeData()`, then `tierState.ExposeData()` and the two
  `Scribe_Values.Look` calls above (FR-008).
- Removed from `GuardiansCallTauntRegistry` automatically once its taunt is expired-or-invalid at query time
  (R3) — no explicit `CompPostTick`/removal hook needed; `PostRemoved`/hediff-gone cases are covered the same
  way since the registry check itself re-validates the owning pawn/hediff still exist.

### `IProvidesTattooGizmo` (new reuse point, `Source/Effects/`)

```csharp
public interface IProvidesTattooGizmo
{
    Gizmo GetGizmo();
}
```

Implemented by any `HediffComp` wanting to contribute one gizmo to its pawn. See
`contracts/triggered-tattoo-effect-contract.md` §1.

### `Patch_Pawn_GetGizmos_TattooGizmos` (new shared patch, `Source/Patches/`)

Harmony postfix on `Pawn.GetGizmos()`. Tattoo-agnostic — iterates `pawn.health.hediffSet.hediffs`, and for
every `HediffWithComps` whose comps implement `IProvidesTattooGizmo`, yields `GetGizmo()` alongside vanilla's
own gizmos. See `contracts/triggered-tattoo-effect-contract.md` §1.

### `GuardiansCallTauntRegistry` (new, Guardian's-Call-specific, `Source/Hediffs/`)

```csharp
public static class GuardiansCallTauntRegistry
{
    public static void Register(HediffComp_GuardiansCallEffect comp);
    public static IEnumerable<HediffComp_GuardiansCallEffect> ActiveTaunters(); // lazily prunes expired/invalid entries
}
```

Not part of the reusable contract (research.md R3) — internal bookkeeping the targeting patch consults.

### `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt` (new, vanilla path, `Source/Patches/`)

Harmony postfix on `Verse.AI.AttackTargetFinder.BestAttackTarget`. See research.md R4 and
`contracts/two-path-targeting-contract.md` §1 for the exact override algorithm.

### `Patch_CE_AttackTargetFinder_GuardiansCallTaunt` (new, CE path, `Source/Patches/`)

Reflection-based, imperatively-applied patch following `Patch_CE_ProjectileImpact_TattooAmmoBonus`'s
convention (feature 003), gated behind `CombatExtendedInterop.IsLoaded`. Exact target method resolved and
confirmed during implementation per research.md R5 — this entry exists in the data model as a placeholder for
whatever CE entry point verification identifies, or is reduced to "not needed, R4's postfix already covers CE"
if verification confirms that outcome. Either way, `TattooMagicMain`'s static constructor gains one line
calling this patch's `TryApply(harmony)`, mirroring the existing CE patch's registration.

### XML: `Defs/HediffDefs/Tattoos/GuardiansCall.xml` (updated)

Adds a `<comps>` entry:

```xml
<li Class="TattooMagic.HediffCompProperties_GuardiansCallEffect">
  <tauntRangeTier1>15</tauntRangeTier1>
  <tauntRangeTier2>15</tauntRangeTier2>
  <tauntDurationTicksTier1>300</tauntDurationTicksTier1>
  <tauntDurationTicksTier2>300</tauntDurationTicksTier2>
  <cooldownDurationTicksTier1>3600</cooldownDurationTicksTier1>
  <cooldownDurationTicksTier2>3600</cooldownDurationTicksTier2>
  <tier2Threshold>15</tier2Threshold>
  <armorOffsetTier2>0.3</armorOffsetTier2>
</li>
```

All values placeholders pending PRD §9's balance pass (Constitution Principle III), consistent with Frost
Sigil/Serpent's Eye's own shipped Defs.

## State Transitions

Same tier-transition shape as features 002/003 (`TattooTierProgress`, unmodified): `tier` starts at `1`,
`progressionCounter` increments once per ability activation, flips to `tier = 2` exactly once when
`progressionCounter` reaches the accessor-resolved `Tier2Threshold` — idempotent, deterministic, Scribe-backed
(Constitution Principle V).

New to this feature: a per-activation, time-boxed window (`tauntEndTick`) independent of tier state, re-derived
fresh from the comp's *current* tier every time `TryActivate()` runs — so an activation made just before the
Tier 1→2 upgrade uses Tier 1's numbers, and the next activation (post-upgrade) uses Tier 2's, with no
retroactive change to an already-running taunt.
