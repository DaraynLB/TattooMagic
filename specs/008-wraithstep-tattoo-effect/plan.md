# Implementation Plan: Wraithstep Tattoo Effect (Fourth Triggered Ability + First Cell-Targeted Ability Slice)

**Branch**: `008-wraithstep-tattoo-effect` | **Date**: 2026-08-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-wraithstep-tattoo-effect/spec.md`

## Summary

Wires up Wraithstep's gameplay effect (PRD §6, roster item 7) — the tattoo's `TattooDef`/`HediffDef` scaffolding
already exists (feature 001) but is inert. This feature makes it a real triggered ability: an activatable gizmo
that, unlike every prior triggered tattoo (Guardian's Call, Stormlash, Bloodrune), requires the player to pick a
destination cell before it fires, then instantly relocates the wearer there on a cooldown. At Tier 2, the maximum
range is larger and, immediately after landing, the wearer is briefly excluded from hostile AI's attack-target
selection. Two genuinely new pieces of infrastructure fall out of this: (1) the first two-phase (click-then-target)
triggered-ability shape, which requires a small, generic opt-out extension to feature 006's mastery-progress
gizmo-wrap hook so a gizmo click alone (without a confirmed destination) doesn't wrongly count as an activation
(research.md R4); and (2) the first *exclusion*-style targeting patch, implemented as a Harmony Prefix composing
`AttackTargetFinder.BestAttackTarget`'s `validator` parameter — the inverse of Guardian's Call's *inclusion*-style
Postfix (research.md R6) — alongside (not modifying) Guardian's Call's own patch.

## Technical Context

**Language/Version**: C# via .NET Framework 4.7.2 (RimWorld 1.6's runtime target, matching features 001–007 and
this project's `rimworld-modding` toolchain)

**Primary Dependencies**: `Lib.Harmony` 2.3.* — one new patch file (`Patch_AttackTargetFinder_BestAttackTarget_
WraithstepUntargetable`, a Prefix alongside Guardian's Call's existing Postfix on the same vanilla method) plus one
small, generic condition added to the existing `Patch_Pawn_GetGizmos_TattooGizmos` Postfix (research.md R4); the
ability's own gizmo contribution otherwise reuses the existing `IProvidesTattooGizmo` reuse point with no new patch.
`Krafs.Rimworld.Ref` 1.6.4871 — same as features 001–007, no new external dependency, no DLC dependency (the design
deliberately avoids RimWorld's native `AbilityDef`/`Verse.Ability`/`CompAbilityEffect_Teleport` framework for the
same DLC-gating reason feature 004 research.md R1 already established, even though the reference assembly confirms
that framework ships a purpose-built teleport comp — research.md R3/R5). Destination targeting uses
`Verse.Targeter`/`TargetingParameters`, confirmed present via direct reflection-metadata inspection of the reference
assembly (research.md R2) — not a new package, part of the base game API surface every RimWorld mod already targets.

**Storage**: RimWorld's built-in Scribe save/load system — Wraithstep's own `cooldownEndTick`/`untargetableEndTick`
fields and its `TattooTierProgress` state persist via `Scribe_Values.Look`, the same shape as every prior triggered
tattoo's own comp. The transient `WraithstepTargetingSession`/targeting-in-progress state is intentionally
non-persisted (data-model.md State Transitions) since no targeting session can span a save/load boundary.

**Testing**: No automated unit-test harness (unchanged from features 001–007 — RimWorld's `Verse`/`RimWorld` game
types aren't practically runnable outside the game process); validated via manual in-game verification in Dev Mode
per Constitution Principle II, using `quickstart.md`. This feature uniquely requires validating live-UI behavior
(range ring, reject-reason feedback) that prior tattoos didn't need to check, in addition to the standard
non-CE/CE-loaded pass.

**Target Platform**: RimWorld 1.6 (Windows/Mac/Linux via the game's own Mono/Unity runtime)

**Project Type**: RimWorld mod — extends the existing single `Source/` C# project + XML Defs from features 001–007;
no new project/scaffold needed

**Performance Goals**: Gizmo contribution cost is identical to every prior triggered tattoo's existing cost profile
(one gizmo collected per `GetGizmos()` call). The new targeting-exclusion Prefix follows the exact same hot-path
cheapness discipline as Guardian's Call's own patch (research.md R6, `targeting-exclusion-contract.md` §2) — a
single `HasActiveUntargetable` boolean check short-circuits the common case (no pawn currently untargetable
anywhere) before any per-candidate work. The range-indicator draw hook (research.md R3) only runs while a Wraithstep
targeting session is actually open (a rare, player-initiated, short-lived UI state), not on every frame
unconditionally.

**Constraints**: Must load with zero Harmony/console errors with Harmony alone, and separately with Harmony + CE
(Constitution Principle I) — the targeting-exclusion Prefix touches the same combat-decision code path Guardian's
Call already does, so it inherits that feature's CE-verification obligation (Constitution Principle IV;
`targeting-exclusion-contract.md` §4) rather than being CE-agnostic the way Bloodrune's healing/pain effect was. All
tunable numbers (range per tier, cooldown, untargetable-window duration, Tier 2 threshold) remain placeholders
pending PRD §9's balance pass, routed through `TattooEffectValues` per FR-013, using deliberately small/fast-to-test
values rather than "plausible-looking" ones (feature 006 research.md R8 precedent).

**Scale/Scope**: One existing inert `HediffDef` (`TattooMagic_Hediff_Wraithstep`) gains a `<comps>` block (no
`hediffClass` change — plain `HediffWithComps` is sufficient, unlike Bloodrune); one new `HediffComp`
(`HediffComp_WraithstepEffect` — gizmo, two-phase targeting, cooldown, tier progression, Tier 2 exclusion window,
and its own range-ring/reject-reason draw callbacks); one new small static helper
(`WraithstepUntargetableRegistry`); one new marker interface (`IHandlesOwnMasteryProgress`); one new Harmony Prefix
(`Patch_AttackTargetFinder_BestAttackTarget_WraithstepUntargetable`); one small, generic condition added to the
existing `Patch_Pawn_GetGizmos_TattooGizmos.cs`. The range-ring/reject-reason UX (FR-003) needed neither a Harmony
patch nor a separate session-tracking class in the end — `RimWorld.Targeter.BeginTargeting`'s own richest overload
already accepts `onUpdateAction`/`onGuiAction` callbacks (research.md R3, corrected from the planning-time estimate
below once decompiled source confirmed the real signature). No changes to Guardian's Call's, Stormlash's, or
Bloodrune's own files.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to this feature? | Assessment |
|---|---|---|
| I. Dual-Mode Compatibility (Harmony + CE) | Yes — the Tier 2 targeting-exclusion Prefix touches the same hostile-targeting decision path Guardian's Call's CE-verified patch does | **PASS** — `quickstart.md` Scenario 9 requires an explicit CE-loaded verification pass for the exclusion window, mirroring Guardian's Call's own obligation; the design does not assume feature 004's precedent covers CE without re-verifying (research.md R6, `targeting-exclusion-contract.md` §4) |
| II. Verify Before Claiming Done | Yes | **PASS** — `quickstart.md` defines nine required in-game Dev Mode scenarios covering targeting/rejection UX, Tier 1 blink, Tier 2 upgrade/range/exclusion, interaction with Guardian's Call, cross-tattoo coexistence, mastery-track pass-through (including the cancelled-attempt non-credit case), persistence, and the CE-loaded exclusion pass |
| III. Data-Driven, Tunable Balance | Yes — range per tier, cooldown, untargetable-window duration, Tier 2 threshold | **PASS** — every value lives as an XML field on `HediffCompProperties_WraithstepEffect` and is read exclusively through `TattooEffectValues` (FR-013), never as an inline magic number; placeholders trace to PRD §9 |
| IV. Two-Path Combat Patching | Yes — the Tier 2 untargetable window intercepts vanilla's hostile-targeting decision logic, the same category of mechanic Guardian's Call's taunt does | **PASS** — `targeting-exclusion-contract.md` documents the required vanilla patch (a Prefix, not a Postfix, per research.md R6's correctness argument) and commits to the same CE-loaded independent-verification obligation Guardian's Call already established, with the same documented fallback (a second, reflection-based CE patch) if verification shows it's needed |
| V. Progression Integrity Across Saves | Yes — Wraithstep's own tier/progress counter and cooldown/untargetable-window timers | **PASS** — all persist via `CompExposeData()`'s `Scribe_Values.Look` calls; tier-crossing logic reuses `TattooTierProgress.TryRegisterQualifyingEvent` unmodified, which is already idempotent and deterministic (feature 002 precedent); the transient targeting-session state is deliberately excluded from persistence since it cannot meaningfully span a save (data-model.md State Transitions) |

No violations requiring justification — **Complexity Tracking is not needed** for this feature. The two touches to
existing shared files (`Patch_Pawn_GetGizmos_TattooGizmos.cs`'s mastery-wrap condition, and the new Prefix living
alongside Guardian's Call's existing Postfix on the same vanilla method) are both small, fully generic extensions
anticipated by their respective contracts (`mastery-progression-contract.md` §2's explicit "this contract can be
extended" clause; `two-path-targeting-contract.md` §3's explicit "a future targeting tattoo... implements its own
registry/patch pair against this same convention" guidance) — not ad hoc special-casing.

*Post-Phase 1 re-check*: `data-model.md` and both contract documents confirm the design adds no new `HediffDef`
class hierarchy change, and touches no other tattoo's own file. The two new patterns (a two-phase gizmo/targeting
shape, and a validator-composing exclusion Prefix) are each documented as small, generalizable extensions with an
explicit "what's reusable vs. tattoo-specific" boundary, not new interfaces every future tattoo must implement.
Table above still holds; no new violations.

*Post-implementation re-check*: both implementation-time verification items research.md flagged (the exact
`Targeter` draw-hook mechanism for R3, and the exact `Pawn`-level teleport-notification signature for R5) were
resolved by decompiling the actual `Krafs.Rimworld.Ref` 1.6.4871 assembly with `ilspycmd` before writing any code
against them, consistent with this project's established practice (feature 004 research.md R4/R5) of confirming
rather than asserting unverifiable internal specifics. R5 confirmed as planned (`Pawn.Notify_Teleported`). R3
resolved *simpler* than planned: `RimWorld.Targeter.BeginTargeting`'s richest overload already accepts
`onUpdateAction`/`onGuiAction` callbacks directly, so no Harmony patch and no separate session-tracking class were
needed after all — `plan.md`'s Project Structure and `data-model.md`/`contracts/cell-targeted-ability-contract.md`
were updated to drop `Patch_Targeter_*_WraithstepRangeIndicator.cs` and `WraithstepTargetingSession.cs`
accordingly. This is a reduction in surface area, not a new violation; the Constitution Check table above is
unaffected (one fewer Harmony patch than planned touches no principle's applicability).

## Project Structure

### Documentation (this feature)

```text
specs/008-wraithstep-tattoo-effect/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/            # Phase 1 output (/speckit-plan command)
│   ├── cell-targeted-ability-contract.md
│   └── targeting-exclusion-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Defs/
└── HediffDefs/
    └── Tattoos/
        └── Wraithstep.xml                       # UPDATED — adds <comps><li Class="TattooMagic.
                                                     #   HediffCompProperties_WraithstepEffect"> with the tunable
                                                     #   range/cooldown/untargetable-window/threshold fields
                                                     #   (data-model.md). hediffClass unchanged.

