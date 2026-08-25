# Contract: Passive Tattoo Effect Infrastructure — Additions (Melee-Attacker-Side Hit Landed + Kill Detection, Shared Healing Helper)

This extends feature 002's
[`passive-tattoo-effect-contract.md`](../../002-frost-sigil-tattoo-effect/contracts/passive-tattoo-effect-contract.md)
and feature 003's
[`passive-tattoo-effect-contract.md`](../../003-serpent-eye-tattoo-effect/contracts/passive-tattoo-effect-contract.md),
both of which remain in force **unmodified** — `TattooEffectValues`, `IProvidesTattooStatOffset`,
`TattooTierProgress`, `StatPart_TattooEffectOffset`, `IOnRangedHitLandedTattooEffect`, and
`CombatExtendedInterop` all mean exactly what those documents say, and this feature is simply another consumer of
the first three. This document covers only what's new: the melee-attacker-side "hit landed" reuse point (with its
built-in kill-blow signal) and a shared wound-healing helper, both intended for reuse by future melee-themed
tattoo effects and any tattoo needing to heal a pawn's injuries, exactly as feature 002's and 003's own pieces are.

## 1. `IOnMeleeHitLandedTattooEffect` — the melee-attacker-side counterpart to `IOnRangedHitLandedTattooEffect`

```csharp
public interface IOnMeleeHitLandedTattooEffect
{
    void OnMeleeHitLanded(Thing target, DamageInfo dinfo, float damageDealt, bool killedTarget);
}
```

- Any `HediffComp` on a pawn implementing this interface is invoked by the same shared
  `Patch_Pawn_PostApplyDamage_TattooOnHit` postfix features 002/003 built — a new sibling dispatch clause inside
  the existing melee branch, not a new patch — whenever **that pawn's own melee attack** (`dinfo.Tool != null`)
  lands on something and deals damage (research.md R2). This fires *in addition to*, not instead of, the existing
  `IOnMeleeHitTattooEffect.OnMeleeHitTaken` notification the struck pawn's own comps already receive for the same
  hit — both are notifications about the same event, from opposite sides of it.
- **Guarantee**: Called at most once per qualifying landed melee hit, with the `Thing` that was hit, the
  `DamageInfo` that produced it, the damage actually dealt, and whether this specific hit was the one that killed
  the target (`killedTarget`); never called for a miss, a fully-absorbed (zero-damage) hit, a ranged hit, or a hit
  whose instigator isn't the pawn holding this comp.
- **`killedTarget` semantics**: `true` only when `target.Dead` reads `true` immediately after this hit resolved —
  confirmed, by inspection of RimWorld's actual `Assembly-CSharp.dll`, to be reliably observable at this point
  because `Pawn.PostApplyDamage`'s own internal call chain (`health.PostApplyDamage` → `pawn.Kill`, when lethal)
  completes synchronously before a Harmony postfix on that method ever runs (research.md R4). No separate "on
  kill" patch or interface exists or is needed — this is the one place a future tattoo should read a melee
  killing-blow signal from.
- **What is NOT guaranteed**: that the target is a `Pawn` in the interface's own type signature (it's typed
  `Thing` for symmetry with `IOnRangedHitLandedTattooEffect`), though in practice, for *this* dispatch site, it
  always is (research.md R3 — `Pawn.PostApplyDamage` only runs when the damaged `Thing` is a `Pawn`). Call order
  between multiple tattoos' comps implementing this interface on the same pawn is also not guaranteed, same
  caveat as every other reuse point in this contract family.
- Implementing this interface requires no changes to the shared Harmony patch or to Vampiric Thorn's own comp —
  dispatch is by interface, not a hardcoded tattoo list, exactly like every prior reuse point in this family.

## 2. `TattooHealingUtility` — the shared wound-healing approach

```csharp
public static class TattooHealingUtility
{
    public static float HealWorstInjury(Pawn pawn, float amount);
}
```

- Heals `pawn`'s currently most-severe open, non-permanent `Hediff_Injury` by up to `amount`; if `amount` isn't
  fully consumed by that injury closing, the remainder rolls onto the next-most-severe open injury within the
  same call, repeating until `amount` is exhausted or nothing is left to heal (research.md R5).
- **Guarantee**: Never throws for a pawn with no open injuries — returns `0f` and does nothing observable. Never
  over-heals a single injury past closed (each application is capped by `Mathf.Min(remaining, injury.Severity)`,
  identical to Bloodrune's own original per-call cap). Terminates in bounded time regardless of `amount` — the
  worst-injury selection requires `Severity > 0f`, so a just-closed injury can never be re-selected, and each loop
  iteration strictly consumes some of `amount` (research.md R5's hotfix note; an earlier version without this
  guard could hang indefinitely once `amount` exceeded a pawn's total remaining open-injury severity).
- **Return value**: the amount of `amount` actually applied (`<= amount`; less than `amount` only when the pawn
  ran out of open injuries to absorb the rest).
- **What this does NOT do**: apply healing over time by itself — it's a single, synchronous operation. A caller
  wanting a burst/window (as Bloodrune's and Vampiric Thorn's own Tier 2 post-kill window both do) supplies its
  own tick-interval scheduling (`CompPostTick` + a remaining-budget field) and calls this helper once per
  interval with that interval's portion, exactly as `HediffComp_BloodruneEffect.HealTick()` and
  `HediffComp_VampiricThornEffect`'s own post-kill tick both do.
- Consumed by both `HediffComp_BloodruneEffect` (updated to call this instead of its own inline scan, research.md
  R5) and `HediffComp_VampiricThornEffect` (this feature's own comp) — the second real consumer that justified
  extracting it out of Bloodrune's file in the first place.

## 3. What is explicitly NOT part of this contract

- The exact numeric values Vampiric Thorn ships with (per-tier lifesteal amounts, the post-kill heal amount/
  duration/interval, the tier threshold) are this feature's own placeholder balance numbers (PRD §9), not reusable
  infrastructure.
- `HediffComp_VampiricThornEffect` is Vampiric-Thorn-specific — later features implement their own comps against
  the points above; they do not reuse Vampiric Thorn's own comp.
- Whether a future melee-themed tattoo should act on `killedTarget` at all, or at which tier, is that tattoo's own
  design decision — `IOnMeleeHitLandedTattooEffect` simply makes the signal available on every call; nothing about
  the interface requires a consumer to use it, the same way `IOnRangedHitLandedTattooEffect`'s consumers aren't
  required to inspect every field of the `DamageInfo` they receive.
