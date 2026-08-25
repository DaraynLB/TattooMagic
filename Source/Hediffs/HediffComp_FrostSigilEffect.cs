using RimWorld;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_FrostSigilEffect : HediffCompProperties
    {
        public float coldResistanceTier1 = 5f;
        public float coldResistanceTier2 = 10f;

        public float slowChanceTier1 = 0.15f;
        public float slowChanceTier2 = 0.3f;

        public float slowMoveSpeedOffsetTier1 = -1.5f;
        public float slowMoveSpeedOffsetTier2 = -2.5f;

        public int slowDurationTicksTier1 = 180;
        public int slowDurationTicksTier2 = 300;

        public float freezeChanceTier2 = 0.1f;
        public int freezeDurationTicksTier2 = 60;

        public int tier2Threshold = 15;

        public HediffCompProperties_FrostSigilEffect()
        {
            compClass = typeof(HediffComp_FrostSigilEffect);
        }
    }

    // Frost Sigil's own effect: Tier 1 passive cold resistance and a chance
    // to slow a melee attacker on hit, upgrading in place to stronger Tier 2
    // values plus a rare freeze once enough procs have landed (PRD §5.4/§6).
    public class HediffComp_FrostSigilEffect : HediffComp, IProvidesTattooStatOffset, IOnMeleeHitTattooEffect, IProvidesTattooTierProgress
    {
        public TattooTierProgress tierState = new TattooTierProgress();

        public HediffCompProperties_FrostSigilEffect Props => (HediffCompProperties_FrostSigilEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        public float GetStatOffset(StatDef stat)
        {
            if (stat != StatDefOf.ComfyTemperatureMin)
                return 0f;

            // Negative offset: lowers the minimum comfortable temperature,
            // i.e. better cold tolerance. The XML/accessor value is the
            // positive "degrees of resistance" a balance pass would tune.
            float resistance = tierState.tier >= 2
                ? TattooEffectValues.Get(ScopeKey, "ColdResistanceTier2", Props.coldResistanceTier2)
                : TattooEffectValues.Get(ScopeKey, "ColdResistanceTier1", Props.coldResistanceTier1);
            return -resistance;
        }

        public void OnMeleeHitTaken(Pawn attacker, DamageInfo dinfo, float damageDealt)
        {
            bool atTier2 = tierState.tier >= 2;

            float slowChance = atTier2
                ? TattooEffectValues.Get(ScopeKey, "SlowChanceTier2", Props.slowChanceTier2)
                : TattooEffectValues.Get(ScopeKey, "SlowChanceTier1", Props.slowChanceTier1);

            bool proc = Rand.Chance(slowChance);

            // Debug-logging-only visibility into the proc roll itself (added
            // feature 005, purely additive/diagnostic — no change to this
            // method's own behavior).
            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] Frost Sigil proc roll on {Pawn?.LabelShort} (attacker " +
                    $"{attacker?.LabelShort}): chance={slowChance:P0}, proc={proc}.");
            }

            if (!proc)
                return;

            if (atTier2)
                GrantSlow(attacker, "SlowMoveSpeedOffsetTier2", Props.slowMoveSpeedOffsetTier2, "SlowDurationTicksTier2", Props.slowDurationTicksTier2);
            else
                GrantSlow(attacker, "SlowMoveSpeedOffsetTier1", Props.slowMoveSpeedOffsetTier1, "SlowDurationTicksTier1", Props.slowDurationTicksTier1);

            // FR-003/FR-004: only successful procs count toward the Tier 2
            // threshold (PRD §5.4 — "melee hits taken where the slow procs").
            tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

            if (atTier2)
            {
                float freezeChance = TattooEffectValues.Get(ScopeKey, "FreezeChanceTier2", Props.freezeChanceTier2);
                if (Rand.Chance(freezeChance))
                {
                    int freezeDuration = (int)TattooEffectValues.Get(ScopeKey, "FreezeDurationTicksTier2", Props.freezeDurationTicksTier2);
                    attacker.stances?.stunner?.StunFor(freezeDuration, Pawn);
                }
            }
        }

        protected void GrantSlow(Pawn attacker, string offsetKey, float offsetDefault, string durationKey, int durationDefault)
        {
            Hediff slowHediff = HediffMaker.MakeHediff(TattooMagicDefOf.TattooMagic_Hediff_FrostSigilSlow, attacker);
            attacker.health.AddHediff(slowHediff);

            if (slowHediff is HediffWithComps slowHediffWithComps)
            {
                slowHediffWithComps.TryGetComp<HediffComp_FrostSigilSlow>()
                    ?.Configure(TattooEffectValues.Get(ScopeKey, offsetKey, offsetDefault));

                int duration = (int)TattooEffectValues.Get(ScopeKey, durationKey, durationDefault);
                slowHediffWithComps.TryGetComp<HediffComp_Disappears>()?.SetDuration(duration);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
        }
    }
}
