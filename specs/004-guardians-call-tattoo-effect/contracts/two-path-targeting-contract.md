# Contract: Two-Path AI-Targeting Convention (Vanilla + Combat Extended)

Documents the convention Guardian's Call establishes for Constitution Principle IV ("any mechanic that
intercepts vanilla targeting or combat-decision logic ... MUST ship a distinct Harmony patch for vanilla's
code path ... and a separate patch for CE's equivalent"). Guardian's Call is the first and only tattoo in this
slice that touches targeting AI (spec Key Entities: "Two-Path AI-Targeting Convention"); this document is what
a future targeting-related tattoo (User Story 3) follows if it ever needs to influence hostile targeting again.

## 1. The vanilla path: `Patch_AttackTargetFinder_BestAttackTarget_GuardiansCallTaunt`

- **Patch target**: `Verse.AI.AttackTargetFinder.BestAttackTarget` (public static method; confirmed signature
  in research.md R4). Discovered and applied via normal `[HarmonyPatch]` attribute + `PatchAll()`, since it's a
  compile-time-referenceable vanilla type — no reflection needed for this half.
- **Shape**: Postfix, never Prefix — vanilla's own algorithm always runs to completion first; the postfix only
  ever *replaces* `__result` with a taunter, and only when that taunter independently passes the same
  `validator`/reachability bar vanilla's own search would require. It is never overridden to an *unreachable
  or invalid* target — a taunter that fails those checks this call simply leaves vanilla's own pick in place.
- **Guarantee**: A hostile pawn (melee or ranged) evaluating its target while within an active Guardian's
  Call taunt's range is redirected to the taunting pawn, provided that pawn is still a legal target under the
  search's own parameters; a hostile is never left without a target because of this patch (FR-001, FR-003,
  Edge Cases).
- **What is NOT guaranteed**: which taunter is chosen when two are simultaneously active and both are legal
  (nearest-to-searcher is the tie-break, not a documented/relied-upon game rule per spec Edge Cases); gizmo
  ordering or any other UI concern (unrelated to this patch).

## 2. The Combat Extended path

Per research.md R5, the exact mechanics of "CE's equivalent" are an implementation-time verification target,
not a documented certainty — CE has historically patched this same vanilla method rather than replacing it
outright, which may mean §1's postfix already governs CE-controlled hostiles. **Both of the following outcomes
are valid, fully-compliant implementations of this contract; which one ships is decided by the verification
step, not by this document:**

- **Outcome A — §1 already covers CE**: confirmed via a CE-loaded quickstart pass (with temporary
  `Log.Message` probes in the §1 postfix) that CE-controlled hostiles are correctly redirected. No second
  patch is added; the CE-loaded verification pass itself is the second, independently-run verification
  Constitution Principle IV requires ("both MUST be verified together when CE is loaded").
- **Outcome B — CE bypasses `BestAttackTarget`**: a second patch,
  `Patch_CE_AttackTargetFinder_GuardiansCallTaunt`, is added following the exact convention
  `Patch_CE_ProjectileImpact_TattooAmmoBonus` (feature 003) already established:
  1. Resolve the real CE type/method via `AccessTools.TypeByName`/`AccessTools.Method` at runtime, never a
     compile-time reference.
  2. Gate entirely behind `CombatExtendedInterop.IsLoaded` — no resolution attempt when CE is absent.
  3. Apply imperatively (`TryApply(Harmony harmony)`) from `TattooMagicMain`'s static constructor, alongside
     the existing CE patch registration, since `PatchAll()`'s attribute scan can't discover it.
  4. Consult the same `GuardiansCallTauntRegistry` (data-model.md) §1's postfix does — no duplicated taunt-
     state tracking.

**Either outcome MUST**: neither throw nor silently no-op in a CE-loaded game (FR-013); be confirmed by running
the mod with Harmony + CE loaded, not assumed from source reading alone (Constitution Principle II).

## 3. What a future targeting-related tattoo reuses from this

- The **pattern** (Postfix-after-vanilla-algorithm rather than a search-restricting Prefix; verify-then-only-
  add-a-second-patch-if-needed for CE) — not a shared reusable class, since each targeting-influencing effect's
  bias logic is inherently effect-specific.
- `CombatExtendedInterop.IsLoaded` for CE detection (feature 003, unmodified) and the reflection-patch
  convention from `passive-tattoo-effect-contract.md` §3 (feature 003) for however its own CE path resolves.
- **What is explicitly NOT reusable**: `GuardiansCallTauntRegistry`, the taunt-range/duration semantics, and
  the nearest-taunter tie-break are Guardian's-Call-specific — a future targeting tattoo with different bias
  semantics (e.g. a "flee from" effect instead of "attack") implements its own registry/patch pair against
  this same convention rather than extending Guardian's Call's.
