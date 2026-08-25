# Phase 0 Research: Ember Ward Tattoo Effect

All items below were resolved by inspecting this repository's existing shipped code (features 001–010) and, where
RimWorld's or Combat Extended's own internal behavior mattered, by decompiling the real, locally installed
`Assembly-CSharp.dll` (RimWorld 1.6, via `ilspycmd`) and `CombatExtended.dll` (Steam Workshop item `2890901044`,
via `ilspycmd`) — no game code executed, read-only inspection only, same method features 007/010 already used.

## R1 — Reuse feature 002's value accessor and tier tracker unmodified

**Decision**: `TattooEffectValues` and `TattooTierProgress` (feature 002) are reused exactly as-is, the same way
every passive tattoo since Frost Sigil has reused them.

**Rationale**: Nothing about Ember Ward's shape needs a different value-override mechanism or a different
tier/counter model than every prior tattoo already uses successfully.

**Alternatives considered**: None — this is settled infrastructure per features 002/010's own contracts.

## R2 — Heat resistance maps to `StatDefOf.ComfyTemperatureMax`, via the existing `IProvidesTattooStatOffset` contract

**Decision**: Ember Ward's heat-resistance bonus (FR-001) is a positive offset to `StatDefOf.ComfyTemperatureMax`
(raising the pawn's maximum comfortable temperature), contributed via `IProvidesTattooStatOffset`, exactly
mirroring how Frost Sigil (feature 002) contributes a *negative* offset to `StatDefOf.ComfyTemperatureMin` for
cold resistance. Confirmed `StatDefOf.ComfyTemperatureMax` exists in RimWorld 1.6's `StatDefOf` (decompiled
`RimWorld.StatDefOf`) as the Min stat's direct sibling.

**New requirement this creates**: `TattooEffectStatPartInstaller` (feature 002) currently registers
`StatPart_TattooEffectOffset` on `ComfyTemperatureMin` but **not** `ComfyTemperatureMax` — this feature adds one
`Register(StatDefOf.ComfyTemperatureMax);` line there. This is an additive registration, not a change to any
existing tattoo's behavior (Frost Sigil doesn't touch `ComfyTemperatureMax` and never will).

**Rationale**: Reuses the exact existing stat-offset contract (feature 002) with zero new infrastructure beyond
one registration line — the same "small, additive registration" shape feature 005 already used when it needed
`MeleeCooldownFactor`/`RangedCooldownFactor` etc.

**Alternatives considered**: A custom "heat resistance" pseudo-stat with its own display/tooltip — rejected;
`ComfyTemperatureMax` is RimWorld's own real stat for exactly this concept and already shows in the pawn's stat
tab with a correct explanation via `StatPart_TattooEffectOffset.ExplanationPart` (feature 002), no new UI needed.

## R3 — Burn-damage reduction maps to `StatDefOf.ArmorRating_Heat`, already registered — and is CE-safe with zero branching

**Decision**: Ember Ward's ongoing "reduction to fire/burn damage taken" (FR-002/FR-005's non-full-ignore part) is
a positive offset to `StatDefOf.ArmorRating_Heat`, contributed via the same `IProvidesTattooStatOffset` contract.
**No new registration is needed** — `TattooEffectStatPartInstaller` already registers
`StatPart_TattooEffectOffset` on `StatDefOf.ArmorRating_Heat` (present since an earlier feature, unused by any
tattoo shipped so far — this feature is its first real consumer).

