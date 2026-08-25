# Contract: Passive Tattoo Effect Infrastructure — Addition (Pre-Armor Incoming Damage Reuse Point)

This extends feature 002's
[`passive-tattoo-effect-contract.md`](../../002-frost-sigil-tattoo-effect/contracts/passive-tattoo-effect-contract.md)
and feature 010's
[`melee-hit-landed-contract.md`](../../010-vampiric-thorn-tattoo-effect/contracts/melee-hit-landed-contract.md),
both of which remain in force **unmodified** — `TattooEffectValues`, `IProvidesTattooStatOffset`,
`StatPart_TattooEffectOffset`, and `TattooTierProgress` all mean exactly what those documents say, and this
feature is simply another consumer of them. This document covers only what's new: a pre-armor "incoming damage"
reuse point, dispatched from a new patch distinct from the existing `Patch_Pawn_PostApplyDamage_TattooOnHit`
family (research.md R4 explains why that existing patch can't serve this purpose).

## 1. `IOnIncomingDamageTattooEffect` — pre-armor incoming-damage reuse point

```csharp
public interface IOnIncomingDamageTattooEffect
{
    bool TryAbsorbIncomingDamage(ref DamageInfo dinfo);
}
```

- Any `HediffComp` on a pawn implementing this interface is invoked by a new, tattoo-agnostic Harmony **prefix**
  on `Pawn.PreApplyDamage`, dispatched once per implementing comp for **every** incoming `DamageInfo` aimed at
  that pawn — before armor or health processing, and regardless of `DamageDef` or whether the instigator is a
  `Pawn` (research.md R4).
- **Implementers must filter for themselves**: return `false` immediately for any `DamageDef` (or other condition)
  the implementer doesn't care about — mirrors `IProvidesTattooStatOffset.GetStatOffset`'s "return 0 for stats you
  don't handle" convention (feature 002). The dispatcher has no per-`DamageDef` routing table and never will.
- **`dinfo` is passed by `ref`** (changed from by-value during this feature's own implementation, research.md
  R10): an implementer may mutate `dinfo.Amount` (via `dinfo.SetAmount(...)`) to apply a partial reduction, in
  addition to or instead of returning `true`. This exists because `IProvidesTattooStatOffset` against an
  armor-category `StatDef` (§below) is **not sufficient on its own** to guarantee a real reduction under every
  combat mod — Combat Extended's own armor formula only reads a pawn's own armor-category stat when the hit body
  part belongs to a specific body-part group that ordinary humans never satisfy (research.md R10) — so a tattoo
  needing a guaranteed-correct reduction under CE should mutate `dinfo.Amount` directly here, typically gated on
  `CombatExtendedInterop.IsLoaded` so it doesn't double up with an already-correct vanilla stat-offset path (see
  Ember Ward's own comp for the reference implementation).
- **Return value contract**: `true` means "fully absorb this instance" — the caller sets `absorbed = true` on the
  original method's `out` parameter and skips the rest of vanilla's/CE's own damage-application pipeline for this
  instance entirely (no wound, no death, no further `PostApplyDamage` dispatch of any kind for it — see "What this
  means downstream" below), regardless of any prior mutation to `dinfo.Amount` in the same call. `false` means
  "let this instance proceed normally, with whatever mutation to `dinfo.Amount` was made"; the implementer may
  still have taken action before returning `false` (e.g. incrementing its own progression counter, or reducing
  `dinfo.Amount` — see Ember Ward's own comp, which does both on every qualifying call regardless of the
  full-ignore roll's outcome).
- **Guarantee**: called at most once per implementing comp per incoming `DamageInfo`, before any armor-stat offset
  from `IProvidesTattooStatOffset` (R3) has had a chance to influence the outcome — this interface fires strictly
  earlier in the pipeline than any stat-driven reduction, and any `dinfo.Amount` mutation made here is visible to
  everything downstream (vanilla's/CE's own armor processing, `PostApplyDamage`) since it mutates the same
  `DamageInfo` that continues through the rest of the call chain. Never called for a destroyed/despawned target
  (mirrors `Thing.TakeDamage`'s own early-out).
- **What is NOT guaranteed**: call order between multiple implementing comps on the same pawn (same caveat as
  every existing reuse point in this family); if more than one comp on the same pawn returns `true` for the same
  instance, only the first one encountered actually short-circuits the dispatch loop — later comps are not called
  for that instance, and any `dinfo.Amount` mutation an earlier comp made before a later comp's own full-absorb is
  moot once absorbed. This mirrors how the existing on-hit dispatch loops (features 002/003/010) never guaranteed
  "every comp always sees every event" once an early comp's own logic already mutated shared state; no tattoo
  shipped so far or planned needs to stack two independent full-absorb rolls or partial reductions on the same
  instance, so this ordering nuance has no observable effect in practice.

## 2. What is explicitly NOT part of this contract

- The exact `DamageDef`s Ember Ward reacts to (`Flame`, `Burn`) are Ember Ward's own filtering choice inside its
  `TryAbsorbIncomingDamage` implementation, not part of this reuse point's contract — a future tattoo built around
  a different `DamageDef` filters for its own defs the same way, independently.
- The exact numeric values (heat-resistance offsets, `ArmorRating_Heat` offsets, the Tier 2 full-ignore chance,
  the tier threshold) Ember Ward ships with are this feature's own placeholder balance numbers (PRD §9), not part
  of the reusable infrastructure.
- `HediffComp_EmberWardEffect` is Ember-Ward-specific — later features implement their own comps against the
  points above; they do not reuse Ember Ward's own comp.
- Whether a future tattoo built on this interface also wants an ongoing partial-reduction stat offset (as Ember
  Ward does) is that tattoo's own design decision — the two mechanisms are independent and composable, not a
  package deal.
