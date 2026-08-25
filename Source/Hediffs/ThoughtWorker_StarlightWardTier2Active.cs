using RimWorld;
using Verse;

namespace TattooMagic
{
    // Small, private lookup — active only while the pawn's Starlight Ward
    // tattoo is at Tier 2 (research.md R5). Deliberately reads the owning
    // comp's own tierState directly rather than mirroring tier onto the
    // hediff's own Severity/stage, keeping TattooTierProgress the single
    // source of truth for tier, consistent with every other tattoo.
    public class ThoughtWorker_StarlightWardTier2Active : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            HediffComp_StarlightWardEffect comp = FindComp(p);
            if (comp == null || comp.Tier < 2)
                return ThoughtState.Inactive;

            return ThoughtState.ActiveDefault;
        }

        private static HediffComp_StarlightWardEffect FindComp(Pawn p)
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
                    if (comps[j] is HediffComp_StarlightWardEffect starlightWard)
                        return starlightWard;
                }
            }

            return null;
        }
    }
}
