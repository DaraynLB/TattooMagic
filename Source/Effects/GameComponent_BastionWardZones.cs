using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // This mod's first ground-fixed, caster-independent, duration-driven
    // zone registry (research.md R4, contracts/ground-fixed-aoe-zone-
    // contract.md). Mirrors GameComponent_PhoenixRegistry's (feature 014)
    // self-registering shape — needs no manual registration anywhere;
    // RimWorld's own Game.FillComponents() auto-discovers it by reflection.
    //
    // Bastion Ward is the first tattoo whose protection isn't carried by
    // the protected pawn's own hediffs (research.md R2) — most pawns
    // standing inside a ward never wear the tattoo at all. TryAbsorb is
    // consulted directly by Patch_Pawn_PreApplyDamage_TattooDamageAbsorb
    // (one small, additive block appended to that shared prefix) rather
    // than through the per-hediff-comp IOnIncomingDamageTattooEffect
    // dispatch, which has no way to reach a comp that doesn't belong to the
    // pawn being damaged.
    public class GameComponent_BastionWardZones : GameComponent
    {
        public List<BastionWardZone> activeZones = new List<BastionWardZone>();

        public GameComponent_BastionWardZones(Game game)
        {
        }

        public static GameComponent_BastionWardZones Get() => Current.Game?.GetComponent<GameComponent_BastionWardZones>();

        public void RegisterZone(BastionWardZone zone)
        {
            if (zone == null)
                return;

            activeZones.Add(zone);

            if (TattooMagicSettings.EnableDebugLogging)
            {
                Log.Message($"[TattooMagic DEBUG] Bastion Ward zone registered at {zone.position} " +
                    $"(radius={zone.radius}, expireTick={zone.expireTick}, heals={zone.heals}, " +
                    $"owningFaction={zone.owningFaction?.Name ?? "none"}).");
            }
        }

        // research.md R2/R9: re-evaluates live on every call — never caches
        // "is pawn P inside zone Z," so a mid-ward faction change (a mental
        // break, a capture) resolves correctly with no extra bookkeeping
        // (spec Edge Cases). Returns true (full absorb, dinfo left
        // unmodified) for the first matching zone; a pawn inside two
        // overlapping wards only needs one of them to protect it.
        public bool TryAbsorb(Pawn defender, ref DamageInfo dinfo)
        {
            if (defender?.Map == null)
                return false;

            // Deliberately no damage-type filtering here (2026-09-19,
            // superseding an earlier same-session attempt to exclude
            // explosive/Flame/Burn damage per the original spec.md
            // Assumptions wording) — the player confirmed full
            // invulnerability (any damage instance reaching Pawn.
            // PreApplyDamage is absorbed, environmental/explosive included)
            // is the intended behavior, not attack-damage-only. See spec.md's
            // Clarifications log, session 2026-09-19.
            for (int i = 0; i < activeZones.Count; i++)
            {
                BastionWardZone zone = activeZones[i];
                if (zone.map != defender.Map)
                    continue;

                if (defender.Position.DistanceTo(zone.position) > zone.radius)
                    continue;

                if (GenHostility.HostileTo(defender, zone.owningFaction))
                    continue;

                if (TattooMagicSettings.EnableDebugLogging)
                {
                    Log.Message($"[TattooMagic DEBUG] Bastion Ward zone at {zone.position} absorbed " +
                        $"{dinfo.Def?.defName} ({dinfo.Amount:F1} dmg) aimed at {defender.LabelShort}.");
                }

                return true;
            }

            return false;
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (activeZones.Count == 0)
                return;

            int now = Find.TickManager.TicksGame;

            for (int i = activeZones.Count - 1; i >= 0; i--)
            {
                BastionWardZone zone = activeZones[i];

                if (now >= zone.expireTick)
                {
                    activeZones.RemoveAt(i);

                    if (TattooMagicSettings.EnableDebugLogging)
                        Log.Message($"[TattooMagic DEBUG] Bastion Ward zone at {zone.position} expired.");

                    continue;
                }

                if (!zone.heals || now < zone.nextHealTick || zone.map == null)
                    continue;

                ApplyHealTick(zone);
                zone.nextHealTick = now + zone.healIntervalTicks;
            }
        }

        // Tier 2 only (a Tier 1 zone always has heals == false, so this is
        // never called for one). No existing tattoo heals any pawn other
        // than its own wearer — this is the first area-enumeration heal
        // loop in the mod (research.md R6), reusing TattooHealingUtility.
        // HealWorstInjury (feature 007) per-pawn.
        private void ApplyHealTick(BastionWardZone zone)
        {
            IReadOnlyList<Pawn> allPawns = zone.map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn pawn = allPawns[i];
                if (pawn.Dead)
                    continue;

                if (pawn.Position.DistanceTo(zone.position) > zone.radius)
                    continue;

                if (GenHostility.HostileTo(pawn, zone.owningFaction))
                    continue;

                float healed = TattooHealingUtility.HealWorstInjury(pawn, zone.healAmountPerTick);

                if (healed > 0f && TattooMagicSettings.EnableDebugLogging)
                {
                    Log.Message($"[TattooMagic DEBUG] Bastion Ward zone at {zone.position} healed " +
                        $"{pawn.LabelShort} for {healed:F1}.");
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref activeZones, "activeZones", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && activeZones == null)
                activeZones = new List<BastionWardZone>();
        }
    }
}
