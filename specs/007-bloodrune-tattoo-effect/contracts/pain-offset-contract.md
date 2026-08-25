# Contract: Tattoo-Granted Pain Offset via a `Hediff` Subclass Override

This documents a new, small pattern this feature introduces — not a shared interface any other tattoo is required
to implement, since Bloodrune is the only current consumer (research.md R3). It exists as a reference for any
*future* tattoo that also wants to affect its wearer's pain, so that tattoo can follow the same shape rather than
reinventing it, without this feature building unused generalized infrastructure today.

## 1. Why this isn't routed through `IProvidesTattooStatOffset`

Every prior tattoo effect that offsets a numeric pawn stat (Frost Sigil's cold resistance, Guardian's Call's Tier 2
armor, Stormlash's move/attack speed) does so by implementing `IProvidesTattooStatOffset.GetStatOffset(StatDef
stat)`, consumed generically by `StatPart_TattooEffectOffset` (feature 002) for whichever `StatDef`s that `StatPart`
is registered against (`TattooEffectStatPartInstaller`).

Pain is different: RimWorld's `HediffSet` computes a pawn's total pain by summing each currently-held `Hediff`'s own
`PainOffset` property directly — there is no `StatDef` for "pain" in the vanilla stat system, so
`StatPart_TattooEffectOffset` has no `StatDef` it could be registered against to cover this. A tattoo wanting to
affect pain therefore cannot use the existing stat-offset mechanism at all; it has to influence pain at the `Hediff`
level directly.

## 2. The pattern

1. The tattoo's own applied `Hediff` uses a small subclass of `HediffWithComps` (here, `Hediff_BloodruneEffect`)
   instead of plain `HediffWithComps`, overriding the `PainOffset` property.
2. That override delegates to the tattoo's own effect comp (here, `HediffComp_BloodruneEffect.CurrentPainOffset`)
   via `TryGetComp<T>()` — the same lookup idiom already used everywhere else in this codebase — rather than
   computing the value inline on the `Hediff` itself, so all of the tattoo's tunable-value logic
   (`TattooEffectValues`, tier state, active-window checks) stays in one place with the rest of that tattoo's
   behavior.
3. The comp's exposed value MUST return `0f` whenever the tattoo's pain-affecting condition isn't currently true
   (e.g. wrong tier, no active effect window) — mirroring `IProvidesTattooStatOffset`'s own "return 0f for anything
   you don't currently affect, never throw" contract.

## 3. What this does NOT cover

- This does not create a shared, multi-tattoo-summing mechanism the way `StatPart_TattooEffectOffset` sums every
  `IProvidesTattooStatOffset` comp's contribution. If two future tattoos both wanted to affect pain on the same
  pawn simultaneously, each would need its own `Hediff` subclass override, and RimWorld's own `HediffSet` pain
  calculation (which already sums every held `Hediff`'s `PainOffset`, not just this mod's) handles combining them
  correctly with no extra work from this mod — this is a property RimWorld's engine already provides generically,
  which is exactly why no new shared "summing" mechanism was needed here the way one was for stat offsets.
- This is not a general-purpose "any `HediffComp` can contribute a pain offset" interface. If a second tattoo needs
  this, extracting a small `IProvidesTattooPainOffset` interface (implemented by the comp, read by each tattoo's own
  `Hediff` subclass, or by a shared subclass those tattoos' Hediffs derive from) is the natural next step — deferred
  until an actual second consumer exists, consistent with this project's established reuse-when-proven approach
  (research.md R3's alternatives).
