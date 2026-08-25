# Test Script: Guardian's Call — Step-by-Step Manual Verification

Companion to [quickstart.md](./quickstart.md) — same 6 scenarios, but written as a literal click-by-click
script instead of "Expected" prose, with the exact strings/defNames/log lines this implementation actually
uses so you don't have to go hunting through source while playing. Check off each `[ ]` as you go; this file
mirrors `tasks.md` T008/T009/T011/T014/T017, which is what "done" is still waiting on.

**Exact Dev Mode button wording can drift slightly between RimWorld point releases — if a label below doesn't
match exactly, look for the closest equivalent (usually one tab over).**

---

## 0. Setup (once)

1. [ ] `dotnet build` from `Source/` — confirm `Assemblies/TattooMagic.dll` is fresh (already verified clean by
   the implementation session, but re-confirm after any local changes).
2. [ ] Launch RimWorld → Mod settings → confirm TattooMagic is enabled (and Combat Extended, for Scenario 5 —
   leave CE **disabled** for Scenarios 1–4/6, enable it only for Scenario 5).
3. [ ] Load or start a colony save with at least one colonist and open ground nearby to spawn hostiles on.
4. [ ] Escape menu → Options → turn on **Dev mode** (bottom of the list). A dev toolbar appears at the top of
   the screen and a row of dev tool buttons appears along the bottom.
5. [ ] Leave the dev console open (there's a "Console"/log toggle in the dev toolbar, or press the key you
   have bound to it) — you'll be watching for `[TattooMagic]` lines throughout.

### Fast-iteration trick (recommended)

This tattoo's placeholder numbers are real ticks, not instant:

| Value | Ticks | Real time @ 1x speed | Real time @ 3x speed |
|---|---|---|---|
| Tier 1/2 taunt duration | 300 | 5s | ~1.7s |
| Tier 1/2 cooldown | 3600 | 60s | ~20s |
| Tier 2 threshold | 15 activations | up to 15 min | up to 5 min |

Waiting out 15 real cooldowns for Scenario 4 is slow. Instead: select the tattooed pawn with Dev Mode on, open
the **Inspect** panel (bottom-right dev panel that appears when a pawn is selected in Dev Mode) and drill into
the pawn → `health` → `hediffSet` → the Guardian's Call hediff → its `HediffComp_GuardiansCallEffect` → you'll
see live, editable fields: `tierState.progressionCounter`, `tierState.tier`, `cooldownEndTick`, `tauntEndTick`.
Click a numeric value to type a new one directly (e.g. set `progressionCounter` to `14` before your next
activation to trigger the Tier 2 upgrade on that click, or zero out `cooldownEndTick` to skip a cooldown wait).
This edits the live object, no rebuild needed.

### Applying the hediff without the ritual station

Health tab (of the selected pawn) → in Dev Mode a **"+ Add hediff"** control appears near the hediff list →
search `guardian's call` or the defName `TattooMagic_Hediff_GuardiansCall` → no body part selection needed
(it's a whole-pawn hediff, same as Frost Sigil/Serpent's Eye) → Add.

---

## Scenario 1 — Gizmo, activation, cooldown (non-CE)

1. [ ] Confirm Combat Extended is **disabled**.
2. [ ] Apply Guardian's Call to a colonist (ritual station, or the Dev Mode Add-Hediff trick above).
3. [ ] Select that colonist. **Check**: a gizmo labeled exactly **"Guardian's Call"** appears in the command
   row, enabled (not greyed out).
4. [ ] Use the Dev Mode **"Spawn pawn"** tool (bottom dev toolbar, person-with-plus icon) to place 2–3 hostile
   pawns (any raider/pirate kind) within ~15 tiles of the colonist, but don't let them engage yet (pause the
   game with Space if needed).
