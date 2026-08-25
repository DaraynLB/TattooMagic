namespace TattooMagic
{
    // Marker interface (no members) for an IProvidesTattooGizmo comp whose
    // gizmo click does not itself complete the ability's activation (e.g. a
    // two-phase click-then-target flow). Patch_Pawn_GetGizmos_TattooGizmos
    // skips its automatic click-time mastery-progress credit for any comp
    // implementing this marker; the comp is then responsible for calling
    // TattooTrackerUtility.GetTracker(pawn)?.RegisterMasteryActivation()
    // itself, exactly once, only on a genuinely completed activation
    // (research.md R4 of feature 008, contracts/cell-targeted-ability-
    // contract.md §2).
    public interface IHandlesOwnMasteryProgress
    {
    }
}
