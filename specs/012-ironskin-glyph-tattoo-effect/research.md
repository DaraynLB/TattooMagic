# Phase 0 Research: Ironskin Glyph Tattoo Effect

All items below were resolved by inspecting this repository's existing shipped code (features 001–011) and, where
Combat Extended's own internal behavior mattered, by decompiling the real, locally installed `CombatExtended.dll`
(Steam Workshop item `2890901044`, via `ilspycmd`) and RimWorld's own `Assembly-CSharp.dll` (1.6) — no game code
executed, read-only inspection only, the same method features 007/010/011 already used. This feature's Input
explicitly required an independent re-verification of CE's *non-ambient* (direct-hit) armor path rather than
assuming feature 011's ambient-path finding transfers unchanged — R3 below is that independent verification.

## R1 — Reuse features 002/011's value accessor, tier tracker, stat-offset contract, and incoming-damage reuse
point, all unmodified

**Decision**: `TattooEffectValues`, `TattooTierProgress`, `IProvidesTattooStatOffset`,
`StatPart_TattooEffectOffset`, `IOnIncomingDamageTattooEffect`, and `Patch_Pawn_PreApplyDamage_TattooDamageAbsorb`
(features 002/011) are reused exactly as-is — no signature or behavior change to any of them.

**Rationale**: Ironskin Glyph's shape (a passive armor-rating stat offset plus a guaranteed-under-CE damage
reduction plus a tiered progression counter with a Tier 2 exclusive proc) is structurally identical to Ember Ward's
(feature 011) own shape, just against a different `DamageDef`/`StatDef` pairing (Sharp/Blunt instead of Heat) —
exactly the reuse User Story 3 and FR-013 call for.

**New requirement this creates**: none. `TattooEffectStatPartInstaller` (feature 002) already registers
`StatPart_TattooEffectOffset` on both `StatDefOf.ArmorRating_Sharp` and `StatDefOf.ArmorRating_Blunt` (present
since an earlier feature, unused by any tattoo shipped so far — this feature is their first real consumer, mirroring
how feature 011 was `ArmorRating_Heat`'s first real consumer). No new registration line is needed.

**Alternatives considered**: None — this is settled infrastructure per features 002/003/010/011's own contracts,
and the spec's own User Story 3 scope note already predicts zero new shared infrastructure.

## R2 — Armor-rating bonus filters by `armorCategory.armorRatingStat`, not an enumerated `DamageDef` list

**Decision**: Both the `IProvidesTattooStatOffset.GetStatOffset` side and the `IOnIncomingDamageTattooEffect.
TryAbsorbIncomingDamage` side filter generically on **which armor-category stat a given `DamageDef` resolves to**
(`dinfo.Def.armorCategory?.armorRatingStat`), matching it against `StatDefOf.ArmorRating_Sharp` or
`StatDefOf.ArmorRating_Blunt`, rather than enumerating individual `DamageDef`s (`Bullet`, `Stab`, `Cut`, `Blunt`,
`Crush`, etc.).

**Rationale**: Unlike Ember Ward's two related `DamageDef`s (`Flame`/`Burn`, one a `ParentName` child of the
other), "physical (sharp or blunt) damage" spans an open-ended set of vanilla and modded `DamageDef`s (melee tools,
projectiles, explosions, traps). Decompiling `Verse.DamageDef` confirms every `DamageDef` already declares its own
`armorCategory` (a `DamageArmorCategoryDef`), and `DamageArmorCategoryDef.armorRatingStat` is exactly the `StatDef`
vanilla's `ArmorUtility` (and CE's `ArmorUtilityCE`, R3) already use to decide which armor stat a given hit
consults — filtering on the same field the game's own armor pipeline uses is the correct generalization, robust
to any current or future `DamageDef` (including modded ones) without needing this tattoo's own maintained list.
Confirmed `RimWorld.DamageArmorCategoryDefOf` only exposes a `Sharp` constant (no `Blunt` constant exists as a
`[DefOf]` field) — irrelevant to this design since the comparison is against `armorRatingStat` (`StatDefOf.
ArmorRating_Sharp`/`ArmorRating_Blunt`, both of which do have well-known `StatDefOf` constants), not against the
category `Def` itself.

