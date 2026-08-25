# Phase 0 Research: Serpent's Eye Tattoo Effect (CE-Aware Slice)

Grounded against a real, installed Combat Extended copy (Steam Workshop item `2890901044`, `CETeam.CombatExtended`,
v16.7.3.0, RimWorld 1.6) rather than assumption: its shipped `Defs/Stats/*.xml` and `Assemblies/CombatExtended.dll`
(inspected read-only via `System.Reflection.MetadataLoadContext`, no game code executed) were checked directly to
ground every CE-specific decision below.

## R1: How does Serpent's Eye reuse feature 002's value accessor and tier tracker?

**Decision**: Identically to Frost Sigil — `HediffComp_SerpentsEyeEffect` composes a `TattooTierProgress` field,
calls `ExposeData()` on it from `CompExposeData()`, and reads every tunable value through
`TattooEffectValues.Get(ScopeKey, valueKey, xmlDefault)` with `ScopeKey = parent.def.defName`
(`TattooMagic_Hediff_SerpentsEye`). No change to either class.

**Rationale**: FR-012 requires reuse without modification; both pieces are already tattoo-agnostic per feature
002's `research.md` R5, and Serpent's Eye's needs (a tiered counter, override-ready values) are exactly what they
were built for.

**Alternatives considered**: n/a — this is a direct, unmodified reuse, not a design choice.

## R2: How does Serpent's Eye detect "the wearer's own ranged shot landed," and how is it dispatched?

**Decision**: A new interface, `IOnRangedHitLandedTattooEffect` (`void OnRangedHitLanded(Thing target, DamageInfo
dinfo, float damageDealt)`), dispatched from the *same* existing `Pawn.PostApplyDamage` postfix
(`Patch_Pawn_PostApplyDamage_TattooOnHit`) feature 002 built for melee — as a new, sibling `else` branch, not a
new patch on the same method. Where the existing branch fires on `dinfo.Tool != null` (melee) and notifies the
*struck* pawn's (`__instance`) comps, the new branch fires on `dinfo.Tool == null` (not melee) and notifies the
*instigating* pawn's (`dinfo.Instigator`) comps instead — the same underlying signal, inverted, dispatched to the
other side of the interaction.