5. [ ] Click the "Guardian's Call" gizmo.
   - [ ] **Check dev console**: a line like
     `[TattooMagic] Guardian's Call activated by <Name>: tier=1, range=15, duration=300, cooldown=3600, progress=1.`
   - [ ] **Check gizmo**: immediately shows disabled (greyed), tooltip/label reads
     `Guardian's Call is recharging: 60s remaining.` (or close to it — counts down).
   - [ ] Unpause. **Check**: the spawned hostiles' current/next target selection redirects to the tattooed
     colonist (watch their movement/attack lines, or check their current job/target via the Inspect panel).
6. [ ] While still on cooldown, click the gizmo again. **Check**: nothing happens — no new activation log line,
   no error in the dev console (the gizmo is disabled, so it shouldn't even register a click).
7. [ ] Wait ~60s real time (1x speed) or use the Inspect-panel trick to zero `cooldownEndTick`. **Check**: the
   gizmo becomes enabled again (no more disabled greyscale/tooltip).

## Scenario 2 — Taunt expiry and live re-evaluation

1. [ ] With hostiles nearby (as Scenario 1), activate the gizmo and let the full 5s (300-tick) taunt duration
   elapse without downing/killing the colonist.
2. [ ] **Check**: after the 5s window, hostiles' next targeting decision no longer favors the previously
   taunted colonist (no more `taunt redirected` log lines mentioning them).
3. [ ] Repeat: activate the gizmo, and **immediately** (within the 5s window) spawn one *new* hostile pawn
   within taunt range using the Dev Mode Spawn tool.
4. [ ] **Check dev console**: a `[TattooMagic] Guardian's Call taunt postfix evaluating searcher=<new pawn>...`
   line appears for the new arrival, followed by a `taunt redirected` line naming it — confirming the taunt is
   a live standing check, not a one-time snapshot only applied to hostiles present at activation.

## Scenario 3 — Taunt ends immediately on downed/killed/hediff-removed

1. [ ] Activate Guardian's Call with hostiles nearby.
2. [ ] While the taunt is still active (within the 5s window — pause the game right after activating to give
   yourself time), do **one** of:
   - Down the tattooed pawn (Dev Mode → right-click pawn → damage/down tools), or
   - Kill the tattooed pawn, or
   - Enable **God Mode** (dev toolbar checkbox) → open the pawn's Health tab → remove the Guardian's Call
     hediff directly.
3. [ ] Unpause. **Check**: hostiles' next targeting decision no longer favors that pawn — no lingering
   `taunt redirected` lines naming them, and no red error in the dev console.

## Scenario 4 — Progression counter, Tier 2 upgrade, armor buff (non-CE)

1. [ ] Guardian's Call at Tier 1 (fresh tattoo, `progressionCounter = 0`).
2. [ ] Activate repeatedly until `tierState.progressionCounter` reaches `15` (use the Inspect-panel trick to
   set `cooldownEndTick` to `0` before each click instead of waiting out real cooldowns — much faster than 15
   real minutes).
   - [ ] **Check**: each activation's log line shows `progress=` incrementing by exactly 1 per click, never
     more (even though the taunt may redirect multiple hostiles at once).
3. [ ] On the activation where `progress` reaches `15`: **Check** the log line now shows `tier=2` — the
   upgrade happened in place, no new ritual/ingredient/slot prompt appeared.
4. [ ] Before activating again, open the colonist's **Stats tab** → find **Armor - Sharp** / **Armor - Blunt**
   / **Armor - Heat**. **Check**: values match their normal (non-buffed) baseline — no Guardian's Call line in
   the "Tattoo effects" explanation yet.
5. [ ] Activate the ability once more (now at Tier 2). **Check**: those same three armor stats now read
   measurably higher, and hovering shows a "Tattoo effects" explanation line contributing `+0.3` (the
   placeholder `armorOffsetTier2`) — while the taunt itself still fires exactly as at Tier 1.
6. [ ] Wait out (or fast-forward past) the 5s taunt/buff window. **Check**: re-open the Stats tab — armor
   values return to baseline, "Tattoo effects" line for Guardian's Call is gone or zeroed.

## Scenario 5 — Combat Extended path (**required**, not optional)

1. [ ] Enable Combat Extended in mod settings, restart if required, reload the save.
2. [ ] Apply Guardian's Call to a colonist (or re-use one already tattooed).
3. [ ] Spawn a CE-equipped **ranged** hostile via the Dev Mode Spawn tool within taunt range (CE auto-equips
   ammo-compatible gear on normal hostile pawn kinds when CE is active — no special CE-only dev step needed).
4. [ ] Activate Guardian's Call.
5. [ ] **Check dev console** — this is the actual verification, don't skip it:
   - [ ] Does a `[TattooMagic] Guardian's Call taunt postfix evaluating searcher=<CE pawn> (CE loaded=True)`
     line appear for the CE-controlled hostile? If **no**, that alone is Outcome B (see below) — stop and
     report it.
   - [ ] If yes, does a `taunt redirected` line follow, and does the hostile's actual behavior redirect to the
     colonist exactly like Scenario 1? If yes → **Outcome A confirmed**, nothing further needed for this
     scenario type.
6. [ ] Repeat step 3–5 with a **melee** CE-equipped hostile (separately from ranged) — Constitution Principle
   IV requires both cases verified, not just one.
7. [ ] **If Outcome B** (no postfix log line for CE pawns, or logged-but-not-actually-redirected): this means
   `Patch_CE_AttackTargetFinder_GuardiansCallTaunt` (tasks.md T010) needs to be implemented — this was
   deliberately left unbuilt pending this exact check. Come back and ask for T010 to be implemented, following
   `contracts/two-path-targeting-contract.md` §2, then repeat this whole scenario until it passes.
8. [ ] **Check**: zero red Harmony/mod errors in the dev console throughout, both ranged and melee cases.

## Scenario 6 — No effect without the tattoo; removal guard; save/reload persistence

1. [ ] Select an untattooed colonist (including one with Frost Sigil/Serpent's Eye but not Guardian's Call).
   **Check**: no "Guardian's Call" gizmo appears, and they're never named in a `taunt redirected` log line.
2. [ ] With a Guardian's Call-tattooed pawn at a known tier/progress (e.g. tier 2, progress 17), save the game,
   then reload. **Check**: `tierState.tier` and `progressionCounter` (via the Inspect panel) are unchanged
   immediately after reload vs. immediately before saving.
3. [ ] With Dev Mode on and **God Mode off**, open the tattooed pawn's Health tab. **Check**: the Guardian's
   Call hediff row has no delete/remove control (matches vanilla, reuses feature 003's existing guard).
4. [ ] Enable **God Mode**, remove the hediff via the Health tab. **Check**: removal succeeds immediately, the
   gizmo disappears, and any currently-active taunt/armor buff stops immediately (no lingering redirect on the
   next targeting decision).
5. [ ] Re-apply the tattoo, disable God Mode. Attempt to remove it via a direct
   `pawn.health.RemoveHediff(...)` call if you have a throwaway debug action bound for this (or skip if none is
   available — feature 003's guard is unmodified and already covered by that feature's own test pass). **Check
   if attempted**: the hediff remains, no error logged.

---

## Final sign-off

- [ ] All of Scenarios 1–4 and 6 pass with zero red Harmony/mod errors.
- [ ] Scenario 5 passes with one of its two outcomes confirmed **empirically** (not assumed) — Outcome A logged
  and observed, or Outcome B triggered T010's implementation and then passed on re-test.
- [ ] Re-run all six scenarios back-to-back once in a single session (tasks.md T017) to catch any interaction
  effects, confirming zero red errors throughout.
- [ ] Remove or re-confirm the `Prefs.DevMode`-gated `Log.Message` probes in
  `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt` are acceptable to ship as-is (tasks.md T018 —
  they're already gated, so this is "confirm gating is sufficient" rather than "delete them").
- [ ] Final `dotnet build` — 0 errors, 0 warnings.

Once every box above is checked, tasks.md's T008/T009/T011/T014/T017/T018 can all be marked `[X]` and this
feature is done.
