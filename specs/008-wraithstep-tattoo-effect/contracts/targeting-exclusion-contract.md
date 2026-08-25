# Contract: AI-Targeting Exclusion (the Inverse of Feature 004's Two-Path Targeting Convention)

Extends `two-path-targeting-contract.md` (feature 004), which documents the **inclusion** case (biasing hostile AI
*toward* a specific pawn — Guardian's Call's taunt). This document is what feature 004 §3 anticipated as "a future
targeting tattoo with different bias semantics (e.g. a 'flee from' effect instead of 'attack')" — Wraithstep's Tier 2
untargetable window is the **exclusion** case (making hostile AI's search *skip* a specific pawn entirely). That
prior document remains in force, unmodified, for Guardian's Call's own inclusion behavior; this document does not
alter it.

## 1. Why exclusion needs a different patch shape than inclusion

Guardian's Call's Postfix (`Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt`) runs vanilla's own search
to completion, then only ever *replaces* `__result` with an independently-validated taunter — safe, because it only
ever swaps in a **known-legal** alternative after the fact.

Exclusion cannot use the same shape: if the vanilla algorithm's own scoring would have picked the
to-be-excluded pawn as its best (possibly *only*) legal candidate, a Postfix has no way to know what the
second-best candidate would have been — nulling `__result` would leave the hostile targetless instead of correctly
falling through to whoever vanilla's own search would have picked next.

## 2. The pattern: a Prefix composing the vanilla `validator` parameter

`Patch_AttackTargetFinder_BestAttackTarget_WraithstepUntargetable` is a Harmony **Prefix** on the same method
Guardian's Call already patches (`Verse.AI.AttackTargetFinder.BestAttackTarget`, signature confirmed by feature 004
research.md R4), declaring the shared `validator` parameter as `ref Predicate<Thing> validator` so it can compose a
wrapper *before* vanilla's search runs:

```text
validator = originalValidator == null
    ? (Predicate<Thing>)IsNotWraithstepUntargetable
    : (t => originalValidator(t) && IsNotWraithstepUntargetable(t));
```

Vanilla's own search then naturally skips the excluded pawn during its normal candidate scan, using its own
existing logic to fall through to the correct next-best legal target — no part of vanilla's scoring is
reimplemented.

**Guard the hot path** the same way `GuardiansCallTauntRegistry.HasActiveTaunters` does: bail out (leave `validator`
untouched) whenever `WraithstepUntargetableRegistry.HasActiveUntargetable` is `false`, which is the overwhelming
common case (`BestAttackTarget` runs on a hot AI path — research.md R3 of feature 004).

## 2b. The search-exclusion Prefix alone is not sufficient — pair it with an active correction

**Confirmed necessary via live testing (2026-08-15), not a hypothetical**: §2's Prefix only stops a *fresh*
`BestAttackTarget` search from picking the excluded pawn. It does nothing for a hostile that already has that pawn
locked in via `Pawn.mindState.enemyTarget`, `Pawn.mindState.meleeThreat`, or its running `Job`'s own `targetA` — none
of those are read from the search at all once a hostile is already acting on a decision it made earlier. Live
testing demonstrated exactly this: the exclusion log line fired (proving the Prefix ran and correctly excluded the
wearer from that search), and the hostile hit her anyway, on both sides of that log line — the hostile's attack was
never going through the search the Prefix patches. This is the identical structural gap Guardian's Call's own
research (feature 004, `HediffComp_GuardiansCallEffect.ForceNearbyHostilesToReconsiderTarget`) already found and
solved on the *opposite* problem (forcing aggro onto a taunter rather than excluding a target).

**Any exclusion-style tattoo MUST therefore pair the search-exclusion Prefix (§2) with an active correction**: for
every hostile whose `mindState.enemyTarget`, `mindState.meleeThreat`, or running job's `targetA` currently is the
protected pawn, null the two `mindState` fields and call `hostile.jobs.EndCurrentJob(JobCondition.InterruptForced)`
to force that hostile back through its own AI loop, where it will then correctly run the (already-excluding) search.
This correction MUST run both immediately when the protected window opens (to catch a hostile that already had the
pawn locked on beforehand) and periodically for the remainder of the window (`HediffComp_WraithstepEffect` uses a
30-tick interval via `CompPostTick`, mirroring `HediffComp_GuardiansCallEffect.ReassertIntervalTicks` exactly) — a
one-shot correction at activation only was rejected for the same reason Guardian's Call's own single-activation
correction was insufficient: a hostile can acquire the protected pawn through some other AI path *during* the
window, not just be locked on before it opens.

## 3. Composition with Guardian's Call's own Postfix (same method, both patches active)

Because Harmony runs every Prefix before the original method, and the original method before every Postfix,
Guardian's Call's own Postfix — which already re-checks `validator(taunterPawn)` before accepting a taunt candidate
— sees the *composed* validator (this Prefix already ran). A pawn currently inside a Wraithstep untargetable window
is therefore correctly rejected as an eligible Guardian's Call taunt target too, with no code change to Guardian's
Call's own file and no special-case interaction code anywhere — it falls out of the two patches' independent,
already-correct designs. This MUST be confirmed in `quickstart.md`, not merely assumed from this document's
reasoning.

## 4. The Combat Extended path — CONFIRMED (2026-08-15)

Per feature 004 research.md R5, and the fact that feature 004 shipped with only its single vanilla-method patch (no
second CE-specific targeting file exists in `Source/Patches/`), the same `BestAttackTarget` patch point was
confirmed sufficient for CE-controlled hostiles without a second patch. That expectation was independently verified
here too, not merely assumed: decompiling `CombatExtended.dll` showed CE patches `BestAttackTarget` via a Transpiler
that rewrites only the method's *ranged*-attack-targeting branch (replacing it with CE's own
`FindAttackTargetForRangedAttack`, in `CombatExtended.HarmonyCE.Harmony_AttackTargetFinder`) — melee targeting runs
untouched vanilla logic. CE's replacement still reads the same composed `validator`/`attackTargetValidator` this
Prefix modifies, and a live test against a ranged-armed CE hostile confirmed the exclusion holds for that path too
(the exclusion log fired, zero shots landed). **No second, CE-specific patch was needed** — Constitution Principle
IV is satisfied via "the same patch verified twice" (non-CE per T017, CE-loaded per T018), the same allowance
Guardian's Call's own precedent already established.

## 5. What a future exclusion-style tattoo reuses from this

- The **pattern** (Prefix composing `ref Predicate<Thing> validator`, rather than a Postfix overriding `__result`)
  for any future "hostiles should not be able to target this pawn" effect — **paired with §2b's active correction**;
  the search-exclusion Prefix alone is confirmed insufficient on its own.
- `CombatExtendedInterop.IsLoaded` (feature 003, unmodified) for CE detection, and the reflection-patch convention
  from `passive-tattoo-effect-contract.md` §3 (feature 003) for however its own CE path resolves, if one turns out
  to be needed.
- **What is explicitly NOT reusable**: `WraithstepUntargetableRegistry` and the untargetable-window
  duration/semantics are Wraithstep-specific — a future exclusion-style tattoo with different semantics (e.g.
  permanent rather than windowed, or conditional on some other state) implements its own registry/Prefix pair
  against this same convention, per `two-path-targeting-contract.md` §3's identical guidance for the inclusion
  case. Its own active-correction method (§2b) is likewise its own — `ForceNearbyHostilesOffWearer` is
  Wraithstep-owned, though its *shape* (clear `mindState.enemyTarget`/`meleeThreat`, end the current job, on
  activation and periodically thereafter) is the reusable part.
