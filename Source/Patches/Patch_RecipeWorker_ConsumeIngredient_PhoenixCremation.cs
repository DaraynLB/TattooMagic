using HarmonyLib;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // Prefix on Verse.RecipeWorker.ConsumeIngredient — the CremateCorpse
    // bill declares no workerClass override, so it runs through this exact
    // base method (research.md R5). Runs BEFORE the base method's own
    // ingredient.Destroy() call, so the corpse's location/map are still
    // readable here for the fallback-spawn gap (research.md R4).
    [HarmonyPatch(typeof(RecipeWorker), nameof(RecipeWorker.ConsumeIngredient))]
    public static class Patch_RecipeWorker_ConsumeIngredient_PhoenixCremation
    {
        public static void Prefix(Thing ingredient, RecipeDef recipe)
        {
            if (recipe?.defName != "CremateCorpse")
                return;

            if (!(ingredient is Corpse corpse) || corpse.InnerPawn == null)
                return;

            Hediff hediff = corpse.InnerPawn.health?.hediffSet?.hediffs?.Find(h => h.def == TattooMagicDefOf.TattooMagic_Hediff_Phoenix);
            HediffComp_PhoenixEffect comp = (hediff as HediffWithComps)?.TryGetComp<HediffComp_PhoenixEffect>();
            if (comp == null)
                return;

            // FR-020/FR-022: guarantees the next scheduled attempt, doesn't
            // change its timing. FR-023 (gear lost) needs no code here —
            // it's inherent to the vanilla CremateCorpse bill itself.
            comp.cremationGuaranteed = true;
            comp.cremationFallbackPos = corpse.PositionHeld;
            comp.cremationFallbackMap = corpse.MapHeld;

            if (TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic DEBUG] Phoenix: {corpse.InnerPawn.LabelShort}'s corpse cremated via CremateCorpse — next revival attempt guaranteed to succeed.");
        }
    }
}
