using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // Registers StatPart_TattooEffectOffset onto the vanilla StatDefs this
    // feature's tattoo effects touch, in C# rather than via an XML patch
    // (research.md R3). A future passive tattoo needing a stat bonus on a
    // different StatDef must register it here too before implementing
    // IProvidesTattooStatOffset against that stat.
    [StaticConstructorOnStartup]
    public static class TattooEffectStatPartInstaller
    {
        static TattooEffectStatPartInstaller()
        {
            Register(StatDefOf.ComfyTemperatureMin);
            Register(StatDefOf.ComfyTemperatureMax);
            Register(StatDefOf.MoveSpeed);
            Register(StatDefOf.ShootingAccuracyPawn);
            Register(CombatExtendedInterop.AimingAccuracy);
            Register(StatDefOf.ArmorRating_Sharp);
            Register(StatDefOf.ArmorRating_Blunt);
            Register(StatDefOf.ArmorRating_Heat);
            Register(StatDefOf.MeleeCooldownFactor);
            Register(StatDefOf.RangedCooldownFactor);
            Register(StatDefOf.MeleeDamageFactor);
            Register(StatDefOf.PainShockThreshold);
            Register(StatDefOf.MentalBreakThreshold);
            Register(StatDefOf.PsychicSensitivity);
        }

        private static void Register(StatDef stat)
        {
            if (stat == null)
                return;

            if (stat.parts == null)
                stat.parts = new List<StatPart>();

            stat.parts.Add(new StatPart_TattooEffectOffset { parentStat = stat });

            // StatDef.SetImmutability() runs before this static constructor
            // and marks any stat with no existing <parts>/skill/capacity
            // offsets (and no other Def content dynamically touching it) as
            // immutable, permanently caching its value per-pawn the first
            // time it's queried, bypassing stat.parts entirely thereafter.
            // Registering a StatPart here doesn't undo that determination on
            // its own — explicitly clear it so every stat this installer
            // touches is always recomputed fresh, regardless of whether
            // some other vanilla/mod content happened to already reference it.
            stat.immutable = false;
            stat.Worker.DeleteStatCache();
        }
    }
}
