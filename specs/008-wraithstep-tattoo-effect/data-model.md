# Phase 1 Data Model: Wraithstep Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain, feature 002's `TattooEffectValues` /
`TattooTierProgress`, and feature 004's `IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos`, all reused
unmodified in shape (spec FR-012/FR-014/FR-015). Two existing shared files gain a small, fully generic extension
(not Wraithstep-specific code, per FR-020): `Patch_Pawn_GetGizmos_TattooGizmos.cs` (research.md R4) and a new Prefix
alongside (not replacing) `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs` (research.md R6). No
existing tattoo's own file is modified.

## Entities

### `HediffCompProperties_WraithstepEffect`

XML-facing properties on `Defs/HediffDefs/Tattoos/Wraithstep.xml`'s comp list — every field is only ever read as an
`xmlDefault` fallback through `TattooEffectValues.Get` (FR-013), never directly at a decision point.

| Field | Type | Meaning |
|---|---|---|
| `rangeTier1` / `rangeTier2` | `float` | Maximum selectable blink distance (cells), each tier's own value (FR-007/FR-008). |
| `cooldownDurationTicksTier1` / `cooldownDurationTicksTier2` | `int` | Ticks after a successful blink before the gizmo can be used again. |
| `untargetableWindowTicksTier2` | `int` | Tier-2-only duration, starting the instant a blink resolves, during which the wearer is excluded from hostile targeting (FR-009). No Tier 1 equivalent — Tier 1 grants no exclusion at all. |
| `tier2Threshold` | `int` | Successful-activation count at which Tier 1 auto-upgrades to Tier 2 (PRD §5.4 pattern, same shape as every prior tiered tattoo). |
| `abilityIconPath` | `string` | Texture path for the gizmo icon; falls back to a placeholder if unset, same convention as every prior triggered tattoo. |

### `HediffComp_WraithstepEffect`

The new tattoo-specific comp, attached via `TattooMagic_Hediff_Wraithstep`'s `<comps>` list. Implements
`IProvidesTattooGizmo` (reused, feature 004) and the new `IHandlesOwnMasteryProgress` marker (research.md R4).

| Field | Type | Persistence | Meaning |
|---|---|---|---|
| `tierState` | `TattooTierProgress` (reused, unmodified) | `ExposeData()` from `CompExposeData()` | Tier + successful-activation-count progression, identical mechanism to every prior tiered tattoo. |
| `cooldownEndTick` | `int` | `Scribe_Values.Look` | Game tick after which the gizmo is usable again. |
| `untargetableEndTick` | `int` | `Scribe_Values.Look` | Game tick after which the Tier 2 targeting exclusion (if any is active) ends. `0`/already-past at Tier 1 or between blinks. |

Behavior:

- `GetGizmo()` → builds and returns one `Command_Action` each call (icon, label "Wraithstep", `action =>
  BeginTargetSelection()`, `disabled`/`disabledReason` computed live from `cooldownEndTick`) — same outward shape as
  every prior triggered tattoo's gizmo. Unlike them, this `action` does not itself relocate the pawn (research.md R1).
- `BeginTargetSelection()` → no-ops if still on cooldown (defensive; the gizmo already prevents this). Otherwise
  calls `Find.Targeter.BeginTargeting(targetParams, action: OnDestinationConfirmed, highlightAction: null,
  targetValidator: IsValidDestination, caster: Pawn, onGuiAction: DrawRejectReasonLabel, onUpdateAction:
  DrawRangeRing)` — `RimWorld.Targeter`'s own richest overload (research.md R3, confirmed via decompiled source),
  where `targetParams` is built for cell-only targeting (`canTargetLocations = true`, `canTargetPawns = false`,
  `canTargetSelf = false`). No separate session-tracking state is needed: the two draw callbacks are plain closures
  passed directly into this one call.
- `IsValidDestination(LocalTargetInfo target)` → `bool`; the single source of truth for FR-002, checked in this
  order: `target.Cell.InBounds(map)`, `!target.Cell.Fogged(map)` (explored), `target.Cell.Walkable(map)`,
  `target.Cell.GetFirstPawn(map) == null` (unoccupied), and `(target.Cell - Pawn.Position).LengthHorizontalSquared
  <= Range * Range`. Implemented as `GetRejectReason(target) == null` so the two can never disagree (research.md R3).
