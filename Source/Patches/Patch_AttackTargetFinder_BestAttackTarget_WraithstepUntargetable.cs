using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    // The inverse of Guardian's Call's inclusion-style targeting patch
    // (research.md R6, contracts/targeting-exclusion-contract.md). A Prefix,
    // not a Postfix: composes the vanilla validator parameter before
    // AttackTargetFinder.BestAttackTarget's own search runs, so vanilla's
    // algorithm naturally skips any pawn currently inside a Wraithstep Tier 2
    // untargetable window and falls through to its own correct next-best
    // candidate — a Postfix overriding __result could only null it, leaving a
    // hostile targetless instead. Because Harmony runs every Prefix before
    // the original method and the original before every Postfix, Guardian's
    // Call's own Postfix (which re-checks validator(taunterPawn) before
    // accepting a taunt candidate) sees this already-composed validator too,
    // so a Wraithstep-untargetable pawn is correctly excluded from Guardian's
    // Call's own taunt as well, with no change to either feature's file
    // (contracts/targeting-exclusion-contract.md §3).
    [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget))]
    public static class Patch_AttackTargetFinder_BestAttackTarget_WraithstepUntargetable
    {
        public static void Prefix(ref Predicate<Thing> validator)
        {
            // Cheap common-case bail: no active untargetable windows
            // anywhere, skip composing a wrapper entirely (research.md R6 —
            // this runs on a hot AI path, same discipline as Guardian's
            // Call's own registry gate).
            if (!WraithstepUntargetableRegistry.HasActiveUntargetable)
                return;

            Predicate<Thing> originalValidator = validator;

            validator = originalValidator == null
                ? (Predicate<Thing>)IsNotWraithstepUntargetable
                : (t => originalValidator(t) && IsNotWraithstepUntargetable(t));
        }

        private static bool IsNotWraithstepUntargetable(Thing t)
        {
            if (!(t is Pawn pawn))
                return true;

            bool untargetable = WraithstepUntargetableRegistry.IsUntargetable(pawn);

            if (untargetable && TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic] Wraithstep untargetable window excluding {pawn.LabelShort} from a hostile target search.");

            return !untargetable;
        }
    }
}
