using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_IronskinGlyphEffect : HediffCompProperties
    {
        public float armorRatingBonusTier1 = 0.08f;
        public float armorRatingBonusTier2 = 0.16f;

        public float fullNegateChanceTier2 = 0.15f;

        public int tier2Threshold = 10;

        public HediffCompProperties_IronskinGlyphEffect()
        {
            compClass = typeof(HediffComp_IronskinGlyphEffect);
        }
    }

    // Ironskin Glyph's own effect: Tier 1 passive armor-rating bonus, applied
    // identically to both ArmorRating_Sharp and ArmorRating_Blunt (research.md
    // R5 — a single generalist toughness value, not two independently-tunable
    // knobs), upgrading in place to stronger values plus a chance to fully
    // negate a physical hit once enough qualifying instances have occurred
    // (PRD §5.4/§6). The physical-damage reduction itself is implemented
    // twice, deliberately: an ArmorRating_Sharp/Blunt stat offset for vanilla
    // RimWorld's own armor pipeline, which correctly consults it, PLUS a
    // direct dinfo.Amount reduction in TryAbsorbIncomingDamage when Combat
    // Extended is loaded, because CE's own armor formula (both its ambient
    // and its non-ambient/direct-hit sub-paths, research.md R3) only reads a
    // pawn's own armor-category stat when the hit body part belongs to its
    // CoveredByNaturalArmor group — never true for an ordinary human — so the
    // stat offset alone silently no-ops under CE for this tattoo's core claim
    // (research.md R3, independently confirmed by decompiling CE's direct-hit
    // path rather than assumed from feature 011's ambient-path finding). Both
    // paths are driven by the exact same armorRatingBonusTier1/Tier2 XML
    // values, just applied through different mechanisms depending on which
    // engine is running.
    public class HediffComp_IronskinGlyphEffect : HediffComp, IProvidesTattooStatOffset, IOnIncomingDamageTattooEffect, IProvidesTattooTierProgress
    {
        public TattooTierProgress tierState = new TattooTierProgress();

        public HediffCompProperties_IronskinGlyphEffect Props => (HediffCompProperties_IronskinGlyphEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        public float GetStatOffset(StatDef stat)
        {
            if (stat != StatDefOf.ArmorRating_Sharp && stat != StatDefOf.ArmorRating_Blunt)
                return 0f;

            bool atTier2 = tierState.tier >= 2;

            return atTier2
                ? TattooEffectValues.Get(ScopeKey, "ArmorRatingBonusTier2", Props.armorRatingBonusTier2)
                : TattooEffectValues.Get(ScopeKey, "ArmorRatingBonusTier1", Props.armorRatingBonusTier1);
        }

        // FR-009/FR-010: implementer-side filtering, per the
        // IOnIncomingDamageTattooEffect contract — the dispatcher calls this
        // for every incoming DamageInfo regardless of type. Filters by which
        // armor-category stat the DamageDef resolves to (research.md R2),
        // not an enumerated DamageDef list, since "physical" spans an
        // open-ended set of Sharp/Blunt DamageDefs.
        public bool TryAbsorbIncomingDamage(ref DamageInfo dinfo)
        {
            StatDef armorStat = dinfo.Def?.armorCategory?.armorRatingStat;

            if (armorStat != StatDefOf.ArmorRating_Sharp && armorStat != StatDefOf.ArmorRating_Blunt)
                return false;

            if (Pawn == null || Pawn.Dead)
                return false;

            if (dinfo.Amount <= 0f)
                return false;

            // FR-003: every qualifying instance counts, before the tier-gated
            // steps below — an instance that both crosses the threshold and
            // rolls a full negate still counts and still upgrades in the
            // same call (research.md R4, mirrors Ember Ward's ordering
            // rationale, feature 011). This MUST run before reading
            // tierState.tier below, so a tier-up-triggering hit gets Tier
            // 2's stronger reduction/roll in this same call, not Tier 1's.
            int thresholdBefore = (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);
            bool tieredUp = tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] Ironskin Glyph progression on {Pawn?.LabelShort}: tier={tierState.tier}, " +
                    $"progress={tierState.progressionCounter}/{thresholdBefore}{(tieredUp ? " — TIER UP!" : "")}.");
            }

            bool atTier2 = tierState.tier >= 2;

            // research.md R3: Combat Extended's own armor formula
            // (ArmorUtilityCE.GetAfterArmorDamage, both its ambient and its
            // non-ambient/direct-hit sub-paths) only reads a pawn's own
            // armor-category stat when the hit body part is in the
            // CoveredByNaturalArmor group — never true for an ordinary
            // human — so the GetStatOffset contribution above silently
            // no-ops under CE for this tattoo's core "reduce physical
            // damage" claim. This runs entirely in our own prefix, strictly
            // before any CE armor processing, so it's correct regardless of
            // which internal CE code path this particular hit would
            // otherwise take. Vanilla RimWorld is unaffected — it isn't
            // gated this way — so this only needs to run under CE.
            if (CombatExtendedInterop.IsLoaded)
            {
                float reduction = atTier2
                    ? TattooEffectValues.Get(ScopeKey, "ArmorRatingBonusTier2", Props.armorRatingBonusTier2)
                    : TattooEffectValues.Get(ScopeKey, "ArmorRatingBonusTier1", Props.armorRatingBonusTier1);
                float reducedAmount = Mathf.Max(0f, dinfo.Amount * (1f - reduction));
                dinfo.SetAmount(reducedAmount);

                if (TattooMagicSettings.EnableDebugLogging)
                {
                    Log.Message($"[TattooMagic DEBUG] Ironskin Glyph CE direct reduction on {Pawn?.LabelShort}: " +
                        $"reduction={reduction:P0}, amount reduced to {reducedAmount:F1}.");
                }
            }

            // FR-006: Tier 1 never rolls or grants a full negate.
            if (!atTier2)
                return false;

            float fullNegateChance = TattooEffectValues.Get(ScopeKey, "FullNegateChanceTier2", Props.fullNegateChanceTier2);
            bool proc = Rand.Chance(fullNegateChance);

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] Ironskin Glyph full-negate roll on {Pawn?.LabelShort}: " +
                    $"chance={fullNegateChance:P0}, proc={proc}.");
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
