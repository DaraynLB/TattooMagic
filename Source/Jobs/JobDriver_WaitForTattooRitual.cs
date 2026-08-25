using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    // Assigned to the recipient (not the performer) so they walk to and
    // hold the station's recipient spot while a performer's
    // JobDriver_TattooRitual works. Ends automatically once
    // pendingRitualTattoo is cleared (ritual resolved or was cancelled).
    public class JobDriver_WaitForTattooRitual : JobDriver
    {
        private const TargetIndex StationInd = TargetIndex.A;

        private Building_TattooRitualStation Station => (Building_TattooRitualStation)job.GetTarget(StationInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Deliberately NOT reserving the station itself here: this pawn
            // only needs to read its position, not claim exclusive use of
            // it. Reserving it would block the performer's own
            // JobDriver_TattooRitual from ever reserving the same station
            // (RimWorld only allows one reservation per Thing by default),
            // making the ritual permanently unstartable — this was a real
            // bug found via testing, not a hypothetical.
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(StationInd);
            this.FailOn(() => TattooTrackerUtility.GetTracker(pawn)?.pendingRitualTattoo == null);

            Toil gotoSpot = ToilMaker.MakeToil();
            gotoSpot.initAction = () =>
            {
                pawn.pather.StartPath(Station.RecipientSpotCell, PathEndMode.OnCell);
            };
            gotoSpot.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return gotoSpot;

            Toil wait = Toils_General.Wait(99999);
            wait.handlingFacing = true;
            yield return wait;
        }
    }
}