- `GetRejectReason(LocalTargetInfo target)` → `string`, evaluating the same checks as `IsValidDestination` in the
  same order and returning the first one that fails as a short player-facing string (e.g. "Out of range.", "Not
  explored.", "Can't stand there.", "Occupied."); `null` when the target is valid. Consumed by
  `DrawRejectReasonLabel` for FR-003's live reject-reason feedback.
- `DrawRangeRing(LocalTargetInfo target)` → `Targeter`'s `onUpdateAction` callback (research.md R3); draws
  `GenDraw.DrawRadiusRing(Pawn.Position, Range)` every Update frame the targeter is open, tracking the wearer's
  current position live.
- `DrawRejectReasonLabel(LocalTargetInfo target)` → `Targeter`'s `onGuiAction` callback (research.md R3); calls
  `GenUI.DrawMouseAttachment(null, rejectReason)` — RimWorld's own stock mouse-following label widget — whenever
  `GetRejectReason(target)` is non-null for whatever cell is currently under the mouse.
- `OnDestinationConfirmed(LocalTargetInfo target)` → the actual activation, called only for a destination
  `IsValidDestination` already accepted (FR-002; `Find.Targeter` will not invoke this for a rejected cell). A
  cancelled session (Escape/right-click, or any other path that ends targeting without a confirmed cell) simply
  never calls this method — no relocation, no cooldown, no tier-progression increment, and no mastery credit occur
  (spec Edge Cases, FR-002, SC-005), with no separate cleanup step required (`Targeter` itself simply stops invoking
  the callbacks once its session ends):
  1. `Pawn.Position = target.Cell;` then the pawn's own teleport-notification callback (research.md R5) so pathing/
     region/visual-tween state stays consistent.
  2. Sets `cooldownEndTick` from the accessor-resolved, tier-appropriate cooldown.
  3. If already at Tier 2, sets `untargetableEndTick = now + accessor-resolved UntargetableWindowTicksTier2` and
     registers this comp with `WraithstepUntargetableRegistry` (research.md R6); at Tier 1 this step is skipped
     entirely (no exclusion window exists yet).
  4. Calls `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)` exactly once
     (FR-006) — reads whichever tier's numbers were current *before* this call for steps 1-3, matching every prior
     tattoo's "an activation just before the upgrade uses the old tier's numbers" precedent.
  5. Calls `TattooTrackerUtility.GetTracker(Pawn)?.RegisterMasteryActivation()` exactly once (FR-019, research.md
     R4) — the manual mastery credit this comp is responsible for since it opts out of the shared patch's automatic
     click-time wrap.
- `IsCurrentlyUntargetable` (`bool`, computed, not persisted) → `Find.TickManager.TicksGame < untargetableEndTick`.
  Read by `WraithstepUntargetableRegistry`/the targeting-exclusion Prefix (research.md R6), not by anything on this
  comp itself.
- `ForceNearbyHostilesOffWearer(Pawn)` → the active half of the Tier 2 exclusion (research.md R6 addendum,
  confirmed necessary via live testing): for every hostile on the map whose `mindState.enemyTarget`,
  `mindState.meleeThreat`, or running `Job.targetA` currently is the wearer, nulls the two `mindState` fields and
  calls `hostile.jobs.EndCurrentJob(JobCondition.InterruptForced)`, forcing an immediate re-decision that then
  correctly runs through the (already-excluding) targeting search. Called once immediately in
  `OnDestinationConfirmed` (Tier 2 only) and again every `ReassertIntervalTicks` (30) from `CompPostTick` for the
  remainder of the active window — mirrors `HediffComp_GuardiansCallEffect`'s identical one-shot-plus-periodic-
  reassert shape for the same reason (a hostile can acquire the wearer through some path other than the excluded
  search at any point during the window, not just be locked on before it opens).
- `CompPostTick(ref float severityAdjustment)` → no-ops once `Find.TickManager.TicksGame >= untargetableEndTick`
  (the common case — outside an active window, or always at Tier 1); otherwise, every `ReassertIntervalTicks` ticks,
  calls `ForceNearbyHostilesOffWearer(Pawn)`.
- `Range` (`float`, computed) → accessor-resolved `RangeTier2` if `tierState.tier >= 2`, else `RangeTier1`.
- `CompExposeData()` → `base.CompExposeData()`, then `tierState.ExposeData()` and the `Scribe_Values.Look` calls
  for `cooldownEndTick`/`untargetableEndTick` (FR-010).

### `WraithstepUntargetableRegistry` (new, `Source/Hediffs/`, static)

Mirrors `GuardiansCallTauntRegistry`'s *shape* only (a lazily-pruned `HashSet`, per
`two-path-targeting-contract.md` §3's explicit guidance that a second targeting-influencing tattoo owns its own
registry rather than reusing Guardian's Call's) — tracks different state for a different purpose.

