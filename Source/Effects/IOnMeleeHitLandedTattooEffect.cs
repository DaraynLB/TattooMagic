using Verse;

namespace TattooMagic
{
    // Implemented by any HediffComp that reacts to its wearer's own melee
    // attack landing on something. Consumed by the shared
    // Patch_Pawn_PostApplyDamage_TattooOnHit postfix, which calls this on
    // every implementing comp on the attacking pawn whenever a melee hit it
    // dealt actually dealt damage. The melee-attacker-side counterpart to
    // IOnRangedHitLandedTattooEffect. killedTarget is true only when this
    // specific hit was the one that killed target; target is always a Pawn
    // in practice for this dispatch site, typed as Thing for signature
    // symmetry with IOnRangedHitLandedTattooEffect. Call order between
    // multiple implementers on the same pawn is not guaranteed.
    public interface IOnMeleeHitLandedTattooEffect
    {
        void OnMeleeHitLanded(Thing target, DamageInfo dinfo, float damageDealt, bool killedTarget);
    }
}
