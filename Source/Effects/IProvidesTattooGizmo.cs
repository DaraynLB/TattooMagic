using Verse;

namespace TattooMagic
{
    // Implemented by any HediffComp that wants to contribute one gizmo to its
    // pawn's gizmo row. Consumed by the shared Patch_Pawn_GetGizmos_TattooGizmos
    // postfix, which calls this on every implementing comp whenever the pawn's
    // gizmos are requested (selection/UI refresh, not per-tick).
    // Implementers MUST compute disabled/disabledReason/icon state live from
    // their own persisted fields each call rather than caching a Gizmo
    // instance, since that state can change between calls
    // (contracts/triggered-tattoo-effect-contract.md §1).
    public interface IProvidesTattooGizmo
    {
        Gizmo GetGizmo();
    }
}
