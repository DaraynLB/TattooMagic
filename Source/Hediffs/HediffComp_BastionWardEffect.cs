using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_BastionWardEffect : HediffCompProperties
    {
        public int durationTicks = 480;

        public float radiusTier1 = 4.5f;
        public float radiusTier2 = 6.5f;

        public float rangeTier1 = 12f;
        public float rangeTier2 = 16f;

        // Tier-independent (FR-014) — unlike most triggered tattoos, Tier 2
        // does not change the cooldown.
        public int cooldownDurationTicks = 3600;

        public int tier2Threshold = 15;

        // Tier 2 only — no Tier 1 equivalent (Tier 1 grants no heal at all).
        public float healAmountPerTickTier2 = 1f;
        public int healIntervalTicksTier2 = 60;

        public string abilityIconPath = "UI/Commands/BastionWard";

        // Off by default (2026-09-19) — the cast-time melee-attacker
        // rescue-eject (EjectMeleeAttackersFromIncomingZone) proved by far
        // the hardest part of this feature to verify (see that method's own
        // comment for the detection-signal history), so it's an XML-tunable
        // opt-in rather than mandatory behavior, matching every other
        // per-tattoo knob on this comp rather than a mod-wide Mod Settings
        // checkbox (superseded — was briefly TattooMagicSettings.
        // EnableBastionWardRescueEject before being moved here as the
        // single source of truth). The ward's core targeting/zone/damage-
        // negation/tiering-and-healing behavior is complete and unaffected
        // either way.
        public bool enableRescueEject = false;

        public HediffCompProperties_BastionWardEffect()
        {
            compClass = typeof(HediffComp_BastionWardEffect);
        }
    }

    // Bastion Ward's own effect: a two-phase, ground-targeted triggered
    // ability (research.md R1, reusing feature 008's cell-targeted-ability
    // contract) that spawns a ground-fixed, caster-independent zone
    // (GameComponent_BastionWardZones, research.md R4) rather than a
    // self-buff — this mod's first tattoo whose protection isn't carried by
    // the protected pawn's own hediffs. At the moment of casting, any
    // hostile currently in melee with an ally about to be inside the new
    // zone is displaced outside it (research.md R5). Upgrades in place to a
    // Tier 2 that grants a larger radius and a periodic heal for anyone
    // inside, once enough activations have registered (PRD §5.4).
    // Implements IProvidesTattooGizmo (reused, feature 004) and
    // IHandlesOwnMasteryProgress (reused, feature 008) since a gizmo click
    // only opens targeting — it doesn't itself complete the activation.
    // Deliberately does NOT implement IOnIncomingDamageTattooEffect — the
    // comp itself never absorbs damage; the zone it spawns does, via
    // GameComponent_BastionWardZones.TryAbsorb (research.md R2).
    public class HediffComp_BastionWardEffect : HediffComp, IProvidesTattooGizmo, IHandlesOwnMasteryProgress, IProvidesTattooTierProgress
    {
        // Search radius (cells) for a walkable cell outside the new zone to
        // eject a melee attacker to (research.md R5). Not XML-tunable —
        // an implementation detail of where an ejected hostile lands, not a
        // gameplay balance number per FR-016's explicit tunable list.
        private const int DisplacementSearchRadius = 10;

        public TattooTierProgress tierState = new TattooTierProgress();

        public int cooldownEndTick;

        public HediffCompProperties_BastionWardEffect Props => (HediffCompProperties_BastionWardEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        // The zone radius and max cast range for the comp's current tier,
        // resolved through TattooEffectValues — consulted by the targeting
        // validator/preview and by OnDestinationConfirmed alike.
        private float CurrentTierRadius => tierState.tier >= 2
            ? TattooEffectValues.Get(ScopeKey, "RadiusTier2", Props.radiusTier2)
            : TattooEffectValues.Get(ScopeKey, "RadiusTier1", Props.radiusTier1);

        private float CurrentTierRange => tierState.tier >= 2
            ? TattooEffectValues.Get(ScopeKey, "RangeTier2", Props.rangeTier2)
            : TattooEffectValues.Get(ScopeKey, "RangeTier1", Props.rangeTier1);

        public Gizmo GetGizmo()
        {
            bool onCooldown = Find.TickManager.TicksGame < cooldownEndTick;

            return new Command_Action
            {
                defaultLabel = "Bastion Ward",
                defaultDesc = "Drop a ground-fixed ward at a targeted location, fully negating melee and ranged damage to allies inside for a duration, then go on cooldown.",
                icon = ContentFinder<Texture2D>.Get(Props.abilityIconPath, false),
                action = BeginTargetSelection,
                Disabled = onCooldown,
                disabledReason = onCooldown
                    ? $"Bastion Ward is recharging: {RemainingCooldownSeconds()}s remaining."
                    : null,
            };
        }

        private int RemainingCooldownSeconds()
        {
            return Mathf.CeilToInt((cooldownEndTick - Find.TickManager.TicksGame) / 60f);
        }

        // Opens RimWorld's own cell targeter rather than completing the
        // activation immediately (research.md R1) — the real effect only
        // happens in OnDestinationConfirmed, once the player picks a cell
        // IsValidZoneCenter accepts. A cancelled session (Escape/right-
        // click, or any other path that ends targeting without a confirmed
        // cell) simply never calls OnDestinationConfirmed — no cooldown, no
        // tier-progression increment, and no mastery credit occur. No-ops
        // if still on cooldown (defensive; the gizmo already prevents this).
        private void BeginTargetSelection()
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Map == null)
                return;

            if (Find.TickManager.TicksGame < cooldownEndTick)
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
                targetValidator: IsValidZoneCenter,
                caster: pawn,
                onGuiAction: DrawRejectReasonLabel,
                onUpdateAction: DrawZoneRadiusPreview);
        }

        // FR-001a: the single source of truth for a valid zone center —
        // reused unmodified by GetRejectReason so the two can never
        // disagree (mirrors HediffComp_WraithstepEffect's identical
        // pattern). No walkable/unoccupied check, unlike Wraithstep's own
        // validator (explicitly not reused per the cell-targeted-ability
        // contract's §3 caveat) — an AoE center isn't "occupied" the way a
        // teleport destination must be.
        private bool IsValidZoneCenter(LocalTargetInfo target)
        {
            return GetRejectReason(target) == null;
        }

        // Same checks as IsValidZoneCenter, in the same order, returning
        // the first failure's player-facing reason (or null once valid) —
        // drawn live near the cursor by DrawRejectReasonLabel.
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
            if ((cell - pawn.Position).LengthHorizontalSquared > CurrentTierRange * CurrentTierRange)
                return "Too far away.";

            return null;
        }

        // Verse.Targeter's onUpdateAction callback — fires every Update
        // frame the targeter is open, so the radius preview tracks the
        // cursor's current cell live.
        private void DrawZoneRadiusPreview(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(target.Cell, CurrentTierRadius);
        }

        // Verse.Targeter's onGuiAction callback — fires every OnGUI frame
        // with whatever cell is currently under the mouse; draws a short
        // reason label there whenever that cell fails IsValidZoneCenter.
        private void DrawRejectReasonLabel(LocalTargetInfo target)
        {
            string rejectReason = GetRejectReason(target);
            if (rejectReason != null)
                GenUI.DrawMouseAttachment(null, rejectReason);
        }

        // The actual activation, called only for a destination
        // IsValidZoneCenter already accepted. Reads whichever tier's
        // numbers are current at the moment of activation, so an
        // activation made just before a Tier 1->2 upgrade uses Tier 1's
        // numbers and the next activation uses Tier 2's, with no
        // retroactive change to an already-spawned zone (same precedent as
        // every prior triggered tattoo).
        private void OnDestinationConfirmed(LocalTargetInfo target)
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Map == null)
                return;

            bool atTier2 = tierState.tier >= 2;
            int now = Find.TickManager.TicksGame;

            float radius = atTier2
                ? TattooEffectValues.Get(ScopeKey, "RadiusTier2", Props.radiusTier2)
                : TattooEffectValues.Get(ScopeKey, "RadiusTier1", Props.radiusTier1);

            // Before the zone is registered, so the ejection check ("who is
            // currently melee-locked onto an ally that will end up inside")
            // evaluates against the zone's about-to-exist footprint, not a
            // zone that already excludes the attacker by its own presence
            // (research.md R5, FR-006).
            EjectMeleeAttackersFromIncomingZone(target.Cell, radius);

            int duration = (int)TattooEffectValues.Get(ScopeKey, "DurationTicks", Props.durationTicks);
            int healInterval = (int)TattooEffectValues.Get(ScopeKey, "HealIntervalTicksTier2", Props.healIntervalTicksTier2);

            var zone = new BastionWardZone
            {
                map = pawn.Map,
                position = target.Cell,
                radius = radius,
                expireTick = now + duration,
                owningFaction = pawn.Faction,
                heals = atTier2,
                healAmountPerTick = atTier2
                    ? TattooEffectValues.Get(ScopeKey, "HealAmountPerTickTier2", Props.healAmountPerTickTier2)
                    : 0f,
                healIntervalTicks = healInterval,
                nextHealTick = now + healInterval,
            };
            GameComponent_BastionWardZones.Get()?.RegisterZone(zone);

            int cooldown = (int)TattooEffectValues.Get(ScopeKey, "CooldownDurationTicks", Props.cooldownDurationTicks);
            cooldownEndTick = now + cooldown;

            // One qualifying event per successful activation.
            tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

            // This comp is responsible for its own mastery credit
            // (IHandlesOwnMasteryProgress opts it out of the shared
            // click-time wrap), fired only here — a genuine, resolved
            // activation — never on a click alone.
            TattooTrackerUtility.GetTracker(pawn)?.RegisterMasteryActivation();

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic] Bastion Ward activated by {pawn.LabelShort}: tier={tierState.tier}, " +
                    $"center={target.Cell}, radius={radius}, duration={duration}, heals={atTier2}, " +
                    $"cooldown={cooldown}, progress={tierState.progressionCounter}.");
            }
        }

        // Touch/adjacent range for the enemyTarget fallback below — cardinal
        // (1.0) and diagonal (~1.41) neighbors both qualify, nothing farther.
        private const float TouchRangeForEjectFallback = 1.5f;

        // Gated behind Props.enableRescueEject (XML-tunable, off by default,
        // 2026-09-19) — this sub-mechanic proved by far the hardest part of
        // Bastion Ward to verify (see the detection-signal history below),
        // so it's opt-in rather than mandatory; the ward's core targeting/
        // zone/damage-negation/tiering-and-healing behavior is complete and
        // unaffected regardless of this field.
        //
        // At the moment of casting, displaces any hostile currently in
        // melee combat with an ally who will end up inside the new zone
        // (FR-006), excluding ranged attackers (FR-008). Primary signal is
        // mindState.meleeThreat (research.md R5) — precise, but live
        // testing against 6+ simultaneous attackers in one brawl (2026-09-19
        // session) showed only 1-2 of them actually held meleeThreat at the
        // exact cast tick; the rest were silently skipped even though they
        // were actively landing melee hits moments before/after. Fallback:
        // mindState.enemyTarget == ally plus a touch-range distance check
        // (research.md R5's originally-documented CE fallback, repurposed
        // here for the same underlying problem — meleeThreat being a
        // narrower snapshot than "currently fighting in melee" — rather
        // than the CE-absent-signal case it was first written for) catches
        // the rest of the scrum while still excluding ranged attackers,
        // since a non-adjacent ranged attacker fails the distance check.
        // A one-time reaction to this cast only (FR-007) — never called
        // again for hostiles that enter later. If no valid walkable cell
        // outside the radius exists for a given hostile, it is simply left
        // in place (spec Clarifications, FR-006's fallback) — its attacks
        // against the ally are still fully negated once the zone registers
        // (GameComponent_BastionWardZones.TryAbsorb), so the ward's core
        // protection is unaffected either way.
        private void EjectMeleeAttackersFromIncomingZone(IntVec3 center, float radius)
        {
            if (!Props.enableRescueEject)
                return;

            Pawn pawn = Pawn;
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

                Pawn ally = hostile.mindState?.meleeThreat as Pawn;
                if (ally == null &&
                    hostile.mindState?.enemyTarget is Pawn targeted &&
                    hostile.Position.DistanceTo(targeted.Position) <= TouchRangeForEjectFallback)
                {
                    ally = targeted;
                }

                if (ally == null)
                    continue;

                if (GenHostility.HostileTo(ally, pawn))
                    continue;

                if (ally.Position.DistanceTo(center) > radius)
                    continue;

                bool ejected = CellFinder.TryFindRandomCellNear(
                    hostile.Position,
                    map,
                    DisplacementSearchRadius,
                    c => c.Standable(map) && !c.Fogged(map) && c.DistanceTo(center) > radius,
                    out IntVec3 result);

                if (ejected)
                {
                    hostile.Position = result;
                    hostile.Notify_Teleported();

                    if (TattooMagicSettings.EnableDebugLogging)
                    {
                        Log.Message($"[TattooMagic DEBUG] Bastion Ward ejected {hostile.LabelShort} (meleeing " +
                            $"{ally.LabelShort}) from the incoming ward to {result}.");
                    }
                }
                else if (TattooMagicSettings.EnableDebugLogging)
                {
                    Log.Message($"[TattooMagic DEBUG] Bastion Ward found no valid cell to eject {hostile.LabelShort} " +
                        $"(meleeing {ally.LabelShort}) to — left in place per spec fallback.");
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
            Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick", 0);
        }
    }
}
