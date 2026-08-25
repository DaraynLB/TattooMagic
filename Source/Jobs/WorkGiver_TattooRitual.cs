using RimWorld;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    public class WorkGiver_TattooRitual : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForDef(TattooMagicDefOf.TattooMagic_RitualStation);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building_TattooRitualStation station))
                return false;

            if (station.IsForbidden(pawn))
            {
                if (TattooMagicSettings.EnableDebugLogging)
                    Log.Message($"[TattooMagic DEBUG] {pawn.LabelShort}: station is forbidden.");
                return false;
            }

            Pawn recipient = FindReadyRecipient(pawn, station, verbose: TattooMagicSettings.EnableDebugLogging);
            if (recipient == null)
                return false;

            if (!pawn.CanReserve(station))
            {
                if (TattooMagicSettings.EnableDebugLogging)
                    Log.Message($"[TattooMagic DEBUG] {pawn.LabelShort}: cannot reserve the station (someone else has it, or it's unreachable).");
                return false;
            }

            if (!pawn.CanReserve(recipient))
            {
                if (TattooMagicSettings.EnableDebugLogging)
                    Log.Message($"[TattooMagic DEBUG] {pawn.LabelShort}: cannot reserve {recipient.LabelShort} (someone else has them reserved).");
                return false;
            }

            TattooMagicDef tattoo = TattooTrackerUtility.GetTracker(recipient)?.pendingRitualTattoo;
            if (tattoo == null)
            {
                if (TattooMagicSettings.EnableDebugLogging)
                    Log.Message($"[TattooMagic DEBUG] {pawn.LabelShort}: {recipient.LabelShort} has no pending tattoo (shouldn't happen here).");
                return false;
            }

            bool ingredientsOk = TattooRitualIngredientUtility.HasIngredientsAvailable(pawn.Map, tattoo, verbose: TattooMagicSettings.EnableDebugLogging);
            if (TattooMagicSettings.EnableDebugLogging)
            {
                if (!ingredientsOk)
                    Log.Message($"[TattooMagic DEBUG] {pawn.LabelShort}: not enough ingredients for {tattoo.defName} (see counts above).");
                else
                    Log.Message($"[TattooMagic DEBUG] {pawn.LabelShort}: ALL CHECKS PASSED for ritual on {recipient.LabelShort} ({tattoo.defName}).");
            }

            return ingredientsOk;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var station = (Building_TattooRitualStation)t;
            Pawn recipient = FindReadyRecipient(pawn, station, verbose: false);
            if (recipient == null)
                return null;

            return JobMaker.MakeJob(TattooMagicDefOf.TattooMagic_TattooRitual, station, recipient);
        }

        private static Pawn FindReadyRecipient(Pawn performer, Building_TattooRitualStation station, bool verbose)
        {
            bool sawAnyCandidate = false;

            foreach (Pawn pawn in performer.Map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn == performer)
                    continue;

                HediffComp_TattooTracker tracker = TattooTrackerUtility.GetTracker(pawn);
                if (tracker?.pendingRitualTattoo == null)
                    continue;

                sawAnyCandidate = true;

                // Defensive re-checks (FR-009/FR-010): Dialog_ChooseTattoo
                // already refuses to queue an invalid request, but slot
                // capacity/applied tattoos could change while the recipient
                // is still walking to the station.
                if (tracker.FreeSlots <= 0 || tracker.HasTattoo(tracker.pendingRitualTattoo))
                {
                    if (verbose)
                        Log.Message($"[TattooMagic DEBUG] {pawn.LabelShort} has a pending tattoo but FreeSlots={tracker.FreeSlots} or already has it — clearing pending request.");
                    tracker.pendingRitualTattoo = null;
                    continue;
                }

                if (pawn.Position == station.RecipientSpotCell)
                    return pawn;

                if (verbose)
                    Log.Message($"[TattooMagic DEBUG] {pawn.LabelShort} has a pending tattoo but is at {pawn.Position}, not the recipient spot {station.RecipientSpotCell}.");
            }

            if (verbose && !sawAnyCandidate)
                Log.Message($"[TattooMagic DEBUG] {performer.LabelShort}: no free colonist has a pending tattoo at all.");

            return null;
        }
    }
}
