using RimWorld;
using LudeonTK;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    // Dev Mode utility (not shipped-feature behavior): automates Scenario 2's
    // "fewer actual breaks at equal mood" comparison end-to-end. Pins the
    // targeted pawn's Mood to an exact value every single simulated tick
    // (Need.CurLevel's public setter, re-applied inside the loop — not a
    // one-off drag that then drifts) and fast-forwards via Find.TickManager.
    // DoSingleTick() for a fixed duration, counting how many times the
    // pawn's own break-risk window opens and how many actual mental breaks
    // fire. No manual pinning, no waiting on real time (feature 013 testing
    // session, 2026-08-22 — the prior manual-drag approach for this
    // scenario specifically was correctly called out as impractical: the
    // UI only offers coarse +/-10% steps, and nothing keeps a value pinned
    // while time passes without constant re-intervention).
    //
    // Works on both a tattooed pawn and an untattooed control — Starlight
    // Ward's own comp isn't required; this reads only public vanilla
    // MentalBreaker/Pawn state, same as the tattoo's own detection does.
    public static class DebugAction_TestStarlightWardBreakExposure
    {
        private const float PinnedMood = 0.33f;
        private const int DurationTicks = 20000; // ~8 simulated hours

        [DebugAction("TattooMagic", "TEST: Break exposure at pinned mood (33%, ~8h)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestExposureAtPinnedMood(Pawn p)
        {
            MentalBreaker breaker = p.mindState?.mentalBreaker;
            if (breaker == null || p.needs?.mood == null)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no MentalBreaker/Mood need — cannot run this trial.");
                return;
            }

            HediffComp_StarlightWardEffect comp = FindComp(p);
            float originalMood = p.needs.mood.CurLevel;

            Log.Message($"[TattooMagic TEST] Starting break-exposure trial on {p.LabelShort}: pinning Mood at " +
                $"{PinnedMood:P0} for {DurationTicks} ticks. Thresholds — Minor: {breaker.BreakThresholdMinor:P0}, " +
                $"Major: {breaker.BreakThresholdMajor:P0}, Extreme: {breaker.BreakThresholdExtreme:P0}. " +
                $"Starlight Ward: {(comp != null ? $"yes (tier {comp.Tier})" : "no")}.");

            p.needs.mood.CurLevel = PinnedMood;

            bool wasImminent = false;
            bool wasInMentalState = p.InMentalState;
            int windowOpens = 0;
            int breaksObserved = 0;

            for (int i = 0; i < DurationTicks; i++)
            {
                Find.TickManager.DoSingleTick();

                // Check status as a RESULT of this tick's real simulation —
                // before re-pinning overwrites it. Checking after re-pinning
                // (the original bug) forces every read to see exactly
                // PinnedMood, silently hiding any transient dip a pawn whose
                // threshold sits just below the pin (e.g. Marie at 32% vs a
                // 33% pin) would otherwise show — found via live testing
                // 2026-08-22, where HediffComp_StarlightWardEffect's own
                // independent detection caught windows this loop was
                // missing.
                bool isImminentNow = breaker.BreakMinorIsImminent || breaker.BreakMajorIsImminent || breaker.BreakExtremeIsImminent;
                if (isImminentNow && !wasImminent)
                    windowOpens++;
                wasImminent = isImminentNow;

                bool inMentalStateNow = p.InMentalState;
                if (inMentalStateNow && !wasInMentalState)
                {
                    breaksObserved++;
                    // End it immediately so one long break doesn't consume
                    // the rest of the sampling window — this trial counts
                    // discrete onsets, not cumulative time spent broken.
                    p.MentalState?.RecoverFromState();
                }
                wasInMentalState = p.InMentalState;

                p.needs.mood.CurLevel = PinnedMood; // re-pin AFTER checking, for the next iteration
            }

            p.needs.mood.CurLevel = originalMood;

            Log.Message($"[TattooMagic TEST] Break-exposure trial on {p.LabelShort} complete: " +
                $"risk-window opened {windowOpens} time(s), {breaksObserved} actual mental break(s) triggered, " +
                $"over {DurationTicks} simulated ticks pinned at {PinnedMood:P0}. " +
                $"(A pawn whose threshold sits above the pinned mood should show 0 window-opens and 0 breaks; a " +
                $"pawn whose threshold sits below it should show a nonzero window-open count, with breaksObserved " +
                $"possibly still 0 over a single trial if the underlying MTB odds are low — that's expected, not a " +
                $"failure — windowOpens is this trial's primary signal.)");
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
