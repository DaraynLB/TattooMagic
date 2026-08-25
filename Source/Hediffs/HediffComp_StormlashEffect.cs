using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_StormlashEffect : HediffCompProperties
    {
        public float moveSpeedOffsetTier1 = 1.5f;
        public float moveSpeedOffsetTier2 = 2.5f;

        public float attackSpeedBonusTier1 = 0.15f;
        public float attackSpeedBonusTier2 = 0.3f;

        public int boostDurationTicksTier1 = 300;
        public int boostDurationTicksTier2 = 300;

        public int cooldownDurationTicksTier1 = 1800;
        public int cooldownDurationTicksTier2 = 1800;

        public int tier2Threshold = 15;

        public string abilityIconPath = "UI/Commands/Stormlash";

        public HediffCompProperties_StormlashEffect()
        {
            compClass = typeof(HediffComp_StormlashEffect);
        }
    }

    // Stormlash's own effect: the mod's second triggered ability — an
    // activatable move-speed/attack-speed boost (gizmo + cooldown), the
    // second consumer of feature 004's IProvidesTattooGizmo reuse point.
    // Upgrades in place to a Tier 2 that grants a bigger boost plus brief
    // immunity to movement-slow effects for the boost's duration, once
    // enough activations have registered (PRD §5.4/§6).
    public class HediffComp_StormlashEffect : HediffComp, IProvidesTattooGizmo, IProvidesTattooStatOffset, IGrantsStatOffsetImmunity, IProvidesTattooTierProgress
    {
        public TattooTierProgress tierState = new TattooTierProgress();

        public int cooldownEndTick;
        public int boostEndTick;

        public HediffCompProperties_StormlashEffect Props => (HediffCompProperties_StormlashEffect)props;

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
                defaultLabel = "Stormlash",
                defaultDesc = "Grant yourself a temporary move speed and attack speed boost, then go on cooldown.",
                icon = ContentFinder<Texture2D>.Get(Props.abilityIconPath, false),
                action = TryActivate,
                Disabled = onCooldown,
                disabledReason = onCooldown
                    ? $"Stormlash is recharging: {RemainingCooldownSeconds()}s remaining."
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
        // already-running boost.
        public void TryActivate()
        {
            int now = Find.TickManager.TicksGame;
            if (now < cooldownEndTick)
                return;

            bool atTier2 = tierState.tier >= 2;

            int duration = (int)(atTier2
                ? TattooEffectValues.Get(ScopeKey, "BoostDurationTicksTier2", Props.boostDurationTicksTier2)
                : TattooEffectValues.Get(ScopeKey, "BoostDurationTicksTier1", Props.boostDurationTicksTier1));
            int cooldown = (int)(atTier2
                ? TattooEffectValues.Get(ScopeKey, "CooldownDurationTicksTier2", Props.cooldownDurationTicksTier2)
                : TattooEffectValues.Get(ScopeKey, "CooldownDurationTicksTier1", Props.cooldownDurationTicksTier1));

            cooldownEndTick = now + cooldown;
            boostEndTick = now + duration;

            // FR-004/FR-005: one qualifying event per activation.
            tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic] Stormlash activated by {Pawn?.LabelShort}: tier={tierState.tier}, " +
                    $"duration={duration}, cooldown={cooldown}, progress={tierState.progressionCounter}.");
            }
        }

        // FR-001/FR-006/FR-008: only contributes an offset while this
        // activation's boost window is still open — 0f otherwise.
        public float GetStatOffset(StatDef stat)
        {
            if (Find.TickManager.TicksGame >= boostEndTick)
                return 0f;

            bool atTier2 = tierState.tier >= 2;

            if (stat == StatDefOf.MoveSpeed)
            {
                return atTier2
                    ? TattooEffectValues.Get(ScopeKey, "MoveSpeedOffsetTier2", Props.moveSpeedOffsetTier2)
                    : TattooEffectValues.Get(ScopeKey, "MoveSpeedOffsetTier1", Props.moveSpeedOffsetTier1);
            }

            if (stat == StatDefOf.MeleeCooldownFactor || stat == StatDefOf.RangedCooldownFactor)
            {
                float bonus = atTier2
                    ? TattooEffectValues.Get(ScopeKey, "AttackSpeedBonusTier2", Props.attackSpeedBonusTier2)
                    : TattooEffectValues.Get(ScopeKey, "AttackSpeedBonusTier1", Props.attackSpeedBonusTier1);
                return -bonus;
            }

            return 0f;
        }

        // FR-007: Tier 2 only, and only while this activation's boost
        // window is still open — vetoes any other comp's negative
        // contribution to MoveSpeed (e.g. Frost Sigil's slow), routed
        // through StatPart_TattooEffectOffset (contracts/stat-offset-
        // immunity-contract.md §2). Does not affect any other stat, and
        // does not by itself neutralize Combat Extended's suppression-slow
        // (which never reaches this contract — see
        // Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity).
        public bool BlocksNegativeOffsets(StatDef stat)
        {
            return stat == StatDefOf.MoveSpeed
                && tierState.tier >= 2
                && Find.TickManager.TicksGame < boostEndTick;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
            Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick", 0);
            Scribe_Values.Look(ref boostEndTick, "boostEndTick", 0);
        }
    }
}
