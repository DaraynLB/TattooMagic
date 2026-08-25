# Phase 0 Research: Berserker's Mark Tattoo Effect

## R1: Reusing the triggered-tattoo gizmo/cooldown/tier shape wholesale, gated stat offsets instead of a new mechanism

**Decision**: Berserker's Mark's own `HediffComp_BerserkersMarkEffect` implements `IProvidesTattooGizmo` (the
gizmo/cooldown/tier shape, now a five-times-proven pattern: Guardian's Call, Stormlash, Bloodrune, Wraithstep,
and this one) *and* `IProvidesTattooStatOffset` (reused unmodified from features 002-004). All three of this
tattoo's effects — melee damage boost, pain-threshold increase, defense reduction — are implemented as `GetStatOffset`
branches, each gated on `tierState.tier` and `Find.TickManager.TicksGame < boostEndTick`, exactly mirroring how
`HediffComp_GuardiansCallEffect.GetStatOffset` gates its Tier 2 armor buff on tier + `tauntEndTick`.

**Rationale**: Every one of this tattoo's PRD-described effects — "melee damage," "pain threshold," "defense" —
turns out to map onto an ordinary per-pawn `StatDef` (R2/R3 below), not a value RimWorld computes some other way
(unlike Bloodrune's "current pain," which has no `StatDef` and needed a `Hediff.PainOffset` override instead,
per feature 007 research.md R3). Once that's established, there is no reason to invent anything new: the
gizmo/cooldown/tier shape and the stat-offset contract are both already generic, already-proven infrastructure,
and Guardian's Call's Tier 2 armor buff is a near-exact precedent for "a stat offset that only applies while an
activation's window is still open." Reusing both wholesale is the fastest path to a working feature and the
strongest possible confirmation that these reuse points generalize to a *combination* consumer (gizmo trigger
gating three simultaneous stat effects), not just a single-stat one.

**Alternatives considered**: A dedicated buff/debuff `HediffDef` granted and removed on activation (mirroring how
some RimWorld mods implement timed combat buffs). Rejected: this project's own established pattern (Guardian's
Call's Tier 2 armor buff, Stormlash's Tier 2 speed buff) already handles "a stat offset active only during a
timed window" via a gate inside the wearer's own permanent comp, with no second hediff, no grant/removal timing
to get wrong, and no extra Scribe-tracked object. Introducing a second hediff here would be inventing a new shape
for a problem the codebase has already solved twice.

## R2: Melee damage via `StatDefOf.MeleeDamageFactor` — no CE branching needed

