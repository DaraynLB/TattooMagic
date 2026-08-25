# Contract: Stat-Offset Immunity (Negative-Offset Veto Reuse Point)

This is a new sibling to feature 002's
[`passive-tattoo-effect-contract.md`](../../002-frost-sigil-tattoo-effect/contracts/passive-tattoo-effect-contract.md)
and feature 004's
[`triggered-tattoo-effect-contract.md`](../../004-guardians-call-tattoo-effect/contracts/triggered-tattoo-effect-contract.md) —
both documents and everything they cover remain in force **unmodified**. This document covers only what's new:
a way for one tattoo's effect to grant genuine (not just bigger-number) immunity to another effect's negative
contribution to a shared stat, and what Stormlash specifically does with Combat Extended's suppression-slow,
which sits entirely outside this mechanism (§3).

Stormlash is the first consumer. A future tattoo needing the same kind of "temporarily veto negative
contributions to a stat" behavior builds against this document, without touching Stormlash's own files.

## 1. `IGrantsStatOffsetImmunity` — the negative-offset veto contract

```csharp
public interface IGrantsStatOffsetImmunity
{
    bool BlocksNegativeOffsets(StatDef stat);
}
```

- Implemented by any `HediffComp` that wants to veto *other* comps' negative `IProvidesTattooStatOffset`
  contributions to a specific `StatDef`, for as long as some condition of its own holds (e.g. an active buff
  window).
- Invoked by `StatPart_TattooEffectOffset` (§2) once per stat query, alongside its existing
  `IProvidesTattooStatOffset` sweep — not a separate patch or dispatch point.
- **Scope**: per-stat, not global. Returning `true` for `StatDefOf.MoveSpeed` has no effect on any other
  `StatDef`; a comp must return `true` specifically for the `StatDef` it wants to protect.
- **What it does NOT do**: it does not affect the implementing comp's *own* `GetStatOffset` contribution (still
  summed normally, including if that comp itself somehow returned a negative value for the same stat — this
  contract only vetoes *other* comps), and it does not affect positive contributions from any comp (only
  negative ones are clamped).
- Implementing this interface requires no change to `StatPart_TattooEffectOffset`'s registration
  (`TattooEffectStatPartInstaller`) or to any other tattoo's comp — dispatch is by interface over the pawn's
  hediff comps, identical in spirit to `IProvidesTattooStatOffset` itself.

## 2. `StatPart_TattooEffectOffset.GetOffset` — the updated summation shape

Still one generic, reusable `StatPart` per registered `StatDef` (feature 002), still carrying no tattoo-specific
knowledge. Its per-query algorithm is now:

1. Walk the pawn's `HediffWithComps` comps once; if any implements `IGrantsStatOffsetImmunity` and its
   `BlocksNegativeOffsets(parentStat)` returns `true`, note `vetoActive = true` for this query.
2. Walk the pawn's `IProvidesTattooStatOffset` comps (unchanged from feature 002) and sum `GetStatOffset(parentStat)`
   across all of them — **except**, if `vetoActive`, each individual contribution is first clamped to
   `Mathf.Max(0f, offset)` before being added.

Consequence: a comp granting immunity for `MoveSpeed` neutralizes *any* other comp's negative `MoveSpeed`
contribution present on the same pawn at query time — it does not need to know which comp(s), if any, are
currently contributing a negative offset, and they do not need to know immunity exists.

**What is explicitly NOT part of this contract**: this only covers negative contributions that already route
through `IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset` — i.e. this mod's own stat-offset mechanism.
It has no effect on a stat-reducing mechanism that computes its result some other way (a different mod's own
`StatWorker` or `StatPart`, a hediff's own `statOffsets`/`statFactors` XML, etc.). §3 covers the one such
external mechanism this feature specifically neutralizes (Combat Extended's suppression-slow), via a completely
different technique, precisely because it does *not* go through this contract.

## 3. Combat Extended's suppression-slow — why it's a separate patch, not a veto

Grounded in research.md R4: Combat Extended installs its own `StatWorker` on vanilla's `MoveSpeed` StatDef,
which multiplies the *unfinalized* value by `0.67f` when `CompSuppressable.IsCrouchWalking` is true. This
multiplication happens **before** any `StatDef.parts` (including `StatPart_TattooEffectOffset`, and therefore
§1/§2's veto mechanism) ever run — so nothing built against §1 can reach it; a veto only ever clamps
contributions inside the parts pipeline, and CE's suppression penalty was never inside that pipeline to begin
with.

Instead, `Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity` (Stormlash-specific, not part of this
reusable contract — data-model.md) neutralizes CE's multiplier at its source: a Harmony postfix on
`CompSuppressable.IsCrouchWalking`'s getter forcing `__result = false` while the queried pawn has an active
Tier 2 Stormlash boost. A future tattoo needing the same kind of "neutralize a non-`IProvidesTattooStatOffset`
external slow mechanism" behavior should expect to add its own targeted patch the same way, rather than trying
to route it through §1/§2 — the veto contract and this technique solve two different problems (this mod's own
internal offset stacking, vs. an external mod's own stat-computation pipeline) that happen to produce a similar
player-facing result.
