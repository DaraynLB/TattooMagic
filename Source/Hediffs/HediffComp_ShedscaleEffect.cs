using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_ShedscaleEffect : HediffCompProperties
    {
        public float tier1RegrowthDays = 7f;
        public float tier2RegrowthDays = 4f;

        public float tier1EfficiencyFactor = 0.80f;

        public int tier2DaysWornThreshold = 30;
        public int tier2PartsRegrownThreshold = 3;

        public List<BodyPartDef> tier1EligiblePartDefs = new List<BodyPartDef>();
        public List<BodyPartDef> tier2AdditionalEligiblePartDefs = new List<BodyPartDef>();

        public HediffCompProperties_ShedscaleEffect()
        {
            compClass = typeof(HediffComp_ShedscaleEffect);
        }
    }

    // Shedscale's own effect: fully automatic, polling-based regrowth of missing eligible body parts
    // (research.md R1/R2/R3/R7 — no Harmony patch anywhere in this feature), a stacking hunger/pain cost while
    // any regrowth is active (research.md R4), and a permanent Tier 1 dual-condition -> Tier 2 upgrade
    // (research.md R11). Implements IProvidesTattooTierProgress directly (not via the shared, composed
    // TattooTierProgress) because Tier 2 races two independently-paced counters rather than one monotonic
    // counter against one threshold.
    public class HediffComp_ShedscaleEffect : HediffComp, IProvidesTattooTierProgress
    {
        public int tier = 1;

        public int daysWornCounter;
        public int partsRegrownCounter;

        public int nextDailyCheckTick;

        // Key = a currently-missing, currently-tracked body part; value = elapsed whole days counted toward
        // that part's tier duration so far. An entry only ever leaves this dictionary via CompleteRegrowth, or
        // by this comp itself ceasing to exist when the tattoo is removed (research.md R1/R10).
        public Dictionary<BodyPartRecord, int> activeRegrowths = new Dictionary<BodyPartRecord, int>();

        public HediffCompProperties_ShedscaleEffect Props => (HediffCompProperties_ShedscaleEffect)props;

        public string ScopeKey => parent.def.defName;

        public int Tier => tier;

        public int ActiveRegrowthCount => activeRegrowths.Count;

        public int ProgressionCounter => LeadingProgress().counter;

        public int? NextTierThreshold => tier >= 2 ? (int?)null : LeadingProgress().threshold;

        // research.md R11: Tier 2 races two independently-paced counters (a day count and a part count), so
        // there is no single monotonic counter/threshold pair the way every other tattoo has. Report whichever
        // race is currently leading (closer, proportionally, to its own threshold) so the shared tier-progress
        // UI shows an honest "how close to Tier 2" readout, recomputed fresh on every read rather than cached.
        private (int counter, int threshold) LeadingProgress()
        {
            int daysThreshold = (int)TattooEffectValues.Get(ScopeKey, "Tier2DaysWornThreshold", Props.tier2DaysWornThreshold);
            int partsThreshold = (int)TattooEffectValues.Get(ScopeKey, "Tier2PartsRegrownThreshold", Props.tier2PartsRegrownThreshold);

            float daysRatio = daysThreshold > 0 ? (float)daysWornCounter / daysThreshold : 0f;
            float partsRatio = partsThreshold > 0 ? (float)partsRegrownCounter / partsThreshold : 0f;

            return daysRatio >= partsRatio ? (daysWornCounter, daysThreshold) : (partsRegrownCounter, partsThreshold);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn == null || Pawn.Dead)
                return;

            int now = Find.TickManager.TicksGame;
            if (now < nextDailyCheckTick)
                return;

            nextDailyCheckTick = now + GenDate.TicksPerDay;
            DoDailyPass();
        }

        // FR-001/FR-002/FR-006/FR-014/FR-016: the single once-per-day pass driving everything about this
        // tattoo's own state — no Harmony patch anywhere reacts to surgery/damage events directly (research.md
        // R1/R7); the next daily scan simply sees whatever the pawn's HediffSet looks like by then.
        private void DoDailyPass()
        {
            daysWornCounter++;
            TryUpgradeToTier2();

            ScanForNewlyEligibleParts();

            bool malnourished = Pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition);
            if (!malnourished)
                AdvanceActiveRegrowths();

            UpdateStrainHediff();
        }

        private void ScanForNewlyEligibleParts()
        {
            List<Hediff_MissingPart> missingParts = Pawn.health.hediffSet.GetMissingPartsCommonAncestors();
            for (int i = 0; i < missingParts.Count; i++)
            {
                BodyPartRecord part = missingParts[i].Part;
                if (part == null || activeRegrowths.ContainsKey(part) || !IsEligiblePart(part))
                    continue;

                // Defensive cleanup: a leftover ImperfectRegrowth hediff on this exact part would only exist
                // here if the part was lost again after a prior regrowth cycle completed (spec.md Edge Cases)
                // — remove it before starting the fresh timer rather than leaving a stale entry on a
                // currently-missing part.
                Hediff stale = Pawn.health.hediffSet.hediffs.FirstOrDefault(h =>
                    h.def == TattooMagicDefOf.TattooMagic_Hediff_ShedscaleImperfectRegrowth && h.Part == part);
                if (stale != null)
                    Pawn.health.RemoveHediff(stale);

                activeRegrowths[part] = 0;
            }
        }

        private void AdvanceActiveRegrowths()
        {
            if (activeRegrowths.Count == 0)
                return;

            List<BodyPartRecord> parts = activeRegrowths.Keys.ToList();
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPartRecord part = parts[i];
                int elapsed = activeRegrowths[part] + 1;

                float durationDays = tier >= 2
                    ? TattooEffectValues.Get(ScopeKey, "Tier2RegrowthDays", Props.tier2RegrowthDays)
                    : TattooEffectValues.Get(ScopeKey, "Tier1RegrowthDays", Props.tier1RegrowthDays);

                if (elapsed >= (int)durationDays)
                    CompleteRegrowth(part);
                else
                    activeRegrowths[part] = elapsed;
            }
        }

        // FR-007/FR-012/FR-016 (partsRegrownCounter half): research.md R2/R6.
        private void CompleteRegrowth(BodyPartRecord part)
        {
            Pawn.health.RestorePart(part);

            if (tier < 2)
            {
                float efficiencyFactor = TattooEffectValues.Get(ScopeKey, "Tier1EfficiencyFactor", Props.tier1EfficiencyFactor);
                float maxHealth = part.def.GetMaxHealth(Pawn);

                Hediff imperfect = HediffMaker.MakeHediff(TattooMagicDefOf.TattooMagic_Hediff_ShedscaleImperfectRegrowth, Pawn, part);
                imperfect.Severity = maxHealth * (1f - efficiencyFactor);
                Pawn.health.AddHediff(imperfect);

                HediffComp_GetsPermanent getsPermanent = imperfect.TryGetComp<HediffComp_GetsPermanent>();
                if (getsPermanent != null)
                    getsPermanent.IsPermanent = true;
            }

            partsRegrownCounter++;
            TryUpgradeToTier2();

            activeRegrowths.Remove(part);
        }

        // FR-016/FR-018 (research.md R11): whichever of the two independent conditions is met first flips the
        // tier permanently and retroactively clears every already-regrown part's Tier 1 penalty.
        private void TryUpgradeToTier2()
        {
            if (tier >= 2)
                return;

            int daysThreshold = (int)TattooEffectValues.Get(ScopeKey, "Tier2DaysWornThreshold", Props.tier2DaysWornThreshold);
            int partsThreshold = (int)TattooEffectValues.Get(ScopeKey, "Tier2PartsRegrownThreshold", Props.tier2PartsRegrownThreshold);

            if (daysWornCounter < daysThreshold && partsRegrownCounter < partsThreshold)
                return;

            tier = 2;

            List<Hediff> hediffs = Pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (hediffs[i].def == TattooMagicDefOf.TattooMagic_Hediff_ShedscaleImperfectRegrowth)
                    Pawn.health.RemoveHediff(hediffs[i]);
            }

            if (TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic DEBUG] Shedscale on {Pawn?.LabelShort}: reached Tier 2 (daysWorn={daysWornCounter}, partsRegrown={partsRegrownCounter}) — all Imperfect Regrowth penalties cleared.");
        }

        // research.md R4: hunger rate has no StatDef a StatPart can intercept — HediffSet.GetHungerRateFactor
        // reads HediffStage.hungerRateFactor/hungerRateFactorOffset directly, so a real companion Hediff whose
        // Severity tracks the live regrowth count is the only vanilla-native way to move this Need.
        private void UpdateStrainHediff()
        {
            Hediff strain = Pawn.health.hediffSet.GetFirstHediffOfDef(TattooMagicDefOf.TattooMagic_Hediff_ShedscaleStrain);

            if (activeRegrowths.Count > 0)
            {
                if (strain == null)
                {
                    strain = HediffMaker.MakeHediff(TattooMagicDefOf.TattooMagic_Hediff_ShedscaleStrain, Pawn);
                    Pawn.health.AddHediff(strain);
                }

                strain.Severity = activeRegrowths.Count;
            }
            else if (strain != null)
            {
                Pawn.health.RemoveHediff(strain);
            }
        }

        // research.md R3: an explicit tier-scoped BodyPartDef allow-list, not raw BodyPartDepth — Torso/Neck
        // are Outside-depth same as Arm/Hand, so depth alone doesn't cleanly separate "eligible" from "never."
        //
        // A whole-limb loss/replacement is reported by vanilla at a structural joint above the eligible part
        // itself, not the eligible part directly — confirmed via live testing: installing/removing a full
        // "bionic arm" attaches its Hediff_AddedPart at the Shoulder, and GetMissingPartsCommonAncestors()
        // stops the instant it finds a missing-part hediff on any part, so amputating the Shoulder reports
        // "Shoulder" as missing, never descending to Arm/Hand/Finger underneath. Denylisted parts (Torso,
        // Neck, Spine, Pelvis, the brain, the core part) are excluded outright, without ever looking at their
        // own children — a hypothetical "missing Torso" must never be treated as "regrow the kidneys inside
        // it." Everything else recurses into its subtree, since RestorePart(part) already restores that whole
        // subtree in one call regardless of which level Shedscale started tracking it at.
        public bool IsEligiblePart(BodyPartRecord part)
        {
            if (part == null)
                return false;

            if (part == Pawn.RaceProps.body.corePart
                || part.def == BodyPartDefOf.Torso
                || part.def == BodyPartDefOf.Neck
                || part.def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource)
                || part.def.tags.Contains(BodyPartTagDefOf.Spine)
                || part.def.tags.Contains(BodyPartTagDefOf.Pelvis))
                return false;

            if (Props.tier1EligiblePartDefs.Contains(part.def))
                return true;

            if (tier >= 2 && Props.tier2AdditionalEligiblePartDefs.Contains(part.def))
                return true;

            for (int i = 0; i < part.parts.Count; i++)
            {
                if (IsEligiblePart(part.parts[i]))
                    return true;
            }

            return false;
        }

        // research.md R10: the Shedscale hediff's own removal (via TattooHediffRemovalGuard) discards this
        // comp instance and every field on it for free — activeRegrowths/daysWornCounter/partsRegrownCounter
        // need no explicit cleanup here. The Strain hediff is a *separate* HediffDef on the pawn, though, so it
        // doesn't disappear on its own just because the tattoo did.
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            Hediff strain = Pawn?.health?.hediffSet?.GetFirstHediffOfDef(TattooMagicDefOf.TattooMagic_Hediff_ShedscaleStrain);
            if (strain != null)
                Pawn.health.RemoveHediff(strain);
        }

        // Dev Mode only (Source/Debug/DebugAction_TestShedscaleRegrowth.cs).
        public void DevForceDailyPass()
        {
            DoDailyPass();
        }

        public void DevCompleteAllActiveRegrowths()
        {
            foreach (BodyPartRecord part in activeRegrowths.Keys.ToList())
                CompleteRegrowth(part);

            // CompleteRegrowth itself doesn't touch the Strain hediff — only DoDailyPass does, at the end of
            // its own pass. Without this, a tester using this tool outside a real daily pass would see a
            // stale Strain hediff (wrong severity, or lingering at 0 active regrowths) until tomorrow's tick.
            UpdateStrainHediff();
        }

        public void DevSetDaysWornCounter(int value)
        {
            daysWornCounter = value;
            TryUpgradeToTier2();
        }

        public void DevSetPartsRegrownCounter(int value)
        {
            partsRegrownCounter = value;
            TryUpgradeToTier2();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref tier, "tier", 1);
            Scribe_Values.Look(ref daysWornCounter, "daysWornCounter", 0);
            Scribe_Values.Look(ref partsRegrownCounter, "partsRegrownCounter", 0);
            Scribe_Values.Look(ref nextDailyCheckTick, "nextDailyCheckTick", 0);
            Scribe_Collections.Look(ref activeRegrowths, "activeRegrowths", LookMode.BodyPart, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && activeRegrowths == null)
                activeRegrowths = new Dictionary<BodyPartRecord, int>();
        }
    }
}