Source/
├── Effects/
│   └── IHandlesOwnMasteryProgress.cs            # NEW — marker interface (no members), consumed by the small
                                                     #   generic condition added to Patch_Pawn_GetGizmos_
                                                     #   TattooGizmos.cs (research.md R4)
├── Hediffs/
│   ├── HediffComp_WraithstepEffect.cs           # NEW — gizmo (IProvidesTattooGizmo), IHandlesOwnMasteryProgress,
                                                     #   two-phase target selection (Find.Targeter.BeginTargeting,
                                                     #   RimWorld namespace — research.md R3), destination
                                                     #   validity/reject-reason logic plus its own range-ring/
                                                     #   reject-label draw callbacks, instant relocation, cooldown,
                                                     #   TattooTierProgress, Tier 2 untargetable-window state
                                                     #   (research.md R1/R2/R3/R4/R5, data-model.md). Also defines
                                                     #   HediffCompProperties_WraithstepEffect in the same file,
                                                     #   matching every prior tattoo effect's one-file-per-tattoo
                                                     #   convention.
│   └── WraithstepUntargetableRegistry.cs         # NEW — lazily-pruned HashSet of comps with an active Tier 2
                                                     #   untargetable window, mirroring GuardiansCallTauntRegistry's
                                                     #   shape (research.md R6, targeting-exclusion-contract.md §2)
