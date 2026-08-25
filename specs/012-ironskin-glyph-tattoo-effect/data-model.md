# Phase 1 Data Model: Ironskin Glyph Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain, feature 002's `TattooEffectValues` /
`TattooTierProgress` / `IProvidesTattooStatOffset` / `StatPart_TattooEffectOffset`, and feature 011's
`IOnIncomingDamageTattooEffect` / `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` / `CombatExtendedInterop` — all
reused unmodified (research.md R1). No new interface, no new Harmony patch, and no new
`TattooEffectStatPartInstaller` registration (`ArmorRating_Sharp`/`ArmorRating_Blunt` are already registered,
research.md R1). The only new production code is one `HediffComp` and its `HediffCompProperties`.

## No changes: `TattooEffectStatPartInstaller`

`Register(StatDefOf.ArmorRating_Sharp);` and `Register(StatDefOf.ArmorRating_Blunt);` are already present (added by
an earlier feature, unused by any tattoo shipped so far). This feature is their first real consumer — zero lines
added or changed in this file.

## No changes: `IOnIncomingDamageTattooEffect`, `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`

Both remain exactly as feature 011 shipped them (`dinfo` already by `ref`, dispatch already unconditional per
`DamageDef` — research.md R1/R3). Ironskin Glyph is simply the second implementer.

## `HediffCompProperties_IronskinGlyphEffect` / `HediffComp_IronskinGlyphEffect`

Attached to `TattooMagic_Hediff_IronskinGlyph` (the existing stub Hediff from feature 001, currently with no
`<comps>` block). Composes a `TattooTierProgress` (feature 002, unmodified) and implements
`IProvidesTattooStatOffset`, `IOnIncomingDamageTattooEffect`, and `IProvidesTattooTierProgress` (the same
three-interface shape `HediffComp_EmberWardEffect` already uses).

| Field (XML, on the props class) | Type | Notes |
|---|---|---|
| `armorRatingBonusTier1` / `armorRatingBonusTier2` | float | Positive offset to **both** `StatDefOf.ArmorRating_Sharp` and `StatDefOf.ArmorRating_Blunt` identically (research.md R5 — a single generalist value, not two independently-tunable knobs) |
| `fullNegateChanceTier2` | float | Tier 2 only (FR-005/FR-006) — chance, per qualifying physical instance, to fully absorb it via `IOnIncomingDamageTattooEffect` |
| `tier2Threshold` | int | Qualifying-event count (physical hits absorbed, research.md R4) needed to promote Tier 1 → Tier 2 |

All fields above are read exclusively through `TattooEffectValues.Get(...)` at point of use (FR-012), matching
every tattoo shipped so far.

| Field (runtime, on the comp) | Type | Persistence | Notes |
|---|---|---|---|
| `tierState` | `TattooTierProgress` | `ExposeData()` from `CompExposeData()` | `tier` is `1` or `2`; `progressionCounter` counts qualifying physical hits (research.md R4) |

No other runtime-only fields are needed — no time-boxed state, unlike Vampiric Thorn's post-kill window or
Bloodrune's heal burst.

**Behavior**:
- `GetStatOffset(StatDef stat)`:
  - `stat == StatDefOf.ArmorRating_Sharp || stat == StatDefOf.ArmorRating_Blunt` → tier-appropriate
    `armorRatingBonus` value (positive; FR-001), identical for both stats (research.md R5).
  - Any other stat → `0f` (contract requirement, features 002/011 precedent).