**How this actually reduces fire/burn damage (confirmed by decompiling `Verse.ArmorUtility` and the `Flame`/`Burn`
`DamageDef` XML in RimWorld's own `Data/Core/Defs/DamageDefs/Damages_Environmental.xml`)**:
- Both `DamageDefOf.Flame` and `DamageDefOf.Burn` (`Burn` inherits from `Flame` via `ParentName`) declare
  `<armorCategory>Heat</armorCategory>`.
- `Verse.ArmorUtility.GetPostArmorDamage(...)` — called from `DamageWorker_AddInjury.Apply` for every pawn hit
  under vanilla — reads `damageDef.armorCategory.armorRatingStat` generically (whatever stat the category names)
  and rolls vanilla's standard armor outcome against it: a chance to fully deflect (zero damage), a chance to
  halve the damage, or the full amount getting through, scaling with `armorRating - armorPenetration`. For a
  `Heat`-category `DamageDef`, that stat is `StatDefOf.ArmorRating_Heat` — precisely the stat this feature grants
  an offset to.
- **This is not a new mechanism** — it's the same armor pipeline vanilla apparel (e.g. anything with heat armor)
  already uses; Ember Ward's tattoo simply contributes to the same pawn-level stat apparel does.

**Combat Extended compatibility (Constitution Principle I) — confirmed to need zero CE-specific branching**:
decompiling `CombatExtended.dll`'s `Harmony_DamageWorker_AddInjury_ApplyDamageToPart` (a transpiler that reroutes
`DamageWorker_AddInjury`'s armor-block call from `Verse.ArmorUtility.GetPostArmorDamage` to CE's own
`ArmorUtilityCE.GetAfterArmorDamage`) shows CE's replacement armor system still resolves the stat to consult as
`dinfo.Def.armorCategory.armorRatingStat` — the exact same generic lookup vanilla uses, not a CE-specific
replacement stat (unlike, say, CE's `AimingAccuracy` replacing `ShootingAccuracyPawn`, which this codebase already
special-cases in `CombatExtendedInterop`). For `Flame`/`Burn`'s `Heat` category, that resolves to
`StatDefOf.ArmorRating_Heat` under **both** vanilla and CE — confirmed directly in `ArmorUtilityCE`'s decompiled
body (`StatExtension.GetStatValue(pawn, armorRatingStat, ...)` with `armorRatingStat` passed through unchanged
from `dinfo.Def.armorCategory.armorRatingStat`). **No `if (CombatExtendedInterop.IsLoaded)` branch is needed
anywhere in Ember Ward's reduction logic** — the single stat offset is read correctly by whichever armor pipeline
(vanilla or CE) is active, with no code awareness of which one is running.

**Rationale**: Reuses proven, already-registered infrastructure with zero new patches for the ongoing-reduction
half of the effect, and rides on a genuine cross-mod StatDef passthrough rather than needing dual-path CE code —
the best possible outcome for Principle I (a feature that's CE-correct *by construction*, not by branching).

**Alternatives considered**: A custom Harmony patch computing a flat percentage reduction directly on
`totalDamageDealt` in the existing `Patch_Pawn_PostApplyDamage_TattooOnHit` postfix — rejected; that patch runs
**after** damage has already been applied (see R4), so it cannot reduce the damage itself, only react to it having
happened. A dedicated `Pawn.PreApplyDamage` patch doing manual percentage math — rejected in favor of the stat
offset specifically *because* the existing armor pipeline already does this exact job, correctly, under both
vanilla and CE, for free.

## R4 — The progression counter and the Tier 2 full-ignore roll cannot reuse the existing `PostApplyDamage` postfix — they need a new `PreApplyDamage` prefix

**Decision**: A new Harmony **Prefix** patch on `Pawn.PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)`
(decompiled signature confirmed) is added — `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb` — dispatching a new,
generic interface (`IOnIncomingDamageTattooEffect`, R5) to the *about-to-be-damaged* pawn's own comps for every
incoming `DamageInfo`, before armor/health processing.

**Why the existing shared postfix (`Patch_Pawn_PostApplyDamage_TattooOnHit`, features 002/003/010) cannot serve
this purpose**, confirmed by decompiling `Verse.Thing.TakeDamage` and `Verse.Pawn.PreApplyDamage`:
1. `Thing.TakeDamage(DamageInfo dinfo)` calls `PreApplyDamage(ref dinfo, out absorbed)` **first**; if
   `absorbed == true`, it returns immediately — `dinfo.Def.Worker.Apply(...)` (which is what eventually calls
   `Pawn.PostApplyDamage`) **never runs** for a fully-absorbed hit. Since PRD §6/this feature's spec (Edge Cases)
   explicitly requires the progression counter to still increment on a Tier 2 full-ignore, any counter logic
   living downstream of `PostApplyDamage` would silently fail to count exactly the instances a full-ignore is
   supposed to still count.
2. Even for *non*-fully-absorbed hits, the existing postfix's own top-level guard
   (`if (totalDamageDealt <= 0f) return;`) is correct for its existing consumers (melee/ranged hit-landed/taken —
   "nothing consequential happened, don't dispatch") but wrong for Ember Ward: an instance Ember Ward's own armor
   contribution reduced all the way to zero is exactly the kind of "tattoo did its job" event PRD §6's "instances
   ... absorbed" wording is describing, not a no-op to be skipped.
3. Fire/burn damage frequently has no `Pawn` instigator at all — decompiling `RimWorld.Fire.DoFireDamage` shows
   its `DamageInfo`'s instigator is `instigator ?? this` (falling back to the `Fire` thing itself for
   environmental/building fires with no known igniter). The existing postfix's melee/ranged dispatch branches
   both require `dinfo.Instigator is Pawn` before dispatching anything — correctly, for their own purpose (they
   answer "which pawn's attack was this"), but this would silently exclude the common case of a colonist standing
   in a burning building from ever triggering Ember Ward's counter if reused as-is.

