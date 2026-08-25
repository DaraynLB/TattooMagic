# Phase 1 Data Model: Tattoo-Mastery XP Track & Slot Unlocking

Builds on feature 001's `HediffComp_TattooTracker`/`TattooTrackerUtility` and feature 002's `TattooEffectValues`,
all reused and extended in place rather than duplicated. Feature 004's `IProvidesTattooGizmo` interface is
reused completely unmodified; its shared consumer, `Patch_Pawn_GetGizmos_TattooGizmos`, gains one small,
still-tattoo-agnostic extension (see below).

## Entities

### `HediffCompProperties_TattooTracker` (feature 001, updated)

XML-facing properties on `Defs/HediffDefs/TattooTracker.xml`'s comp list — read only as `xmlDefault` fallbacks
through `TattooEffectValues.Get`, never directly at a decision point (spec FR-012).

| Field | Type | Meaning |
|---|---|---|
| `slot2Threshold` | `int` | Pawn-wide `masteryProgress` value at which slot capacity grows from 1 to 2. Default `3`. |
| `slot3Threshold` | `int` | Pawn-wide `masteryProgress` value at which slot capacity grows from 2 to 3. Default `8`. |

Both are new fields on the existing properties class; no existing field changes shape. Both defaults are
deliberately small — fast to reach through a handful of real activations, or instantly via the Dev Mode debug
action below — rather than a "plausible-looking" larger number, since neither is final until PRD §9's balance
pass regardless (research.md R8).

### `HediffComp_TattooTracker` (feature 001, updated)

| Field | Type | Persistence | Meaning |
|---|---|---|---|
| `slotCapacity` | `int` | `Scribe_Values.Look` (existing, unchanged) | How many tattoos this pawn may currently have applied. This feature is the mechanism that grows it from 1 toward 3; ritual/slot-enforcement code (feature 001) keeps reading it exactly as before. |
| `masteryProgress` | `int` | `Scribe_Values.Look` (new) | Pawn-wide count of triggered-tattoo ability activations, regardless of which triggered tattoo. Starts at 0. |
| `appliedTattoos` / `pendingRitualTattoo` | *(existing, unchanged)* | *(existing, unchanged)* | Untouched by this feature. |

New behavior:

