# TattooMagic — Progress Update, October 21

## What we're building

Two things today. First, as you asked, **TattooMagic now supports RimWorld 1.7**. Then we finished **Soul Tether**, the second of the four tattoos you signed off on in September, and tested it in game from start to finish on 1.7.

## Moving to RimWorld 1.7

RimWorld 1.7 is a bigger change under the hood than most updates. Ludeon replaced the Unity engine with its own new engine, Waylith, which runs on a much newer version of .NET. Old mods mostly keep working through a compatibility layer, but we didn't want TattooMagic depending on that, so it now ships properly built for each version:

- **Both versions are supported.** 1.7 is still on Steam's beta branch, and most players are on 1.6. The mod now ships two builds, one for 1.6 and one for 1.7, and the game loads the right one automatically. Nobody on 1.6 loses the mod, and 1.7 players get a build made for 1.7.
- **No changes to the mod's own code were needed.** Every tattoo compiled for 1.7 as-is, and the mod loads with zero errors alongside Combat Extended and the rest of a full mod list.
- **When 1.7 becomes the main release,** we'll formally drop the 1.6 build. That's a small, planned cleanup.

Along the way we found and fixed a small, older issue: Bastion Ward's ground highlight caused a harmless yellow warning in the game's log at every startup. It's gone now.

## What we tested today

All of Soul Tether's testing happened on 1.7, with Combat Extended loaded.

**The pull.** Activating the ability lets you pick one friendly pawn within range: a downed colonist, an injured ally, even a colony animal. They're snatched to the caster's side with a quick flash where they stood, then appear next to the caster a split second later. The caster can't target themselves, and enemies or anyone out of range are clearly rejected with a short reason under the cursor.

**The tend buff.** A pulled pawn gets a short "soul-tethered" buff, visible in their health tab with a countdown. We measured it directly. While a doctor is tending that pawn, their tend speed jumps by exactly the intended amount (+50% at Tier 1), and every tend's quality gets the intended bonus. Once the buff runs out, both go back to normal: tends on that pawn are exactly as they'd be without the tattoo.

**Corpses.** Soul Tether can also bring back a fallen colonist's body, with no buff. Your own colonists' corpses can be pulled; enemy corpses can't.

**Cooldown and Tier 2.** The ability correctly refuses to fire while recharging and shows the seconds remaining. After 15 pulls it upgrades itself in place, with no new ritual and no extra cost. At Tier 2 the reach goes from 10 to 16 tiles and the buff doubles (+100% tend speed, twice the quality bonus). It still only ever moves the one pawn you picked, even with others standing right next to them.

**Everything else.** Pulling someone who's already next to you still works. A pawn in a mental break stays in it after the pull, and a bleeding pawn keeps bleeding (the pull fixes their position, not their health). Removing the tattoo takes the ability away but doesn't cancel a buff it already gave someone. Pulling an already-buffed pawn again resets their buff to full instead of stacking a second one. The caster's progress, cooldown and any buffs all survive saving and reloading. The mod's existing protection against other mods stripping tattoos covers Soul Tether automatically.

## Issues we found (and fixed)

- **Soul Tether could pull pawns that weren't really "friendly".** Enemies were correctly blocked, but neutral visitors and wandering pawns with no faction (and their corpses) could be pulled. The check only asked "is this pawn hostile?" rather than "is this one of ours, or an ally?". You confirmed the intended rule is your colony plus allied factions only. That's what it does now, and it was re-tested: neutral and factionless pawns are refused with "Not friendly.", while your own colonists still pull normally.

## Decisions you made today

- **A pull interrupts what the target was doing.** We found that pulling a pawn mid-task ends that task, because that's how every teleport works in RimWorld, including the game's own Skip psycast. A colonist working normally just picks up their next task; a drafted colonist waits for a new order. You chose to keep this standard behaviour rather than try to preserve the task, which risked pawns getting stuck. Mental states, injuries and bleeding are untouched either way.
- **One log message stays as it is.** On the exact pull that triggers the Tier 2 upgrade, a developer-only log line labels the pull "Tier 2" even though that pull still used Tier 1's numbers. It has no effect on gameplay, and you decided it wasn't worth changing.

## Where this leaves Soul Tether

The mechanics are done. Every part of the design has been confirmed working in game on RimWorld 1.7: targeting and its rules, the pull and its effect, the tend buff and its expiry, corpses, cooldown, the Tier 2 upgrade, single-target behaviour, every edge case we planned, saving and reloading, and Combat Extended. The mod builds with zero errors and zero warnings for both game versions.

What's left is balance, not mechanics. Like the rest of the roster, Soul Tether runs on placeholder numbers: range, cooldown, buff strength and duration, and the Tier 2 threshold. One thing worth deciding in that pass: the buff currently lasts exactly as long as the cooldown, so a single Soul Tether wearer can never top up a patient's buff before it runs out; only a second wearer can. If you'd like one caster to be able to keep a patient buffed, the buff needs to last a bit longer than the cooldown.

Soul Tether, the 15th tattoo, is feature-complete. Stillshroud and Thunderclap Sigil are next.
