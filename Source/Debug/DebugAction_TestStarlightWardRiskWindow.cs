using System.Linq;
using RimWorld;
using LudeonTK;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    // Dev Mode utility (not shipped-feature behavior): deterministically
    // drives a targeted pawn through one full Starlight Ward risk-window
    // cycle — either a clean recovery (should count as resisted) or a
    // forced mental break mid-window (should NOT count) — and logs
    // PASS/FAIL. Replaces manually dragging Mood and waiting on real-time
    // drift/RNG for the same check (feature 013 testing session,
    // 2026-08-22): forces Mood directly (Need.CurLevel has a public
    // setter) and fast-forwards game state via Find.TickManager.
    // DoSingleTick() in a tight loop, so a full trial completes in well
    // under a second of wall-clock time regardless of how many game ticks
    // it actually simulates. Mirrors DebugAction_TestTattooRemovalGuard's
    // permanent-test-tool precedent (feature 003).
    public static class DebugAction_TestStarlightWardRiskWindow
    {
        private const int MaxTicksPerPhase = 1000;

        [DebugAction("TattooMagic", "TEST: Starlight Ward risk window (clean recovery, should count)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestCleanRecovery(Pawn p) => RunTrial(p, forceBreak: false);

        [DebugAction("TattooMagic", "TEST: Starlight Ward risk window (forced break, should NOT count)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestForcedBreak(Pawn p) => RunTrial(p, forceBreak: true);

        private static void RunTrial(Pawn p, bool forceBreak)
        {
            HediffComp_StarlightWardEffect comp = FindComp(p);
            if (comp == null)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no Starlight Ward tattoo to test.");
                return;
            }

            MentalBreaker breaker = p.mindState?.mentalBreaker;
            if (breaker == null || p.needs?.mood == null)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no MentalBreaker/Mood need — cannot run this trial.");
                return;
            }

            int counterBefore = comp.ProgressionCounter;
            float originalMood = p.needs.mood.CurLevel;

            Log.Message($"[TattooMagic TEST] Starting Starlight Ward trial ({(forceBreak ? "forced break" : "clean recovery")}) " +
                $"on {p.LabelShort}: counter starts at {counterBefore}, threshold(minor)={breaker.BreakThresholdMinor:P0}.");

            // Phase 1: force Mood below the Minor threshold and tick until
            // our own risk window opens.
            float belowThreshold = Mathf.Clamp01(breaker.BreakThresholdMinor - 0.05f);
            p.needs.mood.CurLevel = belowThreshold;

            int ticks = 0;
            while (!comp.inRiskWindow && ticks < MaxTicksPerPhase)
            {
                Find.TickManager.DoSingleTick();
                if (p.needs.mood.CurLevel > belowThreshold)
                    p.needs.mood.CurLevel = belowThreshold;
                ticks++;
            }

            if (!comp.inRiskWindow)
            {
                Log.Message($"[TattooMagic TEST] FAIL: risk window never opened for {p.LabelShort} within {MaxTicksPerPhase} ticks.");
                p.needs.mood.CurLevel = originalMood;
                return;
            }

            Log.Message($"[TattooMagic TEST] Risk window open for {p.LabelShort} after {ticks} ticks.");

            // Phase 2: optionally force a real mental break while the
            // window is open, then let Mood recover so the window closes.
            if (forceBreak)
            {
                MentalBreakDef breakDef = DefDatabase<MentalBreakDef>.AllDefs
                    .Where(d => d.intensity == MentalBreakIntensity.Minor && d.Worker.BreakCanOccur(p))
                    .FirstOrDefault();

                if (breakDef == null)
                {
                    Log.Message($"[TattooMagic TEST] FAIL: no eligible Minor MentalBreakDef found to force on {p.LabelShort}.");
                    p.needs.mood.CurLevel = originalMood;
                    return;
                }

                breakDef.Worker.TryStart(p, "TattooMagic debug test", causedByMood: false);
                Log.Message($"[TattooMagic TEST] Forced {breakDef.defName} on {p.LabelShort}.");
            }

            p.needs.mood.CurLevel = 1f;

            ticks = 0;
            while (comp.inRiskWindow && ticks < MaxTicksPerPhase)
            {
                Find.TickManager.DoSingleTick();
                ticks++;
            }

            if (comp.inRiskWindow)
            {
                Log.Message($"[TattooMagic TEST] FAIL: risk window never closed for {p.LabelShort} within {MaxTicksPerPhase} ticks.");
                p.needs.mood.CurLevel = originalMood;
                return;
            }

            int counterAfter = comp.ProgressionCounter;
            bool counted = counterAfter > counterBefore;
            bool pass = forceBreak ? !counted : counted;

            Log.Message($"[TattooMagic TEST] Starlight Ward trial ({(forceBreak ? "forced break" : "clean recovery")}) on {p.LabelShort} " +
                $"complete after {ticks} ticks: counter {counterBefore} -> {counterAfter}. RESULT: {(pass ? "PASS" : "FAIL")}");

            // Cleanup: restore original mood, end any mental state our
            // forced break started, mirroring vanilla's own "T: Stop mental
            // state" tool.
            p.needs.mood.CurLevel = originalMood;
            if (p.InMentalState)
                p.MentalState.RecoverFromState();
        }

        private static HediffComp_StarlightWardEffect FindComp(Pawn p)
        {
            if (p?.health?.hediffSet?.hediffs == null)
                return null;

            var hediffs = p.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                var comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is HediffComp_StarlightWardEffect starlightWard)
                        return starlightWard;
                }
            }

            return null;
        }
    }
}
