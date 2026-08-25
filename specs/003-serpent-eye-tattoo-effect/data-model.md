# Phase 1 Data Model: Serpent's Eye Tattoo Effect (CE-Aware Slice)

Derived from the spec's Key Entities section and the decisions in `research.md`. This feature fills in the
`appliedHediff` feature 001 left as an inert stub (`TattooMagic_Hediff_SerpentsEye`), reuses feature 002's
`TattooEffectValues`/`TattooTierProgress`/`IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset` unmodified, and
adds two new reusable pieces (`IOnRangedHitLandedTattooEffect`, `CombatExtendedInterop`) alongside Serpent's Eye's
own effect.

## CombatExtendedInterop (static, resolved once at startup — not persisted)

The CE-branching convention this feature establishes (research.md R3).

| Member | Type | Notes |
|---|---|---|
| `IsLoaded` | `bool` | `ModsConfig.IsActive("CETeam.CombatExtended")`, computed once in the `[StaticConstructorOnStartup]` static constructor |
| `AimingAccuracy` | `StatDef` | `DefDatabase<StatDef>.GetNamedSilentFail("AimingAccuracy")`; `null` when CE is absent |

## IOnRangedHitLandedTattooEffect (new interface)

The ranged-attacker-side reuse point (research.md R2), the counterpart to feature 002's
`IOnMeleeHitTattooEffect`.

| Member | Notes |
|---|---|
| `void OnRangedHitLanded(Thing target, DamageInfo dinfo, float damageDealt)` | Implemented by any `HediffComp` that reacts to its wearer's own ranged shot landing on something; consumed by the updated `Patch_Pawn_PostApplyDamage_TattooOnHit`'s new sibling dispatch clause |

## HediffCompProperties_SerpentsEyeEffect / HediffComp_SerpentsEyeEffect

Attached to `TattooMagic_Hediff_SerpentsEye` (the existing stub Hediff from feature 001). Composes a
`TattooTierProgress` (feature 002, unmodified) and implements both `IProvidesTattooStatOffset` (feature 002,
unmodified interface) and the new `IOnRangedHitLandedTattooEffect`.

| Field (XML, on the props class) | Type | Notes |
|---|---|---|
| `accuracyBonusTier1` / `accuracyBonusTier2` | float | Offset applied to `StatDefOf.ShootingAccuracyPawn` when CE is absent, and to `CombatExtendedInterop.AimingAccuracy` when CE is present (research.md R4) |
| `swayRecoilBonusTier1` / `swayRecoilBonusTier2` | float | Offset applied to `StatDefOf.ShootingAccuracyPawn` only when CE is present (under CE this stat is "weapon handling," driving sway/recoil, not accuracy — research.md R4); has nothing to apply to when CE is absent, per spec Assumptions |
| `ceAmmoEffectivenessMultiplierTier2` | float | Tier 2 only, CE only (FR-006) — multiplies a landed CE projectile's `DamageAmount` (research.md R5); no Tier 1 equivalent, no vanilla equivalent |
| `tier2Threshold` | int | Qualifying-event count (ranged hits landed) needed to promote Tier 1 → Tier 2 |

All fields above are read exclusively through `TattooEffectValues.Get(...)` at point of use (FR-011); the XML
value here is only ever consulted as that call's `xmlDefault` argument.

| Field (runtime, on the comp) | Type | Notes |
|---|---|---|
| `tierState` | `TattooTierProgress` | Composed, not inherited (feature 002 research.md R5, reused unmodified); `tierState.tier` is `1` or `2`; `tierState.progressionCounter` counts qualifying "ranged hit landed" events |

**Behavior**:
- `GetStatOffset(StatDef stat)`:
  - `stat == StatDefOf.ShootingAccuracyPawn`: if `CombatExtendedInterop.IsLoaded`, returns the tier's
    `SwayRecoilBonusTierN`; else returns the tier's `AccuracyBonusTierN`.
  - `CombatExtendedInterop.IsLoaded && stat == CombatExtendedInterop.AimingAccuracy`: returns the tier's
    `AccuracyBonusTierN`.
  - Anything else: `0f`.
- `OnRangedHitLanded(Thing target, DamageInfo dinfo, float damageDealt)`: calls
  `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)` (FR-003/FR-004) — every
  qualifying landed shot counts, at either tier, mirroring Frost Sigil's "every successful proc counts" pattern
  but without a chance roll gating it (Serpent's Eye's counter has no proc-chance step; every landed shot always
  counts per FR-003).
- The CE-only Tier 2 ammo-effectiveness multiplier is *not* read from this comp directly by the CE patch; instead
  `Patch_CE_ProjectileImpact_TattooAmmoBonus` looks up the launcher pawn's `HediffComp_SerpentsEyeEffect`, checks
  `tierState.tier >= 2`, and if so reads `ceAmmoEffectivenessMultiplierTier2` via `TattooEffectValues` itself
  (same accessor, different call site — still satisfies FR-011).

**Validation rules**:
- `tierState.tier` is never anything other than `1` or `2`.
- `tierState.progressionCounter` never decreases and continues incrementing harmlessly past the Tier 2 threshold
  (no error, no further tier change — edge case from spec, same as feature 002).
- The comp only acts (FR-008) while attached to a live, applied `TattooMagic_Hediff_SerpentsEye` — removing the
  hediff removes the comp with it, satisfying FR-014 for free via normal Hediff lifecycle, exactly like feature
  002's FR-011.
