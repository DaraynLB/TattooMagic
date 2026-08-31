using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    public class JobDriver_TattooRitual : JobDriver
    {
        private const TargetIndex StationInd = TargetIndex.A;
        private const TargetIndex RecipientInd = TargetIndex.B;

        // How long the performer will stand at the station waiting for the
        // recipient to first arrive (~2 in-game hours — generous enough for
        // a long walk across a big base, or a mod-driven detour, without
        // pinning the performer forever), and — separately — how long the
        // recipient may be pulled away from the spot mid-ritual before the
        // ritual is abandoned (~1 hour).
        private const int RecipientArrivalTimeoutTicks = 5000;
        private const int RecipientAbsenceGraceTicks = 2500;

        private Building_TattooRitualStation Station => (Building_TattooRitualStation)job.GetTarget(StationInd).Thing;
        private Pawn Recipient => (Pawn)job.GetTarget(RecipientInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(StationInd), job, errorOnFailed: errorOnFailed)
                && pawn.Reserve(job.GetTarget(RecipientInd), job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(StationInd);
            this.FailOnDespawnedOrNull(RecipientInd);
            this.FailOn(() => TattooTrackerUtility.GetTracker(Recipient)?.pendingRitualTattoo == null);
            this.FailOn(() => Recipient.Dead || Recipient.Downed);

            yield return Toils_Goto.GotoThing(StationInd, PathEndMode.InteractionCell);

            // Wait for the recipient to actually reach the ritual spot before
            // starting. The recipient's own WaitForTattooRitual job walks
            // them there, but nothing guarantees they arrive before the
            // performer does — and anything that delays or briefly interrupts
            // their walk (Privacy Please!'s automatic bed-claim / bedroom
            // avoidance being the first reported case, but a need or a mental
            // state does it too) makes the performer reliably win that race.
            // Without this step the work toil below would fail on its very
            // first tick. Times out so a recipient who genuinely can't get
            // there doesn't pin the performer forever.
            Toil waitForRecipient = ToilMaker.MakeToil();
            int giveUpTick = 0;
            waitForRecipient.initAction = () => giveUpTick = Find.TickManager.TicksGame + RecipientArrivalTimeoutTicks;
            waitForRecipient.tickAction = () =>
            {
                if (Recipient.Position == Station.RecipientSpotCell)
                {
                    ReadyForNextToil();
                    return;
                }

                if (Find.TickManager.TicksGame >= giveUpTick)
                    EndJobWith(JobCondition.Incompletable);
            };
            waitForRecipient.defaultCompleteMode = ToilCompleteMode.Never;
            waitForRecipient.handlingFacing = true;
            yield return waitForRecipient;

            Toil consumeIngredients = ToilMaker.MakeToil();
            consumeIngredients.initAction = () =>
            {
                TattooMagicDef tattoo = TattooTrackerUtility.GetTracker(Recipient)?.pendingRitualTattoo;
                if (tattoo == null || !TattooRitualIngredientUtility.TryConsumeIngredients(Map, tattoo))
                    EndJobWith(JobCondition.Incompletable);
            };
            consumeIngredients.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return consumeIngredients;

            int workTarget = WorkTicks();
            int worked = 0;
            int awaySinceTick = -1;

            Toil work = ToilMaker.MakeToil();
            work.initAction = () =>
            {
                worked = 0;
                awaySinceTick = -1;
            };
            work.tickAction = () =>
            {
                if (Recipient.Position != Station.RecipientSpotCell)
                {
                    // Recipient briefly pulled off the spot. Pause progress
                    // and give them a window to walk back rather than
                    // instant-failing the whole ritual —
                    // JobDriver_WaitForTattooRitual re-paths them there while
                    // pendingRitualTattoo is still set.
                    if (awaySinceTick < 0)
                        awaySinceTick = Find.TickManager.TicksGame;
                    else if (Find.TickManager.TicksGame - awaySinceTick >= RecipientAbsenceGraceTicks)
                        EndJobWith(JobCondition.Incompletable);
                    return;
                }

                awaySinceTick = -1;
                worked++;
                if (worked >= workTarget)
                    ReadyForNextToil();
            };
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.handlingFacing = true;
            work.FailOnCannotTouch(StationInd, PathEndMode.InteractionCell);
            work.WithProgressBar(StationInd, () => workTarget > 0 ? (float)worked / workTarget : 1f);
            yield return work;

            Toil resolve = ToilMaker.MakeToil();
            resolve.initAction = () => ResolveRitual();
            resolve.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return resolve;
        }

        private int WorkTicks()
        {
            TattooMagicDef tattoo = TattooTrackerUtility.GetTracker(Recipient)?.pendingRitualTattoo;
            return tattoo != null ? Mathf.RoundToInt(tattoo.workAmount) : 600;
        }

        private void ResolveRitual()
        {
            HediffComp_TattooTracker tracker = TattooTrackerUtility.GetTracker(Recipient);
            TattooMagicDef tattoo = tracker?.pendingRitualTattoo;
            if (tracker == null || tattoo == null)
                return;

            // Clearing this now, before the roll, is what lets
            // JobDriver_WaitForTattooRitual's FailOn release the recipient
            // regardless of which branch below runs.
            tracker.pendingRitualTattoo = null;

            // Defensive re-check (playtesting finding, feature 006):
            // Dialog_ChooseTattoo already filters out a tattoo the recipient
            // is tracked as having, but that filter only runs while the
            // dialog is open. Re-verify against the recipient's actual
            // hediffs here too, so nothing between queuing and resolving
            // this ritual (a Dev Mode hediff grant, a save import) can ever
            // result in two live comps for what's supposed to be one
            // tattoo — ingredients are already spent either way (same as
            // the mishap branch above), but no duplicate Hediff is granted.
            tracker.SyncAppliedTattoosFromActualHediffs();
            if (tracker.HasTattoo(tattoo))
            {
                Log.Warning($"[TattooMagic] Skipped granting duplicate {tattoo.label} tattoo to {Recipient.LabelShortCap} — they already have it.");
                return;
            }

            float performerSkill = pawn.skills.GetSkill(SkillDefOf.Artistic).Level;
            float successChance = TattooRitualSuccessCurve.GetSuccessChance(tattoo, performerSkill);

            if (!Rand.Chance(successChance))
            {
                // Mishap (FR-005/FR-008): ingredients already consumed in
                // the prior toil are lost; no Hediff is granted and the
                // recipient's slot/appliedTattoos stay untouched.
                Messages.Message(
                    $"{pawn.LabelShortCap}'s tattoo ritual on {Recipient.LabelShortCap} failed. The {tattoo.label} tattoo was not applied, and the ritual ingredients were wasted.",
                    Recipient,
                    MessageTypeDefOf.NegativeEvent);
                return;
            }

            Hediff hediff = HediffMaker.MakeHediff(tattoo.appliedHediff, Recipient);
            Recipient.health.AddHediff(hediff);
            tracker.appliedTattoos.Add(tattoo);

            Messages.Message(
                $"{pawn.LabelShortCap}'s tattoo ritual on {Recipient.LabelShortCap} succeeded! They now have the {tattoo.label} tattoo.",
                Recipient,
                MessageTypeDefOf.PositiveEvent);
        }
    }
}
