using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TattooMagic
{
    public class HediffCompProperties_SoulTetherEffect : HediffCompProperties
    {
        public float rangeTier1 = 10f;
        public float rangeTier2 = 16f;

        // Tier-independent (FR-018) — like Bastion Ward, Tier 2 does not
        // change the cooldown.
        public int cooldownDurationTicks = 1800;

        public int tier2Threshold = 15;

        public float tendQualityBonusTier1 = 0.15f;
        public float tendQualityBonusTier2 = 0.30f;

        public float tendSpeedBonusTier1 = 0.5f;
        public float tendSpeedBonusTier2 = 1.0f;

        // Shared across tiers (spec Assumptions) — only magnitude differs
        // between Tier 1 and Tier 2, not duration.
        public int tendBuffDurationTicks = 1800;

        // How long the "grab" visual/audio plays before the target actually
        // arrives (2026-09-19 addendum — see spec.md Clarifications). Kept
        // short and tunable on purpose: this is a battlefield rescue tool,
        // not a leisurely effect, so it must read as a quick yank rather
        // than a slow float. 20 ticks = 1/3 of a second at normal speed.
        public int pullEffectDurationTicks = 20;

        public string abilityIconPath = "UI/Commands/SoulTether";

        public HediffCompProperties_SoulTetherEffect()
        {
            compClass = typeof(HediffComp_SoulTetherEffect);
        }
    }

    // Soul Tether's own effect: a two-phase, pawn/corpse-targeted triggered
    // ability (research.md R1 of specs/017-soul-tether-tattoo-effect) — this
    // mod's first ability targeting a Pawn/Corpse rather than a ground cell
    // (contrast Wraithstep/Bastion Ward). Pulls the chosen friendly target
    // (living pawn or corpse) to a cell adjacent to the caster, then grants a
    // living target a flat tend speed/quality buff (never a corpse — FR-011).
    // Upgrades in place to a Tier 2 that extends max range and strengthens
    // the tend buff, while remaining strictly single-target at both tiers.
    // Implements IProvidesTattooGizmo (reused, feature 004) and
    // IHandlesOwnMasteryProgress (reused, feature 008) since a gizmo click
    // only opens targeting — it doesn't itself complete the activation.
    //
    // The actual relocation is deferred by a short, tunable window
    // (pullEffectDurationTicks, 2026-09-19 addendum) rather than happening
    // instantly on confirm — OnTargetConfirmed plays a "vanish" flash/line/
    // sound at the target's current position immediately and schedules the
    // real Position change; CompPostTick resolves it once the window
    // elapses, playing an "arrival" flash/sound at the destination. Reuses
    // vanilla's own proven Skip/Teleport psycast VFX+SFX (RimWorld.
    // CompAbilityEffect_Teleport, decompiled) rather than inventing new
    // motes/art — FleckDefOf.PsycastSkipFlashEntry/PsycastSkipInnerExit/
    // PsycastSkipOuterRingExit and SoundDefOf.Psycast_Skip_Entry/Exit are
    // all already-shipped vanilla assets.
    public class HediffComp_SoulTetherEffect : HediffComp, IProvidesTattooGizmo, IHandlesOwnMasteryProgress, IProvidesTattooTierProgress
    {
        // Search radius (cells) for a walkable landing cell adjacent to the
        // caster (contracts/pull-and-apply-effect-contract.md §1). Not
        // XML-tunable — an implementation detail of exactly where the target
        // lands, not a gameplay balance number.
        private const int LandingSearchRadius = 2;

        // Warm gold-white tint applied to every vanilla Fleck this effect
        // reuses (2026-09-19 addendum), so the pull reads as angelic/holy
        // ("Life Grip"-style) rather than the source assets' native
        // electric-blue "psychic skip" look. A placeholder aesthetic
        // choice, not a locked design — mirrors Bastion Ward's own
        // FillColor precedent (a private code constant, not XML-exposed,
        // since this is cosmetic rather than a gameplay balance number).
        private static readonly Color HolyTetherColor = new Color(1f, 0.92f, 0.65f);

        public TattooTierProgress tierState = new TattooTierProgress();

        public int cooldownEndTick;

        // Pending-pull state (2026-09-19 addendum) — set by OnTargetConfirmed,
        // consumed by ResolvePendingPullIfDue once pullResolveTick is
        // reached. pendingTarget is null whenever no pull is in flight (the
        // overwhelming majority of the time), which every check below gates
        // on first.
        private Thing pendingTarget;
        private IntVec3 pendingDestination;
        private int pullResolveTick = -1;
        private float pendingTendQualityBonus;
        private float pendingTendSpeedBonus;
        private int pendingTendBuffDurationTicks;

        public HediffCompProperties_SoulTetherEffect Props => (HediffCompProperties_SoulTetherEffect)props;

        private string ScopeKey => parent.def.defName;

        public int Tier => tierState.tier;

        public int ProgressionCounter => tierState.progressionCounter;

        public int? NextTierThreshold => tierState.tier >= 2
            ? (int?)null
            : (int)TattooEffectValues.Get(ScopeKey, "Tier2Threshold", Props.tier2Threshold);

        // The maximum selectable pull range for the comp's current tier,
        // resolved through TattooEffectValues — consulted by both the
        // target-validity check and the live range-ring draw callback.
        private float CurrentTierRange => tierState.tier >= 2
            ? TattooEffectValues.Get(ScopeKey, "RangeTier2", Props.rangeTier2)
            : TattooEffectValues.Get(ScopeKey, "RangeTier1", Props.rangeTier1);

        public Gizmo GetGizmo()
        {
            bool onCooldown = Find.TickManager.TicksGame < cooldownEndTick;

            return new Command_Action
            {
                defaultLabel = "Soul Tether",
                defaultDesc = "Pull a chosen friendly pawn (or their corpse) to your side, granting a tend speed/quality buff on arrival, then go on cooldown.",
                icon = ContentFinder<Texture2D>.Get(Props.abilityIconPath, false),
                action = BeginTargetSelection,
                Disabled = onCooldown,
                disabledReason = onCooldown
                    ? $"Soul Tether is recharging: {RemainingCooldownSeconds()}s remaining."
                    : null,
            };
        }

        private int RemainingCooldownSeconds()
        {
            return Mathf.CeilToInt((cooldownEndTick - Find.TickManager.TicksGame) / 60f);
        }

        // Opens RimWorld's own pawn/corpse targeter rather than completing
        // the activation immediately — the real effect only happens in
        // OnTargetConfirmed, once the player picks a target IsValidTarget
        // accepts. A cancelled session (Escape/right-click, or any other
        // path that ends targeting without a confirmed target) simply never
        // calls OnTargetConfirmed — no cooldown, no tier-progression
        // increment, and no mastery credit occur.
        private void BeginTargetSelection()
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Map == null)
                return;

            if (Find.TickManager.TicksGame < cooldownEndTick)
                return;

            var targetParams = new TargetingParameters
            {
                canTargetLocations = false,
                canTargetPawns = true,
                canTargetCorpses = true,
                canTargetSelf = false,
                neverTargetHostileFaction = true,
            };

            Find.Targeter.BeginTargeting(
                targetParams,
                action: OnTargetConfirmed,
                highlightAction: null,
                targetValidator: IsValidTarget,
                caster: pawn,
                onGuiAction: DrawRejectReasonLabel,
                onUpdateAction: DrawRangeRing);
        }

        // FR-002/FR-004/FR-005/FR-006: the single source of truth for a
        // valid target — reused unmodified by GetRejectReason so the two can
        // never disagree (mirrors HediffComp_WraithstepEffect's identical
        // pattern).
        private bool IsValidTarget(LocalTargetInfo target)
        {
            return GetRejectReason(target) == null;
        }

        // Same checks as IsValidTarget, in the same order, returning the
        // first failure's player-facing reason (or null once valid) — drawn
        // live near the cursor by DrawRejectReasonLabel.
        private string GetRejectReason(LocalTargetInfo target)
        {
            Pawn pawn = Pawn;
            Map map = pawn?.Map;
            if (map == null)
                return "No map.";

            Thing targetThing = target.Thing;
            if (targetThing == null)
                return "Not a valid target.";

            if (targetThing == pawn)
                return "Cannot target yourself.";

            if (!targetThing.Spawned || targetThing.Map != map)
                return "Not on the same map.";

            // research.md R2: a bare Corpse's own Thing.Faction is never set
            // by vanilla (Pawn.MakeCorpse never assigns it), so calling
            // GenHostility.HostileTo directly on a Corpse always evaluates
            // as "not hostile" regardless of who it actually was. Route
            // through InnerPawn, whose own Faction field is untouched by
            // death, instead.
            Pawn effectivePawn = targetThing as Pawn ?? (targetThing as Corpse)?.InnerPawn;
            if (effectivePawn == null)
                return "Not a valid target.";

            // FR-005 means colony or ally, not merely "not hostile" (issue #5):
            // HostileTo alone is false for factionless and neutral pawns too.
            // The HostileTo check stays so a berserk colonist is still rejected.
            if (!IsColonyOrAlly(effectivePawn, pawn) || GenHostility.HostileTo(effectivePawn, pawn))
                return "Not friendly.";

            if ((targetThing.Position - pawn.Position).LengthHorizontalSquared > CurrentTierRange * CurrentTierRange)
                return "Too far away.";

            return null;
        }

        // Same faction as the caster (colonists, slaves, colony animals) or a
        // faction formally allied with it. A dead pawn keeps its Faction, so
        // this works unchanged on a corpse's InnerPawn.
        private static bool IsColonyOrAlly(Pawn target, Pawn caster)
        {
            Faction targetFaction = target.Faction;
            Faction casterFaction = caster.Faction;
            if (targetFaction == null || casterFaction == null)
                return false;

            if (targetFaction == casterFaction)
                return true;

            return targetFaction.RelationWith(casterFaction, allowNull: true)?.kind == FactionRelationKind.Ally;
        }

        // Verse.Targeter's onUpdateAction callback — fires every Update
        // frame the targeter is open, so the range ring tracks the caster's
        // own position live (mirrors Wraithstep's own range-ring draw).
        private void DrawRangeRing(LocalTargetInfo target)
        {
            Pawn pawn = Pawn;
            if (pawn != null)
                GenDraw.DrawRadiusRing(pawn.Position, CurrentTierRange);
        }

        // Verse.Targeter's onGuiAction callback — fires every OnGUI frame
        // with whatever target is currently under the mouse; draws a short
        // reason label there whenever that target fails IsValidTarget.
        private void DrawRejectReasonLabel(LocalTargetInfo target)
        {
            string rejectReason = GetRejectReason(target);
            if (rejectReason != null)
                GenUI.DrawMouseAttachment(null, rejectReason);
        }

        // The actual activation, called only for a target IsValidTarget
        // already accepted. Reads whichever tier's numbers are current at
        // the moment of activation, so an activation made just before a
        // Tier 1->2 upgrade uses Tier 1's numbers and the next activation
        // uses Tier 2's, with no retroactive change (same precedent as every
        // prior triggered tattoo). Cooldown/tier-progression/mastery credit
        // all fire here, immediately — only the physical relocation and the
        // tend-buff grant are deferred to ResolvePendingPullIfDue, so the
        // ability reads as "used" the instant the player commits to a
        // target, matching every other triggered tattoo's convention.
        private void OnTargetConfirmed(LocalTargetInfo target)
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Map == null)
                return;

            if (Find.TickManager.TicksGame < cooldownEndTick)
                return;

            Thing targetThing = target.Thing;
            if (targetThing == null)
                return;

            bool atTier2 = tierState.tier >= 2;
            int now = Find.TickManager.TicksGame;

            // contracts/pull-and-apply-effect-contract.md §1 step 1: find a
            // walkable landing cell adjacent to the caster, falling back to
            // the caster's own cell if none is found nearby (FR-007) — never
            // fails the cast.
            bool foundCell = CellFinder.TryFindRandomCellNear(
                pawn.Position,
                pawn.Map,
                LandingSearchRadius,
                c => c.Standable(pawn.Map),
                out IntVec3 destination);

            if (!foundCell)
                destination = pawn.Position;

            // "Vanish" VFX/SFX at the target's current position, played
            // immediately — reuses vanilla's own Skip/Teleport psycast
            // assets (see class doc comment), tinted gold-white for a holy/
            // angelic feel (HolyTetherColor). A brief energy-tether line
            // between caster and target sells the "pull," not just a blink.
            Map map = pawn.Map;
            SpawnTintedTetherLine(pawn.DrawPos, targetThing.DrawPos, map);
            SpawnTintedFleck(targetThing.Position, map, FleckDefOf.PsycastSkipFlashEntry);
            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(targetThing.Position, map));

            // Defer the actual relocation and tend-buff grant to
            // ResolvePendingPullIfDue, pullEffectDurationTicks from now
            // (FR-007's fallback destination is already resolved above, so
            // it can't go stale even if the target moves in the interim —
            // see that method's own re-validation).
            pendingTarget = targetThing;
            pendingDestination = destination;
            pullResolveTick = now + (int)TattooEffectValues.Get(ScopeKey, "PullEffectDurationTicks", Props.pullEffectDurationTicks);

            if (targetThing is Pawn)
            {
                pendingTendQualityBonus = atTier2
                    ? TattooEffectValues.Get(ScopeKey, "TendQualityBonusTier2", Props.tendQualityBonusTier2)
                    : TattooEffectValues.Get(ScopeKey, "TendQualityBonusTier1", Props.tendQualityBonusTier1);
                pendingTendSpeedBonus = atTier2
                    ? TattooEffectValues.Get(ScopeKey, "TendSpeedBonusTier2", Props.tendSpeedBonusTier2)
                    : TattooEffectValues.Get(ScopeKey, "TendSpeedBonusTier1", Props.tendSpeedBonusTier1);
                pendingTendBuffDurationTicks = (int)TattooEffectValues.Get(ScopeKey, "TendBuffDurationTicks", Props.tendBuffDurationTicks);
            }

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
                Log.Message($"[TattooMagic] Soul Tether activated by {pawn.LabelShort}: tier={tierState.tier}, " +
                    $"target={targetThing.LabelShort}, destination={destination}, resolvesAt={pullResolveTick}, " +
                    $"tendBuffPending={targetThing is Pawn}, cooldown={cooldown}, progress={tierState.progressionCounter}.");
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            ResolvePendingPullIfDue();
        }

        // Fires once pullResolveTick is reached: performs the actual
        // relocation (contracts/pull-and-apply-effect-contract.md §1 step 2)
        // and the tend-buff grant that OnTargetConfirmed deferred, plus the
        // "arrival" VFX/SFX half of the Skip/Teleport combo. Defensively
        // no-ops (silently drops the pending pull, no error) if the target
        // was destroyed or despawned while in flight — e.g. a pulled corpse
        // fully decayed in that same sub-second window, or a mod interaction
        // removed the pawn — rather than throwing on a stale reference.
        private void ResolvePendingPullIfDue()
        {
            if (pendingTarget == null)
                return;

            if (Find.TickManager.TicksGame < pullResolveTick)
                return;

            Thing targetThing = pendingTarget;
            pendingTarget = null;

            if (targetThing.Destroyed || !targetThing.Spawned)
                return;

            targetThing.Position = pendingDestination;
            if (targetThing is Pawn livingTarget)
                livingTarget.Notify_Teleported();

            Map map = targetThing.Map;
            if (map != null)
            {
                SpawnTintedFleck(pendingDestination, map, FleckDefOf.PsycastSkipInnerExit);
                SpawnTintedFleck(pendingDestination, map, FleckDefOf.PsycastSkipOuterRingExit);
                SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(pendingDestination, map));
            }

            // FR-010/FR-011: only a living pawn gets the tend buff — never a
            // pulled corpse.
            if (targetThing is Pawn pulledPawn)
            {
                SoulTetherTendBuffUtility.GrantOrRefresh(
                    pulledPawn, pendingTendQualityBonus, pendingTendSpeedBonus, pendingTendBuffDurationTicks);
            }

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic] Soul Tether pull resolved: {targetThing.LabelShort} arrived at " +
                    $"{pendingDestination}, tendBuffGranted={targetThing is Pawn}.");
            }
        }

        // Reimplements FleckMaker.ConnectingLine's own math (research.md R9)
        // rather than calling it directly, since that convenience wrapper
        // has no color parameter — building the FleckCreationData by hand
        // lets HolyTetherColor tint the line via its instanceColor field.
        private static void SpawnTintedTetherLine(Vector3 start, Vector3 end, Map map)
        {
            Vector3 offset = end - start;
            float length = offset.MagnitudeHorizontal();

            FleckCreationData data = FleckMaker.GetDataStatic(start + offset * 0.5f, map, FleckDefOf.LineEMP);
            data.exactScale = new Vector3(length, 1f, 0.5f);
            data.rotation = Mathf.Atan2(0f - offset.z, offset.x) * 57.29578f;
            data.instanceColor = HolyTetherColor;

            map.flecks.CreateFleck(data);
        }

        // Same as FleckMaker.Static(IntVec3, Map, FleckDef), but tinted via
        // instanceColor (research.md R9) — that convenience wrapper has no
        // color parameter either.
        private static void SpawnTintedFleck(IntVec3 cell, Map map, FleckDef fleckDef)
        {
            FleckCreationData data = FleckMaker.GetDataStatic(cell.ToVector3Shifted(), map, fleckDef);
            data.instanceColor = HolyTetherColor;

            map.flecks.CreateFleck(data);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            tierState.ExposeData();
            Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick", 0);
            Scribe_References.Look(ref pendingTarget, "pendingTarget");
            Scribe_Values.Look(ref pendingDestination, "pendingDestination");
            Scribe_Values.Look(ref pullResolveTick, "pullResolveTick", -1);
            Scribe_Values.Look(ref pendingTendQualityBonus, "pendingTendQualityBonus", 0f);
            Scribe_Values.Look(ref pendingTendSpeedBonus, "pendingTendSpeedBonus", 0f);
            Scribe_Values.Look(ref pendingTendBuffDurationTicks, "pendingTendBuffDurationTicks", 0);
        }
    }
}
