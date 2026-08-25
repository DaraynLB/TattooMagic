using Verse;

namespace TattooMagic
{
    // Implemented by any HediffComp that wants to react to (and potentially
    // partially reduce or fully block) a specific kind of incoming damage
    // before it's applied to its own pawn. Consumed by
    // Patch_Pawn_PreApplyDamage_TattooDamageAbsorb, a tattoo-agnostic Harmony
    // prefix on Pawn.PreApplyDamage, dispatched to every comp on the
    // about-to-be-damaged pawn implementing this interface, for every
    // incoming DamageInfo regardless of DamageDef. Implementers must return
    // false immediately for any DamageDef they don't care about (mirrors
    // IProvidesTattooStatOffset's "return 0 for stats you don't handle"
    // convention) — the dispatcher has no per-DamageDef routing table.
    //
    // dinfo is passed by ref so an implementer can mutate dinfo.Amount for a
    // partial reduction (e.g. via dinfo.SetAmount(...)) in addition to, or
    // instead of, a full absorb — added in feature 011 (research.md R10) once
    // Ember Ward needed a way to guarantee a real damage reduction under
    // Combat Extended, whose own armor pipeline turned out not to reliably
    // consult a pawn's own StatDef-level armor contribution for this kind of
    // hit (see IProvidesTattooStatOffset's own doc comment for why that path
    // alone isn't sufficient). Returning true fully absorbs the instance
    // (zero damage, no wound, no death) and skips vanilla's/CE's own
    // damage-application pipeline for it entirely, regardless of any prior
    // mutation to dinfo.Amount in the same call.
    public interface IOnIncomingDamageTattooEffect
    {
        bool TryAbsorbIncomingDamage(ref DamageInfo dinfo);
    }
}
