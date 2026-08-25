using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    // Read-only, per-pawn view of the tattoos actually applied to this pawn
    // right now — deliberately scoped to display only (no removal/reassignment
    // controls; the ritual station's Dialog_ChooseTattoo already owns applying
    // new ones).
    public class ITab_Pawn_Tattoos : ITab
    {
        private Vector2 scrollPosition;

        public ITab_Pawn_Tattoos()
        {
            size = new Vector2(400f, 400f);
            labelKey = "TattooMagic_TabTattoos";
        }

        public override bool IsVisible => SelPawn?.RaceProps != null && SelPawn.RaceProps.Humanlike;

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

            HediffComp_TattooTracker tracker = TattooTrackerUtility.GetTracker(SelPawn);
            tracker?.SyncAppliedTattoosFromActualHediffs();

            if (tracker == null || tracker.appliedTattoos.Count == 0)
            {
                Widgets.Label(rect, "No tattoos applied.");
                return;
            }

            // Scrolled, not a fixed-height Listing_Standard: a pawn with
            // several tiered/modifier-bearing tattoos applied can need more
            // vertical space than the tab's fixed size — up to 3 slots, each
            // with an icon, label, tier-progress line, modifier line, and a
            // wrapped description, easily exceeds it (playtesting finding).
            float viewWidth = rect.width - 16f;

            float totalHeight = 0f;
            for (int i = 0; i < tracker.appliedTattoos.Count; i++)
            {
                TattooMagicDef tattoo = tracker.appliedTattoos[i];
                string tierText = TattooTierProgressUtility.DescribeTier(SelPawn, tattoo);
                totalHeight += TattooRowDrawer.MeasureHeight(viewWidth, tattoo, tierText) + 12f;
            }

            Rect viewRect = new Rect(0f, 0f, viewWidth, totalHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float y = 0f;
            for (int i = 0; i < tracker.appliedTattoos.Count; i++)
            {
                TattooMagicDef tattoo = tracker.appliedTattoos[i];

                // Null for a tattoo with no tier system of its own (e.g. a
                // flat passive with no dedicated effect comp yet) — that row
                // just skips the tier line entirely.
                string tierText = TattooTierProgressUtility.DescribeTier(SelPawn, tattoo);

                float rowHeight = TattooRowDrawer.MeasureHeight(viewWidth, tattoo, tierText);
                Rect rowRect = new Rect(0f, y, viewWidth, rowHeight);
                TattooRowDrawer.Draw(rowRect, tattoo, tierText);

                y += rowHeight + 6f;
                Widgets.DrawLineHorizontal(0f, y, viewWidth);
                y += 6f;
            }

            Widgets.EndScrollView();
        }
    }
}
