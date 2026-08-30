# Phase 1 Data Model: Phoenix Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain, feature 002/003's `TattooEffectValues`
tunable accessor and `TattooHediffRemovalGuard` (Phoenix's own sanctioned-removal-bypass consumer, same as
every prior tattoo), and `IProvidesTattooTierProgress` (feature 002/consumed generically by
`ITab_Pawn_Tattoos`) — all reused, none modified. **Phoenix does not compose the shared `TattooTierProgress`
class** (research.md deliberately doesn't cover this — see the note under `HediffComp_PhoenixEffect` below);
it implements `IProvidesTattooTierProgress` directly with its own fields, since its Tier-2 progression
counter (consecutive days alive) resets on every revival — semantically different from every other tattoo's
monotonically-increasing `progressionCounter`, which `TattooTierProgress.TryRegisterQualifyingEvent` assumes.

Five genuinely new pieces of production code, none of which existed in any form before this feature
(research.md's own framing: this is the first tattoo to touch death, corpses, or faction-wide state at all):
`HediffComp_PhoenixEffect` (+ its properties class), a new `GameComponent_PhoenixRegistry`, a small
`PhoenixRevivalUtility` static helper, three new Harmony patches, and one new debuff `HediffDef`
(`TattooMagic_Hediff_ParalyticAbasia`) with no dedicated comp of its own (reuses vanilla's
`HediffComp_Disappears`, research.md R10). No `contracts/` directory — nothing here is a new interface
intended for a *future* tattoo to consume (mirrors feature 012/013's own precedent of skipping `contracts/`
when nothing generically reusable is introduced); `IProvidesTattooTierProgress` is reused, not extended.

## `TattooMagicDefOf` (updated — three new entries)

```csharp
public static TattooMagicDef TattooMagic_Phoenix;
public static HediffDef TattooMagic_Hediff_Phoenix;
public static HediffDef TattooMagic_Hediff_ParalyticAbasia;
```

`TattooMagic_Phoenix` (the recipe-side `TattooMagicDef`, not just its hediff) is needed so
`Dialog_ChooseTattoo`'s cap check (below) can identify "is this iterated tattoo Phoenix specifically" without
a defName string comparison — mirrors how every other `[DefOf]` entry in this class is resolved by name.

## `HediffCompProperties_PhoenixEffect` / `HediffComp_PhoenixEffect`

Attached to `TattooMagic_Hediff_Phoenix` (a new, plain `HediffWithComps` — no custom `Hediff` subclass needed,
research.md R3: the wait/retry timer is driven externally by `GameComponent_PhoenixRegistry`, not by any
`Hediff`-level death/destroy override). Implements `IProvidesTattooTierProgress` directly (not via composed
`TattooTierProgress`, see header note) and needs no `IOnIncomingDamageTattooEffect`/`IOnMeleeHit*` interface
— neither the revival mechanic nor the cauterize passive is damage-event-shaped the way those interfaces
assume.

| Field (XML, on the props class) | Type | Notes |
|---|---|---|
| `waitDaysTier1` / `waitDaysTier2` | int | Days after death before the first revival attempt (FR-010): 5 / 3 |
| `decompositionChanceCurve` | `SimpleCurve` | X = corpse `RotProgress` in days, Y = revival success chance (0–1). Mirrors `TattooMagicDef.successCurveOverride`/`TattooRitualSuccessCurve`'s existing `SimpleCurve` convention (feature 001). Authored with a final control point at a small nonzero Y (e.g. `0.02`) so `SimpleCurve.Evaluate`'s standard clamp-to-last-point behavior gives FR-013's "approaches but never mathematically reaches zero" for free, with no separate floor-clamping code needed |
| `burnSeverityTier1` / `burnSeverityTier2` | float | Severity applied to the new/merged `Burn` hediff on a revival attempt (FR-015). Cauterization bursts always use `burnSeverityTier1` regardless of the wearer's own tier (FR-029) |
| `abasiaBaseDurationDays` | int | Paralytic Abasia's duration on its first occurrence for a colonist (FR-018): 2 |
| `abasiaDurationIncrementDays` | int | Added per subsequent occurrence (FR-018): 1 |
| `tier2DaysAliveThreshold` | int | Consecutive days alive (while equipped) needed for the permanent Tier 1 → 2 upgrade (FR-019) |
| `cauterizeChanceTier1` / `cauterizeChanceTier2` | float | Passive per-check proc chance (FR-029) |
| `minimumQualifyingBleedRate` | float | Trivial-wound exclusion floor for cauterization eligibility (FR-026) |
| `factionCap` | int | The rarity cap (FR-001): 3. Lives here (read via `TattooEffectValues`, Constitution Principle III) rather than a bare code constant, consistent with every other tunable in this feature, even though it is read by `GameComponent_PhoenixRegistry`/`Dialog_ChooseTattoo`, not by this comp itself — `TattooMagic_Hediff_Phoenix`'s own defName is still the natural `ScopeKey` for it |

All fields read exclusively through `TattooEffectValues.Get(...)` at point of use (FR-... every numeric in
this feature), matching every tattoo shipped so far; each carries the standard top-of-file XML placeholder
comment pointing at PRD §9 / this spec's own Assumptions section.

| Field (runtime, on the comp) | Type | Persistence | Notes |
|---|---|---|---|
| `tier` | int (1 or 2) | `Scribe_Values` | Permanent once 2; never reverts on death (FR-019) |
| `daysAliveCounter` | int | `Scribe_Values` | Consecutive days alive while equipped; reset to `0` on every revival (FR-019); advanced by a self-gated once-per-day check inside `CompPostTick` while `Pawn != null && !Pawn.Dead` |
| `nextDaysAliveCheckTick` | int | `Scribe_Values` | Self-gate for the above (mirrors `HediffComp_VampiricThornEffect.nextPostKillHealTick`'s established interval-gate shape) |
| `revivalAttemptCount` | int | `Scribe_Values` | Every attempt (success or failure) increments this; used for "3rd lifetime attempt onward" (FR-017). Reset to `0` only at the moment `tier` becomes `2` (FR-019a) — **not** on every revival, unlike `daysAliveCounter` |
| `abasiaOccurrenceCount` | int | `Scribe_Values` | Times the debuff has actually been applied; drives the escalating duration (FR-018). Reset to `0` together with `revivalAttemptCount` at the Tier-2 upgrade moment (FR-019a) |
| `reviveAttemptTick` | int | `Scribe_Values`, default `0` | Absolute `Find.TickManager.TicksGame` value of the next scheduled attempt; `0` means "no pending revival." Set by the `GameComponent` sweep the first time it observes this wearer `Dead` with no pending attempt yet; advanced by `+1 day` on every failed natural attempt (FR-012) |
| `cremationGuaranteed` | bool | `Scribe_Values`, default `false` | Set by `Patch_RecipeWorker_ConsumeIngredient_PhoenixCremation` (research.md R4/R5). Once true, the next resolved attempt bypasses the decomposition roll entirely (FR-020) and skips the "corpse destroyed" permanent-loss check (research.md R6) that would otherwise fire once the corpse is gone |
| `cremationFallbackPos` | `IntVec3` | `Scribe_Values` | Captured at cremation time (`corpse.PositionHeld`), used only as `GenSpawn.Spawn`'s fallback location if `ResurrectionUtility.TryResurrect` leaves the pawn unspawned because its corpse no longer exists (research.md R4) |
| `cremationFallbackMap` | `Map` | `Scribe_References` | Captured at cremation time (`corpse.MapHeld`); paired with `cremationFallbackPos` above |
| `nextCauterizeCheckTick` | int | `Scribe_Values` | Self-gate for the ~1×/second passive bleed check (research.md R8), independent of every field above (FR-025: no shared cooldown/state with the revival mechanic) |

**Behavior**:
- `Tier => tier`; `ProgressionCounter => daysAliveCounter`; `NextTierThreshold => tier >= 2 ? (int?)null :
  (int)TattooEffectValues.Get(ScopeKey, "Tier2DaysAliveThreshold", Props.tier2DaysAliveThreshold)` —
  satisfies `IProvidesTattooTierProgress` for `ITab_Pawn_Tattoos` with no reliance on `TattooTierProgress`.
- `CompPostMake()`: `base.CompPostMake()`, then `Current.Game?.GetComponent<GameComponent_PhoenixRegistry>()
  ?.Register(Pawn)`. This is the **one** registration entry point for the ordinary path (ritual application
  grants the hediff via the same `HediffMaker.MakeHediff`/`pawn.health.AddHediff` call every tattoo's ritual
  already uses, feature 001 — `CompPostMake` fires once, automatically, the instant that happens, no
  Phoenix-specific hook needed in `JobDriver_TattooRitual` itself) — this is also fired during ordinary pawn
  generation, which is what makes the "joins the faction already wearing Phoenix" case (FR-005) work
  correctly even *before* `Patch_Pawn_SetFaction_PhoenixRegistryUpdate`'s own join-side registration runs:
  `Register` is idempotent (data-model.md's `GameComponent_PhoenixRegistry` validation rules), so whichever
  of the two paths reaches a given pawn first is harmless — a pawn generated with the hediff already on it
  and *then* faction-set to the player registers once via `CompPostMake`, and the faction-change postfix's own
  `Register` call is then a no-op; a pawn whose faction is set first and hediff added second (unlikely given
  generation order, but not load-bearing either way) registers via the postfix instead, and `CompPostMake`'s
  call is the no-op. Neither ordering can produce a missed or duplicate registration.
- `CompPostTick(ref float severityAdjustment)` (only ever runs while `Pawn` is alive, research.md R3):
  1. Self-gated days-alive advance: once per in-game day, `daysAliveCounter++`; if `tier == 1 &&
     daysAliveCounter >= threshold`, set `tier = 2` and reset `revivalAttemptCount`/`abasiaOccurrenceCount`
     to `0` (FR-019/FR-019a) in the same step.
  2. Self-gated cauterize check (research.md R8): scan for the highest-priority qualifying bleed, roll
     `cauterizeChanceTier1`/`Tier2`, on success `Tended(...)` every qualifying bleed on that one body part and
     apply a burn (`PhoenixRevivalUtility.ApplyBurn`, shared with revival attempts, always at Tier-1 severity
     here per FR-029).
- `CompExposeData()`: `base.CompExposeData()` plus `Scribe_Values.Look`/`Scribe_References.Look` for every
  runtime field in the table above.

**Validation rules**:
- `tier` is never anything other than `1` or `2`, and never decreases (FR-019's "not reversed by any later
  death").
- `reviveAttemptTick` and `cremationGuaranteed`/the two fallback fields are only ever meaningful while `Pawn
  .Dead`; the `GameComponent` sweep is solely responsible for setting/clearing them (this comp's own
  `CompPostTick` never runs post-death to touch them, research.md R3).
- The comp only acts while attached to a live, applied `TattooMagic_Hediff_Phoenix` — the existing
  `TattooHediffRemovalGuard`/`Patch_HealthTracker_RemoveHediff_ProtectTattoos` (feature 003) already covers
  protecting it, no Phoenix-specific configuration needed (same generic `appliedHediff` registry every tattoo
  uses).

## `PhoenixRevivalUtility` (new static helper, `Source/Effects/`)

Pure logic factored out of `GameComponent_PhoenixRegistry`'s sweep so a Dev Mode debug action (quickstart.md)
can also call it directly to force-resolve an attempt without waiting real/simulated days — mirrors why
`TattooHealingUtility`/`TattooRitualSuccessCurve` are their own static classes rather than inlined.

```csharp
public static class PhoenixRevivalUtility
{
    // Called once per pending wearer per sweep tick when reviveAttemptTick has arrived.
    // Returns true if the pawn is now alive again.
    public static bool TryResolveAttempt(Pawn pawn, HediffComp_PhoenixEffect comp);

    // Shared by both revival attempts (tier-appropriate severity) and cauterization
    // (always Tier 1 severity, research.md R9/FR-029). Marks the new Burn hediff
    // permanent immediately (HediffComp_GetsPermanent) so it survives
    // ResurrectionUtility.TryResurrect's own hediff-cleanup pass on success —
    // research.md R9's corrected finding, caught via live testing.
    public static void ApplyBurn(Pawn pawn, BodyPartRecord part, float severity);

    // FR-016/FR-017: always increments revivalAttemptCount; applies the debuff only
    // when revivalAttemptCount (post-increment) >= 3 AND the attempt succeeded.
    public static void RegisterAttemptAndMaybeApplyAbasia(Pawn pawn, HediffComp_PhoenixEffect comp, bool succeeded);
}
```

**`TryResolveAttempt` behavior**:
1. If `!comp.cremationGuaranteed`: re-check the permanent-loss conditions (research.md R6 — corpse null/
   destroyed/dessicated/buried, **or** FR-008's head/brain-intact gate). If any is true, this is a permanent
   failure: clear `reviveAttemptTick`, unregister from `GameComponent_PhoenixRegistry`, return `false` — no
   burn, no attempt counted (the wearer is gone, not merely unlucky).
2. Roll success: `comp.cremationGuaranteed` → always succeed; else evaluate
   `Props.decompositionChanceCurve.Evaluate(rotProgressDays)` and `Rand.Chance(...)`.
3. `ApplyBurn` at the tier-appropriate severity, always (success or failure, FR-015).
4. `RegisterAttemptAndMaybeApplyAbasia(pawn, comp, succeeded)`.
5. On failure: `comp.reviveAttemptTick = Find.TickManager.TicksGame + GenDate.TicksPerDay` (FR-012's daily
   retry); `comp.cremationGuaranteed` stays whatever it was (a cremation guarantee earned mid-wait persists
   across a failed... though by design a guaranteed attempt cannot fail, so this branch is natural-path-only).
6. On success: call `ResurrectionUtility.TryResurrect(pawn, ...)`; apply the cremation fallback spawn if
   needed (research.md R4); reset `comp.daysAliveCounter = 0`/`nextDaysAliveCheckTick` (FR-019's "resets on
   every revival"); clear `reviveAttemptTick`/`cremationGuaranteed`/both fallback fields.

## `GameComponent_PhoenixRegistry` (new, `Source/Effects/`)

The single faction-wide source of truth for both the rarity cap and the revival-sweep driver
(research.md R3/R11). Auto-added by vanilla's own `Game.FillComponents()` — no registration code needed.

```csharp
public class GameComponent_PhoenixRegistry : GameComponent
{
    public List<Pawn> registeredWearers = new List<Pawn>();
    private int nextSweepTick;

    public GameComponent_PhoenixRegistry(Game game) { }

    public bool IsAtCap => registeredWearers.Count >= (int)TattooEffectValues.Get(
        TattooMagicDefOf.TattooMagic_Hediff_Phoenix.defName, "FactionCap",
        /* HediffCompProperties_PhoenixEffect default */ 3);

    public void Register(Pawn pawn) { if (!registeredWearers.Contains(pawn)) registeredWearers.Add(pawn); }

    public void Unregister(Pawn pawn) => registeredWearers.Remove(pawn);

    public override void GameComponentTick() { /* self-gated sweep, research.md R3 */ }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref registeredWearers, "registeredWearers", LookMode.Reference);
        Scribe_Values.Look(ref nextSweepTick, "nextSweepTick", 0);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && registeredWearers == null)
            registeredWearers = new List<Pawn>();
    }
}
```

**`GameComponentTick()` behavior** (self-gated to `Find.TickManager.TicksGame >= nextSweepTick`, then
`nextSweepTick = TicksGame + SweepIntervalTicks`, a small constant ~2,000 ticks): for each `Pawn` in
`registeredWearers`:
- Not `Dead` → nothing to do (alive-state bookkeeping is entirely the comp's own `CompPostTick`'s job).
- `Dead`, and the comp's `reviveAttemptTick == 0` → this is a newly-observed death; schedule the first wait:
  `reviveAttemptTick = TicksGame + waitDays(tier) * GenDate.TicksPerDay` (FR-010), gated first by the
  head/brain/not-destroyed precondition (FR-008) — if it already fails at this point, skip scheduling
  entirely and unregister immediately (the wearer was already ineligible the moment they died, e.g. killed by
  a headshot).
- `Dead`, `reviveAttemptTick != 0`, and `TicksGame >= reviveAttemptTick` → `PhoenixRevivalUtility
  .TryResolveAttempt(pawn, comp)`.
- `Dead`, `reviveAttemptTick != 0`, `TicksGame < reviveAttemptTick`, and **not** `cremationGuaranteed` →
  re-check the permanent-loss conditions (research.md R6) every sweep, not only at the scheduled attempt tick,
  so a corpse destroyed/buried mid-wait is noticed promptly rather than only discovered days later at the
  next scheduled roll.

**Validation rules**:
- `registeredWearers` never contains duplicates (`Register` is idempotent) and never contains a pawn who no
  longer carries `TattooMagic_Hediff_Phoenix` for more than one sweep interval (every removal path —
  permanent loss, faction-leave, the future removal ritual's integration point — calls `Unregister`
  directly rather than relying on the sweep to notice).
- `IsAtCap` counts every registered pawn regardless of alive/dead-pending-revival state (FR-002) — this falls
  out naturally from `registeredWearers` never distinguishing the two; a pawn is only ever added once, at
  first application or at faction-join, and only ever removed on permanent loss.
- FR-005 (join-while-over-cap) is satisfied by `Register` having no cap check at all — only the *new
  application* path (`Dialog_ChooseTattoo`, below) checks `IsAtCap`; joining already-tattooed always
  registers.

## `Dialog_ChooseTattoo` (updated — one new condition in the existing tattoo-listing loop)

```csharp
foreach (TattooMagicDef tattoo in DefDatabase<TattooMagicDef>.AllDefsListForReading)
{
    if (recipientTracker.HasTattoo(tattoo))
        continue;

    // FR-006: Phoenix isn't offered once the faction is at/over the cap.
    if (tattoo == TattooMagicDefOf.TattooMagic_Phoenix
        && PhoenixRegistryUtility.Get()?.IsAtCap == true)
        continue;

    if (listing.ButtonText(tattoo.label))
        TryQueueRitual(recipientTracker, tattoo);
}
```

`PhoenixRegistryUtility.Get()` is a one-line static wrapper around `Current.Game?.GetComponent
<GameComponent_PhoenixRegistry>()`, mirroring `TattooTrackerUtility.GetTracker`'s existing lookup-wrapper
shape (kept as a tiny static method on `GameComponent_PhoenixRegistry` itself rather than a whole new file,
since it's a one-liner).

## Three new Harmony patches (`Source/Patches/`)

| Patch class | Target | Kind | Purpose |
|---|---|---|---|
| `Patch_RecipeWorker_ConsumeIngredient_PhoenixCremation` | `Verse.RecipeWorker.ConsumeIngredient` | Prefix | Detects `CremateCorpse` on a Phoenix-tattooed corpse; sets `cremationGuaranteed` + captures fallback spawn location before `Destroy()` runs (research.md R4/R5) |
| `Patch_Pawn_SetFaction_PhoenixRegistryUpdate` | `Verse.Pawn.SetFaction` | Postfix | Strip + unregister on leaving `Faction.OfPlayer` (FR-004); register (allowed over cap) on joining `Faction.OfPlayer` already tattooed (FR-005) |
| `Patch_PawnGuestTracker_SetGuestStatus_PhoenixCaptured` | `RimWorld.Pawn_GuestTracker.SetGuestStatus` | Postfix | Strip + unregister when a Phoenix-tattooed colonist is captured as another faction's prisoner — the one case `SetFaction` itself doesn't cover (research.md R7) |

Both faction-change patches funnel into one shared helper (e.g. `PhoenixFactionLeaveUtility
.StripIfNoLongerOurs(Pawn pawn)`) so the strip-and-unregister logic exists exactly once. No patch targets
`Corpse.TickRare`, `Pawn.Kill`, `Pawn_HealthTracker.Kill`, or any `Hediff.Notify_*` override — the
`GameComponent` sweep (research.md R3) replaces all of that.

## New `HediffDef`: `TattooMagic_Hediff_ParalyticAbasia`

Plain `HediffWithComps`, `<stages>` with severe `capMods` (near-zero `Moving`/`Manipulation`/
`Consciousness`), one comp: vanilla's own `HediffCompProperties_Disappears`. No custom `HediffComp` — its
duration is set programmatically at apply time (research.md R10):

```csharp
Hediff abasia = HediffMaker.MakeHediff(TattooMagicDefOf.TattooMagic_Hediff_ParalyticAbasia, pawn);
pawn.health.AddHediff(abasia);
var disappears = abasia.TryGetComp<HediffComp_Disappears>();
int durationTicks = (durationDays) * GenDate.TicksPerDay;
disappears.ticksToDisappear = durationTicks;
disappears.disappearsAfterTicks = durationTicks;
```

where `durationDays = Props.abasiaBaseDurationDays + (comp.abasiaOccurrenceCount - 1) *
Props.abasiaDurationIncrementDays`, evaluated *after* `abasiaOccurrenceCount` has already been incremented
for this occurrence (first occurrence: `1` → base duration; second: `2` → base + 1 increment; etc.).

## New XML: `Defs/HediffDefs/Tattoos/Phoenix.xml`

`hediffClass` = `HediffWithComps` (no custom subclass, per the comp's own header note); `<comps>` carries
`HediffCompProperties_PhoenixEffect` with every tunable from the table above as a placeholder value, each
under a top-of-file comment pointing at this spec's own Assumptions section (the decomposition curve and
Tier-2 day threshold are explicitly called out there as balance-pass placeholders, mirroring every prior
tattoo's `<!-- placeholders pending PRD §9 -->` convention).

## New XML: `Defs/HediffDefs/ParalyticAbasia.xml`

Sits directly under `Defs/HediffDefs/` (not the `Tattoos/` subfolder — mirrors `TattooTracker.xml`/
`FrostSigilSlow.xml`, both existing *support* hediffs that live at this same level rather than under
`Tattoos/`, since Paralytic Abasia isn't itself an applied tattoo).

## New XML: `Defs/TattooDefs/Phoenix.xml`

`tattooType = Passive` (no player-activated gizmo — revival is automatic-on-death, cauterization is a
passive proc; `Triggered` in this mod's existing vocabulary means "grants an activatable ability," which
Phoenix never does), `appliedHediff = TattooMagic_Hediff_Phoenix`, ingredients/`workAmount` per the standard
ritual-application shape every prior tattoo already has.

## Persistence summary (Constitution Principle V)

- `HediffComp_PhoenixEffect`'s full state table (above) — `Scribe_Values`/`Scribe_References` on the Phoenix
  hediff itself, the same mechanism every prior tattoo's comp state already uses. Deterministic and
  idempotent across save/reload mid-wait: a pending revival's `reviveAttemptTick` and `cremationGuaranteed`
  flag resume exactly as they were: the `GameComponent` sweep simply re-evaluates `TicksGame >=
  reviveAttemptTick` fresh on the next post-load tick, with no special "resuming a wait" code path needed.
- `GameComponent_PhoenixRegistry.registeredWearers` — `Scribe_Collections.Look(..., LookMode.Reference)`,
  auto-persisted as part of `Game.components`'s own generic polymorphic Scribe pass (research.md R11) with no
  extra wiring; `LookMode.Reference` correctly resolves a dead-but-not-yet-revived pawn the same way any other
  cross-save `Pawn` reference does (e.g. `TattooMagicDef.appliedHediff`... more precisely, the same mechanism
  vanilla itself uses for `Corpse.InnerPawn` and every other long-lived `Pawn` reference across save/load).
- The Paralytic Abasia debuff itself needs no Phoenix-specific persistence at all — once applied, its
  `HediffComp_Disappears.ticksToDisappear` countdown is saved/loaded by that vanilla comp's own existing
  `ExposeData()`, identical to any other timed vanilla hediff.
