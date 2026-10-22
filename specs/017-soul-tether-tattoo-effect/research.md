# Phase 0 Research: Soul Tether Tattoo Effect

No `NEEDS CLARIFICATION` markers exist in spec.md. Every finding below is grounded in this repo's actual code
(file:line citations) and, where this mod has no existing precedent, in the actual installed game assembly
(`RimWorldWin64_Data/Managed/Assembly-CSharp.dll`, decompiled via `ilspycmd` — not the `Krafs.Rimworld.Ref` stub,
which is reference-only and cannot be reflected over) rather than assumed from the proposal's narrative or general
modding recollection. Two genuinely new dispatch points are required (R3, R4) — this is Soul Tether's equivalent of
Bastion Ward's own R2 finding that an existing pattern doesn't fit and a small, additive extension is needed instead
of reinvention.

## R1: The gizmo — reuse the two-phase targeting contract, but as this mod's first *pawn/corpse*-targeted ability

**Decision**: Soul Tether's `GetGizmo()` returns a plain `Command_Action` (`IProvidesTattooGizmo`, unmodified) whose
`action` opens `Find.Targeter.BeginTargeting` with:

```csharp
new TargetingParameters
{
    canTargetLocations = false,
    canTargetPawns = true,
    canTargetCorpses = true,
    canTargetSelf = false,
    neverTargetHostileFaction = true,
}
```

All real activation (pull, tend-buff grant, cooldown, tier-progression, mastery credit) happens only in a confirmed-
target callback, never on the gizmo click — identical shape to `specs/008-wraithstep-tattoo-effect/contracts/
cell-targeted-ability-contract.md` §1, and Soul Tether implements `IHandlesOwnMasteryProgress` for the same reason
Wraithstep and Bastion Ward do.