**Confirmed safe under Combat Extended**: CE patches `DamageWorker.Apply` (an unconditional-early-return prefix
gated on a wall-fragmentation setting) and `Thing.TakeDamage` (a postfix, ammo-exploder bookkeeping only) — neither
intercepts or bypasses `Pawn.PreApplyDamage`, and `RimWorld.Fire.DoFireDamage` (the source of ordinary burn-tick
damage) is core RimWorld code CE does not patch. The new prefix fires identically whether or not CE is loaded.

**Rationale**: A new, narrowly-scoped prefix is the minimum change that correctly supports both "count every
qualifying instance, including full-ignores" and "count instances with no pawn instigator," neither of which the
existing postfix's contract can be stretched to cover without breaking its existing consumers' semantics.

**Alternatives considered**: Extending `Patch_Pawn_PostApplyDamage_TattooOnHit` with a third, differently-gated
branch — rejected; that patch's entire existing contract (R2 above) is postfix/post-armor/instigator-is-a-Pawn,
and bolting on a pre-armor, instigator-agnostic exception would make the file's single top-level guard clause
incorrect for its own new branch, inviting exactly the kind of subtle bug this research step exists to avoid.

## R5 — `IOnIncomingDamageTattooEffect`: new reuse point, tattoo-agnostic per FR-013

**Decision**: New interface, dispatched unconditionally (for every `DamageDef`, every pawn hit) by the new prefix
(R4) to every comp on the about-to-be-damaged pawn implementing it — mirroring `IProvidesTattooStatOffset`'s own
"implementer filters, dispatcher doesn't" convention (`GetStatOffset` is called for every stat query; implementers
return `0f` for stats they don't care about) rather than teaching the patch which `DamageDef`s matter to which
tattoo.

```csharp
public interface IOnIncomingDamageTattooEffect
{
    bool TryAbsorbIncomingDamage(DamageInfo dinfo);
}
```

Returning `true` fully absorbs the instance (the prefix sets `absorbed = true` and returns `false`, skipping
vanilla's/CE's own damage-application pipeline entirely for it); returning `false` lets the instance proceed
normally to `PreApplyDamage`'s own body (and thus armor, R3). A comp implementing this is entirely responsible for
its own `DamageDef` filtering, its own progression-counter bookkeeping (R6), and its own tier-gated roll — the
dispatcher has no tattoo-specific knowledge.

**Rationale**: This is the "damage taken by type" reuse point spec.md's User Story 3 calls for — generic enough
that a future tattoo built around a different `DamageDef` (e.g. a different elemental theme) could implement the
same interface with its own filtering, without touching this patch or Ember Ward's own comp, exactly matching how
every prior reuse point in this codebase (`IOnMeleeHitTattooEffect`, `IOnRangedHitLandedTattooEffect`,
`IOnMeleeHitLandedTattooEffect`) already generalizes past its first tattoo.

**Alternatives considered**: A `DamageDef[] ReactsToDamageDefs` property the patch checks before calling, letting
the dispatcher skip irrelevant comps cheaply — rejected as unnecessary complexity; a pawn typically carries very
few hediff comps, so the per-hit cost of calling through and returning `false` immediately is negligible (the same
judgment call features 002/003 already made for their own dispatch loops, which iterate all comps unconditionally
too).

## R6 — Progression counter semantics: "absorbed" = a qualifying-type instance that reached this comp with nonzero incoming amount

**Decision**: `HediffComp_EmberWardEffect.TryAbsorbIncomingDamage` increments its `TattooTierProgress` counter
once per call where `dinfo.Def` is `Flame` or `Burn` **and** `dinfo.Amount > 0f`, regardless of whether the Tier 2
full-ignore roll (if attempted) then succeeds — the counter increment happens before that roll, not
conditioned on its outcome. This satisfies spec.md's edge case ("a Tier 2 full-ignore roll succeeds ... the
progression counter still increments") directly, since both live in the same method call, ordered deliberately.

**Rationale**: PRD §6's "instances of fire/burn damage absorbed" doesn't distinguish partial vs. full absorption,
and this codebase's existing precedent (Frost Sigil's "melee hits taken where the slow procs," Vampiric Thorn's
"melee hits landed") already favors the simplest self-consistent reading over an unmeasurable finer distinction —
here, "was this pawn wearing Ember Ward when a fire/burn instance reached it" is directly observable at this
dispatch point; "how much did Ember Ward's own stat offset specifically contribute to the eventual reduction" is
not cleanly separable from base armor/other effects without instrumenting `ArmorUtility` itself, which is out of
proportion to what the PRD is asking for.

