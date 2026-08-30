# Implementation Plan: Phoenix Tattoo Effect

**Branch**: `014-phoenix-tattoo-effect` | **Date**: 2026-08-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/014-phoenix-tattoo-effect/spec.md`

## Summary

The 14th tattoo, and the first to touch death, corpses, or faction-wide state at all. A colonist wearing
Phoenix who actually dies (not downed) automatically revives after a tiered wait (5 days Tier 1 / 3 Tier 2),
rolling a decomposition-based success chance once a day until it succeeds or the body is permanently lost
(destroyed, buried, or fully decayed — research.md R6); every attempt burns the corpse/wearer and advances a
lifetime counter that starts applying an escalating-duration Paralytic Abasia debuff from the 3rd attempt
onward. A separate day-counter (reset on every revival) permanently upgrades the tattoo to Tier 2. A
deliberate crematorium-bill cremation guarantees the next attempt succeeds, at the cost of any un-stripped
gear (already inherent to that vanilla bill, research.md R5). A faction-wide 3-tattoo cap is enforced via a
new `GameComponent`, which also — necessarily, research.md R3 — is what actually drives the multi-day
wait/retry timer, since a normal `HediffComp` goes silent the instant its pawn dies and the closest vanilla
precedent (`Hediff_DeathRefusal`'s `Corpse.TickRare` hook) can't survive the cremation exception destroying
the corpse mid-wait. An entirely independent passive gives the wearer a chance to auto-cauterize the single
most severe qualifying bleed (severed limbs always prioritized) via self-gated `HediffComp` polling, sharing
only the burn-application code with the revival mechanic and no other state.

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–013
and this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (three new patches — `RecipeWorker.ConsumeIngredient`,
`Pawn.SetFaction`, `Pawn_GuestTracker.SetGuestStatus`, research.md R4/R5/R7 — plus reuse, unmodified, of
`Patch_HealthTracker_RemoveHediff_ProtectTattoos` via the existing generic `appliedHediff` registry),
`Krafs.Rimworld.Ref` 1.6.* — same as features 001–013, no new external dependency, no DLC dependency. Combat
Extended interaction confirmed **absent** for resurrection/rot/cremation/faction-change by fully decompiling
the real, locally installed `CombatExtended.dll`; the cauterize passive shares `BleedRate`/`Tended()` surface
with CE's own field-stabilization patch but needs no CE-specific branch (research.md R12) — the second
tattoo, after Starlight Ward, confirmed CE-clean by full decompile rather than assumed.

**Storage**: RimWorld's built-in Scribe save/load system, used two ways this feature is the first to combine:
per-pawn state (`HediffComp_PhoenixEffect`'s tier/day-counter/wait-timer/cremation-flag fields) via
`CompExposeData()`, the same mechanism every prior tattoo already uses; and, new to this mod, faction-wide
state (`GameComponent_PhoenixRegistry.registeredWearers`, a `List<Pawn>` via `LookMode.Reference`) persisted
automatically as part of `Game.components`'s own generic polymorphic Scribe pass — no manual registration
code needed, since RimWorld's own `Game.FillComponents()` already auto-discovers every `GameComponent`
subclass by reflection (research.md R11).

**Testing**: No automated unit-test harness (unchanged from features 001–013). Validated via manual in-game
verification in Dev Mode per Constitution Principle II, using `quickstart.md`. This feature's real-world
multi-day timers (5/3-day waits, daily retries, a days-long Tier-2 progression clock) make waiting them out
for real impractical for testing, so `quickstart.md` leans more heavily on dedicated Dev Mode debug-action
tools than any prior feature has — forcing death, forcing a scheduled attempt to resolve immediately, pinning
corpse rot progress, and forcing cremation-guarantee state — mirroring feature 013's own precedent of
building deterministic debug tooling for a mechanic too slow/organic to reliably trigger by hand.

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime).
`RimWorld.ResurrectionUtility.TryResurrect`, `RimWorld.CompRottable` (`RotProgress`/`Stage`/
`GenTemperature.RotRateAtTemperature`), `Verse.RecipeWorker.ConsumeIngredient` + the vanilla `CremateCorpse`
`RecipeDef`, `Verse.Pawn.SetFaction`/`RimWorld.Pawn_GuestTracker.SetGuestStatus`, `Verse.HealthUtility`'s own
bleed-scanning shape, `Verse.Gene_Clotting`'s `Tended(...)`-to-stop-bleeding precedent, and
`Verse.HediffComp_Disappears`'s public `ticksToDisappear`/`disappearsAfterTicks` fields were all grounded
against the real, installed `Assembly-CSharp.dll` (decompiled read-only via `ilspycmd`, no game code
executed) rather than assumed — including one place where the design proposal's own assumption
("the Resurrector Mech Serum uses the same [head/brain] gate") turned out to be incorrect on decompile
(research.md R1), and one genuine gap found in vanilla's own `ResurrectionUtility.TryResurrect` that this
feature's cremation path has to work around directly (research.md R4).

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–013; no new project/scaffold needed.

**Performance Goals**: `HediffComp_PhoenixEffect.CompPostTick` runs two independently self-gated checks
(days-alive advance, ~once/in-game-day; cauterize scan, ~once/real-second, matching feature 013's own
interval-gating convention) only while the wearer is alive — cheap relative to the existing per-tick
`CompPostTick` dispatch every `HediffWithComps` pawn already receives, and correctly does nothing at all once
the wearer is dead (vanilla's own `HealthTick()`/`HealthTickInterval()` early-return `if (Dead)`, research.md
R3). `GameComponent_PhoenixRegistry.GameComponentTick()` self-gates its own sweep to roughly once per 2,000
ticks (~20×/in-game-day — comfortably fine-grained against multi-day timers) and iterates only its own
`registeredWearers` list (bounded by the 3-tattoo cap, so at most 3–4 pawns ever, FR-005's temporary-4th
case included), not every pawn in the game.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony +
CE (Constitution Principle I) — satisfied with no CE-specific code anywhere in this feature (research.md
R12). All tunable numbers (tiered wait days, the decomposition-vs-chance curve, burn severities, Abasia
duration/increment, the Tier-2 day threshold, cauterize proc chances, the trivial-bleed floor, the faction
cap itself) remain placeholders pending this spec's own Assumptions/PRD §9 balance pass, routed through
`TattooEffectValues` (Constitution Principle III) — including the decomposition curve, authored as a
`SimpleCurve` whose final control point sits at a small nonzero Y value so `SimpleCurve.Evaluate`'s own
clamp-to-last-point behavior satisfies FR-013's "approaches but never reaches zero" with no separate
floor-clamping code (research.md/data-model.md). The burial-ends-revival behavior (Assumptions section) is
carried forward exactly as flagged there — an unconfirmed working assumption, not re-litigated by this plan.

**Scale/Scope**: One new tattoo `HediffDef`/`TattooMagicDef` pair (plain `HediffWithComps`, no custom
`Hediff` subclass needed — a first for a tattoo this mechanically complex, research.md R3); one new
`HediffComp` (tier/day-counter/wait-timer/cauterize, implementing `IProvidesTattooTierProgress` directly
rather than composing the shared `TattooTierProgress`, since Phoenix's own progression counter resets on
revival unlike every other tattoo's monotonic one, data-model.md); one new small static utility
(`PhoenixRevivalUtility`, shared between the `GameComponent`'s sweep and Dev Mode debug tooling); one new
`GameComponent` (this mod's first, both the faction cap registry and the revival-timer driver); three new
Harmony patches (cremation detection, two faction-change hooks funneling into one shared strip helper); one
new debuff `HediffDef` with no dedicated comp (reuses vanilla's own `HediffComp_Disappears`, its duration set
programmatically per occurrence, research.md R10); one new `TattooMagicDefOf` entries trio; one small,
additive condition in `Dialog_ChooseTattoo`'s existing tattoo-listing loop — no changes to any of the other
13 tattoos' own files, `IOnIncomingDamageTattooEffect`, `IOnMeleeHit*Tattoo Effect`, or
`TattooTierProgress` itself.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Only the general "loads/behaves cleanly" bar — nothing in this feature touches a combat stat, targeting, or armor pipeline CE replaces | **PASS** — research.md R12 confirms, by fully decompiling `CombatExtended.dll`, zero CE references to resurrection/rot/cremation/faction-change anywhere in CE's own code; the one genuine shared surface (CE's generic `BleedRate` stabilization postfix) already composes correctly with this feature's own `Tended()`-based cauterize call with no branch needed, since vanilla's own `IsTended()` check runs before CE's postfix ever sees the value |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines the required in-game Dev Mode scenarios covering all four user stories (natural revival at both tiers with daily retry, the 3-tattoo cap across application/pending-death/leave-and-rejoin, cremation's guaranteed-bypass with gear loss, and the independent cauterize passive), plus persistence, the removal-guard, permanent-loss conditions (destroyed/dessicated/buried), and a CE-load/regression pass — leaning on new dedicated debug-action tooling (mirroring feature 013's precedent) since this feature's timers are real-world days long |
| III. Data-Driven, Tunable Balance | Yes — every numeric this feature introduces (see Scale/Scope) | **PASS** — every value lives on `HediffCompProperties_PhoenixEffect` XML fields (including the faction cap itself and the decomposition curve as a `SimpleCurve`, mirroring `TattooMagicDef.successCurveOverride`'s existing convention) and is read exclusively through `TattooEffectValues.Get(...)` at point of use, matching every tattoo shipped so far; placeholders trace to this spec's own Assumptions section via the standard XML top-of-file comment convention |
| IV. Two-Path Combat Patching | Only required for targeting/AI-decision logic; this feature adds no targeting or attack-target-selection interception of any kind | **PASS (N/A)** — same non-applicability as every non-Guardian's-Call tattoo |
| V. Progression Integrity Across Saves | Yes — Phoenix's own per-pawn tier/day-counter/wait-timer/cremation state, **and**, new to this mod, faction-wide registry state | **PASS** — per-pawn state persists via `CompExposeData()` exactly like every prior tattoo (data-model.md); the new faction-wide `GameComponent_PhoenixRegistry` persists automatically through `Game.components`'s own existing generic polymorphic Scribe pass (research.md R11) with no bespoke wiring, and is deterministic/idempotent across save/reload mid-wait — a pending revival's absolute `reviveAttemptTick` and `cremationGuaranteed` flag simply get re-evaluated fresh against the current tick count on the next sweep after load, with no separate "resume an in-progress wait" code path needed (data-model.md's persistence summary) |

No violations requiring justification — **Complexity Tracking is not needed** for this feature. This is
mechanically the largest tattoo shipped so far (three new patches, a new `GameComponent`, a new debuff
`HediffDef`, where most prior features needed at most one new patch and reused everything else), but every
piece of new surface is required directly by this feature's own FRs (death/corpse/faction/rarity-cap
mechanics none of the other 13 tattoos need at all) — not speculative infrastructure for a future tattoo —
and every existing shared contract that does fit (`IProvidesTattooTierProgress`, `TattooEffectValues`,
`TattooHediffRemovalGuard`) is reused unmodified, per the same "no premature abstraction, no unjustified
exception either" bar every prior feature's plan has held itself to.

*Post-Phase 1 re-check*: `data-model.md` confirms the design above holds with no new violations — five new
production pieces (`HediffComp_PhoenixEffect` + properties, `GameComponent_PhoenixRegistry`,
`PhoenixRevivalUtility`, three Harmony patches sharing one strip helper, one comp-less debuff `HediffDef`),
zero changes to any existing tattoo's own files or to any of `IOnIncomingDamageTattooEffect`/`IOnMeleeHit*
TattooEffect`/`TattooTierProgress`. The one deliberate, well-justified deviation from the closest available
vanilla precedent (`Hediff_DeathRefusal`'s `Corpse.TickRare`-driven ticking, research.md R3) is necessary,
not incidental: that pattern cannot survive the cremation exception destroying the corpse `Thing` mid-wait,
which this feature's own FR-020/FR-022 require to still work.

## Project Structure

### Documentation (this feature)

```text
specs/014-phoenix-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

