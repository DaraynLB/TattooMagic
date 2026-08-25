# Phase 0 Research: Vampiric Thorn Tattoo Effect

Grounded against the actual shipped code of features 001–009 (read directly, not assumed from spec/PRD text alone)
and, for R4, against the real `Assembly-CSharp.dll` (decompiled read-only via `ilspycmd`, no game code executed).

## R1: How does Vampiric Thorn reuse feature 002's value accessor and tier tracker?

**Decision**: Identically to every prior tattoo — `HediffComp_VampiricThornEffect` composes a `TattooTierProgress`
field, calls `ExposeData()` on it from `CompExposeData()`, and reads every tunable value through
`TattooEffectValues.Get(ScopeKey, valueKey, xmlDefault)` with `ScopeKey = parent.def.defName`
(`TattooMagic_Hediff_VampiricThorn`). No change to either class.

**Rationale**: FR-012/FR-013 require reuse without modification; both pieces are already tattoo-agnostic
(feature 002 research.md R5) and Vampiric Thorn's needs (a tiered counter, override-ready values) are exactly what
they were built for — the sixth tattoo to reuse them unmodified.

**Alternatives considered**: n/a — direct, unmodified reuse, not a design choice.

## R2: How does Vampiric Thorn detect "the wearer's own melee attack landed," and how is it dispatched?

**Decision**: A new interface, `IOnMeleeHitLandedTattooEffect` (`void OnMeleeHitLanded(Thing target, DamageInfo
dinfo, float damageDealt, bool killedTarget)`), dispatched from the *same* existing `Pawn.PostApplyDamage` postfix
(`Patch_Pawn_PostApplyDamage_TattooOnHit`) that features 002 and 003 already extended — as a **second** clause
inside the existing `isMelee` branch, alongside the untouched `DispatchMeleeHit` call (which notifies the *struck*
pawn's `IOnMeleeHitTattooEffect` comps, feature 002). The new clause, `DispatchMeleeHitLanded`, notifies the
*attacking* pawn's own comps instead — the melee-attacker-side counterpart to feature 003's ranged-attacker-side
`DispatchRangedHit`, mirroring that method's exact shape (walk `attacker.health.hediffSet.hediffs`, check each
comp against the interface, dispatch, credit one tattoo-mastery event via
`TattooTrackerUtility.GetTracker(attacker)?.RegisterMasteryActivation()`).

Read directly from `Source/Patches/Patch_Pawn_PostApplyDamage_TattooOnHit.cs`: the postfix already computes
`bool isMelee = dinfo.Tool != null;` and, when true, calls `DispatchMeleeHit(__instance, attacker, dinfo,
totalDamageDealt)`. Adding `DispatchMeleeHitLanded(attacker, __instance, dinfo, totalDamageDealt)` immediately
after is a strictly additive change to this one shared file — the existing melee-taken clause, the ranged clause,
and every existing consumer (`HediffComp_FrostSigilEffect`, `HediffComp_SerpentsEyeEffect`) are untouched.

**Rationale**: Reusing the same patch/method keeps "something hit something" as a single tattoo-agnostic dispatch
point, exactly matching that file's own stated intent and the precedent feature 003 already set when it added the
ranged clause alongside the original melee one without modifying it. This satisfies FR-013's "add new, similarly
generic reuse point(s) rather than hardcoding Vampiric-Thorn-specific detection logic into a one-off location."

**Alternatives considered**: A wholly separate new Harmony patch on `Pawn.PostApplyDamage` (rejected — would
double-postfix the same vanilla method for no benefit, identical reasoning to feature 003 R2's rejection of the
same option for the ranged case).

## R3: Is the melee attack's target guaranteed to be a `Pawn`, and does that matter for the interface signature?

