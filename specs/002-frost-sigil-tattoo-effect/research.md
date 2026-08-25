# Phase 0 Research: Frost Sigil Tattoo Effect (Effect Pattern Vertical Slice)

## R1: How does a tattoo's numeric values become override-ready (FR-012/SC-007) without building a settings UI?

**Decision**: A single static accessor, `TattooEffectValues.Get(string scopeKey, string valueKey, float
xmlDefault)`, backed by an in-memory `Dictionary<string, float>` that starts empty. Every tattoo-effect
value this feature reads at a decision point — slow chance, slow magnitude/duration, freeze chance/
duration, the Tier 1→2 threshold, and the cold-resistance amount — is read through this method, never by
reading the owning Def/Comp field directly at the use site. Nothing in this feature ever writes to the
dictionary; that's deliberately left for a future settings-UI feature (PRD §10) to populate.

**Rationale**: This is the cheapest possible seam that satisfies FR-012 without building persistence or a
UI now. A future settings feature only needs to write into this one dictionary (or replace its backing
store) — zero changes to any tattoo's effect code, satisfying SC-007.

**Alternatives considered**: Wiring up `ModSettings`/`Dialog_ModSettings` now (rejected — PRD §10 explicitly
defers the UI, and the UI itself gains nothing from an early start since it grows incrementally as each of
the 11 tattoos ships); a `StatModifier`-per-value typed system instead of a flat string-keyed dictionary
(rejected as over-engineered for ~7 values on one tattoo — a string key scales fine and a future feature can
still layer type-safety on top without touching call sites).

## R2: How does Frost Sigil detect "this pawn was struck by a melee attacker" in a way that survives CE?

**Decision**: A single Harmony postfix on `Pawn.PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)` —
the universal point where RimWorld's core health system finalizes any damage application, regardless of
which Verb produced it. The postfix filters to `totalDamageDealt > 0` (the hit actually connected),
`dinfo.Instigator is Pawn attacker`, and `dinfo.Tool != null` (melee, not ranged/explosive/environmental —
see below for why `Tool`, not `Weapon`). It then iterates the struck pawn's hediffs for any implementing a
shared `IOnMeleeHitTattooEffect` interface (R5) and invokes it — Frost Sigil's comp is the first, not the
only, consumer.

**Update from in-game testing**: the melee filter originally checked `dinfo.Weapon != null &&
dinfo.Weapon.IsMeleeWeapon`, which is null for unarmed/natural attacks (bare fists, animal bites/claws) —
`Weapon` is only populated when the attacker is wielding an actual equipped `ThingDef`. This silently
excluded every unarmed melee hit, discovered via live testing (an unarmed attacker's punches never
dispatched to Frost Sigil's comp at all). Switched to `dinfo.Tool != null` instead: `Tool` (`Verse.Tool`,
confirmed via reflection against the real assembly) is populated for *any* melee swing — a weapon's own
Tool (e.g. a knife's "blade") as well as a race's natural Tools (e.g. "left fist") — and is the same field
that produces the "(human left fist)" source label seen in injury tooltips. It is never populated for
ranged/explosive damage, so it's a strictly better melee-vs-not signal than `Weapon`, and it matches PRD
§6's unqualified "a melee attacker" wording rather than an unstated "armed attacker only" restriction.

**Rationale**: `Pawn.PostApplyDamage` sits below the verb/weapon layer, in code CE does not replace — CE
reworks armor penetration and ballistics feeding *into* damage resolution, not the pawn health system that
finalizes it. This means the same patch works whether CE is loaded or not, without the vanilla/CE dual-patch
pattern Constitution Principle IV requires for *targeting/decision* logic (see Constitution Check). This is
an assumption about CE's internals, not a certainty — it MUST be confirmed in-game with CE loaded per
`quickstart.md`, and Principle II already treats that as the actual bar for "done," not this research note.

**Alternatives considered**: Patching `Verb_MeleeAttack.ApplyMeleeDamageToTarget` directly (rejected — CE
ships its own melee-verb subclass for parry/dodge resolution; patching the vanilla verb risks silently
missing CE-resolved melee attacks, which is exactly the failure mode Principle I forbids); polling pawn
health each tick for "was I just hit" (rejected — no clean signal exists without an event hook, and would
mean per-tick work per pawn for a rare event).

## R3: How is Frost Sigil's cold-resistance bonus applied, given it must be tier-aware and accessor-driven?

**Decision**: A generic, reusable `StatPart_TattooEffectOffset`, registered in C# at mod startup (not XML)
onto `StatDefOf.ComfyTemperatureMin` and `StatDefOf.MoveSpeed`'s `parts` list. At stat-evaluation time it
sums `IProvidesTattooStatOffset.GetStatOffset(stat)` (R5) across every hediff comp on the pawn implementing
that interface. Frost Sigil's own comp implements it, returning `TattooEffectValues.Get(...)` for its
current tier's cold-resistance value whenever `ComfyTemperatureMin` is queried.

**Rationale**: Native `HediffStage.statOffsets` (the vanilla severity-driven stat-bonus mechanism used by
e.g. malnutrition) is read directly by RimWorld's stat engine with no code chokepoint — it can't route
through `TattooEffectValues`, so it can't satisfy FR-012. A `StatPart` is a real, standard RimWorld
extensibility point (`StatDef.parts: List<StatPart>`) that runs on every stat query, giving a live,
always-current, override-respecting value without per-tick polling — the stat cache invalidates and
re-queries the `StatPart` whenever RimWorld recomputes the stat.

**Alternatives considered**: `HediffStage.statOffsets` keyed by hediff `Severity` (rejected per FR-012
above — no accessor hook); recomputing and re-applying a raw stat offset every tick via `CompPostTick`
(rejected — unnecessary per-tick cost when `StatPart` already gives an on-demand hook for free).

## R4: How is the slow-on-hit proc's magnitude applied to the attacker, given it also must be accessor-driven?

**Decision**: Reuse the same `IProvidesTattooStatOffset` mechanism (R3/R5), on the *attacker's* side this
time: when Frost Sigil's on-hit proc succeeds, the mod grants a transient `HediffDef`
(`TattooMagic_Hediff_FrostSigilSlow`) to the attacker carrying a small comp that stores the resolved
move-speed offset (read from `TattooEffectValues` at grant time, for whichever tier the defending pawn was
at) as a plain field, and implements `IProvidesTattooStatOffset` against `StatDefOf.MoveSpeed`. Duration is
handled by vanilla's built-in `HediffComp_Disappears` (`disappearsAfterTicks`, itself read from
`TattooEffectValues` at grant time and set on the comp instance), so the hediff self-removes with no custom
tick logic needed. Tier 2's rare freeze does not need a hediff at all — it calls vanilla's existing
`attacker.stances.stunner.StunFor(ticks, tattooedPawn)`, the same stunner API used by vanilla EMP effects.

