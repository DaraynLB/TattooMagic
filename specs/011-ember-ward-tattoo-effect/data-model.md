# Phase 1 Data Model: Ember Ward Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain and feature 002's `TattooEffectValues` /
`TattooTierProgress` / `IProvidesTattooStatOffset` / `StatPart_TattooEffectOffset`, all reused unmodified
(research.md R1). Adds one new registration to the existing stat-part installer (research.md R2), zero new
registration for the already-installed `ArmorRating_Heat` (research.md R3), one new reuse point
(`IOnIncomingDamageTattooEffect`, research.md R4-R5) via a new, additive Harmony patch, and one new
`HediffComp`.

## Updated: `TattooEffectStatPartInstaller` (feature 002 file, additive change only)

Adds one registration call:

```csharp
Register(StatDefOf.ComfyTemperatureMax);
```

alongside the existing, untouched `Register(StatDefOf.ComfyTemperatureMin);` and every other existing
registration (including `Register(StatDefOf.ArmorRating_Heat);`, already present and, until this feature, unused
by any shipped tattoo). No other line in this file changes.

## `IOnIncomingDamageTattooEffect` (new interface, `Source/Effects/`)

The pre-armor incoming-damage reuse point (research.md R4-R5); see
[`contracts/incoming-damage-contract.md`](./contracts/incoming-damage-contract.md) for the full contract.

| Member | Notes |
|---|---|
| `bool TryAbsorbIncomingDamage(ref DamageInfo dinfo)` | Implemented by any `HediffComp` that reacts to a specific kind of incoming damage before it's applied to its own pawn. Consumed by the new `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` prefix, dispatched for every incoming `DamageInfo` regardless of `DamageDef`; implementers filter for themselves. `dinfo` is by `ref` (research.md R10) so an implementer can mutate `dinfo.Amount` for a partial reduction, in addition to or instead of returning `true` to fully absorb the instance. |

## New: `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` (`Source/Patches/`)

```csharp
[HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
public static class Patch_Pawn_PreApplyDamage_TattooDamageAbsorb
{
    public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = false;

        if (__instance?.health?.hediffSet == null)
            return true;

        List<Hediff> hediffs = __instance.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                continue;

            List<HediffComp> comps = hediffWithComps.comps;
            for (int j = 0; j < comps.Count; j++)
            {
                if (comps[j] is IOnIncomingDamageTattooEffect effect && effect.TryAbsorbIncomingDamage(ref dinfo))
                {
                    absorbed = true;
                    TattooTrackerUtility.GetTracker(__instance)?.RegisterMasteryActivation();
                    return false;
                }
            }
        }

        return true;
    }
}
```

