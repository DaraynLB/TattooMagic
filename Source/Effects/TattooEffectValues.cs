using System.Collections.Generic;

namespace TattooMagic
{
    // The single override-capable read point for every tattoo effect's tunable
    // numeric values (FR-012). No setter exists yet — nothing in the codebase
    // writes to the override dictionary today. A future settings-UI feature
    // (PRD §10) can populate it without any tattoo's effect code changing.
    public static class TattooEffectValues
    {
        private static readonly Dictionary<string, float> overrides = new Dictionary<string, float>();

        public static float Get(string scopeKey, string valueKey, float xmlDefault)
        {
            string fullKey = scopeKey + "." + valueKey;
            return overrides.TryGetValue(fullKey, out float value) ? value : xmlDefault;
        }
    }
}
