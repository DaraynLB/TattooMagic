using System.Collections.Generic;
using Verse;

namespace TattooMagic
{
    public static class TattooTierProgressUtility
    {
        public static IProvidesTattooTierProgress GetTierProgress(Pawn pawn, TattooMagicDef tattoo)
        {
            if (pawn?.health?.hediffSet == null || tattoo?.appliedHediff == null)
                return null;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(tattoo.appliedHediff);
            if (!(hediff is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                return null;

            List<HediffComp> comps = hediffWithComps.comps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is IProvidesTattooTierProgress provider)
                    return provider;
            }

            return null;
        }

        // Null for a tattoo with no tier system of its own (e.g. a flat
        // passive with no dedicated effect comp yet).
        public static string DescribeTier(Pawn pawn, TattooMagicDef tattoo)
        {
            IProvidesTattooTierProgress info = GetTierProgress(pawn, tattoo);
            if (info == null)
                return null;

            return info.NextTierThreshold.HasValue
                ? $"Tier {info.Tier} ({info.ProgressionCounter}/{info.NextTierThreshold.Value} to Tier 2)"
                : $"Tier {info.Tier} (max)";
        }
    }
}
