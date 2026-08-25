# Phase 0 Research: Tattoo-Mastery XP Track & Slot Unlocking

## R1: Where to hook "a triggered ability was activated" pawn-wide, without per-tattoo opt-in code

**Decision**: Extend the existing shared `Patch_Pawn_GetGizmos_TattooGizmos` postfix (feature 004) to wrap the
`action` delegate of each `Command_Action` it collects from an `IProvidesTattooGizmo` comp, before yielding it —
the wrapped action calls the tattoo tracker's mastery-registration method, then the original action. No change
to `HediffComp_GuardiansCallEffect.cs`, `HediffComp_StormlashEffect.cs`, or `IProvidesTattooGizmo` itself.

**Rationale**: `IProvidesTattooGizmo` is only ever implemented by triggered tattoos — a passive tattoo has
nothing to click, so it never implements this interface. That means "a comp's gizmo action is invoked" and "a
triggered-tattoo ability was activated" are the same event, structurally, with no need to distinguish triggered
from passive at the call site (spec FR-004 falls out for free). Wrapping the action at the shared collection
point, rather than adding a call inside each tattoo's own `TryActivate()`, means a future triggered tattoo
(Bloodrune, Wraithstep, Berserker's Mark) automatically contributes to mastery progress the moment it implements
`IProvidesTattooGizmo` and returns a `Command_Action` — exactly spec FR-015's "no per-tattoo opt-in code"
requirement — and matches this project's established pattern of proving a reuse point generalizes without
touching prior consumers' files (feature 005's Tier 2 immunity did the same for `StatPart_TattooEffectOffset`
without touching Frost Sigil).

