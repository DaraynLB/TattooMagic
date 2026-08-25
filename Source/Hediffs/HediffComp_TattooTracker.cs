using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TattooMagic
{
    public class HediffCompProperties_TattooTracker : HediffCompProperties
    {
        // PRD §9 balance-pass placeholders — deliberately small for fast
        // manual testing rather than "plausible-looking" (research.md R8).
        public int slot2Threshold = 3;
        public int slot3Threshold = 8;

        public HediffCompProperties_TattooTracker()
        {
            compClass = typeof(HediffComp_TattooTracker);
        }
    }

    // Per-pawn bookkeeping for the tattoo ritual system: how many tattoo
    // slots this pawn has unlocked (grown by a separate, not-yet-built
    // progression feature; this feature only reads it), which tattoos are
    // currently applied, and which tattoo (if any) is queued to be applied
    // next via the ritual station.
    public class HediffComp_TattooTracker : HediffComp
    {
        public int slotCapacity = 1;

        // Pawn-wide count of triggered-tattoo ability activations, regardless
        // of which triggered tattoo (feature 006).
        public int masteryProgress;

        public List<TattooMagicDef> appliedTattoos = new List<TattooMagicDef>();

        public TattooMagicDef pendingRitualTattoo;

        public HediffCompProperties_TattooTracker Props => (HediffCompProperties_TattooTracker)props;

        public int FreeSlots => slotCapacity - appliedTattoos.Count;

        // Mirrors every tattoo-specific comp's own ScopeKey => parent.def.defName
        // (research.md R3); for this comp always resolves to "TattooMagic_Tracker".
        private string ScopeKey => parent.def.defName;

        public bool HasTattoo(TattooMagicDef tattoo) => appliedTattoos.Contains(tattoo);

        // Self-heals appliedTattoos against the pawn's actual hediffs, keyed
        // off TattooMagicDef.appliedHediff — the same reverse-lookup pattern
        // TattooHediffRemovalGuard already uses. appliedTattoos is normally
        // only ever written by JobDriver_TattooRitual's own ResolveRitual; a
        // tattoo hediff granted any other way (Dev Mode's "give hediff", a
        // save imported from elsewhere) would otherwise never appear here,
        // silently overstating FreeSlots and letting HasTattoo miss it
        // entirely (playtesting finding, feature 006). Only ever adds,
        // never removes — tattoo removal isn't a feature yet (PRD §3).
        public void SyncAppliedTattoosFromActualHediffs()
        {
            if (Pawn?.health?.hediffSet == null)
                return;

            List<TattooMagicDef> allTattoos = DefDatabase<TattooMagicDef>.AllDefsListForReading;
            for (int i = 0; i < allTattoos.Count; i++)
            {
                TattooMagicDef tattoo = allTattoos[i];
                if (tattoo.appliedHediff == null || appliedTattoos.Contains(tattoo))
                    continue;

                if (Pawn.health.hediffSet.HasHediff(tattoo.appliedHediff))
                    appliedTattoos.Add(tattoo);
            }
        }

        // Called once per real triggered-tattoo activation via the wrapped
        // gizmo action in Patch_Pawn_GetGizmos_TattooGizmos (research.md R1).
        public void RegisterMasteryActivation()
        {
            masteryProgress++;

            if (TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic] {Pawn?.LabelShort} tattoo-mastery progress: {masteryProgress} (slot capacity {slotCapacity}).");

            CheckSlotThresholds();
        }

        // Also called directly by the Dev Mode debug action after a manual
        // progress jump, so a tester can reach any progress value (including
        // a single jump across both thresholds at once) without waiting out
        // cooldowns (research.md R9).
        public void DevAddMasteryProgress(int amount)
        {
            masteryProgress += amount;
            CheckSlotThresholds();
        }

        // Idempotent, deterministic threshold crossing (research.md R4):
        // grants at most one slot per loop iteration, defensively handling a
        // single jump in masteryProgress that clears both thresholds at once
        // by granting each slot in turn with its own notification, rather
        // than silently skipping straight to 3 slots with none.
        private void CheckSlotThresholds()
        {
            while (slotCapacity < 3 && masteryProgress >= ThresholdForNextSlot(slotCapacity))
            {
                slotCapacity++;
                Messages.Message($"{Pawn.LabelShortCap} has grown skilled enough with their tattoos to support {slotCapacity} tattoo slots!", Pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        private int ThresholdForNextSlot(int currentCapacity)
        {
            if (currentCapacity == 1)
                return (int)TattooEffectValues.Get(ScopeKey, "Slot2Threshold", Props.slot2Threshold);

            return (int)TattooEffectValues.Get(ScopeKey, "Slot3Threshold", Props.slot3Threshold);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref slotCapacity, "slotCapacity", 1);
            Scribe_Values.Look(ref masteryProgress, "masteryProgress", 0);
            Scribe_Collections.Look(ref appliedTattoos, "appliedTattoos", LookMode.Def);
            Scribe_Defs.Look(ref pendingRitualTattoo, "pendingRitualTattoo");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && appliedTattoos == null)
                appliedTattoos = new List<TattooMagicDef>();
        }
    }
}
