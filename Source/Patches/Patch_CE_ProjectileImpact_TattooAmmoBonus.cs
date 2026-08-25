using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace TattooMagic
{
    // Reflection-based Harmony patch onto Combat Extended's own
    // ProjectileCE.Impact(Thing), applied imperatively rather than
    // discovered via [HarmonyPatch] attribute scanning, since ProjectileCE
    // isn't a compile-time reference (research.md R5, contract §3). Scales
    // exactly one in-flight projectile instance's DamageAmount when its
    // launcher is a Pawn with a Tier 2 Serpent's Eye — never touches shared
    // AmmoDef/ThingDef data other pawns firing the same ammo rely on.
    public static class Patch_CE_ProjectileImpact_TattooAmmoBonus
    {
        private static FieldInfo launcherField;
        private static PropertyInfo damageAmountProperty;

        // Never even attempts AccessTools.TypeByName when CE is absent
        // (constitution Principle I) — safe to call unconditionally.
        public static void TryApply(Harmony harmony)
        {
            if (!CombatExtendedInterop.IsLoaded)
                return;

            Type projectileCEType = AccessTools.TypeByName("CombatExtended.ProjectileCE");
            if (projectileCEType == null)
                return;

            MethodInfo impactMethod = AccessTools.Method(projectileCEType, "Impact", new[] { typeof(Thing) });
            launcherField = AccessTools.Field(projectileCEType, "launcher");
            damageAmountProperty = AccessTools.Property(projectileCEType, "DamageAmount");

            if (impactMethod == null || launcherField == null || damageAmountProperty == null)
                return;

            harmony.Patch(impactMethod,
                postfix: new HarmonyMethod(typeof(Patch_CE_ProjectileImpact_TattooAmmoBonus), nameof(Postfix)));
        }

        private static void Postfix(object __instance)
        {
            if (!(launcherField.GetValue(__instance) is Pawn launcher))
                return;

            HediffComp_SerpentsEyeEffect comp = FindTier2SerpentsEyeComp(launcher);
            if (comp == null)
                return;

            float multiplier = TattooEffectValues.Get(
                comp.parent.def.defName, "CeAmmoEffectivenessMultiplierTier2", comp.Props.ceAmmoEffectivenessMultiplierTier2);

            float currentDamage = (float)damageAmountProperty.GetValue(__instance);
            damageAmountProperty.SetValue(__instance, currentDamage * multiplier);
        }

        private static HediffComp_SerpentsEyeEffect FindTier2SerpentsEyeComp(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return null;

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is HediffComp_SerpentsEyeEffect comp && comp.tierState.tier >= 2)
                        return comp;
                }
            }

            return null;
        }
    }
}
