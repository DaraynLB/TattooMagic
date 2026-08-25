using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    public enum TattooEffectType
    {
        Passive,
        Triggered,
    }

    public class TattooMagicDef : Def
    {
        public TattooEffectType tattooType = TattooEffectType.Passive;

        public List<IngredientCount> ingredients = new List<IngredientCount>();

        public float workAmount = 600f;

        public SimpleCurve successCurveOverride;

        public HediffDef appliedHediff;

        // Human-authored, not derived — the actual Tier 1/Tier 2 numbers
        // live scattered across each tattoo's own HediffCompProperties
        // (differently shaped per effect, e.g. FrostSigil's cold resistance
        // + slow chance vs VampiricThorn's flat lifesteal), so there's no
        // single generic value to read here. Left blank for a tattoo with no
        // effect comp built yet (EmberWard, IronskinGlyph, StarlightWard) —
        // inventing numbers before the comp exists would just be wrong.
        public string modifierSummary;

        public string iconPath;

        private Texture2D iconCached;

        // Lazily resolved and cached (research.md-style single read point, like
        // TattooEffectValues) rather than in ResolveReferences: ContentFinder
        // isn't guaranteed populated that early in load order, and this is
        // only ever read from UI code (ITab_Pawn_Tattoos), never per-tick.
        public Texture2D Icon
        {
            get
            {
                if (iconCached == null)
                    iconCached = iconPath.NullOrEmpty() ? BaseContent.BadTex : (ContentFinder<Texture2D>.Get(iconPath, false) ?? BaseContent.BadTex);

                return iconCached;
            }
        }

        // IngredientCount.filter (a ThingFilter) doesn't populate
        // AllowedThingDefs from its raw XML <thingDefs> list on its own —
        // vanilla RecipeDef.ResolveReferences() does this same per-
        // ingredient call for its own ingredients list; the generic Def
        // post-load pass only auto-resolves simple Def-reference fields,
        // not this. Without it, every ingredient filter is silently empty.
        public override void ResolveReferences()
        {
            base.ResolveReferences();
            foreach (IngredientCount ingredient in ingredients)
                ingredient.ResolveReferences();
        }
    }
}
