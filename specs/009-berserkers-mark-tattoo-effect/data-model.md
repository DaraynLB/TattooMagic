# Phase 1 Data Model: Berserker's Mark Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain, feature 002's `TattooEffectValues` /
`TattooTierProgress` / `IProvidesTattooStatOffset` / `StatPart_TattooEffectOffset`, and feature 004's
`IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos`, all reused unmodified (spec FR-012/FR-014/FR-015).
Feature 006's tattoo-mastery gizmo-wrap hook requires no change either — it already wraps every `Command_Action`
collected from any `IProvidesTattooGizmo` comp, including this one, the moment `HediffComp_BerserkersMarkEffect`
starts returning one (spec FR-019). No existing shared file is modified beyond `TattooEffectStatPartInstaller`
(two new `Register(...)` calls) and the already-inert `BerserkersMark.xml` Defs.

## Entities

### `HediffCompProperties_BerserkersMarkEffect`

XML-facing properties on `Defs/HediffDefs/Tattoos/BerserkersMark.xml`'s comp list — every field here is only
ever read as an `xmlDefault` fallback through `TattooEffectValues.Get`, never directly at a decision point
(FR-013).

| Field | Type | Meaning |
|---|---|---|
| `meleeDamageOffsetTier1` / `meleeDamageOffsetTier2` | `float` | Additive offset to `StatDefOf.MeleeDamageFactor` while a window is active (research.md R2). Tier 2 MUST be larger than Tier 1 (FR-006). |
| `painThresholdOffsetTier1` / `painThresholdOffsetTier2` | `float` | Additive offset to `StatDefOf.PainShockThreshold` while a window is active (research.md R3). Present at both tiers — the spec's Tier 2 change is scoped to damage/defense, not pain threshold (FR-006/FR-007; pain-threshold magnitude is not required to change at Tier 2, though the XML leaves both tunable independently in case a later balance pass wants that). |
| `defenseOffsetTier1` / `defenseOffsetTier2` | `float` | Positive user-facing magnitude; applied as a *negative* offset to `StatDefOf.ArmorRating_Sharp/Blunt/Heat` while a window is active (research.md R3). Tier 2 MUST be smaller than Tier 1, and non-zero at both tiers (FR-007). |
| `boostDurationTicksTier1` / `boostDurationTicksTier2` | `int` | How long, from activation, the window (all three offsets) stays open. |
| `cooldownDurationTicksTier1` / `cooldownDurationTicksTier2` | `int` | Ticks after activation before the gizmo can be used again. |
| `tier2Threshold` | `int` | Activation count at which Tier 1 auto-upgrades to Tier 2 (PRD §5.4 pattern, same shape as every prior tiered tattoo's own threshold field). |
| `abilityIconPath` | `string` | Texture path for the gizmo icon; falls back to a placeholder if unset, same as every prior triggered tattoo's `abilityIconPath`. |

Tier 1 and Tier 2 magnitude/duration fields are independent tunables, matching the "same effect, stronger (or in
defense's case, gentler) at Tier 2" shape every prior tiered tattoo uses.

### `HediffComp_BerserkersMarkEffect`

The new tattoo-specific comp, attached via `TattooMagic_Hediff_BerserkersMark`'s `<comps>` list. Implements
`IProvidesTattooGizmo` (reused, feature 004) and `IProvidesTattooStatOffset` (reused, feature 002).

| Field | Type | Persistence | Meaning |
|---|---|---|---|
| `tierState` | `TattooTierProgress` (reused, unmodified) | `ExposeData()` from `CompExposeData()` | Tier + activation-count progression, identical mechanism to every prior tiered tattoo. |
| `cooldownEndTick` | `int` | `Scribe_Values.Look` | Game tick after which the gizmo is usable again. |
| `boostEndTick` | `int` | `Scribe_Values.Look` | Game tick after which this activation's melee-damage boost, pain-threshold increase, and defense reduction all end together. |

Behavior:

- `GetGizmo()` → builds and returns one `Command_Action` each call (icon, label "Berserker's Mark", `action =>
  TryActivate()`, `disabled`/`disabledReason` computed live from `cooldownEndTick`) — same shape as every prior
  triggered tattoo's `GetGizmo()`.
- `TryActivate()` → no-ops if still on cooldown (defensive; the gizmo itself should already prevent this per
  Edge Cases); otherwise sets `cooldownEndTick`/`boostEndTick` from accessor-resolved values for the comp's
  current tier and calls `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)`
  exactly once per activation (FR-004). Like Stormlash and Bloodrune, there is no registry to update and no
  target-search side effect — the entire effect is self-contained state on the wearer's own comp.
- `GetStatOffset(StatDef stat)` → returns `0f` immediately if `Find.TickManager.TicksGame >= boostEndTick`
  (window not active); otherwise, for `MeleeDamageFactor` returns the tier-appropriate `MeleeDamageOffset`, for
  `PainShockThreshold` returns the tier-appropriate `PainThresholdOffset`, for `ArmorRating_Sharp`/`Blunt`/`Heat`
  returns the tier-appropriate `DefenseOffset` **negated**, and returns `0f` for any other `StatDef` — mirrors
  `HediffComp_GuardiansCallEffect.GetStatOffset`'s exact tier + window-check shape (research.md R1).
- `CompExposeData()` → `base.CompExposeData()`, then `tierState.ExposeData()` and the `Scribe_Values.Look` calls
  above (FR-010).

No new `Hediff` subclass, no new interface, no new Harmony patch (research.md R4).

### `TattooEffectStatPartInstaller` (updated, `Source/Effects/`)

Two new `Register(...)` calls added to the existing static constructor, alongside the ones features 002/003/004/
005 already added:

```csharp
Register(StatDefOf.MeleeDamageFactor);
Register(StatDefOf.PainShockThreshold);
```

`StatDefOf.ArmorRating_Sharp`/`Blunt`/`Heat` are already registered (feature 004) and need no change.

### XML: `Defs/HediffDefs/Tattoos/BerserkersMark.xml` (updated)

`hediffClass` stays `HediffWithComps` (research.md R4); adds a `<comps>` entry:

```xml
<hediffClass>HediffWithComps</hediffClass>
...
<comps>
  <li Class="TattooMagic.HediffCompProperties_BerserkersMarkEffect">
    <meleeDamageOffsetTier1>0.20</meleeDamageOffsetTier1>
    <meleeDamageOffsetTier2>0.40</meleeDamageOffsetTier2>
    <painThresholdOffsetTier1>0.15</painThresholdOffsetTier1>
    <painThresholdOffsetTier2>0.15</painThresholdOffsetTier2>
    <defenseOffsetTier1>0.15</defenseOffsetTier1>
    <defenseOffsetTier2>0.08</defenseOffsetTier2>
    <boostDurationTicksTier1>180</boostDurationTicksTier1>
    <boostDurationTicksTier2>180</boostDurationTicksTier2>
    <cooldownDurationTicksTier1>1800</cooldownDurationTicksTier1>
    <cooldownDurationTicksTier2>1800</cooldownDurationTicksTier2>
    <tier2Threshold>15</tier2Threshold>
    <abilityIconPath>UI/Commands/BerserkersMark</abilityIconPath>
  </li>
</comps>
```

All values placeholders pending PRD §9's balance pass (Constitution Principle III), consistent with every tattoo
shipped so far; small/fast-to-test rather than "plausible-looking" per feature 006 research.md R8 precedent.
`defenseOffsetTier2 < defenseOffsetTier1` encodes FR-007's "smaller penalty at Tier 2" requirement directly in
the placeholder data, not just in code.

## State Transitions

Same tier-transition shape as every prior tiered tattoo (`TattooTierProgress`, unmodified): `tier` starts at
`1`, `progressionCounter` increments once per ability activation, flips to `tier = 2` exactly once when
`progressionCounter` reaches the accessor-resolved `Tier2Threshold` — idempotent, deterministic, Scribe-backed
(Constitution Principle V).

New to this feature: a per-activation, time-boxed window (`boostEndTick`) independent of tier state, re-derived
fresh from the comp's *current* tier every time `TryActivate()` runs — so an activation made just before the
Tier 1→2 upgrade uses Tier 1's numbers, and the next activation (post-upgrade) uses Tier 2's, with no retroactive
change to an already-running window (same shape as Guardian's Call's `tauntEndTick`/Stormlash's `boostEndTick`/
Bloodrune's `burstEndTick`). A second activation before a prior window closes overwrites `boostEndTick` outright
(refresh, not stack), consistent with every prior triggered tattoo's precedent. All three stat offsets derive
their answer from this same tier + `boostEndTick` state on every `GetStatOffset` query, so there is no separate
persisted state per effect — when the window closes, `PainShockThreshold` reverts on the pawn's very next stat
query, which (per Edge Cases) may immediately trigger RimWorld's own pain-shock evaluation if the pawn's current
pain exceeds the reverted threshold — an intended consequence of the trade-off, not state this feature needs to
special-case.
