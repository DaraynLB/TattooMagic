# Phase 0 Research: Guardian's Call Tattoo Effect (First Triggered Ability + Two-Path AI Targeting Slice)

## R1: How does a tattoo grant an activatable gizmo with a cooldown, given no DLC dependency is allowed?

**Decision**: RimWorld's native `AbilityDef`/`Verse.Ability`/`Pawn_AbilityTracker` framework (used by Royalty
psycasts, Biotech genes, Ideology role abilities) is **not** available — confirmed it is gated behind those
DLCs, and the constitution's Target Platform section requires the mod to work with no DLC dependency. Instead,
this feature adds a small, tattoo-agnostic reuse point mirroring the existing interface+shared-patch pattern
(`IOnMeleeHitTattooEffect` / `Patch_Pawn_PostApplyDamage_TattooOnHit`, research.md R2/R5 of feature 002):

- `IProvidesTattooGizmo` — `Gizmo GetGizmo();` — implemented by any `HediffComp` that wants to contribute a
  gizmo to its pawn.
- `Patch_Pawn_GetGizmos_TattooGizmos` — one shared Harmony postfix on the real, public `Pawn.GetGizmos()`
  (`IEnumerable<Gizmo>`), iterating the pawn's hediff comps and yielding `GetGizmo()` for every comp
  implementing the interface, alongside vanilla's own gizmos. Tattoo-agnostic — carries no Guardian's-Call-
  specific knowledge, exactly like the existing on-hit patch.

The gizmo itself is a plain `Command_Action` (no DLC-only `Command_Ability` radial-cooldown widget). Guardian's
Call's own comp builds it: `action` calls the comp's `TryActivate()`; `disabled`/`disabledReason` are computed
from the comp's own `cooldownEndTick` field (`Find.TickManager.TicksGame < cooldownEndTick`) each time the
gizmo is requested, showing remaining cooldown seconds in the disabled reason — vanilla's standard rendering
already greys out a disabled `Command_Action` and shows its `disabledReason` as a tooltip, satisfying FR-002's
"remaining cooldown visible to the player" without needing DLC-only cooldown-arc rendering.

