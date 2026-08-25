# Contract: Passive Tattoo Effect Infrastructure — Additions (Ranged Hits + CE Branching)

This extends feature 002's
[`passive-tattoo-effect-contract.md`](../../002-frost-sigil-tattoo-effect/contracts/passive-tattoo-effect-contract.md),
which remains in force **unmodified** — `TattooEffectValues`, `IProvidesTattooStatOffset`, `TattooTierProgress`,
and `StatPart_TattooEffectOffset` all mean exactly what that document says, and this feature is simply another
consumer of them (research.md R1). This document covers only what's new: the ranged-hit-landed reuse point and
the CE-branching convention, both intended for reuse by the remaining passive tattoos (Ember Ward, Ironskin
Glyph, Starlight Ward) exactly as feature 002's pieces are.

## 1. `IOnRangedHitLandedTattooEffect` — the ranged-attacker-side counterpart to `IOnMeleeHitTattooEffect`

```
public interface IOnRangedHitLandedTattooEffect
{
    void OnRangedHitLanded(Thing target, DamageInfo dinfo, float damageDealt);
}
```

- Any `HediffComp` on a pawn implementing this interface is invoked by the same shared
  `Patch_Pawn_PostApplyDamage_TattooOnHit` postfix feature 002 built — a new sibling dispatch clause, not a new
  patch — whenever **that pawn's own attack** (not a melee attack, i.e. `dinfo.Tool == null`) lands on something
  and deals damage (research.md R2).
- **Guarantee**: Called at most once per qualifying landed hit, with the `Thing` that was hit, the `DamageInfo`
  that produced it, and the damage actually dealt; never called for a miss, a fully-absorbed (zero-damage) hit,
  a melee hit, or a hit whose instigator isn't the pawn holding this comp (in particular: never for turret-fired
  shots, since vanilla turrets are `Building`s, not `Pawn`s).
- **What is NOT guaranteed**: that the target is a `Pawn` (it can be any `Thing` — a wall, a turret, an animal);
  a tattoo that only cares about hitting pawns must check `target is Pawn` itself. Call order between multiple
  tattoos' comps implementing this interface on the same pawn is also not guaranteed, same caveat as
  `IOnMeleeHitTattooEffect`.
- Implementing this interface requires no changes to the shared Harmony patch or to Serpent's Eye's own comp —
  dispatch is by interface, not a hardcoded tattoo list, exactly like feature 002's melee contract.

## 2. `CombatExtendedInterop` — the CE-branching convention (FR-013 / User Story 3)

```
public static class CombatExtendedInterop
{
    public static readonly bool IsLoaded;
    public static readonly StatDef AimingAccuracy;
}
```

- Computed once, at startup, via `[StaticConstructorOnStartup]`. `IsLoaded` reflects whether Combat Extended
  (`packageId` `CETeam.CombatExtended`) is active for the current session; `AimingAccuracy` is CE's own
  accuracy-equivalent `StatDef`, resolved by name and `null` when CE is absent.
- **Guarantee**: Never throws, in either configuration. Recomputed fresh each session — correctly reflects CE
  being added or removed between saves (per this feature's own edge cases), never cached across a mod-list
  change within the same process in a stale way.
- **The convention a future CE-aware tattoo follows**: check `CombatExtendedInterop.IsLoaded` before doing
  anything CE-specific; resolve any additional CE-only `StatDef`s the same way (`DefDatabase<StatDef>
  .GetNamedSilentFail("...")`), adding them to this class rather than re-implementing detection inline in a
  tattoo's own comp. A `StatDef` this class exposes is only ever non-null when CE is actually active, so
  callers do not need to separately check `IsLoaded` before using a resolved `StatDef` field — a `null` check
  on the field itself is sufficient and equivalent.
- **What this does NOT cover**: it does not provide a general mechanism for reflecting into
  `CombatExtended.dll`'s own types/methods (needed for genuinely novel CE mechanics with no `StatDef`
  equivalent, like Tier 2's ammo-effectiveness bonus — research.md R5). That remains a one-off, per-feature
  concern; see §3.

## 3. Reflection-based CE assembly patches — the pattern, not a reusable type

Unlike §1–2, this is **not** a reusable class future tattoos call into — CE's damage/projectile internals don't
expose a generic hook like `IProvidesTattooStatOffset` does for stats. It's documented here as the *pattern* a
future tattoo needing similarly deep CE integration (i.e. something with no corresponding `StatDef`) should
follow, demonstrated by `Patch_CE_ProjectileImpact_TattooAmmoBonus`:

1. Never reference the CE type at compile time; resolve it via `AccessTools.TypeByName("CombatExtended.Whatever")`
   at runtime.
2. Gate the entire patch behind `CombatExtendedInterop.IsLoaded` — do not even attempt the type resolution when
   CE is absent.
3. Apply the patch imperatively (`harmony.Patch(...)`) from `TattooMagicMain`'s static constructor, since
   `PatchAll()`'s attribute scan cannot discover a target type that doesn't exist at compile time.
4. Prefer mutating a single, per-event instance (here, one in-flight `ProjectileCE`) over shared/cached data
   (`ThingDef`, `AmmoDef`) — a future tattoo following this pattern for a different CE mechanic should look for
   the same kind of instance-scoped hook rather than patching shared definitions.

## 4. What is explicitly NOT part of this contract

- The exact numeric values Serpent's Eye ships with (accuracy/sway-recoil bonuses per tier, the ammo-
  effectiveness multiplier, the tier threshold) are this feature's own placeholder balance numbers (PRD §9), not
  reusable infrastructure.
- `HediffComp_SerpentsEyeEffect` and `Patch_CE_ProjectileImpact_TattooAmmoBonus` are Serpent's Eye-specific —
  later features implement their own comps/patches against the points above; they do not reuse Serpent's Eye's
  own comp or its specific CE patch.
- Which vanilla/CE `StatDef` a *future* tattoo's accuracy-like bonus should target is not decided here — R4's
  `ShootingAccuracyPawn`/`AimingAccuracy` split is specific to what those two stats mean, confirmed by reading
  CE's own Defs; a future tattoo touching different stats must do the same kind of verification, not assume this
  feature's specific mapping generalizes.
