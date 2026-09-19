# Phase 0 Research: Bastion Ward Tattoo Effect

No `NEEDS CLARIFICATION` markers exist in spec.md — every item below resolves an implementation question the spec
itself leaves open (interpreted through the customer proposal and Constitution), not a scope ambiguity. Findings are
grounded in this repo's actual code (file:line citations throughout), not assumed from the proposal's narrative. Two
items (R1's range cap, R5's ejection fallback) were resolved directly via the spec's own `## Clarifications` /
Session 2026-09-18 Q&A rather than inferred from code — both are reflected below and in FR-001a/FR-006.

## R1: The gizmo — reuse Wraithstep's two-phase ground-targeting contract as-is

**Decision**: Bastion Ward's `GetGizmo()` returns a plain `Command_Action` (via `IProvidesTattooGizmo`,
`Source/Effects/IProvidesTattooGizmo.cs:13-16`, unmodified) whose `action` delegate opens
`Find.Targeter.BeginTargeting(targetParams, action: OnDestinationConfirmed, ...)` with
`TargetingParameters { canTargetLocations = true, canTargetPawns = false, canTargetSelf = false }`. All real
activation (zone spawn, cooldown start, tier-progression, mastery credit) happens only in
`OnDestinationConfirmed(LocalTargetInfo target)` — never on the gizmo click itself. Bastion Ward implements the
marker interface `IHandlesOwnMasteryProgress` (`Source/Effects/IHandlesOwnMasteryProgress.cs`) so the shared
gizmo-postfix's click-time mastery wrap is skipped, and calls
`TattooTrackerUtility.GetTracker(pawn)?.RegisterMasteryActivation()` itself inside `OnDestinationConfirmed`.

**Rationale**: `specs/008-wraithstep-tattoo-effect/contracts/cell-targeted-ability-contract.md` already documents
this exact shape as this mod's established, reusable pattern for "a triggered tattoo whose gizmo requires the
player to pick a destination before it fires," and explicitly anticipates a future area-targeted ability reusing
it. `HediffComp_WraithstepEffect.cs:97-161` is the concrete precedent (`GetGizmo()`, `BeginTargetSelection`).
Grepping all of `Source/` for `Command_Target|Targeter|LocalTargetInfo|TargetingParameters` returns only this one
file — no tattoo uses RimWorld's built-in `Command_Target` gizmo class; the contract's own hand-rolled
`Command_Action` + `Find.Targeter` shape is what this mod actually uses everywhere a target must be chosen.

**Alternatives considered**: RimWorld's native `Command_Target` gizmo class — rejected for the same reason the
Wraithstep contract already rejected it (not this mod's established idiom, and `IProvidesTattooGizmo`'s dispatch
model already assumes a `Command_Action`-shaped return for the shared mastery-credit wrap to special-case). A new,
Bastion-Ward-specific targeting mechanism — rejected as needless duplication of an already-proven, generic contract.

