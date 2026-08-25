# Phase 0 Research: Wraithstep Tattoo Effect (Fourth Triggered Ability + First Cell-Targeted Ability Slice)

## R1: Reusing the triggered-tattoo gizmo/cooldown/tier shape, adapted for a two-phase (click-then-target) flow

**Decision**: `HediffComp_WraithstepEffect` implements `IProvidesTattooGizmo` and is picked up by the existing,
unmodified `Patch_Pawn_GetGizmos_TattooGizmos` (feature 004), holding a `TattooTierProgress` field (feature 002) and
its own `cooldownEndTick` int — the same shape `HediffComp_GuardiansCallEffect`, `HediffComp_StormlashEffect`, and
`HediffComp_BloodruneEffect` already establish. The one structural difference: those three tattoos' `GetGizmo()`
returns a `Command_Action` whose `action` *is* the complete activation (fires immediately, synchronously). Wraithstep's
`action` instead only **opens** RimWorld's own targeter (`Find.Targeter.BeginTargeting`, R2) — the actual relocation,
cooldown start, and tier-progression increment happen inside the callback that fires only once the player confirms a
valid destination cell. A second callback (`actionWhenFinished`) fires on cancellation and does nothing.

**Rationale**: Every gizmo-contribution and tier/cooldown mechanic already proven three times over is reused
unmodified; the only new piece is *when* the activation logic runs relative to the gizmo click, which is inherent to
what "pick a destination" (spec User Story 1) requires and cannot be avoided by copying the prior tattoos' shape
wholesale.

**Alternatives considered**: Firing the relocation immediately on click at a fixed offset/direction with no player
choice (matching the other tattoos' one-click shape exactly). Rejected: contradicts the spec's explicit
player-directed-destination requirement (FR-001, FR-002) and the "short-range blink/teleport for escape/repositioning"
framing (PRD §6), which implies the player picks where to go, not a fixed vector.

## R2: Destination-cell targeting mechanism

**Decision**: Use `Verse.Targeter.BeginTargeting`, accessed via `Find.Targeter`, with a `TargetingParameters` instance
configured for cell-only targeting (`canTargetLocations = true`, `canTargetPawns = false`, `canTargetSelf = false`) and
a `validator` delegate enforcing FR-002 (walkable, unfogged/explored, unoccupied, within range). Confirmed present via
direct metadata inspection of the `Krafs.Rimworld.Ref` 1.6.4871 reference assembly (not assumed from memory alone):
`Verse.Targeter.BeginTargeting`, `Verse.TargetingParameters` with `canTargetLocations`/`canTargetPawns`/`canTargetSelf`
fields, and a `validator` field all exist as real members, alongside `actionWhenFinished`, `mouseAttachment`, and
`onGuiAction` parameters on `BeginTargeting` itself. This is the same underlying entry point vanilla uses for every
player-directed cell/thing selection (placing buildings, throwing items, aiming weapons), so Wraithstep's targeting
session behaves identically to what players already expect from those interactions (esc/right-click cancels, left-click
on a valid cell confirms).

A destination is "confirmed" exactly when `BeginTargeting`'s primary `action` callback fires (which, per its own
`validator` parameter, vanilla will not invoke for a cell the validator rejects) — this is also the exact point
FR-005/FR-020's "successfully activates" language refers to. Cancellation (Escape/right-click) resolves through the
separate `actionWhenFinished` callback without the primary `action` ever firing, giving a clean, built-in way to
distinguish "confirmed" from "cancelled" without any custom state machine.

**Rationale**: `BeginTargeting` is the single existing engine mechanism for exactly this interaction (player picks a
cell before an effect resolves); building a bespoke input-handling loop would duplicate infrastructure the game
already ships and already gets input edge cases (map-edge, multi-monitor, camera panning mid-target) right.

**Alternatives considered**: A custom `Command_Action` two-click state machine (first click arms, second click on the
map confirms), hand-rolled without `Targeter`. Rejected: reinvents cancellation, right-click-to-cancel, and
mouse-cursor feedback that `Find.Targeter` already provides for free, for no benefit.

## R3: Live range indicator + reject-reason feedback while targeting (FR-003)

**Decision (superseded — see "Resolved during implementation" below)**: ~~A small Harmony Postfix on `Verse.
Targeter`'s per-frame draw hook...~~ — this planning-time approach assumed `Targeter` lived in namespace `Verse` and
that no built-in per-session draw callback existed on `BeginTargeting` itself. Both assumptions were wrong once the
actual reference assembly was decompiled (below), and the real API makes a Harmony patch and a separate
session-tracking static class unnecessary for this requirement.

