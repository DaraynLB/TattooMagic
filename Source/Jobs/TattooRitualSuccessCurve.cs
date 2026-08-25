using Verse;

namespace TattooMagic
{
    public static class TattooRitualSuccessCurve
    {
        // Linear scaling with Artistic skill: 0% success at skill 0 (a
        // totally unskilled performer's attempt is never blocked outright
        // per spec FR-005, but is guaranteed to mishap), up to a certain
        // 100% at skill 20 (max). 5 percentage points per skill level.
        public static readonly SimpleCurve Default = new SimpleCurve
        {
            new CurvePoint(0f, 0f),
            new CurvePoint(20f, 1f),
        };

        public static float GetSuccessChance(TattooMagicDef tattoo, float performerArtisticSkill)
        {
            SimpleCurve curve = tattoo?.successCurveOverride ?? Default;
            return curve.Evaluate(performerArtisticSkill);
        }
    }
}
