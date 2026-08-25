using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace TattooMagic
{
    // Shared, tattoo-agnostic reuse point for "grant this pawn a gizmo"
    // (research.md R1, contract triggered-tattoo-effect-contract.md §1).
    // Postfixes the real, public Pawn.GetGizmos() — yields vanilla's own
    // gizmos first, untouched, then appends GetGizmo() for every hediff comp
    // implementing IProvidesTattooGizmo. Carries no Guardian's-Call-specific
    // knowledge, exactly like the existing on-hit patch
    // (Patch_Pawn_PostApplyDamage_TattooOnHit).
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_TattooGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
        {
            foreach (Gizmo gizmo in gizmos)
                yield return gizmo;

            if (__instance?.health?.hediffSet == null)
                yield break;

            List<Hediff> hediffs = __instance.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (!(comps[j] is IProvidesTattooGizmo provider))
                        continue;

                    Gizmo gizmo = provider.GetGizmo();

                    // Tattoo-agnostic mastery-progress hook (research.md R1,
                    // contracts/mastery-progression-contract.md §1-2): wraps
                    // whatever Command_Action a triggered tattoo's own comp
                    // returned, so its click counts toward the pawn-wide
                    // tattoo-mastery track with zero per-tattoo opt-in code.
                    // Skipped for a comp implementing IHandlesOwnMasteryProgress
                    // (feature 008 research.md R4, contracts/cell-targeted-
                    // ability-contract.md §2) — its gizmo click only opens a
                    // targeting session rather than completing the activation,
                    // so crediting progress here (on click, not on a confirmed
                    // target) would count cancelled attempts too; that comp
                    // credits progress itself, only on a genuine activation.
                    if (gizmo is Command_Action commandAction && !(provider is IHandlesOwnMasteryProgress))
                    {
                        Action originalAction = commandAction.action;
                        commandAction.action = () =>
                        {
                            originalAction?.Invoke();
                            TattooTrackerUtility.GetTracker(__instance)?.RegisterMasteryActivation();
                        };
                    }

                    yield return gizmo;
                }
            }
        }
    }
}