**Resolved during implementation** (via `ilspycmd`-decompiled source of `Krafs.Rimworld.Ref` 1.6.4871's
`Assembly-CSharp.dll` — not guessed, not left as a planning-time placeholder): `Targeter` and `TargetingParameters`
both live in namespace **`RimWorld`**, not `Verse` (still accessed as `Find.Targeter`, since `Verse.Find.Targeter`'s
property type resolves to `RimWorld.Targeter`). The richest `BeginTargeting` overload already takes everything R3
needs as plain delegate parameters:

```text
Targeter.BeginTargeting(TargetingParameters targetParams, Action<LocalTargetInfo> action,
    Action<LocalTargetInfo> highlightAction, Func<LocalTargetInfo, bool> targetValidator, Pawn caster = null,
    Action actionWhenFinished = null, Texture2D mouseAttachment = null, bool playSoundOnAction = true,
    Action<LocalTargetInfo> onGuiAction = null, Action<LocalTargetInfo> onUpdateAction = null)
```

- `targetValidator` (`Func<LocalTargetInfo, bool>`) *is* R2's `IsValidDestination` — passed directly, no separate
  `TargetingParameters.validator` (which is a different, `Predicate<TargetInfo>`-typed field also confirmed to exist,
  but unneeded here since the richer overload's own `targetValidator` already covers cell validity end to end).
- `onUpdateAction` fires every Update frame the targeter is open, with the `LocalTargetInfo` currently under the
  mouse — used to call `GenDraw.DrawRadiusRing(pawn.Position, Range)` (confirmed present:
  `GenDraw.DrawRadiusRing(IntVec3 center, float radius)`), redrawn live off the wearer's own position every frame.
- `onGuiAction` fires every `OnGUI` frame, also with the current `LocalTargetInfo` — used to call
  `GenUI.DrawMouseAttachment(null, rejectReason)` (confirmed present, RimWorld's own stock mouse-following label
  widget, the same one vanilla's own placement ghosts/targeted items use for reject feedback) whenever
  `GetRejectReason(target)` (R2) is non-null.

Both callbacks are ordinary closures capturing the comp's own `this`, passed straight into the single
`BeginTargeting` call inside `BeginTargetSelection()` — no Harmony patch, no static `WraithstepTargetingSession`
holder, and no `actionWhenFinished` cleanup step are needed: a cancelled session simply never invokes `action`
(`OnDestinationConfirmed`), and once the targeter closes, `onGuiAction`/`onUpdateAction` simply stop being called by
`Targeter` itself — there is no session state left to clear. This is a strictly simpler implementation than R3
originally planned, discovered by decompiling the actual method signature rather than assumed from memory (also
confirmed along the way: `Pawn.Notify_Teleported(bool endCurrentJob = true, bool resetTweenedPos = true)` for R5,
and `IntVec3.InBounds`/`Walkable` (`Verse.GenGrid`) plus `IntVec3.Fogged`/`GetFirstPawn` (`Verse.GridsUtility`) for
R2's destination-validity checks — all exactly as R2/R5 already assumed, so only R3's mechanism needed correcting).

**Alternatives considered** (evaluated before decompiling, superseded once the real signature was known):
- A custom class implementing `ITargetingSource` so `BeginTargeting`'s Verb-aware overload auto-draws the ring the
  same way weapon aiming does. Unnecessary once the plain `TargetingParameters` overload's own `onGuiAction`/
  `onUpdateAction` parameters were confirmed to cover the same need directly, with no `Verb`/`ITargetingSource`
  coupling at all.
- Reusing RimWorld's native `AbilityDef`/`Verse.Ability`/`Pawn_AbilityTracker`/`CompAbilityEffect_Teleport`
  framework. Still rejected, for the reason feature 004 (research.md R1) already established: that framework's
  presentation is only reachable through an `AbilityDef` actually granted to a pawn, which in practice depends on
  DLC-granted content this mod does not require players to own.
- A hand-drawn UI reachable from `HediffComp_WraithstepEffect.CompPostTick` (a simulation-tick hook, not a draw
  hook). Unnecessary — `onGuiAction`/`onUpdateAction` already run at the correct per-frame cadence, supplied
  directly by the `BeginTargeting` call itself.

## R4: Avoiding a mastery-track double-count from the two-phase click/confirm flow

**Decision**: `HediffComp_WraithstepEffect` implements a new, generic marker interface,
`IHandlesOwnMasteryProgress` (no members — a pure opt-out flag), and `Patch_Pawn_GetGizmos_TattooGizmos`
(feature 004/006) is extended with one additional condition: it only auto-wraps a collected `Command_Action` for
mastery-progress credit (feature 006, `mastery-progression-contract.md` §2) when the owning comp does **not**
implement `IHandlesOwnMasteryProgress`. `HediffComp_WraithstepEffect` calls
`TattooTrackerUtility.GetTracker(Pawn)?.RegisterMasteryActivation()` itself, exactly once, from inside the
destination-confirmed callback (R2) — the same place its own `tierState.TryRegisterQualifyingEvent(...)` call lives —
so both counters advance together for the same real, completed blink.

**Rationale**: The existing shared hook (`Patch_Pawn_GetGizmos_TattooGizmos`) credits mastery progress the moment a
collected `Command_Action`'s `action` delegate is invoked — correct for every prior triggered tattoo, whose `action`
*is* the complete, synchronous activation. For Wraithstep, `action` only opens the targeter (R1/R2); crediting
mastery there would count every gizmo click, including ones the player immediately cancels, which directly
contradicts this feature's own FR-020/SC-014 ("successfully activating... automatically increase... mastery
progress," "no Wraithstep-specific code added to feature 006's own files"). `mastery-progression-contract.md` §2
explicitly anticipates exactly this situation: *"If the comp's `GetGizmo()` returns something other than a
`Command_Action`... [or needs different semantics]... this contract [can be] extended, which is an explicit,
reviewable change to this one shared patch, not a silent gap."* The chosen fix is a small, fully generic condition
(checking a marker interface, not `HediffComp_WraithstepEffect` by name or type) — satisfying both the letter and the
intent of "no Wraithstep-specific code" while correctly generalizing for any future ability with the same two-phase
shape (e.g. a hypothetical future targeted tattoo).

**Alternatives considered**:
- Accept the click-time mastery credit as-is (treat "activating" loosely, same granularity as a plain click).
  Rejected: directly contradicts this feature's own spec language (FR-020, SC-014, and User Story 3's explicit
  "successfully activating") which was deliberately written this way in response to a review question about
  targeting feedback and cancellation semantics; silently shipping looser behavior than the accepted spec promises
  would be a correctness gap, not a simplification.
- Have `Patch_Pawn_GetGizmos_TattooGizmos` special-case `HediffComp_WraithstepEffect` by type. Rejected: exactly the
  "Wraithstep-specific code added to feature 006's own files" FR-020 forbids, and less reusable than a marker
  interface for any future two-phase ability.
- Move the wrap-point later (e.g. have the shared patch itself defer crediting until some later confirmation signal).
  Rejected as unnecessary complexity: the shared patch has no visibility into an individual comp's own async
  targeting flow, and would need a new callback-based contract just to support one tattoo's shape; a simple opt-out
  is far smaller and fully generic.

## R5: Instant relocation mechanics

**Decision**: Set `Pawn.Position` to the confirmed destination cell, then call the pawn's own teleport-notification
callback (confirmed present in the reference assembly as `Notify_Teleported`/`Notify_Teleported_Int`) to correctly
reset pathing, region-tracking, and visual tweening state the way any other in-engine teleport effect does, rather
than leaving those systems out of sync with the pawn's new cell (which a bare `Position` assignment alone does not
reliably do). The exact declaring type and full parameter list of `Notify_Teleported` is confirmed present by name
only (reference assembly method bodies are stripped — see R3) and is verified against decompiled source before
implementation, consistent with this feature's other implementation-time verification items.

