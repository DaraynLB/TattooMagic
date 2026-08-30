using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
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

            // Found via live testing (2026-08-28): Dialog_ChooseTattoo's
            // TryQueueRitual force-starts a wait job on the recipient, but
            // nothing ever clears HediffComp_TattooTracker.pendingRitualTattoo
            // if the player manually redirects that pawn afterward (a
            // different order, a draft, anything that interrupts the wait
            // job) — with no cancel gizmo, the recipient stays permanently
            // "pending" with no job driving them back to the station,
            // spamming WorkGiver_TattooRitual's own debug scan log on every
            // other colonist forever. Clearing pendingRitualTattoo here is
            // enough on its own: both JobDriver_WaitForTattooRitual and
            // JobDriver_TattooRitual already FailOn it going null, so
            // whichever job (if any) is still active ends itself on its own
            // next tick — no separate EndCurrentJob call needed.
            HediffComp_TattooTracker tracker = TattooTrackerUtility.GetTracker(__instance);
            if (tracker?.pendingRitualTattoo != null)
            {
                TattooMagicDef pendingTattoo = tracker.pendingRitualTattoo;
                yield return new Command_Action
                {
                    defaultLabel = $"Cancel {pendingTattoo.label} ritual",
                    defaultDesc = $"Cancel {__instance.LabelShortCap}'s queued {pendingTattoo.label} tattoo ritual and free them for other work. Safe even mid-ritual — any ingredients already consumed by the performer are lost, same as a failed ritual.",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/TattooRitual", false),
                    action = () => tracker.pendingRitualTattoo = null,
                };
            }

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
