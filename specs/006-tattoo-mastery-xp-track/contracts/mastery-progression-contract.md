# Contract: Mastery-Progression Hook on the Shared Gizmo Reuse Point

This is a small, generic extension to feature 004's
[`triggered-tattoo-effect-contract.md`](../../004-guardians-call-tattoo-effect/contracts/triggered-tattoo-effect-contract.md)
§1 (`IProvidesTattooGizmo` / `Patch_Pawn_GetGizmos_TattooGizmos`) — that contract remains in force **unmodified**
for how a triggered tattoo contributes a gizmo. This document describes what the shared patch additionally does
with the `Command_Action` it collects, and what that means for any triggered tattoo, present or future
(Bloodrune, Wraithstep, Berserker's Mark).

## 1. What changes for a triggered tattoo's own comp

**Nothing.** A `HediffComp` implementing `IProvidesTattooGizmo` per the feature 004 contract needs no new
method, no new field, and no new interface to participate in tattoo-mastery progress. If `GetGizmo()` returns a
`Command_Action` (as every triggered tattoo shipped so far does — Guardian's Call, Stormlash), that gizmo's
click is automatically counted as one tattoo-mastery qualifying event for the pawn that owns it, the moment the
comp is picked up by `Patch_Pawn_GetGizmos_TattooGizmos`. No per-tattoo opt-in code, no registration call, no
change to `HediffComp_GuardiansCallEffect.cs` or `HediffComp_StormlashEffect.cs`.

## 2. What the shared patch guarantees

- The mastery-progression call fires **after** the comp's own original `action` delegate runs, so a tattoo's own
  tier-progression counter (if it has one, per feature 004 contract §2 step 5) and the pawn-wide mastery
  counter always advance together for the same activation — never one without the other.
- The mastery-progression call is looked up fresh via `TattooTrackerUtility.GetTracker(pawn)` each time the
  wrapped action fires (not cached), consistent with every other tattoo-tracker lookup in this codebase.
- If the comp's `GetGizmo()` returns something other than a `Command_Action` (no such implementer exists today),
  the patch leaves that gizmo's action completely untouched — it is simply not counted. A future non-`Command_
  Action` triggered ability would need this contract extended, which is an explicit, reviewable change to this
  one shared patch, not a silent gap.
- Because the gizmo's own `Disabled` state (feature 004 contract §1) already prevents RimWorld from invoking a
  disabled action, an ability still on cooldown never contributes mastery progress — there is nothing extra for
  a triggered tattoo's own comp to guard against here.

## 3. What is explicitly NOT part of this contract

The two slot-unlock threshold values, the exact notification wording, and `HediffComp_TattooTracker`'s own
`RegisterMasteryActivation()` internals (data-model.md) are this feature's own implementation, not a reusable
interface — a future feature that wants to react to "a slot was just unlocked" would need its own extension
point, not something this contract provides today.

## 4. The passive-tattoo counterpart (added 2026-08-12, research.md R10)

A `HediffComp` implementing `IOnMeleeHitTattooEffect` or `IOnRangedHitLandedTattooEffect` per feature 002/003's
own contract (`Patch_Pawn_PostApplyDamage_TattooOnHit`) also needs **no new method, field, or interface** to
participate in tattoo-mastery progress — the same zero-opt-in guarantee §1 makes for triggered tattoos. Every
dispatched `OnMeleeHitTaken`/`OnRangedHitLanded` call now also credits one tattoo-mastery qualifying event to the
comp-owning pawn (the struck pawn for melee, the attacker for ranged), immediately after that call, before the
dispatch loop moves to the next comp.

**What's different from §1-2's triggered-tattoo behavior**: this credits mastery progress on *every dispatched
notification*, regardless of whether the tattoo's own effect internally procs (e.g. Frost Sigil's slow chance).
A triggered tattoo's gizmo already gates on cooldown before the wrapped action can even fire (§2); a passive
tattoo's on-hit dispatch has no equivalent gate at this shared point — that gating, where a tattoo has any, is
private to its own comp (research.md R10). A future passive tattoo that wants proc-accurate mastery contribution
would need this contract extended (e.g. a return value), which — like a future non-`Command_Action` triggered
ability (§2) — is an explicit, reviewable change to this shared patch, not a silent gap.
