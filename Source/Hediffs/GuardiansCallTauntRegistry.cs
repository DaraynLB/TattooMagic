using System.Collections.Generic;
using Verse;

namespace TattooMagic
{
    // Guardian's-Call-specific bookkeeping (research.md R3) — intentionally
    // NOT part of the reusable gizmo/cooldown contract. Tracks the comps with
    // a currently-active taunt so the hot-path targeting patch can check a
    // single, usually-empty set instead of scanning every pawn's hediffs on
    // every targeting decision.
    public static class GuardiansCallTauntRegistry
    {
        private static readonly HashSet<HediffComp_GuardiansCallEffect> activeTaunters =
            new HashSet<HediffComp_GuardiansCallEffect>();

        // Cheap pre-check for the common case (no taunts active anywhere) —
        // may still include stale entries the next ActiveTaunters() call
        // would prune, but that's fine for a fast "is it even worth looking"
        // gate.
        public static bool HasActiveTaunters => activeTaunters.Count > 0;

        public static void Register(HediffComp_GuardiansCallEffect comp)
        {
            activeTaunters.Add(comp);
        }

        // Lazily prunes any entry whose owning pawn is dead/downed/no longer
        // carries the hediff, or whose taunt has expired, at query time — no
        // scheduled removal callback needed (Edge Cases: the taunt ends
        // immediately once the tattooed pawn is downed/killed/loses the
        // hediff).
        public static IEnumerable<HediffComp_GuardiansCallEffect> ActiveTaunters()
        {
            if (activeTaunters.Count == 0)
                yield break;

            List<HediffComp_GuardiansCallEffect> expired = null;

            foreach (HediffComp_GuardiansCallEffect comp in activeTaunters)
            {
                if (IsExpiredOrInvalid(comp))
                {
                    if (expired == null)
                        expired = new List<HediffComp_GuardiansCallEffect>();
                    expired.Add(comp);
                    continue;
                }

                yield return comp;
            }

            if (expired != null)
            {
                foreach (HediffComp_GuardiansCallEffect comp in expired)
                    activeTaunters.Remove(comp);
            }
        }

        private static bool IsExpiredOrInvalid(HediffComp_GuardiansCallEffect comp)
        {
            Pawn pawn = comp.Pawn;

            if (pawn == null || pawn.Dead || pawn.Downed)
                return true;

            if (pawn.health?.hediffSet == null || !pawn.health.hediffSet.hediffs.Contains(comp.parent))
                return true;

            return Find.TickManager.TicksGame >= comp.tauntEndTick;
        }
    }
}
