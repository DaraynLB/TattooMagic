using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // Dev Mode utility (not shipped-feature behavior): Phoenix's own core
    // timers are genuinely real-world days long (5/3-day waits, daily
    // retries, a days-long Tier 2 progression clock), impractical to wait
    // out for real — mirrors feature 013's own precedent of adding
    // deterministic debug tooling for a mechanic too slow to hand-test.
    public static class DebugAction_TestPhoenixRevival
    {
        private static HediffComp_PhoenixEffect GetPhoenixComp(Pawn p)
        {
            Hediff hediff = p?.health?.hediffSet?.hediffs?.Find(h => h.def == TattooMagicDefOf.TattooMagic_Hediff_Phoenix);
            return (hediff as HediffWithComps)?.TryGetComp<HediffComp_PhoenixEffect>();
        }

        [DebugAction("TattooMagic", "TEST: Phoenix - kill pawn now", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void KillPawnNow(Pawn p)
        {
            if (GetPhoenixComp(p) == null)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no Phoenix tattoo.");
                return;
            }

            Log.Message($"[TattooMagic TEST] Killing {p.LabelShort} now (Phoenix should schedule a revival attempt on the next registry sweep).");
            p.Kill(null);
        }

        [DebugAction("TattooMagic", "TEST: Phoenix - force registry sweep now", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceRegistrySweepNow()
        {
            GameComponent_PhoenixRegistry registry = GameComponent_PhoenixRegistry.Get();
            if (registry == null)
            {
                Log.Message("[TattooMagic TEST] No GameComponent_PhoenixRegistry found on the current game.");
                return;
            }

            Log.Message($"[TattooMagic TEST] Forcing a Phoenix registry sweep now ({registry.registeredWearers.Count} registered wearer(s)).");
            registry.ForceSweepNow();
        }

        [DebugAction("TattooMagic", "TEST: Phoenix - set corpse rot progress (days)", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetCorpseRotProgress()
        {
            Corpse corpse = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Corpse>().FirstOrDefault();
            if (corpse == null)
            {
                Log.Message("[TattooMagic TEST] No corpse under the cursor.");
                return;
            }

            CompRottable rottable = corpse.TryGetComp<CompRottable>();
            if (rottable == null)
            {
                Log.Message($"[TattooMagic TEST] {corpse.LabelShort} has no CompRottable.");
                return;
            }

            Find.WindowStack.Add(new Dialog_Slider(
                days => $"Set rot progress to {days} day(s)",
                0,
                20,
                days => rottable.RotProgress = days * GenDate.TicksPerDay));
        }

        [DebugAction("TattooMagic", "TEST: Phoenix - force pending attempt to resolve now", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForcePendingAttemptNow()
        {
            Corpse corpse = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Corpse>().FirstOrDefault();
            Pawn pawn = corpse?.InnerPawn;
            if (pawn == null)
            {
                Log.Message("[TattooMagic TEST] No corpse under the cursor.");
                return;
            }

            HediffComp_PhoenixEffect comp = GetPhoenixComp(pawn);
            if (comp == null)
            {
                Log.Message($"[TattooMagic TEST] {pawn.LabelShort} has no Phoenix tattoo.");
                return;
            }

            if (comp.reviveAttemptTick == 0)
            {
                Log.Message($"[TattooMagic TEST] {pawn.LabelShort} has no pending revival attempt scheduled — force a registry sweep first.");
                return;
            }

            comp.reviveAttemptTick = Find.TickManager.TicksGame;
            bool resurrected = PhoenixRevivalUtility.TryResolveAttempt(pawn, comp);
            Log.Message($"[TattooMagic TEST] Forced Phoenix revival attempt on {pawn.LabelShort}: RESULT: {(resurrected ? "SUCCEEDED" : "FAILED")}.");
        }

        // Unlike ForcePendingAttemptNow (which needs a live Corpse under the
        // cursor to find its target), this works for a wearer who no longer
        // has a Corpse at all — a cremated pawn, most notably, where the
        // Corpse Thing itself was destroyed as part of the guarantee. Reads
        // the registry directly instead.
        [DebugAction("TattooMagic", "TEST: Phoenix - force ALL pending attempts to resolve now", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceAllPendingAttemptsNow()
        {
            GameComponent_PhoenixRegistry registry = GameComponent_PhoenixRegistry.Get();
            if (registry == null)
            {
                Log.Message("[TattooMagic TEST] No GameComponent_PhoenixRegistry found on the current game.");
                return;
            }

            int resolvedCount = 0;
            foreach (Pawn pawn in registry.registeredWearers.ToList())
            {
                if (pawn == null || !pawn.Dead)
                    continue;

                HediffComp_PhoenixEffect comp = GetPhoenixComp(pawn);
                if (comp == null || comp.reviveAttemptTick == 0)
                    continue;

                bool wasCremationGuaranteed = comp.cremationGuaranteed;
                comp.reviveAttemptTick = Find.TickManager.TicksGame;
                bool resurrected = PhoenixRevivalUtility.TryResolveAttempt(pawn, comp);
                resolvedCount++;

                Log.Message($"[TattooMagic TEST] Forced Phoenix revival attempt on {pawn.LabelShort} " +
                    $"(cremationGuaranteed was {wasCremationGuaranteed}): RESULT: {(resurrected ? "SUCCEEDED" : "FAILED")}.");
            }

            if (resolvedCount == 0)
                Log.Message("[TattooMagic TEST] No dead wearer with a pending revival attempt found in the registry.");
        }

        [DebugAction("TattooMagic", "TEST: Phoenix - set days-alive counter", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetDaysAliveCounter(Pawn p)
        {
            HediffComp_PhoenixEffect comp = GetPhoenixComp(p);
            if (comp == null)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no Phoenix tattoo.");
                return;
            }

            Find.WindowStack.Add(new Dialog_Slider(
                value => $"Set {p.LabelShort}'s days-alive counter to {value}",
                0,
                60,
                value =>
                {
                    comp.DevSetDaysAliveCounter(value);
                    Log.Message($"[TattooMagic TEST] {p.LabelShort}'s Phoenix days-alive counter set to {value} (tier now {comp.tier}).");
                },
                comp.daysAliveCounter));
        }

        [DebugAction("TattooMagic", "TEST: Phoenix - set lifetime attempt counter", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetLifetimeAttemptCounter(Pawn p)
        {
            HediffComp_PhoenixEffect comp = GetPhoenixComp(p);
            if (comp == null)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no Phoenix tattoo.");
                return;
            }

            Find.WindowStack.Add(new Dialog_Slider(
                value => $"Set {p.LabelShort}'s lifetime revival-attempt counter to {value}",
                0,
                10,
                value =>
                {
                    comp.revivalAttemptCount = value;
                    Log.Message($"[TattooMagic TEST] {p.LabelShort}'s Phoenix lifetime attempt counter set to {value}.");
                },
                comp.revivalAttemptCount));
        }
    }
}