**Rationale**: This is the cheapest seam consistent with the codebase's established convention (a narrow
interface + one shared, tattoo-agnostic Harmony patch) and avoids a hard or soft dependency on any DLC's
ability system, which the constitution forbids relying on. It also generalizes cleanly for User Story 3 — a
future triggered tattoo (Bloodrune, Stormlash, Wraithstep, Berserker's Mark) implements `IProvidesTattooGizmo`
on its own comp and is picked up automatically, no changes to Guardian's Call's files or the shared patch.

**Alternatives considered**: The vanilla `Ability`/`AbilityDef` framework (rejected — DLC-gated, confirmed via
research; would silently fail to grant a gizmo at all on a Core+Harmony-only install, violating Constitution
Target Platform); a `ThingComp`/`CompUseable` on an equippable item instead of a `HediffComp` (rejected — the
tattoo is granted via the existing `appliedHediff` hediff chain per FR-010, not a separate wearable item, and
`ThingComp`s don't attach to hediffs); a custom full `Gizmo` subclass with hand-drawn radial cooldown fill
(rejected as unnecessary polish — a disabled `Command_Action` with a text `disabledReason` already satisfies
FR-002 and SC-003, and a nicer radial widget can be layered on later without changing the underlying
interface/patch contract).

**Confirmed compatible with Royalty (or any DLC granting abilities) being present, not just absent**: read the
decompiled `Pawn.GetGizmos()` directly — it is a single non-virtual method that already yields gizmos from many
independent sources in sequence (equipment, apparel, drafter, ..., and `abilities.GetGizmos()` guarded only by
`if (abilities != null)`, not a DLC flag — that tracker simply doesn't exist on a pawn unless something, e.g. a
Royalty psycast, Biotech gene, or Ideology role, actually granted it one). `Patch_Pawn_GetGizmos_TattooGizmos`
postfixes this same method and only *appends* Guardian's Call's gizmo after whatever it already yielded — it
never touches, wraps, or conditions on the `abilities` branch. So a psycaster colonist who is also
Guardian's-Call-tattooed keeps their normal DLC ability gizmos (radial cooldown, targeting UI, all of it)
untouched, and simply also sees our plain `Command_Action` alongside them. The only difference a Royalty player
would notice is cosmetic — their psycast gizmos get a radial cooldown fill, ours shows cooldown as a disabled
state + text reason — not a functional gap, and exactly what FR-002 requires. "No DLC dependency" (constitution
Target Platform) means this feature must not *require* a DLC to function, not that it must avoid coexisting
with one; this design does both.

## R2: How is the taunt duration and cooldown state tracked and exposed, given it must survive save/reload?

**Decision**: `HediffComp_GuardiansCallEffect` (the new comp) holds two plain `int` tick fields —
`cooldownEndTick` and `tauntEndTick` — both `Scribe_Values.Look`'d from `CompExposeData()`, the same pattern
`TattooTierProgress.ExposeData()` already uses (feature 002). `TryActivate()` sets both from
`Find.TickManager.TicksGame` plus accessor-resolved durations; no per-tick `CompPostTick` polling is needed —
every consumer (the gizmo's `disabled` check, the targeting patch's taunt-active check, the Tier 2 armor-offset
check) compares the current tick against the stored end tick on demand, exactly when each is queried.

**Rationale**: Matches feature 002's R4 finding (grant-time-resolved fields beat per-tick recomputation) and
keeps Guardian's Call's own persisted state to two ints plus the reused `TattooTierProgress`, with no new
Scribe-facing types needed.

**Alternatives considered**: A `HediffComp_Disappears`-style transient companion hediff for the taunt window,
mirroring Frost Sigil's attacker-side slow hediff (rejected — the taunt duration lives on the *caster's own*
comp, not a grantee's, so a second hediff would just be indirection around the same two ticks with no benefit).

## R3: How does the live taunt state get discovered by the targeting patch without per-call full-map scans?

**Decision**: A small Guardian's-Call-specific static registry, `GuardiansCallTauntRegistry`
(`Source/Hediffs/GuardiansCallTauntRegistry.cs`), holding a `HashSet<HediffComp_GuardiansCallEffect>` of comps
with a currently-active taunt. `TryActivate()` adds itself to the set; the set entry is treated as expired (and
lazily removed) the moment `Find.TickManager.TicksGame >= tauntEndTick`, or immediately if the owning pawn is
dead/downed/no longer carries the hediff — checked at query time, not via a scheduled removal callback, so a
downed/killed/hediff-stripped pawn's taunt disappears on the very next targeting query with no extra cleanup
code (Edge Cases: "taunt effect ends immediately"). This registry is intentionally **not** part of the reusable
contract (unlike R1's interface/patch pair) — it's Guardian's-Call-specific bookkeeping the targeting patch
(R4) consults, not a general reuse point, consistent with spec User Story 3 only promising the ability/cooldown
pattern and the two-path targeting *convention*, not this specific taunt-tracking mechanism.

**Rationale**: `AttackTargetFinder.BestAttackTarget` is called very frequently (per targeting decision, for
every hostile pawn/turret needing a target) — walking every pawn's `hediffSet.hediffs` on every call (the
pattern `StatPart_TattooEffectOffset` uses, acceptable there because stat queries are cached by RimWorld) would
add avoidable cost to a hot AI path. A registry populated only while at least one Guardian's Call taunt is
active keeps the common case (no active taunts anywhere) a single empty-set check.

**Alternatives considered**: Scanning `pawn.Map.mapPawns.AllPawnsSpawned` for hediff carriers on every
targeting call (rejected — the exact hot-path cost this registry avoids); a `MapComponent`-per-map registry
(rejected as unnecessary complexity — a single mod-wide static set is sufficient since taunts are short-lived
and pawns carry a map reference themselves for distance checks).

## R4: The vanilla targeting patch — where taunt bias is actually injected (FR-001, FR-003, Constitution IV path 1)

**Decision**: A Harmony **Postfix** on the public, stable `Verse.AI.AttackTargetFinder.BestAttackTarget`
(confirmed signature: `public static IAttackTarget BestAttackTarget(IAttackTargetSearcher searcher,
TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f,
IntVec3 locus = default, float maxTravelRadiusFromLocus = float.MaxValue, bool canBash = false, bool
canTakeTargetsCloserThanEffectiveMinRange = true)` — this is the single top-level entry point vanilla's hostile
AI (both ranged and melee searchers; melee falls back internally to `FindBestReachableMeleeTarget`, still
reached through this same public method) uses to pick a target, so patching here rather than the private
per-shot `GetShootingTargetScore` scorer covers melee and ranged hostiles uniformly with one patch (Edge Case:
taunt applies "regardless of whether melee or ranged").

The postfix runs vanilla's own algorithm untouched first, then only *overrides* `__result` when a strictly
better, still-legal candidate exists:

1. Resolve `searcher.Thing as Pawn` — bail (leave `__result` alone) if not a pawn or not currently hostile to
   any registered taunter (R3).
2. For each active taunter within the accessor-resolved taunt range of the searcher pawn's position: confirm
   it still passes the caller's own `validator` (if any) and `searcherPawn.CanReach(taunterPawn, PathEndMode.Touch,
   Danger.Some)` (public vanilla API, cheap relative to the full search vanilla just ran) — i.e. the taunter must
   be a target vanilla itself would have accepted as *legal*, just not necessarily vanilla's top *score*.
3. If one or more taunters qualify, override `__result` with the nearest qualifying taunter to the searcher
   (spec explicitly leaves multi-taunter tie-breaking undefined — nearest is a simple, deterministic choice);
   otherwise leave vanilla's own `__result` untouched.

Because this always lets vanilla's real algorithm run first and only ever *replaces* an already-legal pick, a
taunter that's out of reach, out of `validator`, or otherwise not a legal target this call never gets forced on
a hostile — hostiles are never left targetless because of a taunt.

**Rationale**: A prefix that tries to force/restrict the *search itself* (e.g. by wrapping `validator`) would
require re-deriving vanilla's own reachability/LOS/flags legality rules or risk returning a target vanilla
would have rejected, and would need a fallback re-invocation of the original method if the restricted search
came up empty. Letting the original method run first and conditionally swapping its answer avoids duplicating
any of vanilla's legality logic and cannot leave a hostile with no target when it would otherwise have had one.

**Alternatives considered**: Postfixing the private `GetShootingTargetScore` with a large score bonus for
taunters (rejected — only fires for ranged verb-based scoring, not melee's separate
`FindBestReachableMeleeTarget` path, failing the "regardless of melee or ranged" requirement); a Prefix that
substitutes a taunter-only `validator` (rejected per Rationale above — correctness risk and re-invocation
complexity for no real benefit over a Postfix).

## R5: The Combat Extended path — what "CE's equivalent" actually is, and how it will be verified (Constitution IV path 2)

**Decision**: Confirmed CE has, at least in past releases, shipped its own Harmony patch targeting this exact
same vanilla method (a `Harmony-AttackTargetFinder_BestAttackTarget_Patch`, referenced in CE's own issue
history) rather than routing CE-controlled pawns through an entirely separate targeting entry point — CE
appears to layer cover/suppression-aware scoring on top of vanilla's `BestAttackTarget`, not replace its
dispatch. This means R4's Postfix is likely to already observe and correctly override CE-controlled hostiles'
target choice, since Harmony composes multiple patches on the same method rather than one replacing another.
This is treated as a **verification target, not a settled fact** — consistent with this project's established
practice (feature 002 R2) of flagging real uncertainty about a live game/mod's internals for in-game
confirmation rather than asserting unverifiable specifics from documentation alone.

Implementation MUST include an explicit CE-loaded verification pass (quickstart Scenario 5) with `Log.Message`
probes temporarily added to the R4 postfix (per this project's proactive-debug-logging practice) confirming it
fires for CE-controlled hostile pawns and that its override is not subsequently discarded by a later CE patch
on the same method. Two concrete outcomes are possible and both are explicitly in scope for the implementation
task, not deferred:

- **If verification confirms the R4 postfix already governs CE pawns' target choice**: Constitution Principle
  IV's "two independently-verified paths" is satisfied by the *same* patch verified twice — once in a non-CE
  game, once in a CE-loaded game — which is explicitly permitted since the constitution requires two verified
  *paths of behavior*, not necessarily two distinct patch classes, and PRD §7's premise (vanilla and CE use
  different code) is re-confirmed or corrected by this empirical check either way.
- **If verification instead shows CE-controlled pawns bypass `BestAttackTarget`** (e.g. via CE's own
  suppression-driven job selection short-circuiting before target search): a second, reflection-based Postfix
  is added following the exact `Patch_CE_ProjectileImpact_TattooAmmoBonus` convention (R5 of feature 003) —
  `AccessTools.TypeByName`/`Method` resolution gated behind `CombatExtendedInterop.IsLoaded`, applied
  imperatively from `TattooMagicMain` alongside the existing CE patch — targeting whatever method that
  in-game probing identifies as CE's actual entry point.

**Rationale**: Fabricating a specific CE internal method name from documentation/search alone (without
decompiling the exact loaded CE version) would violate this project's own standard of "verify before claiming
done" (Constitution Principle II) worse than leaving it as a scoped, concrete implementation task with a
defined fallback. This keeps the plan actionable (a clear task exists either way) without asserting unverified
internals as fact.

**Alternatives considered**: Deciding now, from documentation alone, on a specific CE class/method to patch
(rejected — risks shipping a patch against a method that no longer exists or was never the real entry point,
silently no-oping under CE, which is exactly what Principle I forbids); skipping CE verification and treating
R4 as "probably fine" (rejected outright — the constitution explicitly requires this to be an independently
verified path, not an assumption).

## R6: Tier 2's armor buff — which StatDef(s), and does CE need a different one? (FR-006/FR-007)

**Decision**: Register `StatPart_TattooEffectOffset` (feature 002, unmodified) onto
`StatDefOf.ArmorRating_Sharp`, `StatDefOf.ArmorRating_Blunt`, and `StatDefOf.ArmorRating_Heat` in
`TattooEffectStatPartInstaller` (adding three `Register(...)` calls to that file — the installer's own doc
comment already anticipates future tattoos registering additional StatDefs there; this is not a modification
to Frost Sigil's or Serpent's Eye's own effect files). Confirmed Combat Extended does **not** define separate
armor-rating StatDefs of its own — it reuses these same three vanilla `StatDefOf` fields and reinterprets their
values under its own deflection-based penetration model, rather than replacing them the way it replaces
accuracy (`AimingAccuracy`, feature 003). `HediffComp_GuardiansCallEffect.GetStatOffset(StatDef stat)` returns
the accessor-resolved Tier 2 buff magnitude for these three stats (and only while a Tier 2 activation's
taunt/buff window is still open, R2), zero otherwise — no `CombatExtendedInterop` branching is needed for this
stat specifically, mirroring Frost Sigil's R6 finding for `ComfyTemperatureMin`/`MoveSpeed`.

**Rationale**: Satisfies FR-007's stacking requirement structurally: because the buff routes through the same
generic `StatPart_TattooEffectOffset` summation every other tattoo's stat offset uses, any future tattoo (e.g.
Ironskin Glyph) registering its own `IProvidesTattooStatOffset` contribution against these same StatDefs adds
rather than overrides, with neither tattoo needing to know about the other (SC-012).

**Alternatives considered**: A CE-only separate "deflection" stat (rejected — no such distinct StatDef exists
to target; would be inventing an integration point CE doesn't have); registering only `ArmorRating_Sharp`
(rejected — PRD's "shield wall" framing implies general protection, and registering all three costs nothing
extra since the shared `StatPart` is generic).

## R7: Testing strategy

**Decision**: No automated test project, unchanged from features 001–003 — RimWorld's `Verse`/`RimWorld` types
and `Verse.AI.AttackTargetFinder` specifically aren't practically runnable outside the game process.
Verification is manual, in Dev Mode, per `quickstart.md`, and per this feature's own `Log.Message`-probe
practice (R5) for the highest-risk, genuinely novel assumption (whether the R4 postfix governs CE pawns).

**Rationale**: Unchanged from prior features; Constitution Principle II already sets in-game verification as
the real bar for "done," and this feature's targeting-AI work is exactly the kind of Harmony/game-state
interaction that a standalone unit test cannot exercise meaningfully.

**Alternatives considered**: n/a — same reasoning as features 001–003.
