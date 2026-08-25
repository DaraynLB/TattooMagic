# Contract: Cell-Targeted Triggered Abilities (and the Mastery-Progress Opt-Out)

Documents the pattern Wraithstep establishes for a triggered tattoo whose gizmo requires the player to pick a
destination before it fires — the first of its kind in this mod (every prior triggered tattoo's gizmo click *is*
its complete activation). This is what a future targeted ability (e.g. a hypothetical projectile- or
area-targeted tattoo) follows if it needs the same two-phase shape, per this project's established
reuse-when-proven convention.

## 1. The two-phase gizmo shape

- `GetGizmo()` still returns a plain `Command_Action` via `IProvidesTattooGizmo` (feature 004, unmodified) — this
  part of the contract does not change. What changes is what the `action` delegate *does*.
- The `action` delegate only **opens** a targeting session (`Find.Targeter.BeginTargeting`) — it MUST NOT itself
  apply the ability's effect, start its cooldown, or advance any progression counter.
- The real activation logic lives in the `action`-callback `Find.Targeter.BeginTargeting` invokes once the player
  confirms a valid target (per that call's own `validator`) — this is the only place cooldown, tier-progression, and
  mastery-progress state may advance for this ability.
- A **cancelled** targeting session (Escape/right-click, or any other path that ends the session without the
  primary callback firing) MUST leave the ability's cooldown, tier counter, and mastery progress completely
  unchanged — a gizmo click alone, without a confirmed target, is never itself an "activation."

## 2. The mastery-progress opt-out (`IHandlesOwnMasteryProgress`)

`mastery-progression-contract.md` (feature 006) §2 credits mastery progress the moment a collected `Command_Action`'s
`action` delegate is invoked — correct for every synchronous, one-click ability, but wrong for a two-phase ability
per §1 above (it would credit progress on a click alone, before — or even without — a confirmed activation).

`Patch_Pawn_GetGizmos_TattooGizmos` (feature 004/006) is extended with one small, fully generic condition: it skips
its automatic mastery-wrap for any `Command_Action` whose owning comp implements `IHandlesOwnMasteryProgress` (a
marker interface, no members, `Source/Effects/IHandlesOwnMasteryProgress.cs`). A comp implementing this marker MUST
call `TattooTrackerUtility.GetTracker(pawn)?.RegisterMasteryActivation()` itself, exactly once, at the same point it
advances its own tier-progression counter (i.e. only on a confirmed, successful activation per §1) — never more,
never less.

This is a **generic** extension (checked by interface, not by comp type), not "Wraithstep-specific code" — it
satisfies feature 007's FR-020-equivalent convention of not requiring per-tattoo special-casing inside shared
files, and generalizes automatically to any future ability with the same two-phase shape.

## 3. What a future cell/target-selecting tattoo reuses from this

- The **shape** (`Command_Action` opens targeting → confirmed-target callback does the real work → cancelled
  sessions are inert) and the `IHandlesOwnMasteryProgress` opt-out — both fully generic, no Wraithstep-specific
  coupling.
- **What is explicitly NOT reusable**: `HediffComp_WraithstepEffect`'s own `IsValidDestination`/`GetRejectReason`
  validity rules (walkable/unfogged/unoccupied/range) are Wraithstep-specific to a plain-cell-teleport ability — a
  future targeted ability with different targeting rules (e.g. "must target a hostile pawn," "must have line of
  sight") implements its own `TargetingParameters`/validator against this same two-phase shape, rather than
  extending Wraithstep's own comp.
- The live range-ring/reject-reason feedback (FR-003) needs no shared infrastructure at all, and no Harmony patch:
  `RimWorld.Targeter.BeginTargeting`'s own richest overload (research.md R3) already accepts `onUpdateAction`/
  `onGuiAction` callback delegates fired every frame for the duration of that specific `BeginTargeting` call. A
  future targeted ability that wants the same live feedback simply passes its own closures into its own
  `BeginTargeting` call — there is no session-tracking class to extend or share.
