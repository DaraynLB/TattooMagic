# Phase 1 Data Model: Shedscale Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain, `TattooEffectValues` (tunable accessor) and
`TattooHediffRemovalGuard` (Shedscale's own sanctioned-removal-bypass consumer, same as every prior tattoo),
and `IProvidesTattooTierProgress` (consumed generically by `ITab_Pawn_Tattoos` via
`TattooTierProgressUtility`) — all reused, none modified. **Shedscale does not compose the shared
`TattooTierProgress` class**, for the same category of reason Phoenix (feature 014) didn't: its Tier 2
condition races two independently-paced counters (days worn vs. parts regrown) rather than one monotonic
counter against one threshold, which `TattooTierProgress.TryRegisterQualifyingEvent` assumes (research.md
R11). It implements `IProvidesTattooTierProgress` directly instead.

Three genuinely new pieces of production code — smaller in surface than any prior mechanically-rich tattoo,
because research.md R1/R7/R10 each independently eliminated a category of state or patching the design
proposal's own narrative framing seemed to call for: `HediffComp_ShedscaleEffect` (+ its properties class),
one new `Alert_ShedscaleRegrowthBlocked`, and one small `Thought_ShedscaleRegrowthActive` +
`ThoughtWorker_ShedscaleRegrowthActive` pair mirroring feature 013's exact precedent. Two new *support*
`HediffDef`s with no comps of their own beyond a vanilla one (`TattooMagic_Hediff_ShedscaleStrain`,
`TattooMagic_Hediff_ShedscaleImperfectRegrowth`). **Zero new Harmony patches** (research.md R7) — the first
mechanically-rich tattoo in this mod's roster to need none. No `contracts/` directory — nothing here is a new
interface intended for a *future* tattoo to consume (mirrors feature 012/013/014's own precedent);
`IProvidesTattooTierProgress` is reused, not extended.

## `TattooMagicDefOf` (updated — three new entries)

```csharp
public static HediffDef TattooMagic_Hediff_Shedscale;
public static HediffDef TattooMagic_Hediff_ShedscaleStrain;
public static HediffDef TattooMagic_Hediff_ShedscaleImperfectRegrowth;
```

No `TattooMagicDef TattooMagic_Shedscale` entry (unlike Phoenix's `TattooMagic_Phoenix`) — nothing in
`Dialog_ChooseTattoo` needs to identify Shedscale specifically, since it carries no rarity cap to gate on
(FR-019); every consumer that needs the tattoo's own hediff (Dev Mode debug actions, the two support-hediff
add/remove call sites) only ever needs the `HediffDef`, not the recipe-side `TattooMagicDef`.

## `HediffCompProperties_ShedscaleEffect` / `HediffComp_ShedscaleEffect`

Attached to `TattooMagic_Hediff_Shedscale` (a new, plain `HediffWithComps` — no custom `Hediff` subclass
needed, same convention as Phoenix, research.md-confirmed nothing here needs `Hediff`-level overrides).
Implements `IProvidesTattooTierProgress` directly (see header note). Needs no `IOnIncomingDamageTattooEffect`/
`IOnMeleeHit*` interface — nothing here is damage-event-shaped.

| Field (XML, on the props class) | Type | Notes |
|---|---|---|
| `tier1RegrowthDays` / `tier2RegrowthDays` | float | Days per part at each tier (FR-006): 7 / 4 |
| `tier1EfficiencyFactor` | float | The regrown part's health fraction while under the Tier 1 penalty (FR-012): placeholder `0.80` (the proposal's 75-85% range expressed as a single tunable value; authoring it as a `FloatRange` for a per-regrowth random roll is a balance-pass call this spec doesn't require, research.md R6) |
| `tier2DaysWornThreshold` | int | Consecutive days worn needed to reach Tier 2 via the "worn long enough" path (FR-016): placeholder `30` |
| `tier2PartsRegrownThreshold` | int | Lifetime parts regrown needed to reach Tier 2 via the "used often enough" path (FR-016): placeholder `3` |
| `tier1EligiblePartDefs` | `List<BodyPartDef>` | External limbs/extremities eligible at both tiers (FR-004): Finger, Toe, Hand, Foot, Arm, Leg, Ear, Nose, Jaw, Eye (research.md R3) |
| `tier2AdditionalEligiblePartDefs` | `List<BodyPartDef>` | Internal organs eligible only at Tier 2 (FR-005): Kidney, Lung (research.md R3) |

All fields read exclusively through `TattooEffectValues.Get(...)` at point of use, matching every tattoo
shipped so far; each carries the standard top-of-file XML placeholder comment pointing at this spec's own
Assumptions section.

| Field (runtime, on the comp) | Type | Persistence | Notes |
|---|---|---|---|
| `tier` | int (1 or 2) | `Scribe_Values` | Permanent once 2 (FR-017); reset to `1` only implicitly by never existing past tattoo removal (research.md R10) — never reversed while the same tattoo instance is worn |
| `daysWornCounter` | int | `Scribe_Values` | Consecutive days this specific tattoo instance has been worn; advanced by the self-gated once-per-day check inside `CompPostTick` regardless of regrowth activity |
| `partsRegrownCounter` | int | `Scribe_Values` | Lifetime count of regrowths completed by this specific tattoo instance (FR-016); incremented in `CompleteRegrowth` |
| `nextDailyCheckTick` | int | `Scribe_Values` | Self-gate for the once-per-day pass (mirrors `HediffComp_PhoenixEffect.nextDaysAliveCheckTick`'s established shape) |
| `activeRegrowths` | `Dictionary<BodyPartRecord, int>` | `Scribe_Collections` (`LookMode.BodyPart`, `LookMode.Value`) | Key = the missing part currently regrowing; value = elapsed whole days counted toward that part's tier duration so far. Entries are added the day a new eligible missing part is first observed and removed the moment that part's regrowth completes (`CompleteRegrowth`) — there is no other way for an entry to leave this dictionary (research.md R1: a part can't become "un-eligible" except by finishing or by the whole tattoo being removed, R10) |

**Behavior**:
- `Tier => tier`.
- `ActiveRegrowthCount => activeRegrowths.Count` — public; read by `ThoughtWorker_ShedscaleRegrowthActive`
  (FR-015) and by Dev Mode tooling. **Not** read by `Alert_ShedscaleRegrowthBlocked`, which scans installed
  prosthetics/bionics directly rather than this comp's own state (research.md R9) — the two mechanisms are
  intentionally independent, since a blocked part never enters `activeRegrowths` at all (research.md R1).
- `ProgressionCounter` / `NextTierThreshold` (research.md R11): computed fresh on every read as whichever of
  the two races is proportionally closer to completion —
  ```csharp
  private (int counter, int threshold) LeadingProgress()
  {
      int daysThreshold = (int)TattooEffectValues.Get(ScopeKey, "Tier2DaysWornThreshold", Props.tier2DaysWornThreshold);
      int partsThreshold = (int)TattooEffectValues.Get(ScopeKey, "Tier2PartsRegrownThreshold", Props.tier2PartsRegrownThreshold);
      float daysRatio = (float)daysWornCounter / daysThreshold;
      float partsRatio = (float)partsRegrownCounter / partsThreshold;
      return daysRatio >= partsRatio ? (daysWornCounter, daysThreshold) : (partsRegrownCounter, partsThreshold);
  }

  public int ProgressionCounter => LeadingProgress().counter;
  public int? NextTierThreshold => tier >= 2 ? (int?)null : LeadingProgress().threshold;
  ```
- `CompPostTick(ref float severityAdjustment)`: self-gated to once per in-game day
  (`Find.TickManager.TicksGame >= nextDailyCheckTick`), then runs `DoDailyPass()`.
- `DoDailyPass()`:
  1. `daysWornCounter++`; call `TryUpgradeToTier2()`.
  2. `bool malnourished = Pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition);` (FR-014).
  3. Scan `Pawn.health.hediffSet.GetMissingPartsCommonAncestors()` (research.md R1/R3): for each missing part
     whose `.Part.def` is eligible at the wearer's current tier and not already a key in `activeRegrowths`,
     add it with elapsed days `0`. (Defensively, if a leftover
     `TattooMagic_Hediff_ShedscaleImperfectRegrowth` instance already exists on that exact part from a prior
     loss-and-regrowth cycle, remove it here before starting the fresh timer — cosmetic cleanup only, since a
     genuinely fresh `Hediff_MissingPart` implies the part was lost again after last regrowing.)
  4. If **not** malnourished: for each entry in `activeRegrowths`, increment its elapsed-days value; if it now
     meets or exceeds the tier-appropriate duration for that part, call `CompleteRegrowth(part)` (which removes
     the entry). Malnourished: skip this step entirely for every tracked part (newly-added entries from step 3
     also do not advance this same tick, satisfying the malnutrition edge case in spec.md).
  5. `UpdateStrainHediff()`.
- `CompleteRegrowth(BodyPartRecord part)`:
  1. `Pawn.health.RestorePart(part)` (research.md R2) — restores the part and clears any old permanent
     injuries on it (FR-007) in one call.
  2. If `tier == 1`: add `TattooMagic_Hediff_ShedscaleImperfectRegrowth` to `part` with severity
     `part.def.GetMaxHealth(Pawn) * (1f - efficiencyFactor)`, and immediately set its
     `HediffComp_GetsPermanent.IsPermanent = true` (research.md R6, mirrors `PhoenixRevivalUtility.ApplyBurn`'s
     own immediate-permanent pattern) — skips vanilla's normal "heals or goes permanent over time" wait
     entirely, since this condition is permanent from the moment regrowth completes (FR-012).
  3. `partsRegrownCounter++`; call `TryUpgradeToTier2()`.
  4. Remove `part` from `activeRegrowths`.
- `TryUpgradeToTier2()`: no-op if `tier >= 2`; otherwise checks both thresholds
  (`daysWornCounter >= daysThreshold || partsRegrownCounter >= partsThreshold`) and, if either is met, sets
  `tier = 2` and removes every existing `TattooMagic_Hediff_ShedscaleImperfectRegrowth` instance found on
  `Pawn.health.hediffSet.hediffs` (FR-018's retroactive clear — filtering by `.def`, no separate tracking list
  needed since the `HediffDef` itself is the unique marker).
- `UpdateStrainHediff()`: if `activeRegrowths.Count > 0`, `GetFirstHediffOfDef(TattooMagic_Hediff_
  ShedscaleStrain)` or add it if absent, then set its `Severity = activeRegrowths.Count`; if `Count == 0` and
  the Strain hediff is present, `RemoveHediff` it (research.md R4).
- `CompPostPostRemoved()`: if the Strain hediff is present on `Pawn`, remove it (research.md R10) — the tattoo
  hediff's own removal doesn't cascade to a separate `HediffDef` automatically. Every already-completed
  `ShedscaleImperfectRegrowth` instance is deliberately left untouched (it represents a finished regrowth, not
  ongoing tattoo state — nothing in the spec says removing the tattoo should undo a part that already grew
  back). `activeRegrowths` itself needs no explicit clearing here — the comp instance and all its fields are
  discarded along with the `Hediff` by the same `RemoveHediff` call that triggered this method (research.md
  R10); any parts that were mid-regrowth simply have no tracking left to resume from if a new tattoo is
  applied later, exactly matching the spec's clarified removal behavior.
- `CompExposeData()`: `base.CompExposeData()` plus `Scribe_Values.Look` for `tier`/`daysWornCounter`/
  `partsRegrownCounter`/`nextDailyCheckTick` and `Scribe_Collections.Look(ref activeRegrowths, "activeRegrowths",
  LookMode.BodyPart, LookMode.Value)`.

**Validation rules**:
- `activeRegrowths` never contains a `BodyPartRecord` that is not currently missing on `Pawn` — the only way
  an entry is added is the daily missing-part scan (step 3 above), and the only ways it's removed are
  `CompleteRegrowth` (part is no longer missing, by construction — `RestorePart` just ran) or the whole comp
  ceasing to exist (research.md R10). Nothing else in this feature ever deletes an entry mid-flight, so there
  is no "was tracked, now silently isn't" state to reconcile.
- `tier` is never anything other than `1` or `2`, and never decreases while the same tattoo instance persists
  (FR-017).
- The comp only acts while attached to a live, applied `TattooMagic_Hediff_Shedscale` — the existing
  `TattooHediffRemovalGuard`/`Patch_HealthTracker_RemoveHediff_ProtectTattoos` (feature 001) already covers
  protecting it, no Shedscale-specific configuration needed.

## New `HediffDef`: `TattooMagic_Hediff_ShedscaleStrain` (support hediff, hunger/pain stacking)

Plain `Hediff` (no comps needed — `Severity` is driven entirely by `HediffComp_ShedscaleEffect.
UpdateStrainHediff`, not by any natural severity-gain rate of its own). `<stages>` keyed by ascending
`minSeverity` (1 through a placeholder cap of 5, the last stage's values applying to any higher stack count
per vanilla's own highest-applicable-stage resolution, research.md R4), each setting `hungerRateFactorOffset`
and `painOffset` scaled to that stack count — e.g. stage `minSeverity=1` a small offset pair, each subsequent
stage larger, all placeholder values pending the balance pass. Lives at `Defs/HediffDefs/` directly (not
`Tattoos/`), mirroring `TattooTracker.xml`/`FrostSigilSlow.xml`/`ParalyticAbasia.xml`'s own precedent for
support hediffs that aren't themselves an applied tattoo.

## New `HediffDef`: `TattooMagic_Hediff_ShedscaleImperfectRegrowth` (support hediff, per-part efficiency penalty)

`hediffClass = Hediff_Injury` (not a custom subclass) — the same class vanilla itself uses for old, permanent
scars, deliberately reused rather than reinvented (research.md R6). One comp:
`HediffCompProperties_GetsPermanent`, force-activated immediately at apply time
(`HediffComp_ShedscaleEffect.CompleteRegrowth`, above) rather than left to vanilla's normal
heals-or-becomes-permanent timer. No `<stages>` of its own — a `Hediff_Injury`'s severity directly reduces
`HediffSet.GetPartHealth(part)` for whichever specific `BodyPartRecord` it's attached to, which every vanilla
capacity calculation (Manipulation for a hand/arm, Moving for a leg/foot, Sight for an eye, Hearing for an
ear, etc.) already reads generically — this is why no per-part-type `capMods` list is needed on this mod's
side at all: reducing the part's own reported health, the same mechanism vanilla's own old-wound scars use, is
the universal one that automatically feeds whichever capacity that specific part happens to serve. Lives at
`Defs/HediffDefs/` directly, same reasoning as the Strain hediff above.

## New `ThoughtDef` + pair: `TattooMagic_Thought_ShedscaleRegrowthActive`

```csharp
public class Thought_ShedscaleRegrowthActive : Thought_Situational
{
    protected override float BaseMoodOffset =>
        TattooEffectValues.Get("TattooMagic_Hediff_Shedscale", "RegrowthMoodPenalty", CurStage.baseMoodEffect);
}

public class ThoughtWorker_ShedscaleRegrowthActive : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        HediffComp_ShedscaleEffect comp = FindComp(p); // same lookup shape as ThoughtWorker_StarlightWardTier2Active
        return (comp != null && comp.ActiveRegrowthCount > 0) ? ThoughtState.ActiveDefault : ThoughtState.Inactive;
    }
}
```

Exact structural mirror of feature 013's `Thought_StarlightWardTier2Active`/`ThoughtWorker_
StarlightWardTier2Active` (research.md R5), with `baseMoodEffect` authored negative in XML. Zero persisted
mood state — `ActiveRegrowthCount` is read live from the comp on every mood recalculation, so the debuff
appears and disappears in lockstep with `activeRegrowths.Count` crossing zero with no separate bookkeeping
(FR-015).

## New `Alert_ShedscaleRegrowthBlocked` (`Source/UI/` or `Source/Effects/` — no strong existing precedent for where UI-alert classes live in this mod yet; placed in `Source/UI/` alongside `Dialog_ChooseTattoo`/`ITab_Pawn_Tattoos`, since it's colonist-facing UI surface, not a tattoo-effect mechanism)

```csharp
public class Alert_ShedscaleRegrowthBlocked : Alert
{
    private List<Pawn> blockedPawnsResult = new List<Pawn>();

    private List<Pawn> BlockedPawns
    {
        get
        {
            blockedPawnsResult.Clear();
            foreach (Pawn p in PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended)
            {
                if (!HasShedscale(p)) continue;
                if (HasBlockedEligiblePart(p)) blockedPawnsResult.Add(p);
            }
            return blockedPawnsResult;
        }
    }

    public Alert_ShedscaleRegrowthBlocked() { defaultLabel = "AlertShedscaleRegrowthBlocked".Translate(); }

    public override TaggedString GetExplanation() { /* lists blockedPawnsResult with NameShortColored, mirrors Alert_Hypothermia */ }

    public override AlertReport GetReport() => AlertReport.CulpritsAre(BlockedPawns);
}
```

`HasBlockedEligiblePart(Pawn p)`: true if `p.health.hediffSet.hediffs` contains any `Hediff_AddedPart` whose
`.Part.def` is in the wearer's tier-appropriate eligible list (`HediffComp_ShedscaleEffect`'s own props,
resolved via `TattooTierProgressUtility`-style lookup) — i.e., a prosthetic/bionic/peg leg occupying a slot
that would otherwise be eligible for natural regrowth (research.md R1/R9). This scan is entirely independent
of `activeRegrowths` (a blocked part is, by construction, never tracked there) and needs no comp-side state at
all — it re-derives the answer fresh from `HediffSet` on every alert query, matching vanilla's own alert
convention (research.md R9).

## New XML: `Defs/HediffDefs/Tattoos/Shedscale.xml`

`hediffClass = HediffWithComps` (no custom subclass, per the comp's own header note); `<comps>` carries
`HediffCompProperties_ShedscaleEffect` with every tunable from the table above as a placeholder value, each
under a top-of-file comment pointing at this spec's own Assumptions section.

## New XML: `Defs/HediffDefs/ShedscaleStrain.xml` and `Defs/HediffDefs/ShedscaleImperfectRegrowth.xml`

Support hediffs, per their own sections above — both sit directly under `Defs/HediffDefs/`, not `Tattoos/`.

## New XML: `Defs/ThoughtDefs/Shedscale.xml`

Mirrors `Defs/ThoughtDefs/StarlightWard.xml`'s exact shape (one stage, `thoughtClass`/`workerClass` pointing
at the pair above, placeholder negative `baseMoodEffect`).

## New XML: `Defs/TattooDefs/Shedscale.xml`

`tattooType = Passive` (fully automatic — no player-activated gizmo, matching Phoenix's own reasoning for the
same field), `appliedHediff = TattooMagic_Hediff_Shedscale`, ingredients/`workAmount` per the standard
ritual-application shape every prior tattoo already has. No cap-related field — unlike Phoenix, Shedscale
carries no rarity cap (FR-019), so `Dialog_ChooseTattoo`'s existing tattoo-listing loop needs **no new
condition at all** for this feature — the single largest structural difference from Phoenix's own plan.

## Persistence summary (Constitution Principle V)

- `HediffComp_ShedscaleEffect`'s full state table (above) — `Scribe_Values`/`Scribe_Collections` on the
  Shedscale hediff itself, the same mechanism every prior tattoo's comp state already uses. Deterministic and
  idempotent across save/reload mid-regrowth: `activeRegrowths`' per-part elapsed-days values simply resume
  counting on the next in-game day after load, with no special "resuming a wait" code path needed (contrast
  with Phoenix's own absolute-tick `reviveAttemptTick`, which needed re-evaluation against a resumed
  `TicksGame` — Shedscale's own counters are already whole relative day counts, so a reload mid-day at worst
  loses a few in-progress hours of the *current* day, never a whole tracked day, since the daily gate only
  commits progress once a full day has elapped).
- The two support hediffs (`ShedscaleStrain`, `ShedscaleImperfectRegrowth`) need no Shedscale-specific
  persistence code at all — once added to the pawn, their `Severity`/permanence state is saved/loaded by
  `Hediff`'s own base `ExposeData()`/`HediffComp_GetsPermanent.CompExposeData()`, identical to any other
  vanilla hediff.
- `Alert_ShedscaleRegrowthBlocked` and `ThoughtWorker_ShedscaleRegrowthActive` persist nothing at all by
  design (research.md R5/R9) — both re-derive their answer fresh from live pawn/hediff state on every query.
