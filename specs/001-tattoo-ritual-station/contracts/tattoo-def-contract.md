# Contract: TattooDef XML Schema & Runtime Extension Points

This feature's "external interface" isn't a network API — it's the Def
schema and runtime hooks that *later* features (individual tattoo effects,
XP/slot progression) are built against. This contract documents what those
later features can rely on without needing to read this feature's C#.

## 1. TattooDef XML shape (stable contract)

Every one of the 11 starter tattoos is authored as one `TattooDef` XML entry
under `Defs/TattooDefs/`. A later feature adding a 12th tattoo, or filling in
Tier 1/Tier 2 effects for an existing one, MUST be able to do so by:

- Adding/editing a `TattooDef` XML file (recipe fields per `data-model.md`)
  without touching `Source/`.
- Adding/editing the matching `HediffDef` under `Defs/HediffDefs/Tattoos/`
  (referenced by `TattooDef.appliedHediff`) to add real `HediffStage`
  effects — this feature guarantees the Hediff exists and is granted/removed
  correctly, but ships it with no stages defined.

**Guarantee**: `TattooDef.appliedHediff` is always non-null and always
resolves to a real, unique `HediffDef` for every shipped `TattooDef`. Later
features may assume this without a null-check against this feature's data.

## 2. Slot/tracker read contract (`HediffComp_TattooTracker`)

The XP/slot-progression feature needs to *increase* `slotCapacity` on the
tracker comp. This feature guarantees:

- Every humanlike colonist pawn has exactly one `TattooMagic_Tracker` Hediff
  (and therefore exactly one `HediffComp_TattooTracker`) from the moment
  they're spawned/generated, before any ritual can target them.
- `slotCapacity` starts at `1` and is a plain mutable `int` field exposed via
  `Scribe_Values` — a later feature may increment it directly (e.g. on an XP
  threshold) without needing to go through this feature's code.
- `appliedTattoos` is read-many/write-single: this feature is the only
  writer (on ritual Success), but any later feature may read it (e.g. to
  check "does this pawn have tattoo X" for a tier-progression counter).

**Guarantee**: `appliedTattoos.Count` never exceeds `slotCapacity` at any
point observable outside a ritual's own resolution — a later feature reading
these two fields never needs to reconcile an inconsistent state.

## 3. What is explicitly NOT part of this contract

- The success-curve shape (`research.md` R4) is an internal implementation
  detail of the ritual job, not something later features read or depend on.
- The `JobDriver_TattooRitual` toil sequence is internal; nothing outside
  this feature should call into it directly.
- Tier 1 → Tier 2 progression counters (PRD §5.4) are NOT part of this
  feature's data model at all — a later feature owns adding that state,
  most likely as additional fields on the per-tattoo Hediff or its own
  `HediffComp`, not on `HediffComp_TattooTracker`.
