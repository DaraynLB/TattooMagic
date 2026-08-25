# Implementation Plan: Vampiric Thorn Tattoo Effect (Sixth Passive Tattoo, Melee Lifesteal Slice)

**Branch**: `010-vampiric-thorn-tattoo-effect` | **Date**: 2026-08-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-vampiric-thorn-tattoo-effect/spec.md`

## Summary

Turn Vampiric Thorn from an inert stub Hediff (shipped by feature 001) into a working PRD §6 tattoo: an instant
small heal to the wearer's own worst wound on every melee hit they land, upgrading in place to a stronger Tier 2
heal plus a brief additional heal-over-time window on a killing blow, via a "melee hits landed" progression
counter (PRD §5.4), reusing feature 002's value accessor (`TattooEffectValues`) and tier tracker
(`TattooTierProgress`) unmodified. This is the sixth passive tattoo and the third to extend the shared
`Pawn.PostApplyDamage` dispatch patch (after Frost Sigil's melee-taken clause and Serpent's Eye's ranged-landed
clause): it adds one new reuse point, `IOnMeleeHitLandedTattooEffect`, dispatched to the *attacking* pawn's own
comps whenever their own melee attack lands — the melee-attacker-side counterpart to Serpent's Eye's
ranged-attacker-side reuse point, carrying a `killedTarget` flag confirmed (by inspecting RimWorld's actual
assembly) to be reliably readable at that same dispatch point with no separate "on kill" patch needed. It also
extracts Bloodrune's (feature 007) inline "heal the worst open wound, roll over the leftover" logic into a small
shared `TattooHealingUtility`, since this is the first time a second tattoo needs the identical pattern — Bloodrune
is updated to call the same helper rather than leaving two parallel copies of it. No CE-specific branching is
needed anywhere in this feature (confirmed against a real, installed CE copy, not assumed) — healing an injury and
detecting a melee kill are both vanilla health/pawn-lifecycle concepts CE doesn't touch.

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–009 and
this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* (no new patch — this feature adds a second dispatch clause to the
existing shared `Pawn.PostApplyDamage` postfix, `Patch_Pawn_PostApplyDamage_TattooOnHit`, per FR-013),
`Krafs.Rimworld.Ref` 1.6.* — same as features 001–009, no new external dependency, no DLC dependency, no Combat
Extended-specific interaction needed (research.md R6 confirms CE neither replaces the melee-hit-landed dispatch
path nor reinterprets injury healing or pawn-death detection)

**Storage**: RimWorld's built-in Scribe save/load system — Vampiric Thorn's tier, progression counter, and
post-kill recovery window state (Tier 2) all persist as `HediffComp` state on the existing
`TattooMagic_Hediff_VampiricThorn` hediff, the exact same shape every prior tattoo's own comp already uses

**Testing**: No automated unit-test harness (unchanged from features 001–009 — RimWorld's `Verse`/`RimWorld` game
types aren't practically runnable outside the game process); validated via manual in-game verification in Dev Mode
per Constitution Principle II, using `quickstart.md`. No CE-specific behavioral scenario is uniquely required
beyond the standing "loads cleanly and behaves correctly with CE present" check (research.md R6 — mirrors
Bloodrune's/Berserker's Mark's precedent, not Serpent's Eye's, since nothing here is CE-dependent).

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime), with the
`killedTarget` timing decision (research.md R4) grounded against RimWorld's real, installed `Assembly-CSharp.dll`
(decompiled read-only during planning, no game code executed) rather than assumed, and CE-agnosticism grounded
against a real, locally installed Combat Extended copy (Steam Workshop item `2890901044`, `CETeam.CombatExtended`)

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features
001–009; no new project/scaffold needed

**Performance Goals**: The new dispatch clause runs only on actual landed melee hits (an existing, already-cheap
event-driven trigger the shared patch already fires for every melee hit, regardless of this feature), adding one
more `is IOnMeleeHitLandedTattooEffect` interface check per hediff comp on the attacker — the same per-hit cost
shape Frost Sigil's and Serpent's Eye's own clauses already have. The Tier 2 post-kill heal-over-time tick only
runs while a post-kill window is open (a few seconds at most per kill), gated the same way Bloodrune's own
`CompPostTick` heal-burst loop already is — not on every tick for every pawn.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony + CE
(Constitution Principle I) — expected to be trivially satisfied since research confirms this feature touches no
CE-replaced mechanism (research.md R6). All tunable numbers (Tier 1/Tier 2 per-hit lifesteal, Tier 2 post-kill
heal amount/duration, the heal-interval mechanism constant, the Tier 1→2 threshold) remain placeholders pending
PRD §9's balance pass, routed through `TattooEffectValues` per FR-012, using deliberately small/fast-to-test values
rather than "plausible-looking" ones (feature 006 research.md R8 precedent).

**Scale/Scope**: One existing inert `HediffDef` (`TattooMagic_Hediff_VampiricThorn`) gains a `<comps>` block; one
new `HediffComp` (the effect itself — tier progression, instant per-hit lifesteal, Tier 2 post-kill
heal-over-time window); one new reusable interface (`IOnMeleeHitLandedTattooEffect`); one new reusable static
helper (`TattooHealingUtility`, also adopted by Bloodrune's existing comp); one additive clause in the existing
shared dispatch patch — no new `HediffDef`, no new Harmony patch, no changes to Frost Sigil's, Serpent's Eye's,
Guardian's Call's, Stormlash's, or Berserker's Mark's own files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Touches a combat dispatch path (melee-hit-landed) and pawn health, so the question is live, but research.md R6 confirms CE neither replaces the `Pawn.PostApplyDamage` dispatch this feature extends (already proven under CE by Frost Sigil's own shipped melee clause) nor reinterprets `Hediff_Injury.Heal`/pawn death detection | **PASS** — no CE-specific branching is needed anywhere in this feature; the standing "loads cleanly with Harmony + CE" check still applies and is covered by quickstart.md's final scenario |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines the required in-game Dev Mode scenarios covering the Tier 1 per-hit lifesteal, the miss/blocked/struck-not-striking exclusions, the no-injury/no-tattoo no-ops, the progression counter and Tier 2 upgrade, the Tier 2 post-kill window (including the Tier 1 exclusion and the refresh-not-stack behavior), persistence, the removal guard, and a regression/CE-load check against existing on-hit consumers |
| III. Data-Driven, Tunable Balance | Yes — Tier 1/Tier 2 per-hit lifesteal, Tier 2 post-kill heal amount/duration, the heal-interval constant, the Tier 2 threshold | **PASS** — every value lives on `HediffCompProperties_VampiricThornEffect` XML fields (data-model.md) and is read exclusively through `TattooEffectValues` (FR-012), never as an inline magic number; placeholders trace to PRD §9 |
| IV. Two-Path Combat Patching | Only required for targeting/AI-decision logic (Guardian's Call is the PRD example); this feature adds no targeting or attack-target-selection interception of any kind | **PASS (N/A)** — same non-applicability as every passive/triggered tattoo except Guardian's Call; this feature's one dispatch-path change hooks the existing universal post-damage-application step, not verb selection or targeting AI |
| V. Progression Integrity Across Saves | Yes — Vampiric Thorn's own tier/progress counter and the Tier 2 post-kill window's timer/remaining-budget state | **PASS** — all persist via `CompExposeData()`'s `Scribe_Values.Look` calls (data-model.md); tier-crossing logic reuses `TattooTierProgress.TryRegisterQualifyingEvent` unmodified, already idempotent and deterministic (feature 002 precedent); the post-kill window's fields are plain absolute-tick/remaining-amount values that resume correctly on load with no derived-state reconstruction needed |

No violations requiring justification — **Complexity Tracking is not needed** for this feature.

*Post-Phase 1 re-check*: `data-model.md` and `contracts/melee-hit-landed-contract.md` confirm the design adds one
new interface (`IOnMeleeHitLandedTattooEffect`), one new shared static helper (`TattooHealingUtility`, also
adopted by Bloodrune's existing file as an additive-behavior-preserving refactor), and one additive clause to the
existing shared `Patch_Pawn_PostApplyDamage_TattooOnHit` — none touch targeting/AI logic or any CE-replaced
mechanism, and all persisted state routes through the same Scribe-backed `HediffComp` mechanism every prior
feature already established. Table above still holds; no new violations.

## Project Structure

### Documentation (this feature)

```text
specs/010-vampiric-thorn-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── melee-hit-landed-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── Tattoos/
        └── VampiricThorn.xml               # UPDATED — add HediffCompProperties_VampiricThornEffect
                                             #   entry with tier1/tier2/post-kill numeric fields
                                             #   (data-model.md), replacing the "effect not yet
                                             #   implemented" stub; hediffClass stays HediffWithComps

