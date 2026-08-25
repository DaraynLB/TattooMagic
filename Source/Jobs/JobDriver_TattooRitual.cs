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

            yield return Toils_Goto.GotoThing(StationInd, PathEndMode.InteractionCell);

            Toil consumeIngredients = ToilMaker.MakeToil();
            consumeIngredients.initAction = () =>
            {
                TattooMagicDef tattoo = TattooTrackerUtility.GetTracker(Recipient)?.pendingRitualTattoo;
                if (tattoo == null || !TattooRitualIngredientUtility.TryConsumeIngredients(Map, tattoo))
                    EndJobWith(JobCondition.Incompletable);
            };
            consumeIngredients.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return consumeIngredients;

            Toil work = Toils_General.Wait(WorkTicks());
            work.WithProgressBarToilDelay(StationInd);
            work.FailOnCannotTouch(StationInd, PathEndMode.InteractionCell);
            work.FailOn(() => Recipient.Position != Station.RecipientSpotCell);
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
