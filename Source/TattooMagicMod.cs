using HarmonyLib;
using Verse;

namespace TattooMagic
{
    [StaticConstructorOnStartup]
    public static class TattooMagicMain
    {
        static TattooMagicMain()
        {
            var harmony = new Harmony("handle.tattoomagic");
            harmony.PatchAll();

            // Reflection-target patch — can't be discovered by PatchAll()'s
            // attribute scan since CombatExtended.ProjectileCE only exists
            // (and is only resolved) when CE is loaded (research.md R5).
            Patch_CE_ProjectileImpact_TattooAmmoBonus.TryApply(harmony);

            // Reflection-target patch — can't be discovered by PatchAll()'s
            // attribute scan since CombatExtended.CompSuppressable only
            // exists (and is only resolved) when CE is loaded (feature 005
            // research.md R4).
            Patch_CE_CompSuppressable_IsCrouchWalking_StormlashImmunity.TryApply(harmony);

            Log.Message("[TattooMagic] Mod loaded, Harmony patches applied.");
        }
    }
}
