# TattooMagic

A RimWorld 1.6 / 1.7 mod that turns tattoos into a source of magic. Pawns apply
magical tattoos at a dedicated ritual station, gaining passive buffs and/or
gizmo-activated abilities. See [docs/PRD.md](docs/PRD.md) for full scope.

Currently implemented: all 11 tattoos from the PRD's starter roster, each
with a working Tier 1 effect that upgrades itself in place to a stronger
Tier 2 the more it's used — Frost Sigil, Serpent's Eye, Guardian's Call,
Stormlash, Ironskin Glyph, Ember Ward, Vampiric Thorn, Berserker's Mark,
Bloodrune, Wraithstep, and Starlight Ward — plus the tattoo ritual station
(recipe, Artistic-skill-scaled success/mishap, slot enforcement) and the
tattoo-mastery XP track that grows a pawn from 1 starting tattoo slot up to
3. The mod works whether or not Combat Extended is loaded.

Every tattoo's numeric balance (proc chances, tier-up thresholds, buff
amounts, ingredient costs, etc.) is still placeholder, pending a real
balance pass — see [docs/tattoo-tuning-guide.md](docs/tattoo-tuning-guide.md)
for a player-facing guide to hand-editing those values yourself.

## Requirements

- RimWorld 1.6 or 1.7
- [Harmony](https://steamcommunity.com/workshop/filedetails/?id=2009463077) (hard dependency)
- Combat Extended is a supported soft dependency — the mod works with or without it.

## Building

```sh
cd Source
dotnet build
```

One build produces two DLLs: `1.6/Assemblies/` (`net472`, for RimWorld 1.6's
Unity/Mono runtime) and `1.7/Assemblies/` (`net10.0`, for RimWorld 1.7's
Waylith engine). `LoadFolders.xml` makes the game load the shared root content
plus the matching version folder. Requires the .NET 10 SDK plus the .NET
Framework 4.7.2 Developer Pack.

## Installing

A `dotnet build` from `Source/` also publishes `About/`, `LoadFolders.xml`, `1.6/`, `1.7/` and the
shared content folders into your RimWorld `Mods/TattooMagic` folder automatically (see the
`PublishToModsFolder` target in `Source/TattooMagic.csproj`) — no manual
copy step needed for day-to-day development.

## Credits

Built with the help of Claude (Anthropic).
