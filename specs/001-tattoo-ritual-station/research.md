# Phase 0 Research: Tattoo Ritual Station & Applying a Tattoo

## R1: How should the "ritual" (performer + recipient + station) be modeled?

**Decision**: A custom `JobDef`/`JobDriver` (`TattooRitual`) assigned to the
*performer*, whose toils bring the performer to the station, reserve the
recipient (also required to path to/stand at the station), consume the
ingredient cost, run a skill-check toil, and resolve success/mishap at the
end. Driven by a custom `WorkGiver` so it surfaces through the normal work
system rather than a manually-queued Bill.

**Rationale**: Vanilla `Bill`/`RecipeDef` machinery is built to consume
ingredients and produce a *Thing* (or operate on the Bill-holder itself via
`RecipeDef.Worker`), not to act on a second, independently-selected pawn at a
third pawn's workbench. The closest built-in analogues don't fit:
- **Surgery / medical bills** (`BillMedical`, `RecipeDef` with
  `<surgery>true</surgery>`) do operate performer-on-recipient with a skill
  check, but they run through the Health tab against Medicine skill and
  don't require a workbench — wrong skill, wrong UX entry point, and
  semantically reserved for injuries/implants in players' mental model.
- **Ideology rituals** (`RitualDef`) support a leader/participants + outcome
  chances, but pull in `RitualObligationTrigger`, precept, and ideoligion
  infrastructure that assumes the Ideology DLC's ritual system is in play —
  unwanted DLC coupling for what the PRD describes as an always-available
  crafting-station action.

A bespoke Job keeps the mechanic available without Ideology, keyed to
Artistic (not Medicine) skill, and anchored to a physical station as PRD §5.2
specifies.

**Alternatives considered**: Reusing `BillMedical`/surgery pipeline
(rejected — wrong skill/UX/DLC assumptions above); reusing `RitualDef`
(rejected — same DLC coupling concern, and rituals are participant-group
oriented, not a simple two-pawn recipe).

## R2: How should "this pawn has tattoo X applied" and slot capacity persist?

**Decision**: One invisible, always-present tracker `Hediff`
(`TattooMagic_Tracker`, added to humanlike pawns via a Harmony postfix on
pawn generation/spawn) carrying a `HediffComp_TattooTracker`. That comp
exposes (via `ExposeData`/`Scribe`) the pawn's current slot capacity (default
`1`, per PRD §5.1) and the list of currently-applied `TattooDef`s. Each
individual applied tattoo is *additionally* granted as its own real,
player-visible `Hediff` (one `HediffDef` per tattoo) via
`pawn.health.AddHediff`, so it shows on the Health tab like other semi-
permanent pawn modifications (PRD's own "similar to bionics" framing, §5).

**Rationale**: Hediffs are natively Scribe-persisted by RimWorld's health
system, satisfying Constitution Principle V without hand-rolling save/load
code. Splitting "bookkeeping" (the tracker comp: capacity + membership) from
"player-visible effect" (one Hediff per tattoo) means this feature only has
to stand up the bookkeeping and stub Hediffs; the later effects/progression
feature fills in each tattoo Hediff's actual stat/ability behavior without
touching the tracker.

**Alternatives considered**: A `ThingComp` attached directly to the pawn
`ThingDef` (rejected — RimWorld's core pawn ThingDefs aren't ones this mod
owns, so injecting a comp onto them requires a fragile Def-patch across every
humanlike race rather than the well-established "invisible tracker hediff"
idiom); a `GameComponent`-keyed dictionary from pawn → tattoo state
(rejected — works but does not travel with the pawn on caravan/transfer the
way a Hediff automatically does, and duplicates save-plumbing Hediffs give
for free).

## R3: How is the ingredient recipe defined and consumed?

**Decision**: Each `TattooDef` (a custom `Def` subclass, not vanilla
`RecipeDef`) carries an ingredient list in the same shape as vanilla
`RecipeDef.ingredients` (`ThingDef`/category filter + count), plus a
`workAmount`. The custom `JobDriver_TattooRitual` resolves and consumes these
directly (via standard ingredient-search/haul-then-consume toils) rather than
running through `RecipeWorkerCounter`/Bill machinery, since R1 already
rejected Bills as the execution vehicle.

**Rationale**: Reusing the *data shape* of `RecipeDef.ingredients` keeps the
XML familiar to anyone who's authored a vanilla recipe, satisfies
Constitution Principle III (data-driven, not hardcoded), and leaves the
actual numbers as placeholders per PRD §9 without inventing a new schema
convention.

**Alternatives considered**: A flat `List<ThingDefCountClass>` with no
filter/category support (rejected — less flexible for a later balance pass
that might want "any leather" rather than one exact ThingDef).

## R4: How does performer skill translate into a success/mishap chance?

**Decision**: A single shared `SimpleCurve` (Artistic skill level 0–20 →
success probability), defined as a tunable constant referenced by all 11
tattoos in v1, with per-`TattooDef` override left as an available-but-unused
XML field for a future balance pass. Points are `0 → 0%, 20 → 100%`, linear
in between (5 percentage points per skill level) — a deliberate design
decision (confirmed during in-game playtesting, superseding this doc's
original `0 → 50%, 20 → 95%` placeholder), not a placeholder: a totally
unskilled performer's attempt is never *blocked* (per spec FR-005) but is
guaranteed to mishap, and skill 20 is a true certainty.

**Rationale**: Mirrors vanilla's own pattern of skill-keyed `SimpleCurve`s
for success-chance mechanics (e.g. surgery success factors), which is a
familiar, inspectable, XML-editable shape — satisfying Constitution
Principle III. A shared curve avoids ten-way duplication for v1 since the
PRD gives no reason yet for tattoos to differ in difficulty; the override
field keeps that door open without speculative complexity now.

**Alternatives considered**: Hardcoded per-tattoo pass/fail thresholds in C#
(rejected — directly conflicts with Constitution Principle III); a flat
skill-independent chance (rejected — spec FR-006 requires skill to visibly
affect outcomes).

## R5: Testing strategy

**Decision**: No automated test project. Verification is manual, in Dev
Mode, following `quickstart.md`.

**Rationale**: RimWorld's `Verse`/`RimWorld` game types are not practically
instantiable outside the running game process (no headless harness exists in
this toolchain), and Constitution Principle II already establishes that
in-game verification is the actual bar for "done," not a unit-test pass.

**Alternatives considered**: A pure-C# unit test project around any
game-independent helper logic (e.g. the success-curve lookup) — left as an
option for implementation time if such logic ends up cleanly isolated, but
not required by this plan since the curve lookup is trivial enough that
in-game verification covers it.
