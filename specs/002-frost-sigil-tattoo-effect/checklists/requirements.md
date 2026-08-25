# Specification Quality Checklist: Frost Sigil Tattoo Effect (Effect Pattern Vertical Slice)

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-08-06
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- User Story 3, FR-010, SC-006, and the "Effect Pattern" key entity claim
  reuse only for future **passive** tattoos (Ember Ward, Ironskin Glyph,
  Serpent's Eye, Starlight Ward, Vampiric Thorn), matching what Frost
  Sigil (itself passive) actually exercises. Reuse is explicitly NOT claimed
  for the 5 **triggered**/gizmo tattoos (Bloodrune, Stormlash, Wraithstep,
  Berserker's Mark, Guardian's Call), which need a different pattern
  (on-demand ability gizmo, cooldown, use-count counter) — see Assumptions.
  That pattern is intended to be the next vertical-slice feature after this
  one, not something this feature builds or validates.
- FR-012/SC-007 add a value-accessor indirection for tattoo balance numbers
  (so a future settings UI can plug in without touching effect code) while
  explicitly NOT building that settings UI itself, consistent with PRD §10
  deferring the UI but not precluding data-driven values now. This is a
  deliberate scope line, not an oversight: the indirection is cheap now and
  costly to retrofit across multiple tattoos' effect code later, while the
  UI itself gains nothing from being built early.
- User Story 3 and FR-010/SC-006 describe a maintainability/reuse outcome
  (the effect pattern is reusable by future tattoo work) rather than an
  end-player-facing outcome. This is intentional: the feature's explicit
  purpose, per the user's request, is to establish a reusable pattern before
  wiring up the remaining 10 tattoos, so a developer picking up the next
  tattoo is a legitimate stakeholder for this spec alongside the player.
  These items stay verifiable by code review rather than by play-testing,
  and don't reference any specific language, framework, or class name, so
  they don't violate the "no implementation details" check.
- No numeric balance values (cold-resistance amount, slow chance/magnitude,
  freeze chance/duration, tier threshold) are specified — this matches PRD
  §9, which explicitly defers per-tattoo tuning numbers to a separate
  balance pass. The spec's requirements and success criteria are written to
  hold for whatever values are eventually configured.
- All items pass on first validation pass; no iteration needed.
