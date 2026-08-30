# Phase 0 Research: Phoenix Tattoo Effect

All findings below are grounded against the real, locally installed RimWorld 1.6 game code
(`Assembly-CSharp.dll` under `RimWorldWin64_Data\Managed\`, decompiled read-only via `ilspycmd`, no game
code executed) and the real, locally installed `CombatExtended.dll` (Steam Workshop item `2890901044`) —
mirroring features 007/010/011/012/013's own decompile-to-verify approach, not assumed. This is the first
tattoo whose research required decompiling `RimWorld.ResurrectionUtility`, `RimWorld.CompRottable`,
`Verse.Corpse`, `Verse.GameComponent`/`Verse.Game`, `Verse.HealthUtility`, and `Verse.HediffComp_Disappears`
— none of the prior 13 features touched death, corpses, or game-wide (non-pawn) state at all.

## R1: How vanilla resurrection actually works, and what it does *not* gate on

**Decision**: Call `RimWorld.ResurrectionUtility.TryResurrect(Pawn pawn, ResurrectionParams parms = null)`
directly for the act of reviving — do not reimplement pawn restoration. But **every one of Phoenix's own
preconditions and mechanics (head/brain-intact gate, body-not-destroyed gate, decomposition-based chance,
tiered wait, daily retry) is entirely new, custom logic with no vanilla equivalent to delegate to.**

**Rationale**: Decompiling `RimWorld.ResurrectionUtility.TryResurrect` shows its *only* preconditions are
`!pawn.Dead` and `pawn.Discarded` (both error-and-abort cases, not gameplay gates). It unconditionally
proceeds even if `pawn.Corpse` is `null` (see R4). Decompiling the actual Resurrector Mech Serum pipeline
(`Data/Core/Defs/ThingDefs_Items/Items_Exotic.xml` → `MechSerumResurrector`'s `CompProperties_Targetable`
→ `JobDriver_Resurrect.Resurrect()` → `ResurrectionUtility.TryResurrect`) shows its *only* eligibility gate
is `CompTargetable.nonDessicatedCorpsesOnly`, i.e. `corpse.GetRotStage() != RotStage.Dessicated` — a rot-stage
check, not an anatomical one. **This corrects the design proposal's assumption** ("the Resurrector Mech
Serum uses the same basic [head/brain] gate") — no such gate exists anywhere in vanilla. Phoenix's FR-008
head/brain-intact and body-not-destroyed preconditions must be implemented from scratch:
`pawn.health.hediffSet.GetBrain() != null` (returns `null` once the brain body part is fully missing/destroyed)
and `pawn.Corpse != null && !pawn.Corpse.Destroyed`.

**Alternatives considered**: Gating on `CompTargetable.nonDessicatedCorpsesOnly` like the Serum — rejected;
that's a *rot-stage* gate (R2 already covers decomposition via the chance curve itself), not the anatomical
gate FR-008 actually asks for, and reusing `CompTargetable` at all would mean building an item-targeting UI
Phoenix doesn't need (revival is automatic, not player-invoked).

## R2: Corpse decomposition/rot state, and what "fully decayed" means

**Decision**: Poll `corpse.GetComp<CompRottable>().RotProgress` (ticks accumulated since death) and
`.Stage` each attempt for the decomposition-based chance roll (R varies smoothly with `RotProgress`, R9's
curve). Treat `Stage == RotStage.Dessicated` as "fully decayed" (FR-014's second permanent-loss condition)
— **not** `corpse.Destroyed`, since a dessicated corpse is a stable, still-existing, still-inspectable
`Thing` in vanilla, not an auto-destroyed one. "Kept frozen halts decomposition" (FR-013) is **already true
today via existing, unmodified vanilla logic** — no new code is needed to make freezing pause rot.

**Rationale**: Decompiling `RimWorld.CompRottable`: `Stage` is a computed property comparing `RotProgress`
against `PropsRot.TicksToRotStart`/`TicksToDessicated` (`Fresh` → `Rotting` → `Dessicated`). Auto-destroy on
rot only fires from the `Rotting` stage and only when `PropsRot.rotDestroys` is true — confirmed **not** set
on the corpse's own rot props (human corpses persist indefinitely once dessicated; only raw-food-type rot
props set `rotDestroys`). `CompRottable.TickInterval` computes `RotProgress += GenTemperature
.RotRateAtTemperature(parent.AmbientTemperature) * delta`, and `RotRateAtTemperature` already returns `0`
at/below freezing (confirmed via `CompInspectStringExtra`'s own `"(CurrentlyFrozen)"` branch, gated on the
same near-zero-rate check) — so a corpse sitting in a freezer already has `RotProgress` genuinely frozen by
vanilla's own temperature-rate curve, with zero extra code needed for FR-013's "steady odds if decomposition
is halted" behavior once Phoenix's own chance curve reads `RotProgress` fresh at each attempt.

**Alternatives considered**: Treating `corpse.Destroyed` as "fully decayed" — rejected; wrong signal (never
becomes true from rot alone) and would make FR-014's "or has fully decayed away" branch unreachable.

## R3: Driving the multi-day wait/retry timer — a `GameComponent` sweep, not `Corpse.TickRare`

**Decision**: `GameComponent_PhoenixRegistry.GameComponentTick()` (self-gated to run its actual sweep once
every ~2,000 ticks — roughly 20x/in-game-day, comfortably granular against multi-day timers) iterates its
own registered-wearer list (R11) and, for each pawn that is `Dead` with `HediffComp_PhoenixEffect
.reviveAttemptTick != 0`, checks whether `Find.TickManager.TicksGame >= reviveAttemptTick` and resolves an
attempt (R9) if so. **No Harmony patch on `Corpse.TickRare()` and no `Hediff.Notify_PawnDied()`/
`Notify_PawnCorpseDestroyed()` overrides are used** — a deliberate departure from the closest vanilla
precedent, `Verse.Hediff_DeathRefusal` (Anomaly's own "self-resurrect" hediff).

**Rationale**: `Hediff_DeathRefusal` is the one existing vanilla hediff shaped like Phoenix's core loop — it
survives on the pawn past death, counts down, and calls `ResurrectionUtility.TryResurrect` on completion.
But it only works because `Verse.Corpse.TickRare()` has a **hardcoded, `Hediff_DeathRefusal`-specific type
check** (`InnerPawn.health.hediffSet.GetFirstHediff<Hediff_DeathRefusal>()`) — there is no generic "keep
ticking a hediff on a dead pawn's corpse" mechanism to hook into for a *new* hediff type, and mirroring it
would require a new Harmony patch on `Corpse.TickRare()` regardless. Worse, that pattern **cannot support
Phoenix's cremation exception at all**: cremation deliberately destroys the `Corpse` `Thing`
(`ingredient.Destroy()`, R5) before the wait elapses, so a corpse-tick-driven timer would simply stop
ticking the moment the corpse is destroyed — exactly the case where FR-022 requires the guaranteed revival
to *still* fire once the tier's wait later elapses. A `GameComponent`'s own `GameComponentTick()` has no
such dependency: it runs every tick regardless of whether any particular `Thing` is spawned, so a pending
Phoenix revival keeps advancing correctly whether the corpse is on a map, in a caravan, buried (R6), or
already cremated to nothing. Since a faction-wide `GameComponent` registry (R11) is independently required
for the rarity cap, using its own tick loop to also drive revival resolution needs no second component and
naturally shares the one "list of current Phoenix wearers" the cap already has to maintain. This also
sidesteps a second real gotcha found while researching R1: `Pawn_HealthTracker.HealthTick()`/
`HealthTickInterval()` both early-return `if (Dead)`, so a normal `HediffComp.CompPostTick`/`CompTickInterval`
on Phoenix's own hediff **goes silent the instant its wearer dies** and could never see a multi-day wait
through on its own — the `GameComponent` sweep is what makes the wait/retry timer work at all, independent
of the cremation question. Detecting "wearer just died, schedule the first wait" and "corpse is now gone
without a cremation guarantee, cancel forever" are both handled the same way — as plain conditions checked
during the same periodic sweep — rather than as separate event hooks; at multi-day timer granularity, a
sweep noticing a death or a destroyed corpse a few thousand ticks late is imperceptible.

**Alternatives considered**:
- Mirroring `Hediff_DeathRefusal` exactly (`Corpse.TickRare()` patch + `Notify_PawnDied`/
  `Notify_PawnCorpseDestroyed` overrides on a custom `Hediff` subclass) — rejected: breaks under cremation
  (above), and needs a new Harmony patch this design avoids entirely.
- A dedicated `WorldComponent` instead of `GameComponent` — rejected (R11): the cap is scoped to "your whole
  faction" within one save/colony, which `GameComponent` already covers; `WorldComponent` implies
  cross-save-world state this mod has no use for.

## R4: The cremation guarantee, and a real gap in `ResurrectionUtility.TryResurrect`

**Decision**: `Patch_RecipeWorker_ConsumeIngredient_PhoenixCremation` (a Harmony **prefix** on
`Verse.RecipeWorker.ConsumeIngredient`, R5) detects a Phoenix-tattooed corpse being consumed by the
`CremateCorpse` bill specifically, and — **before** the base method's `ingredient.Destroy()` runs — sets
`cremationGuaranteed = true` on the wearer's `HediffComp_PhoenixEffect` *and* captures
`cremationFallbackPos = corpse.PositionHeld` / `cremationFallbackMap = corpse.MapHeld`. When the guaranteed
attempt later resolves (R3's sweep, potentially days afterward), after calling `ResurrectionUtility
.TryResurrect(pawn, ...)`, explicitly check `if (!pawn.Spawned && cremationFallbackMap != null)
GenSpawn.Spawn(pawn, cremationFallbackPos, cremationFallbackMap);` as a fallback.

**Rationale**: Decompiling `ResurrectionUtility.TryResurrect` in full (R1) shows the corpse-handling block is
guarded by `if (corpse != null)` — reading `Corpse corpse = pawn.Corpse;` at call time. If the corpse no
longer exists (already destroyed by cremation, potentially days earlier), this block is skipped entirely,
`flag` (whether to actually spawn the pawn) stays `false`, and the method's own later `if (flag && (parms ==
null || !parms.dontSpawn)) { GenSpawn.Spawn(pawn, loc, map); ... }` **never runs** — vanilla's own method
resurrects the pawn into an unspawned, placeless limbo state for exactly this case, a real, decompile-
confirmed gap, not a hypothetical. This only matters for the cremation path: the *normal* (non-cremated)
revival path always has a live `pawn.Corpse` at attempt time (by definition — if the corpse were gone,
R3/R6 would already have cancelled the pending revival as a permanent loss), so vanilla's own spawn logic
handles it with zero extra code. Only the cremation path needs the explicit fallback spawn, using a location
captured *before* the corpse was destroyed (nothing later can read `corpse.PositionHeld`/`MapHeld` off a
`Thing` that no longer exists).

**Alternatives considered**: Reconstructing a spawn location from the pawn's last known map some other way
(e.g. its faction's home map) at guaranteed-revival time — rejected as strictly worse than capturing the
real cremation location once, at the one moment it's still available.

## R5: Identifying "cremated via the crematorium bill specifically"

**Decision**: The building is `ElectricCrematorium` (`Data/Core/Defs/ThingDefs_Buildings/
Buildings_Production.xml` — vanilla has only this one crematorium, no separate non-electric variant); its
bill is `RecipeDef` `CremateCorpse` (`Data/Core/Defs/RecipeDefs/Recipes_Cremation.xml`), which declares no
`workerClass` override, so it runs through the base `Verse.RecipeWorker.ConsumeIngredient(Thing ingredient,
RecipeDef recipe, Map map)`. Patch that one method: `recipe.defName == "CremateCorpse" && ingredient is
Corpse corpse` is an unambiguous, single-choke-point detector, with both the exact `Corpse` instance and the
`RecipeDef` available as sibling parameters of the same call.

**Rationale**: `Verse.AI.Toils_Recipe.ConsumeIngredients` calls `recipe.Worker.ConsumeIngredient(ingredients
[i], recipe, map)` for each consumed ingredient — this is the one, well-defined place any bill's ingredient
consumption happens. Base `RecipeWorker.ConsumeIngredient` is just `ingredient.Destroy()` — a plain
`Thing.Destroy()`, structurally unrelated to `DamageInfo`/`DamageDefOf.Flame` application or `Corpse.Kill(...)`,
which is the path any fire/combat/wildfire destruction actually takes. **There is no code-path overlap
between deliberate-bill cremation and any other fire-based destruction** — patching this one method cannot
misfire on a burning building or a raid (FR-021's requirement is satisfied structurally, not by an extra
"was this actually the crematorium" heuristic).

**Bonus finding — FR-023 (gear lost) needs no new code.** Reusing the vanilla `CremateCorpse` bill as-is
already destroys any apparel left on the corpse along with it (the same reason players are expected to strip
a corpse before cremating it today) — this is inherent, existing behavior of the bill Phoenix's cremation
exception reuses, not something this feature has to implement.

## R6: Non-cremation destruction and burial as permanent loss

**Decision**: During the `GameComponent` sweep (R3), for any pawn with a pending (non-cremation-guaranteed)
revival, treat it as permanently lost — unregister (R11), clear pending state — the moment any of these is
true: `pawn.Corpse == null`, `pawn.Corpse.Destroyed`, `pawn.Corpse.GetRotStage() == RotStage.Dessicated`
(R2), or `pawn.Corpse.ParentHolder is Building_Grave` (buried).

**Rationale**: FR-014 requires outright destruction or full decay to permanently end revival; the spec's own
Assumptions section additionally states burial is treated the same way (explicitly flagged there as an
unconfirmed working assumption, carried forward unchanged here — not re-litigated by this plan). A buried
corpse is **not** a destroyed `Thing` in vanilla — `Building_Grave` holds it live in its own `ThingOwner`
(the same general "container holds a still-existing Thing" shape as any storage building), so the
destroyed/dessicated checks alone would miss it; the `ParentHolder is Building_Grave` check is the one that
actually captures "this body has been buried." A raid or map event that destroys a corpse mid-wait (Edge
Cases) is covered by the plain `Destroyed` check with no special-casing needed — destruction is destruction
regardless of cause, except cremation specifically (R5), which never reaches this check at all because
`cremationGuaranteed` is set before the corpse is destroyed and the sweep checks that flag first.

## R7: Faction-leave and capture hooks

**Decision**: One postfix on `Verse.Pawn.SetFaction(Faction newFaction, Pawn recruiter = null)` covers
recruit-away, defection, exile/banishment, and enslavement — whenever a Phoenix-tattooed pawn's faction
changes away from `Faction.OfPlayer`, strip the tattoo (`TattooHediffRemovalGuard.RemoveTattooHediff`) and
unregister it (R11); whenever it changes *to* `Faction.OfPlayer` and the pawn already carries the tattoo
(the "joins already wearing it" case, FR-005), register it (allowed even over cap). A **second**, separate
postfix on `RimWorld.Pawn_GuestTracker.SetGuestStatus` covers plain capture-as-prisoner, which — confirmed
by decompiling both methods — does **not** go through `SetFaction` at all.

**Rationale**: `PawnBanishUtility.Banish` (exile) and the recruit-success/enslave paths in
`Pawn_GuestTracker.SetGuestStatus` all call `pawn.SetFaction(...)` directly — one postfix on `SetFaction`
catches all of those, matching-on-transition (`__instance.Faction` before vs. the `newFaction` argument, or
comparing against `Faction.OfPlayer` before/after). But decompiling `Pawn_GuestTracker.SetGuestStatus`'s
`GuestStatus.Prisoner` case shows it only mutates `HostFaction`/guest bookkeeping — the captured pawn's own
`Faction` field is left untouched, so a raid capturing a Phoenix-tattooed colonist would be invisible to a
`SetFaction`-only patch. Both patches funnel into one small shared helper (e.g.
`PhoenixFactionLeaveUtility.StripIfNoLongerOurs(Pawn pawn)`) so the strip/unregister logic exists exactly
once.

## R8: Passive bleed cauterization

**Decision**: `HediffComp_PhoenixEffect.CompPostTick`, self-gated to check roughly once per second (~60
ticks, matching feature 013's own established interval-gating convention and vanilla's own bleed-drain
cadence, R8 below) while the wearer is alive: scan `pawn.health.hediffSet.hediffs`, split into
`Hediff_MissingPart` bleeds (severed/missing limbs) and `Hediff_Injury` bleeds with `BleedRate` at or above a
tunable trivial-exclusion floor. A qualifying `Hediff_MissingPart` bleed always outranks every `Hediff_Injury`
bleed (FR-027) regardless of relative severity; otherwise the highest-`BleedRate` candidate wins. On a
successful proc, call `.Tended(1f, 1f)` on every qualifying bleed sharing the target's exact `BodyPartRecord`
(FR-028), then apply a burn (R9) to that part.

**Rationale**: Bleed-to-blood-loss drain itself runs via `Verse.HediffGiver_Bleeding.OnIntervalPassed`, gated
at `pawn.IsHashIntervalTick(60, delta)` inside `Pawn_HealthTracker.HealthTickInterval` — confirming ~1
real-second is the natural granularity to check at, not sooner. `Hediff.Bleeding` is a computed property
(`BleedRate > 1E-05f`), not a settable flag — both `Hediff_Injury.BleedRate` and `Hediff_MissingPart
.BleedRate` are independently overridden with the same general shape (severity/part-health × body-part
bleed-rate × race bleed-rate factor), giving a clean, consistent number to compare across both types for
"most severe." **Stopping a bleed**: `Verse.Gene_Clotting` (Biotech) is a direct, already-shipped vanilla
precedent for exactly this action — its own `TickInterval` calls `hediff.Tended(TendingQualityRange
.RandomInRange, TendingQualityRange.TrueMax, 1)` on every bleeding hediff it finds, once per interval; both
`Hediff_Injury.BleedRate` and `Hediff_MissingPart.BleedRate` explicitly return `0f` once `IsTended()` is
true, confirming `Tended(...)` — not a `Severity` reduction — is the correct call to actually stop a wound's
bleeding without changing its severity/pain. `Verse.HealthUtility.FindMostBleedingHediff` is vanilla's own
"find the worst bleed" scan, confirming a flat `BleedRate` comparison (no extra weighting) is the standard
approach; the severed-limb-always-wins rule (FR-027) is Phoenix-specific and implemented as an explicit
type-priority check ahead of that comparison, not part of any vanilla scan.

**Alternatives considered**: Reducing `Hediff_Injury.Severity` to "heal" the wound closed — rejected; that's
the mechanism `TattooHealingUtility.HealWorstInjury` (Vampiric Thorn) already uses for *healing*, a
conceptually different action from *stopping a bleed without healing it*, and doesn't address
`Hediff_MissingPart` at all (a missing limb's "severity" isn't a wound to heal).

## R9: Burn injury — reuse vanilla's own `Burn` `HediffDef`, made permanent immediately

**Decision**: No custom burn `HediffDef`. Apply burns via `HediffMaker.MakeHediff(BurnHediffDef, pawn,
targetPart)` → set `.Severity` → `pawn.health.AddHediff(hediff)` → immediately set `hediff.TryGetComp
<HediffComp_GetsPermanent>().IsPermanent = true`, using a tier-appropriate severity value for revival attempts
(severe at Tier 1, lighter at Tier 2, FR-015) and the flat Tier-1 severity value for cauterization burns
regardless of the wearer's own tier (FR-029's "unchanged from Tier 1" — reuses the same `burnSeverityTier1`
tunable Tier 1 revival burns use, not a third separate value). `HediffDefOf.Burn` doesn't exist (vanilla's own
curated `HediffDefOf` has no field for it); resolved once via `DefDatabase<HediffDef>.GetNamed("Burn")` inside a
`[StaticConstructorOnStartup]` static constructor instead.

**Rationale**: `Data/Core/Defs/HediffDefs/Hediffs_Local_Injuries.xml`'s `Burn` def (`ParentName="BurnBase"`,
`hediffClass` resolving to `Hediff_Injury : HediffWithComps`) already carries a `HediffCompProperties_
GetsPermanent` comp (`permanentLabel="burn scar"`) — the same comp vanilla itself uses to eventually turn a
burn into a permanent scar. Decompiling `Verse.DamageWorker_AddInjury.ApplyDamageToPart` (the method vanilla's
own combat/fire damage actually creates injuries through) confirms the authoritative injury-creation shape:
`HediffMaker.MakeHediff(hediffDef, pawn)` → set `.Part`/`.Severity` → `pawn.health.AddHediff(...)` — the same
general call shape this mod's own `TattooHealingUtility.cs` already uses in the healing direction
(`Hediff_Injury.Heal(...)`), confirming the injury-creation counterpart needs no `DamageInfo`/combat-pipeline
involvement at all, since Phoenix applies burns as a direct punishment effect, not as combat damage resolution.

**Correction, found via live in-game testing after this feature's first implementation pass**: the original
decision here assumed vanilla's own adjacent-injury-merge logic (`Hediff_Injury.TryMergeWith`) would handle
FR-015's "these burns MUST stack" requirement for free, by merging repeated *non-permanent* burns on the same
body part. That assumption turned out to be incomplete: decompiling `Pawn_HealthTracker.Notify_Resurrected`
(called from `ResurrectionUtility.TryResurrect`, R1) shows it unconditionally runs `hediffSet.hediffs.RemoveAll
((Hediff x) => x.def.everCurableByItem && x is Hediff_Injury && !x.IsPermanent());` on **every** resurrection —
i.e. any non-permanent `Burn` injury (the successful attempt's own freshly-applied one included, since it was
applied before this call) gets stripped the instant the pawn revives. This was directly observed in testing: a
successful revival left no visible burn at all. Making each burn permanent immediately (rather than relying on
vanilla's own gradual heal-into-scar path) fixes this — a permanent injury never matches `!x.IsPermanent()`, so
it survives every future resurrection too, not just the current one. The tradeoff: `TryMergeWith` explicitly
refuses to merge once either side `IsPermanent()`, so repeated burns no longer merge into one growing severity
value — each attempt now leaves its own separate, permanent scar on the target body part instead. This is
judged a **better**, not merely acceptable, match for FR-015's actual language ("visibly accumulates scarring")
and the design proposal's own "battle-worn look... built from attempts" framing than a single ever-larger
number would have been, so the design decision was kept rather than reverted once the underlying bug was
understood.

## R10: Paralytic Abasia — a new custom `HediffDef` with a programmatically-set duration

**Decision**: A new `HediffDef` (`TattooMagic_Hediff_ParalyticAbasia`), `hediffClass = HediffWithComps`,
`<stages>` applying severe `capMods` (near-zero `Moving`/`Manipulation`/`Consciousness`, "near-total
paralysis" per the spec), with vanilla's own `HediffCompProperties_Disappears`/`HediffComp_Disappears` for
its lifetime. Its escalating duration (FR-018: 2 days first occurrence, +1 day each subsequent) is set
**programmatically at apply time**, not via a fixed XML value: after `pawn.health.AddHediff(...)`, fetch the
new hediff's `HediffComp_Disappears` and set both `ticksToDisappear` and `disappearsAfterTicks` to that
occurrence's duration in ticks.

**Rationale**: Decompiling `Verse.HediffComp_Disappears` confirms both `ticksToDisappear` (current countdown)
and `disappearsAfterTicks` (used only by `Progress`/the remaining-time label, for display) are plain public
fields, not read-only — directly settable from arbitrary calling code, exactly like this feature needs for a
duration that varies per application. This is a standard, well-established RimWorld modding technique (many
mods set this field programmatically for variable-duration effects); no fragility beyond an ordinary public
field write. Paralytic Abasia is confirmed (proposal + spec Assumptions) to be a wholly new hediff, not a
reuse of any vanilla/DLC debuff — there is nothing to compose against besides `HediffComp_Disappears` itself.

## R11: The faction-wide Phoenix Registry — a new `GameComponent`, self-registering

**Decision**: `GameComponent_PhoenixRegistry : GameComponent`, holding `List<Pawn> registeredWearers`
(Scribe'd via `LookMode.Reference`) — the single source of truth for both the 3-tattoo cap (FR-001–FR-006)
and the revival-sweep driver (R3). **No Harmony patch or manual registration step is needed to add this
component to the game at all.**

**Rationale**: This mod has zero existing `GameComponent`/`WorldComponent` usage (confirmed, prior research
pass) — genuinely new territory. Decompiling `Verse.Game.ExposeData()`/`FillComponents()` shows RimWorld
itself already auto-discovers every non-abstract `GameComponent` subclass via reflection
(`typeof(GameComponent).AllSubclassesNonAbstract()`) and instantiates any not already present
(`Activator.CreateInstance(type, this)`, requiring only a public `(Game game)` constructor) — this runs
during `ExposeData()`'s `LoadingVars` phase for *both* a brand-new game and a loaded save, so simply defining
the class with the right constructor shape is sufficient; RimWorld adds it to `Game.components` and persists
it (via the same polymorphic `Scribe_Collections.Look(..., LookMode.Deep, this)` every other `GameComponent`
already goes through) with no extra wiring. Look it up anywhere via `Current.Game?.GetComponent
<GameComponent_PhoenixRegistry>()`, the same pattern `TattooTrackerUtility.GetTracker` already uses for
per-pawn comp lookups, wrapped in a small static accessor for convenience at call sites (Dialog_ChooseTattoo,
the two faction-change patches, the cremation patch).

**Alternatives considered**: A Harmony postfix on `Game.FinalizeInit()` manually adding the component if
missing — unnecessary; decompiled evidence shows vanilla's own `FillComponents()` already does this
generically for every mod-defined `GameComponent` subclass with no cooperation required.

## R12: Combat Extended — no interaction needed anywhere in this feature

**Decision**: No CE-specific branch anywhere in Phoenix's implementation — resurrection, corpse/rot,
cremation, and the two faction-change patches are all confirmed CE-untouched; the cauterization mechanic
needs no CE-specific handling either, despite CE genuinely touching the same `BleedRate` surface.

**Rationale**: Full decompile of the real, locally installed `CombatExtended.dll` found zero references to
`Resurrect`, `CompRottable`, `RotProgress`, `Crematorium`, `CremateCorpse`, or `RecipeWorker` anywhere in
CE's own code (its only `Corpse` references are unrelated animal-hunting/loot code); `SetFaction` references
are limited to unrelated cross-mod compatibility shims. CE **does** generically Harmony-patch the
`BleedRate` getter on every `HediffWithComps` subclass (reflection over `AllSubclassesNonAbstract
(typeof(HediffWithComps))`) with a **postfix** multiplying by its own `HediffComp_Stabilize.BleedModifier`
only `if (__result > 0f)`. Because vanilla's own `Hediff_Injury.BleedRate`/`Hediff_MissingPart.BleedRate`
already return exactly `0f` once `IsTended()` is true — checked inside the base getter, *before* CE's
postfix ever runs — Phoenix's own `hediff.Tended(...)` cauterize call still fully zeroes `BleedRate` under
CE with no extra code, and reading `.BleedRate`/`.Bleeding` for "which wound is worst" already reflects any
CE stabilization state for free, since it's the exact same property either way. Mirrors feature 013's own
"confirmed zero interaction via full decompile, not assumed" precedent, extended one step further here since
this feature's bleeding mechanic genuinely shares surface with CE without needing to special-case it.

## R13: Skill independence (FR-031)

**Decision**: No skill check anywhere in Phoenix's own mechanics (revival roll, cauterization proc,
Paralytic Abasia) — every roll (`Rand.Chance`) and threshold read in this design is skill-free by
construction, matching FR-031's explicit requirement, distinct from the unrelated ritual-station application
skill check (feature 001, `TattooRitualSuccessCurve`) that still gates getting Phoenix applied in the first
place. Noted here for completeness; no design decision was needed to satisfy this — nothing in the design
above reads any pawn skill at all.
