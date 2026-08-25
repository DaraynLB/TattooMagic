using RimWorld;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_SerpentsEyeEffect : HediffCompProperties
    {
        public float accuracyBonusTier1 = 0.05f;
        public float accuracyBonusTier2 = 0.1f;

        public float swayRecoilBonusTier1 = 0.05f;
        public float swayRecoilBonusTier2 = 0.1f;

        public float ceAmmoEffectivenessMultiplierTier2 = 1.15f;

        public int tier2Threshold = 15;

        public HediffCompProperties_SerpentsEyeEffect()
        {
            compClass = typeof(HediffComp_SerpentsEyeEffect);
        }
    }

    // Serpent's Eye's own effect: Tier 1 passive ranged-accuracy bonus, plus
    // (under Combat Extended) a weapon sway/recoil reduction, upgrading in
    // place to stronger Tier 2 values plus a CE-only ammo-effectiveness bonus
    // once enough of the wearer's own ranged shots have landed (PRD §5.4/§6).
    //
    // GetStatOffset branches on Combat Extended's presence because CE itself
    // relabels StatDefOf.ShootingAccuracyPawn from "shooting accuracy" into
    // "weapon handling" (sway/recoil), moving real accuracy onto CE's own
    // AimingAccuracy StatDef instead (research.md R4) — offsetting
    // ShootingAccuracyPawn for "accuracy" unconditionally would silently
    // mislabel a sway/recoil bonus as accuracy under CE.
    public class HediffComp_SerpentsEyeEffect : HediffComp, IProvidesTattooStatOffset, IOnRangedHitLandedTattooEffect, IProvidesTattooTierProgress
    {
        public TattooTierProgress tierState = new TattooTierProgress();

        public HediffCompProperties_SerpentsEyeEffect Props => (HediffCompProperties_SerpentsEyeEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        public float GetStatOffset(StatDef stat)
        {
            bool atTier2 = tierState.tier >= 2;

            if (stat == StatDefOf.ShootingAccuracyPawn)
            {
                return CombatExtendedInterop.IsLoaded
                    ? SwayRecoilBonus(atTier2)
                    : AccuracyBonus(atTier2);
            }

            if (CombatExtendedInterop.IsLoaded && stat == CombatExtendedInterop.AimingAccuracy)
                return AccuracyBonus(atTier2);

            return 0f;
        }

        private float AccuracyBonus(bool atTier2)
        {
            return atTier2
                ? TattooEffectValues.Get(ScopeKey, "AccuracyBonusTier2", Props.accuracyBonusTier2)
                : TattooEffectValues.Get(ScopeKey, "AccuracyBonusTier1", Props.accuracyBonusTier1);
        }

        private float SwayRecoilBonus(bool atTier2)
        {
            return atTier2
                ? TattooEffectValues.Get(ScopeKey, "SwayRecoilBonusTier2", Props.swayRecoilBonusTier2)
                : TattooEffectValues.Get(ScopeKey, "SwayRecoilBonusTier1", Props.swayRecoilBonusTier1);
        }

        public void OnRangedHitLanded(Thing target, DamageInfo dinfo, float damageDealt)
        {
            // FR-003/FR-004: every landed shot counts, at either tier — no
            // proc-chance gate, unlike Frost Sigil's on-hit slow.
            tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
        }
    }
}