**Decision**: In practice, yes for this specific dispatch path — `Patch_Pawn_PostApplyDamage_TattooOnHit` is
`[HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]`, i.e. it only fires when `Pawn.PostApplyDamage`
itself runs, which (per `Thing.TakeDamage`'s polymorphic dispatch) only happens when the *damaged Thing* is a
`Pawn`. So `__instance` — the struck pawn passed as `target` to `DispatchMeleeHitLanded` — is always a `Pawn` for
this feature's dispatch, meaning a melee attack landing on a non-Pawn `Thing` (a wall, a door, a plant) never
reaches this patch at all and therefore never triggers Vampiric Thorn's lifesteal or counter.

The interface signature nonetheless takes `Thing target` (not `Pawn target`), matching
`IOnRangedHitLandedTattooEffect`'s existing signature exactly (feature 003), which carries the same documented
caveat ("target is not guaranteed to be a Pawn") for forward-compatibility with any future dispatch site that
might feed the same interface from a context where the target genuinely could be a non-Pawn `Thing` — keeping the
contract's shape consistent across both "hit landed" reuse points costs nothing and avoids a signature mismatch if
a future tattoo wants to treat both symmetrically.

**Rationale**: This resolves the one place the spec's own edge cases don't explicitly cover (melee attacks against
non-Pawn things) — not because it needs a special rule, but because the existing dispatch mechanism structurally
never surfaces that case in the first place, consistent with how the same mechanism already excludes non-Pawn
ranged targets today.

**Alternatives considered**: Typing the interface as `Pawn target` since it's always true today (rejected — breaks
signature symmetry with `IOnRangedHitLandedTattooEffect` for no behavioral gain, and forecloses reuse by a
hypothetical future dispatch site with a genuinely non-Pawn target).

## R4: Is `Pawn.Dead` reliably observable inside the shared postfix to detect a killing blow, or is a separate patch needed?

**Decision**: `Pawn.Dead` is reliably `true` by the time the postfix runs for a lethal hit — no separate patch on
`Pawn.Kill` or elsewhere is needed. Confirmed by decompiling RimWorld 1.6's real `Assembly-CSharp.dll` (not the
stripped `Krafs.Rimworld.Ref` reference assembly): `Pawn.PostApplyDamage` calls `health.PostApplyDamage(dinfo,
totalDamageDealt)` synchronously, which itself calls `this.pawn.Kill(dinfo)` synchronously when `ShouldBeDead()`
is true, before either method returns. `Pawn.PostApplyDamage` even self-checks `if (Dead) return;` immediately
after that call, confirming the death transition is expected to have already completed by that point in its own
control flow. Since a Harmony `Postfix` runs only after the entire patched method (including every nested call)
has returned, `__instance.Dead` accurately reflects the post-hit death state inside
`Patch_Pawn_PostApplyDamage_TattooOnHit`'s postfix.

**Decision (design consequence)**: The kill-blow signal needs no new patch or interface — `DispatchMeleeHitLanded`
computes `bool killedTarget = struckPawn.Dead;` once, at the same dispatch site as the ordinary landed-hit
notification, and passes it as `OnMeleeHitLanded`'s fourth parameter. A comp that only cares about "landed" ignores
the flag; `HediffComp_VampiricThornEffect` (Tier 2 only) checks it to open the post-kill recovery window. This
keeps User Story 3's "one new reuse point" framing (SC-007 refers to it in the singular) accurate — the kill-blow
signal is an additional field on the same call, not a second interface or a second Harmony patch.

**Rationale**: This is guaranteed by vanilla's own synchronous control flow (verified by inspection, not
incidental/version-fragile behavior), so it needs no empirical in-game confirmation step in `quickstart.md` beyond
the ordinary "does the kill bonus actually apply" behavioral check every other feature's quickstart already
requires. Folding the kill flag into the existing landed-hit call is also the minimal-surface design: it adds one
`bool` parameter to one new interface rather than a second interface plus a second dispatch loop for what is, from
the shared patch's point of view, the same event with one extra bit of information already on hand for free
(`__instance.Dead`).

**Alternatives considered**: A second interface (e.g. `IOnMeleeKillTattooEffect`) with its own dispatch clause
(rejected — doubles the dispatch machinery for information the existing dispatch site can compute for free, and
splits what the spec's Key Entities section describes as one reuse point plus "whatever's needed for" the kill
signal into two parallel mechanisms without a concrete second consumer justifying the split); a new patch on
`Pawn.Kill` (rejected — unnecessary once `Pawn.Dead` is confirmed reliably observable in the existing postfix, and
would require correlating a separate `Kill` call back to the specific `DamageInfo`/attacker that caused it, which
the existing postfix already has on hand directly).

## R5: How does Vampiric Thorn heal the wearer's own injuries, for both the instant per-hit lifesteal and the Tier 2 time-boxed post-kill window?