└── Patches/
    ├── Patch_AttackTargetFinder_BestAttackTarget_WraithstepUntargetable.cs
                                                     # NEW — Prefix composing the vanilla `validator` parameter to
                                                     #   exclude untargetable pawns from hostile target selection
                                                     #   (research.md R6, targeting-exclusion-contract.md §2),
                                                     #   living alongside (not modifying) Patch_AttackTargetFinder_
                                                     #   BestAttackTarget_GuardiansCallTaunt.cs
    └── Patch_Pawn_GetGizmos_TattooGizmos.cs        # UPDATED — one additional, fully generic condition: skip the
                                                     #   existing automatic mastery-progress wrap when the owning
                                                     #   comp implements IHandlesOwnMasteryProgress (research.md R4,
                                                     #   cell-targeted-ability-contract.md §2)
```

**Structure Decision**: Extends the existing single RimWorld-mod project from features 001–007 (`About/`,
`Assemblies/`, `Defs/`, `Source/`) rather than introducing a new project, Def type, or interface family beyond the
two small, purpose-built additions above. Wraithstep's own ability logic lives in `Source/Hediffs/`, next to every
other tattoo-effect comp. Unlike Bloodrune (which needed a `Hediff` subclass override for `PainOffset`), Wraithstep
needs no `hediffClass` change — its effects (position, targeting eligibility) don't route through any per-`Hediff`
override point, only through its own comp's state and the two Harmony patches above.

## Complexity Tracking

*No entries — Constitution Check reported no violations.*