The gizmo itself already gates cooldown (`Disabled = true` when on cooldown per feature 004's contract), so
RimWorld never invokes a disabled gizmo's action — the wrapped action only ever fires on an activation that is
actually going to happen, keeping mastery progress in lockstep with each tattoo's own tier-progression counter
(spec Edge Cases).

**Alternatives considered**:
- Add an explicit call inside each `TryActivate()` (Guardian's Call, Stormlash, and every future triggered
  tattoo). Rejected: this is a cross-cutting concern, not tattoo-specific behavior; putting it in every
  implementer means a future tattoo author has to remember to add it, which is exactly the "per-tattoo opt-in
  code" FR-015 rules out, and it would require editing feature 004/005's already-shipped files for a change that
  has nothing to do with what those files do.
- A new `IOnTattooAbilityActivated` interface, implemented alongside `IProvidesTattooGizmo` by each triggered
  comp. Rejected as unnecessary: it would still be per-tattoo opt-in code, just spread across an extra
  interface instead of a raw call — no benefit over wrapping the gizmo's own action, since the action *is* the
  activation.

## R2: Where to store pawn-wide mastery progress and slot capacity

**Decision**: Extend the existing `HediffComp_TattooTracker` (feature 001) — already the mod's single per-pawn
bookkeeping comp, already owning `slotCapacity` — with a new persisted `masteryProgress` int and a
`RegisterMasteryActivation()` method that increments it, checks it against the two accessor-resolved
thresholds, grows `slotCapacity` in place when a threshold is crossed, and messages the player.

**Rationale**: `HediffComp_TattooTracker`'s own doc-comment already anticipates this exact feature ("slot
capacity... grown by a separate, not-yet-built progression feature; this feature only reads it"). Every pawn
already carries this comp from spawn (`Patch_Pawn_SpawnSetup_AddTattooTracker`), so there's no lifecycle problem
to solve — it's guaranteed present the first time a triggered ability could possibly be activated. Reusing it
keeps `slotCapacity` as the single source of truth (spec FR-013) rather than introducing a second place that
claims to know a pawn's slot count.

**Alternatives considered**:
- A new dedicated `HediffComp_TattooMastery` (or a new Hediff entirely) holding progress and mirroring
  `slotCapacity` back onto the tracker. Rejected: two comps owning overlapping state invites desync, and there's
  no reason to split "how many activations has this pawn logged" from "how many slots does this pawn have" when
  one already causes the other.
- A `GameComponent`/`WorldComponent` keyed by pawn. Rejected: every other piece of per-pawn tattoo state in this
  mod lives on a `HediffComp` attached to the pawn (Constitution Principle V's established precedent); a
  world-keyed side table would be a new persistence shape for no benefit and a real risk of desyncing from the
  pawn it's meant to describe across cross-map moves, pawn transfer, etc.

## R3: Threshold storage and the `TattooEffectValues` scope key

**Decision**: Add `slot2Threshold`/`slot3Threshold` fields to `HediffCompProperties_TattooTracker`, set in
`Defs/HediffDefs/TattooTracker.xml`. Read them through the existing `TattooEffectValues.Get` accessor (feature
002) using `ScopeKey = parent.def.defName` — which for this comp is always `"TattooMagic_Tracker"` (already a
stable, unique `HediffDef` name exposed via `TattooMagicDefOf.TattooMagic_Tracker`) — exactly the same
`ScopeKey` shape every tattoo-specific comp already uses (`HediffComp_GuardiansCallEffect.ScopeKey`,
`HediffComp_StormlashEffect`'s equivalent).

**Rationale**: No new `Def` type or settings object is needed — `TattooMagic_Tracker` is already a per-pawn,
always-present `HediffDef` with its own defName, which is all `TattooEffectValues`'s scope/value-key scheme
requires. This keeps the mastery track's tunables discoverable and overridable through the exact same mechanism
(and, eventually, the exact same future settings UI, PRD §10) as every other tattoo's numbers, satisfying
Constitution Principle III without inventing a parallel convention.

**Alternatives considered**:
- A new standalone `TattooMagicMasterySettingsDef` purely to hold the two thresholds. Rejected: adds a new Def
  type and a new scope-key convention for two numbers, when the tracker's own already-existing, already-unique
  `HediffDef` name does the job with zero new infrastructure.

## R4: Idempotent, deterministic threshold crossing

**Decision**: `RegisterMasteryActivation()` increments `masteryProgress`, then loops
`while (slotCapacity < 3 && masteryProgress >= ThresholdForNextSlot(slotCapacity))`, incrementing
`slotCapacity` and emitting one `Messages.Message` per increment inside the loop. `ThresholdForNextSlot` maps
`slotCapacity == 1 → Slot2Threshold`, `slotCapacity == 2 → Slot3Threshold`.

**Rationale**: Mirrors `TattooTierProgress.TryRegisterQualifyingEvent`'s existing idempotence shape (feature
002): once a tier/slot has been granted, re-evaluating past its threshold is inert. The `while` loop (rather
than a single `if`) defensively handles a large single jump in `masteryProgress` (e.g. a `TattooEffectValues`
override lowering both thresholds below the pawn's current progress at once, or a Dev Mode/debug adjustment) by
granting each slot in turn with its own notification, instead of silently skipping straight to 3 slots with no
explanation — consistent with Constitution Principle V's determinism requirement without adding real complexity
(the loop runs at most twice, ever, per pawn).

**Alternatives considered**:
- A single `if (slotCapacity == 1 && progress >= t2) slotCapacity = 2; else if (slotCapacity == 2 && progress >=
  t3) slotCapacity = 3;` (no loop). Rejected: silently strands a pawn at 2 slots for one extra activation if a
  large jump ever clears both thresholds at once, instead of granting both — a smaller correctness gap than the
  loop costs to write.

## R5: Player-visible slot capacity in existing UI, and the now-obsolete dev-only test aid

**Decision**: Add an always-visible label in `Dialog_ChooseTattoo` (once a recipient is selected) showing that
pawn's current slot capacity and how many are free — no `Prefs.DevMode` gate. Remove the existing
`"[DEV] Grant +1 tattoo slot"` button and its surrounding `Prefs.DevMode` block from the same file.

**Rationale**: `Dialog_ChooseTattoo` is already the feature 001 tattoo-selection UI and already reads
`tracker.slotCapacity`/`tracker.FreeSlots` in its dev-only button label — satisfying spec FR-010/SC-006 is a
small extension of an existing display, not new UI surface. The dev button's own comment states its purpose
explicitly: *"there's no in-game way to earn extra tattoo slots yet... this button exists purely to unblock
that test and should go away once slot progression is actually implemented."* This feature is that progression
—leaving the button in place after this feature ships would be dead, misleading debug scaffolding sitting next
to the real mechanism it was a stand-in for.

**Alternatives considered**:
- Leave the dev button in place alongside the new real mechanism. Rejected per the button's own comment and
  this project's stated aversion to backwards-compatibility scaffolding once its reason for existing is gone;
  it would also let a developer silently bypass the real progression while testing, masking bugs in the real
  mechanism.

## R6: Player notification wording and channel

**Decision**: `Messages.Message($"{pawn.LabelShortCap} has grown skilled enough with their tattoos to support {slotCapacity} tattoo slots!", pawn, MessageTypeDefOf.PositiveEvent);`
— same call shape (`text, LookTargets, MessageTypeDef`) as the existing ritual success/failure messages in
`JobDriver_TattooRitual.cs`.

**Rationale**: Reuses an established, already-verified-working convention in this codebase rather than
introducing a new notification mechanism (letter, dialog, etc.) for what is a minor, non-blocking positive
event — consistent with how the ritual's own success/failure is communicated today.

**Alternatives considered**:
- A `Letter` (via `Find.LetterStack.ReceiveLetter`). Rejected as heavier than warranted for a per-pawn
  progression tick that happens repeatedly over a colony's lifetime (up to twice per pawn); `Messages.Message`
  matches the weight of the ritual-result precedent already in this codebase.

## R7: Testing approach

**Decision**: No automated unit-test harness, unchanged from every prior feature (research.md precedent from
features 001–005) — RimWorld's `Verse`/`RimWorld` game types aren't practically runnable outside the game
process. Validated via manual in-game verification in Dev Mode per Constitution Principle II, using
`quickstart.md`.

**Rationale**: Consistent with this project's established testing approach; this feature introduces no new
external system (no CE interaction, no targeting AI) that would change that calculus.

## R8: Placeholder threshold values chosen for fast manual testing, not just plausibility

**Decision**: `slot2Threshold = 3`, `slot3Threshold = 8` (rather than a "plausible-looking" draft like `10`/`30`).

**Rationale**: Both remain PRD §9 placeholders regardless of the number chosen — nothing about them is final
until a dedicated balance pass. Picking small numbers up front avoids the temporary-bump-then-revert-then-
rebuild cycle prior features needed when their initial "plausible" placeholder made manual testing impractically
slow (e.g. feature 005's Frost Sigil proc-chance/duration values, deliberately widened for Scenario 4 and then
reverted in that feature's own Polish phase — two extra edit/rebuild round trips). There is no cost to shipping
the fast-to-reach number as the actual placeholder, since a real balance pass replaces both values anyway
regardless of what they start as.

**Alternatives considered**:
- Ship "plausible" values (`10`/`30`) and lower them temporarily during manual testing via a `TattooEffectValues`
  override or a throwaway XML edit, then revert afterward. Rejected: `TattooEffectValues` has no setter yet
  (feature 002's own doc-comment: "No setter exists yet — nothing in the codebase writes to the override
  dictionary today"), so the only lever available is editing XML directly and rebuilding — exactly the
  edit → rebuild → test → edit-back → rebuild cycle this decision avoids entirely.

## R9: A Dev Mode debug action to jump mastery progress directly, replacing the removed slot-capacity button

**Decision**: Add a `Prefs.DevMode`-gated button to `Dialog_ChooseTattoo`, in the same spot the removed
`"[DEV] Grant +1 tattoo slot"` button occupied (R5), that adds a fixed amount of `masteryProgress` to the
selected pawn's tracker and immediately re-runs the same threshold-check logic a real activation uses. To avoid
duplicating that logic, the `while` loop introduced in R4 is extracted into its own method,
`CheckSlotThresholds()`, called by both `RegisterMasteryActivation()` (the real path) and the new debug action.

**Rationale**: Two independent benefits, not just raw speed:
- **Speed**: reaching `slot3Threshold` through real activations means waiting out a triggered tattoo's cooldown
  (300-1800 ticks) once per point of progress; a debug jump reaches any target value in one click.
- **Coverage of a branch real testing can't reach at all**: the `while` loop's defensive multi-threshold-
  crossing case (R4) — a single jump in `masteryProgress` that crosses both the slot-2 and slot-3 thresholds at
  once — can only be exercised by adding more than one point of progress in a single check. A stream of real
  activations (each calling `RegisterMasteryActivation()`, which only ever adds exactly one point before
  checking) can never produce that input. Without a debug jump, this branch would ship having only ever been
  exercised by static reasoning (research.md R4's own justification), not an actual run — the same gap this
  project's Constitution Principle II exists to close.

**Alternatives considered**:
- Rely solely on RimWorld Dev Mode's built-in cooldown-reset tools plus real repeated clicking to reach higher
  thresholds. Rejected: still only ever adds one point of progress per click, so it can never exercise the
  multi-crossing branch, and remains slower than a direct jump even for the ordinary single-threshold cases.

## R10: Extending pawn-wide mastery progress to passive tattoos (added 2026-08-12, post-implementation)

**Decision**: Extend the existing shared `Patch_Pawn_PostApplyDamage_TattooOnHit` postfix (feature 002/003) to
also call `TattooTrackerUtility.GetTracker(pawn)?.RegisterMasteryActivation()` once per dispatched notification —
inside `DispatchMeleeHit`'s loop, credited to the struck pawn, for every `IOnMeleeHitTattooEffect` comp notified;
inside `DispatchRangedHit`'s loop, credited to the attacker, for every `IOnRangedHitLandedTattooEffect` comp
notified. No change to `HediffComp_FrostSigilEffect.cs`, `HediffComp_SerpentsEyeEffect.cs`, or either interface.

**Rationale**: Live playtesting of this feature surfaced a real gap: a pawn who only ever receives passive
tattoos (Frost Sigil, Serpent's Eye, and future ones) had *no possible path* to ever unlock slot 2 or 3, since
`RegisterMasteryActivation()` was only ever reachable through the gizmo wrap (R1), and passive tattoos have no
gizmo. This was the original spec's explicit, deliberate scope (spec.md FR-004/SC-008 as originally written,
citing PRD §5.1's "active tattoo abilities are used" wording) — not an implementation oversight — but revisiting
it, permanently capping every passive-only pawn at 1 slot forever, with no mechanism to ever change that, is a
worse outcome than the alternative. `Patch_Pawn_PostApplyDamage_TattooOnHit` is the exact structural analog of
`Patch_Pawn_GetGizmos_TattooGizmos` for passive tattoos — already the single shared, tattoo-agnostic dispatch
point every current and future `IOnMeleeHitTattooEffect`/`IOnRangedHitLandedTattooEffect` implementer goes
through — so extending it the same way R1 extended the gizmo patch satisfies FR-016/FR-017 (this decision's own
added requirements) with zero changes to Frost Sigil's or Serpent's Eye's own files, preserving this feature's
core design principle end to end.

Granularity: counts every dispatched notification, **not** gated on whether that tattoo's own effect internally
procs (Frost Sigil rolls its own slow chance privately inside `OnMeleeHitTaken`; the shared dispatch point has no
visibility into that roll's outcome, and gating on it would require changing the interface's return signature and
every implementer's file — exactly the per-tattoo opt-in code FR-017 rules out). This means mastery-progress
accrual from passive tattoos is coarser, and likely much faster in active combat, than each such tattoo's own
private tier-progression counter (which does gate on internal procs, e.g. Frost Sigil's own tier-2 threshold).
Flagged as a balance-pass concern (`docs/PRD.md` §9) alongside the two threshold placeholders (R8) rather than
solved here — like those thresholds, the exact numbers aren't final regardless, only the mechanism needs to work
correctly for whatever values are eventually chosen.

**Alternatives considered**:
- Change `IOnMeleeHitTattooEffect.OnMeleeHitTaken`/`IOnRangedHitLandedTattooEffect.OnRangedHitLanded` to return a
  `bool` indicating whether a qualifying event occurred (mirroring each tattoo's own private proc-gating), and
  update `HediffComp_FrostSigilEffect`/`HediffComp_SerpentsEyeEffect` to report it. Rejected: requires touching
  every current and future passive tattoo's own file to return an accurate signal, which is exactly the
  per-tattoo opt-in code this feature's design (R1, FR-015, and this decision's own FR-017) has consistently
  avoided for triggered tattoos; the coarser dispatch-level count keeps that guarantee intact for passive tattoos
  too, at the cost of precision that a future balance pass can address separately (e.g. a lower passive
  contribution weight, or per-hit-type threshold scaling) without touching this shared patch's structure again.
- Leave passive-only pawns permanently capped at slot 1, matching the original spec, and only document the gap
  in `docs/PRD.md` §9 as a future open question. Rejected (explicit product decision after playtesting): a
  mechanic that structurally cannot ever apply to roughly half the tattoo roster (5-6 of 11 planned tattoos are
  passive) is a worse default than accepting a temporary balance imprecision that the placeholder threshold
  values already tolerate everywhere else in this feature.
