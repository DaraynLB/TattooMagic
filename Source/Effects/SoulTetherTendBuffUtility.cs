using Verse;

namespace TattooMagic
{
    // Grants, refreshes, and reads the transient Soul Tether tend buff
    // (research.md R6 of specs/017-soul-tether-tattoo-effect). The single
    // point both HediffComp_SoulTetherEffect (on grant) and the two new
    // tend-quality/tend-speed dispatch points (research.md R4/R5) consult.
    public static class SoulTetherTendBuffUtility
    {
        public static void GrantOrRefresh(Pawn patient, float qualityBonus, float speedBonus, int durationTicks)
        {
            if (patient?.health?.hediffSet == null)
                return;

            Hediff existing = patient.health.hediffSet.GetFirstHediffOfDef(TattooMagicDefOf.TattooMagic_Hediff_SoulTetherTendBuff);

            HediffWithComps buffHediff;
            if (existing is HediffWithComps existingWithComps)
            {
                buffHediff = existingWithComps;
            }
            else
            {
                Hediff created = HediffMaker.MakeHediff(TattooMagicDefOf.TattooMagic_Hediff_SoulTetherTendBuff, patient);
                patient.health.AddHediff(created);
                buffHediff = created as HediffWithComps;
            }

            if (buffHediff == null)
                return;

            buffHediff.TryGetComp<HediffComp_SoulTetherTendBuff>()?.Configure(qualityBonus, speedBonus);
            buffHediff.TryGetComp<HediffComp_Disappears>()?.SetDuration(durationTicks);
        }

        public static float GetActiveQualityBonus(Pawn patient)
        {
            return GetComp(patient)?.qualityBonus ?? 0f;
        }

        public static float GetActiveSpeedBonus(Pawn patient)
        {
            return GetComp(patient)?.speedBonus ?? 0f;
        }

        private static HediffComp_SoulTetherTendBuff GetComp(Pawn patient)
        {
            Hediff hediff = patient?.health?.hediffSet?.GetFirstHediffOfDef(TattooMagicDefOf.TattooMagic_Hediff_SoulTetherTendBuff);
            return (hediff as HediffWithComps)?.TryGetComp<HediffComp_SoulTetherTendBuff>();
        }
    }
}
