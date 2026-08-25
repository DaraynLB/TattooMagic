using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    // Ingredients are found and consumed directly from the map's stock
    // rather than physically hauled/animated toil-by-toil (research.md R3).
    // The WorkGiver already gates on availability before offering the job,
    // and this consumes atomically once the ritual's work toil completes,
    // so partial consumption on failure (spec FR-008/edge cases) can't happen.
    public static class TattooRitualIngredientUtility
    {
        public static bool HasIngredientsAvailable(Map map, TattooMagicDef tattoo, bool verbose = false)
        {
            bool allOk = true;
            foreach (IngredientCount ingredient in tattoo.ingredients)
            {
                float available = CountAvailable(map, ingredient);
                float needed = ingredient.GetBaseCount();
                bool ok = available >= needed;
                allOk &= ok;

                if (verbose)
                {
                    int allowedDefCount = ingredient.filter.AllowedThingDefs.Count();
                    string allowedNames = string.Join(", ", ingredient.filter.AllowedThingDefs.Select(d => d.defName));
                    Log.Message($"[TattooMagic DEBUG]   ingredient: need {needed}, have {available}, ok={ok}, filter allows {allowedDefCount} def(s): [{allowedNames}]");
                }
            }
            return allOk;
        }

        public static bool TryConsumeIngredients(Map map, TattooMagicDef tattoo)
        {
            if (!HasIngredientsAvailable(map, tattoo))
                return false;

            foreach (IngredientCount ingredient in tattoo.ingredients)
            {
                float remaining = ingredient.GetBaseCount();
                foreach (Thing thing in ThingsMatching(map, ingredient).ToList())
                {
                    if (remaining <= 0f)
                        break;

                    int take = Mathf.Min(thing.stackCount, Mathf.CeilToInt(remaining));
                    remaining -= take;
                    thing.SplitOff(take).Destroy();
                }
            }
            return true;
        }

        private static float CountAvailable(Map map, IngredientCount ingredient)
        {
            float total = 0f;
            foreach (Thing thing in ThingsMatching(map, ingredient))
                total += thing.stackCount;
            return total;
        }

        private static IEnumerable<Thing> ThingsMatching(Map map, IngredientCount ingredient)
        {
            foreach (ThingDef thingDef in ingredient.filter.AllowedThingDefs)
            {
                foreach (Thing thing in map.listerThings.ThingsOfDef(thingDef))
                {
                    if (!thing.IsForbidden(Faction.OfPlayer))
                        yield return thing;
                }
            }
        }
    }
}
