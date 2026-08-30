using RimWorld;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    [DefOf]
    public static class TattooMagicDefOf
    {
        public static ThingDef TattooMagic_RitualStation;

        public static JobDef TattooMagic_TattooRitual;

        public static JobDef TattooMagic_WaitForTattooRitual;

        public static HediffDef TattooMagic_Tracker;

        public static HediffDef TattooMagic_Hediff_FrostSigilSlow;

        public static TattooMagicDef TattooMagic_Phoenix;

        public static HediffDef TattooMagic_Hediff_Phoenix;

        public static HediffDef TattooMagic_Hediff_ParalyticAbasia;

        static TattooMagicDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TattooMagicDefOf));
        }
    }
}
