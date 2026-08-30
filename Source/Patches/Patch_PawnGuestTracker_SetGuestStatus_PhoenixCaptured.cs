using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // Postfix on RimWorld.Pawn_GuestTracker.SetGuestStatus — covers plain
    // capture-as-prisoner, which Pawn.SetFaction alone doesn't see (a
    // captured pawn keeps its own Faction unchanged; only its HostFaction
    // changes, research.md R7). Shares Patch_Pawn_SetFaction_
    // PhoenixRegistryUpdate's own strip/unregister helper.
    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus))]
    public static class Patch_PawnGuestTracker_SetGuestStatus_PhoenixCaptured
    {
        // Pawn_GuestTracker.pawn is private with no public accessor.
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_GuestTracker), "pawn");

        public static void Postfix(Pawn_GuestTracker __instance, Faction newHost, GuestStatus guestStatus)
        {
            if (guestStatus != GuestStatus.Prisoner)
                return;

            Pawn pawn = PawnField?.GetValue(__instance) as Pawn;

            // Only a player-faction colonist being taken prisoner by
            // someone else counts — not the player capturing an enemy.
            if (pawn == null || pawn.Faction != Faction.OfPlayer || newHost == Faction.OfPlayer)
                return;

            Hediff hediff = pawn.health?.hediffSet?.hediffs?.Find(h => h.def == TattooMagicDefOf.TattooMagic_Hediff_Phoenix);
            if (hediff == null)
                return;

            PhoenixFactionLeaveUtility.StripAndUnregister(pawn, hediff);
        }
    }
}