**Alternatives considered**: Gating the counter on `dinfo.Amount` compared against some computed "would have been
absorbed" delta — rejected as unmeasurable at this dispatch point (pre-armor) without duplicating `ArmorUtility`'s
own math, and unnecessary complexity for a placeholder-numbers feature (Constitution Principle III).

## R7 — No automated test harness; manual in-game verification only

**Decision**: No automated test suite, unchanged from every prior feature (001–010) — RimWorld's `Verse`/`RimWorld`
types aren't practically runnable outside the live game process. Verified manually per Constitution Principle II,
via `quickstart.md`.

**Rationale**: Consistent with this project's established testing approach; no aspect of Ember Ward's design
introduces a testability need the project doesn't already have an answer for.

## R8 — Placeholder values stay small and fast-to-test, not "plausible-looking"

**Decision**: Ember Ward's Tier 1/Tier 2 heat-resistance and `ArmorRating_Heat` offsets, the Tier 2 full-ignore
chance, and the Tier 2 threshold are deliberately small, fast-to-verify placeholder numbers (Constitution
Principle III; PRD §9), matching feature 006's research.md R8 precedent — e.g. a low tier-2 threshold so a tester
doesn't need dozens of repeated fire exposures to observe the tier-up in a single manual test pass.

**Rationale**: Every prior tattoo already follows this precedent; no reason to deviate here.

## R9 — CORRECTION to R3: RimWorld Dev Mode's "Apply damage" tool cannot validate the `ArmorRating_Heat` reduction
under CE, because CE explicitly bypasses its own armor pipeline for instigator-less damage — found live during
implementation testing, not during planning

**What happened**: Live testing (feature 011 implementation, post-T009) applied `ArmorRating_Heat` values of
`0.000` (no tattoo), `0.080` (Tier 1), `0.160` (Tier 2), and finally an artificial `2.000` (25× the Tier 2
placeholder, confirmed via debug logging to be the live, correctly-computed value RimWorld's `StatWorker` was
returning at the moment of each hit) against the same pawn, using RimWorld Dev Mode's "Apply damage" tool
(`Verse.DebugTools_Health.Options_ApplyDamage`, see `quickstart.md` Prerequisites) to deal `Burn` damage. The
actual damage dealt was **identical (3.3 of 5.0 requested) at all four values**, including the deliberately
absurd `2.000` — a value large enough that vanilla RimWorld's own `ArmorUtility.ApplyArmor` would treat as
near-guaranteed full deflection (R3). This is conclusive evidence the stat value has zero effect on damage dealt
via this specific test method — not evidence the stat itself is broken (the debug logging added specifically for
this investigation, `Patch_Pawn_PostApplyDamage_TattooDamageAbsorbDebug`, confirmed `pawn.GetStatValue
(StatDefOf.ArmorRating_Heat)` correctly read `0.000`/`0.080`/`0.160`/`2.000` live and in sync with tier/removal
state at every single hit — the offset mechanism (R2/R3) is proven correct).