**Explicitly NOT reused** (per the Wraithstep contract's own §3 caveat): `HediffComp_WraithstepEffect`'s
`IsValidDestination`/`GetRejectReason` rules (walkable/unfogged/unoccupied/short-range) are teleport-specific.
Bastion Ward writes its own validator (in-bounds, unfogged, no unwalkable center cell required beyond that — an
AoE center is not itself occupied the way a teleport destination must be) and its own live-feedback drawing
(`onUpdateAction` drawing a radius ring at the tier-appropriate zone size, not a range ring from the caster).

**Max cast range (spec Clarifications, FR-001a)**: unlike the rest of this validator, the *range* half is
deliberately reused from Wraithstep's own shape, not reinvented — the spec's own clarification session settled on
"capped, XML-tunable range... mirrors Wraithstep's own per-tier range convention" specifically to keep Bastion Ward
consistent with every other targeted ability in this mod rather than becoming an unlimited-range "place anywhere
visible" outlier. Concretely: `HediffCompProperties_BastionWardEffect` gains `rangeTier1`/`rangeTier2` fields
(data-model.md), and both the validator (`target.Cell.DistanceTo(Pawn.Position) <= tierRange`) and a rejection
reason ("too far away," mirroring `HediffComp_WraithstepEffect`'s own `GetRejectReason` for its out-of-range case)
are added alongside the in-bounds/unfogged checks — the one piece of Wraithstep's validator that *is* reused
verbatim, even though its walkable/unoccupied checks are not (they don't apply to an AoE center).

## R2: Damage negation dispatch — the existing `IOnIncomingDamageTattooEffect` pattern does not fit; a new,
narrowly-scoped extension to the shared prefix is required

**Decision**: `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.cs:21-63` is the mod's single dispatch point for "can
this incoming damage instance be fully absorbed," but it iterates `HediffComp`s **on the pawn about to take the
damage** (`__instance.health.hediffSet.hediffs`). `IOnIncomingDamageTattooEffect.TryAbsorbIncomingDamage`
(`Source/Effects/IOnIncomingDamageTattooEffect.cs:27-30`) already supports "return `true` = full, unconditional
absorption" as a first-class outcome — but every existing implementer (Ember Ward, Ironskin Glyph) is a
**self-only** defensive tattoo: the wearer protects themselves, so the interface being scoped to the defender's own
hediffs is correct for them. Bastion Ward's protection is **zone-bound, not hediff-bound**: the vast majority of
pawns standing inside an active ward do not wear the Bastion Ward tattoo at all (spec Edge Cases, FR-003/FR-004).
There is no way to make a per-hediff-comp interface answer "is this un-tattooed pawn standing in someone else's
zone" without either (a) forcing every pawn on the map to carry a phantom comp, which is absurd, or (b) extending
the dispatch point itself.

The chosen fix: add one new, self-contained check inside `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb.Prefix`,
**after** its existing hediff-comp loop finds nothing (i.e. it does not change behavior for any of the three
existing tattoos that already use this prefix) —

```csharp
if (GameComponent_BastionWardZones.Get()?.TryAbsorb(__instance, ref dinfo) == true)
{
    absorbed = true;
    return false;
}
```

`GameComponent_BastionWardZones.TryAbsorb` (data-model.md) re-derives, on every call, whether `__instance` is
currently standing inside any active ward on its own map AND is not hostile to that ward's owning faction —
satisfying the spec's Edge Cases requirement that faction relationship is evaluated live, not cached, and that a
pawn in the overlap of two wards needs only one of them to protect it.

**Rationale**: This is the smallest possible change that lets an un-tattooed pawn benefit from someone else's
tattoo effect, and it reuses rather than forks the existing single dispatch point — a second, independently
registered Harmony prefix on the same `Pawn.PreApplyDamage` target was considered and rejected (see Alternatives)
specifically because Harmony's own prefix-chaining semantics (an earlier prefix returning `false` skips *all*
subsequent prefixes, including the original method) would make the two patches' relative execution order load-bearing
and fragile, whereas a single append inside the one already-battle-tested prefix has no such hazard. The doc
comment on the existing file already anticipated extension "without any change here" for future *hediff-comp*
consumers — this is the one legitimate exception, because Bastion Ward genuinely is not hediff-comp-shaped for the
defender side, and the change is additive-only (a new independent branch after the existing loop, not a rewrite of
it).

**Alternatives considered**:
- *A second, independently-attributed Harmony prefix on `Pawn.PreApplyDamage`*: rejected — same-method multi-prefix
  ordering between two separately declared `[HarmonyPatch]` classes is not guaranteed without explicit
  `[HarmonyPriority]` tuning, and a bug here (the existing tattoo-absorb prefix returning `false` before the
  zone-check prefix ever runs, or vice versa in a way that silently double-processes) is exactly the kind of subtle
  combat-path defect Constitution Principle II exists to catch. A single append avoids the hazard entirely.
- *Giving `HediffComp_BastionWardEffect` itself an `IOnIncomingDamageTattooEffect` implementation that checks "is
  any pawn within my zone" for whichever pawn happens to be hit*: doesn't type-check against the existing
  dispatch's own iteration (it only ever calls the interface on comps belonging to `__instance`, so a comp on the
  *caster's* hediff would never even be considered when an *ally* — a different pawn — takes damage). Rejected as
  not actually solving the problem, not merely undesirable.
- *A brand-new, Bastion-Ward-specific damage-interception patch on a different target entirely (e.g.
  `Verse.DamageWorker.Apply`)*: rejected — `Pawn.PreApplyDamage` is already the mod's proven, working full-absorb
  interception point (R3 below confirms it is CE-safe too); introducing a second interception target for one
  tattoo would duplicate proven infrastructure for no benefit.

## R3: Combat Extended interop for the zone check — no CE-specific branching needed

**Decision**: `GameComponent_BastionWardZones.TryAbsorb` needs no `CombatExtendedInterop.IsLoaded` branch at all.

**Rationale**: `specs/011-ember-ward-tattoo-effect/research.md` already decompiled the locally installed
`CombatExtended.dll` and confirmed CE patches `DamageWorker.Apply` (an unconditional-early-return prefix gated on a
wall-fragmentation setting) and `Thing.TakeDamage` (a postfix, ammo-exploder bookkeeping only) —
**neither intercepts or bypasses `Pawn.PreApplyDamage`**. Since R2's fix lives inside
`Pawn.PreApplyDamage`'s existing, already-CE-verified prefix, it inherits that same CE-safety for free. Per
`IOnIncomingDamageTattooEffect`'s own doc comment, returning full absorption "skips vanilla's/CE's own
damage-application pipeline for it entirely" — the instance never reaches CE's downstream armor formula (whose
known `CoveredByNaturalArmor` gap for humans is what forces Ember Ward/Ironskin Glyph's *own* CE-specific
`dinfo.Amount` mutation, R3-equivalent in their own research docs) at all, because the absorb happens further
upstream than that formula ever runs. `CombatExtendedInterop.IsLoaded` (`Source/Effects/CombatExtendedInterop.cs:11-27`)
remains the mod's single CE-detection point and is reused as-is if any other part of this feature needs it (R6
below flags the one place it might).

