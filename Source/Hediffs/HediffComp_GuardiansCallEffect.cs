using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TattooMagic
{
    public class HediffCompProperties_GuardiansCallEffect : HediffCompProperties
    {
        public float tauntRangeTier1 = 15f;
        public float tauntRangeTier2 = 15f;

        public int tauntDurationTicksTier1 = 300;
        public int tauntDurationTicksTier2 = 300;

        public int cooldownDurationTicksTier1 = 3600;
        public int cooldownDurationTicksTier2 = 3600;

        public int tier2Threshold = 15;

        public float armorOffsetTier2 = 0.3f;

        public string abilityIconPath = "UI/Commands/GuardiansCall";

        public HediffCompProperties_GuardiansCallEffect()
        {
            compClass = typeof(HediffComp_GuardiansCallEffect);
        }
    }

    // Guardian's Call's own effect: the mod's first triggered ability — an
    // activatable taunt (gizmo + cooldown) that forces nearby hostiles to
    // prioritize attacking this pawn for a duration, upgrading in place to a
    // Tier 2 that additionally grants a temporary armor buff for that same
    // duration, once enough activations have registered (PRD §5.4/§6).
    // Implements IProvidesTattooGizmo (new, R1) and IProvidesTattooStatOffset
    // (reused, R6).
    public class HediffComp_GuardiansCallEffect : HediffComp, IProvidesTattooGizmo, IProvidesTattooStatOffset, IProvidesTattooTierProgress
    {
        // How often, in ticks, the active taunt re-asserts itself against
        // already-engaged hostiles for the rest of its duration (see
        // CompTick). 30 ticks = 0.5s at normal speed — frequent enough that
        // a hostile pulled away by some other AI layer gets corrected back
        // before it can land more than a swing or two.
        private const int ReassertIntervalTicks = 30;

        public TattooTierProgress tierState = new TattooTierProgress();

        public int cooldownEndTick;
        public int tauntEndTick;

        public HediffCompProperties_GuardiansCallEffect Props => (HediffCompProperties_GuardiansCallEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        // The taunt range for the comp's current tier, resolved through
        // TattooEffectValues — consulted by the targeting patch, which has
        // no other reason to know about tier state directly.
        public float TauntRange => tierState.tier >= 2
            ? TattooEffectValues.Get(ScopeKey, "TauntRangeTier2", Props.tauntRangeTier2)
            : TattooEffectValues.Get(ScopeKey, "TauntRangeTier1", Props.tauntRangeTier1);

        public Gizmo GetGizmo()
        {
            bool onCooldown = Find.TickManager.TicksGame < cooldownEndTick;

            return new Command_Action
            {
                defaultLabel = "Guardian's Call",
                defaultDesc = "Force nearby hostiles to prioritize attacking you for a duration, then goes on cooldown.",
                icon = ContentFinder<Texture2D>.Get(Props.abilityIconPath, false),
                action = TryActivate,
                Disabled = onCooldown,
                disabledReason = onCooldown
                    ? $"Guardian's Call is recharging: {RemainingCooldownSeconds()}s remaining."
                    : null,
            };
        }

        private int RemainingCooldownSeconds()
        {
            return Mathf.CeilToInt((cooldownEndTick - Find.TickManager.TicksGame) / 60f);
        }

        // No-ops if still on cooldown (defensive; the gizmo itself already
        // prevents this per Edge Cases). Reads whichever tier's values are
        // current at the moment of activation, so an activation made just
        // before a Tier 1->2 upgrade uses Tier 1's numbers and the next
        // (post-upgrade) activation uses Tier 2's, with no retroactive
        // change to an already-running taunt (data-model.md State
        // Transitions).
        public void TryActivate()
        {
            int now = Find.TickManager.TicksGame;
            if (now < cooldownEndTick)
                return;

            bool atTier2 = tierState.tier >= 2;

            int duration = (int)(atTier2
                ? TattooEffectValues.Get(ScopeKey, "TauntDurationTicksTier2", Props.tauntDurationTicksTier2)
                : TattooEffectValues.Get(ScopeKey, "TauntDurationTicksTier1", Props.tauntDurationTicksTier1));
            int cooldown = (int)(atTier2
                ? TattooEffectValues.Get(ScopeKey, "CooldownDurationTicksTier2", Props.cooldownDurationTicksTier2)
                : TattooEffectValues.Get(ScopeKey, "CooldownDurationTicksTier1", Props.cooldownDurationTicksTier1));

            cooldownEndTick = now + cooldown;
            tauntEndTick = now + duration;

            GuardiansCallTauntRegistry.Register(this);

            // FR-004/FR-005: one qualifying event per activation, regardless
            // of how many hostiles the taunt actually redirects.
            tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic] Guardian's Call activated by {Pawn?.LabelShort}: tier={tierState.tier}, " +
                    $"range={TauntRange}, duration={duration}, cooldown={cooldown}, progress={tierState.progressionCounter}.");
            }

            ForceNearbyHostilesToReconsiderTarget();
        }

        // Re-asserts the yank for the whole active taunt window, not just
        // once at activation. Some AI layers (observed under Combat
        // Extended's own opportunistic-target-switch behavior) can pull an
        // already-yanked hostile off the taunter again mid-fight through a
        // path that never calls BestAttackTarget at all, so a one-time
        // activation-time correction isn't enough to hold aggro for the
        // full duration — whatever pulls a hostile away, this corrects it
        // back within half a second, without needing to know what that
        // other mechanism actually is. Gated on tauntEndTick first (the
        // common case once the taunt ends) so this is a no-op outside an
        // active window, same cheapness principle as the targeting patch's
        // own registry gate.
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            int now = Find.TickManager.TicksGame;
            if (now >= tauntEndTick)
                return;

            if (now % ReassertIntervalTicks != 0)
                return;

            ForceNearbyHostilesToReconsiderTarget();
        }

        // A real taunt yanks aggro immediately, not just on whatever hostile
        // target search happens to occur next. Patch_AttackTargetFinder_
        // BestAttackTarget_GuardiansCallTaunt only ever biases a *fresh*
        // targeting decision — a hostile already mid-combat with someone
        // else won't make a fresh decision on its own until that fight ends
        // for some other reason (target dies/flees/unreachable). So on
        // activation, force any already-engaged hostile in range off its
        // current target, compelling an immediate re-decision that the
        // targeting patch then correctly biases toward this pawn. This is a
        // one-off scan triggered by a player action, not a per-tick cost —
        // unlike the targeting patch itself, it doesn't need the registry's
        // hot-path cheapness.
        private void ForceNearbyHostilesToReconsiderTarget()
        {
            Map map = Pawn?.Map;
            if (map == null)
                return;

            float range = TauntRange;
            float rangeSq = range * range;
            IReadOnlyList<Pawn> allPawns = map.mapPawns.AllPawnsSpawned;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn hostile = allPawns[i];
                if (hostile == Pawn || hostile.Dead || hostile.Downed)
                    continue;

                if (!GenHostility.HostileTo(hostile, Pawn))
                    continue;

                float distSq = (hostile.Position - Pawn.Position).LengthHorizontalSquared;
                if (distSq > rangeSq)
                    continue;

                // Already fully locked onto us on every field that matters —
                // nothing to correct. Checked across mindState.enemyTarget
                // (general AI bookkeeping), mindState.meleeThreat (the
                // melee-specific "who am I actually swinging at" field,
                // separate from enemyTarget), and the running job's own
                // targetA (what JobDriver_AttackMelee's toils actually read
                // each swing) — investigation showed enemyTarget alone can
                // be "correct" while the pawn keeps hitting someone else
                // entirely, because melee combat re-derives its real target
                // from these other fields, not from enemyTarget.
                bool jobAlreadyTargetsUs = hostile.CurJob != null && hostile.CurJob.targetA.Thing == Pawn;
                bool meleeThreatAlreadyUs = hostile.mindState.meleeThreat == Pawn;
                bool enemyTargetAlreadyUs = hostile.mindState.enemyTarget == Pawn;
                if (jobAlreadyTargetsUs && meleeThreatAlreadyUs && enemyTargetAlreadyUs)
                    continue;

                // Never force an unreachable/illegal target — same bar the
                // targeting patch itself requires.
                if (!hostile.CanReach(Pawn, PathEndMode.Touch, Danger.Some))
                    continue;

                if (TattooMagicSettings.EnableDebugLogging)
                {
                    Thing previousTarget = hostile.CurJob?.targetA.Thing ?? hostile.mindState.enemyTarget ?? hostile.mindState.meleeThreat;
                    Log.Message($"[TattooMagic] Guardian's Call taunt yanking {hostile.LabelShort} off its current " +
                        $"target ({previousTarget?.LabelShort ?? "none"}) onto {Pawn.LabelShort}.");
                }

                // Directly mutate the fields melee combat actually reads,
                // in place, rather than ending the job — interrupting was
                // what let some other AI layer re-decide (and re-drift)
                // in the first place. This takes effect on the very next
                // swing without a job restart.
                hostile.mindState.enemyTarget = Pawn;
                hostile.mindState.meleeThreat = Pawn;
                if (hostile.CurJob != null)
                    hostile.CurJob.targetA = Pawn;
            }
        }

        // FR-006/FR-007: only contributes an armor offset while at Tier 2
        // AND this activation's taunt/buff window is still open — 0f at
        // Tier 1, and 0f at Tier 2 between activations. Routes through the
        // same generic StatPart_TattooEffectOffset every other tattoo's stat
        // offset uses, so it stacks additively with any future tattoo
        // registering its own contribution to these StatDefs (research.md
        // R6, SC-012).
        public float GetStatOffset(StatDef stat)
        {
            if (stat != StatDefOf.ArmorRating_Sharp && stat != StatDefOf.ArmorRating_Blunt && stat != StatDefOf.ArmorRating_Heat)
                return 0f;

            if (tierState.tier < 2 || Find.TickManager.TicksGame >= tauntEndTick)
                return 0f;

            return TattooEffectValues.Get(ScopeKey, "ArmorOffsetTier2", Props.armorOffsetTier2);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
            Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick", 0);
            Scribe_Values.Look(ref tauntEndTick, "tauntEndTick", 0);
        }
    }
}
