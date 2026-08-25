using UnityEngine;
using Verse;

namespace TattooMagic
{
    // Shared "heal the worst open wound, roll over the leftover" approach,
    // extracted from Bloodrune's (feature 007) own inline logic once a
    // second tattoo (Vampiric Thorn) needed the identical pattern (FR-014).
    public static class TattooHealingUtility
    {
        // Heals pawn's currently most-severe open, non-permanent
        // Hediff_Injury by up to amount; if amount remains after that
        // injury closes, rolls the leftover onto the next-most-severe
        // injury within the same call, repeating until amount is exhausted
        // or nothing is left to heal. Returns the amount actually applied.
        // No-ops and returns 0f for a pawn with no open injuries.
        public static float HealWorstInjury(Pawn pawn, float amount)
        {
            float remaining = amount;
            var hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null)
                return 0f;

            while (remaining > 0f)
            {
                Hediff_Injury worst = null;
                for (int i = 0; i < hediffs.Count; i++)
                {
                    // Severity > 0f excludes an injury this same call already
                    // closed but that hasn't been removed from hediffSet yet
                    // (removal happens on a later health tick, not
                    // synchronously inside Heal()) — without this guard, a
                    // fully-closed injury with no other open injuries left
                    // would keep getting re-selected as "worst" every
                    // iteration with a healAmount of 0, spinning forever.
                    if (hediffs[i] is Hediff_Injury injury && !injury.IsPermanent() && injury.Severity > 0f && (worst == null || injury.Severity > worst.Severity))
                        worst = injury;
                }

                if (worst == null)
                    break;

                float healAmount = Mathf.Min(remaining, worst.Severity);
                worst.Heal(healAmount);
                remaining -= healAmount;
            }

            return amount - remaining;
        }
    }
}