**Rationale**: Reusing the same offset-interface mechanism from R3 means there is exactly one reusable
"stat-offset-carrying hediff comp" concept in this feature, used on both the defender (passive, tier-driven)
and the attacker (transient, grant-time-resolved) sides — one thing to document, one thing future tattoos
reuse. Vanilla's stunner API means "freeze" needs zero new persisted state.

**Alternatives considered**: Two distinct pre-authored `HediffDef`s (`FrostSigilSlowT1`/`FrostSigilSlowT2`)
with XML-baked `statOffsets` per tier (rejected — same FR-012 gap as R3: the magnitude would bypass the
accessor entirely); a custom stun/freeze hediff instead of the vanilla stunner API (rejected — the stunner
API already does exactly this and is battle-tested by vanilla itself).

## R5: What is the actual reusable "pattern" this feature commits to for future passive tattoos (FR-010)?

**Decision**: Four small, independent, composition-friendly pieces, documented in
`contracts/passive-tattoo-effect-contract.md` for later features to build against:

- `TattooEffectValues` (R1) — the value accessor.
- `IProvidesTattooStatOffset` (R3/R4) — `float GetStatOffset(StatDef stat)` — implemented by any hediff
  comp that wants to contribute a live stat bonus; consumed by `StatPart_TattooEffectOffset`.
- `IOnMeleeHitTattooEffect` (R2) — `void OnMeleeHitTaken(Pawn attacker, DamageInfo dinfo, float
  damageDealt)` — implemented by any hediff comp that reacts to its wearer being struck in melee; consumed
  by the one shared `Pawn.PostApplyDamage` postfix.
- `TattooTierProgress` — a small plain (non-`HediffComp`) class with `tier`/`progressionCounter` fields, an
  `ExposeData()` method, and `TryRegisterQualifyingEvent(string thresholdKey, int defaultThreshold)` that
  increments the counter, checks the threshold via `TattooEffectValues`, and flips `tier` in place when
  reached. Any passive tattoo's comp composes one of these as a field rather than inheriting from a shared
  `HediffComp` base class.

**Rationale**: Interfaces + composition (rather than a shared `HediffComp` base class) sidestep C# single
inheritance — a future passive tattoo's comp can implement either interface independently, or both, or
compose `TattooTierProgress` without being forced into an inheritance hierarchy shaped by Frost Sigil's own
needs. This directly satisfies User Story 3 / FR-010's "without modifying Frost Sigil's own effect code."

**Alternatives considered**: A single abstract `HediffComp_PassiveTattooEffect` base class carrying tier,
counter, and both hook methods (rejected — forces every future passive tattoo's comp through one
inheritance chain, and a tattoo that only needs a stat bonus with no on-hit proc would inherit unused hook
methods it must no-op).

## R6: CE stat-compatibility check for Frost Sigil specifically

**Decision**: No vanilla/CE stat branching is implemented in this feature. `StatDefOf.ComfyTemperatureMin`
(cold resistance) and `StatDefOf.MoveSpeed` (the slow proc) are not among the StatDefs Combat Extended
replaces — CE's stat rework targets accuracy, armor penetration, suppression, and stamina, not thermal
comfort or base movement speed. The Tier 2 freeze uses vanilla's stunner API, which CE does not override.

**Rationale**: Constitution Principle I requires branching only where CE actually *replaces* a stat; branching
against a StatDef CE never touches would be dead code. This finding is specific to Frost Sigil — per the
spec's Assumptions, CE-focused tattoo work (ammo/loadout integration) is flagged for Serpent's Eye, not this
feature.

**Alternatives considered**: n/a — this is a factual check against what CE replaces, not a design choice.

## R7: Testing strategy

**Decision**: No automated test project, consistent with feature 001 (research.md R5 there). Verification is
manual, in Dev Mode, following `quickstart.md`, including a CE-loaded pass for the melee-hit detection patch
(R2) specifically, since that decision rests on an assumption about CE's internals that only in-game
behavior can actually confirm.

**Rationale**: Unchanged from feature 001 — RimWorld's `Verse`/`RimWorld` types aren't practically
instantiable outside the running game process, and Constitution Principle II already sets in-game
verification as the real bar for "done."

**Alternatives considered**: A pure-C# unit test around `TattooEffectValues` and `TattooTierProgress` (both
are game-independent) — left as an optional add at implementation time since they're simple enough that
in-game verification covers them, same reasoning feature 001 used for its success-curve lookup.
