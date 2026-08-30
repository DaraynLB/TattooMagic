# Quickstart: Validating the Phoenix Tattoo Effect

Manual, in-game validation steps satisfying Constitution Principle II ("Verify Before Claiming Done"). Run
these after implementation, before marking this feature complete. No automated test suite exists for this
feature (no automated `Verse`/`RimWorld` test harness across features 001–013 either).

**Testing-methodology note**: this feature's core timers are genuinely **days long** (5/3-day waits, daily
retries, a days-long Tier-2 progression clock) and its cap/faction mechanics span multiple colonists and
faction-change events — none of that is practical to wait out for real. `Source/Debug/
DebugAction_TestPhoenixRevival.cs` adds several permanent Dev Mode tools under the "TattooMagic" category to
make every scenario below deterministic and fast, mirroring feature 013's own precedent of building dedicated
debug tooling for a mechanic too slow/organic to reliably hand-test:

- **"TEST: Phoenix — kill pawn now"** — kills the targeted pawn via a real death (not downing), leaving an
  intact corpse, so the normal `GameComponent_PhoenixRegistry` sweep picks it up exactly as it would in real
  play.
- **"TEST: Phoenix — force registry sweep now"** — runs `GameComponent_PhoenixRegistry`'s sweep logic
  immediately rather than waiting for its own ~2,000-tick self-gate, so a state change (a death, a cremation)
  is reflected without a real-time wait.
