using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    // Read-only browser of every tattoo the ritual station can apply —
    // independent of any specific pawn, unlike ITab_Pawn_Tattoos (which only
    // shows what's already applied to the selected pawn). Deliberately
    // scoped to display only, same as that tab: no application/removal here
    // either — Dialog_ChooseTattoo already owns applying new ones.
    public class Dialog_TattooCodex : Window
    {
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(630f, 900f);

        public Dialog_TattooCodex()
        {
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            List<TattooMagicDef> allTattoos = DefDatabase<TattooMagicDef>.AllDefsListForReading;

            Rect headerRect = new Rect(0f, 0f, inRect.width, 32f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "Tattoo Codex");
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(0f, headerRect.height + 8f, inRect.width, inRect.height - headerRect.height - 8f);
            float viewWidth = outRect.width - 16f;

            float totalHeight = 0f;
            for (int i = 0; i < allTattoos.Count; i++)
                totalHeight += TattooRowDrawer.MeasureHeight(viewWidth, allTattoos[i], null) + 12f;

            Rect viewRect = new Rect(0f, 0f, viewWidth, totalHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float y = 0f;
            for (int i = 0; i < allTattoos.Count; i++)
            {
                TattooMagicDef tattoo = allTattoos[i];
                float rowHeight = TattooRowDrawer.MeasureHeight(viewWidth, tattoo, null);
                Rect rowRect = new Rect(0f, y, viewWidth, rowHeight);
                TattooRowDrawer.Draw(rowRect, tattoo, null);

                y += rowHeight + 6f;
                Widgets.DrawLineHorizontal(0f, y, viewWidth);
                y += 6f;
            }

            Widgets.EndScrollView();
        }
    }
}
