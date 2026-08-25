# Phase 1 Data Model: Starlight Ward Tattoo Effect

Builds on feature 001's `TattooMagicDef` → `appliedHediff` chain, feature 002's `TattooEffectValues` /
`TattooTierProgress` / `IProvidesTattooStatOffset` / `StatPart_TattooEffectOffset`, and feature 003's
`TattooHediffRemovalGuard` — all reused unmodified. Two genuinely new pieces of production code: one `HediffComp`
(+ its `HediffCompProperties`), and a small situational-mood-thought pair (`ThoughtDef` + `ThoughtWorker` +
`Thought_Situational` subclass) — the first tattoo to touch mood at all. No new shared interface/contract is
introduced: neither the risk-window detection nor the mood-thought lookup is consumed by anything but Starlight
Ward's own comp (research.md R4/R5).

## `TattooEffectStatPartInstaller` (updated — two new registrations, nothing else)

```csharp
Register(StatDefOf.MentalBreakThreshold);
Register(StatDefOf.PsychicSensitivity);
```

Added alongside the existing registrations (research.md R3). `StatPart_TattooEffectOffset` itself is unmodified —
already generic since feature 002.

## `HediffCompProperties_StarlightWardEffect` / `HediffComp_StarlightWardEffect`

Attached to `TattooMagic_Hediff_StarlightWard` (the existing stub Hediff from feature 001, currently with no
`<comps>` block). Composes a `TattooTierProgress` (feature 002, unmodified) and implements
`IProvidesTattooStatOffset` and `IProvidesTattooTierProgress` — no `IOnIncomingDamageTattooEffect` (this tattoo's
qualifying event isn't damage-based, research.md R4).

| Field (XML, on the props class) | Type | Notes |
|---|---|---|
| `mentalBreakResistanceTier1` / `mentalBreakResistanceTier2` | float | **Negative** offset to `StatDefOf.MentalBreakThreshold` (research.md R1 — lower threshold is better; sign matters, unlike every prior stat-offset tattoo) |
| `psychicSensitivityResistanceTier1` / `psychicSensitivityResistanceTier2` | float | **Negative** offset to `StatDefOf.PsychicSensitivity` (research.md R2) |
| `moodBuffTier2` | float | Tier 2 only (FR-005/FR-006); read by `Thought_StarlightWardTier2Active.MoodOffset()`, not by the comp itself — see below |
| `tier2Threshold` | int | Qualifying-event count (resisted risk windows, research.md R4) needed to promote Tier 1 → Tier 2 |

All fields above are read exclusively through `TattooEffectValues.Get(...)` at point of use (FR-011), matching
every tattoo shipped so far — including `moodBuffTier2`, via the `Thought_Situational` override (research.md R5).

| Field (runtime, on the comp) | Type | Persistence | Notes |
|---|---|---|---|
| `tierState` | `TattooTierProgress` | `ExposeData()` from `CompExposeData()` | `tier` is `1` or `2`; `progressionCounter` counts resisted risk windows (research.md R4) |
| `inRiskWindow` | bool | `Scribe_Values`, default `false` | Whether a risk window is currently open (research.md R4) |
| `riskWindowHadBreak` | bool | `Scribe_Values`, default `false` | Whether `pawn.InMentalState` was observed true at any point during the current window |
| `nextRiskCheckTick` | int | `Scribe_Values`, default `0` | Self-gated interval, mirrors `HediffComp_VampiricThornEffect.nextPostKillHealTick`'s existing pattern |

**Behavior**:
- `GetStatOffset(StatDef stat)`:
  - `stat == StatDefOf.MentalBreakThreshold` → tier-appropriate `mentalBreakResistance` value (negative; FR-001).
  - `stat == StatDefOf.PsychicSensitivity` → tier-appropriate `psychicSensitivityResistance` value (negative;
    FR-002).
  - Any other stat → `0f` (contract requirement, features 002/011 precedent).
- `CompPostTick(ref float severityAdjustment)`:
  1. `base.CompPostTick(...)`.
  2. If `Pawn == null || Pawn.Dead`, return.
  3. Self-gate: if `Find.TickManager.TicksGame < nextRiskCheckTick`, return; otherwise set
     `nextRiskCheckTick = Find.TickManager.TicksGame + CheckIntervalTicks` (a small constant, ~60 ticks/1 real
     second — coarser than vanilla's own 150-tick check is unnecessary here since a risk window requires ≥2000
     ticks of sustained low mood to even open, research.md R4).
  4. Read `bool isImminentNow = breaker.BreakMinorIsImminent || breaker.BreakMajorIsImminent ||
     breaker.BreakExtremeIsImminent;` off `Pawn.mindState.mentalBreaker`.
  5. If `isImminentNow && !inRiskWindow`: a new window opens — `inRiskWindow = true; riskWindowHadBreak = false;`.
  6. If `inRiskWindow && Pawn.InMentalState`: `riskWindowHadBreak = true`. **Checked unconditionally, not nested
     under `isImminentNow`** — every `Break*IsImminent` property requires `pawn.MentalStateDef == null`, so the
     instant a break actually starts, `isImminentNow` flips to `false` on this same check, the same tick
     `InMentalState` becomes `true`. Nesting this under `isImminentNow` would make it unreachable, since those two
     conditions are mutually exclusive by `MentalBreaker`'s own definition — a real bug caught during
     implementation testing (2026-08-22), not a hypothetical.
  7. If `!isImminentNow && inRiskWindow`: the window just closed. If `!riskWindowHadBreak`, call
     `tierState.TryRegisterQualifyingEvent(ScopeKey, "Tier2Threshold", Props.tier2Threshold)` (FR-003/FR-004).
     Either way, `inRiskWindow = false`.
  7. (Dev Mode logging on window-open/window-resisted/window-broken transitions, matching every prior tattoo's
     `TattooMagicSettings.EnableDebugLogging`-gated `Log.Message` convention.)
- `CompExposeData()`: `base.CompExposeData()`, then `tierState.ExposeData()` (FR-007), plus
  `Scribe_Values.Look` for `inRiskWindow`, `riskWindowHadBreak`, `nextRiskCheckTick`.

**Validation rules**:
- `tierState.tier` is never anything other than `1` or `2`; `tierState.progressionCounter` never decreases and
  continues incrementing harmlessly past the Tier 2 threshold (feature 002 precedent, unmodified).
- The comp only acts (FR-008) while attached to a live, applied `TattooMagic_Hediff_StarlightWard` — removing the
  hediff removes the comp with it (normal Hediff lifecycle, same as every prior tattoo); the existing
  `TattooHediffRemovalGuard`/`Patch_HealthTracker_RemoveHediff_ProtectTattoos` (feature 003 FR-015 precedent)
  already covers it via the generic `appliedHediff` registry, no Starlight-Ward-specific configuration needed.
- A window that had a break during it never registers a qualifying event, even once it eventually closes (FR-009
  applied to this tattoo's own event type: no genuine resistance occurred, so nothing counts).
- The full mood buff and stronger resistance values only ever apply once `tierState.tier >= 2` (FR-005/FR-006).

## `ThoughtDef` + `ThoughtWorker_StarlightWardTier2Active` + `Thought_StarlightWardTier2Active`

New, small, self-contained pair — not a shared contract (research.md R5). No `contracts/` directory is added for
this feature, mirroring feature 012's own precedent of skipping it when nothing new and generic is introduced for
other tattoos to consume.

```csharp
public class ThoughtWorker_StarlightWardTier2Active : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        var comp = FindComp(p); // scans p.health.hediffSet for a HediffComp_StarlightWardEffect, mirrors
                                 // StatPart_TattooEffectOffset's own by-type/interface pawn-comp scan
        return (comp != null && comp.Tier >= 2) ? ThoughtState.ActiveDefault : ThoughtState.Inactive;
    }
}

public class Thought_StarlightWardTier2Active : Thought_Situational
{
    public override float MoodOffset()
    {
        var props = (HediffCompProperties_StarlightWardEffect)def.hediffDefCompProps; // or a direct XML default
                                                                                        // fallback constant
        return TattooEffectValues.Get("TattooMagic_Hediff_StarlightWard", "MoodBuffTier2", <xmlDefault>);
    }
}
```

(Exact fallback-default plumbing is an implementation detail for the tasks phase — either the `ThoughtDef`'s own
`stages[0].baseMoodEffect`, resolved via `def.stages[0].baseMoodEffect`, or a constant mirrored from the
`HediffCompProperties_StarlightWardEffect` default; both satisfy FR-011's "XML value is the fallback default"
shape identically to every other tattoo's numeric field.)

**Behavior**: Automatically evaluated for every pawn each mood recompute via vanilla's own
`SituationalThoughtHandler`/`ThoughtUtility.situationalNonSocialThoughtDefs` (research.md R5) — no per-pawn
registration or dispatch code needed anywhere in this mod.

**Validation rules**:
- Active if and only if the wearer has `TattooMagic_Hediff_StarlightWard` at Tier 2 (FR-005); inactive at Tier 1
  (FR-006) or with no Starlight Ward tattoo applied at all (FR-008).
- Removing the hediff (via the sanctioned path or Dev Mode) makes the thought inactive on the very next mood
  recompute — no lingering effect (FR-013), since `CurrentStateInternal` re-derives its answer from the pawn's
  live hediff/comp state every time, holding no cached tier of its own.

### XML: `Defs/HediffDefs/Tattoos/StarlightWard.xml` (updated)

`hediffClass` stays `HediffWithComps`; adds a `<comps>` entry, replacing the current "effect not yet implemented"
description text:

```xml
<hediffClass>HediffWithComps</hediffClass>
...
<!-- All numeric values below are placeholders pending the PRD §9 balance pass. They are read
     exclusively through TattooEffectValues at point of use (FR-011), small/fast-to-test rather
     than "plausible-looking" per feature 006 research.md R8 precedent. -->
<comps>
  <li Class="TattooMagic.HediffCompProperties_StarlightWardEffect">
    <mentalBreakResistanceTier1>-0.03</mentalBreakResistanceTier1>
    <mentalBreakResistanceTier2>-0.06</mentalBreakResistanceTier2>
    <psychicSensitivityResistanceTier1>-0.10</psychicSensitivityResistanceTier1>
    <psychicSensitivityResistanceTier2>-0.20</psychicSensitivityResistanceTier2>
    <moodBuffTier2>3</moodBuffTier2>
    <tier2Threshold>5</tier2Threshold>
  </li>
</comps>
```

### XML: new `Defs/ThoughtDefs/StarlightWard.xml`

```xml
<ThoughtDef>
  <defName>TattooMagic_Thought_StarlightWardTier2</defName>
  <thoughtClass>TattooMagic.Thought_StarlightWardTier2Active</thoughtClass>
  <workerClass>TattooMagic.ThoughtWorker_StarlightWardTier2Active</workerClass>
  <stages>
    <li>
      <label>starlight's calm</label>
      <description>My Starlight Ward tattoo has grown strong enough to fill me with a quiet, steady calm.</description>
      <baseMoodEffect>3</baseMoodEffect>
    </li>
  </stages>
</ThoughtDef>
```

All values placeholders pending PRD §9's balance pass (Constitution Principle III) — carried by the XML comment
above and by `baseMoodEffect` living in the `ThoughtDef` itself, matching the top-of-file comment every previously
shipped tattoo's `HediffDef` XML carries without exception.

## Persistence summary (Constitution Principle V)

- `HediffComp_StarlightWardEffect.tierState` (`tier`, `progressionCounter`) — `Scribe_Values`, on the Starlight
  Ward hediff, saved/loaded automatically as part of the pawn's health state, identical mechanism to every prior
  tiered tattoo.
- `HediffComp_StarlightWardEffect.inRiskWindow` / `riskWindowHadBreak` / `nextRiskCheckTick` — also
  `Scribe_Values`, on the same hediff. Deterministic and idempotent across a save/reload mid-window: a window
  that was open when the game was saved simply resumes being tracked as open, re-evaluated fresh against the
  pawn's actual mood/mental-state on the next check after load, exactly as if no save/load had happened
  (Constitution Principle V).
- `Thought_StarlightWardTier2Active` holds no state of its own to persist — it re-derives its active/inactive
  status from the comp's live `Tier` on every mood recompute, the same as every vanilla situational thought.
- `TattooEffectValues`'s override dictionary — still not persisted by this feature either, same as every prior
  feature.