- `ScopeKey` (private, mirrors every tattoo-specific comp's own `ScopeKey => parent.def.defName`) → always
  `"TattooMagic_Tracker"` (research.md R3).
- `RegisterMasteryActivation()` → `masteryProgress++;` then calls `CheckSlotThresholds()`. This is the method the
  gizmo-wrap hook (below) calls once per real triggered-tattoo activation.
- `CheckSlotThresholds()` (private) → while `slotCapacity < 3` and `masteryProgress` has reached the
  accessor-resolved threshold for the next slot (`Slot2Threshold` when `slotCapacity == 1`, `Slot3Threshold`
  when `slotCapacity == 2`), increments `slotCapacity` and calls `Messages.Message(...)` (research.md R4, R6)
  once per increment. No-ops once `slotCapacity` is already 3. Extracted into its own method (rather than
  living inline inside `RegisterMasteryActivation()`) specifically so the Dev Mode debug action below can reuse
  the exact same threshold logic after a direct progress jump, rather than duplicating it (research.md R9).
- `DevAddMasteryProgress(int amount)` (public, only ever called from a `Prefs.DevMode`-gated UI control) →
  `masteryProgress += amount;` then calls `CheckSlotThresholds()` — the same threshold-check path a real
  activation uses, letting a tester reach any progress value (including a single jump across both thresholds
  at once) without waiting out cooldowns (research.md R9).
- `CompExposeData()` → existing body plus one new `Scribe_Values.Look(ref masteryProgress, "masteryProgress", 0)`
  call.

### `Patch_Pawn_GetGizmos_TattooGizmos` (feature 004, updated)

Still tattoo-agnostic — gains a small, generic step between collecting a gizmo from an `IProvidesTattooGizmo`
comp and yielding it:

```text
for each comp implementing IProvidesTattooGizmo on this pawn's hediffs:
    gizmo = comp.GetGizmo()
    if gizmo is Command_Action commandAction:
        originalAction = commandAction.action
        commandAction.action = () => {
            originalAction?.Invoke()
            TattooTrackerUtility.GetTracker(pawn)?.RegisterMasteryActivation()
        }
    yield return gizmo
```

The patch still never references any specific tattoo's comp type — it only knows "this is a `Command_Action`
collected from some `IProvidesTattooGizmo` implementer," matching the file's existing tattoo-agnostic framing.
See `contracts/mastery-progression-contract.md` §1 for the exact contract this establishes for future triggered
tattoos.

### `Patch_Pawn_PostApplyDamage_TattooOnHit` (feature 002/003, updated 2026-08-12, research.md R10)

The passive-tattoo counterpart to the gizmo wrap above — still tattoo-agnostic, gains one small, generic step in
each of its two existing dispatch loops:

```text
DispatchMeleeHit(struckPawn, attacker, dinfo, damage):
    for each comp on struckPawn implementing IOnMeleeHitTattooEffect:
        comp.OnMeleeHitTaken(attacker, dinfo, damage)
        TattooTrackerUtility.GetTracker(struckPawn)?.RegisterMasteryActivation()

DispatchRangedHit(attacker, target, dinfo, damage):
    for each comp on attacker implementing IOnRangedHitLandedTattooEffect:
        comp.OnRangedHitLanded(target, dinfo, damage)
        TattooTrackerUtility.GetTracker(attacker)?.RegisterMasteryActivation()
```

Counts every dispatched notification, not gated on whether that tattoo's own effect internally procs — see
research.md R10 for the granularity tradeoff and why. See `contracts/mastery-progression-contract.md` §4.

### XML: `Defs/HediffDefs/TattooTracker.xml` (updated)

```xml
<comps>
  <li Class="TattooMagic.HediffCompProperties_TattooTracker">
    <slot2Threshold>3</slot2Threshold>
    <slot3Threshold>8</slot3Threshold>
  </li>
</comps>
```

Both values placeholders pending PRD §9's balance pass (Constitution Principle III), consistent with every
tattoo shipped so far — deliberately small for fast manual testing rather than "plausible-looking" (research.md
R8).

### `Source/UI/Dialog_ChooseTattoo.cs` (updated)

- Removed: the `Prefs.DevMode`-gated `"[DEV] Grant +1 tattoo slot..."` block (research.md R5) — including its
  `rowCount` contribution, so the dialog's scroll-view sizing shrinks back down correctly once it's gone.
- Added: an always-visible label once `selectedRecipient != null`, e.g.
  `"{selectedRecipient.LabelShortCap}: {tracker.appliedTattoos.Count}/{tracker.slotCapacity} tattoo slots used"`,
  reading the same `tracker` this file already resolves via `TattooTrackerUtility.GetTracker` for its existing
  "no free tattoo slots" check — no new lookup introduced.
- Added: a `Prefs.DevMode`-gated `"[DEV] +N tattoo mastery progress"` button in the same spot the removed
  slot-capacity button occupied, calling `tracker.DevAddMasteryProgress(amount)` (research.md R9) — this is
  test-only surface, not a product feature, so it stays behind the same `Prefs.DevMode` gate the button it
  replaces used.

## State Transitions

`masteryProgress` only ever increases (never decremented by this feature), starting at 0. `slotCapacity`
transitions `1 → 2 → 3` strictly in order, each transition gated on `masteryProgress` reaching that
transition's accessor-resolved threshold, each transition idempotent (re-registering an activation after
`slotCapacity` is already 3 changes only `masteryProgress`, never overshoots 3) — same idempotence shape as
`TattooTierProgress`'s existing `tier` transition (feature 002), applied at the pawn-wide level instead of the
per-tattoo level (research.md R4).

Both `masteryProgress` and `slotCapacity` persist via `HediffComp_TattooTracker.CompExposeData()`, the same
`Scribe_Values.Look`-backed mechanism every other tattoo's tier/progress state already relies on (Constitution
Principle V).
