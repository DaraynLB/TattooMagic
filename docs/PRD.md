# TattooMagic — Product Requirements Document

## 1. Summary

TattooMagic is a RimWorld mod that turns tattoos into a source of magic. Pawns
apply magical tattoos at a dedicated ritual/crafting station, gaining passive
buffs and/or gizmo-activated abilities. The mod targets RimWorld **1.6**,
depends on **Harmony**, and must remain fully functional and balanced when
**Combat Extended (CE)** is loaded (https://steamcommunity.com/sharedfiles/filedetails/?id=2890901044).

## 2. Goals

- Ship a self-contained magic-tattoo system that feels distinct from vanilla
  Ideology tattoos and from existing psycast-style mods.
- Launch with **11 hand-designed tattoos** covering a mix of passive buffs and
  triggered (gizmo + cooldown) abilities.
- Give every tattoo its own **2-tier progression** (a base effect, then one
  in-place upgrade), so tattoos keep paying off the longer a pawn uses them
  rather than being "apply once and forget."
- Give tanky/frontline pawns a way to hold enemy attention (taunt), addressing
  the common problem where raiders ignore a colony's tank and focus down
  squishier pawns instead.
- Give pawns a sense of progression: they start with 1 tattoo slot and earn 2
  more through a dedicated XP track.
- Be fully compatible with Combat Extended — tattoo effects that touch combat
  stats must resolve correctly against CE's armor penetration, suppression,
  stamina, and ammo/loadout systems, not just vanilla stats.

## 3. Non-Goals (v1)

- No new magic "schools" or faction system — that's a possible v2 direction,
  not required for launch.
- No tattoo removal/respec system in v1 (open question, see §9).
- No custom tattoo textures/art pipeline beyond simple placeholder tattoo
  icons — visual polish is a fast-follow.
- No compatibility work for other magic mods (e.g. Vanilla Psycasts Expanded)
  beyond not conflicting with their def names/hediffs.

## 4. Target Platform & Dependencies

| Item | Decision |
|---|---|
| RimWorld version | 1.6 |
| Hard dependency | Harmony (via `Lib.Harmony` / standard modding toolchain) |
| Soft dependency | Combat Extended — must be detected at runtime and integrated when present; mod must also work correctly with CE **absent** |
| DLC dependency | None required; may reference Ideology's tattoo layer for visuals if present |

## 5. Core Mechanic

Tattoos are semi-permanent pawn modifications, conceptually similar to
bionics: applied via a ritual, occupying a limited number of slots, each
granting either a **passive buff** (always-on stat/hediff modifier) or a
**triggered ability** (gizmo with a cooldown, in the style of Vanilla
Psycasts).

### 5.1 Tattoo Slots & Progression

- Every pawn starts with **1 tattoo slot**.
- Pawns unlock **2 additional slots** (3 total) via a new **tattoo-mastery XP
  track** — a hidden per-pawn stat, distinct from vanilla skills, that gains
  XP when the pawn's active tattoo abilities are used (e.g. in combat).
- Two XP thresholds gate slot 2 and slot 3 respectively. Exact thresholds are
  a balance/tuning task, not a design blocker — see §9 open questions.
- A pawn cannot apply more tattoos than they have unlocked slots.

### 5.2 Applying a Tattoo (Ritual/Crafting Station)

- A new dedicated workbench (the "tattoo ritual station") is required to
  apply a magic tattoo to a pawn.
- Applying a tattoo consumes **ingredients** (consumable materials, akin to
  bionic surgery reagents) — exact recipe/cost per tattoo is a balance task.
- The pawn performing the ritual (not the recipient) must pass a **skill
  check**. Proposed skill: **Artistic** (thematic fit for tattoo work);
  confirm during implementation. Low skill should carry a mishap/failure
  chance rather than a hard block, consistent with RimWorld's surgery-style
  failure conventions.
- Applying a tattoo is a one-way action in v1 (no removal — see §9).

### 5.3 Triggered Abilities

- Triggered tattoos add an ability gizmo to the pawn's UI (Psycast-style),
  with its own cooldown.
- Cooldown is the only hard gate in v1 — no additional resource cost (e.g. no
  psyfocus-equivalent) unless balance testing shows it's needed.

### 5.4 Tiered Tattoos

- Every tattoo (all 11) has **2 tiers**: a base effect at Tier 1, and an
  in-place upgrade to Tier 2 once its progression condition is met. Tiering up
  does not consume an extra slot or require a second ritual — it happens
  automatically to a tattoo the pawn already has applied.
- Each tattoo tracks its own progression counter, separate from the pawn-wide
  tattoo-mastery XP track (§5.1) that gates slot unlocks. Two counter types,
  chosen per tattoo based on its type:
  - **Triggered tattoos** (Bloodrune, Stormlash, Wraithstep, Berserker's Mark,
    Guardian's Call): counter increments each time the pawn **activates** the
    ability. Reaching a use-count threshold (exact numbers TBD, see §9)
    promotes it to Tier 2.
  - **Passive tattoos** (Ember Ward, Frost Sigil, Ironskin Glyph, Serpent's
    Eye, Starlight Ward, Vampiric Thorn): counter increments on a
    **thematically-relevant combat event** while the tattoo is equipped and
    the effect is actually relevant (e.g. Ironskin Glyph counts hits absorbed,
    Vampiric Thorn counts melee hits landed, Serpent's Eye counts ranged hits
    landed) — see §6 for each tattoo's specific counter. This keeps "using the
    tattoo's theme in combat" as the upgrade path even without a gizmo to
    click, and avoids rewarding a passive tattoo just for being equipped and
    idle.
- Tier 2 is strictly better than Tier 1 (bigger numbers and/or an added minor
  effect) but must not trivialize the tattoo's original risk/reward — e.g.
  Berserker's Mark's Tier 2 should still carry a defense tradeoff, just a
  less punishing one.

## 6. Starter Tattoo Roster (v1 — 11 tattoos)

Proposed starting set, mixing passive and triggered effects across combat,
defense, and utility roles. All 11 are 2-tier per §5.4. **This list is a
draft for review/approval**, not final; tier thresholds are placeholders
pending balance testing (§9).

| # | Name | Type | Tier 1 (base) | Progression counter | Tier 2 (upgraded) |
|---|---|---|---|---|---|
| 1 | Ember Ward | Passive | + heat resistance; small reduction to fire/burn damage taken | Instances of fire/burn damage absorbed | Further burn damage reduction; chance to fully ignore a burn instance |
| 2 | Frost Sigil | Passive | + cold resistance; chance to slow a melee attacker on hit | Melee hits taken where the slow procs | Higher slow chance/magnitude; rare chance to briefly freeze the attacker |
| 3 | Bloodrune | Triggered | Short burst of lifesteal / self-heal over a few seconds | Times activated | Longer duration and/or stronger heal; also reduces pain slightly |
| 4 | Stormlash | Triggered | Temporary move speed + attack speed boost | Times activated | Bigger speed boost; brief immunity to movement-slow effects (incl. CE suppression slow) during the window |
| 5 | Ironskin Glyph | Passive | + armor rating (mapped to CE armor stats when CE is active) | Hits absorbed while equipped | Further armor increase; small chance to fully negate a hit |
| 6 | Serpent's Eye | Passive | + accuracy; reduced weapon sway/recoil (CE-aware) | Ranged hits landed | Further accuracy/sway reduction; bonus to CE ammo effectiveness |
| 7 | Wraithstep | Triggered | Short-range blink/teleport — escape/repositioning tool | Times activated | Longer blink range; brief untargetable window immediately after blinking |
| 8 | Berserker's Mark | Triggered | Temporary melee damage + pain threshold up, defense down | Times activated | Bigger melee damage boost; defense penalty reduced (better risk/reward, not removed) |
| 9 | Starlight Ward | Passive | + mental break resistance / psychic sensitivity resistance | Mental-break risk events resisted while equipped | Further resistance; small passive mood buff |
| 10 | Vampiric Thorn | Passive | Melee attacks lifesteal a small amount of HP | Melee hits landed | Higher lifesteal %; brief heal-over-time on a killing blow |
| 11 | Guardian's Call | Triggered | Forces nearby hostiles to prioritize attacking this pawn for a duration, then goes on cooldown | Times activated | Same taunt effect, plus a temporary HP/armor buffer ("shield wall") for the taunt's duration |

## 7. Combat Extended Integration

CE compatibility is a first-class requirement, not an afterthought:

- **CE combat stats**: passive/triggered effects that modify accuracy,
  armor, speed, etc. must resolve against CE's replacement StatDefs
  (armor penetration, recoil, suppression resistance, stamina) when CE is
  loaded, in addition to vanilla stats when it isn't.
- **CE ammo/loadout system**: at least one tattoo effect should meaningfully
  interact with CE's ammo types or loadouts (e.g. temporarily boosting
  effectiveness of the pawn's currently loaded ammo) — candidate: Serpent's
  Eye or a dedicated ammo-focused tattoo.
- Detection: mod must detect CE's presence at runtime (soft dependency,
  no hard reference) and branch behavior accordingly — must not throw or
  degrade when CE is absent.
- Testing must cover both configurations: vanilla combat stats only, and
  CE loaded.
- **Taunt targeting**: Guardian's Call requires patching hostile-pawn target
  selection so the tattooed pawn is prioritized. Vanilla and CE use different
  target-scanning code paths (CE has its own attack-target/AI overrides), so
  this needs two Harmony patch paths — one for vanilla `AttackTargetFinder`-
  style logic, one for CE's equivalent — verified to work independently and
  together.

## 8. Success Criteria

- Loads cleanly with Harmony only, and separately with Harmony + CE, with no
  errors in the dev console.
- All 11 tattoos are selectable at the ritual station, respect slot limits,
  and function (passive effects apply; triggered abilities fire and go on
  cooldown correctly).
- XP track correctly unlocks slot 2 and slot 3 at their thresholds.
- Every tattoo's Tier 1 → Tier 2 progression counter increments correctly on
  its designated trigger event and upgrades the tattoo's effect in place once
  its threshold is reached, with no slot or ritual required for the upgrade.
- Guardian's Call reliably redirects hostile AI targeting to the tattooed
  pawn for its duration, and correctly upgrades to Tier 1 → Tier 2 after its
  usage threshold, in both non-CE and CE-loaded games.
- CE-aware tattoos measurably affect CE stats (verified in-game with CE
  loaded), and don't error or no-op silently when CE is absent.

## 9. Open Questions

- Exact XP thresholds/curve for slot 2 and slot 3 — needs balance pass.
- Exact ingredient recipe and skill-check thresholds/failure consequences
  per tattoo — needs balance pass.
- Should tattoos ever be removable/respec-able? Deferred out of v1 scope but
  likely a fast-follow ask.
- Confirm "Artistic" as the applying-pawn skill vs. a new custom skill.
- Should triggered abilities carry a secondary resource cost beyond cooldown
  (e.g. HP cost, matching the "blood magic" tattoo theme) — flagged for
  balance testing, not a v1 blocker either way.
- Guardian's Call specifics needing balance numbers: taunt duration, cooldown,
  Tier 2 HP/armor buffer size, and the usage count required to upgrade to
  Tier 2.
- Per-tattoo Tier 1 → Tier 2 thresholds for the other 10 tattoos (use-counts
  for triggered, combat-event-counts for passive) — needs a full balance
  pass; §6 lists the counter type per tattoo but not the numbers.
- If tattoo removal/respec is ever added post-v1 (§3), decide whether a
  removed-then-reapplied tattoo keeps its progression counter or resets —
  not a v1 concern since removal is out of scope.
- Ingredient materials should be precious/luxury-tier resources (e.g.
  silver, gold, jade) rather than arbitrary crafting materials — feedback
  from playtesting, 2026-08-06. Current interim recipe (herbal medicine +
  silver) was chosen only to fix an "impossible to obtain" complaint about
  the original placeholder (neutroamine) and is not the final theme;
  revisit once the precious-materials list is decided, across all 11
  tattoos.
- Tattoo-mastery XP (§5.1) now accrues from passive tattoos too (every
  melee-hit-taken/ranged-hit-landed dispatch, feature 006 research.md R10,
  playtesting finding 2026-08-12), not gated by that tattoo's own internal
  proc chance the way its private tier-progression counter is — likely a
  much faster accrual rate in active combat than triggered-tattoo clicks
  (which are cooldown-gated). Needs a real balance pass alongside the
  slot-2/slot-3 threshold numbers themselves; not addressed by feature 006,
  which only needed passive tattoos to have *some* path to progress, not a
  tuned one.

## 10. Out of Scope / Future Considerations

- Additional tattoo schools/factions (per earlier brainstorm: fire/frost/
  blood-style schools) as a v2 expansion.
- Tattoo removal/respec.
- Custom art/texture pipeline for tattoo visuals.
- Compatibility shims for other magic mods (Vanilla Psycasts Expanded, etc.).
- Player-configurable tattoo balance values (ingredient recipes, the
  success-chance curve, work amount, etc.) via an in-game Mod Settings
  menu — feedback from playtesting, 2026-08-06. Not v1 scope. Note: per-
  tattoo ingredient recipes are already technically supported by the
  existing data model (each tattoo has its own editable XML ingredient
  list) — the actual gap is a player-facing in-game settings UI to adjust
  these without hand-editing XML/rebuilding the mod.
