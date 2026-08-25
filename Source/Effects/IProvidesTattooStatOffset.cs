using RimWorld;

namespace TattooMagic
{
    // Implemented by any HediffComp that wants to contribute a live stat
    // offset. Consumed by StatPart_TattooEffectOffset, which sums this across
    // every hediff comp implementing it on a pawn, for whichever StatDefs the
    // StatPart has been registered on. Implementers should return 0f for any
    // StatDef they don't care about, never throw.
    public interface IProvidesTattooStatOffset
    {
        float GetStatOffset(StatDef stat);
    }
}
