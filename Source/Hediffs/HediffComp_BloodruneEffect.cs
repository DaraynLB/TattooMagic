using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_BloodruneEffect : HediffCompProperties
    {
        public float totalHealAmountTier1 = 12f;
        public float totalHealAmountTier2 = 24f;

        public int burstDurationTicksTier1 = 180;
        public int burstDurationTicksTier2 = 300;

        public int healIntervalTicks = 30;

        public int cooldownDurationTicksTier1 = 1800;
        public int cooldownDurationTicksTier2 = 1800;

        public float painOffsetTier2 = 0.15f;

        public int tier2Threshold = 15;

        public string abilityIconPath = "UI/Commands/Bloodrune";

        public HediffCompProperties_BloodruneEffect()
        {
            compClass = typeof(HediffComp_BloodruneEffect);
        }
    }

    // Bloodrune's own effect: the mod's third triggered ability — an
    // activatable heal-over-time burst (gizmo + cooldown), the third
    // consumer of feature 004's IProvidesTattooGizmo reuse point, built
    // after feature 006's tattoo-mastery track already existed (picked up
    // automatically, zero extra code, research.md R1). Upgrades in place to
    // a Tier 2 that heals more/longer and also reduces pain for the burst's
    // duration, once enough activations have registered (PRD §5.4/§6).
    public class HediffComp_BloodruneEffect : HediffComp, IProvidesTattooGizmo, IProvidesTattooTierProgress
    {
        public TattooTierProgress tierState = new TattooTierProgress();

        public int cooldownEndTick;
        public int burstEndTick;
        public int nextHealTick;
        public float healRemaining;

        public HediffCompProperties_BloodruneEffect Props => (HediffCompProperties_BloodruneEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        public Gizmo GetGizmo()
        {
            bool onCooldown = Find.TickManager.TicksGame < cooldownEndTick;

            return new Command_Action
            {
                defaultLabel = "Bloodrune",
                defaultDesc = "Open a burst of self-healing over a few seconds, then go on cooldown.",
                icon = ContentFinder<Texture2D>.Get(Props.abilityIconPath, false),
                action = TryActivate,
                Disabled = onCooldown,
                disabledReason = onCooldown
                    ? $"Bloodrune is recharging: {RemainingCooldownSeconds()}s remaining."
                    : null,
            };
        }

        private int RemainingCooldownSeconds()
        {
            return Mathf.CeilToInt((cooldownEndTick - Find.TickManager.TicksGame) / 60f);
        }

        // No-ops if still on cooldown (defensive; the gizmo itself already
        // prevents this per Edge Cases). Reads whichever tier's values are
        // current at the moment of activation, so an activation made just
        // before a Tier 1->2 upgrade uses Tier 1's numbers and the next
        // activation uses Tier 2's, with no retroactive change to an
        // already-running burst.
        public void TryActivate()
        {
            int now = Find.TickManager.TicksGame;
            if (now < cooldownEndTick)
                return;

            bool atTier2 = tierState.tier >= 2;

            float totalHeal = atTier2
                ? TattooEffectValues.Get(ScopeKey, "TotalHealAmountTier2", Props.totalHealAmountTier2)
                : TattooEffectValues.Get(ScopeKey, "TotalHealAmountTier1", Props.totalHealAmountTier1);
            int duration = (int)(atTier2
                ? TattooEffectValues.Get(ScopeKey, "BurstDurationTicksTier2", Props.burstDurationTicksTier2)
                : TattooEffectValues.Get(ScopeKey, "BurstDurationTicksTier1", Props.burstDurationTicksTier1));
            int cooldown = (int)(atTier2
                ? TattooEffectValues.Get(ScopeKey, "CooldownDurationTicksTier2", Props.cooldownDurationTicksTier2)
                : TattooEffectValues.Get(ScopeKey, "CooldownDurationTicksTier1", Props.cooldownDurationTicksTier1));

            cooldownEndTick = now + cooldown;
            burstEndTick = now + duration;
            healRemaining = totalHeal;
            nextHealTick = now;

            // FR-003/FR-004: one qualifying event per activation.
            tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic] Bloodrune activated by {Pawn?.LabelShort}: tier={tierState.tier}, " +
                    $"totalHeal={totalHeal}, duration={duration}, cooldown={cooldown}, progress={tierState.progressionCounter}.");
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            int now = Find.TickManager.TicksGame;
            if (now >= burstEndTick)
                return;

            if (now < nextHealTick || healRemaining <= 0f)
                return;

            HealTick();
            nextHealTick = now + Props.healIntervalTicks;
        }

        // Heals the wearer's currently most-severe open injury first,
        // moving to the next-most-severe once one is fully closed
        // (research.md R2) — a small, intuitive default (close the worst
        // wound first) rather than spreading a thin heal across every
        // injury at once. No-ops (not an error) if there is nothing open to
        // heal (User Story 1, Acceptance Scenario 5). Delegates to the
        // shared TattooHealingUtility (feature 010 research.md R5), which
        // now also backs Vampiric Thorn's own per-hit lifesteal and Tier 2
        // post-kill recovery window.
        private void HealTick()
        {
            healRemaining -= TattooHealingUtility.HealWorstInjury(Pawn, healRemaining);
        }

        // FR-006: only contributes a pain offset while at Tier 2 AND this
        // activation's burst window is still open — 0f at Tier 1, and 0f at
        // Tier 2 between activations. Read by Hediff_BloodruneEffect.
        // PainOffset (research.md R3, contracts/pain-offset-contract.md §2).
        public float CurrentPainOffset
        {
            get
            {
                if (tierState.tier < 2 || Find.TickManager.TicksGame >= burstEndTick)
                    return 0f;

                return -TattooEffectValues.Get(ScopeKey, "PainOffsetTier2", Props.painOffsetTier2);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
            Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick", 0);
            Scribe_Values.Look(ref burstEndTick, "burstEndTick", 0);
            Scribe_Values.Look(ref nextHealTick, "nextHealTick", 0);
            Scribe_Values.Look(ref healRemaining, "healRemaining", 0f);
        }
    }

    // The hediffClass for TattooMagic_Hediff_Bloodrune. The only reason
    // this subclass exists is to override PainOffset — RimWorld computes a
    // pawn's total pain directly from each held Hediff's own PainOffset,
    // not through the StatDef/StatPart system IProvidesTattooStatOffset
    // targets, so a tattoo that wants to affect pain has to do it here
    // rather than through that existing mechanism (research.md R3,
    // contracts/pain-offset-contract.md). Every other behavior (gizmo,
    // cooldown, tier state, healing) is unchanged comp behavior, identical
    // to how Guardian's Call's and Stormlash's plain HediffWithComps
    // Hediffs work.
    public class Hediff_BloodruneEffect : HediffWithComps
    {
        public override float PainOffset => this.TryGetComp<HediffComp_BloodruneEffect>()?.CurrentPainOffset ?? 0f;
    }
}
