using HarmonyLib;
using Verse;

namespace TattooMagic
{
    // Blocks any attempt to remove a tattoo-applied Hediff that doesn't go
    // through TattooHediffRemovalGuard.RemoveTattooHediff. No vanilla
    // mechanic (combat, surgery, healing) ever reaches a tattoo hediff in the
    // first place — they're plain HediffWithComps with no addedPartProps and
    // no HediffComp_Disappears (see Defs/HediffDefs/Tattoos/*.xml), so
    // vanilla generates no removal recipe and nothing decays them. This
    // patch's entire job is guarding against foreign code (another mod's
    // generic hediff-clearing tool, a "cleanse" ability from an unrelated
    // mod) reaching in via the same public RemoveHediff API RimWorld itself
    // uses — not vanilla gameplay, and not a developer/tester deliberately
    // using Dev Mode. DebugSettings.godMode is the same flag vanilla's own
    // health tab checks before showing its per-hediff delete button, so
    // honoring it here restores that expected debug capability rather than
    // leaving it collateral damage of a fix aimed at unattended mod code.
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RemoveHediff))]
    public static class Patch_HealthTracker_RemoveHediff_ProtectTattoos
    {
        public static bool Prefix(Hediff hediff)
        {
            if (DebugSettings.godMode)
                return true;

            if (!TattooHediffRemovalGuard.IsProtected(hediff))
                return true;

            return TattooHediffRemovalGuard.IsSanctioned(hediff);
        }
    }
}
