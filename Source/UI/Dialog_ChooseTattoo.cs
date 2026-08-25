using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    public class Dialog_ChooseTattoo : Window
    {
        private readonly Building_TattooRitualStation station;
        private Pawn selectedRecipient;
        private Pawn selectedPerformer;
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(420f, 560f);

        public Dialog_ChooseTattoo(Building_TattooRitualStation station)
        {
            this.station = station;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            const float rowHeight = 30f;
            int rowCount = station.Map.mapPawns.FreeColonistsSpawned.Count + 2;
            if (selectedRecipient != null)
                rowCount += station.Map.mapPawns.FreeColonistsSpawned.Count + 4;
            if (selectedRecipient != null && Prefs.DevMode)
                rowCount += 1;
            if (selectedRecipient != null && selectedPerformer != null)
                rowCount += DefDatabase<TattooMagicDef>.AllDefsListForReading.Count + 1;

            // Fetched once per frame and self-healed here (rather than at
            // each of this method's several separate lookups) so every
            // check below — the slots-used label, the free-slot gate, and
            // duplicate-tattoo prevention — agrees with the pawn's actual
            // hediffs even if one was granted outside the ritual flow (Dev
            // Mode's "give hediff", a save imported from elsewhere).
            HediffComp_TattooTracker recipientTracker = null;
            if (selectedRecipient != null)
            {
                recipientTracker = TattooTrackerUtility.GetTracker(selectedRecipient);
                recipientTracker?.SyncAppliedTattoosFromActualHediffs();
            }

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, rowCount * rowHeight);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.Label("Choose recipient:");
            foreach (Pawn colonist in station.Map.mapPawns.FreeColonistsSpawned)
            {
                if (listing.RadioButton(colonist.LabelShortCap, selectedRecipient == colonist))
                {
                    selectedRecipient = colonist;
                    if (selectedPerformer == selectedRecipient)
                        selectedPerformer = null;
                }
            }

            listing.GapLine();

            if (selectedRecipient != null && recipientTracker != null)
                listing.Label($"{selectedRecipient.LabelShortCap}: {recipientTracker.appliedTattoos.Count}/{recipientTracker.slotCapacity} tattoo slots used");

            // Dev-only test aid, replacing the now-obsolete "[DEV] Grant +1
            // tattoo slot" button now that real slot progression exists
            // (research.md R5): jumps mastery progress directly so a tester
            // can reach any slot-capacity state, including a single jump
            // across both thresholds at once, without waiting out cooldowns
            // (research.md R9).
            if (selectedRecipient != null && Prefs.DevMode && recipientTracker != null)
            {
                // Must be >= slot3Threshold (8) so a single click from a
                // fresh pawn (progress 0) can cross both slot2Threshold (3)
                // and slot3Threshold (8) in the same CheckSlotThresholds()
                // call — that single-jump multi-crossing case (research.md
                // R4/R9, quickstart.md Scenario 8) is what this button
                // exists to make testable; a smaller amount could only ever
                // cross one threshold per click.
                const int devProgressAmount = 10;
                if (listing.ButtonText($"[DEV] +{devProgressAmount} tattoo mastery progress for {selectedRecipient.LabelShortCap} (currently {recipientTracker.masteryProgress} progress, {recipientTracker.slotCapacity} slots)"))
                    recipientTracker.DevAddMasteryProgress(devProgressAmount);
            }

            if (selectedRecipient != null)
            {
                listing.Label("Choose performer (distinct from recipient):");
                WorkTypeDef artWorkType = DefDatabase<WorkTypeDef>.GetNamed("Art");
                int incapableCount = 0;
                foreach (Pawn colonist in station.Map.mapPawns.FreeColonistsSpawned)
                {
                    if (colonist == selectedRecipient)
                        continue;

                    // A pawn incapable of Artistic work (backstory/trait)
                    // can never be assigned Art-type jobs through the
                    // normal work system — since this dialog force-assigns
                    // the performer directly, that check has to be done
                    // here explicitly, or it'd otherwise be silently
                    // bypassed.
                    if (colonist.WorkTypeIsDisabled(artWorkType))
                    {
                        incapableCount++;
                        continue;
                    }

                    if (listing.RadioButton(colonist.LabelShortCap, selectedPerformer == colonist))
                        selectedPerformer = colonist;
                }

                if (incapableCount > 0)
                    listing.Label($"({incapableCount} colonist(s) not shown — incapable of Art work.)");

                listing.GapLine();
            }

            if (selectedRecipient != null && selectedPerformer != null)
            {
                // FR-009: block the ritual from starting (here: from even
                // being offered) when the recipient has no free slot.
                if (recipientTracker == null || recipientTracker.FreeSlots <= 0)
                {
                    listing.Label($"{selectedRecipient.LabelShortCap} has no free tattoo slots.");
                }
                else
                {
                    listing.Label("Choose tattoo:");
                    foreach (TattooMagicDef tattoo in DefDatabase<TattooMagicDef>.AllDefsListForReading)
                    {
                        // FR-010: a pawn who already has this tattoo can't
                        // select it again.
                        if (recipientTracker.HasTattoo(tattoo))
                            continue;

                        if (listing.ButtonText(tattoo.label))
                            TryQueueRitual(recipientTracker, tattoo);
                    }
                }
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private void TryQueueRitual(HediffComp_TattooTracker tracker, TattooMagicDef tattoo)
        {
            if (tracker.FreeSlots <= 0 || tracker.HasTattoo(tattoo))
                return;

            tracker.pendingRitualTattoo = tattoo;

            Job waitJob = JobMaker.MakeJob(TattooMagicDefOf.TattooMagic_WaitForTattooRitual, station);
            selectedRecipient.jobs.StartJob(waitJob, JobCondition.InterruptForced);

            // Undraft the chosen performer if needed — a drafted pawn can't
            // be handed a normal work job (this is standard RimWorld
            // behavior, not specific to this mod), and forcing the player
            // to manually undraft before picking a performer here would
            // defeat the point of choosing one directly.
            if (selectedPerformer.Drafted)
                selectedPerformer.drafter.Drafted = false;

            Job ritualJob = JobMaker.MakeJob(TattooMagicDefOf.TattooMagic_TattooRitual, station, selectedRecipient);
            selectedPerformer.jobs.StartJob(ritualJob, JobCondition.InterruptForced);

            Close();
        }
    }
}
