# Contract: Ground-Fixed, Duration-Driven AoE Zones

Documents the pattern Bastion Ward establishes for a triggered tattoo whose effect is not carried by a pawn (the
wearer or a hediff) but by a **location** — cast once, then persists independently of its caster until a timer
expires. First of its kind in this mod (every prior tattoo's effect lives on a `HediffComp`, following its wearer).
This is what a future ground-fixed-zone tattoo follows if it needs the same shape, per this project's established
reuse-when-proven convention (research.md R4).

## 1. Ownership and lifetime

- A zone is represented by a plain-data `IExposable` class (`BastionWardZone`, data-model.md), **not** a spawned
  `Thing`/`ThingWithComps` — it needs a position, a radius, an expiry tick, and (optionally) a periodic-effect
  timer, none of which require participating in vanilla's thing-grid/pathing/selection systems.
- Zones live in a single per-save `GameComponent` (`GameComponent_BastionWardZones`, mirroring
  `GameComponent_PhoenixRegistry`'s self-registering shape: a `GameComponent(Game game)` constructor, a
  `public static Get()` lookup via `Current.Game?.GetComponent<T>()`, `ExposeData()` via `Scribe_Collections` with
  `LookMode.Deep`), **not** on the casting pawn's own `HediffComp`. A zone's `IExposable` state carries everything
  needed to answer "is pawn P currently protected/healed" — it never holds a back-reference to its caster, and
  nothing about it is re-derived from the caster at query time.
- Lifetime is driven entirely by `GameComponentTick()` comparing `Find.TickManager.TicksGame` against the zone's own
  `expireTick`, set once at creation from the tattoo's own duration tunable and never recomputed. A zone whose
  creating pawn is downed, killed, or loses the tattoo mid-duration is **not** found, touched, or removed by that
  event — there is no code path connecting a `HediffComp`'s removal to a zone it once created. This is what makes
  "the ward continues running unaffected" (spec Edge Cases, FR-020) true by construction rather than by explicit
  handling: the two lifetimes were never linked in the first place.

## 2. How other systems query a zone

- **Damage negation**: a zone-consuming feature extends the shared `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`
  prefix (research.md R2) with one small, additive check *after* its existing hediff-comp loop —
  `GameComponentType.Get()?.TryAbsorb(__instance, ref dinfo)` — rather than trying to make the defending pawn's own
  hediffs answer a question about a zone it doesn't know it's standing in. This is the one deliberate, narrowly
  justified exception to "never modify the shared dispatch patch for a single tattoo" (research.md R2 spells out why
  a second independently-attributed Harmony prefix on the same method was rejected instead).
- **Periodic per-tick effects** (e.g. a heal): driven from the owning `GameComponent`'s own `GameComponentTick()`,
  never from a `CompPostTick` on any pawn's `HediffComp` — the affected pawns generally don't have the tattoo's
  hediff at all, so there is no comp to tick on their behalf.
- **"Is pawn P inside/eligible" checks always re-derive live** from `pawn.Position`, `pawn.Map`, and
  `GenHostility.HostileTo(pawn, zone.owningFaction)` (research.md R9) on every call — never cached at zone-creation
  time or on the pawn. This is what makes faction changes mid-ward (a mental break, a capture) resolve correctly
  with zero extra code.

## 3. What a future ground-fixed-zone tattoo reuses from this

- The **shape**: a plain `IExposable` zone-state class + a single per-save `GameComponent` registry ticking its own
  expiry/periodic effects, queried live by whatever shared dispatch point (damage, targeting, etc.) needs to know
  about it — fully generic, no Bastion-Ward-specific coupling in the `GameComponent` base shape itself.
- **What is explicitly NOT reusable as a shared class**: `BastionWardZone`'s own fields (`radius`, `heals`,
  `healAmountPerTick`, ...) are Bastion-Ward-specific; a future zone tattoo (e.g. a hypothetical persistent slow
  field or damage-over-time zone) defines its own zone-state class and its own `GameComponent`, rather than trying
  to generalize `BastionWardZone` into a one-size-fits-all zone type before a second real consumer exists to prove
  out what's actually shared (this mod's repeated "no premature abstraction" precedent, e.g. features 012/013/015).
  If a third zone-shaped tattoo arrives later and the two implementations still look identical in shape, extracting
  a shared base at that point is the right call — not now.
- The "extend the shared damage-absorb prefix with one additive, zone-registry check" pattern (§2) is reusable
  verbatim by any future zone effect needing full or partial damage negation, without needing its own second
  Harmony patch.
