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

        static TattooMagicDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TattooMagicDefOf));
        }
    }
}
