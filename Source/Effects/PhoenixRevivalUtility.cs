using RimWorld;
using Verse;

namespace TattooMagic
{
    // Pure logic factored out of GameComponent_PhoenixRegistry's own sweep so
    // Dev Mode debug tooling can also force-resolve an attempt directly,
    // without waiting real/simulated days (mirrors why TattooHealingUtility/
    // TattooRitualSuccessCurve are their own static classes rather than
    // inlined into a single comp).
    [StaticConstructorOnStartup]
    public static class PhoenixRevivalUtility
    {
        // "Burn" isn't exposed via vanilla's own curated HediffDefOf (unlike
        // e.g. HediffDefOf.ResurrectionSickness) — resolved once here,
        // mirroring CombatExtendedInterop's own def-lookup-at-startup
        // pattern, rather than a per-call DefDatabase lookup.
        private static readonly HediffDef BurnHediffDef;

        static PhoenixRevivalUtility()
        {
            BurnHediffDef = DefDatabase<HediffDef>.GetNamed("Burn");
        }

        // FR-014/Assumptions: a wearer's body is permanently lost once its
        // corpse is destroyed, fully decayed (Dessicated — a stable, still-
        // existing state, not an auto-destroyed one, research.md R2), buried
        // (research.md R6), or missing its brain (research.md R1 — no
        // vanilla precedent for this gate; genuinely new). Never checked for
        // a cremation-guaranteed pawn, whose corpse was deliberately
        // destroyed by the sanctioned exception (research.md R4/R5).
        public static bool IsPermanentlyLost(Pawn pawn)
        {
            Corpse corpse = pawn.Corpse;
            if (corpse == null || corpse.Destroyed)
                return LogPermanentlyLost(pawn, "corpse missing/destroyed");

            if (corpse.GetRotStage() == RotStage.Dessicated)
                return LogPermanentlyLost(pawn, "corpse fully decayed (Dessicated)");

            if (corpse.ParentHolder is Building_Grave)
                return LogPermanentlyLost(pawn, "corpse buried");

            if (pawn.health.hediffSet.GetBrain() == null)
                return LogPermanentlyLost(pawn, "brain missing/destroyed");

            return false;
        }

        // Reports exactly which precondition failed — this was previously a
        // silent black box that took real back-and-forth to diagnose live
        // (a hostile pawn correctly failing this check for one reason was
        // indistinguishable in the log from a genuine bug elsewhere).
        private static bool LogPermanentlyLost(Pawn pawn, string reason)
        {
            if (TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic DEBUG] Phoenix: {pawn.LabelShort} is permanently lost — {reason}.");

            return true;
        }

