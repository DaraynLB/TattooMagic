using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace TattooMagic
{
    // Shared, tattoo-agnostic dispatch point for "this pawn was struck by a
    // melee attacker" (research.md R2). Filters to melee hits that actually
    // dealt damage, then calls IOnMeleeHitTattooEffect on every hediff comp
    // implementing it on the struck pawn. Frost Sigil is the first consumer,
    // not the only intended one.
    //
    // Melee detection uses dinfo.Tool != null rather than dinfo.Weapon, since
    // Tool is populated for both armed melee (a weapon's own Tool, e.g. a
    // knife's blade) and unarmed/natural melee (a race's natural Tools, e.g.
    // "left fist") — Weapon alone is null for unarmed attacks and would
    // silently exclude every bare-fisted or animal-bite hit, which PRD §6's
    // "a melee attacker" wording doesn't restrict to armed attackers only.
    //
    // The sibling ranged branch (feature 003, research.md R2) fires on the
    // inverse condition (dinfo.Tool == null) and dispatches to the
    // INSTIGATOR's own comps instead of the struck pawn's — "my own shot
    // landed" rather than "I was struck" — which also naturally excludes
    // turret-fired shots, since vanilla turrets are Buildings, not Pawns.
    //
    // A second melee-side dispatch (feature 010, research.md R2-R4) also
    // fires alongside DispatchMeleeHit, notifying the ATTACKER's own comps
    // that their own melee attack landed ("my own hit landed", the melee
    // counterpart to the ranged branch above), including whether that hit
    // was a killing blow.
    //
    // Since feature 006 (research.md R10), each dispatched notification also
    // credits one tattoo-mastery qualifying event to the comp-owning pawn,
    // alongside the gizmo-wrap hook in Patch_Pawn_GetGizmos_TattooGizmos —
    // this is passive tattoos' side of that same pawn-wide progression track.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Patch_Pawn_PostApplyDamage_TattooOnHit
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (totalDamageDealt <= 0f)
                return;

            if (!(dinfo.Instigator is Pawn attacker))
                return;

            bool isMelee = dinfo.Tool != null;

            // Dev-Mode-only, always-on visibility into every hit this patch
            // sees — the single point every on-hit tattoo effect (Frost
            // Sigil, Serpent's Eye, any future one) is dispatched from, so
            // this answers "is combat even landing hits at all" independent
            // of any specific tattoo's own proc chance.
            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] Pawn.PostApplyDamage: {attacker.LabelShort} " +
                    $"{(isMelee ? "melee-hit" : "ranged-hit")} {__instance.LabelShort} for {totalDamageDealt:F1} dmg.");
            }

            if (isMelee)
            {
                DispatchMeleeHit(__instance, attacker, dinfo, totalDamageDealt);
                DispatchMeleeHitLanded(attacker, __instance, dinfo, totalDamageDealt);
            }
            else
            {
                DispatchRangedHit(attacker, __instance, dinfo, totalDamageDealt);
            }
        }

        private static void DispatchMeleeHit(Pawn struckPawn, Pawn attacker, DamageInfo dinfo, float totalDamageDealt)
        {
            if (struckPawn?.health?.hediffSet == null)
                return;

            int dispatchedTo = 0;
            List<Hediff> hediffs = struckPawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is IOnMeleeHitTattooEffect effect)
                    {
                        effect.OnMeleeHitTaken(attacker, dinfo, totalDamageDealt);
                        dispatchedTo++;

                        // Tattoo-mastery contribution for passive tattoos
                        // (feature 006 scope expansion, research.md R10):
                        // every dispatched notification counts once toward
                        // the struck pawn's pawn-wide masteryProgress,
                        // regardless of whether this tattoo's own effect
                        // internally procs (e.g. Frost Sigil's slow chance)
                        // — that gate belongs to the tattoo's own private
                        // tier-progression counter (tierState), not to this
                        // shared dispatch point, mirroring how the gizmo
                        // wrap (research.md R1) never inspects a triggered
                        // tattoo's own internals either.
                        TattooTrackerUtility.GetTracker(struckPawn)?.RegisterMasteryActivation();
                    }
                }
            }

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] DispatchMeleeHit: {struckPawn.LabelShort} has " +
                    $"{dispatchedTo} IOnMeleeHitTattooEffect comp(s) notified of this hit from {attacker.LabelShort}.");
            }
        }

        // The melee-attacker-side counterpart to DispatchRangedHit (feature
        // 010, research.md R2-R4) — notifies the ATTACKING pawn's own comps
        // that their own melee attack landed, alongside DispatchMeleeHit's
        // existing notification to the struck pawn. killedTarget reflects
        // struckPawn.Dead, read here rather than via a separate patch: it's
        // reliably observable at this point because Pawn.PostApplyDamage's
        // own internal call chain (health.PostApplyDamage -> pawn.Kill, when
        // lethal) completes synchronously before this postfix ever runs.
        private static void DispatchMeleeHitLanded(Pawn attacker, Pawn struckPawn, DamageInfo dinfo, float totalDamageDealt)
        {
            if (attacker?.health?.hediffSet == null)
                return;

            bool killedTarget = struckPawn.Dead;

            List<Hediff> hediffs = attacker.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is IOnMeleeHitLandedTattooEffect effect)
                    {
                        effect.OnMeleeHitLanded(struckPawn, dinfo, totalDamageDealt, killedTarget);

                        // Same tattoo-mastery contribution as the other two
                        // branches, credited to the attacker (feature 006
                        // precedent).
                        TattooTrackerUtility.GetTracker(attacker)?.RegisterMasteryActivation();
                    }
                }
            }
        }

        private static void DispatchRangedHit(Pawn attacker, Thing target, DamageInfo dinfo, float totalDamageDealt)
        {
            if (attacker?.health?.hediffSet == null)
                return;

            List<Hediff> hediffs = attacker.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is HediffWithComps hediffWithComps) || hediffWithComps.comps == null)
                    continue;

                List<HediffComp> comps = hediffWithComps.comps;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (comps[j] is IOnRangedHitLandedTattooEffect effect)
                    {
                        effect.OnRangedHitLanded(target, dinfo, totalDamageDealt);

                        // Same tattoo-mastery contribution as the melee
                        // branch above, credited to the attacker (the
                        // pawn whose own shot landed), once per dispatched
                        // notification (research.md R10).
                        TattooTrackerUtility.GetTracker(attacker)?.RegisterMasteryActivation();
                    }
                }
            }
        }
    }
}
