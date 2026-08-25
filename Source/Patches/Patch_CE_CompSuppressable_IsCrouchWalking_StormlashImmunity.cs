using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace TattooMagic
{
    // Reflection-based Harmony patch onto Combat Extended's own
    // CompSuppressable.IsCrouchWalking property getter, applied imperatively
    // rather than discovered via [HarmonyPatch] attribute scanning, since
    // CompSuppressable isn't a compile-time reference (research.md R4,
    // contract §3, following Patch_CE_ProjectileImpact_TattooAmmoBonus's
    // established convention). CE's own StatWorker_MoveSpeed multiplies
    // MoveSpeed by 0.67 whenever this property is true, *before* this mod's
    // additive StatPart_TattooEffectOffset ever runs — so neutralizing it
    // here, rather than trying to out-add it, is what makes Tier 2's slow
    // immunity genuine rather than merely "less slow." The property's
    // declaring type is a CE-only ThingComp subclass, but ThingComp itself
    // is a vanilla type, so the postfix can accept it directly (and read its
    // public `parent` field) without any reflection beyond resolving and
    // patching the getter method itself.
    public static class Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity
    {
        // Never even attempts AccessTools.TypeByName when CE is absent
        // (constitution Principle I) — safe to call unconditionally.
        public static void TryApply(Harmony harmony)
        {
            if (!CombatExtendedInterop.IsLoaded)
                return;

            Type compSuppressableType = AccessTools.TypeByName("CombatExtended.CompSuppressable");
            if (compSuppressableType == null)
                return;

            var getter = AccessTools.PropertyGetter(compSuppressableType, "IsCrouchWalking");
            if (getter == null)
                return;

            harmony.Patch(getter,
                postfix: new HarmonyMethod(typeof(Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity), nameof(Postfix)));
        }

        private static void Postfix(ThingComp __instance, ref bool __result)
        {
            if (!__result)
                return;

            if (!(__instance.parent is Pawn pawn))
                return;

            if (HasActiveTier2StormlashBoost(pawn))
            {
                __result = false;

                // Dev-Mode-only, always-on visibility (research.md R4) —
                // fires only on the actual true->false flip, i.e. only when
                // this patch changes CE's real suppression-slow outcome.
                if (TattooMagicSettings.EnableDebugLogging)
                {
                    Log.Message($"[TattooMagic DEBUG] Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity: " +
                        $"neutralized suppression-slow (IsCrouchWalking true->false) for {pawn.LabelShort}.");
                }
            }
        }

        private static bool HasActiveTier2StormlashBoost(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return false;

            int now = Find.TickManager.TicksGame;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is HediffComp_StormlashEffect comp
                        && comp.tierState.tier >= 2
                        && now < comp.boostEndTick)
                        return true;
                }
            }

            return false;
        }
    }
}
