# Phase 1 Data Model: Soul Tether Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain, `TattooEffectValues`/`TattooTierProgress`
(composed, single-counter shape — research.md R7), `TattooHediffRemovalGuard` (feature 001/003), the two-phase
targeted-gizmo contract (`IProvidesTattooGizmo` + `IHandlesOwnMasteryProgress`, feature 008), and Frost Sigil's
cross-pawn transient-hediff shape (feature 002, research.md R6) — all reused unmodified except where a genuinely new
target type (pawn/corpse rather than a location, research.md R1/R2) requires it. Introduces two small, additive new
dispatch points (research.md R4/R5) rather than forking or bending an existing one.

## `TattooMagicDefOf` (updated — two new entries)

```csharp
public static HediffDef TattooMagic_Hediff_SoulTether;
public static HediffDef TattooMagic_Hediff_SoulTetherTendBuff;
```

No `TattooMagicDef TattooMagic_SoulTether` DefOf entry needed (mirrors Bastion Ward/Guardian's Call/Wraithstep, not
Phoenix) — nothing gates availability on a cap.

## `HediffCompProperties_SoulTetherEffect` / `HediffComp_SoulTetherEffect`

Attached to `TattooMagic_Hediff_SoulTether` (new, plain `HediffWithComps`). Implements `IProvidesTattooGizmo` and
`IHandlesOwnMasteryProgress`.

