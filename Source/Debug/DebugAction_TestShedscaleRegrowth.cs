using LudeonTK;
using Verse;

namespace TattooMagic
{
    // Dev Mode utility (not shipped-feature behavior): Shedscale's own core timers are genuinely real-world
    // days long (7/4-day regrowths, a days-long Tier 2 progression clock), impractical to wait out for real —
    // mirrors feature 013/014's own precedent of adding deterministic debug tooling for a mechanic too
    // slow/organic to reliably hand-test.
    public static class DebugAction_TestShedscaleRegrowth
    {
        private static HediffComp_ShedscaleEffect GetShedscaleComp(Pawn p)
        {
            Hediff hediff = p?.health?.hediffSet?.hediffs?.Find(h => h.def == TattooMagicDefOf.TattooMagic_Hediff_Shedscale);
            return (hediff as HediffWithComps)?.TryGetComp<HediffComp_ShedscaleEffect>();
        }

        [DebugAction("TattooMagic", "TEST: Shedscale - force daily pass now", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceDailyPassNow(Pawn p)
        {
            HediffComp_ShedscaleEffect comp = GetShedscaleComp(p);
            if (comp == null)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no Shedscale tattoo.");
                return;
            }

            comp.DevForceDailyPass();
            Log.Message($"[TattooMagic TEST] Forced a Shedscale daily pass on {p.LabelShort}: tier={comp.Tier}, daysWorn={comp.daysWornCounter}, partsRegrown={comp.partsRegrownCounter}, activeRegrowths={comp.ActiveRegrowthCount}.");
        }

        [DebugAction("TattooMagic", "TEST: Shedscale - complete all active regrowths now", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CompleteAllActiveRegrowthsNow(Pawn p)
        {
            HediffComp_ShedscaleEffect comp = GetShedscaleComp(p);
            if (comp == null)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no Shedscale tattoo.");
                return;
            }

            int count = comp.ActiveRegrowthCount;
            comp.DevCompleteAllActiveRegrowths();
            Log.Message($"[TattooMagic TEST] Completed {count} active Shedscale regrowth(s) on {p.LabelShort} immediately.");
        }

        [DebugAction("TattooMagic", "TEST: Shedscale - set days-worn counter", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetDaysWornCounter(Pawn p)
        {
            HediffComp_ShedscaleEffect comp = GetShedscaleComp(p);
            if (comp == null)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no Shedscale tattoo.");
                return;
            }

            Find.WindowStack.Add(new Dialog_Slider(
                value => $"Set {p.LabelShort}'s Shedscale days-worn counter to {value}",
                0,
                60,
                value =>
                {
                    comp.DevSetDaysWornCounter(value);
                    Log.Message($"[TattooMagic TEST] {p.LabelShort}'s Shedscale days-worn counter set to {value} (tier now {comp.Tier}).");
                },
                comp.daysWornCounter));
        }

        [DebugAction("TattooMagic", "TEST: Shedscale - set parts-regrown counter", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetPartsRegrownCounter(Pawn p)
        {
            HediffComp_ShedscaleEffect comp = GetShedscaleComp(p);
            if (comp == null)
            {
                Log.Message($"[TattooMagic TEST] {p.LabelShort} has no Shedscale tattoo.");
                return;
            }

            Find.WindowStack.Add(new Dialog_Slider(
                value => $"Set {p.LabelShort}'s Shedscale parts-regrown counter to {value}",
                0,
                10,
                value =>
                {
                    comp.DevSetPartsRegrownCounter(value);
                    Log.Message($"[TattooMagic TEST] {p.LabelShort}'s Shedscale parts-regrown counter set to {value} (tier now {comp.Tier}).");
                },
                comp.partsRegrownCounter));
        }
    }
}
