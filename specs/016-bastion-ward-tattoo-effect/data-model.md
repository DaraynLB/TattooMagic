# Phase 1 Data Model: Bastion Ward Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain, `TattooEffectValues` / `TattooTierProgress`
(composed, single-counter shape — research.md R7), `TattooHediffRemovalGuard` (feature 001/003), the two-phase
ground-targeted gizmo contract (`IProvidesTattooGizmo` + `IHandlesOwnMasteryProgress`, feature 008,
research.md R1), and `TattooHealingUtility.HealWorstInjury` (feature 007, research.md R6) — all reused unmodified.
Introduces one new, Bastion-Ward-owned piece of infrastructure — a ground-fixed zone registry — documented
generically in `contracts/ground-fixed-aoe-zone-contract.md` (research.md R4), plus one small, additive extension
to the existing shared damage-absorb dispatch patch (research.md R2).

## `TattooMagicDefOf` (updated — one new entry)

```csharp
public static HediffDef TattooMagic_Hediff_BastionWard;
```

No `TattooMagicDef TattooMagic_BastionWard` entry needed (mirrors Guardian's Call/Wraithstep, not Phoenix) —
nothing gates availability on a cap; `Dialog_ChooseTattoo`'s existing listing loop needs no new condition.

## `HediffCompProperties_BastionWardEffect` / `HediffComp_BastionWardEffect`

Attached to `TattooMagic_Hediff_BastionWard` (new, plain `HediffWithComps` — no custom `Hediff` subclass needed,
same convention as every prior triggered tattoo). Implements `IProvidesTattooGizmo` and
`IHandlesOwnMasteryProgress`. Deliberately does **not** implement `IOnIncomingDamageTattooEffect` — the comp itself
never absorbs damage; the zone it spawns does, via a different mechanism entirely (see
`GameComponent_BastionWardZones` below, research.md R2).

| Field (XML, on the props class) | Type | Notes |
|---|---|---|
| `durationTicks` | int | Ward lifetime from cast, both tiers (FR-014: tier-independent). Placeholder ~8 seconds → `480` ticks (60 ticks/sec). |
| `radiusTier1` / `radiusTier2` | float | Zone radius in cells (FR-012: Tier 2 strictly larger). Placeholders `4.5` / `6.5`. |
| `rangeTier1` / `rangeTier2` | float | Max cast distance in cells from the caster's own position to the targeted zone center (FR-001a, spec Clarifications) — mirrors `HediffComp_WraithstepEffect`'s own `rangeTier1`/`rangeTier2` fields. Distinct from `radiusTier1`/`radiusTier2` above: range bounds *where* the player can place the ward, radius bounds the protected area once placed. Placeholders `12` / `16`. |
| `cooldownDurationTicks` | int | Ticks after activation before the gizmo is usable again, both tiers (FR-014). Placeholder `3600`. |
| `tier2Threshold` | int | Activation count at which Tier 1 auto-upgrades to Tier 2 (PRD §5.4 pattern). Placeholder `15`, matching Guardian's Call/Wraithstep's own precedent value. |
| `healAmountPerTickTier2` | float | Heal magnitude applied to each ally inside per heal interval, Tier 2 only (FR-013). Placeholder `1.0`. |
| `healIntervalTicksTier2` | int | Ticks between heal applications, Tier 2 only. Placeholder `60` (once per in-game second), mirroring Vampiric Thorn's own `postKillHealIntervalTicks` shape. |
| `abilityIconPath` | string | Texture path for the gizmo icon; falls back to a placeholder if unset. |

All fields read exclusively through `TattooEffectValues.Get(ScopeKey, "...", Props.xxx)` at point of use
(FR-016); each carries the standard top-of-file XML placeholder comment pointing at this spec's own Assumptions
section (Constitution Principle III). Not XML-exposed: the displacement search radius (research.md R5) — an
implementation detail of where an ejected hostile lands, not a gameplay balance number per FR-016's own explicit
list, kept as a small named constant in code instead.

| Field (runtime, on the comp) | Type | Persistence | Notes |
|---|---|---|---|
| `tierState` | `TattooTierProgress` (composed, unmodified) | `ExposeData()` from `CompExposeData()` | Single monotonic activation counter, identical mechanism to Guardian's Call/Wraithstep (research.md R7) — **not** Shedscale's direct-interface deviation, since Bastion Ward has no racing-counters shape. |
| `cooldownEndTick` | int | `Scribe_Values.Look` | Game tick after which the gizmo is usable again. |

**Behavior**:
- `GetGizmo()` → one `Command_Action` per call (icon from `abilityIconPath`, label "Bastion Ward",
  `disabled`/`disabledReason` computed live from `cooldownEndTick`; `action` calls `BeginTargetSelection()`).
- `BeginTargetSelection()` (research.md R1) → `Find.Targeter.BeginTargeting(new TargetingParameters {
  canTargetLocations = true, canTargetPawns = false, canTargetSelf = false }, action: OnDestinationConfirmed,
  targetValidator: IsValidZoneCenter, caster: Pawn, onGuiAction: DrawRejectReasonLabel,
  onUpdateAction: DrawZoneRadiusPreview)`. A cancelled session (Escape/right-click) leaves `cooldownEndTick`,
  `tierState`, and mastery progress completely unchanged, per the cell-targeted-ability contract §1.
- `IsValidZoneCenter(LocalTargetInfo target)` → `target.Cell.InBounds(Pawn.Map) && !target.Cell.Fogged(Pawn.Map) &&
  target.Cell.DistanceTo(Pawn.Position) <= CurrentTierRange`, where `CurrentTierRange` resolves
  `rangeTier1`/`rangeTier2` via `TattooEffectValues` for `tierState.tier` (research.md R1's Clarifications-driven
  addition — FR-001a). No walkable/unoccupied restriction (unlike Wraithstep's own validator, explicitly not reused
  per the contract's §3 caveat) — an AoE center is not itself "occupied" the way a teleport destination must be —
  but the *range* check specifically mirrors Wraithstep's own range-limit convention rather than being omitted.
- `GetRejectReason(LocalTargetInfo target)` / `DrawRejectReasonLabel` → mirrors
  `HediffComp_WraithstepEffect`'s own reject-reason pattern: reports "too far away" when the range check fails,
  "not visible" when fogged, no reason needed for in-bounds (Wraithstep's own walkable/occupied reasons don't apply
  here since this feature's validator has no such checks).
- `DrawZoneRadiusPreview(LocalTargetInfo target)` → draws a radius ring (`GenDraw.DrawRadiusRing`, vanilla API) at
  `target.Cell` sized to the wearer's current tier's radius, live feedback during target selection (mirrors the
  cell-targeted-ability contract's live-feedback allowance, §3).
- `OnDestinationConfirmed(LocalTargetInfo target)` — no-ops defensively if still on cooldown; otherwise, in order:
  1. Resolves this activation's tier-appropriate `radius`/`heals` from `tierState.tier` via `TattooEffectValues`.
  2. Calls `EjectMeleeAttackersFromIncomingZone(target.Cell, radius)` (research.md R5) — **before** the zone is
     registered, so the ejection check ("who is currently melee-locked onto an ally that will end up inside")
     evaluates against the zone's about-to-exist footprint, not a zone that already excludes the attacker by its
     own presence.
  3. Registers a new `BastionWardZone` with `GameComponent_BastionWardZones.Get()` (owning faction =
     `Pawn.Faction`, `expireTick = TicksGame + durationTicks`).
  4. Sets `cooldownEndTick = TicksGame + cooldownDurationTicks`.
  5. Calls `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)`.
  6. Calls `TattooTrackerUtility.GetTracker(Pawn)?.RegisterMasteryActivation()` (per `IHandlesOwnMasteryProgress`,
     research.md R1).
- `EjectMeleeAttackersFromIncomingZone(IntVec3 center, float radius)`: scans `Pawn.Map.mapPawns.AllPawnsSpawned`
  for hostiles (research.md R5) satisfying `GenHostility.HostileTo(hostile, Pawn.Faction)`, not dead/downed, and
  `hostile.mindState?.meleeThreat == ally` for some ally pawn with `!GenHostility.HostileTo(ally, Pawn.Faction)`
  and `ally.Position.DistanceTo(center) <= radius` (i.e. the ally is about to be inside the new zone). For each
  matching hostile, finds a walkable cell outside the new zone via `CellFinder.TryFindRandomCellNear(
  hostile.Position, Pawn.Map, DisplacementSearchRadius, c => c.Standable(Pawn.Map) && !c.Fogged(Pawn.Map) &&
  c.DistanceTo(center) > radius, out IntVec3 result)`; if found, sets `hostile.Position = result;
  hostile.Notify_Teleported();` (FR-006/FR-007/FR-008 — evaluated once, at this call, never again for this cast).
  **If `TryFindRandomCellNear` returns `false`** (spec Clarifications, FR-006's fallback), that hostile is simply
  skipped — no `Position`/`Notify_Teleported()` call, no widened search, no exception, and processing continues to
  the next matching hostile and then to zone registration as normal. The un-ejected hostile is not itself protected
  by the zone (`GameComponent_BastionWardZones.TryAbsorb`, R2/R9, only ever protects pawns not hostile to the
  zone's `owningFaction`), but this still satisfies FR-006's actual guarantee: the *ally* it was meleeing is inside
  the zone and gets `TryAbsorb`'s full negation against every subsequent hit regardless, so the hostile's continued
  presence costs it nothing — no free hit gets through even without the physical shove.
- `CompExposeData()` → `base.CompExposeData()`, then `tierState.ExposeData()` and
  `Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick")`.

## `GameComponent_BastionWardZones` (new, `Source/Effects/`)

Per-save singleton, mirrors `GameComponent_PhoenixRegistry`'s self-registering shape (research.md R4).

```csharp
public class GameComponent_BastionWardZones : GameComponent
{
    private List<BastionWardZone> activeZones = new List<BastionWardZone>();

    public GameComponent_BastionWardZones(Game game) { }

    public static GameComponent_BastionWardZones Get() =>
        Current.Game?.GetComponent<GameComponent_BastionWardZones>();

    public void RegisterZone(BastionWardZone zone);
    public bool TryAbsorb(Pawn defender, ref DamageInfo dinfo);
    public override void GameComponentTick();
    public override void ExposeData();
}
```

**Behavior**:
- `RegisterZone(zone)` → `activeZones.Add(zone)`. Called once, from `HediffComp_BastionWardEffect
  .OnDestinationConfirmed` step 3.
- `TryAbsorb(defender, ref dinfo)` (research.md R2/R9) → iterates `activeZones`; for the first zone where
  `zone.map == defender.Map && defender.Position.DistanceTo(zone.position) <= zone.radius &&
  !GenHostility.HostileTo(defender, zone.owningFaction)`, returns `true` (full negation, `dinfo` left
  unmodified per `IOnIncomingDamageTattooEffect`'s own contract for a full absorb). Returns `false` if no zone
  matches. Re-evaluates hostility live on every call (research.md R9) — never caches "is pawn P inside zone Z."
- `GameComponentTick()`: 
  1. Removes any zone where `Find.TickManager.TicksGame >= zone.expireTick` (FR-005).
  2. For each remaining zone with `zone.heals && TicksGame >= zone.nextHealTick`: enumerates
     `zone.map.mapPawns.AllPawnsSpawned`, filters to pawns within `zone.radius` of `zone.position` that are
     `!GenHostility.HostileTo(pawn, zone.owningFaction)`, calls
     `TattooHealingUtility.HealWorstInjury(pawn, zone.healAmountPerTick)` on each (research.md R6), then sets
     `zone.nextHealTick = TicksGame + zone.healIntervalTicks`.
- `ExposeData()`: `Scribe_Collections.Look(ref activeZones, "activeZones", LookMode.Deep)`.

## `BastionWardZone` (new, plain `IExposable`, `Source/Effects/`)

```csharp
public class BastionWardZone : IExposable
{
    public Map map;
    public IntVec3 position;
    public float radius;
    public int expireTick;
    public Faction owningFaction;
    public bool heals;
    public float healAmountPerTick;
    public int healIntervalTicks;
    public int nextHealTick;

    public void ExposeData()
    {
        Scribe_References.Look(ref map, "map");
        Scribe_Values.Look(ref position, "position");
        Scribe_Values.Look(ref radius, "radius");
        Scribe_Values.Look(ref expireTick, "expireTick");
        Scribe_References.Look(ref owningFaction, "owningFaction");
        Scribe_Values.Look(ref heals, "heals");
        Scribe_Values.Look(ref healAmountPerTick, "healAmountPerTick");
        Scribe_Values.Look(ref healIntervalTicks, "healIntervalTicks");
        Scribe_Values.Look(ref nextHealTick, "nextHealTick");
    }
}
```

Holds no back-reference to its caster or to the `HediffComp` that created it (contracts/ground-fixed-aoe-zone-
contract.md §1) — this is what makes FR-020's "ward continues even if the caster's hediff is removed" true by
construction. `map`/`owningFaction` use `Scribe_References` (per-save object references), not `Scribe_Deep` or
`Scribe_Defs` — both are runtime `Game`-scoped objects, not `Def`s.

Also exposes a derived, non-persisted `List<IntVec3> FieldCells` property (lazily computed via
`GenRadial.RadialCellsAround(position, radius, true)` and cached, since `position`/`radius` never change after
creation) — consumed only by `MapComponent_BastionWardZoneOverlay` (below) for its visual draw, never by
`TryAbsorb`/the heal pass, both of which use a plain distance check instead (FR-005a).

## `MapComponent_BastionWardZoneOverlay` (new, `Source/Effects/`) — FR-005a

Per-map, auto-discovered the same way `GameComponent_BastionWardZones` is per-save (RimWorld's own
`Map.FillComponents()` reflection discovery, no manual registration). Purely visual and purely additive — owns no
state, persists nothing, and cannot desync from the actual mechanic (research.md R4's own design bar for anything
layered on top of the zone registry): every `MapComponentUpdate()` frame, it reads
`GameComponent_BastionWardZones.Get()?.activeZones` live, filters to zones on its own `map`, and for each draws
**two layers** over `zone.FieldCells` — a solid per-cell fill (`Graphics.DrawMesh(MeshPool.plane10, ...,
FillMaterial, 0)` at `AltitudeLayer.MetaOverlays`) plus a bolder boundary line (`GenDraw.DrawFieldEdges(cells,
EdgeColor)`, `EdgeColor = new Color(1f, 0.85f, 0.25f, 0.9f)`).

Two revisions before landing here, both confirmed against the actual installed `Assembly-CSharp.dll` (decompiled
via `ilspycmd`, not guessed): an edges-only first version (`DrawFieldEdges` alone, no fill) was found too subtle
in-game to read as "this ground is warded" at a glance. A second version added a fill via
`SolidColorMaterials.SimpleSolidColorMaterial` (`ShaderDatabase.SolidColor`) — this rendered as solid magenta
in-game (Unity's shader-variant-missing fallback color). Decompiling `Verse.GenDraw` showed vanilla's own
`DrawFieldEdges`/`DrawDiagonalStripes` never use `SolidColorMaterials` for a field overlay at all — both instead
build their material via `MaterialPool.MatFrom(<a real texture>, ShaderDatabase.Transparent, tintColor,
renderQueue)`, drawn at `AltitudeLayer.MetaOverlays` (not `MapDataOverlay`, which is used elsewhere for a
different single-target highlight case). The current, working version mirrors that exact proven pattern —
`MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, FillColor, 2900)` (`BaseContent.WhiteTex`
itself already used this way by several other vanilla translucent-quad overlays), cached lazily in a static field
since `MaterialPool.MatFrom` isn't guaranteed safe to call from a static field initializer at class-load time.
`FillColor = new Color(1f, 0.75f, 0.15f, 0.25f)` — placeholder aesthetic per spec Assumptions, not a locked
design. Because it re-reads the live zone list every frame rather than tracking its own draw state,
the overlay appears and disappears in exact lockstep with
`TryAbsorb`'s own protection window — there is no separate timer to fall out of sync with FR-005's expiry (SC-010).

## `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` (updated, `Source/Patches/`)

One additive check appended after the existing hediff-comp loop, before the final `return true;` (research.md R2):

```csharp
if (GameComponent_BastionWardZones.Get()?.TryAbsorb(__instance, ref dinfo) == true)
{
    absorbed = true;
    return false;
}
```

No change to the existing loop or its behavior for Ember Ward/Ironskin Glyph/any other current consumer — this is
purely additive. No Combat Extended branching needed (research.md R3 — this file's existing patch target,
`Pawn.PreApplyDamage`, is already confirmed CE-safe).

## New XML: `Defs/HediffDefs/Tattoos/BastionWard.xml`

`hediffClass = HediffWithComps`; `<comps>` carries `HediffCompProperties_BastionWardEffect` with every tunable
from the table above as a placeholder value, each under a top-of-file comment pointing at this spec's Assumptions
section — same shape as `Defs/HediffDefs/Tattoos/Wraithstep.xml`.

## New XML: `Defs/TattooDefs/BastionWard.xml`

`tattooType = Triggered`, `appliedHediff = TattooMagic_Hediff_BastionWard`, standard ingredients/`workAmount`
shape — same shape as `Defs/TattooDefs/GuardiansCall.xml`.

## Persistence summary (Constitution Principle V)

- `HediffComp_BastionWardEffect`'s `tierState`/`cooldownEndTick` — `Scribe_Values`/`tierState.ExposeData()` on the
  Bastion Ward hediff itself, identical mechanism to every prior triggered tattoo's comp state (FR-019's
  progression-counter/tier half).
- `GameComponent_BastionWardZones.activeZones` — `Scribe_Collections.Look(..., LookMode.Deep)`, each
  `BastionWardZone.ExposeData()` persisting position/radius/remaining-duration/tier-at-cast (as `radius`/`heals`/
  heal fields, already resolved at cast time — not re-read from `TattooEffectValues` on load, so a balance-pass
  XML edit mid-save never retroactively changes an already-active ward's own numbers) independently of any pawn
  (FR-019's active-ward half). Deterministic and idempotent across save/reload: `expireTick`/`nextHealTick` are
  absolute `TicksGame` values, so a reload simply resumes comparing against the same absolute tick, no relative
  "resume a wait" logic needed (contrast Shedscale's relative day-counters, which needed the opposite design for
  its own different reasons).
- `GameComponent_BastionWardZones` itself needs no explicit registration — RimWorld's own `Game.FillComponents()`
  reflection discovery finds and constructs it automatically, identical to `GameComponent_PhoenixRegistry`
  (feature 014 precedent).
