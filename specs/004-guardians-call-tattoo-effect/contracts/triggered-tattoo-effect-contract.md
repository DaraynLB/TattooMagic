# Contract: Triggered Tattoo Effect Infrastructure (Gizmo + Cooldown Reuse Point)

This is a new sibling to feature 002's
[`passive-tattoo-effect-contract.md`](../../002-frost-sigil-tattoo-effect/contracts/passive-tattoo-effect-contract.md)
(extended by feature 003's) — that document and everything it covers remains in force **unmodified**.
Guardian's Call is the first **triggered** tattoo; this document is what a future triggered tattoo (Bloodrune,
Stormlash, Wraithstep, Berserker's Mark — spec User Story 3) builds against for "an activatable ability with a
cooldown," without touching Guardian's Call's own files.

## 1. `IProvidesTattooGizmo` — the gizmo-contribution contract

```csharp
public interface IProvidesTattooGizmo
{
    Gizmo GetGizmo();
}
```

- Implemented by any `HediffComp` on a pawn that wants to contribute one gizmo to that pawn's gizmo row.
- Invoked by the shared `Patch_Pawn_GetGizmos_TattooGizmos` postfix on `Pawn.GetGizmos()` — called every time
  the game asks for that pawn's gizmos (i.e. on selection / UI refresh, not per-tick), alongside vanilla's own.
- **Guarantee**: `GetGizmo()` is called fresh each time gizmos are requested — implementations MUST compute
  `disabled`/`disabledReason`/icon state live from their own persisted fields (e.g. a stored `cooldownEndTick`
  compared against `Find.TickManager.TicksGame`) rather than caching a `Gizmo` instance, since the disabled
  state can change between calls.
- **What is NOT guaranteed**: gizmo ordering relative to vanilla's own gizmos or another tattoo's gizmo; a
  pawn with two different triggered tattoos gets two independent gizmos with no defined relative order.
- Implementing this interface requires no change to the shared patch or to any other tattoo's comp — dispatch
  is by interface over the pawn's hediff comps, identical in spirit to `IOnMeleeHitTattooEffect`
  (feature 002) and `IOnRangedHitLandedTattooEffect` (feature 003).

## 2. The cooldown/activation shape a triggered tattoo's own comp should follow

Not an interface (a cooldown gate is internal per-tattoo state, not something the shared patch needs to see),
but the pattern Guardian's Call establishes and a future triggered tattoo should mirror:

1. Persist a `cooldownEndTick` (`int`) via `Scribe_Values.Look` in `CompExposeData()`.
2. On activation: bail out (no-op) if `Find.TickManager.TicksGame < cooldownEndTick`; otherwise perform the
   effect and set `cooldownEndTick = Find.TickManager.TicksGame + <accessor-resolved duration>`.
3. Expose that state to the player via `IProvidesTattooGizmo.GetGizmo()` (§1) — a `Command_Action` (or other
   `Gizmo` subclass) whose `disabled`/`disabledReason` are derived from the same `cooldownEndTick` field.
4. Read every tunable (cooldown length, any per-use magnitude) through `TattooEffectValues.Get` (feature 002),
   never a raw XML/Props field at the decision point — unchanged FR-011/FR-012 obligation.
5. If the ability has its own tier-progression counter (PRD §5.4's "times activated" pattern), compose a
   `TattooTierProgress` (feature 002, unmodified) exactly as a passive tattoo's comp does, incrementing it
   once per successful activation.

**What is explicitly NOT part of this contract**: Guardian's Call's own taunt-targeting logic, its
`GuardiansCallTauntRegistry`, and its specific armor-buff StatDef choices are tattoo-specific, not reusable —
see `two-path-targeting-contract.md` for what *is* reusable from the targeting side. The exact numeric values
Guardian's Call ships with (range, durations, cooldown, threshold, buff magnitude) are this feature's own
placeholder balance numbers (PRD §9), not part of this contract either.
