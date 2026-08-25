using UnityEngine;
using Verse;

namespace TattooMagic
{
    // Shared icon+label+description(+optional tier) row layout, reused by
    // ITab_Pawn_Tattoos (only the tattoos actually applied to the selected
    // pawn) and Dialog_TattooCodex (every tattoo in the game, browsed
    // independent of any pawn) — kept in one place so a future layout tweak
    // doesn't have to be made twice.
    public static class TattooRowDrawer
    {
        // 60% bigger than the original 48f icon.
        public const float IconSize = 48f * 1.6f;

        private const float TextGap = 8f;

        // Distinguishes the modifier line from plain description text —
        // a light "buff" green, same intent as RimWorld's own convention for
        // positive stat text.
        private static readonly Color ModifierColor = new Color(0.7f, 0.9f, 0.7f);

        // Custom-sized styles (25% bigger title, 100% bigger description)
        // built lazily off the current GameFont.Small/Tiny styles — Verse's
        // GameFont enum only has four fixed sizes, none of which land on
        // these ratios, so Widgets.Label's ambient Text.Font can't produce
        // them; a scaled copy of the real style plus a direct GUI.Label call
        // is the standard way around that.
        private static GUIStyle titleStyle;
        private static GUIStyle descriptionStyle;

        private static GUIStyle TitleStyle
        {
            get
            {
                if (titleStyle == null)
                {
                    Text.Font = GameFont.Small;
                    GUIStyle baseStyle = Text.CurFontStyle;
                    titleStyle = new GUIStyle(baseStyle) { fontSize = Mathf.RoundToInt(baseStyle.fontSize * 1.25f) };
                }
                return titleStyle;
            }
        }

        private static GUIStyle DescriptionStyle
        {
            get
            {
                if (descriptionStyle == null)
                {
                    Text.Font = GameFont.Tiny;
                    GUIStyle baseStyle = Text.CurFontStyle;
                    descriptionStyle = new GUIStyle(baseStyle) { fontSize = baseStyle.fontSize * 2 };
                }
                return descriptionStyle;
            }
        }

        // tierText may be null/empty — that tattoo's row just omits the line
        // (e.g. a flat passive with no tier system, or the codex browsing
        // every tattoo with no pawn to read progress from).
        public static float MeasureHeight(float rowWidth, TattooMagicDef tattoo, string tierText)
        {
            float textWidth = rowWidth - IconSize - TextGap;
            string modifierText = ModifierText(tattoo);

            float labelHeight = TitleStyle.CalcHeight(new GUIContent(tattoo.LabelCap), textWidth);

            Text.Font = GameFont.Tiny;
            float tierHeight = tierText.NullOrEmpty() ? 0f : Text.CalcHeight(tierText, textWidth);
            float modifierHeight = modifierText.NullOrEmpty() ? 0f : Text.CalcHeight(modifierText, textWidth);
            Text.Font = GameFont.Small;

            float descHeight = DescriptionStyle.CalcHeight(new GUIContent(tattoo.description), textWidth);

            return Mathf.Max(IconSize, labelHeight + tierHeight + modifierHeight + descHeight);
        }

        public static void Draw(Rect rowRect, TattooMagicDef tattoo, string tierText)
        {
            float textWidth = rowRect.width - IconSize - TextGap;
            string modifierText = ModifierText(tattoo);

            Rect iconRect = new Rect(rowRect.x, rowRect.y, IconSize, IconSize);
            Widgets.DrawTextureFitted(iconRect, tattoo.Icon, 1f);

            Rect textRect = new Rect(iconRect.xMax + TextGap, rowRect.y, textWidth, rowRect.height);

            float labelHeight = TitleStyle.CalcHeight(new GUIContent(tattoo.LabelCap), textWidth);
            GUI.Label(new Rect(textRect.x, textRect.y, textRect.width, labelHeight), tattoo.LabelCap, TitleStyle);

            float y = textRect.y + labelHeight;
            if (!tierText.NullOrEmpty())
            {
                Text.Font = GameFont.Tiny;
                float tierHeight = Text.CalcHeight(tierText, textWidth);
                GUI.color = Color.grey;
                Widgets.Label(new Rect(textRect.x, y, textRect.width, tierHeight), tierText);
                GUI.color = Color.white;
                y += tierHeight;
                Text.Font = GameFont.Small;
            }

            if (!modifierText.NullOrEmpty())
            {
                Text.Font = GameFont.Tiny;
                float modifierHeight = Text.CalcHeight(modifierText, textWidth);
                GUI.color = ModifierColor;
                Widgets.Label(new Rect(textRect.x, y, textRect.width, modifierHeight), modifierText);
                GUI.color = Color.white;
                y += modifierHeight;
                Text.Font = GameFont.Small;
            }

            float descHeight = DescriptionStyle.CalcHeight(new GUIContent(tattoo.description), textWidth);
            GUI.Label(new Rect(textRect.x, y, textRect.width, descHeight), tattoo.description, DescriptionStyle);
        }

        // Only passive tattoos get a modifier line (per design: a triggered
        // tattoo's "modifier" is its gizmo behavior/cooldown, not a flat
        // stat, and is already covered by its description). Null for a
        // passive with no effect comp built yet, since modifierSummary is
        // deliberately left blank for those (TattooMagicDef.modifierSummary).
        private static string ModifierText(TattooMagicDef tattoo)
        {
            return tattoo.tattooType == TattooEffectType.Passive ? tattoo.modifierSummary : null;
        }
    }
}