**Decision**: Extract Bloodrune's (feature 007) inline "scan for the most-severe open, non-permanent
`Hediff_Injury`, heal it, move to the next once it closes" logic out of `HediffComp_BloodruneEffect.HealTick()`
into a new shared static helper, `TattooHealingUtility.HealWorstInjury(Pawn pawn, float amount)`
(`Source/Effects/`), which **loops** — heal the current worst injury by up to the remaining amount, and if amount
remains after that injury is fully closed, move to the next-worst and continue — until the amount is exhausted or
there is nothing left to heal. `HediffComp_BloodruneEffect.HealTick()` is updated to call this helper instead of
duplicating the scan inline; its own per-interval behavior is unchanged in the normal case (a single interval's
heal amount is smaller than the worst wound's severity), and gains one incidental improvement: if a single
interval's heal amount now *exceeds* the current worst wound's remaining severity, the leftover no longer waits
for the next tick to roll onto the next-worst wound — it rolls over within the same call, which is a strict
generalization of the existing behavior, not a change to it.

Vampiric Thorn's own comp calls this same helper directly, twice, for two different shapes of the spec's healing
requirement:
- The instant per-hit lifesteal (FR-001) — a single, immediate call, `TattooHealingUtility.HealWorstInjury(Pawn,
  lifestealAmount)`, executed synchronously inside `OnMeleeHitLanded`. Because the helper already loops internally
  to roll leftover healing onto the next-most-severe wound within one call, this directly satisfies the spec's
  edge case ("if the wearer's lifesteal-worthy hit would heal more than their worst wound's remaining severity...
  any leftover healing amount rolls onto the wearer's next-most-severe open wound") without needing Bloodrune's
  tick-interval machinery at all — there is no "burst" for the per-hit heal, it's instant.
- The Tier 2 post-kill recovery window (FR-005) — a Bloodrune-shaped tick-interval heal-over-time
  (`postKillHealRemaining`/`nextPostKillHealTick`/`postKillWindowEndTick` fields, a `CompPostTick` loop calling
  the same helper with a per-interval portion), refreshed rather than stacked on a second kill during an active
  window (FR-006), mirroring Bloodrune's/Guardian's Call's/Stormlash's "refresh, not stack" precedent for
  time-boxed state.

**Rationale**: FR-014 requires reusing "the existing wound-healing approach... rather than introducing a second,
parallel mechanism for healing a pawn's injuries." Before this feature, that approach existed only as private,
inline logic inside `HediffComp_BloodruneEffect`, with no callable shared form — a second tattoo needing the
identical pattern is exactly the point at which duplicating it inline (as a second, parallel implementation) would
violate FR-014's actual intent, even though it would be trivial to copy-paste. Extracting it now, with Bloodrune
updated to call the same helper, is what makes "reuse the existing approach" literally true rather than "reuse the
existing algorithm by copying it," and costs nothing in risk since Bloodrune's own observable behavior is
unchanged in the case its existing quickstart already verified.

**Alternatives considered**: Inline-duplicating Bloodrune's scan logic directly inside
`HediffComp_VampiricThornEffect` (rejected — satisfies FR-014's letter loosely at best, since it's a second,
parallel copy of the same mechanism, exactly what FR-014 says not to do, now that a second real consumer exists to
justify extraction); leaving Bloodrune's own code untouched and only adding the new shared helper for Vampiric
Thorn to call (rejected — would leave two different pieces of code implementing the same "heal worst injury,
roll over leftover" behavior with no guarantee they stay in sync, undermining the "one reusable approach" FR-014
asks for).

**Hotfix discovered during live testing**: the first shipped version of `TattooHealingUtility.HealWorstInjury`'s
worst-injury selection didn't require `Severity > 0f`, only `!injury.IsPermanent()` plus the strictly-greater
comparison. Once the internal `while` loop fully closed a pawn's last remaining open injury (severity to exactly
`0`) while `remaining` was still positive, the next iteration re-scanned and found that same now-zero-severity
hediff again — `Hediff.Heal()` doesn't remove a closed hediff from `hediffSet.hediffs` synchronously, only on a
later health tick — so it kept getting re-selected as "worst" with a computed `healAmount` of `0` forever, an
infinite loop that hung the game (confirmed live: the game froze, and Windows offered to close the unresponsive
process). This was a genuine regression the extraction introduced, not present in Bloodrune's original code:
Bloodrune previously only ever tried once per 30-tick interval (bounded by the tick cadence), never spending an
entire multi-HP budget in one synchronous call the way the shared loop now does for both Bloodrune's own burst and
Vampiric Thorn's post-kill window. Fixed by requiring `injury.Severity > 0f` in the selection condition, so a
just-closed injury is correctly excluded and the loop breaks via `worst == null` instead of re-selecting it —
each iteration now strictly decreases `remaining` (since both `remaining` and `worst.Severity` are guaranteed
positive when a candidate is selected), guaranteeing termination within at most as many iterations as the pawn has
open non-permanent injuries.

**Note on ordering (compound tier-up + kill)**: `OnMeleeHitLanded` (data-model.md) calls
`tierState.TryRegisterQualifyingEvent` before checking `tierState.tier >= 2 && killedTarget`. This is intentional:
a hit that simultaneously crosses the Tier 2 threshold *and* kills its target sees the already-updated `tier`
value by the time the kill-window check runs, so that single hit both triggers the Tier 1→2 upgrade and opens the
post-kill recovery window — not a coincidence of call order, and not required to be sequenced any other way.

## R6: Combat Extended — does anything here need CE-specific branching?

**Decision**: No CE-specific branching is needed anywhere in this feature, confirmed rather than assumed.

- **Healing**: Identical reasoning to Bloodrune's own confirmed finding (feature 007 research.md R4) — healing an
  injury's severity via `Hediff_Injury.Heal(float)` is a core `Verse`/`RimWorld` health-system concept CE does not
  touch or reinterpret; CE's own combat model changes how damage is dealt and armor is resolved, not how an
  already-inflicted injury heals afterward. This applies identically to Vampiric Thorn's lifesteal and post-kill
  recovery, both of which route through the same `Hediff_Injury.Heal` API via `TattooHealingUtility` (R5).
- **Melee-hit-landed detection**: The dispatch path this feature adds is the same `Pawn.PostApplyDamage` postfix
  already confirmed, in production, to correctly observe melee hits under Combat Extended — Frost Sigil's own
  `IOnMeleeHitTattooEffect` consumer (feature 002) has already shipped and been verified with CE loaded (feature
  002 quickstart Scenario 6), exercising the exact same `isMelee` branch and the exact same `dinfo`/`totalDamageDealt`
  values this feature's new clause reads. There is no reason CE's melee pipeline would populate those values
  differently for a second consumer of the same already-verified branch.
- **Kill detection**: `Pawn.Dead`/`Pawn.Kill` (R4) are vanilla `Verse` pawn-lifecycle concepts CE does not
  override or replace with its own death pipeline — CE modifies damage/armor resolution upstream of this point,
  not the pawn death transition itself.

Consistent with FR-016's explicit instruction, this was confirmed by inspection during planning (CE is installed
locally, Steam Workshop item `2890901044`, `CETeam.CombatExtended`) rather than assumed; `quickstart.md` still
includes a CE-loaded pass as the standing "loads cleanly and behaves correctly either way" check (SC-008), same
as every non-CE-branching tattoo since Bloodrune.

**Alternatives considered**: Proactively adding a `CombatExtendedInterop` check "just in case" (rejected — same
reasoning as Bloodrune's own R4: this project's convention is to branch only where CE genuinely replaces something
this feature touches; nothing here qualifies).

## R7: Testing strategy

**Decision**: No automated test project, unchanged from every prior feature (research.md precedent, most recently
restated in feature 007 R7/009). Verification is manual, in Dev Mode, following `quickstart.md`, with a required
non-CE pass and a standing CE-loaded "loads cleanly" pass (R6 — this feature's defining behaviors don't depend on
CE, unlike Serpent's Eye's).

**Rationale**: Unchanged reasoning from every prior feature's own research.md — RimWorld's `Verse`/`RimWorld` game
types aren't practically runnable outside the game process, and Constitution Principle II explicitly treats manual
in-game verification as the actual completion gate regardless of what a compile check or hypothetical unit test
would show.

**Alternatives considered**: n/a — unchanged from prior features.
