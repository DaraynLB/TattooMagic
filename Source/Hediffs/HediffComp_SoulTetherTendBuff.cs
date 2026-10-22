using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_SoulTetherTendBuff : HediffCompProperties
    {
        public HediffCompProperties_SoulTetherTendBuff()
        {
            compClass = typeof(HediffComp_SoulTetherTendBuff);
        }
    }

    // Carries the grant-time-configured tend speed/quality magnitudes for a
    // single active Soul Tether tend buff (research.md R6 of
    // specs/017-soul-tether-tattoo-effect). Values are set once via
    // Configure, by SoulTetherTendBuffUtility.GrantOrRefresh — never
    // re-read from TattooEffectValues on every stat query, mirroring Frost
    // Sigil's own HediffComp_FrostSigilSlow precedent for a transient,
    // cross-pawn-granted effect.
    public class HediffComp_SoulTetherTendBuff : HediffComp
    {
        public float qualityBonus;
        public float speedBonus;

        public void Configure(float quality, float speed)
        {
            qualityBonus = quality;
            speedBonus = speed;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref qualityBonus, "qualityBonus", 0f);
            Scribe_Values.Look(ref speedBonus, "speedBonus", 0f);
        }
    }
}
