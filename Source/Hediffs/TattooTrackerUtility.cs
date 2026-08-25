using Verse;

namespace TattooMagic
{
    public static class TattooTrackerUtility
    {
        public static HediffComp_TattooTracker GetTracker(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return null;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(TattooMagicDefOf.TattooMagic_Tracker);
            return (hediff as HediffWithComps)?.TryGetComp<HediffComp_TattooTracker>();
        }
    }
}
