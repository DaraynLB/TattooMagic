using RimWorld;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_FrostSigilSlow : HediffCompProperties
    {
        public HediffCompProperties_FrostSigilSlow()
        {
            compClass = typeof(HediffComp_FrostSigilSlow);
        }
    }

    // The attacker-side half of Frost Sigil's slow proc. Its move-speed
    // offset is a one-shot snapshot resolved (via TattooEffectValues) at the
    // moment the proc fires, not recalculated afterward — this hediff isn't
    // itself tier-aware, it just carries whichever tier's magnitude applied
    // when it was granted.
    public class HediffComp_FrostSigilSlow : HediffComp, IProvidesTattooStatOffset
    {
        private float moveSpeedOffset;

        public void Configure(float resolvedMoveSpeedOffset)
        {
            moveSpeedOffset = resolvedMoveSpeedOffset;
        }

        public float GetStatOffset(StatDef stat)
        {
            return stat == StatDefOf.MoveSpeed ? moveSpeedOffset : 0f;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref moveSpeedOffset, "moveSpeedOffset", 0f);
        }
    }
}