**Alternatives considered**: An explicit `DamageDef[]`/list-based filter (Ember Ward's own approach) — rejected;
appropriate for Ember Ward's exactly-two-related-defs case, but would require this tattoo to maintain and update a
list against an open-ended, mod-extensible set of "physical" `DamageDef`s, exactly the kind of brittleness the
armor-category-stat lookup avoids for free.

## R3 — CE's non-ambient (direct-hit) armor path has the SAME pawn-stat-reading gap R10 (feature 011) found in the
ambient path — confirmed by decompiling `ArmorUtilityCE.GetAfterArmorDamage`'s full body, not assumed

This is this feature's Input-mandated independent verification (spec.md FR-015) of whether the direct-hit code path
— the one Sharp/Blunt weapon and melee damage actually takes, distinct from the ambient/fire-tick path feature 011
investigated (R9/R10 there) — has the same, a different, or no gap.

**Decision**: Confirmed the **same** gap exists. `IOnIncomingDamageTattooEffect.TryAbsorbIncomingDamage` must apply
the identical CE-gated direct `dinfo.Amount` reduction pattern feature 011 established (R10 there), not rely on the
`IProvidesTattooStatOffset` contribution alone, when Combat Extended is loaded.

**How Sharp/Blunt damage is classified as non-ambient**: decompiling `CombatExtended.DamageDefExtensionCE` shows
`isAmbientDamage` is a `DefModExtension` flag (`bool`, default `false`) that CE's own `DamageDef` XML patches set
`true` specifically for `Flame`/`Burn` (feature 011's R9/R10 subject) and similar environmental/ambient categories.
Sharp/Blunt weapon and melee `DamageDef`s (`Bullet`, `Stab`, `Cut`, `Blunt`, `Crush`, etc.) carry no such patch, so
`DamageInfo.IsAmbientDamage()` is `false` for them and `ArmorUtilityCE.GetAfterArmorDamage` takes its main body
(the non-ambient/direct-hit path) rather than calling `GetAmbientPostArmorDamage` — confirming this is genuinely a
different code path from the one feature 011 investigated, not the same one under a different name.

**Root cause, found by decompiling `ArmorUtilityCE.GetAfterArmorDamage`'s direct-hit body in full** (the branch
reached after the shield/apparel checks, once damage isn't fully stopped by worn apparel):

```csharp
bool flag3 = deflectDamageInfo.Def.armorCategory.armorRatingStat == StatDefOf.ArmorRating_Sharp;
StatDef val4 = (flag3 ? CE_StatDefOf.BodyPartSharpArmor : CE_StatDefOf.BodyPartBluntArmor);
float statValue = StatExtension.GetStatValue(pawn, val4, true, -1);
for (int num3 = list.Count - 1; num3 >= 0; num3--)
{
    BodyPartRecord val5 = list[num3];
    bool flag4 = val5.IsInGroup(CE_BodyPartGroupDefOf.CoveredByNaturalArmor);
    float armorAmount = (flag4 ? pawn.PartialStat(deflectDamageInfo.Def.armorCategory.armorRatingStat, val5) : 0f);
    if (!TryPenetrateArmor(deflectDamageInfo.Def, armorAmount, ref penAmount, ref dmgAmount, null, statValue))
    { /* stopped here */ }
}
```

The pawn's own `armorCategory.armorRatingStat` value — `ArmorRating_Sharp` or `ArmorRating_Blunt`, exactly where
`IProvidesTattooStatOffset`/`GetStatOffset`'s contribution lands via `StatPart_TattooEffectOffset` — is **only ever
read (`pawn.PartialStat(...)`) when the hit body part's `flag4` (`IsInGroup(CE_BodyPartGroupDefOf.
CoveredByNaturalArmor)`) is true**; otherwise `armorAmount` is hardcoded `0f` for the pawn-level contribution.
`CoveredByNaturalArmor` is the same body-part-group classification feature 011's R10 already established is never
satisfied by ordinary human anatomy (thick hides/plating, not skin). Earlier in the same method, only *worn
apparel's own* stat value is read for each apparel item covering the hit part (`val2.PartialStat(...)` in the
per-apparel loop) — a different `Thing`'s stat query a pawn-level Hediff contribution can never reach, exactly
mirroring R10's apparel-vs-pawn distinction. This is unconditional, not probabilistic or CE-version-dependent — the
same conclusion shape as R10: no XML value on this tattoo's stat offset, however large, would ever produce a
measurable difference for a normal human's direct-hit Sharp/Blunt damage under Combat Extended.

**Why this does NOT contradict vanilla RimWorld's own behavior**: vanilla's `Verse.ArmorUtility.
GetPostArmorDamage` (decompiled during feature 011's own R3, confirmed still applicable here since it isn't
Heat-specific — it reads `damageDef.armorCategory.armorRatingStat` generically for any category) reads
`pawn.GetStatValue(armorRatingStat)` directly and unconditionally, no body-part-group gate. The stat-offset
mechanism (`IProvidesTattooStatOffset` against `ArmorRating_Sharp`/`ArmorRating_Blunt`) remains fully correct and
sufficient without CE loaded, exactly as it was for Ember Ward's `ArmorRating_Heat`.

**Fix applied (identical pattern to feature 011 R10, no interface change needed)**: `HediffComp_IronskinGlyphEffect.
TryAbsorbIncomingDamage(ref DamageInfo dinfo)` directly reduces `dinfo.Amount` (via `dinfo.SetAmount(...)`) by the
tier-appropriate armor-rating fraction whenever `CombatExtendedInterop.IsLoaded`, after the tier-progression
registration and before the Tier 2 full-negate roll — the exact same ordering and mechanism
`HediffComp_EmberWardEffect` already uses. Because `IOnIncomingDamageTattooEffect.TryAbsorbIncomingDamage` already
takes `dinfo` by `ref` (feature 011's own R10 change, already in place, no consumer other than Ember Ward existed
yet), **no interface or patch change is required** — Ironskin Glyph is simply the second implementer applying the
same established pattern, exactly matching FR-013's "if a genuine gap is found... any new reuse point added MUST be
similarly generic" language by not needing a new reuse point at all, since the existing one already generalizes to
this case. Vanilla RimWorld is unaffected (the `CombatExtendedInterop.IsLoaded` guard means this code path never
executes there), continuing to rely solely on the already-correct stat-offset mechanism. The `GetStatOffset`
contribution itself is left in place unconditionally (correct and load-bearing for vanilla; visible on the Stats
tab under CE too, same transparency rationale as R10).

**Entry-gate caveat (same shape as feature 011 R9, noted for `quickstart.md`, not a code concern)**: `
ArmorUtilityCE.GetAfterArmorDamage`'s very first check still short-circuits to `return originalDinfo` (skipping
all armor processing entirely, both ambient and direct-hit) when `dinfo.Def.armorCategory == null` or (no CE
projectile, no active CE melee verb, no `Weapon`, AND no `Instigator`) — identical condition to R9. Real Sharp/Blunt
weapon and melee damage always carries a `Weapon` and/or `Instigator` (a gun, a knife, a fist, an explosive's
source), so this bypass does not affect genuine gameplay damage — only RimWorld Dev Mode's instigator-less "Apply
damage" debug tool, exactly the same testing-methodology caveat feature 011's `quickstart.md` already documents for
its own scenarios.

**Alternatives considered**: Reclassifying human `BodyPartDef`s into `CE_BodyPartGroupDefOf.CoveredByNaturalArmor`
— rejected for the same reason feature 011 rejected it: it would change CE's own armor resolution for every other
damage category and every other mod/tattoo's interaction with human body parts, far outside this tattoo's scope.
Applying the direct reduction unconditionally instead of gated on `CombatExtendedInterop.IsLoaded` — rejected;
vanilla's stat-offset path is already correct, and applying both simultaneously under vanilla would double-count
the reduction, the same reasoning feature 011 R10 already established.

## R4 — Progression counter semantics: "hits absorbed" = a qualifying physical-category instance that reached this
comp with nonzero incoming amount, mirroring feature 011's R6

**Decision**: `HediffComp_IronskinGlyphEffect.TryAbsorbIncomingDamage` increments its `TattooTierProgress` counter
once per call where `dinfo.Def.armorCategory?.armorRatingStat` is `ArmorRating_Sharp` or `ArmorRating_Blunt` (R2)
**and** `dinfo.Amount > 0f` (FR-010), regardless of whether the Tier 2 full-negate roll (if attempted) then
succeeds — the counter increment happens before that roll, satisfying the spec's edge case ("a Tier 2 full-negate
roll succeeds ... the progression counter still increments") the same way feature 011's R6 already does for Ember
Ward.

**Rationale**: PRD §5.4/§6's "hits absorbed while equipped" doesn't distinguish partial vs. full absorption, and
this codebase's precedent (features 002/010/011) already favors the simplest self-consistent reading — "was this
pawn wearing Ironskin Glyph when a qualifying physical instance reached it" — over an unmeasurable finer
distinction. Identical reasoning to feature 011's R6, just against Sharp/Blunt instead of Flame/Burn.

**Alternatives considered**: None beyond what feature 011's R6 already rejected — same reasoning applies unchanged.

## R5 — A single armor-rating bonus value applies identically to both `ArmorRating_Sharp` and `ArmorRating_Blunt`,
not two independently-tunable knobs

**Decision**: Each tier has one armor-rating bonus field (`armorRatingBonusTier1`/`Tier2`), contributed identically
to both `StatDefOf.ArmorRating_Sharp` and `StatDefOf.ArmorRating_Blunt` via `GetStatOffset`, and used identically
for the CE-gated direct reduction (R3) regardless of which of the two categories the incoming hit belongs to.

**Rationale**: spec.md's own Assumptions section reads Ironskin Glyph's "armor rating" (PRD §6, undifferentiated) as
"a generalist toughness effect... since Ironskin Glyph's theme... and its PRD description don't scope it to one
physical damage type over the other." A single shared value is the simplest design consistent with that reading —
two independently-tunable knobs would imply a sharp/blunt distinction the spec explicitly says the PRD doesn't
draw, and would double the placeholder-tuning surface (Constitution Principle III) for no expressed design intent.

**Alternatives considered**: Separate `armorRatingSharpTier1/2` and `armorRatingBluntTier1/2` fields — rejected per
above; nothing in the spec or PRD calls for differentiated tuning between the two categories, and the "generalist
toughness" framing in spec.md's Assumptions section explicitly favors one shared value.

## R6 — No automated test harness; manual in-game verification only

**Decision**: No automated test suite, unchanged from every prior feature (001–011) — RimWorld's `Verse`/`RimWorld`
types aren't practically runnable outside the live game process. Verified manually per Constitution Principle II,
via `quickstart.md`.

**Rationale**: Consistent with this project's established testing approach; no aspect of Ironskin Glyph's design
introduces a testability need the project doesn't already have an answer for.

## R7 — Placeholder values stay small and fast-to-test, not "plausible-looking"

**Decision**: Ironskin Glyph's Tier 1/Tier 2 armor-rating bonus, the Tier 2 full-negate chance, and the Tier 1→2
progression threshold are deliberately small, fast-to-verify placeholder numbers (Constitution Principle III; PRD
§9), matching feature 006's research.md R8 precedent (reused by every tattoo since, including feature 011's own
R8) — e.g. a low tier-2 threshold so a tester doesn't need dozens of repeated hits to observe the tier-up in a
single manual test pass.

**Rationale**: Every prior tattoo already follows this precedent; no reason to deviate here.
