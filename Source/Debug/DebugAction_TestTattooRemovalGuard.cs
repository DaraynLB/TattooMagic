using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Verse;

namespace TattooMagic
{
    // Dev Mode utility (not shipped-feature behavior): attempts an
    // unsanctioned RemoveHediff call on every tattoo hediff a targeted pawn
    // currently has, to verify Patch_HealthTracker_RemoveHediff_
    // ProtectTattoos actually blocks it with God Mode off. Keyed off every
    // TattooMagicDef.appliedHediff generically (same reverse-lookup pattern
    // TattooHediffRemovalGuard itself uses), not hardcoded to any one
    // tattoo, so it stays useful for verifying this same guard against every
    // future tattoo without needing its own update each time. Kept
    // permanently at the customer's request (feature 007 testing session).
    public static class DebugAction_TestTattooRemovalGuard
    {
        [DebugAction("TattooMagic", "TEST: unsanctioned RemoveHediff on tattoos", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestUnsanctionedRemoval(Pawn p)
        {
            HashSet<HediffDef> appliedHediffDefs = new HashSet<HediffDef>(
                DefDatabase<TattooMagicDef>.AllDefsListForReading
                    .Select(t => t.appliedHediff)
                    .Where(h => h != null));

            List<Hediff> tattooHediffs = p.health?.hediffSet?.hediffs?
                .Where(h => appliedHediffDefs.Contains(h.def))
                .ToList();

            if (tattooHediffs == null || tattooHediffs.Count == 0)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no tattoo hediffs to test removal on.");
                return;
            }

            foreach (Hediff hediff in tattooHediffs)
            {
                Log.Message($"[TattooMagic TEST] Attempting unsanctioned RemoveHediff on {p.LabelShort}'s " +
                    $"{hediff.def.defName} (God Mode: {DebugSettings.godMode})...");
                p.health.RemoveHediff(hediff);
                bool stillPresent = p.health.hediffSet.hediffs.Contains(hediff);
                Log.Message(stillPresent
                    ? $"[TattooMagic TEST] RESULT: {hediff.def.defName} still present — guard correctly blocked the removal."
                    : $"[TattooMagic TEST] RESULT: {hediff.def.defName} was removed — guard FAILED to block it.");
            }
        }
    }
}