**Rationale**: `RimWorld.TargetingParameters` (confirmed via decompile — it lives in namespace `RimWorld`, not
`Verse`, despite every existing tattoo's own comment referring to "`Verse.TargetingParameters`"; grepping the actual
assembly's types was necessary to catch this) exposes `canTargetPawns`, `canTargetCorpses`, and
`neverTargetHostileFaction` as first-class fields — no existing tattoo in this codebase sets any of the three
(Bastion Ward/Wraithstep both use `canTargetLocations = true, canTargetPawns = false`, the mod's only precedent to
date), so this is new *configuration* of an already-proven API, not new plumbing. `neverTargetHostileFaction = true`
narrows the built-in candidate pool to non-hostile pawns before the custom `validator` ever runs, matching FR-005.

**Alternatives considered**: RimWorld's native `Command_Target` gizmo class — rejected for the same reason
`cell-targeted-ability-contract.md` already rejected it for Bastion Ward/Wraithstep (not this mod's idiom;
`IProvidesTattooGizmo`'s dispatch assumes a `Command_Action`-shaped return).

## R2: Friendly-corpse validation — `GenHostility.HostileTo` silently mis-handles a bare `Corpse`; must check `InnerPawn` explicitly

**Decision**: The targeting `validator` (and its `GetRejectReason` twin, mirroring Wraithstep/Bastion Ward's own
reject-reason pattern) MUST resolve "is this a friendly target" as:

```csharp
Pawn effectivePawn = target.Thing as Pawn ?? (target.Thing as Corpse)?.InnerPawn;
bool friendly = effectivePawn != null && !GenHostility.HostileTo(effectivePawn, Pawn);
```

— never `GenHostility.HostileTo(target.Thing, Pawn)` called directly on a `Corpse` instance.

**Correction (2026-10-21, issue #5):** `!HostileTo` alone is "not hostile", not "friendly". The same
`a.Faction == null || b.Faction == null → false` fallback described below also lets **factionless** pawns through, and
`HostileTo` is false for **neutral** factions too. Live testing pulled a factionless Dev-spawned drifter's corpse. The
spec's Assumptions define friendly as **colony/ally**, and the customer confirmed that rule. The check is now:

```csharp
bool friendly = effectivePawn != null
    && IsColonyOrAlly(effectivePawn, Pawn)            // same faction, or a faction whose relation with it is Ally
    && !GenHostility.HostileTo(effectivePawn, Pawn);  // still rejects e.g. a berserk colonist
```

The `InnerPawn` routing below is unchanged and still required.

**Rationale**: Decompiling `Verse.Thing.Faction` and `Pawn.MakeCorpse` (`Assembly-CSharp.dll`) confirms a `Corpse`'s
own base `Thing.Faction` field is **never set** at corpse creation — `MakeCorpse` only assigns `corpse.InnerPawn =
this`, with no `corpse.SetFaction(...)`/`corpse.Faction = ...` call anywhere in the method. Decompiling
`RimWorld.GenHostility.HostileTo(Thing a, Thing b)` shows its final fallback is `if (a.Faction == null || b.Faction
== null) return false;` — meaning `GenHostility.HostileTo(anyCorpse, casterPawn)` **always evaluates to `false`**
(never hostile), regardless of which faction the corpse's occupant actually belonged to. Calling this API directly
on a `Corpse`, as a first instinct might, would silently let the player target a **hostile faction's own corpse**
(FR-005 would be violated) despite compiling and appearing correct in casual testing with only friendly corpses on
hand. `Corpse.InnerPawn` remains the original `Pawn` object with its original `Faction` field intact regardless of
death (confirmed: `HostileTo`'s body contains no `pawn.Dead` branch that would change this), so routing through
`InnerPawn` is both correct and the only correct option.

**Alternatives considered**: Trusting `canTargetCorpses`/`neverTargetHostileFaction` alone to filter hostile
corpses — rejected once R2's finding was confirmed; those built-in `TargetingParameters` filters cannot distinguish
by faction for a `Corpse` any more than a hand-written check could, since they almost certainly consult the same
`Thing.Faction` field internally. The custom `validator` closure is required regardless, so it MAY as well perform
the correct `InnerPawn`-aware check itself rather than trusting the built-in flags to have already done it.

## R3: Relocating the target — the vanilla `Thing.Position` setter already handles a `Corpse` correctly; `Notify_Teleported()` is Pawn-only

**Decision**: A single reusable relocation step, callable for either a live `Pawn` or a `Corpse` target:

```csharp
target.Position = destinationCell;
if (target is Pawn livingPawn)
    livingPawn.Notify_Teleported();
```

**Rationale**: Decompiling `Verse.Thing.Position`'s setter shows it already performs full spawned-thing bookkeeping
generically for *any* `Thing` — deregisters from `Map.thingGrid`/`Map.coverGrid`/region listers at the old cell,
sets the backing field, then re-registers at the new cell and calls `Map.gasGrid.Notify_ThingSpawned` — with no
Pawn-specific branch. `HediffComp_WraithstepEffect.cs:235` (`pawn.Position = target.Cell; pawn.Notify_Teleported();`)
is this mod's own proven precedent for the living-pawn case; `Notify_Teleported()` itself is declared only on `Pawn`
(pathing/job/drawer refresh a corpse has no equivalent need for), so it is simply skipped for a `Corpse` target
rather than requiring a parallel "notify corpse teleported" step that doesn't exist in the vanilla API.

**Alternatives considered**: Despawn-then-`GenSpawn.Spawn` at the new cell (the usual pattern for moving a `Thing`
across longer distances or between maps) — rejected as unnecessary ceremony for a same-map, always-walkable-cell
relocation once R3's decompile confirmed the plain `Position` setter is already fully correct and is exactly what
Wraithstep already does safely for a spawned `Pawn`.

## R4: Tend quality — a Harmony postfix on `TendUtility.CalculateBaseTendQuality(Pawn, Pawn, ThingDef)`, the one tend-quality entry point that already takes the patient as a parameter

**Decision**: Add a new, narrowly-scoped Harmony postfix:

```csharp
[HarmonyPatch(typeof(TendUtility), nameof(TendUtility.CalculateBaseTendQuality),
    typeof(Pawn), typeof(Pawn), typeof(ThingDef))]
static void Postfix(Pawn patient, ref float __result)
{
    __result += SoulTetherTendBuffUtility.GetActiveQualityBonus(patient);
}
```

**Rationale**: Decompiling `RimWorld.TendUtility` confirms `DoTend(Pawn doctor, Pawn patient, Medicine medicine)` —
the single call site vanilla's own tend job (`Toils_Tend.FinalizeTend`) uses to actually apply a tend — computes
`float quality = CalculateBaseTendQuality(doctor, patient, medicine?.def);` and that exact three-argument overload
is the one exposed to a Harmony postfix (it forwards to a four-argument `(Pawn, Pawn, float, float)` overload
internally, but patching the outer one is sufficient and simpler). This is genuinely the *only* vanilla API in the
tend-quality path that receives the patient at all — vanilla's own tend-quality math (`doctor?.GetStatValue(StatDefOf
.MedicalTendQuality) * medicinePotency * ...`) never references the patient's own stats or hediffs, so no existing
per-pawn stat-offset mechanism (R5 below) could apply here even in principle; a direct postfix on the one
patient-aware method is the smallest correct fix, mirroring Bastion Ward's own R2 philosophy ("smallest possible
change that lets [something] benefit from someone else's tattoo effect... reuses rather than forks the existing
dispatch point" — except here there is no existing dispatch point to extend, so a new one-method postfix is added
instead, still scoped to a single vanilla method rather than a broad interception).

**Alternatives considered**: Registering a `StatPart` on `StatDefOf.MedicalTendQuality` or `StatDefOf
.MedicalTendQualityOffset` (this mod's existing `StatPart_TattooEffectOffset`/`TattooEffectStatPartInstaller`
mechanism, R6 of feature 002) — rejected for quality specifically: those stats are queried with `req.Thing ==
doctor`, and `CalculateBaseTendQuality` already exposes `patient` directly as a plain method parameter, so
patching the method itself is strictly simpler and doesn't require the doctor-side "look up my current patient"
indirection R5 needs for tend *speed*, where no such patient-aware overload exists.

## R5: Tend speed — no patient-aware vanilla method exists; a new `StatPart` on `MedicalTendSpeed` must look up the doctor's current patient

**Decision**: A new, dedicated `StatPart_SoulTetherTendSpeedBuff` (not the existing generic
`StatPart_TattooEffectOffset` — see Alternatives), registered by `TattooEffectStatPartInstaller` onto
`StatDefOf.MedicalTendSpeed`:

```csharp
public override void TransformValue(StatRequest req, ref float val)
{
    if (!(req.Thing is Pawn doctor))
        return;
    Job job = doctor.CurJob;
    if (job == null || job.def != JobDefOf.TendPatient)
        return;
    Pawn patient = job.GetTarget(TargetIndex.A).Pawn;
    if (patient == null)
        return;
    val += SoulTetherTendBuffUtility.GetActiveSpeedBonus(patient);
}
```

**Rationale**: Decompiling `RimWorld.JobDriver_TendPatient.MakeNewToils()` finds the *exact* line consuming this
stat: `int ticks = (int)(1f / pawn.GetStatValue(StatDefOf.MedicalTendSpeed) * 600f);` — a plain doctor-side stat
query with **no patient-aware overload anywhere** (unlike R4's tend-quality case), so there is no single method to
postfix the way R4 does. However, at the exact moment this line runs, `pawn` (the doctor) is already mid-execution
of its own `TendPatient` job with `Deliveree = job.targetA.Pawn` as the patient — i.e. `doctor.CurJob` reliably *is*
that same job at the moment `GetStatValue(MedicalTendSpeed)` resolves, since `MakeNewToils()` only ever runs as part
of driving an already-current job. A `StatPart` reading `((Pawn)req.Thing).CurJob` therefore recovers the patient
correctly and safely, using the same generic "sum contributions when this stat is queried" mechanism
`TattooEffectStatPartInstaller` already establishes for self-buffs (feature 002) — just reading a *different*
pawn's hediff state than the one whose stat is being computed, which the existing generic `StatPart_
TattooEffectOffset`/`IProvidesTattooStatOffset` pair cannot do (see Alternatives).

**Alternatives considered**:
- *Extending `IProvidesTattooStatOffset`/`StatPart_TattooEffectOffset` to also scan `req.Thing`'s current job's
  target*: rejected — that interface's whole contract (`GetStatOffset` on a comp attached to `req.Thing` itself) is
  "this pawn's own hediffs affect this pawn's own stat," used today exclusively for self-buffs (Frost Sigil's cold
  resistance, Ember Ward/Ironskin Glyph's armor, etc.). Bending it to also mean "or some *other* pawn related to
  `req.Thing` via its current job" would silently change what every existing implementer's contract means, for a
  need only this one new stat has. A separate, purpose-built `StatPart` avoids retrofitting a working shared
  interface.
- *A Harmony patch on `Pawn.GetStatValue`/`StatExtension.GetStatValue`*: rejected as unnecessarily broad — that
  extension method is the single call site for *every* stat on *every* `Thing` in the game; a `StatPart` registered
  only on `StatDefOf.MedicalTendSpeed` (the existing, narrower `TattooEffectStatPartInstaller` mechanism) achieves
  the identical outcome without adding a hot-path Harmony patch to a method called constantly for unrelated stats.
- *Decompiling further to find a narrower, tend-speed-specific method to postfix instead of a `StatPart`*: none
  exists — the duration math is inlined directly in `JobDriver_TendPatient.MakeNewToils()` (a compiler-generated
  iterator), which Harmony cannot cleanly target with a prefix/postfix at the granularity needed (patching "one
  local variable's value inside a generated `MoveNext`" requires a transpiler, a strictly higher-risk tool this
  codebase has never needed for any prior feature). The `StatPart` approach avoids touching the iterator at all.

## R6: The tend-buff hediff itself — reuse Frost Sigil's cross-pawn transient-hediff shape exactly

**Decision**: A new `TattooMagic_Hediff_SoulTetherTendBuff` (`HediffWithComps`, `isBad = false`), carrying vanilla's
own `HediffCompProperties_Disappears` (for auto-expiry, `showRemainingTime = true`) plus a new `HediffCompProperties
_SoulTetherTendBuff` / `HediffComp_SoulTetherTendBuff` exposing `qualityBonus`/`speedBonus` float fields set at grant
time via a `Configure(float quality, float speed)` call — read by `SoulTetherTendBuffUtility.GetActiveQualityBonus`/
`GetActiveSpeedBonus` (R4/R5) by scanning the patient's hediffs for this def and summing (or taking) its comp's
configured values. Granted via `HediffMaker.MakeHediff(...)` + `patient.health.AddHediff(...)` +
`TryGetComp<HediffComp_Disappears>()?.SetDuration(duration)`, on the living-pawn branch only (spec FR-011: never
granted to a pulled corpse).

**Rationale**: `Defs/HediffDefs/FrostSigilSlow.xml` + `HediffComp_FrostSigilEffect.GrantSlow` (`Source/Hediffs/
HediffComp_FrostSigilEffect.cs:106-119`) is this mod's exact, already-proven precedent for "grant a transient,
`TattooEffectValues`-configured hediff to a *different* pawn than the one wearing the triggering tattoo, with a
vanilla `HediffCompProperties_Disappears`-driven duration" — the only difference is Soul Tether's buff carries two
configured magnitudes (quality and speed) instead of Frost Sigil's one (move-speed offset), and is *helpful*
(`isBad = false`) rather than harmful. No new grant/expiry mechanism is needed; this is a direct application of an
already-working pattern.

**Alternatives considered**: Having `HediffComp_SoulTetherTendBuff` itself implement `IProvidesTattooStatOffset` for
`MedicalTendQuality`/`MedicalTendSpeed` directly (letting the *existing* `StatPart_TattooEffectOffset` pick it up
automatically) — rejected because that interface's contributions are summed for whichever pawn *has* the comp
(here, the patient), but the actual stat queries needing the bonus (R4's `CalculateBaseTendQuality`, R5's
`MedicalTendSpeed`) are evaluated for the *doctor*, not the patient; `StatPart_TattooEffectOffset`'s `GetOffset`
only ever inspects `req.Thing`'s own hediffs (`Source/Effects/StatPart_TattooEffectOffset.cs:30`), so it structurally
cannot answer "does the patient I'm currently tending have a buff" for a stat being queried on the doctor. The
dedicated `SoulTetherTendBuffUtility` lookup (called from R4's postfix and R5's new `StatPart`) is required instead.

## R7: `TattooEffectValues` / `TattooTierProgress` — confirmed current API, reused unmodified

**Decision**: `HediffComp_SoulTetherEffect` composes `public TattooTierProgress tierState = new
TattooTierProgress();`, identical to every prior triggered tattoo's single monotonic-counter shape (not Shedscale's
direct-interface deviation — Soul Tether has no racing-counters need). Every tunable is read via
`TattooEffectValues.Get(ScopeKey, "ValueKey", Props.xxxField)`.

**Rationale**: Unchanged since feature 002; `Source/Effects/TattooEffectValues.cs` and `Source/Effects/
TattooTierProgress.cs` are used identically by every triggered tattoo to date (FR-020/FR-021 require this reuse
explicitly).

## R8: The reusable pull-and-apply-effect component (FR-019) — a small internal method pair, not a new interface

**Decision**: Factor the target-agnostic mechanics into two small, private-to-`HediffComp_SoulTetherEffect` (or a
tiny static utility, `SoulTetherPullUtility`, if a second consumer materializes later — see Alternatives) steps that
take a `Func<Thing, bool>`-shaped filter and an `Action<Thing>`-shaped arrival effect as explicit parameters:

1. **Filter step** (used inside the targeting `validator`): given a candidate `Thing` (a `Pawn` or `Corpse`),
   apply range (R1), same-map, not-self, and the supplied friendly/hostile predicate (R2's `InnerPawn`-aware check
   for Soul Tether's own friendly-only filter).
2. **Pull-and-apply step** (used inside the confirmed-target callback): relocate the target next to the caster
   (R3), then invoke the supplied arrival-effect delegate on it (Soul Tether's own delegate grants the tend buff via
   R6, skipping corpses per FR-011).

**Rationale**: The proposal (and PRD §10) explicitly anticipates a future "inverse" tattoo pulling a *hostile* pawn
toward the caster and applying a *debuff* instead — differing from Soul Tether only in which predicate/delegate is
supplied, never in the relocation mechanic itself (R3) or the two-phase targeting shape (R1). Parameterizing by
filter and effect, rather than hardcoding "friendly" and "tend buff" inline, satisfies FR-019 while committing to
nothing more than what this feature itself already needs — no new public interface, no new registry, no XML-facing
concept. This mirrors this project's own repeated precedent (research.md R5 of feature 016, and features 012/013/015
per that doc) of declining to build a generic interface/registry for a single current consumer, while still keeping
the *mechanism itself* trivially reusable by a future second consumer that supplies its own filter/effect.

**Alternatives considered**: A new public interface (e.g. `IPullTargetFilter`/`IPullArrivalEffect`) or a shared
`GameComponent`-based registry — rejected as premature per the same "no premature abstraction" precedent already
established in this codebase (feature 016 research.md R5's identical reasoning for why Bastion Ward's rescue-eject
stayed a private method rather than a generalized knockback utility); nothing in the currently-approved batch has a
second confirmed consumer for this mechanism yet (Stillshroud is single-target-in-place with no pull at all;
Thunderclap Sigil is AoE-stun-in-place). If/when the "inverse" tattoo is actually specified, promoting this pair to
a small public utility class is a mechanical, low-risk refactor of already-isolated code, not a redesign.

## R9: Pull VFX/SFX (spec Clarifications, session 2026-09-19) — reuse vanilla's own Skip/Teleport psycast assets; defer relocation by a short window instead of an instant, effect-free teleport

**Decision**: `OnTargetConfirmed` no longer relocates the target immediately. It instead, at the moment of
confirmation: (1) draws a brief energy-tether line between caster and target via `FleckMaker.ConnectingLine(caster
.DrawPos, target.DrawPos, FleckDefOf.LineEMP, map, 0.5f)`; (2) plays a "vanish" flash at the target's current
position via `FleckMaker.Static(target.Position, map, FleckDefOf.PsycastSkipFlashEntry)` plus `SoundDefOf
.Psycast_Skip_Entry.PlayOneShot(...)`; and (3) stores the already-resolved destination cell, target reference, and
tier-resolved tend-buff magnitudes as pending state, with a resolve tick set to `now + pullEffectDurationTicks`
(new XML-tunable field, customer-approved default 20 ticks — a third of a second at normal speed, within the
requested 15-30 tick range). `CompPostTick` checks this pending state every tick and, once due, performs the actual
`Position =`/`Notify_Teleported()` relocation (research.md R3, unchanged), plays the "arrival" half of the effect
(`FleckDefOf.PsycastSkipInnerExit`/`PsycastSkipOuterRingExit` + `SoundDefOf.Psycast_Skip_Exit`) at the destination,
and grants the tend buff to a living target. Cooldown, tier-progression, and mastery credit all still fire at
confirmation time (unchanged), not at resolution — only the physical relocation and buff grant are delayed.

**Rationale**: The customer asked what a "hand reaching out and grabbing the pawn" would take, wanting it to feel
fast ("snatching someone from the jaws of defeat") rather than a slow float, while still reading as a deliberate
pull rather than an instant, unannounced blink. This mod has never used RimWorld's Mote/Fleck VFX system at all
(grepped `Source/` for `Mote|FleckMaker|EffecterDef` — zero hits before this feature), so building a custom
animated effect from scratch would mean inventing new art and a new rendering mechanism with no precedent to lean
on. Decompiling `RimWorld.CompAbilityEffect_Teleport` (the vanilla ability effect backing psycasts like Farskip/
Word of *, which teleport a chosen pawn) found vanilla already ships and uses exactly this shape: a vanish flash +
sound at the origin, an arrival flash + sound at the destination, with the actual `Position =`/`Notify_Teleported()`
call happening between them. Every asset referenced (`FleckDefOf.LineEMP`, `PsycastSkipFlashEntry`,
`PsycastSkipInnerExit`, `PsycastSkipOuterRingExit`, `SoundDefOf.Psycast_Skip_Entry`/`Psycast_Skip_Exit`) is a
field confirmed present on the actual installed game assembly's `FleckDefOf`/`SoundDefOf` and already shipped with
the base game — no new texture, sound file, or Def needs authoring. Reusing this proven, polished vanilla
combination is lower-risk and higher production value than a hand-built placeholder effect this session had no art
input to design well.

**Alternatives considered**:
- *A custom "hand" mote that visibly travels from caster to target and back*: rejected for this pass — no hand
  art exists, and designing a bespoke traveling-sprite animation (as opposed to reusing static flash/line assets)
  is meaningfully more implementation and tuning work for a placeholder that would likely be replaced once real art
  is commissioned anyway.
- *Animating the pawn's own rendered sprite sliding continuously from origin to destination* (the literal "float
  toward them" reading of the request): rejected as a much larger scope jump — RimWorld pawns have no skeletal/
  rigged animation, so this would require overriding where the pawn is *drawn* during the pull window via a
  Harmony patch into pawn-rendering internals, a first for this mod with no precedent to build on and real risk of
  fighting vanilla's own facing/posture/carry-rendering code. Deferred; the flash-and-snap approach was judged
  sufficient to deliver the requested "fast, deliberate yank" feel without that risk.
- *`FleckMaker.MakeConnectingLine`'s `Mote`-based cousin, `MoteMaker.MakeConnectingLine`* (decompiled alongside
  the Fleck version): equivalent capability via the older Mote system; `FleckMaker`'s version was chosen since
  vanilla's own `CompAbilityEffect_Teleport`/`CompTargetable` precedent uses Flecks for this exact "connecting
  line + flash" combination, not Motes, keeping this feature consistent with the vanilla precedent it's copying.
- *Resolving cooldown/tier-progression/mastery credit at the moment the pull resolves, instead of at confirmation*:
  rejected — every other triggered tattoo in this mod credits activation at the moment of a confirmed target/cell,
  not on some later resolution; changing that convention for Soul Tether alone (for a sub-second window) would be
  inconsistent for no real benefit, and would complicate the "was this activation real" bookkeeping for no reason.

## R10: Tinting the reused Skip/EMP assets — `FleckCreationData.instanceColor`, confirmed via `Verse.FleckStatic`

**Decision**: `HolyTetherColor` (a private `Color` constant, `new Color(1f, 0.92f, 0.65f)` — warm gold-white) is
applied to every Fleck R9 spawns, via two small helpers (`SpawnTintedTetherLine`, `SpawnTintedFleck`) that build a
`FleckCreationData` by hand and set its `instanceColor` field before calling `map.flecks.CreateFleck(data)`,
replacing the plain `FleckMaker.Static`/`ConnectingLine` convenience-wrapper calls R9 originally used.

**Rationale**: The customer asked for an angelic/holy feel (referencing World of Warcraft's "Life Grip" as a tonal
touchstone) rather than the reused assets' native electric-blue "psychic skip" look. Decompiling `Verse
.FleckCreationData` confirms it carries a `Color? instanceColor` field; decompiling `Verse.FleckStatic` (the class
backing every Fleck this feature uses) confirms its constructor reads `instanceColor = creationData.instanceColor ??
Color.white` and later assigns `color = instanceColor` at draw time — i.e. every one of these Flecks already
supports per-instance tinting, it simply defaults to an unmultiplied white (its native color) whenever
`instanceColor` is left unset, which is what R9's original `FleckMaker.Static`/`ConnectingLine` calls did. Neither
of those two convenience wrappers exposes a color parameter (confirmed by decompiling `RimWorld.FleckMaker`), so
tinting requires calling their own underlying building blocks (`FleckMaker.GetDataStatic`, plus the same
line-geometry math `ConnectingLine` performs internally) directly instead.

**Alternatives considered**:
- *A Harmony patch or Def-level color override on the vanilla `FleckDef`s themselves*: rejected — `instanceColor`
  is a per-spawn field on `FleckCreationData`, already exactly the mechanism vanilla itself uses for tinted,
  reused effects; patching the shared `FleckDef` would recolor every other use of these effects in the game
  (including vanilla's own Skip/Farskip psycasts), not just Soul Tether's.
- *A new, custom `FleckDef` with a pre-baked gold-white texture*: rejected as unnecessary — `instanceColor`
  achieves the same visual outcome without authoring new art or a new Def, and remains just as trivial to re-tune
  or swap later if the exact shade doesn't read well in-game.
- One open item, not yet resolved by decompiling further: the two "Skip" flashes use a bespoke
  `MotePsychicSkipFlash`/`MotePsychicSkipRing` shader (`Graphic_FleckPulse`) with baked noise/pulse parameters,
  unlike `LineEMP`'s plain `TransparentPostLight` shader — `instanceColor` is very likely still respected (both
  shader types read a common per-instance color input through the same `FleckStatic` draw path), but this hasn't
  been visually confirmed in a running game yet. Flagged for the same live-verification pass as R9 (quickstart.md
  Scenario 1) rather than assumed.
