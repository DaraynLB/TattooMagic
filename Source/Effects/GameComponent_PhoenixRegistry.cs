using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TattooMagic
{
    // This mod's first GameComponent — needs no manual registration
    // anywhere: RimWorld's own Game.FillComponents() already auto-discovers
    // every non-abstract GameComponent subclass by reflection during
    // Game.ExposeData() and instantiates it if not already present
    // (research.md R11). Doubles as the faction-wide 3-tattoo-cap registry
    // (FR-001-FR-006) and the sole driver of Phoenix's own multi-day
    // wait/retry timer (research.md R3) — a normal HediffComp goes silent
    // the instant its pawn dies, and the closest vanilla precedent
    // (Hediff_DeathRefusal's Corpse.TickRare hook) can't survive the
    // cremation exception destroying the Corpse Thing mid-wait.
    public class GameComponent_PhoenixRegistry : GameComponent
    {
        private const int SweepIntervalTicks = 2000;

        public List<Pawn> registeredWearers = new List<Pawn>();

        private int nextSweepTick;

        public GameComponent_PhoenixRegistry(Game game)
        {
        }

        public static GameComponent_PhoenixRegistry Get() => Current.Game?.GetComponent<GameComponent_PhoenixRegistry>();

        public bool IsAtCap => registeredWearers.Count >= (int)TattooEffectValues.Get(
            TattooMagicDefOf.TattooMagic_Hediff_Phoenix.defName, "FactionCap", DefaultFactionCap);

        // Mirrors HediffCompProperties_PhoenixEffect.factionCap's own default
        // — this component has no XML Def of its own to carry that default
        // on, so the value is duplicated here as a plain constant; both
        // ultimately read through the same TattooEffectValues override key,
        // so a future settings-UI override still applies correctly to both.
        private const int DefaultFactionCap = 3;

        public void Register(Pawn pawn)
        {
            if (pawn == null || registeredWearers.Contains(pawn))
                return;

            registeredWearers.Add(pawn);

            if (TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic DEBUG] Phoenix registry: registered {pawn.LabelShort} (now {registeredWearers.Count}).");
        }

        public void Unregister(Pawn pawn)
        {
            if (registeredWearers.Remove(pawn) && TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic DEBUG] Phoenix registry: unregistered {pawn?.LabelShort} (now {registeredWearers.Count}).");
        }

        // Self-heal against registeredWearers drifting out of sync with the
        // actual TattooMagic_Hediff_Phoenix hediffs in the save — found via
        // live testing (2026-08-23): after a save/reload, a dead wearer
        // whose corpse was still sitting on a home map (not yet handed off
        // to WorldPawns) was missing from the reloaded registeredWearers
        // list, silently under-counting the cap. Root cause not fully
        // pinned down (Scribe LookMode.Reference *should* resolve any
        // tracked Pawn regardless of Spawned/Dead state), but this
        // reconciliation fixes the symptom regardless of cause, mirroring
        // HediffComp_TattooTracker.SyncAppliedTattoosFromActualHediffs's own
        // established precedent (feature 006) for exactly this kind of
        // "derived tracking state might drift from the real hediffs"
        // concern. Deliberately does NOT rely on PawnsFinder's own
        // "AliveOrDead" enumerations for the dead half of this scan —
        // decompile-confirmed that MapPawns.AllPawnsUnspawned explicitly
        // filters out any Dead pawn, so a corpse sitting on a home map
        // (as opposed to one already tracked in WorldPawns) would still be
        // missed by those; every map's own Corpse things are scanned
        // directly instead, so a mid-wait Phoenix corpse is never missed
        // regardless of where the underlying persistence gap turns out to
        // be.
        public void SyncFromActualHediffs()
        {
            List<Pawn> aliveOrWorldDead = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead;
            for (int i = 0; i < aliveOrWorldDead.Count; i++)
                TryRegisterIfCarrier(aliveOrWorldDead[i]);

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                List<Thing> corpses = maps[i].listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                for (int j = 0; j < corpses.Count; j++)
                {
                    if (corpses[j] is Corpse corpse && corpse.InnerPawn != null)
                        TryRegisterIfCarrier(corpse.InnerPawn);
                }
            }
        }

        private void TryRegisterIfCarrier(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || registeredWearers.Contains(pawn))
                return;

            // FR-001's cap is scoped to "your whole faction" — skip anyone
            // who isn't player-faction (e.g. a hostile pawn that somehow
            // ended up with the hediff via Dev Mode testing) — found via
            // live testing, not hypothetical: a hostile "Hired thug" corpse
            // was otherwise being added, unregistered, and re-added every
            // single sweep, since IsPermanentlyLost's own precondition
            // checks would immediately reject them right after, wasting
            // cycles and log spam on a pawn that should never have entered
            // the registry at all.
            if (pawn.Faction != Faction.OfPlayer)
                return;

            if (!pawn.health.hediffSet.HasHediff(TattooMagicDefOf.TattooMagic_Hediff_Phoenix))
                return;

            // A pawn destroyed outright while still alive (Dev Mode's "T:
            // Destroy" used directly on a living pawn, bypassing Kill()/the
            // corpse/death pathway entirely — pawn.Dead never becomes true)
            // must never be re-added here either — found via live testing
            // (2026-08-28), same churn shape as the two guards below it: the
            // main GameComponentTick loop already has its own
            // !pawn.Dead && pawn.Destroyed check to purge exactly this case,
            // but this self-heal method never mirrored it, so it kept
            // re-adding the same destroyed-while-alive pawn every sweep only
            // for the main loop to immediately remove it again.
            if (!pawn.Dead && pawn.Destroyed)
                return;

            // A dead wearer already permanently lost (corpse destroyed/
            // buried/decayed, or brain missing) must never be re-added here
            // — found via live testing (2026-08-28): nothing strips the
            // Phoenix hediff itself when a wearer is marked permanently
            // lost, so without this guard this method kept re-registering
            // the same corpse every single sweep, only for the main
            // GameComponentTick loop below to immediately re-detect the
            // same loss and remove it again — an endless add/remove churn
            // for the rest of the save's life (double log spam included),
            // not a one-time transition. Same shape of bug as the
            // hostile-pawn case above, just for a different precondition.
            // Excludes a cremation-guaranteed pawn from this check, exactly
            // like the two existing IsPermanentlyLost call sites in
            // GameComponentTick below — a legitimately cremated corpse
            // reads as "corpse missing/destroyed" too, and must still be
            // re-trackable if a save/reload (R11) ever drops it out of
            // registeredWearers before its guaranteed revival resolves.
            HediffComp_PhoenixEffect comp = GetPhoenixComp(pawn);
            if (pawn.Dead && comp?.cremationGuaranteed != true && PhoenixRevivalUtility.IsPermanentlyLost(pawn))
                return;

            registeredWearers.Add(pawn);

            if (TattooMagicSettings.EnableDebugLogging)
                Log.Message($"[TattooMagic DEBUG] Phoenix registry self-heal: found unregistered wearer {pawn.LabelShort}, adding (now {registeredWearers.Count}).");
        }

        // Dev Mode only (Source/Debug/DebugAction_TestPhoenixRevival.cs) —
        // bypasses the self-gate so a test doesn't have to wait ~2,000 real
        // ticks for the next natural sweep.
        public void ForceSweepNow()
        {
            nextSweepTick = 0;
            GameComponentTick();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            int now = Find.TickManager.TicksGame;
            if (now < nextSweepTick)
                return;

            nextSweepTick = now + SweepIntervalTicks;

            SyncFromActualHediffs();

            for (int i = registeredWearers.Count - 1; i >= 0; i--)
            {
                Pawn pawn = registeredWearers[i];

                if (pawn == null)
                {
                    if (TattooMagicSettings.EnableDebugLogging)
                        Log.Message("[TattooMagic DEBUG] Phoenix registry: removing entry (null pawn reference).");

                    registeredWearers.RemoveAt(i);
                    continue;
                }

                // Covers a wearer's own Pawn Thing being destroyed directly
                // while still alive (e.g. Dev Mode's "T: Destroy" used on a
                // living pawn, which bypasses Kill()/the corpse/death
                // pathway entirely, so pawn.Dead never becomes true) — found
                // via live testing, not hypothetical.
                //
                // Deliberately gated on !pawn.Dead: decompile-confirmed
                // (2026-08-23, chasing a real repeating registry-drop bug)
                // that Thing.Destroyed is true for *every* ordinary dead
                // pawn, not just this edge case — Pawn.Kill() calls
                // base.Kill(), which destroys the pawn's own underlying
                // Thing as part of normal death (the Corpse is a separate
                // Thing that then holds the now-Destroyed pawn as its
                // InnerPawn). ResurrectionUtility.TryResurrect's own
                // pawn.ForceSetStateToUnspawned() call exists specifically
                // to undo this during resurrection, confirming vanilla
                // itself treats "a dead pawn reads as Destroyed" as the
                // normal starting condition, not an error state. An earlier
                // version of this check applied to every pawn regardless of
                // Dead state, which silently dropped every dead wearer
                // (cremated or not) from the registry on the very next
                // sweep after being (re-)added — this is what was actually
                // causing Von and Estelle to cycle in and out, not
                // cremation specifically.
                if (!pawn.Dead && pawn.Destroyed)
                {
                    if (TattooMagicSettings.EnableDebugLogging)
                        Log.Message($"[TattooMagic DEBUG] Phoenix registry: removing entry ({pawn.LabelShort} destroyed while still alive).");

                    registeredWearers.RemoveAt(i);
                    continue;
                }

                // Purges anyone already sitting in the registry from before
                // the faction check existed (TryRegisterIfCarrier/
                // CompPostMake now both gate registration on this, but that
                // only prevents *new* additions — this self-corrects
                // whatever a save already had, e.g. a hostile pawn added by
                // an earlier, less-strict version of the self-heal).
                if (pawn.Faction != Faction.OfPlayer)
                {
                    registeredWearers.RemoveAt(i);

                    if (TattooMagicSettings.EnableDebugLogging)
                        Log.Message($"[TattooMagic DEBUG] Phoenix registry: purged {pawn.LabelShort} (not player-faction: {pawn.Faction?.Name ?? "none"}).");

                    continue;
                }

                if (!pawn.Dead)
                {
                    // FR-003: removing the hediff from a living wearer (e.g.
                    // God Mode's own health-tab delete button, or the
                    // "unsanctioned RemoveHediff" debug tool run with God
                    // Mode on) must free their cap slot — found via live
                    // testing, not hypothetical: every check below this
                    // point is gated on pawn.Dead, so a living pawn who no
                    // longer carries the hediff at all was previously never
                    // reconsidered and stayed registered forever. Mirrors
                    // the dead-pawn "comp == null" branch further down,
                    // just for the living-wearer case that branch can never
                    // reach.
                    if (!pawn.health.hediffSet.HasHediff(TattooMagicDefOf.TattooMagic_Hediff_Phoenix))
                    {
                        registeredWearers.RemoveAt(i);

                        if (TattooMagicSettings.EnableDebugLogging)
                            Log.Message($"[TattooMagic DEBUG] Phoenix registry: removing {pawn.LabelShort} — no longer carries the Phoenix hediff (freed via FR-003).");
                    }

                    continue;
                }

                HediffComp_PhoenixEffect comp = GetPhoenixComp(pawn);
                if (comp == null)
                {
                    if (TattooMagicSettings.EnableDebugLogging)
                        Log.Message($"[TattooMagic DEBUG] Phoenix registry: removing {pawn.LabelShort} — has the Phoenix hediff but no HediffComp_PhoenixEffect found on it.");

                    registeredWearers.RemoveAt(i);
                    continue;
                }

                if (comp.reviveAttemptTick == 0)
                {
                    // Newly-observed death — either schedule the wait, or,
                    // if already permanently ineligible (e.g. killed by a
                    // headshot), drop them immediately with no wait at all.
                    // Never checked for a cremation-guaranteed pawn — same
                    // rule as the other two IsPermanentlyLost call sites
                    // below. A cremation-guaranteed pawn should normally
                    // never even reach this branch (reviveAttemptTick is
                    // already set from their original death, well before
                    // cremation could happen) — this guard exists purely as
                    // a defensive backstop for that invariant, not because
                    // this branch is expected to be the normal path for a
                    // cremated pawn.
                    if (!comp.cremationGuaranteed && PhoenixRevivalUtility.IsPermanentlyLost(pawn))
                    {
                        registeredWearers.RemoveAt(i);
                        continue;
                    }

                    // A float, not an int: this feature's own quickstart.md
                    // testing note expects sub-day waits (e.g. half a day)
                    // to be usable for fast manual testing — truncating to
                    // int here first would silently floor 0.5 down to 0,
                    // firing the revival attempt immediately instead of
                    // waiting at all.
                    float waitDays = comp.tier >= 2
                        ? TattooEffectValues.Get(comp.ScopeKey, "WaitDaysTier2", comp.Props.waitDaysTier2)
                        : TattooEffectValues.Get(comp.ScopeKey, "WaitDaysTier1", comp.Props.waitDaysTier1);

                    comp.reviveAttemptTick = now + (int)(waitDays * GenDate.TicksPerDay);

                    if (TattooMagicSettings.EnableDebugLogging)
                        Log.Message($"[TattooMagic DEBUG] Phoenix: {pawn.LabelShort} died — revival attempt scheduled in {waitDays} day(s).");

                    continue;
                }

                if (now >= comp.reviveAttemptTick)
                {
                    PhoenixRevivalUtility.TryResolveAttempt(pawn, comp);
                    continue;
                }

                // Still waiting — re-check permanent-loss conditions every
                // sweep (not only at the scheduled attempt) so a corpse
                // destroyed/buried mid-wait is noticed promptly. Never
                // checked for a cremation-guaranteed pawn (research.md R4).
                if (!comp.cremationGuaranteed && PhoenixRevivalUtility.IsPermanentlyLost(pawn))
                {
                    comp.reviveAttemptTick = 0;
                    registeredWearers.RemoveAt(i);

                    if (TattooMagicSettings.EnableDebugLogging)
                        Log.Message($"[TattooMagic DEBUG] Phoenix: {pawn.LabelShort}'s body was lost mid-wait — revival no longer possible.");
                }
            }
        }

        private static HediffComp_PhoenixEffect GetPhoenixComp(Pawn pawn)
        {
            Hediff hediff = pawn.health?.hediffSet?.hediffs?.Find(h => h.def == TattooMagicDefOf.TattooMagic_Hediff_Phoenix);
            return (hediff as HediffWithComps)?.TryGetComp<HediffComp_PhoenixEffect>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref registeredWearers, "registeredWearers", LookMode.Reference);
            Scribe_Values.Look(ref nextSweepTick, "nextSweepTick", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && registeredWearers == null)
                registeredWearers = new List<Pawn>();
        }
    }
}