**Decision**: The melee-damage boost offsets `StatDefOf.MeleeDamageFactor` (confirmed present on RimWorld
1.6's `StatDefOf` via reflection against the installed game assembly), registered as a new entry in
`TattooEffectStatPartInstaller` (nothing currently targets it). No Combat Extended-specific branching applies.

**Rationale**: `MeleeDamageFactor` is a real, per-pawn vanilla `StatDef` that multiplies outgoing melee damage —
exactly the PRD §6 concept ("melee damage boost"). Checked directly against Combat Extended's own
`Defs/Stats/Stats_Pawns_Combat.xml` (the installed CE mod, same one used for Wraithstep's/Stormlash's live CE
testing): CE defines its own `MeleeArmorPenetration`, `MeleeCritChance`, and `MeleeParryChance`, but no
replacement or remap of a per-pawn outgoing-melee-damage multiplier — CE's melee rebalance operates through those
different stats (penetration, crit, parry), not by replacing `MeleeDamageFactor`. Since `StatPart_
TattooEffectOffset`'s summation is generic per-`StatDef` (feature 002 R3), registering `MeleeDamageFactor` once
in the shared installer makes it work correctly whether or not CE is loaded, with no `CombatExtendedInterop`
lookup required for this stat — matching the "branch only where CE genuinely replaces a stat this feature
touches" convention Bloodrune's R4 established.

**Alternatives considered**: A raw damage-dealt multiplier applied via a Harmony patch on the melee-damage
pipeline (verb execution or `DamageWorker`). Rejected: `MeleeDamageFactor` is precisely the vanilla extension
point built for "a per-pawn multiplier on melee damage dealt" — patching the damage pipeline directly would
duplicate what the stat system already does correctly, and would need its own CE compatibility investigation
that a `StatDef`-based approach avoids entirely by construction.

## R3: Pain threshold via `StatDefOf.PainShockThreshold`, defense via the same `ArmorRating_Sharp/Blunt/Heat` triplet Guardian's Call already registered — both CE-agnostic

**Decision**: The pain-threshold increase offsets `StatDefOf.PainShockThreshold` (registered as a second new
entry in `TattooEffectStatPartInstaller`). The defense reduction offsets the same
`StatDefOf.ArmorRating_Sharp`/`ArmorRating_Blunt`/`ArmorRating_Heat` triplet Guardian's Call's Tier 2 armor buff
already targets and feature 004 already registered — as a *negative* value instead of Guardian's Call's positive
one, stacking additively via the same generic summation (FR-009). Neither stat needs CE-specific branching.

**Rationale**: `PainShockThreshold` is a real vanilla `StatDef` (confirmed via the same reflection check as R2)
governing the pain level at which a pawn goes down — a direct, off-the-shelf match for "raise the pawn's pain
threshold," and unlike Bloodrune's *current pain* (which bypasses the `StatDef` system entirely), a threshold is
exactly the kind of static-per-query value the stat system is for. For defense: feature 004's own research.md R6
already established, and this feature re-confirms against CE's `Stats_Pawns_Combat.xml`, that Combat Extended
defines no replacement armor-rating input stat of its own (its `AverageSharpArmor` is a derived readout, not an
input CE remaps armor calculations onto) — so reusing Guardian's Call's exact StatDef triplet is both correct and
exactly what FR-009 requires: "the same StatDef(s) any other tattoo's own defense-related bonus would use," so a
well-armored pawn's Ironskin Glyph bonus (once that tattoo ships) and Berserker's Mark's penalty net out through
the same shared summation with neither tattoo needing to know about the other.

**Alternatives considered**:
- `StatDefOf.IncomingDamageFactor` for "defense" instead of armor rating. Rejected: it exists on vanilla's
  `StatDefOf` but has no established precedent in this codebase, and armor rating is already the concept
  Guardian's Call's own Tier 2 "shield wall" flavor-text uses for the *inverse* of this exact trade — reusing the
  same StatDef triplet is what makes FR-009's stacking guarantee (this feature's penalty nets against a future
  Ironskin Glyph's bonus) automatic rather than something a balance pass has to reconcile across two different
  stats later.
- A `Hediff`-level override for pain threshold, mirroring Bloodrune's `PainOffset` pattern. Rejected: that
  pattern exists specifically because *current pain* has no backing `StatDef` — `PainShockThreshold` is a
  `StatDef`, so the existing generic `IProvidesTattooStatOffset` route applies directly with no need for a
  tattoo-specific override class.
- Proactively checking `IGrantsStatOffsetImmunity` implementers for interference with a negative armor offset
  (Stormlash's Tier 2 grants this for `MoveSpeed`, feature 005). Confirmed not applicable: Stormlash's
  `BlocksNegativeOffsets` is hard-scoped to `stat == StatDefOf.MoveSpeed` only (`Source/Hediffs/
  HediffComp_StormlashEffect.cs`), so it has no effect on `ArmorRating_Sharp/Blunt/Heat`, `MeleeDamageFactor`, or
  `PainShockThreshold` — Berserker's Mark's own negative armor contribution is never blocked by another tattoo's
  immunity grant.

## R4: No new `Hediff` subclass, no new interface, no new Harmony patch

**Decision**: `TattooMagic_Hediff_BerserkersMark`'s `hediffClass` stays `HediffWithComps` (unlike Bloodrune,
which needed `Hediff_BloodruneEffect` for its `PainOffset` override). This feature adds exactly one new C# type
(`HediffComp_BerserkersMarkEffect`, plus its paired `HediffCompProperties_BerserkersMarkEffect`) and two
`Register(...)` lines in the existing `TattooEffectStatPartInstaller` — nothing else.

**Rationale**: R1-R3 collectively establish that every effect this tattoo needs is a gated `StatDef` offset, and
the existing `IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset` contract already handles "many hediff
comps contribute to the same stat, summed" generically — there is no remaining piece of behavior that needs a
`Hediff` subclass, a new interface, or a new Harmony patch to express. This is a stronger reuse outcome than any
prior triggered tattoo achieved (Guardian's Call added the targeting-interception patches, Stormlash added
`IGrantsStatOffsetImmunity`, Bloodrune added the `PainOffset` override, Wraithstep added the two-phase
cell-targeting shape) — fitting, since Berserker's Mark is the fifth and last triggered tattoo the PRD calls for,
landing squarely inside infrastructure the previous four already built out.

**Alternatives considered**: None seriously — once R1-R3 are settled, there is no remaining requirement left
unaddressed by existing, already-generic mechanisms.