- `TryAbsorbIncomingDamage(ref DamageInfo dinfo)`:
  1. Resolve `StatDef armorStat = dinfo.Def?.armorCategory?.armorRatingStat;`. If `armorStat` is neither
     `StatDefOf.ArmorRating_Sharp` nor `StatDefOf.ArmorRating_Blunt`, return `false` immediately (FR-009 —
     implementer-side filtering per the `IOnIncomingDamageTattooEffect` contract; research.md R2's
     category-driven filter, not an enumerated `DamageDef` list).
  2. If `Pawn == null || Pawn.Dead`, return `false` (edge case: nothing meaningful to do to a dead pawn's comp).
  3. If `dinfo.Amount <= 0f`, return `false` without registering a qualifying event (FR-010 — an instance already
     reduced to zero before reaching this comp isn't a qualifying "absorbed" event).
  4. `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)` (FR-003) — every
     qualifying instance counts, **before** steps 5-6 below read `tierState.tier`, so a tier-up-triggering hit
     correctly uses its new tier's values in the same call (mirrors Ember Ward's ordering rationale, research.md
     R4/feature 011 R6).
  5. **If `CombatExtendedInterop.IsLoaded`** (research.md R3): read the tier-appropriate `armorRatingBonus` value
     and directly reduce `dinfo.Amount` by that fraction via `dinfo.SetAmount(Mathf.Max(0f, dinfo.Amount *
     (1f - reduction)))`. This is necessary because CE's own `ArmorUtilityCE.GetAfterArmorDamage` (both its ambient
     and its direct-hit sub-paths, research.md R3) only reads a pawn's own armor-category stat when the hit body
     part is in the `CoveredByNaturalArmor` group — never true for a human — so step-4-adjacent `GetStatOffset`
     contribution alone silently no-ops under CE. Skipped entirely when CE isn't loaded, since vanilla's own armor
     pipeline already applies the stat offset correctly.
  6. If `tierState.tier < 2`, return `false` (FR-006 — Tier 1 never rolls or grants a full negate; step 5's CE
     reduction still applies at Tier 1, using Tier 1's own value — only the full-negate roll is Tier 2-exclusive).
  7. At Tier 2: roll `fullNegateChanceTier2` (accessor-resolved). On success, log (Dev Mode) and return `true`
     (FR-005's full negate) — this fully absorbs the (already step-5-reduced, if CE is loaded) instance. On
     failure, return `false` — the instance proceeds with whatever amount step 5 left it at, and (under vanilla
     only) armor processing still applies `GetStatOffset`'s contribution on top, per research.md R3.
- `CompExposeData()`: `base.CompExposeData()`, then `tierState.ExposeData()` (FR-007).

**Validation rules**:
- `tierState.tier` is never anything other than `1` or `2`; `tierState.progressionCounter` never decreases and
  continues incrementing harmlessly past the Tier 2 threshold (feature 002 precedent, unmodified).
- The comp only acts (FR-008) while attached to a live, applied `TattooMagic_Hediff_IronskinGlyph` — removing the
  hediff removes the comp with it, satisfying FR-014 for free via normal Hediff lifecycle (same as every prior
  tattoo); the existing `TattooHediffRemovalGuard`/`Patch_HealthTracker_RemoveHediff_ProtectTattoos` (feature 003
  FR-015 precedent) already covers it via the generic `appliedHediff` registry, no Ironskin-Glyph-specific
  configuration needed.
- `TryAbsorbIncomingDamage` never registers a qualifying event or rolls for full negate for a non-Sharp/Blunt
  `DamageDef`, a zero-amount instance, or when the wearer is dead (FR-009/FR-010).
- The full-negate roll only ever executes once `tierState.tier >= 2` — enforced by the ordering in step 5/6 above,
  not by a separate guard the caller must remember (FR-006).

### XML: `Defs/HediffDefs/Tattoos/IronskinGlyph.xml` (updated)

`hediffClass` stays `HediffWithComps`; adds a `<comps>` entry, replacing the current "effect not yet implemented"
description text:

```xml
<hediffClass>HediffWithComps</hediffClass>
...
<!-- All numeric values below are placeholders pending the PRD §9 balance pass. They are read
     exclusively through TattooEffectValues at point of use (FR-012), small/fast-to-test rather
     than "plausible-looking" per feature 006 research.md R8 precedent. -->
<comps>
  <li Class="TattooMagic.HediffCompProperties_IronskinGlyphEffect">
    <armorRatingBonusTier1>0.08</armorRatingBonusTier1>
    <armorRatingBonusTier2>0.16</armorRatingBonusTier2>
    <fullNegateChanceTier2>0.15</fullNegateChanceTier2>
    <tier2Threshold>10</tier2Threshold>
  </li>
</comps>
```

All values placeholders pending PRD §9's balance pass (Constitution Principle III) — carried by the XML comment
above, matching the top-of-file comment every previously shipped tattoo's `HediffDef` XML carries without
exception.

## Persistence summary (Constitution Principle V)

- `HediffComp_IronskinGlyphEffect.tierState` (`tier`, `progressionCounter`) — `Scribe_Values`, on the Ironskin
  Glyph hediff, saved/loaded automatically as part of the pawn's health state, identical mechanism to every prior
  tiered tattoo.
- No other runtime state exists to persist — no time-boxed windows, no pending rolls.
- `TattooEffectValues`'s override dictionary — still not persisted by this feature either, same as every prior
  feature.