- **"TEST: Phoenix — set corpse rot progress"** — directly sets the targeted corpse's `CompRottable
  .RotProgress` to an entered number of days, to test the decomposition-chance curve at chosen points
  (freshly-dead, mid-`Rotting`, on the edge of `Dessicated`) without waiting for real decomposition.
  Restores nothing afterward — rot only ever needs to move forward for this feature's own scenarios.
- **"TEST: Phoenix — force pending attempt to resolve now"** — sets the targeted pawn's `reviveAttemptTick`
  to the current tick and immediately runs the sweep, so a scheduled roll (natural or cremation-guaranteed)
  resolves instantly instead of after real days pass. Reports the roll's outcome to the debug log.
- **"TEST: Phoenix — set days-alive counter"** — directly sets `daysAliveCounter` on the targeted pawn's
  comp to an entered value (re-checking the Tier-2 threshold immediately), to reach Tier 2 without waiting
  out the full placeholder day count for real.
- **"TEST: Phoenix — set lifetime attempt counter"** — directly sets `revivalAttemptCount` (and, if desired,
  `abasiaOccurrenceCount`) on the targeted pawn's comp, to jump straight to "this pawn's next successful
  revival is their 3rd+ lifetime attempt" without cycling through two real revivals first.

Also confirm **Options → Dev Mode** is enabled for every scenario below, and that God Mode is available where
noted (removal-guard bypass, direct hediff grants for fast setup).

## Prerequisites

- Mod built (`dotnet build` from `Source/`) with output present in `Assemblies/`, per the `rimworld-modding`
  toolchain.
- Mod enabled in RimWorld 1.6. Dev Mode enabled; God Mode available where noted.
- Feature 001 already built and working (the ritual station) — use it to apply Phoenix normally for
  Scenario 1, or use Dev Mode's "Add hediff" tool to grant `TattooMagic_Hediff_Phoenix` directly to a test
  pawn for faster setup in other scenarios (either path exercises `HediffComp_PhoenixEffect.CompPostMake`'s
  registration, data-model.md).
- At least 4 free colonists on one map for Scenario 2 (testing the 3-tattoo cap needs a 4th to be denied).
- A crematorium (`ElectricCrematorium`) built and powered, for Scenario 3.
- A way to inspect a pawn's Phoenix tier/day-counter/lifetime-attempt-counter and a corpse's rot stage (Dev
  Mode inspect panel / the health tab).

## Scenario 1 — Natural revival at both tiers, with daily retry (User Story 1 / SC-001)

1. Apply Phoenix to a colonist (ritual station or Dev Mode "Add hediff"). Confirm they start at Tier 1.
2. Click **"TEST: Phoenix — kill pawn now"**, then **"TEST: Phoenix — force registry sweep now"**.
   **Expected**: the debug log shows a wait scheduled ~5 in-game days out (Tier 1's `waitDaysTier1`
   placeholder), and the colonist's corpse remains intact and unburied.
3. Click **"TEST: Phoenix — set corpse rot progress"** and set it to a small value (a fresh corpse), then
   **"TEST: Phoenix — force pending attempt to resolve now"**. **Expected**: a burn injury (severe, Tier 1
   severity) appears on the corpse/pawn regardless of outcome; on success the colonist stands back up,
   spawned on the map, with `daysAliveCounter` reset to `0`; on failure, the debug log shows the wait
   rescheduled exactly one day later and the pawn is still dead.
4. If Step 3 failed, repeat **"force pending attempt to resolve now"** a few times. **Expected**: each
   failure logs a one-day-later reschedule, and — since decomposition (Step 3's pinned rot value) isn't
   advancing between forced attempts — the odds stay visibly consistent between tries (FR-013's "steady
   chance if decomposition is halted", here demonstrated by simply not advancing rot between forced rolls
   rather than by an actual freezer).
5. Repeat Steps 1–3 on a second colonist, but use **"set days-alive counter"** first (see Scenario 4) to put
   them at Tier 2 before killing them. **Expected**: the scheduled wait is ~3 days (`waitDaysTier2`), and any
   burn applied is the lighter Tier 2 severity.
6. Destroy a third tattooed, dead-and-pending colonist's corpse outright (Dev Mode "Destroy" on the corpse),
   or fully advance its rot to `Dessicated` via **"set corpse rot progress"** and force an attempt.
   **Expected**: revival becomes permanently impossible (the debug log reports a permanent-loss unregister,
   not a retry), matching FR-014 — confirm no further scheduled attempts occur even after further forced
   sweeps.

## Scenario 2 — The faction-wide 3-tattoo cap (User Story 2 / SC-002)

1. Apply Phoenix to three separate colonists (ritual station, one at a time). After the third, open
   `Dialog_ChooseTattoo` for a 4th colonist. **Expected**: Phoenix is not listed as an option (FR-006).
2. Kill one of the three tattooed colonists (as in Scenario 1) and leave them pending revival (do **not**
   force-resolve). Re-open the dialog for the 4th colonist. **Expected**: Phoenix is still absent — a
   pending-revival wearer still counts against the cap (FR-002).
3. Force that pending revival to succeed (Scenario 1, Steps 3–4). Re-check the dialog. **Expected**: still
   3/3 (the colonist is alive and tattooed again, still counting).
4. Permanently destroy one wearer's tattoo hediff (see Scenario 5's removal-guard bypass) or their corpse
   outright with no cremation save (Scenario 1, Step 6). Re-check the dialog. **Expected**: Phoenix becomes
   offerable again (FR-003) — a freed slot, not a permanently exhausted one.
5. With the faction back at 3/3, use Dev Mode to spawn or recruit a new pawn who already carries
   `TattooMagic_Hediff_Phoenix` (grant the hediff via Dev Mode before/at spawn, or grant it to a caravan
   visitor and recruit them). **Expected**: they're allowed to keep it as a temporary 4th (FR-005) — the
   dialog for every *other* colonist still shows Phoenix as unavailable (now 4/3, still over cap) until the
   count naturally drops back to 3 (repeat Step 4 once more to confirm this).
6. With a tattooed colonist alive and well, use Dev Mode to change their faction away from the player's
   (simulating capture/exile/defection — e.g. `Pawn.SetFaction(null)` via a debug tool, or actually banish
   them). **Expected**: the Phoenix hediff is stripped from them automatically at that moment (FR-004), and
   the faction count drops by one immediately (re-check the dialog for another colonist).

## Scenario 3 — Guaranteed revival via deliberate cremation (User Story 3 / SC-003)

1. Apply Phoenix to a colonist, kill them (Scenario 1, Step 2), and advance their corpse's rot deep into
   `Rotting` via **"set corpse rot progress"** (a value that would give a very low natural success chance).
2. Leave a piece of apparel on the corpse. Queue a "Cremate corpse" bill on the crematorium for this specific
   corpse and have a colonist haul it in and perform the bill (Dev Mode's "complete current job instantly" or
   equivalent can skip the real work-amount wait).
3. **Expected**: the corpse is destroyed by the bill; the debug log confirms `cremationGuaranteed` was set
   before destruction (research.md R4/R5) and the apparel that was left on it is gone (FR-023 — inherent
   vanilla cremation-bill behavior, not new code, research.md R5).
4. Confirm the wait timer was **not** shortened: the scheduled `reviveAttemptTick` is unchanged from what it
   was before cremation (still the tier's full wait from the original death, FR-022).
5. Use **"force pending attempt to resolve now"**. **Expected**: revival succeeds regardless of how decomposed
   the corpse was pre-cremation (FR-020), the colonist is spawned back onto the map at the corpse's last known
   location (the cremation fallback-spawn path, research.md R4 — confirm this specifically if the corpse's own
   map/position no longer exist to fall back on otherwise), a tier-appropriate burn is applied, and the
   attempt counted toward the Paralytic Abasia counter exactly like a natural revival (FR-024).
6. Separately, kill and cremate-guarantee a second Phoenix-tattooed corpse, then, **before** its wait period
   elapses, destroy it a *second* way (irrelevant once cremated, but confirms cremation isn't accidentally
   re-checking ordinary destroy conditions) — or more simply, confirm via debug log that a
   `cremationGuaranteed` pawn's forced attempt in Step 5 never runs the permanent-loss/destroyed check
   (research.md R6) the way a non-cremated corpse would.
7. As a negative control: kill a separate tattooed colonist, do **not** cremate them, and instead destroy
   their corpse by ordinary means (Dev Mode "Destroy", or set them on fire via some other source).
   **Expected**: this does **not** guarantee revival — it's ordinary permanent loss (FR-021, Scenario 1 Step
   6's outcome), confirming the distinction between deliberate-bill cremation and any other fire/destruction
   source.

## Scenario 4 — Tier 2 progression and Paralytic Abasia (User Story 1, Acceptance Scenarios 5–6)

1. Apply Phoenix to a colonist. Use **"set days-alive counter"** to set it to one below the
   `tier2DaysAliveThreshold` placeholder, then let one more in-game day pass naturally (or bump it by one
   more via the same tool). **Expected**: the tattoo upgrades to Tier 2 automatically — no ritual, no item —
   and `revivalAttemptCount`/`abasiaOccurrenceCount` both reset to `0` in the same moment (FR-019a), even if
   the colonist already had partial Abasia progress from prior revivals.
2. On a fresh Tier 1 colonist (not yet at Tier 2), use **"set lifetime attempt counter"** to set
   `revivalAttemptCount = 2`, then kill and force-resolve a successful revival (Scenario 1). **Expected**: this
   3rd lifetime attempt applies Paralytic Abasia at 2 days' duration (FR-017/FR-018's base duration) — visible
   as a new hediff with heavily reduced Moving/Manipulation/Consciousness on the revived colonist.
3. Repeat once more (kill, force-resolve success) without resetting the counter. **Expected**: a 4th-attempt
   Abasia application lasts 3 days (base + 1 increment, FR-018), confirming the escalation is per-*occurrence*
   of the debuff, not per attempt.
4. Force a **failed** attempt at a low decomposition-chance value while the colonist is past their 2nd
   lifetime attempt. **Expected**: `revivalAttemptCount` still advances (visible via the debug log/inspect
   panel), but no Abasia hediff appears on the still-dead corpse — the debuff only actually lands the moment a
   revival *succeeds* (FR-017's "banks silently").

## Scenario 5 — Independent passive bleed cauterization (User Story 4 / SC-005)

1. Apply Phoenix to a colonist. Inflict a real bleeding wound (a raid, an animal attack, or Dev Mode's damage
   tools) severe enough to bleed meaningfully — not a scratch.
2. Wait roughly a minute of real time (the ~1×/second self-gated check, research.md R8) with **Random mental
   states**/normal ticking active. **Expected**: over repeated trials (temporarily raising
   `cauterizeChanceTier1`/`Tier2` in the XML for one fast test pass is acceptable, mirroring feature 013's own
   precedent for testing a low-probability passive), the wound is sometimes automatically cauterized — bleeding
   stops and a burn injury appears on the same body part — with **zero** change to the colonist's own Phoenix
   `daysAliveCounter`, `reviveAttemptTick`, or any other revival-mechanic state (FR-025's "no shared
   cooldown").
3. Inflict a trivial (scratch-level) wound only. **Expected**: it's never eligible — confirm via debug log
   that the self-gated check finds no qualifying candidate for it (`BleedRate` under the tunable floor,
   FR-026).
4. Inflict bleeding wounds on two different body parts at once (differing severity). **Expected**: a
   successful proc targets and cauterizes only the more severe part's qualifying wound(s); the other part's
   bleed is untouched and still needs normal tending (FR-027/FR-028).
5. Inflict a bleeding severed/missing-limb wound alongside a separately bleeding, higher-`BleedRate` ordinary
   injury elsewhere. **Expected**: the severed-limb bleed is always the one targeted on a proc, regardless of
   the ordinary injury's higher severity (FR-027's absolute-priority rule).
6. Compare a Tier 1 and a Tier 2 wearer's cauterization burns on the same body-part severity of wound.
   **Expected**: the Tier 2 wearer procs measurably more often over repeated trials, but the burn severity
   left behind is identical between tiers (FR-029).

## Scenario 6 — Removal guard and the tattoo surviving death (FR-009, feature 003 precedent)

1. Apply Phoenix to a colonist. With Dev Mode enabled but **God Mode off**, confirm there is no direct
   delete/remove control for the Phoenix hediff on the health tab (same protection every prior tattoo already
   has, `TattooHediffRemovalGuard`, feature 003 — no Phoenix-specific configuration needed).
2. Kill the colonist and confirm the Phoenix hediff is still present and unchanged on their corpse's health
   tab throughout the wait — it is never consumed by a revival attempt, success or failure (FR-009). Force
   several failed attempts (Scenario 1) and confirm the hediff persists across all of them.
3. Enable **God Mode**, remove the Phoenix hediff via the health tab from a *living* wearer. **Expected**:
   removal succeeds, and — per FR-003 — this frees their faction-cap slot (re-check `Dialog_ChooseTattoo` for
   another colonist, mirroring Scenario 2 Step 4). This is the closest available stand-in for the not-yet-built
   removal ritual referenced by FR-003 (this spec's own Assumptions section scopes that ritual's
   implementation out of this feature — only its effect on cap accounting is in scope here).

## Scenario 7 — Persistence across save/reload (Constitution Principle V)

1. With a Phoenix-tattooed colonist mid-wait (dead, pending revival, a known `reviveAttemptTick` and
   `daysAliveCounter` value beforehand), save and reload. **Expected**: the pending revival resumes exactly
   as it was — no lost timer, no duplicate scheduling, no error — and forcing the sweep afterward behaves
   identically to before the reload.
2. Do the same with a `cremationGuaranteed` pawn mid-wait post-cremation, and again with a colonist sitting
   partway through Tier-2 progression (`daysAliveCounter` between `0` and the threshold). **Expected**: both
   resume correctly.
3. Save and reload with the faction at exactly 3/3 tattoos (including at least one pending-revival slot).
   **Expected**: `GameComponent_PhoenixRegistry.registeredWearers` still reports exactly 3 members
   post-reload, and `Dialog_ChooseTattoo` still correctly withholds Phoenix as an option.

## Scenario 8 — No regressions; zero errors with Combat Extended present (Constitution Principle I/II)

1. With Phoenix applied alongside a few other tattoos on the same colonist (e.g. Frost Sigil, Vampiric
   Thorn), confirm the other tattoos' own behaviors are unaffected by Phoenix's presence — a light re-check,
   not a full re-run of every prior feature's own quickstart.
2. Confirm the mod loads cleanly with zero red Harmony/mod errors both with Combat Extended absent and with
   it present (research.md R12 predicts zero CE-specific behavior difference anywhere in this feature). Run
   Scenario 5 (cauterization) once under CE specifically, since it's the one place this feature genuinely
   shares surface (`BleedRate`) with CE's own patching — confirm bleeding still stops correctly and no
   double-counted/garbled `BleedRate` reduction occurs.
3. Confirm the mod loads cleanly and Scenarios 1–7 behave identically with Royalty and/or Ideology **not**
   installed (nothing in this feature is DLC-gated).

## Pass/fail

All eight scenarios must match their "Expected" outcome with zero red Harmony/mod errors for this feature to
be considered verified, per Constitution Principle II. Phoenix is the 14th tattoo and the first to touch
death, corpses, or faction-wide state — Scenario 8's cross-tattoo check is this feature's own regression
contribution, not a full re-verification of every tattoo shipped so far.
