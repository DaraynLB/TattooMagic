using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_EmberWardEffect : HediffCompProperties
    {
        public float heatResistanceTier1 = 5f;
        public float heatResistanceTier2 = 10f;

        public float armorRatingHeatTier1 = 0.08f;
        public float armorRatingHeatTier2 = 0.16f;

        public float fullIgnoreChanceTier2 = 0.15f;

        public int tier2Threshold = 10;

        public HediffCompProperties_EmberWardEffect()
        {
            compClass = typeof(HediffComp_EmberWardEffect);
        }
    }

    // Ember Ward's own effect: Tier 1 passive heat resistance (a
    // ComfyTemperatureMax stat offset, research.md R2) and burn-damage
    // reduction, upgrading in place to stronger values plus a chance to
    // fully ignore a fire/burn instance once enough qualifying instances
    // have occurred (PRD §5.4/§6). The burn-damage reduction itself is
    // implemented twice, deliberately: an ArmorRating_Heat stat offset
    // (research.md R2-R3) for vanilla RimWorld's own armor pipeline, which
    // correctly consults it, PLUS a direct dinfo.Amount reduction in
    // TryAbsorbIncomingDamage when Combat Extended is loaded, because CE's
    // own ambient-damage armor formula only reads a pawn's own
    // ArmorRating_Heat when the hit body part belongs to its
    // CoveredByNaturalArmor group — never true for an ordinary human — so
    // the stat offset alone silently no-ops under CE for this tattoo's core
    // claim (research.md R10, found and root-caused via live testing, not
    // assumed). Both paths are driven by the exact same
    // armorRatingHeatTier1/Tier2 XML values, just applied through different
    // mechanisms depending on which engine is running.
    public class HediffComp_EmberWardEffect : HediffComp, IProvidesTattooStatOffset, IOnIncomingDamageTattooEffect, IProvidesTattooTierProgress
    {
        public TattooTierProgress tierState = new TattooTierProgress();

        public HediffCompProperties_EmberWardEffect Props => (HediffCompProperties_EmberWardEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        public float GetStatOffset(StatDef stat)
        {
            bool atTier2 = tierState.tier >= 2;

            if (stat == StatDefOf.ComfyTemperatureMax)
            {
                return atTier2
                    ? TattooEffectValues.Get(ScopeKey, "HeatResistanceTier2", Props.heatResistanceTier2)
                    : TattooEffectValues.Get(ScopeKey, "HeatResistanceTier1", Props.heatResistanceTier1);
            }

            if (stat == StatDefOf.ArmorRating_Heat)
            {
                return atTier2
                    ? TattooEffectValues.Get(ScopeKey, "ArmorRatingHeatTier2", Props.armorRatingHeatTier2)
                    : TattooEffectValues.Get(ScopeKey, "ArmorRatingHeatTier1", Props.armorRatingHeatTier1);
            }

            return 0f;
        }

        // FR-009/FR-010: implementer-side filtering, per the
        // IOnIncomingDamageTattooEffect contract — the dispatcher calls this
        // for every incoming DamageInfo regardless of type.
        public bool TryAbsorbIncomingDamage(ref DamageInfo dinfo)
        {
            if (dinfo.Def != DamageDefOf.Flame && dinfo.Def != DamageDefOf.Burn)
                return false;

            if (Pawn == null || Pawn.Dead)
                return false;

            if (dinfo.Amount <= 0f)
                return false;

            // FR-003: every qualifying instance counts, before the tier-gated
            // steps below — an instance that both crosses the threshold and
            // rolls a full ignore still counts and still upgrades in the
            // same call (research.md R6, mirrors Vampiric Thorn's ordering
            // rationale, feature 010). This MUST run before reading
            // tierState.tier below, so a tier-up-triggering hit gets Tier
            // 2's stronger reduction/roll in this same call, not Tier 1's.
            int thresholdBefore = (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);
            bool tieredUp = tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] Ember Ward progression on {Pawn?.LabelShort}: tier={tierState.tier}, " +
                    $"progress={tierState.progressionCounter}/{thresholdBefore}{(tieredUp ? " — TIER UP!" : "")}.");
            }

            bool atTier2 = tierState.tier >= 2;

            // research.md R10: Combat Extended's own ambient-damage armor
            // formula (ArmorUtilityCE.GetAmbientPostArmorDamage) only reads
            // a pawn's own ArmorRating_Heat when the hit body part is in the
            // CoveredByNaturalArmor group — never true for an ordinary
            // human — so the GetStatOffset contribution above silently
            // no-ops under CE for this tattoo's core "reduce burn damage"
            // claim. This runs entirely in our own prefix, strictly before
            // any CE armor processing, so it's correct regardless of which
            // internal CE code path this particular hit would otherwise
            // take. Vanilla RimWorld is unaffected — it isn't gated this
            // way (research.md R3) — so this only needs to run under CE.
            if (CombatExtendedInterop.IsLoaded)
            {
                float reduction = atTier2
                    ? TattooEffectValues.Get(ScopeKey, "ArmorRatingHeatTier2", Props.armorRatingHeatTier2)
                    : TattooEffectValues.Get(ScopeKey, "ArmorRatingHeatTier1", Props.armorRatingHeatTier1);
                float reducedAmount = Mathf.Max(0f, dinfo.Amount * (1f - reduction));
                dinfo.SetAmount(reducedAmount);

                if (TattooMagicSettings.EnableDebugLogging)
                {
                    Log.Message($"[TattooMagic DEBUG] Ember Ward CE direct reduction on {Pawn?.LabelShort}: " +
                        $"reduction={reduction:P0}, amount reduced to {reducedAmount:F1}.");
                }
            }

            // FR-006: Tier 1 never rolls or grants a full ignore.
            if (!atTier2)
                return false;

            float fullIgnoreChance = TattooEffectValues.Get(ScopeKey, "FullIgnoreChanceTier2", Props.fullIgnoreChanceTier2);
            bool proc = Rand.Chance(fullIgnoreChance);

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] Ember Ward full-ignore roll on {Pawn?.LabelShort}: " +
                    $"chance={fullIgnoreChance:P0}, proc={proc}.");
            }

            return proc;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
        }
    }
}