**Root cause, found by decompiling `CombatExtended.ArmorUtilityCE.GetAfterArmorDamage`** (the method CE reroutes
`DamageWorker_AddInjury.ApplyDamageToPart` into, per R3's own earlier decompile): its very first check is

```csharp
if (dinfo.Def.armorCategory == null || (!(dinfo.Weapon?.projectile is ProjectilePropertiesCE)
    && Verb_MeleeAttackCE.LastAttackVerb == null && dinfo.Weapon == null && dinfo.Instigator == null))
{
    return originalDinfo;  // short-circuits — no armor category lookup, no armorRatingStat read, nothing
}
```

RimWorld Dev Mode's "Apply damage" tool constructs its `DamageInfo` as `new DamageInfo(def, 5f)` — no weapon, no
active melee verb, and critically **no instigator argument**, so `Instigator` is `null`. All four conditions in
that second clause are true, so CE's entire armor pipeline is skipped outright for this specific kind of hit —
confirmed the actual, taken code path (not merely a plausible-looking branch), since the artificial `2.000` test
produced no change whatsoever, which only a complete bypass (not merely a small-magnitude effect) explains.

**Why this does NOT contradict R3's own finding**: R3 established that CE's armor pipeline, *when it runs*,
resolves `Heat`-category damage to `StatDefOf.ArmorRating_Heat` exactly like vanilla — that remains true and is
unaffected by this discovery. What R3 didn't anticipate is that CE's pipeline sometimes doesn't run *at all*,
gated on `Instigator == null` among other things. Real fire damage is not affected by this gap:
`RimWorld.Fire.DoFireDamage` (R4) constructs its own `DamageInfo` with `instigator ?? this` — the null-coalescing
fallback to the `Fire` Thing itself means `Instigator` is **never** null for real environmental fire tick damage,
real weapon-based Flame/Burn attacks always carry a `Weapon`/`Instigator` too — so CE's bypass condition should
not trigger for genuine gameplay damage, only for instigator-less synthetic damage like the debug tool's.

**Consequence for verification**: `quickstart.md`'s Scenario 2 needs a correction — the "Apply damage" debug tool
(recommended in that scenario's Prerequisites) cannot validate the ongoing burn-damage reduction specifically
*when Combat Extended is loaded*, since it always triggers this bypass regardless of `ArmorRating_Heat`'s value.
It remains valid for every other scenario this feature's `quickstart.md` uses it for (Scenario 3's progression
counter, Scenario 4's full-ignore roll — neither depends on CE's armor pipeline at all, only on our own
`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`, which runs regardless of instigator) and remains fully valid for
Scenario 2 when CE is **not** loaded (vanilla `ArmorUtility.GetPostArmorDamage`, R3, has no such instigator gate).
A correct CE-loaded validation of Scenario 2's reduction claim requires damage with a real instigator — an actual
fire (`RimWorld.Fire`, ignited near/on the pawn) or a real Flame-dealing weapon attack, not the debug tool.

**Alternatives considered**: Patching around CE's bypass (e.g., forcing an instigator onto debug-tool damage) —
rejected; this is a testing-methodology gap, not a defect in Ember Ward's own code, and RimWorld's own Fire
mechanism already produces damage the bypass doesn't apply to, so no code change is warranted, only a testing
guidance correction.

## R10 — CORRECTION to R2-R3: the `ArmorRating_Heat` stat offset genuinely does not work under Combat Extended
for an ordinary human, at any magnitude — found via real-fire live testing (not the debug tool) after R9's fix
still showed no measurable reduction, root-caused by decompiling `GetAmbientPostArmorDamage` in full. This is a
real code defect, fixed in this same pass, not merely a testing-methodology note like R9.

**What happened**: After R9 identified the debug tool's bypass, live testing switched to real fire
(`RimWorld.Fire`, dev-spawned near the test pawn — a non-null `Instigator`, confirmed not to trigger R9's
bypass) and compared `ArmorRating_Heat=0.160` (Tier 2, 6 samples, avg. reduction ≈ 50%) against
`ArmorRating_Heat=0.000` (no tattoo, 8 samples, avg. reduction ≈ 52%) on the same pawn. The two conditions were
statistically indistinguishable — both groups showed the same spread of outcomes (50% and ~52-65% reduction
ratios) interchangeably, including a `0.160`-tattooed sample landing at the same ~52% ratio as an untattooed
sample. This ruled out R9's bypass (real fire damage doesn't trigger it) while still showing zero attributable
effect from the stat, which is only possible if a *second*, independent gap exists.

**Root cause, found by decompiling the full `ArmorUtilityCE.GetAmbientPostArmorDamage` method** (the method real
fire-tick damage actually reaches, confirmed via `IsAmbientDamage()`'s `DamageDefExtensionCE.isAmbientDamage`
flag routing `Flame`/`Burn` there specifically):

```csharp
float num = 1f + penAmount;
if (part.IsInGroup(CE_BodyPartGroupDefOf.CoveredByNaturalArmor))
{
    num -= StatExtension.GetStatValue(pawn, armorRatingStat, true, -1);   // the PAWN's own stat
}
...
foreach (Apparel item in wornApparel)
{
    if (item.def.apparel.CoversBodyPart(part))
        num -= StatExtension.GetStatValue(item, armorRatingStat, true, -1);   // the APPAREL's own stat
}
```

