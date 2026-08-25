using RimWorld;
using Verse;

namespace TattooMagic
{
    // The single Combat Extended-detection convention (research.md R3,
    // contract §2). Computed once at startup and never throws in either
    // configuration. Any future tattoo needing to branch on CE, or to read a
    // CE-only StatDef by name, should resolve it here rather than
    // re-implementing detection inline in its own comp.
    [StaticConstructorOnStartup]
    public static class CombatExtendedInterop
    {
        public static readonly bool IsLoaded;

        // CE's own accuracy-equivalent StatDef. Non-null only when CE is
        // loaded, so a null check on this field is equivalent to (and
        // sufficient instead of) separately checking IsLoaded.
        public static readonly StatDef AimingAccuracy;

        static CombatExtendedInterop()
        {
            IsLoaded = ModsConfig.IsActive("CETeam.CombatExtended");
            AimingAccuracy = DefDatabase<StatDef>.GetNamedSilentFail("AimingAccuracy");
        }
    }
}
