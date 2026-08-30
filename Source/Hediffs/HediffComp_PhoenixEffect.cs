using RimWorld;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_PhoenixEffect : HediffCompProperties
    {
        public float waitDaysTier1 = 5f;
        public float waitDaysTier2 = 3f;

        public SimpleCurve decompositionChanceCurve;

        public float burnSeverityTier1 = 18f;
        public float burnSeverityTier2 = 8f;

        public int abasiaBaseDurationDays = 2;
        public int abasiaDurationIncrementDays = 1;

        public int tier2DaysAliveThreshold = 30;

        public float cauterizeChanceTier1 = 0.15f;
        public float cauterizeChanceTier2 = 0.35f;

        public float minimumQualifyingBleedRate = 0.15f;

        public int factionCap = 3;

        public HediffCompProperties_PhoenixEffect()
        {
            compClass = typeof(HediffComp_PhoenixEffect);
        }
    }

    // Phoenix's own effect: automatic revival some time after actual death
    // (the wait/retry timer itself is driven externally by
    // GameComponent_PhoenixRegistry's own sweep, not by this comp's own
    // CompPostTick, which goes silent the instant the pawn dies — research.md
    // R3), a permanent days-alive-driven Tier 1 -> 2 upgrade, and an
    // independent passive chance to auto-cauterize a severe bleeding wound.
    // Implements IProvidesTattooTierProgress directly (not via the shared,
    // composed TattooTierProgress) because this tattoo's own progression
    // counter resets to zero on every revival, unlike every other tiered
    // tattoo's monotonically-increasing counter (data-model.md).
    public class HediffComp_PhoenixEffect : HediffComp, IProvidesTattooTierProgress
    {
        private const int CauterizeCheckIntervalTicks = 60;

        public int tier = 1;

        public int daysAliveCounter;
        public int nextDaysAliveCheckTick;

        public int revivalAttemptCount;
        public int abasiaOccurrenceCount;

        public int reviveAttemptTick;
        public bool cremationGuaranteed;
        public IntVec3 cremationFallbackPos = IntVec3.Invalid;
        public Map cremationFallbackMap;

        public int nextCauterizeCheckTick;

        public HediffCompProperties_PhoenixEffect Props => (HediffCompProperties_PhoenixEffect)props;

        public string ScopeKey => parent.def.defName;

        public int Tier => tier;

        public int ProgressionCounter => daysAliveCounter;

        public int? NextTierThreshold => tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2DaysAliveThreshold", Props.tier2DaysAliveThreshold);

        // The one entry point that adds a Phoenix wearer to the faction-wide
        // registry for the ordinary "just got the tattoo" path — covers both
        // ritual application (feature 001's HediffMaker.MakeHediff/AddHediff
        // call) and ordinary pawn generation. Idempotent on the registry side
        // (data-model.md), so this can safely race with
        // Patch_Pawn_SetFaction_PhoenixRegistryUpdate's own join-side
        // registration for a pawn who joins the faction already tattooed.
        public override void CompPostMake()
        {
            base.CompPostMake();

            // FR-001's cap is scoped to "your whole faction" — a pawn who
            // isn't (yet, or at all) player-faction shouldn't occupy a
            // slot. A pawn generated already-tattooed whose faction is set
            // *after* generation is still covered — Patch_Pawn_SetFaction_
            // PhoenixRegistryUpdate's own join branch registers them once
            // their faction actually becomes Faction.OfPlayer.
            if (Pawn?.Faction == Faction.OfPlayer)
                GameComponent_PhoenixRegistry.Get()?.Register(Pawn);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // Never runs while the wearer is dead (Pawn_HealthTracker's own
            // HealthTick/HealthTickInterval early-return if Dead) — by
            // design, research.md R3. The wait/retry timer is driven
            // entirely by GameComponent_PhoenixRegistry instead.
            if (Pawn == null || Pawn.Dead)
                return;

            CheckDaysAlive();
            CheckCauterize();
        }

        private void CheckDaysAlive()
        {
            int now = Find.TickManager.TicksGame;
            if (now < nextDaysAliveCheckTick)
                return;

            nextDaysAliveCheckTick = now + GenDate.TicksPerDay;
            daysAliveCounter++;

            TryUpgradeToTier2();
        }

        // FR-019/FR-019a: shared by the real once-per-day check above and
        // the Dev Mode "set days-alive counter" tool below, which sets
        // daysAliveCounter directly rather than incrementing it.
        private void TryUpgradeToTier2()
        {
            if (tier >= 2)
                return;

            int threshold = (int)TattooEffectValues.Get(ScopeKey, "Tier2DaysAliveThreshold", Props.tier2DaysAliveThreshold);
            if (daysAliveCounter < threshold)
                return;

            tier = 2;
            revivalAttemptCount = 0;
            abasiaOccurrenceCount = 0;

            if (TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic DEBUG] Phoenix on {Pawn?.LabelShort}: reached Tier 2 after {daysAliveCounter} days alive — Paralytic Abasia counters reset.");
        }

        private void CheckCauterize()
        {
            int now = Find.TickManager.TicksGame;
            if (now < nextCauterizeCheckTick)
                return;

            nextCauterizeCheckTick = now + CauterizeCheckIntervalTicks;

            Hediff target = FindCauterizeTarget();
            if (target == null)
                return;

            bool atTier2 = tier >= 2;
            float chance = atTier2
                ? TattooEffectValues.Get(ScopeKey, "CauterizeChanceTier2", Props.cauterizeChanceTier2)
                : TattooEffectValues.Get(ScopeKey, "CauterizeChanceTier1", Props.cauterizeChanceTier1);

            bool proc = Rand.Chance(chance);

            if (TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic DEBUG] Phoenix cauterize roll on {Pawn?.LabelShort}: target={target.Label}, chance={chance:P0}, proc={proc}.");

            if (!proc)
                return;

            BodyPartRecord part = target.Part;
            var hediffs = Pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff h = hediffs[i];
                if (h.Part == part && h.Bleeding)
                    h.Tended(1f, 1f);
            }

            // FR-029: cauterization burns always use Tier 1 severity,
            // regardless of the wearer's own tier — only the proc chance
            // above scales with tier.
            float burnSeverity = TattooEffectValues.Get(ScopeKey, "BurnSeverityTier1", Props.burnSeverityTier1);

            // A missing limb's own BodyPartRecord no longer physically
            // exists on the pawn — Verse.HealthUtility rightly refuses to
            // attach a new Hediff (the burn) directly to it, logging a red
            // "Tried to add health diff to missing part" error instead
            // (found via live testing, not hypothetical: FR-027's own
            // severed-limb-priority case is exactly what triggers this,
            // since an ordinary Hediff_Injury target never has this
            // problem). Burn the stump instead — the nearest still-present
            // part up the body hierarchy — falling back to the pawn's own
            // core part in the vanishingly unlikely case the missing part
            // has no parent at all (e.g. it somehow was the body's own root
            // part).
            BodyPartRecord burnPart = target is Hediff_MissingPart
                ? part.parent ?? Pawn.RaceProps.body.corePart
                : part;
            PhoenixRevivalUtility.ApplyBurn(Pawn, burnPart, burnSeverity);
        }

        // FR-027: a bleeding severed/missing limb always outranks any
        // ordinary bleeding injury, regardless of relative BleedRate;
        // otherwise the single highest-BleedRate qualifying candidate wins.
        private Hediff FindCauterizeTarget()
        {
            float minRate = TattooEffectValues.Get(ScopeKey, "MinimumQualifyingBleedRate", Props.minimumQualifyingBleedRate);

            Hediff bestMissingPart = null;
            Hediff bestInjury = null;

            var hediffs = Pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff h = hediffs[i];
                if (!h.Bleeding)
                    continue;

                if (h is Hediff_MissingPart)
                {
                    if (bestMissingPart == null || h.BleedRate > bestMissingPart.BleedRate)
                        bestMissingPart = h;
                }
                else if (h is Hediff_Injury && h.BleedRate >= minRate)
                {
                    if (bestInjury == null || h.BleedRate > bestInjury.BleedRate)
                        bestInjury = h;
                }
            }

            return bestMissingPart ?? bestInjury;
        }

        // Dev Mode only (Source/Debug/DebugAction_TestPhoenixRevival.cs) —
        // jumps daysAliveCounter directly to a chosen value and immediately
        // re-runs the Tier 2 threshold check, so a tester doesn't have to
        // wait out the real placeholder day count.
        public void DevSetDaysAliveCounter(int value)
        {
            daysAliveCounter = value;
            nextDaysAliveCheckTick = Find.TickManager.TicksGame + GenDate.TicksPerDay;
            TryUpgradeToTier2();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref tier, "tier", 1);
            Scribe_Values.Look(ref daysAliveCounter, "daysAliveCounter", 0);
            Scribe_Values.Look(ref nextDaysAliveCheckTick, "nextDaysAliveCheckTick", 0);
            Scribe_Values.Look(ref revivalAttemptCount, "revivalAttemptCount", 0);
            Scribe_Values.Look(ref abasiaOccurrenceCount, "abasiaOccurrenceCount", 0);
            Scribe_Values.Look(ref reviveAttemptTick, "reviveAttemptTick", 0);
            Scribe_Values.Look(ref cremationGuaranteed, "cremationGuaranteed", false);
            Scribe_Values.Look(ref cremationFallbackPos, "cremationFallbackPos", IntVec3.Invalid);
            Scribe_References.Look(ref cremationFallbackMap, "cremationFallbackMap");
            Scribe_Values.Look(ref nextCauterizeCheckTick, "nextCauterizeCheckTick", 0);
        }
    }
}
