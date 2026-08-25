using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    public class HediffCompProperties_WraithstepEffect : HediffCompProperties
    {
        public float rangeTier1 = 6f;
        public float rangeTier2 = 10f;

        public int cooldownDurationTicksTier1 = 1200;
        public int cooldownDurationTicksTier2 = 1200;

        // Tier 2 only — no Tier 1 equivalent (Tier 1 grants no exclusion at
        // all).
        public int untargetableWindowTicksTier2 = 90;

        public int tier2Threshold = 15;

        public string abilityIconPath = "UI/Commands/Wraithstep";

        public HediffCompProperties_WraithstepEffect()
        {
            compClass = typeof(HediffComp_WraithstepEffect);
        }
    }

    // Wraithstep's own effect: the mod's fourth triggered ability, and the
    // first whose gizmo click doesn't itself complete the activation — it
    // opens RimWorld's own cell targeter (Find.Targeter.BeginTargeting) and
    // only relocates the wearer once a valid destination is confirmed
    // (research.md R1/R2 of feature 008). Implements IProvidesTattooGizmo
    // (reused, feature 004) and IHandlesOwnMasteryProgress (new, research.md
    // R4) since its two-phase shape means the shared gizmo-wrap hook's
    // click-time mastery credit would be wrong here — this comp credits
    // mastery itself, only on a confirmed blink. Upgrades in place to a
    // Tier 2 that grants a longer range and, for a brief window immediately
    // after landing, excludes the wearer from hostile AI's attack-target
    // selection (PRD §5.4/§6).
    public class HediffComp_WraithstepEffect : HediffComp, IProvidesTattooGizmo, IHandlesOwnMasteryProgress, IProvidesTattooTierProgress
    {
        // How often, in ticks, an active Tier 2 untargetable window
        // re-corrects any hostile already locked onto the wearer (mirrors
        // HediffComp_GuardiansCallEffect.ReassertIntervalTicks). Excluding
        // the wearer from AttackTargetFinder.BestAttackTarget's candidate
        // pool (the targeting-exclusion Prefix) only stops a *fresh* target
        // search from picking her — confirmed via live testing (2026-08-15)
        // that a hostile who had already locked the wearer as its
        // mindState.enemyTarget/meleeThreat/CurJob.targetA before (or
        // without) a fresh search keeps attacking regardless, the same gap
        // Guardian's Call's own research (feature 004) found on the
        // opposite (forcing) side of this problem. This periodic correction
        // actively clears that lock so the hostile is forced back through
        // its own top-level AI loop, which then runs the (already-correct)
        // excluded search.
        private const int ReassertIntervalTicks = 30;

        public TattooTierProgress tierState = new TattooTierProgress();

        public int cooldownEndTick;
        public int untargetableEndTick;

        public HediffCompProperties_WraithstepEffect Props => (HediffCompProperties_WraithstepEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        // The maximum selectable blink range for the comp's current tier,
        // resolved through TattooEffectValues — consulted by both the
        // destination-validity check and the live range-ring draw callback.
        public float Range => tierState.tier >= 2
            ? TattooEffectValues.Get(ScopeKey, "RangeTier2", Props.rangeTier2)
            : TattooEffectValues.Get(ScopeKey, "RangeTier1", Props.rangeTier1);

        // Read by WraithstepUntargetableRegistry, not by anything on this
        // comp itself (research.md R6).
        public bool IsCurrentlyUntargetable => Find.TickManager.TicksGame < untargetableEndTick;

        // Confirmed via live testing (2026-08-15) that RimWorld does NOT
        // automatically greay out a custom Command_Action just because its
        // pawn is downed — that assumption in this feature's spec
        // Assumptions section ("no new incapacitation logic is expected to
        // be required beyond what vanilla already provides") was wrong;
        // vanilla's own gizmo-disabling only applies to its own built-in
        // commands. A downed pawn's Wraithstep gizmo therefore needs an
        // explicit check here, same as the cooldown check.
        public Gizmo GetGizmo()
        {
            Pawn pawn = Pawn;
            bool downed = pawn != null && pawn.Downed;
            bool onCooldown = Find.TickManager.TicksGame < cooldownEndTick;

            return new Command_Action
            {
                defaultLabel = "Wraithstep",
                defaultDesc = "Blink to a nearby spot, then go on cooldown.",
                icon = ContentFinder<Texture2D>.Get(Props.abilityIconPath, false),
                action = BeginTargetSelection,
                Disabled = downed || onCooldown,
                disabledReason = downed
                    ? "Wraithstep cannot be used while downed."
                    : onCooldown
                        ? $"Wraithstep is recharging: {RemainingCooldownSeconds()}s remaining."
                        : null,
            };
        }

        private int RemainingCooldownSeconds()
        {
            return Mathf.CeilToInt((cooldownEndTick - Find.TickManager.TicksGame) / 60f);
        }

        // Opens RimWorld's own cell targeter rather than completing the
        // activation immediately (research.md R1/R2) — the real effect only
        // happens in OnDestinationConfirmed, once the player picks a cell
        // IsValidDestination accepts. A cancelled session (Escape/right-
        // click, or any other path that ends targeting without a confirmed
        // cell) simply never calls OnDestinationConfirmed — no cooldown, no
        // tier-progression increment, and no mastery credit occur (FR-002,
        // SC-005). No-ops if downed or still on cooldown (defensive; the
        // gizmo already prevents both per Edge Cases) — e.g. a pawn that
        // goes down after the gizmo was last drawn but before this action
        // fires.
        private void BeginTargetSelection()
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Downed)
                return;

            if (Find.TickManager.TicksGame < cooldownEndTick)
                return;

            if (pawn?.Map == null)
                return;

            var targetParams = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = false,
                canTargetSelf = false,
            };

            Find.Targeter.BeginTargeting(
                targetParams,
                action: OnDestinationConfirmed,
                highlightAction: null,
                targetValidator: IsValidDestination,
                caster: pawn,
                onGuiAction: DrawRejectReasonLabel,
                onUpdateAction: DrawRangeRing);
        }

        // FR-002: the single source of truth for a valid destination cell —
        // reused unmodified by GetRejectReason so the two can never disagree
        // (research.md R3, data-model.md).
        private bool IsValidDestination(LocalTargetInfo target)
        {
            return GetRejectReason(target) == null;
        }

        // Same checks as IsValidDestination, in the same order, returning the
        // first failure's player-facing reason (or null once valid) — drawn
        // live near the cursor by DrawRejectReasonLabel (FR-003).
        private string GetRejectReason(LocalTargetInfo target)
        {
            Pawn pawn = Pawn;
            Map map = pawn?.Map;
            if (map == null)
                return "No map.";

            IntVec3 cell = target.Cell;

            if (!cell.InBounds(map))
                return "Out of bounds.";
            if (cell.Fogged(map))
                return "Not explored.";
            if (!cell.Walkable(map))
                return "Can't stand there.";
            if (cell.GetFirstPawn(map) != null)
                return "Occupied.";
            if ((cell - pawn.Position).LengthHorizontalSquared > Range * Range)
                return "Out of range.";

            return null;
        }

        // Verse.Targeter's onUpdateAction callback (research.md R3) — fires
        // every Update frame the targeter is open for this ability, so the
        // range ring tracks the wearer's own position live.
        private void DrawRangeRing(LocalTargetInfo target)
        {
            Pawn pawn = Pawn;
            if (pawn != null)
                GenDraw.DrawRadiusRing(pawn.Position, Range);
        }

        // Verse.Targeter's onGuiAction callback (research.md R3) — fires
        // every OnGUI frame with whatever cell is currently under the mouse;
        // draws a short reason label there whenever that cell fails
        // IsValidDestination, using RimWorld's own mouse-attachment widget
        // (the same mechanism vanilla's own targeted abilities/placement
        // ghosts use for reject feedback).
        private void DrawRejectReasonLabel(LocalTargetInfo target)
        {
            string rejectReason = GetRejectReason(target);
            if (rejectReason != null)
                GenUI.DrawMouseAttachment(null, rejectReason);
        }

        // The actual activation, called only for a destination
        // IsValidDestination already accepted (Find.Targeter will not invoke
        // this for a rejected cell). Reads whichever tier's numbers are
        // current at the moment of activation, so an activation made just
        // before a Tier 1->2 upgrade uses Tier 1's numbers and the next
        // activation uses Tier 2's, with no retroactive change (same
        // precedent as every prior triggered tattoo).
        private void OnDestinationConfirmed(LocalTargetInfo target)
        {
            Pawn pawn = Pawn;
            if (pawn == null)
                return;

            bool atTier2 = tierState.tier >= 2;

            pawn.Position = target.Cell;
            pawn.Notify_Teleported();

            int now = Find.TickManager.TicksGame;

            int cooldown = (int)(atTier2
                ? TattooEffectValues.Get(ScopeKey, "CooldownDurationTicksTier2", Props.cooldownDurationTicksTier2)
                : TattooEffectValues.Get(ScopeKey, "CooldownDurationTicksTier1", Props.cooldownDurationTicksTier1));
            cooldownEndTick = now + cooldown;

            // Tier 2 only (FR-009) — Tier 1 skips this entirely, matching
            // Props.untargetableWindowTicksTier2 having no Tier 1
            // counterpart.
            if (atTier2)
            {
                int window = (int)TattooEffectValues.Get(
                    ScopeKey, "UntargetableWindowTicksTier2", Props.untargetableWindowTicksTier2);
                untargetableEndTick = now + window;
                WraithstepUntargetableRegistry.Register(this);

                // Correct immediately on landing, not just on the next
                // CompPostTick reassert — a hostile may already have the
                // wearer locked on from before this blink (research.md R6
                // addendum below).
                ForceNearbyHostilesOffWearer(pawn);
            }

            // FR-006: one qualifying event per successful activation.
            tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

            // FR-019/research.md R4: this comp is responsible for its own
            // mastery credit (IHandlesOwnMasteryProgress opts it out of the
            // shared click-time wrap), fired only here — a genuine, resolved
            // activation — never on a click alone.
            TattooTrackerUtility.GetTracker(pawn)?.RegisterMasteryActivation();

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic] Wraithstep activated by {pawn.LabelShort}: tier={tierState.tier}, " +
                    $"destination={target.Cell}, cooldown={cooldown}, untargetableUntil={untargetableEndTick}, " +
                    $"progress={tierState.progressionCounter}.");
            }
        }

        // Re-asserts the untargetable correction for the whole active
        // window, not just once at activation — mirrors HediffComp_
        // GuardiansCallEffect.CompPostTick's identical reassert-loop shape
        // for the same reason: some hostile could lock onto the wearer via
        // a path that only runs between one check and the next (e.g. a
        // hostile that only becomes adjacent, or only decides to attack,
        // partway through the window). Gated on untargetableEndTick first
        // (the common case once the window ends or at Tier 1, where it's
        // never set), so this is a no-op outside an active window.
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            int now = Find.TickManager.TicksGame;
            if (now >= untargetableEndTick)
                return;

            if (now % ReassertIntervalTicks != 0)
                return;

            ForceNearbyHostilesOffWearer(Pawn);
        }

        // The active half of the Tier 2 exclusion (research.md R6 addendum,
        // confirmed necessary via live testing 2026-08-15): the targeting-
        // exclusion Prefix alone only keeps a *fresh* AttackTargetFinder.
        // BestAttackTarget search from picking the wearer — it does nothing
        // for a hostile that already has her locked in via
        // mindState.enemyTarget, mindState.meleeThreat, or its running
        // job's own targetA (the same three fields Guardian's Call's taunt
        // has to correct, for the opposite reason). This directly clears
        // those references on any hostile currently locked onto the wearer
        // and forces its current job to end, compelling an immediate
        // re-decision on its very next AI tick — which then correctly runs
        // through the already-excluded search instead of continuing to
        // swing at an already-decided target.
        private void ForceNearbyHostilesOffWearer(Pawn pawn)
        {
            Map map = pawn?.Map;
            if (map == null)
                return;

            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn hostile = allPawns[i];
                if (hostile == pawn || hostile.Dead || hostile.Downed)
                    continue;

                if (!GenHostility.HostileTo(hostile, pawn))
                    continue;

                bool jobTargetsUs = hostile.CurJob != null && hostile.CurJob.targetA.Thing == pawn;
                bool meleeThreatIsUs = hostile.mindState?.meleeThreat == pawn;
                bool enemyTargetIsUs = hostile.mindState?.enemyTarget == pawn;

                if (!jobTargetsUs && !meleeThreatIsUs && !enemyTargetIsUs)
                    continue;

                if (TattooMagicSettings.EnableDebugLogging)
                {
                    Log.Message($"[TattooMagic] Wraithstep untargetable window forcing {hostile.LabelShort} to " +
                        $"drop its lock on {pawn.LabelShort}.");
                }

                if (hostile.mindState != null)
                {
                    hostile.mindState.enemyTarget = null;
                    hostile.mindState.meleeThreat = null;
                }

                if (hostile.CurJob != null)
                    hostile.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
            Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick", 0);
            Scribe_Values.Look(ref untargetableEndTick, "untargetableEndTick", 0);
        }
    }
}
