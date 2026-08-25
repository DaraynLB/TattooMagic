# Phase 1 Data Model: Tattoo Ritual Station & Applying a Tattoo

Derived from the spec's Key Entities section and the decisions in
`research.md`. Scope is deliberately limited to what this feature needs to
get a tattoo from "selected" to "applied and occupying a slot" — tattoo
*effects* and *slot-capacity growth* belong to later features and are only
referenced here as read/extension points.

## TattooDef (custom `Def`)

Identity + ritual recipe for one of the 11 starter tattoos. One XML file per
tattoo under `Defs/TattooDefs/`.

| Field | Type | Notes |
|---|---|---|
| `defName` | string | Standard Def key, e.g. `Tattoo_BloodRune` |
| `label` | string | Player-facing name, e.g. "Bloodrune" |
| `description` | string | Player-facing flavor text |
| `tattooType` | enum `{Passive, Triggered}` | Stored for later features; unused by this feature's logic beyond display |
| `ingredients` | list of (ThingDef-or-category filter, count) | Consumed by the ritual; shape mirrors vanilla `RecipeDef.ingredients` (research.md R3) |
| `workAmount` | float | Ritual duration input, same convention as `RecipeDef.WorkAmountTotal` |
| `successCurveOverride` | `SimpleCurve` (optional) | If absent, the shared default curve (research.md R4) is used |
| `appliedHediff` | `HediffDef` reference | The stub Hediff granted to the recipient on success (see below) |

**Validation rules** (enforced by the mod, not just XML schema):
- `ingredients` must resolve to at least one entry with count > 0 — a ritual
  with no cost is not a valid TattooDef.
- `appliedHediff` must reference a Hediff that exists and is exclusive to
  this TattooDef (no two TattooDefs may point at the same HediffDef) — this
  is what makes "duplicate tattoo" detection in FR-010 possible by simply
  checking Hediff presence.

## Applied Tattoo Hediff (one `HediffDef` per tattoo, x11)

The player-visible record that a specific tattoo is on a specific pawn.
Defined under `Defs/HediffDefs/Tattoos/`, one per tattoo, each linked from
its `TattooDef.appliedHediff`.

| Field | Type | Notes |
|---|---|---|
| `defName` | string | e.g. `Hediff_Tattoo_BloodRune` |
| `label`/`description` | string | Shown on the Health tab |
| stage/stat effects | — | **Out of scope for this feature.** Ships as an inert stub Hediff (no `HediffStage` effects) in this feature; a later feature fills in the actual gameplay behavior and Tier 1/Tier 2 stages |

This feature's job ends at `pawn.health.AddHediff(appliedHediff)` on ritual
success — it does not define what the Hediff *does*.

## Tattoo Tracker (invisible `HediffDef` + `HediffComp_TattooTracker`)

One instance per humanlike pawn, added on pawn spawn/generation, never
player-visible. The single source of truth this feature reads/writes for
slot enforcement and duplicate-prevention.

| Field (on the comp) | Type | Notes |
|---|---|---|
| `slotCapacity` | int | Defaults to `1` per PRD §5.1. This feature only *reads* this value — growing it is the separate XP/slot-progression feature's responsibility. Exposed via `Scribe_Values` so a future feature can safely increment it. |
| `appliedTattoos` | `List<TattooDef>` | Every TattooDef currently applied to this pawn. Exposed via `Scribe_Collections`. |

**Derived/computed**:
- `FreeSlots` = `slotCapacity - appliedTattoos.Count`. Used to satisfy
  FR-007/FR-009 (slot decrement on success, block when zero free).
- `HasTattoo(TattooDef)` = `appliedTattoos.Contains(def)`. Used to satisfy
  FR-010 (no duplicate tattoos).

**Validation rules**:
- `appliedTattoos.Count` must never exceed `slotCapacity` — enforced at the
  single write path (ritual success), not recomputed/corrected elsewhere.
- A TattooDef must not appear in `appliedTattoos` more than once.

## Ritual Attempt (runtime-only, not persisted)

Represents one in-progress or just-resolved ritual. Lives only as long as
`JobDriver_TattooRitual` runs; nothing about an attempt is saved once it
resolves (its *outcome* becomes either a new Hediff + updated tracker state,
or nothing at all).

| Field | Type | Notes |
|---|---|---|
| `performer` | Pawn | Distinct from recipient (FR-004) |
| `recipient` | Pawn | Target of the ritual |
| `tattoo` | TattooDef | Selected at queue time |
| `state` | enum `{Queued, InProgress, Success, Mishap, Cancelled}` | See state transitions below |

### State transitions

```
Queued --(ingredients available, job starts)--> InProgress
InProgress --(interrupted: draft/downed/mental break/attack)--> Cancelled
InProgress --(toils complete, skill-check resolves true)--> Success
InProgress --(toils complete, skill-check resolves false)--> Mishap
```

- `Cancelled` MUST leave no trace on the recipient (FR-012, edge case:
  interruption) — no ingredient consumption is committed until the job
  actually starts consuming them, matching standard job-interruption
  behavior.
- `Success` MUST, atomically from the player's perspective: add
  `tattoo.appliedHediff` to `recipient`, add `tattoo` to the tracker's
  `appliedTattoos`.
- `Mishap` MUST leave `appliedTattoos`/slot count unchanged and MUST NOT
  refund already-consumed ingredients (FR-008).
- There is no transition back out of `Success` — applying a tattoo is
  one-way in this feature (FR-011); no `Removed` state exists.

## Tattoo Ritual Station (`ThingDef` + `Building` subclass)

The workbench itself.

| Field | Type | Notes |
|---|---|---|
| `defName` | string | `Building_TattooRitualStation` |
| Standard building fields | — | Size, materials, work-to-build, power (if any) — balance/art details, not specified further by this feature |
| Interaction spot(s) | — | Needs at least one reserved cell/spot for the recipient distinct from the performer's standing position, since two pawns participate simultaneously |

No custom persisted fields on the Building itself — all durable state lives
on the pawns (tracker + applied Hediffs), not on the station.