| Field (XML, on the props class) | Type | Notes |
|---|---|---|
| `rangeTier1` / `rangeTier2` | float | Max pull distance in cells from the caster (FR-002, FR-016: Tier 2 strictly longer). Placeholders `10` / `16`, mirroring Wraithstep's own range-per-tier convention. |
| `cooldownDurationTicks` | int | Ticks after activation before the gizmo is usable again, both tiers (FR-013, FR-018: tier-independent, matching Bastion Ward's own precedent). Placeholder `1800`. |
| `tier2Threshold` | int | Activation count at which Tier 1 auto-upgrades to Tier 2 (PRD §5.4 pattern). Placeholder `15`, matching every prior triggered tattoo's own precedent value. |
| `tendQualityBonusTier1` / `tendQualityBonusTier2` | float | Flat additive bonus applied to `TendUtility.CalculateBaseTendQuality`'s result for the pulled target (FR-010, FR-017). Placeholders `0.15` / `0.30`. |
| `tendSpeedBonusTier1` / `tendSpeedBonusTier2` | float | Flat additive bonus to the doctor's `MedicalTendSpeed` while tending the pulled target (FR-010, FR-017). Placeholders `0.5` / `1.0`. |
| `tendBuffDurationTicks` | int | How long the tend buff hediff lasts on the target after landing, both tiers (spec Assumptions places this as a shared placeholder, not tier-differentiated — only magnitude differs per FR-017). Placeholder `1800` (30 seconds). |
| `pullEffectDurationTicks` | int | Delay between the "vanish" VFX/SFX (played at confirmation) and the "arrival" VFX/SFX + actual relocation (FR-007a, research.md R9). Placeholder `20` (a third of a second), within the customer-approved 15-30 tick range — deliberately short; this is a fast rescue snatch, not a slow float. |
| `abilityIconPath` | string | Texture path for the gizmo icon. |

All fields read exclusively through `TattooEffectValues.Get(ScopeKey, "...", Props.xxx)` at point of use
(FR-020); each carries the standard top-of-file XML placeholder comment pointing at this spec's Assumptions section
(Constitution Principle III).

| Field (runtime, on the comp) | Type | Persistence | Notes |
|---|---|---|---|
| `tierState` | `TattooTierProgress` (composed, unmodified) | `ExposeData()` from `CompExposeData()` | Single monotonic activation counter (research.md R7). |
| `cooldownEndTick` | int | `Scribe_Values.Look` | Game tick after which the gizmo is usable again. |
| `pendingTarget` | `Thing` | `Scribe_References.Look` | The `Pawn`/`Corpse` currently mid-flight, or `null` (the common case) if no pull is in progress (research.md R9). |
| `pendingDestination` | `IntVec3` | `Scribe_Values.Look` | The landing cell resolved at confirmation time, applied when the pull resolves. |
| `pullResolveTick` | int | `Scribe_Values.Look` | Game tick at which `ResolvePendingPullIfDue` performs the actual relocation. |
| `pendingTendQualityBonus` / `pendingTendSpeedBonus` / `pendingTendBuffDurationTicks` | float / float / int | `Scribe_Values.Look` | Tier-resolved tend-buff magnitudes, snapshotted at confirmation time (only ever set when the pending target is a living `Pawn`). |

**Behavior**:
- `GetGizmo()` → one `Command_Action` per call (icon from `abilityIconPath`, label "Soul Tether",
  `disabled`/`disabledReason` computed live from `cooldownEndTick`; `action` calls `BeginTargetSelection()`).
- `BeginTargetSelection()` (research.md R1) → `Find.Targeter.BeginTargeting(new TargetingParameters {
  canTargetLocations = false, canTargetPawns = true, canTargetCorpses = true, canTargetSelf = false,
  neverTargetHostileFaction = true }, action: OnTargetConfirmed, targetValidator: IsValidTarget, caster: Pawn,
  onGuiAction: DrawRejectReasonLabel, onUpdateAction: DrawRangeRing)`. A cancelled session (Escape/right-click)
  leaves `cooldownEndTick`, `tierState`, and mastery progress completely unchanged, per the cell-targeted-ability
  contract's §1 (feature 008), extended here to a pawn/corpse target per research.md R1.
- `IsValidTarget(LocalTargetInfo target)` → `GetRejectReason(target) == null`.
- `GetRejectReason(LocalTargetInfo target)` → in order: target must be a `Pawn` or `Corpse` (defensive; the
  `TargetingParameters` flags already restrict this), must not be `Pawn` (the caster itself — defensive,
  `canTargetSelf = false` already prevents this), must be spawned on `Pawn.Map` (spec Edge Cases: no caravan/
  off-map targets), must resolve to a friendly `effectivePawn` via research.md R2's `InnerPawn`-aware check, and
  must be within `CurrentTierRange` of the caster (research.md R1's Clarifications-mirroring range cap, FR-002).
  Returns the first failing reason ("Not a valid target.", "Not on the same map.", "Not friendly.", "Too far
  away.") or `null` once valid — drawn live via `DrawRejectReasonLabel`, mirroring Wraithstep/Bastion Ward's own
  reject-reason pattern exactly.
- `DrawRangeRing(LocalTargetInfo target)` → `GenDraw.DrawRadiusRing(Pawn.Position, CurrentTierRange)`, live feedback
  during target selection (mirrors Wraithstep's own range-ring draw, `HediffComp_WraithstepEffect.cs:200-205`).
- `OnTargetConfirmed(LocalTargetInfo target)` — no-ops defensively if still on cooldown; otherwise, in order:
  1. Resolves `Thing targetThing = target.Thing` (a `Pawn` or `Corpse`, per `IsValidTarget`).
  2. Finds a walkable cell adjacent to the caster (`CellFinder.TryFindRandomCellNear(Pawn.Position, Pawn.Map, 2,
     c => c.Standable(Pawn.Map), out IntVec3 destination)`, falling back to `Pawn.Position` itself if none found —
     spec Acceptance Scenario 1.5/FR-007's "nearest walkable cell it can find" fallback, never failing the cast).
  3. Plays the "vanish" VFX/SFX at `targetThing`'s current position (research.md R9): `FleckMaker.ConnectingLine`
     between caster and target (`FleckDefOf.LineEMP`), `FleckMaker.Static` (`FleckDefOf.PsycastSkipFlashEntry`),
     and `SoundDefOf.Psycast_Skip_Entry.PlayOneShot(...)`.
  4. Stores `pendingTarget`/`pendingDestination`/`pullResolveTick` (= `TicksGame + pullEffectDurationTicks`), and,
     if `targetThing is Pawn` (not a `Corpse` — FR-011), the tier-resolved `pendingTendQualityBonus`/
     `pendingTendSpeedBonus`/`pendingTendBuffDurationTicks` (FR-010) — the actual relocation and buff grant are
     deferred to `ResolvePendingPullIfDue` (FR-007a).
  5. Sets `cooldownEndTick = TicksGame + cooldownDurationTicks`.
  6. Calls `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)`.
  7. Calls `TattooTrackerUtility.GetTracker(Pawn)?.RegisterMasteryActivation()` (per `IHandlesOwnMasteryProgress`).
     Cooldown/tier-progression/mastery credit all fire here, at confirmation — not deferred to resolution.
- `CompPostTick(ref float severityAdjustment)` → calls `ResolvePendingPullIfDue()` every tick (research.md R9).
- `ResolvePendingPullIfDue()` → no-ops if `pendingTarget == null` or `TicksGame < pullResolveTick`; otherwise
  clears `pendingTarget`, defensively no-ops if it was destroyed/despawned in flight, then relocates it per
  research.md R3 (`Position` set + `Notify_Teleported()` iff a living `Pawn`), plays the "arrival" VFX/SFX
  (`FleckDefOf.PsycastSkipInnerExit`/`PsycastSkipOuterRingExit` + `SoundDefOf.Psycast_Skip_Exit`) at the
  destination, and — only if the target is a living `Pawn` — calls `SoulTetherTendBuffUtility.GrantOrRefresh`
  with the snapshotted `pendingTendQualityBonus`/`pendingTendSpeedBonus`/`pendingTendBuffDurationTicks` (research.md
  R6).
- `CompExposeData()` → `base.CompExposeData()`, then `tierState.ExposeData()` and `Scribe_Values`/
  `Scribe_References` for `cooldownEndTick` and every pending-pull field above.

Reusability note (FR-019, research.md R8): the target-filter/pull/arrival-effect split described here is written as
small, target-filter-and-effect-agnostic helpers rather than inlined assumptions about "the target is always
friendly" — the friendliness check itself lives entirely in `GetRejectReason` (filtering before `OnTargetConfirmed`
ever runs), and the tend-buff grant (step 4 of `OnTargetConfirmed` plus `ResolvePendingPullIfDue`'s own grant call)
is the one piece that is Soul-Tether-specific "arrival effect." The VFX/SFX (research.md R9) is likewise generic —
any future consumer of this pull mechanism gets the same vanish/arrival flash-and-snap timing "for free," supplying
only its own filter and its own arrival effect. A future inverse tattoo supplies its own `GetRejectReason`-equivalent
(a hostile-only filter) and its own arrival effect (a debuff grant) while reusing the landing-cell/relocation/VFX
(find-adjacent-cell, relocate) verbatim.

## `HediffCompProperties_SoulTetherTendBuff` / `HediffComp_SoulTetherTendBuff` (new, `Source/Hediffs/`)

Attached to `TattooMagic_Hediff_SoulTetherTendBuff` alongside vanilla's own `HediffCompProperties_Disappears`
(research.md R6), mirroring `Defs/HediffDefs/FrostSigilSlow.xml`'s exact shape.

```csharp
public class HediffCompProperties_SoulTetherTendBuff : HediffCompProperties
{
    public HediffCompProperties_SoulTetherTendBuff() { compClass = typeof(HediffComp_SoulTetherTendBuff); }
}

public class HediffComp_SoulTetherTendBuff : HediffComp
{
    public float qualityBonus;
    public float speedBonus;

    public void Configure(float quality, float speed)
    {
        qualityBonus = quality;
        speedBonus = speed;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref qualityBonus, "qualityBonus", 0f);
        Scribe_Values.Look(ref speedBonus, "speedBonus", 0f);
    }
}
```

Not `isBad` — this is a helpful buff (unlike Frost Sigil's slow), so `Defs/HediffDefs/SoulTetherTendBuff.xml` sets
`isBad = false`. Configured values are grant-time snapshots (`Configure`, called once from `SoulTetherTendBuffUtility
.GrantOrRefresh`), not re-read from `TattooEffectValues` on every stat query — matching Frost Sigil's own precedent
of resolving the tier-appropriate number once at grant time rather than on every read.

## `SoulTetherTendBuffUtility` (new, static, `Source/Effects/`)

```csharp
public static class SoulTetherTendBuffUtility
{
    public static void GrantOrRefresh(Pawn patient, float qualityBonus, float speedBonus, int durationTicks);
    public static float GetActiveQualityBonus(Pawn patient);
    public static float GetActiveSpeedBonus(Pawn patient);
}
```

**Behavior**:
- `GrantOrRefresh`: if `patient` already carries `TattooMagic_Hediff_SoulTetherTendBuff`, reconfigures that
  existing instance in place and resets its `HediffComp_Disappears` duration (no stacking multiple copies — a
  second pull before the first buff expires simply refreshes it to the new tier's numbers and full duration,
  matching this mod's general no-duplicate-hediff convention for singleton tattoo-effect hediffs). Otherwise creates
  one via `HediffMaker.MakeHediff(TattooMagicDefOf.TattooMagic_Hediff_SoulTetherTendBuff, patient)`,
  `patient.health.AddHediff(...)`, configures it, and sets its `HediffComp_Disappears` duration — the exact
  `HediffComp_FrostSigilEffect.GrantSlow` shape (research.md R6).
- `GetActiveQualityBonus(patient)` / `GetActiveSpeedBonus(patient)`: `patient.health?.hediffSet?
  .GetFirstHediffOfDef(TattooMagicDefOf.TattooMagic_Hediff_SoulTetherTendBuff)`'s `HediffComp_SoulTetherTendBuff`
  component's `qualityBonus`/`speedBonus`, or `0f` if the patient has no such hediff (the common case for every
  patient not currently benefiting from a Soul Tether pull) — never throws for a `null` patient or missing hediff.

## `Patch_TendUtility_CalculateBaseTendQuality_SoulTetherBuff` (new, `Source/Patches/`) — research.md R4

```csharp
[HarmonyPatch(typeof(TendUtility), nameof(TendUtility.CalculateBaseTendQuality),
    typeof(Pawn), typeof(Pawn), typeof(ThingDef))]
public static class Patch_TendUtility_CalculateBaseTendQuality_SoulTetherBuff
{
    static void Postfix(Pawn patient, ref float __result)
    {
        __result += SoulTetherTendBuffUtility.GetActiveQualityBonus(patient);
    }
}
```

Purely additive — a patient with no active buff hediff gets `+0f`, identical to vanilla's own unmodified result for
every other tattoo/pawn. No Combat Extended branching needed: tend quality is a vanilla-only mechanic with no CE
override (CE overhauls combat/damage/ammo, not the medical tend system), so this needs no `CombatExtendedInterop`
check, unlike features that touch armor/accuracy/damage stats.

## `StatPart_SoulTetherTendSpeedBuff` (new, `Source/Effects/`) — research.md R5

```csharp
public class StatPart_SoulTetherTendSpeedBuff : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        val += GetOffset(req);
    }

    public override string ExplanationPart(StatRequest req)
    {
        float offset = GetOffset(req);
        return offset == 0f ? null : "Soul Tether: " + offset.ToStringByStyle(ToStringNumberSense.Offset);
    }

    private float GetOffset(StatRequest req)
    {
        if (!(req.Thing is Pawn doctor))
            return 0f;
        Job job = doctor.CurJob;
        if (job == null || job.def != JobDefOf.TendPatient)
            return 0f;
        Pawn patient = job.GetTarget(TargetIndex.A).Pawn;
        return patient == null ? 0f : SoulTetherTendBuffUtility.GetActiveSpeedBonus(patient);
    }
}
```

Registered by `TattooEffectStatPartInstaller` onto `StatDefOf.MedicalTendSpeed` alongside its existing registrations
(one new `Register(StatDefOf.MedicalTendSpeed);` line) — same installer, same `stat.immutable = false;
stat.Worker.DeleteStatCache();` treatment as every other stat it already touches, since `MedicalTendSpeed` has never
been registered with a `StatPart` by this mod before and may already be flagged immutable.

A **separate class** from `StatPart_TattooEffectOffset`, not a modification of it (research.md R5's Alternatives) —
its data source is a *different* pawn (the current job's target) than the one whose stat is being computed, which
`IProvidesTattooStatOffset`'s existing per-`req.Thing`-hediff contract cannot express.

## New XML: `Defs/HediffDefs/Tattoos/SoulTether.xml`

`hediffClass = HediffWithComps`; `<comps>` carries `HediffCompProperties_SoulTetherEffect` with every tunable from
the table above as a placeholder value, each under a top-of-file comment pointing at this spec's Assumptions
section — same shape as `Defs/HediffDefs/Tattoos/BastionWard.xml`.

## New XML: `Defs/HediffDefs/SoulTetherTendBuff.xml`

`hediffClass = HediffWithComps`, `isBad = false`; `<comps>` carries `HediffCompProperties_Disappears`
(`showRemainingTime = true`, no fixed `disappearsAfterTicks` in XML since duration is set at grant time via
`SetDuration`, matching `FrostSigilSlow.xml`'s own comment explaining the same for its offset/duration) plus
`HediffCompProperties_SoulTetherTendBuff` (no XML fields — configured entirely at grant time).

## New XML: `Defs/TattooDefs/SoulTether.xml`

`tattooType = Triggered`, `appliedHediff = TattooMagic_Hediff_SoulTether`, standard ingredients/`workAmount` shape —
same shape as `Defs/TattooDefs/BastionWard.xml`.

## Persistence summary (Constitution Principle V)

- `HediffComp_SoulTetherEffect`'s `tierState`/`cooldownEndTick` — `Scribe_Values`/`tierState.ExposeData()` on the
  Soul Tether hediff itself, identical mechanism to every prior triggered tattoo's comp state (FR-023's
  progression-counter/tier half).
- `TattooMagic_Hediff_SoulTetherTendBuff`'s `qualityBonus`/`speedBonus` (via `HediffComp_SoulTetherTendBuff
  .CompExposeData`) and its `HediffComp_Disappears`'s own remaining-duration state (vanilla, already
  save-safe) — persisted on the *target* pawn as an ordinary hediff, independently of the caster's own Soul Tether
  hediff or tier state (FR-023's tend-buff half; FR-024's "removing the caster's hediff must not retroactively
  affect an already-applied buff").
- No new `GameComponent`/`MapComponent` is introduced — unlike Bastion Ward's ground-fixed zone, Soul Tether's
  entire runtime state lives as ordinary per-pawn hediffs (the caster's tier/cooldown state, and the target's tend
  buff), both of which use this mod's already-proven Scribe mechanisms with no new persistence design needed.