No `contracts/` directory — this feature introduces no new interface or reusable infrastructure a *future*
tattoo is expected to consume (data-model.md's header note); it reuses `IProvidesTattooTierProgress`,
`TattooEffectValues`, and `TattooHediffRemovalGuard` exactly as they stand, mirroring feature 012/013's own
precedent of skipping `contracts/` when nothing generic is introduced.

### Source Code (repository root)

```text
Defs/
├── TattooDefs/
│   └── Phoenix.xml                              # NEW — the recipe-side TattooMagicDef
│                                                     #   (tattooType=Passive, appliedHediff=
│                                                     #   TattooMagic_Hediff_Phoenix, standard
│                                                     #   ingredients/workAmount shape)
└── HediffDefs/
    ├── Tattoos/
    │   └── Phoenix.xml                          # NEW — TattooMagic_Hediff_Phoenix,
    │                                                 #   HediffWithComps + HediffCompProperties_
    │                                                 #   PhoenixEffect (data-model.md), every
    │                                                 #   numeric a placeholder pending this
    │                                                 #   spec's own Assumptions/PRD §9 pass
    └── ParalyticAbasia.xml                      # NEW — TattooMagic_Hediff_ParalyticAbasia,
                                                      #   sits alongside TattooTracker.xml/
                                                      #   FrostSigilSlow.xml (support hediffs,
                                                      #   not under Tattoos/); capMods stages +
                                                      #   vanilla HediffCompProperties_Disappears,
                                                      #   duration set programmatically at apply
                                                      #   time (research.md R10)

Source/
├── Defs/
│   └── TattooMagicDefOf.cs                      # UPDATED — three new entries: TattooMagic_
                                                      #   Phoenix, TattooMagic_Hediff_Phoenix,
                                                      #   TattooMagic_Hediff_ParalyticAbasia
├── Hediffs/
│   └── HediffComp_PhoenixEffect.cs              # NEW — HediffCompProperties_PhoenixEffect +
│                                                     #   HediffComp_PhoenixEffect (data-model.md):
│                                                     #   tier/day-counter/wait-timer/cremation-
│                                                     #   flag state, implements IProvidesTattoo
│                                                     #   TierProgress directly (not via composed
│                                                     #   TattooTierProgress — its own progression
│                                                     #   counter resets on revival, unlike every
│                                                     #   other tattoo's monotonic one); CompPostTick
│                                                     #   drives the alive-only days-alive advance
│                                                     #   and the self-gated cauterize passive
├── Effects/
│   ├── GameComponent_PhoenixRegistry.cs         # NEW — this mod's first GameComponent: the
│                                                     #   faction-wide 3-tattoo-cap registry AND
│                                                     #   the sole driver of the multi-day revival
│                                                     #   wait/retry timer (research.md R3/R11,
│                                                     #   data-model.md) — auto-added by vanilla's
│                                                     #   own Game.FillComponents(), no manual
│                                                     #   registration code
│   └── PhoenixRevivalUtility.cs                 # NEW — static: TryResolveAttempt (decomposition
│                                                     #   roll or cremation-guaranteed success,
│                                                     #   ResurrectionUtility.TryResurrect + the
│                                                     #   cremation fallback-spawn gap, research.md
│                                                     #   R4), ApplyBurn (shared with cauterize),
│                                                     #   RegisterAttemptAndMaybeApplyAbasia —
│                                                     #   factored out so a Dev Mode debug action
│                                                     #   can force-resolve an attempt directly
├── Patches/
│   ├── Patch_RecipeWorker_ConsumeIngredient_PhoenixCremation.cs
│   │                                             # NEW — prefix on Verse.RecipeWorker.
│   │                                             #   ConsumeIngredient; detects the CremateCorpse
│   │                                             #   bill on a Phoenix-tattooed corpse specifically
│   │                                             #   (research.md R5), sets cremationGuaranteed +
│   │                                             #   captures the fallback spawn location before
│   │                                             #   Destroy() runs (research.md R4)
│   ├── Patch_Pawn_SetFaction_PhoenixRegistryUpdate.cs
│   │                                             # NEW — postfix on Verse.Pawn.SetFaction; strip+
│   │                                             #   unregister on leaving Faction.OfPlayer
│   │                                             #   (recruit-away/defect/exile/enslave, FR-004),
│   │                                             #   register (allowed over cap) on joining
│   │                                             #   already-tattooed (FR-005)
│   └── Patch_PawnGuestTracker_SetGuestStatus_PhoenixCaptured.cs
│                                                 # NEW — postfix on RimWorld.Pawn_GuestTracker.
│                                                 #   SetGuestStatus; covers plain capture-as-
│                                                 #   prisoner, which SetFaction alone doesn't see
│                                                 #   (research.md R7); both this and the SetFaction
│                                                 #   patch funnel into one shared strip helper
├── Debug/
│   └── DebugAction_TestPhoenixRevival.cs        # NEW — Dev Mode tools to make this feature's
│                                                     #   real-world-days timers testable without
│                                                     #   waiting them out (quickstart.md), mirroring
│                                                     #   feature 013's own deterministic-debug-tool
│                                                     #   precedent for a too-slow-to-hand-test
│                                                     #   mechanic
└── UI/
    └── Dialog_ChooseTattoo.cs                   # UPDATED — one additive condition in the
                                                      #   existing tattoo-listing loop: skip
                                                      #   offering Phoenix once GameComponent_
                                                      #   PhoenixRegistry.IsAtCap (FR-006)
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–013 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) — no new project or scaffold. Every new file sits in the folder its
existing sibling files already occupy (`Source/Hediffs/` for the tattoo's own comp, `Source/Patches/` for the
three new Harmony patches, `Source/Debug/` for Dev Mode tooling, `Source/UI/`'s existing
`Dialog_ChooseTattoo.cs` gets one additive edit) — no new top-level `Source/` folder is introduced even for
the genuinely new `GameComponent`/faction-registry concept, which lives in `Source/Effects/` alongside this
mod's other cross-cutting shared infrastructure (`TattooEffectValues`, `TattooHediffRemovalGuard`,
`CombatExtendedInterop`), the closest existing fit for "mechanism that isn't itself a tattoo's own effect
comp." `Source/Effects/PhoenixRevivalUtility.cs` is deliberately named and scoped for Phoenix specifically
(not a generic "resurrection utility" other tattoos might use) — nothing else in the roster has any use for
it, mirroring Starlight Ward's own precedent (feature 013) of keeping a feature's genuinely new technique
private to that feature rather than forcing a premature shared abstraction. `TattooTierProgress`,
`IOnIncomingDamageTattooEffect`, `IOnMeleeHit*TattooEffect`, and every other tattoo's own files are untouched.

## Complexity Tracking

*No entries — Constitution Check reported no violations requiring justification.*
