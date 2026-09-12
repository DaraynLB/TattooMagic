using RimWorld;

namespace TattooMagic
{
    // Routes the flat, non-stacking regrowth mood penalty through TattooEffectValues (FR-015), matching every
    // other tattoo's tunable-value convention, rather than leaving it as a bare, unrouted
    // ThoughtDef.stages[0].baseMoodEffect value (research.md R5). CurStage.baseMoodEffect (the ThoughtDef's own
    // XML value) is used only as the accessor's fallback default.
    public class Thought_ShedscaleRegrowthActive : Thought_Situational
    {
        protected override float BaseMoodOffset =>
            TattooEffectValues.Get("TattooMagic_Hediff_Shedscale", "RegrowthMoodPenalty", CurStage.baseMoodEffect);
    }
}
