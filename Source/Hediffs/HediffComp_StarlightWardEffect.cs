using RimWorld;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    public class HediffCompProperties_StarlightWardEffect : HediffCompProperties
    {
        public float mentalBreakResistanceTier1 = -0.03f;
        public float mentalBreakResistanceTier2 = -0.06f;

        public float psychicSensitivityResistanceTier1 = -0.10f;
        public float psychicSensitivityResistanceTier2 = -0.20f;

        public float moodBuffTier2 = 3f;

        public int tier2Threshold = 5;

        public HediffCompProperties_StarlightWardEffect()
        {
            compClass = typeof(HediffComp_StarlightWardEffect);
        }
    }

    // Starlight Ward's own effect: Tier 1 passive mental-break and
    // psychic-sensitivity resistance (both are "lower is better" stats,
    // research.md R1/R2 — the offsets below are negative, unlike every prior
    // stat-offset tattoo), upgrading in place to stronger resistance plus a
    // Tier 2-exclusive passive mood buff once enough "mental-break risk
    // resisted" events have occurred (PRD §5.4/§6). Unlike every prior
    // passive tattoo, this qualifying event isn't damage-based — detection
    // is self-contained CompPostTick polling of already-public vanilla
    // MentalBreaker state (research.md R4), not a Harmony patch.
    public class HediffComp_StarlightWardEffect : HediffComp, IProvidesTattooStatOffset, IProvidesTattooTierProgress
    {
        public TattooTierProgress tierState = new TattooTierProgress();

        // Risk-window tracking (research.md R4) — a contiguous span where the
        // wearer's mood is imminently at risk of a mental break. Counts as a
        // resisted event only if it closes without a break ever occurring
        // during it.
        public bool inRiskWindow;
        public bool riskWindowHadBreak;
        public int nextRiskCheckTick;

        private const int RiskCheckIntervalTicks = 60;

        public HediffCompProperties_StarlightWardEffect Props => (HediffCompProperties_StarlightWardEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        public float GetStatOffset(StatDef stat)
        {
            bool atTier2 = tierState.tier >= 2;

            if (stat == StatDefOf.MentalBreakThreshold)
            {
                return atTier2
                    ? TattooEffectValues.Get(ScopeKey, "MentalBreakResistanceTier2", Props.mentalBreakResistanceTier2)
                    : TattooEffectValues.Get(ScopeKey, "MentalBreakResistanceTier1", Props.mentalBreakResistanceTier1);
            }

            if (stat == StatDefOf.PsychicSensitivity)
            {
                return atTier2
                    ? TattooEffectValues.Get(ScopeKey, "PsychicSensitivityResistanceTier2", Props.psychicSensitivityResistanceTier2)
                    : TattooEffectValues.Get(ScopeKey, "PsychicSensitivityResistanceTier1", Props.psychicSensitivityResistanceTier1);
            }

            return 0f;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || Pawn.Dead)
                return;

            int now = Find.TickManager.TicksGame;
            if (now < nextRiskCheckTick)
                return;

            nextRiskCheckTick = now + RiskCheckIntervalTicks;

            MentalBreaker breaker = Pawn.mindState?.mentalBreaker;
            if (breaker == null)
                return;

            bool isImminentNow = breaker.BreakMinorIsImminent || breaker.BreakMajorIsImminent || breaker.BreakExtremeIsImminent;

            if (isImminentNow && !inRiskWindow)
            {
                inRiskWindow = true;
                riskWindowHadBreak = false;

                if (TattooMagicSettings.EnableDebugLogging)
                    Log.Message($"[TattooMagic DEBUG] Starlight Ward risk window opened for {Pawn.LabelShort}.");
            }

            // Checked unconditionally (not nested under isImminentNow) — every
            // Break*IsImminent property requires pawn.MentalStateDef == null,
            // so the instant a break actually starts, isImminentNow flips to
            // false on this same check, the same tick InMentalState becomes
            // true. Nesting this under isImminentNow would mean it could
            // never fire, since those two conditions are mutually exclusive
            // by MentalBreaker's own definition (research.md R4 follow-up).
            if (inRiskWindow && Pawn.InMentalState)
                riskWindowHadBreak = true;

            if (!isImminentNow && inRiskWindow)
            {
                inRiskWindow = false;

                if (!riskWindowHadBreak)
                {
                    int thresholdBefore = (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);
                    bool tieredUp = tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

                    if (TattooMagicSettings.EnableDebugLogging)
                        Log.Message($"[TattooMagic DEBUG] Starlight Ward risk window resisted for {Pawn.LabelShort}: " +
                            $"tier={tierState.tier}, progress={tierState.progressionCounter}/{thresholdBefore}{(tieredUp ? " — TIER UP!" : "")}.");
                }
                else if (TattooMagicSettings.EnableDebugLogging)
                {
                    Log.Message($"[TattooMagic DEBUG] Starlight Ward risk window broken for {Pawn.LabelShort} — not counted as resisted.");
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
            Scribe_Values.Look(ref inRiskWindow, "inRiskWindow", false);
            Scribe_Values.Look(ref riskWindowHadBreak, "riskWindowHadBreak", false);
            Scribe_Values.Look(ref nextRiskCheckTick, "nextRiskCheckTick", 0);
        }
    }
}
