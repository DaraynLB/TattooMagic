using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    // Generic, reusable StatPart: sums IProvidesTattooStatOffset across every
    // hediff comp on a pawn implementing it, for whichever StatDef this
    // instance is registered on (see TattooEffectStatPartInstaller). Carries
    // no Frost-Sigil-specific (or any tattoo-specific) knowledge.
    public class StatPart_TattooEffectOffset : StatPart
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

            return "Tattoo effects: " + GenText.ToStringByStyle(offset, parentStat.toStringStyle, ToStringNumberSense.Offset);
        }

        private float GetOffset(StatRequest req)
        {
            if (!(req.Thing is Pawn pawn) || pawn.health?.hediffSet == null)
                return 0f;

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            bool negativeOffsetsBlocked = false;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is IGrantsStatOffsetImmunity immunity && immunity.BlocksNegativeOffsets(parentStat))
                    {
                        negativeOffsetsBlocked = true;
                        break;
                    }
                }

                if (negativeOffsetsBlocked)
                    break;
            }

            float total = 0f;
            float rawTotal = 0f;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is IProvidesTattooStatOffset provider)
                    {
                        float offset = provider.GetStatOffset(parentStat);
                        rawTotal += offset;
                        total += negativeOffsetsBlocked ? Mathf.Max(0f, offset) : offset;
                    }
                }
            }

            // Dev-Mode-only, always-on visibility into immunity clamping
            // (contracts/stat-offset-immunity-contract.md §2, feature 005) —
            // fires only when a veto is active and actually changed the
            // outcome, so it stays silent for normal players and for every
            // stat query where nothing is being blocked.
            if (TattooMagicSettings.EnableDebugLogging && negativeOffsetsBlocked && total != rawTotal)
            {
                Log.Message($"[TattooMagic DEBUG] StatPart_TattooEffectOffset: blocked negative offset(s) for " +
                    $"{parentStat.defName} on {(req.Thing as Pawn)?.LabelShort}: raw sum would have been " +
                    $"{rawTotal:F2}, clamped total is {total:F2}.");
            }

            return total;
        }
    }
}
