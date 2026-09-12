using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // This mod's first custom Alert subclass (research.md R9). A body part can never be simultaneously
    // missing and prosthetic-replaced (research.md R1), so there is no "blocked" state to persist anywhere —
    // this alert simply re-derives, fresh on every query, whether a Shedscale-tattooed pawn currently has an
    // installed replacement occupying a slot that would otherwise be eligible for natural regrowth.
    public class Alert_ShedscaleRegrowthBlocked : Alert
    {
        private List<Pawn> blockedPawnsResult = new List<Pawn>();

        private List<Pawn> BlockedPawns
        {
            get
            {
                blockedPawnsResult.Clear();

                foreach (Pawn p in PawnsFinder.AllMapsCaravansAndTravellingTransporters_AliveSpawned_FreeColonists_NoSuspended)
                {
                    HediffComp_ShedscaleEffect comp = FindComp(p);
                    if (comp == null)
                        continue;

                    if (HasBlockedEligiblePart(p, comp))
                        blockedPawnsResult.Add(p);
                }

                return blockedPawnsResult;
            }
        }

        public Alert_ShedscaleRegrowthBlocked()
        {
            // High (not Critical) — this is a discoverable, potentially long-lived informational state (an
            // installed prosthetic/bionic forgoing a regrowth), not an active danger, so it shouldn't pulse
            // red or spam a "ThreatBig" message the way Alert_Critical does. High matches vanilla's own
            // Alert_ReimplantationAvailable — a similar "a beneficial action is available" alert.
            defaultPriority = AlertPriority.High;
            defaultLabel = "AlertShedscaleRegrowthBlocked".Translate();
        }

        public override TaggedString GetExplanation()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Pawn item in blockedPawnsResult)
                stringBuilder.AppendLine("  - " + item.NameShortColored.Resolve());

            return "AlertShedscaleRegrowthBlockedDesc".Translate(stringBuilder.ToString().TrimEndNewlines());
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(BlockedPawns);
        }

        // A prosthetic/bionic arm's own Hediff_AddedPart attaches at the Shoulder joint, not the Arm itself
        // (confirmed via live testing — the health tab labels it "Shoulder | Bionic arm") — installing it
        // restores and covers everything below (Arm, Hand, Fingers) in one recipe application.
        // HediffComp_ShedscaleEffect.IsEligiblePart already walks a part's whole subtree for exactly this
        // reason (research.md R3 follow-up), so a direct call here correctly recognizes an arm/leg-level
        // replacement as blocking the eligible part(s) nested underneath it.
        private static bool HasBlockedEligiblePart(Pawn p, HediffComp_ShedscaleEffect comp)
        {
            List<Hediff> hediffs = p.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_AddedPart addedPart && comp.IsEligiblePart(addedPart.Part))
                    return true;
            }

            return false;
        }

        private static HediffComp_ShedscaleEffect FindComp(Pawn p)
        {
            if (p?.health?.hediffSet?.hediffs == null)
                return null;

            List<Hediff> hediffs = p.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is HediffComp_ShedscaleEffect shedscale)
                        return shedscale;
                }
            }

            return null;
        }
    }
}
