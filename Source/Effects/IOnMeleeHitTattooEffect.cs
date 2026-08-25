using Verse;

namespace TattooMagic
{
    // Implemented by any HediffComp that reacts to its wearer being struck in
    // melee. Consumed by the shared Patch_Pawn_PostApplyDamage_TattooOnHit
    // postfix, which calls this on every implementing comp whenever the pawn
    // takes a melee hit that actually dealt damage. Call order between
    // multiple implementers on the same pawn is not guaranteed.
    public interface IOnMeleeHitTattooEffect
    {
        void OnMeleeHitTaken(Pawn attacker, DamageInfo dinfo, float damageDealt);
    }
}
