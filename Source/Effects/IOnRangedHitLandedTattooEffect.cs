using Verse;

namespace TattooMagic
{
    // Implemented by any HediffComp that reacts to its wearer's own ranged
    // attack landing on something. Consumed by the shared
    // Patch_Pawn_PostApplyDamage_TattooOnHit postfix, which calls this on
    // every implementing comp on the attacking pawn whenever a non-melee hit
    // it dealt actually dealt damage. The ranged-attacker-side counterpart to
    // IOnMeleeHitTattooEffect. Call order between multiple implementers on
    // the same pawn is not guaranteed, and the target is not guaranteed to
    // be a Pawn.
    public interface IOnRangedHitLandedTattooEffect
    {
        void OnRangedHitLanded(Thing target, DamageInfo dinfo, float damageDealt);
    }
}
