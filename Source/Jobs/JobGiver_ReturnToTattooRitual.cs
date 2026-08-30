using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    // Keeps a queued tattoo recipient heading back to the ritual station if
    // their WaitForTattooRitual job gets interrupted by something that isn't
    // a genuine emergency — a routine need, a brief wander, or another mod
    // issuing a job (Privacy Please!'s automatic bed-claim was the first
    // reported case). Injected at the Humanlike_PreMain think-tree hook
    // (Defs/ThinkTreeDefs/TattooRitual.xml): above routine work / joy /
    // idle and non-urgent needs, below emergency work, starvation, mental
    // states, drafted orders and lord duties.
    //
    // Gated entirely on pendingRitualTattoo, so the recipient's
    // Cancel-ritual gizmo (Patch_Pawn_GetGizmos_TattooGizmos) doubles as
    // this giver's off-switch — clear the flag and the recipient stops
    // being sent back.
    public class JobGiver_ReturnToTattooRitual : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            // Already walking to / waiting at the station — leave them be.
            if (pawn.CurJobDef == TattooMagicDefOf.TattooMagic_WaitForTattooRitual)
                return null;

            HediffComp_TattooTracker tracker = TattooTrackerUtility.GetTracker(pawn);
            if (tracker?.pendingRitualTattoo == null)
                return null;

            Building_TattooRitualStation station = FindReachableStation(pawn);
            if (station == null)
                return null;

            return JobMaker.MakeJob(TattooMagicDefOf.TattooMagic_WaitForTattooRitual, station);
        }

        private static Building_TattooRitualStation FindReachableStation(Pawn pawn)
        {
            if (pawn.Map == null)
                return null;

            List<Thing> stations = pawn.Map.listerThings.ThingsOfDef(TattooMagicDefOf.TattooMagic_RitualStation);
            if (stations.Count == 0)
                return null;

            return GenClosest.ClosestThing_Global_Reachable(
                pawn.Position, pawn.Map, stations, PathEndMode.Touch,
                TraverseParms.For(pawn), 9999f,
                t => !t.IsForbidden(pawn)) as Building_TattooRitualStation;
        }
    }
}