**Alternatives considered**: Mirroring Ember Ward/Ironskin Glyph's dual vanilla-stat-offset + CE-direct-mutation
pattern — not applicable; that pattern exists specifically because those tattoos deliver a *partial* reduction via
`IProvidesTattooStatOffset`/`GetStatOffset`, a mechanism CE's armor formula only partially respects. Bastion Ward
never goes through a stat offset at all (R2's fix returns full absorption directly from the prefix), so that gap
does not exist here.

## R4: The ground-fixed persistent zone — new infrastructure, no zone precedent exists in this codebase

**Decision**: Introduce `GameComponent_BastionWardZones` (a per-`Game` singleton, mirroring
`GameComponent_PhoenixRegistry`'s exact shape) holding `List<BastionWardZone> activeZones`, where `BastionWardZone`
is a small `IExposable` plain-data class (not a `Thing`, not spawned on the map): `Map map`, `IntVec3 position`,
`float radius`, `int expireTick`, `Faction owningFaction`, `bool heals`, `float healAmountPerTick`,
`int healIntervalTicks`, `int nextHealTick`. `GameComponentTick()` expires zones past `expireTick` and, for
`heals == true` zones, applies a periodic heal to every ally-faction pawn currently inside (R6). Consulted by R2's
`TryAbsorb` for damage negation.

**Rationale**: Grepping all of `Source/` for `MapComponent|GameComponent|GenSpawn|ThingWithComps` finds exactly one
existing `GameComponent` (`Source/Effects/GameComponent_PhoenixRegistry.cs`, feature 014) and zero uses of
`GenSpawn.Spawn`/custom `ThingWithComps` anywhere — Bastion Ward is the first feature needing a location-based,
caster-independent, duration-driven effect, so this is necessarily new. `GameComponent_PhoenixRegistry`'s shape
(self-registering via RimWorld's own `Game.FillComponents()` reflection discovery, a `public static Get()` lookup,
`ExposeData()` via `Scribe_Collections`, a periodic sweep in `GameComponentTick()`) is the closest proven precedent
in this codebase for "per-save, non-pawn-attached state with its own tick loop," even though its own *content* (a
pawn registry) differs from a zone registry. A plain-data class over a real spawned `Thing`/`ThingWithComps` is
chosen because nothing in the spec requires the zone to be a physical, damageable, forbid-able map object — it only
needs a position, a radius, and a duration, all of which `Scribe_Values`/`Scribe_Deep` on a plain class already
handles identically to every other tattoo's own state (Constitution Principle V), with no new `ThingDef` to author
and no interaction with vanilla's thing-grid/pathing/selection systems to reason about.

**Alternatives considered**: A spawned `Thing`/`ThingWithComps` (standard RimWorld modding practice for map
effects) — rejected for this feature specifically because it adds an entire new `ThingDef` + comp class with zero
in-repo precedent to build on, for no functional requirement this spec actually has (no requirement that the ward
be forbid-able, meleeable, or otherwise interact with vanilla's `Thing` systems). A lightweight purely-visual marker
(a `Mote`/`Mote_Static` spawned via `MoteMaker`, vanilla RimWorld API) is still layered on top for the player to see
the ward's extent (spec User Story 1's "a protective zone appears fixed at that location," an observable outcome
Constitution Principle II's manual verification needs), but it carries no gameplay state of its own — the
`GameComponent`'s zone list remains the single source of truth, so a mote failing to render for any reason cannot
desync from or block the actual mechanic.

**Reuse note for future tattoos**: this is the first ground-fixed AoE zone in the mod, but per the batch's own
proposals (`docs/proposals/2026-09-12-thunderclap-sigil-tattoo.md`, Tier 2 "applied to every hostile inside a
radius") and Stillshroud's proposal (single-target only, no AoE, so it explicitly does NOT need this), no other
tattoo in the currently-approved batch is confirmed to need a *persistent, duration-driven* zone the way Bastion
Ward does — Thunderclap Sigil's AoE is a one-shot application at cast time, not a standing zone. `contracts/
ground-fixed-aoe-zone-contract.md` documents the shape anyway (mirroring feature 004/008's own precedent of
contract-documenting genuinely new mechanisms even before a second confirmed consumer exists), since it is the
single largest new piece of infrastructure this feature introduces and a plausible future AoE-with-duration tattoo
would otherwise have to rediscover the same design questions (R4-R7) from scratch.

## R5: Rescue-eject displacement — adapt the existing melee-lock-detection triad; the position-mutation itself is new

**Decision**: At `OnDestinationConfirmed` (R1), before spawning the zone, scan
`map.mapPawns.AllPawnsSpawned` for hostiles satisfying all of: `GenHostility.HostileTo(hostile, casterPawn)`,
not dead/downed, and currently melee-locked onto a would-be-protected ally — checked via the same triad Guardian's
Call and Wraithstep already use (`hostile.mindState?.meleeThreat == ally`,
`hostile.mindState?.enemyTarget == ally`, `hostile.CurJob?.targetA.Thing == ally`), specifically requiring
`meleeThreat == ally` to satisfy FR-008 ("not a ranged attacker") since `meleeThreat` is this mod's own established
melee-specific signal (distinct from a ranged attacker's `enemyTarget`-only lock with no `meleeThreat` set, per the
doc comments in both existing usages). For each match, find a walkable cell outside the new zone's radius via
`CellFinder.TryFindRandomCellNear(hostile.Position, map, searchRadius, c => c.Standable(map) &&
!c.Fogged(map) && c.DistanceTo(position) > radius, out IntVec3 result)` (vanilla API, no in-repo precedent, general
RimWorld modding knowledge), then `hostile.Position = result; hostile.Notify_Teleported();` — the exact
teleport-and-notify pair `HediffComp_WraithstepEffect.cs:235` already uses for the wearer's own self-teleport,
applied here to a different (hostile) pawn instead. **If `TryFindRandomCellNear` returns `false`** (no matching
cell found within `searchRadius` — e.g. the hostile is boxed in by walls/clutter), per the spec's own Clarifications
session the hostile is left at its current position: no `Position`/`Notify_Teleported()` call happens for that
pawn, no wider search or fallback teleport is attempted, and the cast/zone-registration proceeds normally for every
other hostile and for the zone itself — a per-hostile skip, not a cast-blocking failure. The hostile itself is not
protected by the zone (R2/R9's `TryAbsorb` only protects pawns not hostile to the zone's owning faction), but
FR-006's actual guarantee still holds: the ally it was meleeing is inside the zone and gets full negation against
every subsequent hit regardless (FR-003), so the un-ejected hostile's continued presence costs the ally nothing
even though the physical shove didn't happen for that one pawn.

**Rationale**: `HediffComp_GuardiansCallEffect.cs:172-233` (`ForceNearbyHostilesToReconsiderTarget`) and
`HediffComp_WraithstepEffect.cs:315-353` (`ForceNearbyHostilesOffWearer`) both already establish "who is currently
locked onto pawn X" via this exact triad — reusing it for detection (not the mutation those two methods perform,
which is clearing the lock, not moving the pawn) is a direct, proven fit for FR-006's "currently engaged in melee
combat" requirement. Vanilla's `Position =` + `Notify_Teleported()` pair is already proven safe in this codebase
(Wraithstep) for a spawned pawn's manual reposition, giving confidence it is safe to reuse on a hostile pawn too.

**Alternatives considered**: A generic "knockback" utility abstracted for future reuse — rejected per this
project's own repeated "no premature abstraction" precedent (features 012/013/015 all explicitly declined to
generalize single-use mechanics); nothing in the currently-approved batch (Soul Tether pulls the caster's *target*
toward the caster, not a general push; Thunderclap Sigil stuns in place, no displacement) needs a shared push/pull
utility yet, so this stays a private method on `HediffComp_BastionWardEffect`.

**Open verification item** (Constitution Principle II, not a required dual-patch under Principle IV since this
reads existing AI-decision fields rather than intercepting/overriding targeting logic): whether Combat Extended's
own melee verb (`Verb_MeleeAttackCE`, referenced in `specs/011-ember-ward-tattoo-effect/research.md:218`) populates
`mindState.meleeThreat` identically to vanilla's melee verb. `quickstart.md`'s CE-loaded scenario MUST confirm this
empirically rather than assume it; if CE is found to leave `meleeThreat` unset for its own melee attacks, the
detection condition falls back to `enemyTarget == ally && hostile.Position.DistanceTo(ally.Position) <= 
touchMeleeRange` as a CE-safe equivalent — documented here as the fallback rather than pre-built, since research.md
R4/R5 of feature 004 already established this project's convention of resolving CE-targeting-field questions by
in-game verification with a documented fallback, not by guessing upfront.

## R6: Tier 2 heal — reuse `TattooHealingUtility.HealWorstInjury` per-pawn; the area-enumeration loop is new

**Decision**: `GameComponent_BastionWardZones.GameComponentTick()`, for each `heals == true` zone whose
`Find.TickManager.TicksGame >= nextHealTick`, enumerates `zone.map.mapPawns.AllPawnsSpawned`, filters to pawns
within `radius` of `position` that are not hostile to `owningFaction` (the same ally-check R2 uses), and calls
`TattooHealingUtility.HealWorstInjury(allyPawn, healAmountPerTick)` on each, then sets
`nextHealTick = TicksGame + healIntervalTicks`.

**Rationale**: `Source/Effects/TattooHealingUtility.cs:9-51` (`HealWorstInjury`) is already the mod's single
reusable "heal the worst open wound, roll over the leftover" primitive, used by Bloodrune and Vampiric Thorn for
their own periodic self-heals (`HediffComp_VampiricThornEffect.cs:94-111`'s `CompPostTick` interval-gating shape is
the direct model for this zone's own `nextHealTick` gate). No existing tattoo heals any pawn other than its own
wearer, so the *enumeration* (which pawns, in an area, get healed this tick) is new — but it reuses the exact same
per-pawn-pawn scan idiom R5 and the existing hostile-scanning methods already use
(`map.mapPawns.AllPawnsSpawned` + a distance/hostility filter), just iterating allies instead of hostiles.

**Alternatives considered**: A new `IHealsNearbyAlliesTattooEffect` interface for generality — rejected as
premature; nothing else in the currently-approved batch needs an area-heal (Soul Tether's tend buff is single-target
and stat-based, not a `Hediff_Injury` heal at all), so this stays private logic on `GameComponent_BastionWardZones`
rather than a new cross-tattoo contract, mirroring R5's same "don't abstract for a hypothetical" reasoning.

## R7: `TattooEffectValues` / `TattooTierProgress` — confirmed current API, reused unmodified

**Decision**: `HediffComp_BastionWardEffect` composes `public TattooTierProgress tierState = new
TattooTierProgress();` exactly as Guardian's Call and Wraithstep do (a single monotonic activation counter against
one threshold — **not** Shedscale's direct-`IProvidesTattooTierProgress` deviation, since Bastion Ward has no
racing-counters shape). Every tunable is read via `TattooEffectValues.Get(ScopeKey, "ValueKey",
Props.xmlDefaultField)` where `ScopeKey => parent.def.defName`.

**Rationale**: `Source/Effects/TattooEffectValues.cs` (`public static float Get(string scopeKey, string valueKey,
float xmlDefault)`) and `Source/Effects/TattooTierProgress.cs` (`tier`, `progressionCounter`, `ExposeData()`,
`TryRegisterQualifyingEvent(string scopeKey, string thresholdKey, int defaultThreshold)`) are unchanged since
feature 002 and used identically by every triggered tattoo to date (FR-016/FR-017 require this reuse explicitly).

## R8: Melee-hit interfaces (`IOnMeleeHit*TattooEffect`) are the wrong tool for FR-006's "currently engaged" check

**Decision**: Do not use `IOnMeleeHitLandedTattooEffect`/`IOnMeleeHitTattooEffect`
(`Source/Effects/IOnMeleeHitLandedTattooEffect.cs`, `IOnMeleeHitTattooEffect.cs`) for R5's displacement detection.

**Rationale**: Both are dispatched from `Patch_Pawn_PostApplyDamage_TattooOnHit`, a postfix that only fires
**reactively, when a hit actually lands** — useless for answering "is this hostile mid-melee with this ally right
now," since casting Bastion Ward may happen in any tick, not necessarily the same tick as a landed hit. R5's
`mindState`/`CurJob` triad is a live, query-anytime signal instead, which is what FR-006 actually needs.

## R9: "Ally-faction pawn" — defined via `GenHostility.HostileTo`, re-evaluated live (no cached faction snapshot)

**Decision**: A pawn counts as protected/healable by a given zone iff it is spawned on the zone's `map`, not
dead, and `!GenHostility.HostileTo(pawn, zone.owningFaction)` — evaluated fresh on every damage/heal check, never
cached at cast time or at zone creation.

**Rationale**: Directly required by spec Edge Cases ("faction relationship is evaluated by current faction
relationship each time it would apply, not locked in at the moment a pawn first entered the zone") and Acceptance
Scenario coverage for mental breaks/capture mid-ward. `GenHostility.HostileTo` is vanilla RimWorld's own standard
hostility-check API, already used elsewhere in this codebase for the same purpose (e.g.
`Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt.cs`, `HediffComp_GuardiansCallEffect.cs`). No new
"ally" concept or cached membership list is introduced — this is a request-time query, not stored state, so there
is nothing to desync across a save/reload either.

## R10: Standing repel — prototyped, playtested, then reverted back to the one-time cast-time eject

A continuous-repel version (any hostile standing inside a ward periodically ejected for the ward's whole duration,
via a new `GameComponent_BastionWardZones.EjectHostilesFromZone`/`nextRepelTick` sweep) was built and confirmed
working after the player noticed an ejected hostile could freely walk back into an active ward with no
consequence. Deliberately reverted back to R5's original design in the same session, on the player's own
follow-up call — a hostile freely re-entering is acceptable, since the damage negation (`TryAbsorb`) already
guarantees it can never land a hit either way; a standing barrier was judged not worth the added behavior
complexity. R5 above (the melee-lock-scoped, cast-time-only eject) is the current, shipped design — nothing in
this feature's code differs from what R5 describes.
