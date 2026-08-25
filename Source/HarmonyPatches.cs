using HarmonyLib;
using Verse;

namespace TattooMagic
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_Pawn_SpawnSetup_AddTattooTracker
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance?.RaceProps == null || !__instance.RaceProps.Humanlike)
                return;

            if (__instance.health?.hediffSet == null)
                return;

            if (__instance.health.hediffSet.HasHediff(TattooMagicDefOf.TattooMagic_Tracker))
                return;

            Hediff tracker = HediffMaker.MakeHediff(TattooMagicDefOf.TattooMagic_Tracker, __instance);
            __instance.health.AddHediff(tracker);
        }
    }
}
