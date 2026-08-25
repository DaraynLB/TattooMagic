using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    // The vanilla path of the two-path AI-targeting convention (research.md
    // R4, contracts/two-path-targeting-contract.md §1). Postfix, never
    // Prefix — vanilla's own algorithm always runs to completion first; this
    // only ever *replaces* __result with a taunter that independently passes
    // the same validator/reachability bar vanilla's own search would
    // require, so a hostile is never left targetless because of a taunt.
    [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget))]
    public static class Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt
    {
        public static void Postfix(IAttackTargetSearcher searcher, Predicate<Thing> validator, ref IAttackTarget __result)
        {
            // Cheap common-case bail: no active taunts anywhere, skip the
            // rest entirely (research.md R3 — this runs on a hot AI path).
            if (!GuardiansCallTauntRegistry.HasActiveTaunters)
                return;

            if (!(searcher.Thing is Pawn searcherPawn))
                return;

            // research.md R5 CE-investigation probe (tasks.md T009) — gated
            // behind Dev Mode so it never spams a normal player's log. Remove
            // or leave gated per T018 once the CE verification pass (T009/
            // T011) is complete.
            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic] Guardian's Call taunt postfix evaluating searcher={searcherPawn.LabelShort} " +
                    $"(CE loaded={CombatExtendedInterop.IsLoaded})");
            }

            HediffComp_GuardiansCallEffect nearestTaunter = null;
            float nearestDistSq = float.MaxValue;

            foreach (HediffComp_GuardiansCallEffect comp in GuardiansCallTauntRegistry.ActiveTaunters())
            {
                Pawn taunterPawn = comp.Pawn;
                if (taunterPawn == null || taunterPawn == searcherPawn || !GenHostility.HostileTo(searcherPawn, taunterPawn))
                    continue;

                float range = comp.TauntRange;
                float distSq = (taunterPawn.Position - searcherPawn.Position).LengthHorizontalSquared;
                if (distSq > range * range)
                    continue;

                if (validator != null && !validator(taunterPawn))
                    continue;

                if (!searcherPawn.CanReach(taunterPawn, PathEndMode.Touch, Danger.Some))
                    continue;

                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearestTaunter = comp;
                }
            }

            if (nearestTaunter == null)
                return;

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic] Guardian's Call taunt redirected {searcherPawn.LabelShort}'s target to " +
                    $"{nearestTaunter.Pawn.LabelShort}.");
            }

            __result = (IAttackTarget)nearestTaunter.Pawn;
        }
    }
}
