using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TattooMagic
{
    // Tracks which HediffDefs represent an applied tattoo (any TattooMagicDef's
    // appliedHediff, feature 001) and gates their removal so only this mod's
    // own, explicitly-sanctioned code path can strip one. Closes the gap where
    // any other mod's code can call Pawn_HealthTracker.RemoveHediff on
    // anything, the same public API RimWorld itself uses — consumed by
    // Patch_HealthTracker_RemoveHediff_ProtectTattoos. Applies to every tattoo
    // automatically (keyed off appliedHediff, not a hardcoded defName list),
    // not just whichever tattoo's effect most recently shipped.
    [StaticConstructorOnStartup]
    public static class TattooHediffRemovalGuard
    {
        private static readonly HashSet<HediffDef> protectedHediffDefs;

        // No in-game removal feature exists yet (spec edge case), but one
        // might someday; this is that feature's future entry point. Scoped to
        // the specific Hediff instance being removed, not a pawn/def pair, so
        // concurrent sanctioned removals on different pawns can't collide.
        private static Hediff sanctionedRemoval;

        static TattooHediffRemovalGuard()
        {
            protectedHediffDefs = new HashSet<HediffDef>(
                DefDatabase<TattooMagicDef>.AllDefsListForReading
                    .Select(t => t.appliedHediff)
                    .Where(h => h != null));
        }

        public static bool IsProtected(Hediff hediff)
        {
            return hediff != null && protectedHediffDefs.Contains(hediff.def);
        }

        public static bool IsSanctioned(Hediff hediff)
        {
            return hediff != null && hediff == sanctionedRemoval;
        }

        // The one sanctioned way for this mod's own code to remove a tattoo
        // hediff. Anything that doesn't go through this — another mod's
        // hediff sweep, a debug/editor tool, a stray Harmony patch — is
        // blocked by Patch_HealthTracker_RemoveHediff_ProtectTattoos.
        public static void RemoveTattooHediff(Pawn pawn, Hediff hediff)
        {
            sanctionedRemoval = hediff;
            try
            {
                pawn.health.RemoveHediff(hediff);
            }
            finally
            {
                sanctionedRemoval = null;
            }
        }
    }
}