Verified via reflection against `Verse.DamageInfo` (Assembly-CSharp.dll): `Tool` is a real settable field/property
populated only by melee verbs (confirmed by feature 002 R2's own in-game testing), so its absence is a reliable
"not melee" signal for any damage that both (a) actually landed (`totalDamageDealt > 0`, same guard as the
existing branch) and (b) has a `Pawn` instigator (excludes vanilla turrets, which are `Building`s, satisfying the
spec's "not turret-assisted fire unless the wearer is the one directly firing" carve-out for free). `DamageInfo`
was also checked for a more explicit "ranged" flag (`SourceCategory`) — its only two values
(`ThingOrUnknown`/`Collapse`) don't distinguish ranged from melee or explosions, so it isn't usable here; the
`Tool`-presence signal remains the best available one, consistent with feature 002's own finding.

**Rationale**: Reusing the same patch method avoids double-hooking `Pawn.PostApplyDamage` and keeps one
tattoo-agnostic file as the single dispatch point for "something hit something," exactly matching that file's own
stated intent ("Frost Sigil is the first consumer, not the only one"). This is additive to the file — the
existing melee `if` branch is untouched — so it satisfies FR-012's "without modification to feature 002's code"
for `HediffComp_FrostSigilEffect.cs` specifically (the file this constraint actually protects), while still
correctly treating the shared dispatch patch as reusable infrastructure meant to grow.

**Assumption requiring in-game confirmation (Principle II)**: Combat Extended's own ranged/ballistic pipeline is
assumed to still terminate in vanilla's `Thing.TakeDamage` → `Pawn.PostApplyDamage` for pawn targets, the same
assumption feature 002's R2 made for CE melee and flagged for confirmation rather than certainty. `quickstart.md`
requires a CE-loaded pass specifically exercising this branch.

**Alternatives considered**: A wholly separate new Harmony patch on `Pawn.PostApplyDamage` (rejected — would
double-postfix the same vanilla method for no benefit, since the ranged and melee signals are two branches of the
same `Tool` check); keying off `dinfo.Weapon != null` instead of `Tool == null` (rejected — `Weapon` is also
populated for some non-projectile damage sources like certain explosions, and doesn't reliably exclude melee on
its own the way `Tool`'s presence/absence does).

## R3: How does this feature detect Combat Extended's presence, and what convention does that establish?

**Decision**: A new `[StaticConstructorOnStartup] public static class CombatExtendedInterop` in
`Source/Effects/`, computed once at startup:

- `IsLoaded` — `ModsConfig.IsActive("CETeam.CombatExtended")`. `CETeam.CombatExtended` is confirmed as CE's actual
  `packageId` by reading the installed mod's own `About/About.xml` directly, not assumed from convention.
- `AimingAccuracy` — `DefDatabase<StatDef>.GetNamedSilentFail("AimingAccuracy")`. This resolves to a real
  `StatDef` instance when CE is loaded (CE ships it as plain XML — see R4), and `null` otherwise; no reference to
  `CombatExtended.dll` is needed for this lookup at all, since `StatDef` itself is a vanilla `RimWorld` type.

Any future tattoo needing to branch on CE, or to read a CE-only `StatDef` by name, follows this same
pattern — add the named `StatDef` (or `IsLoaded`-gated check) here rather than re-implementing detection per
tattoo.

**Rationale**: `ModsConfig.IsActive` with the confirmed real `packageId` is the standard, well-known convention
other CE-compatible mods use, cheap to compute once, and never throws regardless of CE's presence. Deriving
`AimingAccuracy` from `DefDatabase<StatDef>` rather than reflecting into `CombatExtended.dll` means the accuracy
and sway/recoil portions of this feature (R4) need zero assembly-level reflection at all — only the ammo-
effectiveness portion (R5) does, isolating the riskier reflection-based code to exactly where it's unavoidable.

**Alternatives considered**: `AppDomain`-wide assembly scanning for a `CombatExtended` assembly name (rejected —
`ModsConfig.IsActive` is simpler, is what CE's own `packageId` is designed for, and doesn't require iterating
loaded assemblies); resolving `AimingAccuracy` via CE assembly reflection (rejected — unnecessary, since
`StatDef.GetNamedSilentFail` already does this with zero assembly coupling).

## R4: What, specifically, does Serpent's Eye offset for its accuracy and sway/recoil bonuses?

**Decision**: `HediffComp_SerpentsEyeEffect.GetStatOffset(StatDef stat)` branches as follows:

- `stat == StatDefOf.ShootingAccuracyPawn`:
  - If `CombatExtendedInterop.IsLoaded` → return the tier's **sway/recoil** bonus.
  - Else → return the tier's **accuracy** bonus.
- `CombatExtendedInterop.IsLoaded && stat == CombatExtendedInterop.AimingAccuracy` → return the tier's
  **accuracy** bonus.
- Anything else → `0f`.

This is not an arbitrary split — it's forced by what Combat Extended itself actually does to the vanilla
`ShootingAccuracyPawn` StatDef, confirmed by reading CE's own `Patches/Core/Stats/Stats.xml`: CE **relabels**
`ShootingAccuracyPawn` to *"weapon handling"* and rewrites its description to: *"How well a shooter can hold a
gun steady when aiming and compensate for recoil. The total sway is calculated as: (4.5 - weapon handling) *
weapon sway factor. The recoil per shot is determined by multiplying this value against the weapon's inherent
recoil amount..."* — i.e. under CE, this stat **is** the pawn-side sway/recoil-reduction stat, not an accuracy
stat at all. CE's own `AimingAccuracy` StatDef (new, CE-only, confirmed via `Defs/Stats/Stats_Pawns_Combat.xml`)
is what actually drives shot accuracy (lead/range error) under CE, and its own description confirms it *also*
independently reduces sway on aimed shots ("at 75% aiming accuracy sway will be reduced to 25%") — but that
secondary effect is CE's own interaction, not something this feature needs to model separately.

`TattooEffectStatPartInstaller` is updated to register `StatPart_TattooEffectOffset` onto
`StatDefOf.ShootingAccuracyPawn` unconditionally (a plain vanilla `StatDef`, always safe to register on, exactly
like Frost Sigil's `ComfyTemperatureMin`/`MoveSpeed` registrations) and onto `CombatExtendedInterop.AimingAccuracy`
only when non-null (mirroring the installer's existing `Register()` null-guard, which already no-ops for a null
`StatDef` — no new guard logic needed).

**Rationale**: Offsetting `ShootingAccuracyPawn` for accuracy under vanilla and for sway/recoil under CE isn't a
workaround — it's the correct behavior given what that one stat *means* in each configuration. Naively offsetting
it for "accuracy" unconditionally in both configurations (the naive reading of FR-001) would silently hand CE
players a sway/recoil bonus mislabeled as accuracy, and give vanilla players nothing extra when CE is present
where FR-001 requires an accuracy bonus — exactly the kind of silent CE misbehavior Constitution Principle I and
this feature's FR-013 exist to catch. Branching on `CombatExtendedInterop.IsLoaded` for the same `StatDef` is
the concrete instance of the "CE-branching convention" User Story 3 asks this feature to establish.

**Alternatives considered**: Only ever offsetting `ShootingAccuracyPawn` and skipping `AimingAccuracy` entirely
under CE (rejected — would satisfy FR-002's sway/recoil requirement but leave FR-001's accuracy requirement unmet
under CE, since CE's real accuracy math runs through `AimingAccuracy`, not `ShootingAccuracyPawn`); offsetting
`AimingAccuracy` for sway/recoil instead of `ShootingAccuracyPawn` (rejected — `AimingAccuracy`'s sway reduction
is a documented *secondary* effect of an accuracy stat, not its primary purpose, and doesn't touch recoil at all
per its own description, whereas `ShootingAccuracyPawn`/weapon-handling explicitly drives both).

**Update from in-game testing (Scenario 4)**: Live testing surfaced something this research didn't predict:
under CE, the "Weapon Handling" stat's tooltip (`ShootingAccuracyPawn`) never shows a `Tattoo effects: ...`
explanation line the way `AimingAccuracy`'s tooltip does, even though the tattoo's Tier 2 bonus is
demonstrably still applied to the *number* (a Tier 2 pawn read `110.0%` against a computed ~`100.0%` no-bonus
baseline — exactly the `+0.1` `swayRecoilBonusTier2` offset, run through CE's near-identity `postProcessCurve`
segment in that range). Root cause, confirmed by inspecting `CombatExtended.dll` directly (via the same
`MetadataLoadContext` reflection approach used to ground R1-R5, not guessed): CE ships
`CombatExtended.HarmonyCE.Harmony_StatWorker_ShootingAccuracy`, a Harmony **prefix** on vanilla's own
`RimWorld.StatWorker_ShootingAccuracy.GetExplanationFinalizePart` — the specialized vanilla stat-worker
subclass that backs `ShootingAccuracyPawn` specifically (distinct from the plain generic `StatWorker` that
`AimingAccuracy`, a CE-defined `StatDef` with no specialized worker, uses). This prefix replaces the section
of the tooltip that would normally walk each `StatPart` and print its `ExplanationPart()` plus the
post-process-curve/final-value lines, substituting CE's own explanation text instead — it does **not** touch
`GetValueUnfinalized`/`TransformValue`, which is why the actual computed value still reflects our
`StatPart_TattooEffectOffset`'s contribution correctly. **Practical consequence**: `ShootingAccuracyPawn`'s
tooltip is not a reliable way to visually confirm this feature's sway/recoil bonus under CE — an A/B
comparison of the final `%` between an otherwise-identical tattooed and untattooed pawn is the only visual
confirmation available for that specific stat. This is a cosmetic quirk of CE's own tooltip implementation for
this one specialized stat worker, not a defect in this feature's `StatPart` registration or offset logic, and
nothing in this feature's own code should be changed in response to it.

## R5: How does Tier 2's CE-only "ammo effectiveness" bonus actually get applied, given CE has no stock stat for it?

**Decision**: Confirmed by inspecting `CombatExtended.dll`'s actual `AmmoDef`, `CompAmmoUser`, and
`ProjectilePropertiesCE` members — none of CE's shipped `StatDef`s (`Defs/Stats/Stats_Pawns_Combat.xml`,
`Stats_Weapons_Ranged.xml`) represent a generic, offsettable "ammo effectiveness" value; damage/penetration for a
given ammo type lives on shared, non-pawn-scoped `ThingDef`/`ProjectilePropertiesCE` data referenced by
`AmmoDef` — mutating it directly would leak the bonus to every pawn using that ammo type, not just the tattooed
wearer, and would persist the change outside any save/reload boundary.

Instead: `CombatExtended.ProjectileCE` (the class backing every CE bullet/projectile instance in flight) exposes
a per-*instance* settable `DamageAmount` property (backed by a private nullable field), confirmed via reflection.
A new, imperatively-applied (not `[HarmonyPatch]`-attributed, since `ProjectileCE` isn't a compile-time reference)
Harmony postfix on `ProjectileCE.Impact(Thing hitThing)` reads that specific projectile instance's `launcher`
field; if it resolves to a `Pawn` whose hediffs include a Tier 2 `HediffComp_SerpentsEyeEffect`, it multiplies
*that one instance's* `DamageAmount` by the tier's accessor-driven ammo-effectiveness multiplier before impact
resolves. Because this mutates one in-flight `Thing` instance rather than shared `AmmoDef`/`ThingDef` data, it
cannot leak to other pawns firing the same ammo, and leaves no state to persist (the projectile is destroyed on
impact regardless).

This patch is applied explicitly from `TattooMagicMain`'s static constructor, immediately after
`harmony.PatchAll()`, guarded by `if (CombatExtendedInterop.IsLoaded)` — `PatchAll()`'s attribute scan can't find
it since its target type only exists (and is only resolved via `AccessTools.TypeByName`) when CE is loaded.

**Rationale**: This is the only mechanism found, across CE's actual shipped Defs and assembly, that changes a
specific pawn's specific shot without corrupting shared data or requiring new per-shot persisted state — directly
satisfying FR-013's "must apply correctly when CE is loaded... must not silently corrupt other behavior."
Confirmed as genuinely CE-exclusive: `Verse.Projectile` (vanilla) has no ammo-type concept at all for this to
apply to, matching the spec's own Assumptions section.

**Assumption requiring in-game confirmation (Principle II)**: That `ProjectileCE.Impact(Thing)` is reliably
called for every CE ranged hit that damages a target (as opposed to some hits resolving through a different
method this research didn't surface) is a real risk given this was confirmed via static reflection, not live
behavior tracing. `quickstart.md`'s CE-loaded pass must specifically confirm the Tier 2 ammo bonus is
measurable, not just that the patch applies without error.

**Alternatives considered**: Directly mutating the wielded weapon's or loaded `AmmoDef`'s `ProjectilePropertiesCE`
fields for the duration the tattooed pawn is wielding it (rejected — shared, cached data; would either leak to
other pawns/instances using the same `ThingDef` or require constant save/restore bookkeeping around every shot,
both far riskier than a one-shot instance mutation); patching earlier in `CompAmmoUser`'s shot-preparation path to
swap in a boosted ammo definition (rejected — same shared-data leak risk, and `CompAmmoUser`'s ammo-swap surface
is more deeply tied to inventory/loadout state than a single projectile instance is).

## R6: Testing strategy

**Decision**: No automated test project, unchanged from features 001/002 (research.md R7 there). Verification is
manual, in Dev Mode, following `quickstart.md`, with mandatory passes in both a non-CE game and a CE-loaded game
— this feature is the first where the CE-loaded pass is load-bearing for the feature's own defining behavior
(FR-002, FR-006), not just a compatibility check on stats CE happens to leave alone (contrast feature 002's R6,
where no CE branching was needed at all).

**Rationale**: Unchanged reasoning from feature 002 R7, but with higher stakes here: Principle II's "done" bar
requires the CE-loaded pass to actually exercise the dual-meaning `ShootingAccuracyPawn` offset (R4) and the
reflection-based ammo patch (R5), both flagged above as assumptions this research could not fully verify
statically.

**Alternatives considered**: n/a — unchanged from feature 002.
