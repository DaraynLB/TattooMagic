# Contract: Pull-and-Apply-Effect (Pawn/Corpse-Targeted Relocation)

Documents the shape Soul Tether establishes for a triggered tattoo that relocates a *chosen pawn or corpse* to the
caster and then applies some effect to it on arrival — this mod's first pawn/corpse-targeted ability (every prior
targeted tattoo, Wraithstep and Bastion Ward, targets a ground cell instead, per `specs/008-wraithstep-tattoo-effect/
contracts/cell-targeted-ability-contract.md`). Written per this project's own convention of contract-documenting a
genuinely new mechanism even before a second confirmed consumer exists (mirrored from `specs/016-bastion-ward-
tattoo-effect/research.md` R4's identical justification for its own ground-fixed-zone contract) — the customer has
explicitly flagged a future "inverse" tattoo (pull a hostile, apply a debuff) as likely, per docs/PRD.md §10 and this
spec's own FR-019.

## 1. What is generic vs. what is Soul-Tether-specific

The mechanism has exactly two target-agnostic steps, both fully reusable as-is:

1. **Find a landing cell adjacent to the caster** — `CellFinder.TryFindRandomCellNear(caster.Position, caster.Map,
   2, c => c.Standable(caster.Map), out IntVec3 destination)`, falling back to `caster.Position` itself if no
   walkable cell is found nearby (never fails the cast; see data-model.md's `OnTargetConfirmed` step 2).
2. **Relocate the target Thing there** — `target.Position = destination;` then `Notify_Teleported()` *iff* `target
   is Pawn` (research.md R3 — the plain `Thing.Position` setter already performs correct spawned-thing bookkeeping
   for any `Thing`, including a `Corpse`; `Notify_Teleported()` is a `Pawn`-only refresh with no `Corpse`
   equivalent or need).

Everything else is Soul-Tether-specific and MUST NOT be assumed by a future consumer:

- **The target filter** ("friendly pawn or friendly pawn's corpse, within range, on the caster's own map, not the
  caster") lives entirely in `HediffComp_SoulTetherEffect.GetRejectReason` (data-model.md) — a future "inverse"
  tattoo writes its own filter (e.g. "hostile pawn, within range, not the caster") from scratch, reusing only the
  `TargetingParameters { canTargetPawns = true, canTargetCorpses = ..., canTargetLocations = false }` shape and the
  two-phase `Find.Targeter.BeginTargeting` flow (§2 below) — never Soul Tether's own friendliness check.
- **The arrival effect** (grant a tend-speed/quality buff, skipped for a corpse target) lives entirely in
  `OnTargetConfirmed` step 4 — a future consumer supplies its own arrival effect (e.g. apply a debuff, which might
  *not* skip a corpse target, or might not accept a corpse target at all) rather than reusing Soul Tether's tend-buff
  grant.
- **Whether corpses are valid targets at all** is a per-consumer decision (`canTargetCorpses` in
  `TargetingParameters`, plus whatever the arrival effect does or doesn't do for one) — not baked into the two
  generic steps above, which work identically whether `target` is a `Pawn` or a `Corpse`.

## 2. The two-phase targeting shape (extends the cell-targeted-ability contract to pawn/corpse targets)

Reuses `cell-targeted-ability-contract.md` §1/§2 verbatim (the `Command_Action` → `Find.Targeter.BeginTargeting` →
confirmed-target-only-activation shape, and the `IHandlesOwnMasteryProgress` opt-out) — the only change from that
contract's own documented shape is the `TargetingParameters` passed to `BeginTargeting`: `canTargetPawns = true`
and/or `canTargetCorpses = true` with `canTargetLocations = false`, instead of Wraithstep/Bastion Ward's
`canTargetLocations = true` with `canTargetPawns = false`. A cancelled targeting session (Escape/right-click) MUST
leave cooldown, tier-progression, and mastery-progress state completely unchanged, exactly as that contract already
requires for a location-targeted ability.

## 3. The `Corpse`-faction pitfall a future consumer MUST NOT repeat (research.md R2)

`GenHostility.HostileTo(someCorpse, otherThing)` is **not** a valid way to check a corpse's original faction — a
bare `Corpse`'s own `Thing.Faction` field is never populated by vanilla (`Pawn.MakeCorpse` never sets it), so this
call always evaluates as "not hostile," regardless of who the corpse actually was. Any future filter that needs to
reason about a `Corpse` target's faction MUST resolve `(target as Corpse)?.InnerPawn` first and check hostility
against that `Pawn` instead — Soul Tether's own `GetRejectReason` (data-model.md) is the concrete precedent for this
pattern.

## 4. What a future consumer reuses vs. writes fresh

| Piece | Reusable as-is? |
|---|---|
| Two-phase gizmo/targeting flow, `IHandlesOwnMasteryProgress` opt-out | Yes — `cell-targeted-ability-contract.md` §1/§2, unchanged. |
| `TargetingParameters` *shape* (pawn/corpse instead of location) | Yes — swap `canTargetLocations`/`canTargetPawns`/`canTargetCorpses` as needed. |
| §1 step 1 (find adjacent landing cell) | Yes, verbatim. |
| §1 step 2 (relocate `Thing`, `Notify_Teleported()` iff `Pawn`) | Yes, verbatim. |
| §3's `Corpse`/`InnerPawn` faction pitfall | Yes — applies to any future filter reasoning about a corpse's faction. |
| Target filter (friendly-only, range, same-map) | No — Soul-Tether-specific; write a new filter per consumer. |
| Arrival effect (tend buff) | No — Soul-Tether-specific; write a new effect per consumer. |
