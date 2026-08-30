using HarmonyLib;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // Postfix on Verse.Pawn.SetFaction — the single choke point covering
    // recruit-away, defection, exile/banishment, and enslavement (research.md
    // R7). Plain capture-as-prisoner does NOT go through SetFaction at all
    // (a captured pawn keeps its own Faction, only HostFaction changes) — see
    // Patch_PawnGuestTracker_SetGuestStatus_PhoenixCaptured for that case,
    // which shares this file's PhoenixFactionLeaveUtility helper.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class Patch_Pawn_SetFaction_PhoenixRegistryUpdate
    {
        public static void Prefix(Pawn __instance, out Faction __state)
        {
            __state = __instance?.Faction;
        }

        public static void Postfix(Pawn __instance, Faction newFaction, Faction __state)
        {
            if (__instance?.health?.hediffSet == null)
                return;

            Hediff hediff = __instance.health.hediffSet.hediffs.Find(h => h.def == TattooMagicDefOf.TattooMagic_Hediff_Phoenix);
            if (hediff == null)
                return;

            bool wasPlayer = __state == Faction.OfPlayer;
            bool isPlayer = newFaction == Faction.OfPlayer;

            if (wasPlayer && !isPlayer)
            {
                // FR-004: recruited away, defected, exiled, or enslaved —
                // the tattoo is automatically stripped the instant they
                // leave, closing the "banish on purpose to free a slot and
                // take the tattoo with them" loophole.
                PhoenixFactionLeaveUtility.StripAndUnregister(__instance, hediff);
            }
            else if (!wasPlayer && isPlayer)
            {
                // FR-005: a pawn who joins the faction already wearing
                // Phoenix is allowed to keep it, even over the cap —
                // Register() itself has no cap check, only the ritual-
                // station application path (Dialog_ChooseTattoo) does.
                GameComponent_PhoenixRegistry.Get()?.Register(__instance);
            }
        }
    }

    // Shared by both faction-change patches — the one place a Phoenix
    // tattoo is actually stripped and its slot freed for a wearer who is no
    // longer part of the player's faction/roster for any reason.
    public static class PhoenixFactionLeaveUtility
    {
        public static void StripAndUnregister(Pawn pawn, Hediff phoenixHediff)
        {
            TattooHediffRemovalGuard.RemoveTattooHediff(pawn, phoenixHediff);
            GameComponent_PhoenixRegistry.Get()?.Unregister(pawn);

            if (TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic DEBUG] Phoenix: stripped from {pawn.LabelShort} — no longer part of the player's faction/roster.");
        }
    }
}
