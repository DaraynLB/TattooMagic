using UnityEngine;
using Verse;

namespace TattooMagic
{
    // Persisted per-install (not per-save) via RimWorld's standard ModSettings
    // Scribe cycle. EnableDebugLogging is the single switch every diagnostic
    // Log.Message call this mod adds for its own testing purposes checks —
    // previously most of those were gated behind Prefs.DevMode, which meant
    // shipping with Dev Mode on (or a player who has it on for an unrelated
    // reason) would spam the log with this mod's own tattoo-activation/
    // targeting-decision traces. This setting is off by default and fully
    // independent of Dev Mode, so a normal player never sees this output
    // unless they deliberately opt in from Mod Settings.
    public class TattooMagicSettings : ModSettings
    {
        public bool enableDebugLogging;

        internal static TattooMagicSettings Instance;

        public static bool EnableDebugLogging => Instance != null && Instance.enableDebugLogging;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableDebugLogging, "enableDebugLogging", false);
        }
    }

    // The Mod subclass RimWorld's mod list discovers to show a "Mod options"
    // entry for this mod. Deliberately kept minimal (one checkbox) — this is
    // not the general tattoo-balance settings UI PRD §10 defers post-v1, just
    // a way for a player to control this mod's own diagnostic logging without
    // needing to touch RimWorld's global Dev Mode toggle.
    public class TattooMagicModSettings : Mod
    {
        private readonly TattooMagicSettings settings;

        public TattooMagicModSettings(ModContentPack content) : base(content)
        {
            settings = GetSettings<TattooMagicSettings>();
            TattooMagicSettings.Instance = settings;
        }

        public override string SettingsCategory() => "TattooMagic";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled(
                "Enable debug logging",
                ref settings.enableDebugLogging,
                "Logs detailed diagnostic messages (tattoo activations, targeting decisions, ritual eligibility checks, etc.) to the debug log. Leave this off for normal play — it's meant for troubleshooting, not everyday use.");

            listing.End();
        }
    }
}