- `OnRangedHitLanded` is never invoked for misses, fully-deflected (zero-damage) shots, or shots fired by anyone
  other than this comp's own pawn — enforced by the shared patch's guard clauses (FR-009), not by this comp.

## Updated: Patch_Pawn_PostApplyDamage_TattooOnHit (feature 002 file, additive change only)

Adds a second dispatch clause alongside the existing, untouched melee one:

| Branch | Condition | Dispatches to | Interface |
|---|---|---|---|
| Existing (melee, unmodified) | `dinfo.Tool != null` | `__instance`'s (struck pawn's) comps | `IOnMeleeHitTattooEffect.OnMeleeHitTaken` |
| New | `dinfo.Tool == null` | `dinfo.Instigator`'s (attacking pawn's) comps, when it `is Pawn` | `IOnRangedHitLandedTattooEffect.OnRangedHitLanded` |

Both branches share the existing `totalDamageDealt > 0` guard (FR-009) at the top of the postfix.

## New: Patch_CE_ProjectileImpact_TattooAmmoBonus (CE-only, imperatively applied)

Not a `[HarmonyPatch]`-attributed class — applied via an explicit `harmony.Patch(...)` call from
`TattooMagicMain`'s static constructor, gated by `CombatExtendedInterop.IsLoaded`, since its target type
(`CombatExtended.ProjectileCE`) is resolved via `AccessTools.TypeByName` rather than referenced at compile time.

| Aspect | Detail |
|---|---|
| Target | `CombatExtended.ProjectileCE.Impact(Thing hitThing)`, postfix |
| Guard | Only patched at all when `CombatExtendedInterop.IsLoaded` is true; no-ops (never even attempts `AccessTools.TypeByName`) otherwise |
| Behavior | Reads the projectile instance's `launcher` field; if it's a `Pawn` with a Tier 2 `HediffComp_SerpentsEyeEffect`, multiplies that instance's `DamageAmount` (via its existing public getter/setter) by `TattooEffectValues.Get(scopeKey, "CeAmmoEffectivenessMultiplierTier2", ...)` before impact resolves |
| Scope of mutation | Exactly one in-flight projectile `Thing` instance — never `AmmoDef`, `ThingDef`, or any other shared data (research.md R5) |
| Persistence | None needed — the projectile is destroyed on impact regardless of this feature |

## New: TattooHediffRemovalGuard + Patch_HealthTracker_RemoveHediff_ProtectTattoos (FR-015, cross-tattoo)

Not Serpent's-Eye-specific — deliberately generic across every tattoo, added to `Source/Effects/` alongside
`CombatExtendedInterop` since both are reusable, tattoo-agnostic infrastructure rather than one tattoo's effect.

| Member | Type | Notes |
|---|---|---|
| `TattooHediffRemovalGuard.IsProtected(Hediff)` | `bool` | True if `hediff.def` is any `TattooMagicDef.appliedHediff` (feature 001's registry), built once into a `HashSet<HediffDef>` at startup |
| `TattooHediffRemovalGuard.IsSanctioned(Hediff)` | `bool` | True only for the specific `Hediff` instance currently mid-removal via the method below |
| `TattooHediffRemovalGuard.RemoveTattooHediff(Pawn, Hediff)` | — | The one sanctioned removal entry point; sets the sanctioned instance, calls `pawn.health.RemoveHediff`, clears it in a `finally` block. No feature currently calls this (no in-game removal exists yet — spec edge case), but it's the entry point a future one must use |
| `Patch_HealthTracker_RemoveHediff_ProtectTattoos` | `[HarmonyPatch]` prefix on `Pawn_HealthTracker.RemoveHediff` | Returns `true` (let removal proceed) if `DebugSettings.godMode` is set, or if the hediff isn't protected, or if it's the currently-sanctioned instance; returns `false` (block) otherwise |

**Behavior**:
- Covers all 11 `TattooMagicDef` entries automatically — no per-tattoo registration, so Ember Ward/Ironskin
  Glyph/Starlight Ward are already covered once they ship their own effects, and Frost Sigil is covered
  retroactively without touching any feature 002 file.
- `DebugSettings.godMode` is the same flag vanilla's own health-tab UI checks before showing its per-hediff
  delete button — honoring it here is what makes Developer Mode/God Mode removal keep working (this was
  caught and fixed after an initial version blocked it too; see spec Edge Cases).

**Validation rules**:
- Blocks exactly one API surface: `Pawn_HealthTracker.RemoveHediff`. A caller manipulating
  `pawn.health.hediffSet.hediffs` directly (bypassing that method) is not caught — a known, accepted gap since
  that pattern is rare and considered bad practice even among mods, not something this feature attempts to
  close.
- Never throws for a non-tattoo hediff — the `IsProtected` check is the first thing evaluated and short-circuits
  to allowing removal.

## Persistence summary (Constitution Principle V)

- `HediffComp_SerpentsEyeEffect.tierState` (`tier`, `progressionCounter`) — `Scribe_Values`, on the Serpent's Eye
  hediff, saved/loaded automatically as part of the pawn's health state, identical mechanism to Frost Sigil's.
- `CombatExtendedInterop.IsLoaded`/`AimingAccuracy` — computed fresh every session at startup, never persisted
  (recomputing is cheap and correctly reflects whichever mod list is active for that session, per the spec's edge
  case about CE being added/removed between sessions).
- The CE ammo-effectiveness bonus — applied and consumed within a single projectile's flight; nothing to persist.
- `TattooEffectValues`'s override dictionary — still not persisted by this feature either, same as feature 002.