Source/
├── Effects/                                # Existing reusable passive-tattoo-effect infrastructure
│   │                                        #   (features 002/003) — TattooEffectValues,
│   │                                        #   TattooTierProgress, IProvidesTattooStatOffset,
│   │                                        #   StatPart_TattooEffectOffset, IOnRangedHitLandedTattooEffect,
│   │                                        #   CombatExtendedInterop, TattooHediffRemovalGuard all
│   │                                        #   reused UNMODIFIED
│   ├── IOnMeleeHitLandedTattooEffect.cs    # NEW — the melee-attacker-side counterpart to
│   │                                        #   IOnRangedHitLandedTattooEffect; tattoo-agnostic (FR-013)
│   └── TattooHealingUtility.cs             # NEW — shared "heal worst open injury, roll over leftover"
│                                            #   helper, extracted from Bloodrune's own inline logic
│                                            #   (research.md R5, FR-014)
├── Hediffs/
│   ├── HediffComp_VampiricThornEffect.cs   # NEW — Vampiric Thorn-specific: composes TattooTierProgress,
│   │                                        #   implements IOnMeleeHitLandedTattooEffect, calls
│   │                                        #   TattooHealingUtility for both the instant per-hit
│   │                                        #   lifesteal and the Tier 2 post-kill heal-over-time window
│   └── HediffComp_BloodruneEffect.cs       # UPDATED — HealTick() now calls
│                                            #   TattooHealingUtility.HealWorstInjury instead of its own
│                                            #   inline scan; no change to its own persisted fields,
│                                            #   CompPostTick cadence, or external behavior
└── Patches/
    └── Patch_Pawn_PostApplyDamage_TattooOnHit.cs   # UPDATED — adds a second, sibling dispatch clause
                                                     #   inside the existing melee branch (dinfo.Tool != null),
                                                     #   dispatched to the ATTACKER's comps via the new
                                                     #   IOnMeleeHitLandedTattooEffect, alongside the
                                                     #   untouched struck-pawn clause and the untouched
                                                     #   ranged clause
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–009 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) — no new project or scaffold. The new reusable pieces land in the existing
`Source/Effects/` folder (features 002/003's home for tattoo-agnostic infrastructure), and the one modified patch
lives in the existing `Source/Patches/` folder (feature 002's home for shared Harmony patches), keeping the
"shared infrastructure vs. tattoo-specific effect" boundary those features established. Vampiric Thorn's own comp
lives in `Source/Hediffs/`, mirroring every other tattoo-effect comp's placement exactly. `HediffComp_BloodruneEffect.cs`
is the one existing tattoo-specific file this feature touches, and only to redirect its internal healing scan
through the new shared helper — not to change any of its own behavior, persisted state, or public shape. No
feature 002/003/004/005/006/007/008/009 file besides that one and
`Patch_Pawn_PostApplyDamage_TattooOnHit.cs` is modified, and both changes are additive (a new clause, a redirected
internal call), not alterations to existing consumers.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