- A **prefix**, not a postfix (research.md R4 — must run before armor/health processing to be able to prevent it).
- Mirrors the existing dispatch loops' shape (`Patch_Pawn_PostApplyDamage_TattooOnHit`'s `DispatchMeleeHit` etc.)
  for consistency, but is a distinct patch on a distinct method, not an added clause to that file (research.md R4
  explains why reusing that file's own postfix is wrong for this purpose).
- Credits `TattooTrackerUtility.GetTracker(__instance)?.RegisterMasteryActivation()` once, only on an actual full
  absorb — mirrors feature 006's "every dispatched notification credits mastery progress" precedent for the case
  that's unambiguously a real completed event; a `false` return (ordinary reduction, no full-ignore) still credits
  mastery progress too, but via the ordinary combat-event path already covered elsewhere (a pawn taking any damage
  at all already triggers `Pawn.PostApplyDamage`'s own existing dispatch/mastery-credit logic for other tattoos
  it may have equipped) — this patch does not need to duplicate that for Ember Ward specifically, since Ember
  Ward's own progression counter (not pawn-wide mastery) is what tracks its Tier 1→2 progress, and that increments
  inside `TryAbsorbIncomingDamage` itself (see below) independent of whether this patch's own mastery credit line
  ever runs.
- `Debug`-mode logging is added mirroring the existing shared patch's own always-on-in-Dev-Mode visibility
  (`TattooMagicSettings.EnableDebugLogging`), logging every dispatch outcome (absorbed or not) for the same
  "is this even firing at all" diagnostic value features 002/003/010 already rely on for their own dispatch
  points.

## `HediffCompProperties_EmberWardEffect` / `HediffComp_EmberWardEffect`

Attached to `TattooMagic_Hediff_EmberWard` (the existing stub Hediff from feature 001, currently with no
`<comps>` block). Composes a `TattooTierProgress` (feature 002, unmodified) and implements
`IProvidesTattooStatOffset` and the new `IOnIncomingDamageTattooEffect`.

| Field (XML, on the props class) | Type | Notes |
|---|---|---|
| `heatResistanceTier1` / `heatResistanceTier2` | float | Positive offset to `StatDefOf.ComfyTemperatureMax` (FR-001) |
| `armorRatingHeatTier1` / `armorRatingHeatTier2` | float | Positive offset to `StatDefOf.ArmorRating_Heat` (FR-002/FR-005) — the ongoing, always-on burn-damage reduction, resolved entirely by RimWorld's/CE's own existing armor pipeline (research.md R3) |
| `fullIgnoreChanceTier2` | float | Tier 2 only (FR-005/FR-006) — chance, per qualifying fire/burn instance, to fully absorb it via `IOnIncomingDamageTattooEffect` |
| `tier2Threshold` | int | Qualifying-event count (fire/burn instances reaching this comp with nonzero incoming amount, research.md R6) needed to promote Tier 1 → Tier 2 |

All fields above are read exclusively through `TattooEffectValues.Get(...)` at point of use (FR-012), matching
every tattoo shipped so far.

| Field (runtime, on the comp) | Type | Persistence | Notes |
|---|---|---|---|
| `tierState` | `TattooTierProgress` | `ExposeData()` from `CompExposeData()` | `tier` is `1` or `2`; `progressionCounter` counts qualifying fire/burn instances (research.md R6) |

No other runtime-only fields are needed — unlike Vampiric Thorn's post-kill window or Bloodrune's heal burst,
Ember Ward has no time-boxed state to track between ticks.

**Behavior**:
- `GetStatOffset(StatDef stat)`:
  - `stat == StatDefOf.ComfyTemperatureMax` → tier-appropriate `heatResistance` value (positive; FR-001).
  - `stat == StatDefOf.ArmorRating_Heat` → tier-appropriate `armorRatingHeat` value (positive; FR-002/FR-005) —
    correct and load-bearing under vanilla RimWorld (research.md R2/R3); under Combat Extended this contribution
    alone is insufficient for an ordinary human (research.md R10) and is supplemented by step 5 below, but is
    still returned here unconditionally so it remains visible on the Stats tab either way.
  - Any other stat → `0f` (contract requirement, research.md R2/R3 precedent).
- `TryAbsorbIncomingDamage(ref DamageInfo dinfo)`:
  1. If `dinfo.Def != DamageDefOf.Flame && dinfo.Def != DamageDefOf.Burn`, return `false` immediately (FR-009 —
     implementer-side filtering per the contract).
  2. If `Pawn == null || Pawn.Dead`, return `false` (edge case: nothing meaningful to do to a dead pawn's comp).
  3. If `dinfo.Amount <= 0f`, return `false` without registering a qualifying event (FR-010 — an instance already
     reduced to zero before reaching this comp isn't a qualifying "absorbed" event).
  4. `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)` (FR-003) — every
     qualifying instance counts, **before** steps 5-6 below read `tierState.tier`, so a full-ignore/CE-reduction
     that also crosses the tier threshold uses its new tier's values in the same call (mirrors Vampiric Thorn's
     ordering rationale, feature 010).
  5. **If `CombatExtendedInterop.IsLoaded`** (research.md R10): read the tier-appropriate `armorRatingHeat`
     value and directly reduce `dinfo.Amount` by that fraction via `dinfo.SetAmount(Mathf.Max(0f, dinfo.Amount *
     (1f - reduction)))`. This is necessary because CE's own `ArmorUtilityCE.GetAmbientPostArmorDamage` only
     reads a pawn's own `ArmorRating_Heat` when the hit body part is in the `CoveredByNaturalArmor` group — never
     true for a human — so step 4's `GetStatOffset` contribution alone silently no-ops under CE. Skipped entirely
     when CE isn't loaded, since vanilla's own armor pipeline already applies the stat offset correctly.
  6. If `tierState.tier < 2`, return `false` (FR-006 — Tier 1 never rolls or grants a full ignore; step 5's CE
     reduction still applies at Tier 1, using Tier 1's own value — only the full-ignore roll is Tier 2-exclusive).
  7. At Tier 2: roll `fullIgnoreChanceTier2` (accessor-resolved). On success, log (Dev Mode) and return `true`
     (FR-005's full-ignore) — this fully absorbs the (already step-5-reduced, if CE is loaded) instance. On
     failure, return `false` — the instance proceeds with whatever amount step 5 left it at, and (under vanilla
     only) armor processing still applies `GetStatOffset`'s contribution on top, per research.md R3.
- `CompExposeData()`: `base.CompExposeData()`, then `tierState.ExposeData()` (FR-007).

**Validation rules**:
- `tierState.tier` is never anything other than `1` or `2`; `tierState.progressionCounter` never decreases and
  continues incrementing harmlessly past the Tier 2 threshold (feature 002 precedent, unmodified).
- The comp only acts (FR-008) while attached to a live, applied `TattooMagic_Hediff_EmberWard` — removing the
  hediff removes the comp with it, satisfying FR-014 for free via normal Hediff lifecycle (same as every prior
  tattoo); the existing `TattooHediffRemovalGuard`/`Patch_HealthTracker_RemoveHediff_ProtectTattoos` (feature 003
  FR-015 precedent) already covers it via the generic `appliedHediff` registry, no Ember-Ward-specific
  configuration needed.
- `TryAbsorbIncomingDamage` never registers a qualifying event or rolls for full-ignore for a non-fire/burn
  `DamageDef`, a zero-amount instance, or when the wearer is dead (FR-009/FR-010).
- The full-ignore roll only ever executes once `tierState.tier >= 2` — enforced by the ordering in step 5/6 above,
  not by a separate guard the caller must remember (FR-006).

### XML: `Defs/HediffDefs/Tattoos/EmberWard.xml` (updated)

`hediffClass` stays `HediffWithComps`; adds a `<comps>` entry:

```xml
<hediffClass>HediffWithComps</hediffClass>
...
<!-- All numeric values below are placeholders pending the PRD §9 balance pass. They are read
     exclusively through TattooEffectValues at point of use (FR-012), small/fast-to-test rather
     than "plausible-looking" per feature 006 research.md R8 precedent. -->
<comps>
  <li Class="TattooMagic.HediffCompProperties_EmberWardEffect">
    <heatResistanceTier1>5</heatResistanceTier1>
    <heatResistanceTier2>10</heatResistanceTier2>
    <armorRatingHeatTier1>0.08</armorRatingHeatTier1>
    <armorRatingHeatTier2>0.16</armorRatingHeatTier2>
    <fullIgnoreChanceTier2>0.15</fullIgnoreChanceTier2>
    <tier2Threshold>10</tier2Threshold>
  </li>
</comps>
```

All values placeholders pending PRD §9's balance pass (Constitution Principle III) — carried by the XML comment
above, matching the top-of-file comment every previously shipped tattoo's `HediffDef` XML carries without
exception.

## Persistence summary (Constitution Principle V)

- `HediffComp_EmberWardEffect.tierState` (`tier`, `progressionCounter`) — `Scribe_Values`, on the Ember Ward
  hediff, saved/loaded automatically as part of the pawn's health state, identical mechanism to every prior tiered
  tattoo.
- No other runtime state exists to persist — no time-boxed windows, no pending rolls.
- `TattooEffectValues`'s override dictionary — still not persisted by this feature either, same as every prior
  feature.