The pawn's own `armorRatingStat` value — exactly where `IProvidesTattooStatOffset`/`GetStatOffset`'s contribution
lands, via `StatPart_TattooEffectOffset` — is **only ever subtracted when the hit body part belongs to the
`CoveredByNaturalArmor` body-part group**, a classification for innate/natural armor (thick hides, plating), not
ordinary human anatomy. Only *worn apparel's own* stat value is read in the general case, a completely different
`Thing`'s stat query that a pawn-level Hediff contribution can never reach. This is unconditional — not a
small-magnitude effect, not probabilistic — so no XML value, however large, would ever produce a measurable
difference for a normal human under this formula. R9's decompiled evidence (an artificial `2.000` producing zero
change under the debug tool) can't distinguish "small effect" from "no effect" on its own, but this direct code
read settles it unambiguously: the stat offset path is architecturally inert for this tattoo's primary claim,
specifically under CE, specifically for humans.

**Why this does NOT contradict R2's finding for vanilla RimWorld**: vanilla's `ArmorUtility.GetPostArmorDamage`
(decompiled during planning) reads `pawn.GetStatValue(armorRatingStat)` directly and unconditionally — no body-
part-group gate exists there. The stat-offset mechanism remains fully correct and sufficient without CE loaded;
this gap is exclusively a CE-side formula quirk.

**Fix implemented**: rather than trying to route around CE's body-part-group gate (e.g., attempting to reclassify
human body parts into `CoveredByNaturalArmor`, which would have unpredictable side effects on unrelated CE armor
math for every other damage type), `IOnIncomingDamageTattooEffect.TryAbsorbIncomingDamage` now takes `ref
DamageInfo dinfo` instead of by value, and `HediffComp_EmberWardEffect` directly reduces `dinfo.Amount` (via
`dinfo.SetAmount(...)`) by the tier-appropriate `armorRatingHeatTier1`/`Tier2` fraction whenever
`CombatExtendedInterop.IsLoaded`, immediately after the tier-progression registration (so a tier-up-triggering hit
correctly uses its new tier's value) and before the Tier 2 full-ignore roll. This runs inside
`Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`'s prefix, strictly before `Pawn.PreApplyDamage`'s own body (and
therefore strictly before any CE armor processing) ever executes, so it's guaranteed to apply regardless of which
internal CE sub-path (`GetAmbientPostArmorDamage`, the direct-hit path, or anything else) this specific instance
would otherwise take — the fix doesn't depend on correctly predicting or replicating CE's own branching, only on
running first. Vanilla RimWorld is untouched by this change (the `CombatExtendedInterop.IsLoaded` guard means the
new code path never executes there), continuing to rely solely on the already-correct stat-offset mechanism
(R2/R3). The `ArmorRating_Heat` `GetStatOffset` contribution itself is left in place unmodified — it's still
correct and load-bearing for vanilla, and remains visible on the Stats tab for player transparency even under CE
(where it's now effectively a display-only value for this specific formula, not misleading since a real
reduction is still happening, just through a different mechanism).

**Consequence for the interface contract**: `IOnIncomingDamageTattooEffect`'s signature changed from
`TryAbsorbIncomingDamage(DamageInfo dinfo)` to `TryAbsorbIncomingDamage(ref DamageInfo dinfo)` — a breaking
change to the reuse point established earlier in this same feature, made before any other tattoo adopted it
(Ember Ward remains the sole consumer, confirmed via the US3 review), so no migration was needed. A future tattoo
built on this interface can now mutate `dinfo.Amount` for its own partial-reduction needs, not just fully
absorb — see `contracts/incoming-damage-contract.md` for the updated contract.

**Alternatives considered**: Reclassifying human `BodyPartDef`s into `CE_BodyPartGroupDefOf.CoveredByNaturalArmor`
via a Def patch — rejected; this would change CE's own armor resolution for every other damage category and
every other mod/tattoo's interaction with human body parts, a much larger and riskier blast radius than a
tattoo-scoped direct reduction. Applying the direct reduction unconditionally (regardless of CE presence) instead
of behind `CombatExtendedInterop.IsLoaded` — rejected; vanilla's stat-offset path is already correct and proven,
and applying both simultaneously under vanilla would double-count the reduction.
