using RimWorld;

namespace TattooMagic
{
    // Implemented by any HediffComp that wants to veto *other* comps'
    // negative IProvidesTattooStatOffset contributions to a specific stat,
    // for as long as some condition of its own holds (e.g. an active buff
    // window). Consumed by StatPart_TattooEffectOffset, which clamps every
    // other comp's own negative offset to 0 for a stat while at least one
    // comp on the pawn reports true here for that stat. Does not affect the
    // implementing comp's own contribution (still governed by
    // IProvidesTattooStatOffset), or any positive contribution from any
    // comp — see contracts/stat-offset-immunity-contract.md §1 (feature 005).
    public interface IGrantsStatOffsetImmunity
    {
        bool BlocksNegativeOffsets(StatDef stat);
    }
}