| Member | Meaning |
|---|---|
| `HasActiveUntargetable` (`bool`) | Cheap common-case bail for the hot-path Prefix (research.md R6) — `true` only while at least one comp's exclusion window is open anywhere. |
| `Register(HediffComp_WraithstepEffect comp)` | Called from `OnDestinationConfirmed` step 3 (Tier 2 only). |
| `IsUntargetable(Pawn pawn)` (`bool`) | Lazily prunes any comp whose pawn is dead/despawned/no longer carries the hediff or whose window has expired (`IsCurrentlyUntargetable == false`) at query time, mirroring `GuardiansCallTauntRegistry.ActiveTaunters()`'s own lazy-prune convention; returns whether `pawn` currently owns a still-active, still-valid entry. |

### `IHandlesOwnMasteryProgress` (new, `Source/Effects/`, marker interface, no members)

Implemented by `HediffComp_WraithstepEffect` only, today. Consumed by the one small, generic extension to
`Patch_Pawn_GetGizmos_TattooGizmos` (research.md R4): a collected `Command_Action` is auto-wrapped for mastery
credit (feature 006 behavior, unchanged for every other implementer) only when its owning comp does **not**
implement this marker.

### XML: `Defs/HediffDefs/Tattoos/Wraithstep.xml` (updated)

`hediffClass` stays plain `HediffWithComps` (no per-Hediff override needed, unlike Bloodrune's `PainOffset` case —
Wraithstep affects position and targeting eligibility, neither of which routes through a `Hediff`-level override).
Adds a `<comps>` entry:

```xml
<comps>
  <li Class="TattooMagic.HediffCompProperties_WraithstepEffect">
    <rangeTier1>6</rangeTier1>
    <rangeTier2>10</rangeTier2>
    <cooldownDurationTicksTier1>1200</cooldownDurationTicksTier1>
    <cooldownDurationTicksTier2>1200</cooldownDurationTicksTier2>
    <untargetableWindowTicksTier2>90</untargetableWindowTicksTier2>
    <tier2Threshold>15</tier2Threshold>
    <abilityIconPath>UI/Commands/Wraithstep</abilityIconPath>
  </li>
</comps>
```

All values placeholders pending PRD §9's balance pass (Constitution Principle III), consistent with every tattoo
shipped so far; small/fast-to-test rather than "plausible-looking" per feature 006 research.md R8 precedent.

## State Transitions

Tier transition: identical, unmodified `TattooTierProgress` shape as every prior tiered tattoo — `tier` starts at
`1`, `progressionCounter` increments once per **successfully-resolved** activation only (never on a cancelled or
rejected targeting attempt), flips to `tier = 2` exactly once when `progressionCounter` reaches the
accessor-resolved `Tier2Threshold` — idempotent, deterministic, Scribe-backed (Constitution Principle V).

New to this feature: a per-activation, time-boxed **untargetable window** (`untargetableEndTick`), Tier 2 only,
starting the instant a blink resolves — independent of the tier-progression counter itself, re-derived fresh from
whatever tier was current *before* that activation's `TryRegisterQualifyingEvent` call (same "an activation just
before the upgrade uses the old tier's numbers" precedent Guardian's Call/Stormlash/Bloodrune already establish). A
second blink while a previous window is still open overwrites `untargetableEndTick` outright (refresh, not stack),
consistent with those tattoos' precedent for their own timed windows.

A **targeting session** is owned entirely by `RimWorld.Targeter` itself (research.md R3) — this feature adds no
session-tracking state of its own, so there is nothing to persist or clear across a save/load boundary for it (no
targeting session can be "in progress" across a save boundary in the first place, since `Targeter` does not support
resuming a session after a scene reload).
