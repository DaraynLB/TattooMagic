using System.Collections.Generic;
using Verse;

namespace TattooMagic
{
    // Wraithstep-specific bookkeeping (research.md R6, contracts/targeting-
    // exclusion-contract.md §2) — mirrors GuardiansCallTauntRegistry's shape
    // (a lazily-pruned HashSet) only, per two-path-targeting-contract.md §3's
    // guidance that a second targeting-influencing tattoo owns its own
    // registry rather than reusing Guardian's Call's. Tracks the comps with a
    // currently-active Tier 2 untargetable window so the hot-path targeting
    // Prefix can check a single, usually-empty set instead of scanning every
    // pawn's hediffs on every targeting decision.
    public static class WraithstepUntargetableRegistry
    {
        private static readonly HashSet<HediffComp_WraithstepEffect> activeUntargetable =
            new HashSet<HediffComp_WraithstepEffect>();

        // Cheap pre-check for the common case (no untargetable windows active
        // anywhere) — may still include stale entries the next IsUntargetable
        // call would prune, but that's fine for a fast "is it even worth
        // looking" gate.
        public static bool HasActiveUntargetable => activeUntargetable.Count > 0;

        public static void Register(HediffComp_WraithstepEffect comp)
        {
            activeUntargetable.Add(comp);
        }

        // Lazily prunes any entry whose owning pawn is dead/despawned/no
        // longer carries the hediff, or whose window has expired, at query
        // time — no scheduled removal callback needed.
        public static bool IsUntargetable(Pawn pawn)
        {
            if (pawn == null || activeUntargetable.Count == 0)
                return false;

            List<HediffComp_WraithstepEffect> expired = null;
            bool found = false;

            foreach (HediffComp_WraithstepEffect comp in activeUntargetable)
            {
                if (IsExpiredOrInvalid(comp))
                {
                    if (expired == null)
                        expired = new List<HediffComp_WraithstepEffect>();
                    expired.Add(comp);
                    continue;
                }

                if (comp.Pawn == pawn)
                    found = true;
            }

            if (expired != null)
            {
                foreach (HediffComp_WraithstepEffect comp in expired)
                    activeUntargetable.Remove(comp);
            }

            return found;
        }

        private static bool IsExpiredOrInvalid(HediffComp_WraithstepEffect comp)
        {
            Pawn pawn = comp.Pawn;

            if (pawn == null || pawn.Dead)
                return true;

            if (pawn.health?.hediffSet == null || !pawn.health.hediffSet.hediffs.Contains(comp.parent))
                return true;

            return !comp.IsCurrentlyUntargetable;
        }
    }
}
