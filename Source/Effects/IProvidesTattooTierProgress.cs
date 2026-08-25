namespace TattooMagic
{
    // Implemented by any HediffComp holding a TattooTierProgress (composed,
    // not inherited — see TattooTierProgress.cs). Consumed by
    // TattooTierProgressUtility so UI code (ITab_Pawn_Tattoos) can read a
    // tattoo's tier/progression without knowing which concrete effect comp
    // backs it, mirroring how IProvidesTattooGizmo decouples gizmo-granting
    // from the shared patch that collects them.
    public interface IProvidesTattooTierProgress
    {
        int Tier { get; }

        int ProgressionCounter { get; }

        // Null once already at the max tier (2) — every tiered comp in this
        // mod currently caps at Tier 2 (TattooTierProgress.tier never exceeds
        // it), so there is nothing further to progress toward.
        int? NextTierThreshold { get; }
    }
}
