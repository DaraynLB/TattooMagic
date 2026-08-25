# Phase 0 Research: Bloodrune Tattoo Effect

## R1: Reusing the triggered-tattoo gizmo/cooldown/tier shape wholesale

**Decision**: Bloodrune's own `HediffComp_BloodruneEffect` implements `IProvidesTattooGizmo` and is picked up by the
existing, unmodified `Patch_Pawn_GetGizmos_TattooGizmos` (feature 004). It holds a `TattooTierProgress` field (feature
002) and its own `cooldownEndTick`/`burstEndTick` ints, following `HediffComp_GuardiansCallEffect`'s and
`HediffComp_StormlashEffect`'s exact shape: `GetGizmo()` returns a `Command_Action` disabled while on cooldown,
`TryActivate()` no-ops if still on cooldown (defensive, since the gizmo already prevents this), sets both timers, and
calls `tierState.TryRegisterQualifyingEvent(...)`.

**Rationale**: This is now a three-times-proven shape (Guardian's Call, Stormlash, and now Bloodrune) — reusing it
exactly, rather than inventing anything new for activation/cooldown/tiering, is both the fastest path to a working
feature and the strongest possible confirmation that the shared reuse points (`IProvidesTattooGizmo`,
`TattooTierProgress`, `TattooEffectValues`) are genuinely reusable infrastructure, not something that happened to work
twice by coincidence.

**Alternatives considered**: None seriously — deviating from an already-proven, twice-shipped shape for a tattoo that
fits it exactly would be reinventing a solved problem with no benefit.

## R2: Healing as a periodic tick during the burst window, not an instant full heal

**Decision**: `HediffComp_BloodruneEffect.CompPostTick` checks `Find.TickManager.TicksGame < burstEndTick`, and on a
fixed interval (mirroring `HediffComp_GuardiansCallEffect`'s `ReassertIntervalTicks` pattern) calls a `HealTick()` step
that heals a portion of the total configured heal amount, applied to the wearer's own most-severe currently-open
`Hediff_Injury` via that class's own `Heal(float)` API — moving on to the next-most-severe injury once one is fully
healed, doing nothing (no error) if there are no open injuries to heal.

**Rationale**: The spec (User Story 1) explicitly calls for "a burst over a few seconds," not an instant restore —
ticking a portion of the total heal amount over the burst's duration is what makes it read as a burst rather than a
single frame-one effect, and reuses the exact periodic-CompPostTick-during-an-active-window pattern Guardian's Call
already established for its taunt-reassertion loop, rather than inventing a second way to do "something periodic while
a timer is running." Targeting the most severe open injury first is a small, intuitive default (heal the worst wound
first) rather than an even split across every injury, which would often leave every wound only partially treated
instead of closing any of them.

**Alternatives considered**:
- A single instant `Heal()` call the moment the ability activates. Rejected: doesn't match the spec's "burst over a
  few seconds" framing, and removes the visual/gameplay read of an active effect running (no distinguishable "burst
  window" for Tier 2's pain reduction to be scoped to, either).
- Healing every open injury a little each tick instead of the worst one first. Rejected as a slightly weaker default:
  it can leave several wounds all lingering instead of fully closing any of them within one burst, which reads as
  less useful in the common case (a colonist with 2-3 wounds after a fight). Not a hard requirement either way — this
  remains a placeholder-quality decision like every other numeric/behavioral default in this feature, and can be
  revisited in the balance pass (PRD §9) without changing this feature's structure.

## R3: Pain reduction via a `Hediff`-level `PainOffset` override, not the existing `IProvidesTattooStatOffset` route

**Decision**: Bloodrune's own applied `HediffDef` (`TattooMagic_Hediff_Bloodrune`) changes its `hediffClass` from
plain `HediffWithComps` to a new subclass, `TattooMagic.Hediff_BloodruneEffect : HediffWithComps`, which overrides
`PainOffset` to return `TryGetComp<HediffComp_BloodruneEffect>()?.CurrentPainOffset ?? 0f` — a small value delegated
to the comp, which returns the tunable Tier-2 pain-reduction magnitude (as a negative offset) only while `tierState.tier
>= 2` and the current burst window (`burstEndTick`) is still open, and `0f` otherwise.

**Rationale**: RimWorld computes a pawn's total pain directly by summing each of its `Hediff`s' own `PainOffset`
property (`HediffSet`'s pain calculation), not through the `StatDef`/`StatWorker`/`StatPart` system the existing
`IProvidesTattooStatOffset` / `StatPart_TattooEffectOffset` mechanism (feature 004 R6, reused by every stat-offset
tattoo since) targets — there is no `StatDef` for "pain" that a `StatPart` could offset the way `ArmorRating_Sharp` or
`MoveSpeed` can be. Since Bloodrune's pain reduction applies to its own wearer (not a different pawn, unlike Frost
Sigil's slow proc, which is why that effect needs a *separate* temporary hediff granted to the attacker), the
simplest correct implementation is to override `PainOffset` directly on Bloodrune's own `Hediff` instance and delegate
to its own comp for the tier/window-aware value — no second `HediffDef`, no new project-wide interface, since no other
tattoo needs a pain offset today and inventing a generalized `IProvidesTattooPainOffset` interface for a single
consumer would be a premature abstraction this project's conventions otherwise avoid.

**Alternatives considered**:
- Route pain through the existing `IProvidesTattooStatOffset` mechanism anyway, by finding or adding some proxy
  `StatDef`. Rejected: no such vanilla `StatDef` exists for pain (it deliberately bypasses the `Stat` system), and
  inventing one would require a new, non-standard `StatWorker`/`StatDef` wired into RimWorld's pain calculation
  separately — considerably more machinery than a direct `PainOffset` override for a single tattoo's Tier 2 bonus.
- A separate, temporary "Bloodrune pain relief" companion `HediffDef` granted to the wearer for the burst's duration
  (mirroring Frost Sigil's `TattooMagic_Hediff_FrostSigilSlow` shape exactly). Rejected as unnecessary indirection:
  that pattern exists specifically because Frost Sigil's slow must apply to a *different* pawn (the attacker) than
  the one carrying the tattoo, so a second grantable hediff is the only way to attach a temporary effect to someone
  else. Bloodrune's pain reduction only ever needs to affect its own wearer, who already has a `Hediff` instance
  (Bloodrune's own) permanently attached for exactly this kind of override to live on.
- A generalized `IProvidesTattooPainOffset` interface, consumed by a new shared mechanism analogous to
  `StatPart_TattooEffectOffset`. Rejected for now as premature: it would be infrastructure built for a single current
  consumer, unlike `IProvidesTattooGizmo` (built because two tattoos needed it before feature 004 shipped) or the
  gizmo-wrap hook (built because the whole roster of future triggered tattoos needs it). If a second tattoo later
  needs a pain offset, extracting a shared interface at that point costs little and would be justified by an actual
  second consumer, consistent with this project's established reuse-when-proven-not-when-speculative approach.

## R4: No Combat Extended-specific interaction needed

**Decision**: Bloodrune's effect (healing, cooldown, pain reduction) requires no CE-aware branching and no
`CombatExtendedInterop` usage.

**Rationale**: Combat Extended replaces or remaps specific combat-facing `StatDef`s this mod has previously had to
branch on — armor (Ironskin Glyph), accuracy/sway (Serpent's Eye), movement/attack-speed (Stormlash), suppression-slow
(Stormlash's Tier 2 immunity). Healing an injury's severity and offsetting a `Hediff`'s `PainOffset` are core
`Verse`/`RimWorld` health-system concepts that CE does not touch or reinterpret — CE's own combat model changes how
damage is dealt and armor is resolved, not how an already-inflicted injury heals afterward or how pain is calculated
from existing hediffs. This mirrors Frost Sigil's own core slow effect, which was likewise CE-agnostic (only Serpent's
Eye's/Ironskin Glyph's/Stormlash's stat-facing pieces needed CE branches).

**Alternatives considered**: Proactively add a `CombatExtendedInterop` check "just in case." Rejected: this project's
established convention (Constitution Principle I) is to branch only where CE genuinely replaces a stat this feature
touches; adding a defensive branch with nothing on the other side of it would be dead code, not caution.
