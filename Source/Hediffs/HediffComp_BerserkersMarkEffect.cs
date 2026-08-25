using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_BerserkersMarkEffect : HediffCompProperties
    {
        public float meleeDamageOffsetTier1 = 0.20f;
        public float meleeDamageOffsetTier2 = 0.40f;

        public float painThresholdOffsetTier1 = 0.15f;
        public float painThresholdOffsetTier2 = 0.15f;

        // Positive user-facing magnitude; applied as a negative offset to the
        // armor-rating triplet in GetStatOffset (research.md R3).
        public float defenseOffsetTier1 = 0.15f;
        public float defenseOffsetTier2 = 0.08f;

        public int boostDurationTicksTier1 = 180;
        public int boostDurationTicksTier2 = 180;

        public int cooldownDurationTicksTier1 = 1800;
        public int cooldownDurationTicksTier2 = 1800;

        public int tier2Threshold = 15;

        public string abilityIconPath = "UI/Commands/BerserkersMark";

        public HediffCompProperties_BerserkersMarkEffect()
        {
            compClass = typeof(HediffComp_BerserkersMarkEffect);
        }
    }

    // Berserker's Mark's own effect: the mod's fifth and final triggered
    // ability — an activatable risk/reward window (gizmo + cooldown) that
    // simultaneously boosts melee damage and pain threshold while reducing
    // defense, all three gated on tier + an active window check, exactly
    // mirroring HediffComp_GuardiansCallEffect.GetStatOffset's shape
    // (research.md R1). Implements IProvidesTattooGizmo (reused, feature 004)
    // and IProvidesTattooStatOffset (reused, features 002-004).
    public class HediffComp_BerserkersMarkEffect : HediffComp, IProvidesTattooGizmo, IProvidesTattooStatOffset, IProvidesTattooTierProgress
    {
        public TattooTierProgress tierState = new TattooTierProgress();

        public int cooldownEndTick;
        public int boostEndTick;

        public HediffCompProperties_BerserkersMarkEffect Props => (HediffCompProperties_BerserkersMarkEffect)props;

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
                defaultLabel = "Berserker's Mark",
                defaultDesc = "Trade defense for a temporary boost to melee damage and pain threshold, then go on cooldown.",
                icon = ContentFinder<Texture2D>.Get(Props.abilityIconPath, false),
                action = TryActivate,
                Disabled = onCooldown,
                disabledReason = onCooldown
                    ? $"Berserker's Mark is recharging: {RemainingCooldownSeconds()}s remaining."
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
        // (post-upgrade) activation uses Tier 2's, with no retroactive
        // change to an already-running window (data-model.md State
        // Transitions). A second activation before a prior window closes
        // overwrites boostEndTick outright (refresh, not stack), same as
        // every prior triggered tattoo.
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

            tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic] Berserker's Mark activated by {Pawn?.LabelShort}: tier={tierState.tier}, " +
                    $"duration={duration}, cooldown={cooldown}, progress={tierState.progressionCounter}.");
            }
        }

        // FR-006/FR-007/FR-009: 0f immediately once the window has closed;
        // otherwise the tier-appropriate offset for each of the three stats
        // this tattoo touches (armor rating negated, since Props.defenseOffset*
        // is a positive user-facing magnitude), and 0f for any other StatDef.
        public float GetStatOffset(StatDef stat)
        {
            if (Find.TickManager.TicksGame >= boostEndTick)
                return 0f;

            bool atTier2 = tierState.tier >= 2;

            if (stat == StatDefOf.MeleeDamageFactor)
            {
                return atTier2
                    ? TattooEffectValues.Get(ScopeKey, "MeleeDamageOffsetTier2", Props.meleeDamageOffsetTier2)
                    : TattooEffectValues.Get(ScopeKey, "MeleeDamageOffsetTier1", Props.meleeDamageOffsetTier1);
            }

            if (stat == StatDefOf.PainShockThreshold)
            {
                return atTier2
                    ? TattooEffectValues.Get(ScopeKey, "PainThresholdOffsetTier2", Props.painThresholdOffsetTier2)
                    : TattooEffectValues.Get(ScopeKey, "PainThresholdOffsetTier1", Props.painThresholdOffsetTier1);
            }

            if (stat == StatDefOf.ArmorRating_Sharp || stat == StatDefOf.ArmorRating_Blunt || stat == StatDefOf.ArmorRating_Heat)
            {
                float defenseOffset = atTier2
                    ? TattooEffectValues.Get(ScopeKey, "DefenseOffsetTier2", Props.defenseOffsetTier2)
                    : TattooEffectValues.Get(ScopeKey, "DefenseOffsetTier1", Props.defenseOffsetTier1);
                return -defenseOffset;
            }

            return 0f;
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
