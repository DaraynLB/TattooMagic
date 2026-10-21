using RimWorld;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    // MedicalTendSpeed is queried on the DOCTOR (JobDriver_TendPatient.
    // MakeNewToils's `pawn.GetStatValue(StatDefOf.MedicalTendSpeed)`,
    // decompiled — research.md R5 of specs/017-soul-tether-tattoo-effect),
    // with no patient-aware overload to postfix the way tend quality has
    // (Patch_TendUtility_CalculateBaseTendQuality_SoulTetherBuff). This
    // StatPart instead looks up the doctor's own current job to find which
    // patient (if any) is being tended right now, and applies that
    // patient's active Soul Tether buff to the doctor's stat for the
    // duration of that lookup — a deliberately separate class from the
    // existing, generic StatPart_TattooEffectOffset (feature 002), whose
    // IProvidesTattooStatOffset contract only ever inspects req.Thing's own
    // hediffs, not a different, job-related pawn's.
    public class StatPart_SoulTetherTendSpeedBuff : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            val += GetOffset(req);
        }

        public override string ExplanationPart(StatRequest req)
        {
            float offset = GetOffset(req);
            if (offset == 0f)
                return null;

            return "Soul Tether: " + GenText.ToStringByStyle(offset, parentStat.toStringStyle, ToStringNumberSense.Offset);
        }

        private float GetOffset(StatRequest req)
        {
            if (!(req.Thing is Pawn doctor))
                return 0f;

            Job job = doctor.CurJob;
            if (job == null || job.def != JobDefOf.TendPatient)
                return 0f;

            Pawn patient = job.GetTarget(TargetIndex.A).Pawn;
            if (patient == null)
                return 0f;

            return SoulTetherTendBuffUtility.GetActiveSpeedBonus(patient);
        }
    }
}
