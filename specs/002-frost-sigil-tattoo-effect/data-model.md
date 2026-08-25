# Phase 1 Data Model: Frost Sigil Tattoo Effect (Effect Pattern Vertical Slice)

Derived from the spec's Key Entities section and the decisions in `research.md`. This feature fills in the
`appliedHediff` this feature 001 left as an inert stub (`TattooMagic_Hediff_FrostSigil`), and introduces the
reusable infrastructure (`contracts/passive-tattoo-effect-contract.md`) that later passive-tattoo features
build against — it does not touch feature 001's ritual/tracker data model at all.

## TattooEffectValues (static accessor, not persisted)

The single override-capable read point required by FR-012/SC-007.

| Member | Type | Notes |
|---|---|---|
| `Get(scopeKey, valueKey, xmlDefault)` | `float` | Returns an override if one has been set for `scopeKey.valueKey`, else `xmlDefault`. This feature never calls a setter — none exists yet; a future settings feature (PRD §10) owns adding one. |

`scopeKey` for Frost Sigil is its `HediffDef.defName` (`TattooMagic_Hediff_FrostSigil`); `valueKey` is a
short name per value (e.g. `"ColdResistanceTier1"`, `"SlowChanceTier2"`) — see the field table below for the
full set.

## HediffCompProperties_FrostSigilEffect / HediffComp_FrostSigilEffect

Attached to `TattooMagic_Hediff_FrostSigil` (the existing stub Hediff from feature 001). Composes a
`TattooTierProgress` and implements both `IProvidesTattooStatOffset` and `IOnMeleeHitTattooEffect`
(`research.md` R5).

| Field (XML, on the props class) | Type | Notes |
|---|---|---|
| `coldResistanceTier1` / `coldResistanceTier2` | float | `ComfyTemperatureMin` offset per tier; placeholder balance numbers (PRD §9) |
| `slowChanceTier1` / `slowChanceTier2` | float (0–1) | Chance the on-hit proc triggers |
| `slowMoveSpeedOffsetTier1` / `Tier2` | float | Magnitude of the attacker's `MoveSpeed` offset while slowed (negative = slower; summed additively by `StatPart_TattooEffectOffset`) |
| `slowDurationTicksTier1` / `Tier2` | int | How long the slow lasts on the attacker |
| `freezeChanceTier2` | float (0–1) | Tier 2 only — Tier 1 has no freeze (PRD §6) |
| `freezeDurationTicksTier2` | int | Passed to `stances.stunner.StunFor(...)` |
| `tier2Threshold` | int | Qualifying-event count needed to promote Tier 1 → Tier 2 |

All fields above are read exclusively through `TattooEffectValues.Get(...)` at point of use (FR-012); the
XML value here is only ever consulted as that call's `xmlDefault` argument.

| Field (runtime, on the comp) | Type | Notes |
|---|---|---|
| `tierState` | `TattooTierProgress` | Composed, not inherited (research.md R5); `tierState.tier` is `1` or `2`; `tierState.progressionCounter` counts qualifying events |

**Behavior**:
- `GetStatOffset(StatDef stat)`: if `stat == ComfyTemperatureMin`, returns
  `TattooEffectValues.Get(..., tierState.tier == 1 ? "ColdResistanceTier1" : "ColdResistanceTier2", ...)`;
  else `0`.
- `OnMeleeHitTaken(Pawn attacker, DamageInfo dinfo, float damageDealt)`: rolls
  `TattooEffectValues.Get(..., "SlowChanceTierN", ...)`; on success, grants
  `TattooMagic_Hediff_FrostSigilSlow` to `attacker` (see below), then calls
  `tierState.TryRegisterQualifyingEvent("Tier2Threshold", default)` (FR-003/FR-004). If already Tier 2, also
  rolls `freezeChanceTier2` and, on success, calls `attacker.stances.stunner.StunFor(...)`.

**Validation rules**:
- `tierState.tier` is never anything other than `1` or `2`.
- `tierState.progressionCounter` never decreases and continues incrementing harmlessly past the Tier 2
  threshold (no error, no further tier change — edge case from spec).
- The comp only acts (FR-007) while attached to a live, applied `TattooMagic_Hediff_FrostSigil` — removing
  the hediff removes the comp with it, satisfying FR-011 for free via normal Hediff lifecycle.

## TattooMagic_Hediff_FrostSigilSlow (new `HediffDef`, transient — granted to attackers, not tattooed pawns)

| Field | Type | Notes |
|---|---|---|
| `defName` | string | `TattooMagic_Hediff_FrostSigilSlow` |
| `hediffClass` | — | `HediffWithComps` |
| comps | — | `HediffCompProperties_Disappears` (vanilla; `disappearsAfterTicks` set at grant time from `slowDurationTicksTierN`) + a small new comp implementing `IProvidesTattooStatOffset` against `MoveSpeed`, whose offset value is set once at grant time (not tier-aware itself — it's a one-shot snapshot of whichever tier the *defender* was at when the proc fired) |

**Validation rules**:
- Always self-removes via `HediffComp_Disappears`; this feature does not need a manual removal path.
- Stacks/refreshes on a repeat proc are not required to behave any particular way (edge case in spec —
  overlapping slows must not error, but exact stacking behavior is unconstrained).

## Reusable infrastructure (not tattoo-specific — see `contracts/passive-tattoo-effect-contract.md`)

| Piece | Kind | Owns |
|---|---|---|
| `IProvidesTattooStatOffset` | interface | `float GetStatOffset(StatDef stat)` |
| `IOnMeleeHitTattooEffect` | interface | `void OnMeleeHitTaken(Pawn attacker, DamageInfo dinfo, float damageDealt)` |
| `TattooTierProgress` | plain class (composed, not a `HediffComp`) | `tier`, `progressionCounter`, `ExposeData()`, `TryRegisterQualifyingEvent(...)` |
| `StatPart_TattooEffectOffset` | `StatPart`, registered in C# at startup onto `ComfyTemperatureMin` and `MoveSpeed` | Sums `IProvidesTattooStatOffset` across a pawn's hediffs for the queried stat |
| `Patch_Pawn_PostApplyDamage_TattooOnHit` | Harmony postfix on `Pawn.PostApplyDamage` | Dispatches to `IOnMeleeHitTattooEffect` on melee hits that dealt damage |

None of these five carry any Frost-Sigil-specific knowledge; Frost Sigil is their first consumer, not their
only intended one (FR-010).

## Persistence summary (Constitution Principle V)

- `HediffComp_FrostSigilEffect.tierState` (`tier`, `progressionCounter`) — `Scribe_Values`, on the
  Frost Sigil hediff, saved/loaded like any other `HediffComp` automatically as part of the pawn's health
  state.
- The transient slow hediff and its offset value/remaining-duration — `Scribe`-persisted the same way while
  it exists; expected to normally expire well before a save/reload matters, but must not error if a save
  happens while it's active.
- `TattooEffectValues`'s override dictionary is explicitly **not** persisted by this feature — it starts
  empty every session since nothing sets it yet; a future settings feature owns its persistence.
