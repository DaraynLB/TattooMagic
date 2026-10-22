using HarmonyLib;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // Additive tend-quality bonus for a Soul Tether-buffed patient
    // (research.md R4 of specs/017-soul-tether-tattoo-effect). Vanilla's own
    // tend-quality math never references the patient's own stats or
    // hediffs (RimWorld.TendUtility, decompiled) — this is the one
    // patient-aware overload DoTend actually calls, so a narrow postfix here
    // is the smallest correct fix, rather than a StatPart on a doctor-side
    // stat (contrast Patch's sibling, StatPart_SoulTetherTendSpeedBuff, for
    // tend SPEED, which has no equivalent patient-aware method to postfix).
    // Purely additive: a patient with no active buff hediff gets +0f,
    // identical to vanilla's unmodified result.
    [HarmonyPatch(typeof(TendUtility), nameof(TendUtility.CalculateBaseTendQuality),
        new[] { typeof(Pawn), typeof(Pawn), typeof(ThingDef) })]
    public static class Patch_TendUtility_CalculateBaseTendQuality_SoulTetherBuff
    {
        public static void Postfix(Pawn patient, ref float __result)
        {
            float bonus = SoulTetherTendBuffUtility.GetActiveQualityBonus(patient);
            if (bonus == 0f)
                return;

            __result += bonus;

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] Soul Tether tend-quality bonus of {bonus:F2} applied for " +
                    $"{patient?.LabelShort}'s tend (new base quality: {__result:F2}).");
            }
        }
    }
}