Destination-cell **validity** (FR-002: walkable, unfogged/explored, unoccupied, within range) is computed directly
against well-established, stable `IntVec3`/`Map` grid queries (walkability, fog-of-war, pawn-occupancy, and a plain
distance check against the ability's current range) rather than through `CompAbilityEffect_Teleport`'s own
`CanTeleportThingTo`/`IsValidTeleportCell` helpers the reference assembly also confirms exist. Those helpers belong to
the same DLC-adjacent Ability-system Comp hierarchy R3 already declines to depend on for the gizmo/targeting UX, for
the same reason — this feature's correctness should not hinge on an internal contract belonging to a system this
project has consistently avoided coupling to since feature 004.

**Rationale**: `Notify_Teleported` is a plain `Pawn`-level (not Ability-system-specific) callback that exists
specifically to keep a manually-repositioned pawn's engine-internal state consistent — using it is the
correctly-scoped fix for "my object moved without walking there," independent of whatever system triggered the move.
Computing validity from primitive grid queries keeps Wraithstep's correctness self-contained within this feature's own
code, matching this project's established preference (R3, and feature 004 R1) for not depending on the Ability
system's internals even where a superficially convenient helper exists there.

**Alternatives considered**: De-spawn and respawn the pawn at the new cell instead of a direct `Position`
reassignment. Rejected: heavier-weight (triggers full despawn/respawn side effects — inventory, mood thoughts about
"just been in danger," rendering pop) than a plain teleport most players expect from a "blink," and vanilla's own
`Notify_Teleported` exists specifically to make the lighter-weight direct-reposition approach correct without needing
a despawn/respawn cycle.

## R6: Tier 2's untargetable window — a Prefix composing the vanilla `validator`, not a Postfix overriding `__result`

**Decision**: A new Harmony **Prefix** on the same method Guardian's Call already patches,
`Verse.AI.AttackTargetFinder.BestAttackTarget` (signature confirmed by feature 004 research.md R4:
`BestAttackTarget(IAttackTargetSearcher searcher, TargetScanFlags flags, Predicate<Thing> validator = null, ...)`),
declares its `validator` parameter as `ref Predicate<Thing> validator` (a standard, well-established Harmony pattern
for a Prefix to mutate an argument the original method — and any later Postfix — will see), and wraps it so that any
pawn currently inside an active Wraithstep Tier 2 untargetable window is rejected by the *composed* validator before
vanilla's own search algorithm runs, exactly like `validator == null ? IsNotWraithstepUntargetable : (t =>
originalValidator(t) && IsNotWraithstepUntargetable(t))`.

**Rationale**: Guardian's Call's own patch (research.md R4/R5 of feature 004) is a **Postfix** that *overrides*
`__result` after vanilla's search already completed — correct for *forcing* a specific pawn to be picked, since it
only ever replaces an already-legal result with a different, independently-validated one. Exclusion is the opposite
problem: if the vanilla algorithm's own internal scoring would have picked the Wraithstep-protected pawn as its best
(often *the* only) candidate, a Postfix has no way to recover what the second-best candidate would have been — it can
only null the result, leaving the hostile targetless where vanilla would correctly have picked someone else. Composing
into the `validator` predicate instead lets vanilla's own, already-correct search naturally skip the excluded pawn
during its normal candidate scan and arrive at whatever the next-best legal target actually is, with no need to
reimplement any part of vanilla's scoring.

**Interaction with Guardian's Call (both patch the same method)**: because Harmony runs all Prefixes before the
original method, and the original method runs before any Postfix, Guardian's Call's own Postfix — which itself reads
the (by then already-composed) `validator` parameter to check whether a taunting pawn is still legal (see its own
source: `if (validator != null && !validator(taunterPawn)) continue;`) — will correctly also treat a
simultaneously-Wraithstep-untargetable pawn as an invalid taunt candidate. This is the more correct combined outcome
(a pawn mid-blink-and-untargetable should not be force-targeted by an unrelated taunt effect either) and falls out of
the two patches' existing designs with no special-case code required in either; it is called out explicitly here so
it is verified (quickstart) rather than discovered as a surprise.

**The Combat Extended path — CONFIRMED live (2026-08-15), not just assumed from precedent**: per feature 004
research.md R5 (and the fact that feature 004 shipped with only its single vanilla-method patch — no second
CE-specific targeting file exists in `Source/Patches/` today), the same `BestAttackTarget` Postfix was confirmed
sufficient for CE-controlled hostiles without a second patch. Decompiling the actual `CombatExtended.dll` (via
`ilspycmd`, not guessed) during live investigation confirmed CE patches this exact method too — but only via a
Transpiler rewriting its *ranged*-attack-targeting branch (`Harmony_AttackTargetFinder_BestAttackTarget`, replacing
that branch with CE's own `FindAttackTargetForRangedAttack`); melee targeting is untouched by CE's patch and runs
plain vanilla logic. This meant every melee-only live test up to that point (which passed) hadn't actually exercised
CE's replacement code at all. A dedicated follow-up test against a ranged-armed CE hostile (a "scavenger gunner"
confirmed equipped with a Ruger Redhawk and ammunition for the whole encounter) showed the exclusion log line firing
correctly while the hostile was in range, with zero shots fired at the untargetable wearer — confirming CE's ranged
replacement still respects the same composed `validator` this Prefix modifies, since `FindAttackTargetForRangedAttack`
reads it via the identical `attackTargetValidator` parameter the Harmony-modified local flows into. No second,
CE-specific patch was needed; Constitution Principle IV is satisfied via "the same patch verified twice" (non-CE and
CE-loaded), per `targeting-exclusion-contract.md` §4.

**Alternatives considered**:
- A Postfix nulling `__result` when it equals an untargetable pawn (mirroring Guardian's Call's shape exactly).
  Rejected: as explained above, this can leave a hostile with no target at all instead of vanilla's actual next-best
  choice, which is a strictly worse and more surprising outcome than the Prefix/validator-composition approach.
- Reusing `GuardiansCallTauntRegistry`'s exact class for Wraithstep's own untargetable-pawn bookkeeping. Rejected per
  `two-path-targeting-contract.md` §3 ("What is explicitly NOT reusable"): a new, small, Wraithstep-owned registry
  (`WraithstepUntargetableRegistry`) mirroring its *shape* (a lazily-pruned `HashSet`) but tracking different state is
  the documented, expected pattern for a second targeting-influencing tattoo with different bias semantics.

**R6 addendum (discovered during live verification, not anticipated at planning time): the search-exclusion Prefix
alone is not sufficient — an active correction is also required**

Live testing (2026-08-15) demonstrated the Prefix above working exactly as designed and *still* failing to protect
the wearer: a hostile ("Claymore") attacked a Tier 2, currently-untargetable wearer, and the debug log showed the
`excluding <wearer> from a hostile target search` line firing both immediately before and immediately after the hit
landed — proving the exclusion genuinely ran and genuinely excluded the wearer from that search's candidate pool,
while the hostile hit her anyway. The only explanation consistent with that evidence: the hostile's melee attack was
not being decided by a *fresh* `BestAttackTarget` search at all — it was continuing to act on a target it had already
locked in via `Pawn.mindState.enemyTarget`, `Pawn.mindState.meleeThreat`, or its running `Job`'s own `targetA`, none
of which a search-time exclusion touches. This is structurally the identical gap Guardian's Call's own research
(feature 004 research.md, `HediffComp_GuardiansCallEffect.ForceNearbyHostilesToReconsiderTarget`) found on the
*opposite* problem (forcing aggro onto a taunter): "a hostile already mid-combat with someone else won't make a fresh
decision on its own" until something else ends that engagement.

**Decision**: `HediffComp_WraithstepEffect` gains a `ForceNearbyHostilesOffWearer(Pawn)` method — for every hostile
pawn on the map whose `mindState.enemyTarget`, `mindState.meleeThreat`, or running `Job.targetA` currently *is* the
wearer, it nulls those two `mindState` fields and calls `hostile.jobs.EndCurrentJob(JobCondition.InterruptForced)`
(confirmed via decompiled source: `Pawn_JobTracker.EndCurrentJob(JobCondition condition, bool startNewJob = true,
bool canReturnToPool = true)`, `JobCondition.InterruptForced` confirmed as a real enum value) — forcing that hostile
back through its own top-level AI loop on its very next tick, which then correctly runs the already-working excluded
search. This is called once immediately in `OnDestinationConfirmed` (Tier 2 only, right after registering with
`WraithstepUntargetableRegistry`) to correct anyone who already had the wearer locked on *before* this specific
blink, and again every `ReassertIntervalTicks` (30, mirroring `HediffComp_GuardiansCallEffect`'s identical constant)
from a new `CompPostTick` override for the remainder of the active window, to catch a hostile that locks onto the
wearer *during* the window through some path other than the excluded search.

**Rationale**: Excluding a pawn from a search's candidate pool and forcibly correcting an already-decided target are
two different problems that happen to look similar; Guardian's Call already proved (the hard way, per its own
research.md) that a real RimWorld encounter needs both a search-time bias *and* an active, periodic correction to
hold up against every AI path that can result in a pawn attacking — a single-shot or search-only fix reliably looks
correct in a quick manual check and then fails under an actual sustained encounter, exactly as observed here.

**Alternatives considered**:
- Only correct once, at activation, with no periodic reassertion. Rejected: mirrors Guardian's Call's own rejected
  alternative for the identical reason — a hostile that acquires the wearer through some other AI path partway
  through the window (not just one already locked on at the instant of landing) would go uncorrected until the next
  activation.
- Scope the correction to hostiles within some radius of the wearer (mirroring Guardian's Call's `TauntRange`-bounded
  scan). Rejected: unlike a taunt (which only makes sense within some pull radius), exclusion has no natural radius —
  the check itself is only ever true for a hostile that already has the wearer specifically locked in, which is
  inherently a small, relevant set regardless of map size, so an unbounded scan costs nothing extra in practice.
