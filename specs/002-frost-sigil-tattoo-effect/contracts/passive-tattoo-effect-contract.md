# Contract: Passive Tattoo Effect Infrastructure

This feature's "external interface" is the small set of reusable types it introduces for wiring up a
**passive** tattoo's gameplay effect (per PRD §5.4/§6). A later feature adding one of the 5 remaining
passive tattoos (Ember Ward, Ironskin Glyph, Serpent's Eye, Starlight Ward, Vampiric Thorn) can rely on
everything documented here without reading this feature's Frost Sigil-specific C#.

**Not covered by this contract**: the triggered/gizmo-ability pattern (Bloodrune, Stormlash, Wraithstep,
Berserker's Mark, Guardian's Call). Those need on-demand activation, a cooldown, and a use-count progression
counter — a different shape this feature does not build. See `spec.md` Assumptions.

## 1. `TattooEffectValues` — the value accessor (FR-012)

```
public static class TattooEffectValues
{
    public static float Get(string scopeKey, string valueKey, float xmlDefault);
}
```

- **Guarantee**: Returns `xmlDefault` until something sets an override. No setter exists yet, and this
  feature never calls one — that's a future settings-UI feature's job (PRD §10).
- A later tattoo's effect code MUST read every tunable numeric value through this accessor (not directly off
  its own Def/Comp field) at the point that value is used, using its own `HediffDef.defName` as `scopeKey`.
- **What is NOT guaranteed**: thread-safety, persistence across sessions, or any particular internal storage
  shape — only the `Get(...)` signature and its "override wins, else default" behavior are contract.

## 2. `IProvidesTattooStatOffset` — live stat-bonus contract

```
public interface IProvidesTattooStatOffset
{
    float GetStatOffset(StatDef stat);
}
```

- Any `HediffComp` implementing this interface is automatically summed into `StatPart_TattooEffectOffset`'s
  contribution for whichever `StatDef`s that `StatPart` has been registered on — **currently
  `ComfyTemperatureMin` and `MoveSpeed` only** (registered by this feature at startup). A tattoo needing a
  stat bonus on a *different* stat MUST register `StatPart_TattooEffectOffset` on that stat too (one line at
  startup, per `research.md` R3) before implementing this interface against it — the interface alone does
  not make an arbitrary stat tattoo-aware.
- **Guarantee**: The `StatPart` calls `GetStatOffset` on every live-relevant stat query; implementers should
  return `0f` for any `StatDef` they don't care about, not throw.
- Implementing this interface requires no changes to `StatPart_TattooEffectOffset` or to any other tattoo's
  comp — this is what FR-010's reuse claim rests on for stat bonuses.

## 3. `IOnMeleeHitTattooEffect` — on-hit-proc contract

```
public interface IOnMeleeHitTattooEffect
{
    void OnMeleeHitTaken(Pawn attacker, DamageInfo dinfo, float damageDealt);
}
```

- Any `HediffComp` on a pawn implementing this interface is invoked by the single shared
  `Patch_Pawn_PostApplyDamage_TattooOnHit` postfix whenever that pawn is struck by a melee attack that dealt
  damage (`research.md` R2).
- **Guarantee**: Called at most once per qualifying hit, with the real attacking `Pawn` and the `DamageInfo`
  that produced the hit; never called for non-melee damage or a fully-absorbed (zero-damage) hit.
- **What is NOT guaranteed**: call order between multiple tattoos' comps implementing this interface on the
  same pawn — a future tattoo relying on ordering relative to another must not assume one.
- Implementing this interface requires no changes to the shared Harmony patch or to Frost Sigil's comp — the
  patch dispatches generically by interface, not by a hardcoded list of tattoo types.

## 4. `TattooTierProgress` — tier/counter state (composed, not inherited)

```
public class TattooTierProgress
{
    public int tier = 1;
    public int progressionCounter = 0;
    public void ExposeData();
    public bool TryRegisterQualifyingEvent(string thresholdKey, int defaultThreshold);
}
```

- A future passive tattoo's comp composes one of these as a plain field (`public TattooTierProgress
  tierState = new TattooTierProgress();`) rather than inheriting from a shared `HediffComp` base class — see
  `research.md` R5 for why composition was chosen over inheritance.
- **Guarantee**: `TryRegisterQualifyingEvent` increments `progressionCounter`, checks it against
  `TattooEffectValues.Get(scopeKey, thresholdKey, defaultThreshold)`, and flips `tier` from `1` to `2` in
  place the first time the threshold is reached — never past `2`, and never decrements. Returns `true` only
  on the call where the tier-up actually happens, `false` otherwise (including all calls after Tier 2 is
  already reached).
- The owning comp is responsible for calling `ExposeData()` from its own `CompExposeData()` override — this
  class does not self-persist.

## 5. What is explicitly NOT part of this contract

- The exact numeric values Frost Sigil ships with (cold resistance amount, slow chance/magnitude, freeze
  chance/duration, tier threshold) are this feature's own placeholder balance numbers (PRD §9), not part of
  the reusable infrastructure.
- `HediffComp_FrostSigilEffect` and `TattooMagic_Hediff_FrostSigilSlow` are Frost Sigil-specific — later
  features implement their own comps/hediffs against the four contract points above, they do not subclass or
  reuse Frost Sigil's own comp.
- The triggered/gizmo-ability pattern (§ header note above) — not designed by this feature at all.