        // Called once a pending attempt's scheduled tick has arrived (or,
        // via Dev Mode tooling, forced early). Returns true if the pawn is
        // now alive again.
        public static bool TryResolveAttempt(Pawn pawn, HediffComp_PhoenixEffect comp)
        {
            if (!comp.cremationGuaranteed && IsPermanentlyLost(pawn))
            {
                comp.reviveAttemptTick = 0;
                GameComponent_PhoenixRegistry.Get()?.Unregister(pawn);

                if (TattooMagicSettings.EnableDebugLogging)
                    Log.Message($"[TattooMagic DEBUG] Phoenix: {pawn.LabelShort} is permanently lost — revival no longer possible.");

                return false;
            }

            bool succeeded;
            if (comp.cremationGuaranteed)
            {
                // FR-020: bypasses the decomposition-based chance entirely.
                succeeded = true;
            }
            else
            {
                Corpse corpse = pawn.Corpse;
                float rotProgressDays = (corpse?.TryGetComp<CompRottable>()?.RotProgress ?? 0f) / GenDate.TicksPerDay;
                float chance = comp.Props.decompositionChanceCurve?.Evaluate(rotProgressDays) ?? 0f;
                succeeded = Rand.Chance(chance);
            }

            bool atTier2 = comp.tier >= 2;
            float burnSeverity = atTier2
                ? TattooEffectValues.Get(comp.ScopeKey, "BurnSeverityTier2", comp.Props.burnSeverityTier2)
                : TattooEffectValues.Get(comp.ScopeKey, "BurnSeverityTier1", comp.Props.burnSeverityTier1);

            // FR-015: every attempt burns the wearer, success or failure.
            ApplyBurn(pawn, pawn.RaceProps.body.corePart, burnSeverity);
            RegisterAttemptAndMaybeApplyAbasia(pawn, comp, succeeded);

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] Phoenix revival attempt on {pawn.LabelShort}: " +
                    $"cremationGuaranteed={comp.cremationGuaranteed}, succeeded={succeeded}, attemptCount={comp.revivalAttemptCount}.");
            }

            if (!succeeded)
            {
                // FR-012: retries once per day.
                comp.reviveAttemptTick = Find.TickManager.TicksGame + GenDate.TicksPerDay;
                return false;
            }

            // Captured before clearing the comp's own fields below — needed
            // only as a fallback if the corpse no longer exists by now
            // (the cremation path; research.md R4).
            IntVec3 fallbackPos = comp.cremationFallbackPos;
            Map fallbackMap = comp.cremationFallbackMap;

            comp.reviveAttemptTick = 0;
            comp.cremationGuaranteed = false;
            comp.cremationFallbackPos = IntVec3.Invalid;
            comp.cremationFallbackMap = null;
            // FR-019: the days-alive clock resets on every revival.
            comp.daysAliveCounter = 0;
            comp.nextDaysAliveCheckTick = 0;

            bool resurrected = ResurrectionUtility.TryResurrect(pawn);

            // research.md R4: ResurrectionUtility.TryResurrect silently skips
            // GenSpawn.Spawn when pawn.Corpse is already null at call time —
            // only ever true here for a cremated corpse, since the natural
            // path's own IsPermanentlyLost check above guarantees a live
            // corpse exists for every non-cremation attempt.
            if (resurrected && !pawn.Spawned && fallbackMap != null)
                GenSpawn.Spawn(pawn, fallbackPos, fallbackMap);

            return resurrected;
        }

        // Shared by both revival attempts (tier-appropriate severity) and
        // the cauterize passive (always Tier 1 severity, FR-029) — reuses
        // vanilla's own Burn HediffDef, no custom HediffDef.
        public static void ApplyBurn(Pawn pawn, BodyPartRecord part, float severity)
        {
            Hediff hediff = HediffMaker.MakeHediff(BurnHediffDef, pawn, part);
            hediff.Severity = severity;
            pawn.health.AddHediff(hediff);

            // Made permanent immediately, rather than left to heal into a
            // scar over time the way vanilla fire damage normally would —
            // required so it survives ResurrectionUtility.TryResurrect's own
            // Notify_Resurrected cleanup, which unconditionally strips every
            // non-permanent, everCurableByItem Hediff_Injury on the pawn
            // (Burn included) on every resurrection. Without this, a
            // successful revival's own burn (and every earlier one still
            // non-permanent) would vanish the instant it's applied — a real
            // bug caught via live in-game testing, not a hypothetical
            // (confirmed by decompiling Pawn_HealthTracker.Notify_Resurrected
            // during the fix). This also means repeated burns on the same
            // body part accumulate as separate permanent scars rather than
            // merging into one growing injury (Hediff_Injury.TryMergeWith
            // refuses to merge once either side IsPermanent()) — a better
            // match for FR-015's "visibly accumulates scarring... across a
            // wearer's lifetime revivals" than a single ever-growing number
            // would have been anyway.
            if (hediff is HediffWithComps hediffWithComps)
                hediffWithComps.TryGetComp<HediffComp_GetsPermanent>()?.IsPermanent = true;
        }

        // FR-016/FR-017/FR-018: always advances the lifetime attempt
        // counter; only actually applies Paralytic Abasia when the attempt
        // succeeded and this is the wearer's 3rd (or later) lifetime
        // attempt, with an escalating duration set programmatically on the
        // granted hediff's own HediffComp_Disappears (research.md R10,
        // mirrors HediffComp_FrostSigilEffect.GrantSlow's existing pattern).
        public static void RegisterAttemptAndMaybeApplyAbasia(Pawn pawn, HediffComp_PhoenixEffect comp, bool succeeded)
        {
            comp.revivalAttemptCount++;

            if (!succeeded || comp.revivalAttemptCount < 3)
                return;

            comp.abasiaOccurrenceCount++;

            int baseDays = (int)TattooEffectValues.Get(comp.ScopeKey, "AbasiaBaseDurationDays", comp.Props.abasiaBaseDurationDays);
            int incrementDays = (int)TattooEffectValues.Get(comp.ScopeKey, "AbasiaDurationIncrementDays", comp.Props.abasiaDurationIncrementDays);
            int durationDays = baseDays + (comp.abasiaOccurrenceCount - 1) * incrementDays;

            Hediff abasia = HediffMaker.MakeHediff(TattooMagicDefOf.TattooMagic_Hediff_ParalyticAbasia, pawn);
            pawn.health.AddHediff(abasia);

            if (abasia is HediffWithComps abasiaWithComps)
                abasiaWithComps.TryGetComp<HediffComp_Disappears>()?.SetDuration(durationDays * GenDate.TicksPerDay);

            if (TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic DEBUG] Phoenix: Paralytic Abasia applied to {pawn.LabelShort}, occurrence {comp.abasiaOccurrenceCount}, duration {durationDays} day(s).");
        }
    }
}
