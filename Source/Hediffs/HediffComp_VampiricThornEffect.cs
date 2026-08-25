using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_VampiricThornEffect : HediffCompProperties
    {
        public float lifestealAmountTier1 = 2f;
        public float lifestealAmountTier2 = 4f;

        public float postKillHealAmountTier2 = 10f;
        public int postKillHealDurationTicksTier2 = 180;
        public int postKillHealIntervalTicks = 30;

        public int tier2Threshold = 15;

        public HediffCompProperties_VampiricThornEffect()
        {
            compClass = typeof(HediffComp_VampiricThornEffect);
        }
    }

    // Vampiric Thorn's own effect: Tier 1 instantly heals a small amount of
    // the wearer's own worst wound on every melee hit they land, upgrading
    // in place to a stronger Tier 2 heal plus a brief heal-over-time window
    // triggered by a killing blow, once enough hits have landed (PRD §5.4/§6).
    public class HediffComp_VampiricThornEffect : HediffComp, IOnMeleeHitLandedTattooEffect, IProvidesTattooTierProgress
    {
        public TattooTierProgress tierState = new TattooTierProgress();

        // Tier 2-exclusive post-kill recovery window (FR-005/FR-006) — a
        // fresh kill overwrites these outright rather than stacking with an
        // already-running window, the same refresh-not-stack shape
        // Bloodrune's/Guardian's Call's/Stormlash's own time-boxed state
        // already uses.
        public int postKillWindowEndTick;
        public float postKillHealRemaining;
        public int nextPostKillHealTick;

        public HediffCompProperties_VampiricThornEffect Props => (HediffCompProperties_VampiricThornEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        public void OnMeleeHitLanded(Thing target, DamageInfo dinfo, float damageDealt, bool killedTarget)
        {
            // Edge case: the wearer died in the same resolution as their own
            // attack (e.g. a mutual killing blow) — nothing further applies.
            if (Pawn == null || Pawn.Dead)
                return;

            bool atTier2 = tierState.tier >= 2;

            float lifestealAmount = atTier2
                ? TattooEffectValues.Get(ScopeKey, "LifestealAmountTier2", Props.lifestealAmountTier2)
                : TattooEffectValues.Get(ScopeKey, "LifestealAmountTier1", Props.lifestealAmountTier1);

            TattooHealingUtility.HealWorstInjury(Pawn, lifestealAmount);

            // FR-002/FR-003: every landed hit counts, no proc-chance gate.
            // Registering the event before reading tierState.tier below
            // means a hit that both crosses the Tier 2 threshold and kills
            // its target sees the already-updated tier (research.md R5 note)
            // — intentional, not incidental.
            tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

            // FR-005: Tier 2 only. FR-006: a second kill while a window is
            // still running overwrites it outright rather than stacking.
            if (tierState.tier >= 2 && killedTarget)
            {
                int now = Find.TickManager.TicksGame;
                int duration = (int)TattooEffectValues.Get(ScopeKey, "PostKillHealDurationTicksTier2", Props.postKillHealDurationTicksTier2);

                postKillWindowEndTick = now + duration;
                postKillHealRemaining = TattooEffectValues.Get(ScopeKey, "PostKillHealAmountTier2", Props.postKillHealAmountTier2);
                nextPostKillHealTick = now;
            }

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] Vampiric Thorn lifesteal on {Pawn?.LabelShort}: tier={tierState.tier}, " +
                    $"progress={tierState.progressionCounter}, lifestealAmount={lifestealAmount}, killedTarget={killedTarget}.");
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            int now = Find.TickManager.TicksGame;
            if (now >= postKillWindowEndTick)
                return;

            if (now < nextPostKillHealTick || postKillHealRemaining <= 0f)
                return;

            // Mirrors HediffComp_BloodruneEffect.HealTick's refactored shape
            // (research.md R5): each interval offers the whole remaining
            // budget to TattooHealingUtility, which caps itself against the
            // current worst wound and rolls over within the call if needed.
            postKillHealRemaining -= TattooHealingUtility.HealWorstInjury(Pawn, postKillHealRemaining);
            nextPostKillHealTick = now + Props.postKillHealIntervalTicks;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
            Scribe_Values.Look(ref postKillWindowEndTick, "postKillWindowEndTick", 0);
            Scribe_Values.Look(ref postKillHealRemaining, "postKillHealRemaining", 0f);
            Scribe_Values.Look(ref nextPostKillHealTick, "nextPostKillHealTick", 0);
        }
    }
}
