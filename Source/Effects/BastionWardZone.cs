using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // Plain, ground-fixed zone state — not a Thing, not spawned on the map.
    // Deliberately holds no back-reference to its caster or to the
    // HediffComp that created it (contracts/ground-fixed-aoe-zone-
    // contract.md §1): this is what makes "the ward keeps running even
    // after the caster is downed, killed, or loses the tattoo" true by
    // construction rather than by explicit handling (spec Edge Cases,
    // FR-020). Tracked in a list on GameComponent_BastionWardZones.
    public class BastionWardZone : IExposable
    {
        public Map map;
        public IntVec3 position;
        public float radius;
        public int expireTick;
        public Faction owningFaction;

        // Tier-2-only healing; false/zeroed for every Tier 1 zone.
        public bool heals;
        public float healAmountPerTick;
        public int healIntervalTicks;
        public int nextHealTick;

        // Lazily computed, not persisted — position/radius never change
        // after a zone is created, so this is safe to cache once (rather
        // than recomputing every draw frame) and to simply recompute on
        // first access after a save/reload, exactly like GameComponent_
        // PhoenixRegistry's own IsAtCap derived-property precedent. Consumed
        // only by MapComponent_BastionWardZoneOverlay's purely-visual field
        // draw — never by TryAbsorb/the heal pass, both of which use a
        // plain distance check instead.
        private List<IntVec3> fieldCellsCache;

        public List<IntVec3> FieldCells =>
            fieldCellsCache ?? (fieldCellsCache = GenRadial.RadialCellsAround(position, radius, true).ToList());

        public void ExposeData()
        {
            Scribe_References.Look(ref map, "map");
            Scribe_Values.Look(ref position, "position");
            Scribe_Values.Look(ref radius, "radius");
            Scribe_Values.Look(ref expireTick, "expireTick");
            Scribe_References.Look(ref owningFaction, "owningFaction");
            Scribe_Values.Look(ref heals, "heals");
            Scribe_Values.Look(ref healAmountPerTick, "healAmountPerTick");
            Scribe_Values.Look(ref healIntervalTicks, "healIntervalTicks");
            Scribe_Values.Look(ref nextHealTick, "nextHealTick");
        }
    }
}
