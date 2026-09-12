using RimWorld;
using Verse;

namespace TattooMagic
{
    // Flat, non-stacking mood modifier active whenever the pawn's Shedscale tattoo currently has at least one
    // part regrowing (FR-015) — zero persisted mood state, mirrors ThoughtWorker_StarlightWardTier2Active
    // (feature 013, research.md R5) exactly.
    public class ThoughtWorker_ShedscaleRegrowthActive : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            HediffComp_ShedscaleEffect comp = FindComp(p);
            if (comp == null || comp.ActiveRegrowthCount <= 0)
                return ThoughtState.Inactive;

            return ThoughtState.ActiveDefault;
        }

        private static HediffComp_ShedscaleEffect FindComp(Pawn p)
        {
            if (p?.health?.hediffSet?.hediffs == null)
                return null;

            var hediffs = p.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                var comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is HediffComp_ShedscaleEffect shedscale)
                        return shedscale;
                }
            }

            return null;
        }
    }
}
