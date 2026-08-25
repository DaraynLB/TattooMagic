using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // Shared, tattoo-agnostic dispatch point for "this pawn is about to take
    // damage" (research.md R4-R5, feature 011). Unlike
    // Patch_Pawn_PostApplyDamage_TattooOnHit, this is a PREFIX on
    // Pawn.PreApplyDamage, so it runs before armor/health processing and can
    // fully prevent the damage — necessary because a postfix can't stop
    // damage that already happened, and because this dispatch must not be
    // gated on the instigator being a Pawn (fire/burn damage frequently has
    // no pawn instigator at all, e.g. an environmental/building fire).
    //
    // Ember Ward (feature 011) is the first consumer, not the only intended
    // one — a future tattoo reacting to a different DamageDef can implement
    // IOnIncomingDamageTattooEffect and filter for its own defs without any
    // change here.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    public static class Patch_Pawn_PreApplyDamage_TattooDamageAbsorb
    {
        public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            if (__instance?.health?.hediffSet == null)
                return true;

            List<Hediff> hediffs = __instance.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (!(comps[j] is IOnIncomingDamageTattooEffect effect))
                        continue;

                    bool didAbsorb = effect.TryAbsorbIncomingDamage(ref dinfo);

                    if (TattooMagicSettings.EnableDebugLogging)
                    {
                        Log.Message($"[TattooMagic DEBUG] Patch_Pawn_PreApplyDamage_TattooDamageAbsorb: " +
                            $"{__instance.LabelShort} incoming {dinfo.Def?.defName} ({dinfo.Amount:F1} dmg) " +
                            $"vs {comps[j].GetType().Name}: absorbed={didAbsorb}.");
                    }

                    if (didAbsorb)
                    {
                        absorbed = true;
                        TattooTrackerUtility.GetTracker(__instance)?.RegisterMasteryActivation();
                        return false;
                    }
                }
            }

            return true;
        }
    }

    // Debug-only visibility companion to the prefix above: logs the ACTUAL
    // damage a pawn ends up taking from a fire/burn hit, after armor has
    // resolved (totalDamageDealt), for every pawn — tattooed or not. Exists
    // because the prefix's own log only fires for pawns with a comp
    // implementing IOnIncomingDamageTattooEffect, and even then only shows
    // the pre-armor requested amount, not what actually landed — neither is
    // enough to A/B compare an Ember Ward-tattooed pawn against an
    // untattooed control pawn taking the same hit. Scoped to Flame/Burn only
    // to avoid spamming the log with every other damage type. Unlike
    // Patch_Pawn_PostApplyDamage_TattooOnHit's own logging, this one is not
    // gated on dinfo.Instigator being a Pawn, since Dev Mode's "Apply
    // damage" tool (and real environmental fire) has none.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Patch_Pawn_PostApplyDamage_TattooDamageAbsorbDebug
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (!TattooMagicSettings.EnableDebugLogging)
                return;

            if (dinfo.Def != DamageDefOf.Flame && dinfo.Def != DamageDefOf.Burn)
                return;

            float rawArmorRatingHeat = __instance.GetStatValue(StatDefOf.ArmorRating_Heat);

            int emberWardCompCount = 0;
            List<Hediff> hediffs = __instance.health?.hediffSet?.hediffs;
            if (hediffs != null)
            {
                for (int i = 0; i < hediffs.Count; i++)
                {
                    if (!(hediffs[i] is HediffWithComps hwc) || hwc.comps == null)
                        continue;

                    for (int j = 0; j < hwc.comps.Count; j++)
                    {
                        if (hwc.comps[j] is HediffComp_EmberWardEffect)
                            emberWardCompCount++;
                    }
                }
            }

            Log.Message($"[TattooMagic DEBUG] Fire/burn damage result: {__instance.LabelShort} actually took " +
                $"{totalDamageDealt:F1} dmg from {dinfo.Def.defName} (requested {dinfo.Amount:F1} pre-armor). " +
                $"Live ArmorRating_Heat={rawArmorRatingHeat:F3}, EmberWardEffect comp count={emberWardCompCount}.");
        }
    }
}
