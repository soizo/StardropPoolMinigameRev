<p align="center">
  <img src="documentation/images/banner.gif" alt="Stardrop Pool Minigame Rev">
</p>

<p align="center">
  A playable 8-ball minigame for the pool table in Stardrop Saloon.
</p>

<p align="center">
  <a href="#">Demo</a>
  ·
  <a href="#">Download</a>
  ·
  <a href="../../issues">Issues</a>
</p>

# Stardrop Pool Minigame Rev

Stardrop Pool Minigame Rev is a Stardew Valley SMAPI mod which turns the pool table in the Stardrop Saloon into an 8-ball minigame. Play on your own, challenge an NPC, or watch two NPCs play.

The mod targets Stardew Valley 1.6, SMAPI 4.x and .NET 6. Its DLL unique ID is `soizo.StardropPoolMinigameRev`.

# Table of Contents

- [Current features](#current-features)
- [Playing](#playing)
- [Configuration](#configuration)
- [State persistence](#state-persistence)
- [Installation](#installation)
- [Building](#building)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [Third-party credits and technical notes](#third-party-credits-and-technical-notes)

&nbsp;
&nbsp;
&nbsp;

# Current features

- Classic 8-ball matches with solids, stripes, the cue ball and the 8-ball.
- Solo play.
- NPC matchups against **Sam**, **Sebastian**, **Abigail** or **Gus**.
- NPC watch mode, in which two of those NPCs play against each other.
- Saloon pool-table interaction which detects NPCs at the table and eligible NPCs elsewhere in the Saloon. Inviting an NPC currently requires at least two hearts of friendship.
- Custom cue choices, including NPC-themed cues where available.
- NPC emotes during play, including configurable artwork for emote 8.
- A watch-presence indicator for other players when an NPC match is being watched. This does not make the minigame a Farmer-versus-Farmer multiplayer mode.

# Playing

Interact with the pool table in the Stardrop Saloon. In the default interaction mode, the available choices depend on which eligible NPCs are present:

- with no relevant NPC present, start a solo match;
- with one NPC at the table, play against that NPC;
- with two NPCs at the table, watch them play or choose one as your opponent;
- with eligible NPCs in the Saloon but none at the table, invite an NPC or play solo.

The current four NPC participants are Sam, Sebastian, Abigail and Gus. NPC availability is also subject to the game's location and friendship state.

## Controls

The game is mouse-led. Click and drag on the table to aim and set shot power, then release to shoot. The on-screen controls provide actions such as resetting the rack or returning to the menu. Press **Y** to open or close the emote menu.

Keyboard-only play is **planned**, not currently supported. Controller support is **planned**, not currently supported. Farmer-versus-Farmer multiplayer is **planned**, not currently supported; the current watch-presence messaging only indicates that another farmer is watching an NPC match.

# Configuration

If Generic Mod Config Menu is installed, the mod exposes two options:

- **Default table interaction mode**: default interaction, always solo, always play against a randomly selected eligible NPC, or always watch the first detected eligible pairing at the table when possible.
- **Eye emote appearance**: default, big eyes, or small eyes for emote 8.

Without Generic Mod Config Menu, the default settings are used. Configuration is stored through SMAPI's normal mod config mechanism.

# State persistence

The mod stores pool state in the current save through SMAPI's save-data API. A current table snapshot can preserve balls, turns, shot statistics, pocketed balls and the random state needed to continue a compatible match. The current table state is associated with the in-game day and opponent, and is cleared when it belongs to an earlier day.

NPC watch sessions are also saved with their day, time slot, NPC pairing and simulation snapshot, so a compatible watch session can continue rather than starting over. The selected player cue index is saved as well. This is match/session state, not a permanent player-versus-player record system.

NPC profiles are loaded from the mod's `profile.json` and normalised for the four supported NPCs. The file contains profile and emote data used by the minigame; it is not a player save file.

&nbsp;
&nbsp;
&nbsp;

# Installation

These instructions apply to a complete release package. Place its outer `StardropPoolMinigameRev` folder, which contains the DLL mod and its sibling Content Patcher pack, in Stardew Valley's `Mods` folder. The DLL mod has the unique ID `soizo.StardropPoolMinigameRev`; its companion Content Patcher pack has the unique ID `soizo.StardropPoolMinigameRevCP` and supplies the `Minigames/stardropPool` and `Minigames/stardropPoolFont` game assets.

The current source checkout does not include the `[CP] StardropPoolMinigameRev/` source folder required to assemble that release package. See [Building](#building) before attempting to package this checkout.

# Building

Requirements:

- .NET 6 SDK
- Stardew Valley 1.6 and SMAPI 4.x development references resolved by `Pathoschild.Stardew.ModBuildConfig` 4.3.2

Build the project in Release mode:

```sh
dotnet build StardropPoolMinigameRev.csproj -c Release
```

The project targets `net6.0`. Its post-build deployment target is disabled for mod deployment and mod zipping, but it copies the manifest, profile, translations and asset files into the normal build output.

To create the project's package layout, run:

```sh
./package.sh
```

`package.sh` removes and recreates `build/StardropPoolMinigameRev`, builds the Release project, copies the DLL mod files, and then copies `[CP] StardropPoolMinigameRev` as a sibling Content Patcher folder. The script therefore requires that `[CP] StardropPoolMinigameRev/Assets/Tilesheets/stardropPool.png` exists at the repository root; that source Content Patcher folder is not present in this checkout, so the script cannot complete here until the pack is supplied. The script copies `stardropPool.png` as a DLL fallback, but does not copy `upsidedown_emotes.png`, which the DLL also requires. The script must therefore be completed or corrected before it can produce an installable release package.

# Roadmap

The following features are planned and should not be treated as available in the current release:

- Controller support.
- Complete keyboard-only play.
- Farmer-versus-Farmer multiplayer.

# Contributing

Keep changes within the current scope unless the feature is explicitly marked as planned. Prefer small, focused changes and verify both the in-game interaction flow and save/load behaviour. Build with the project file above before submitting a change.

The implementation is a C# SMAPI mod centred on `ModEntry`, `PoolTableInteractionMenu`, `GameScene` and the save-data types. Translations live in `i18n/default.json`; the shipped visual assets are under `Assets/Tilesheets`.

# Third-party credits and technical notes

This project uses or is inspired by the following work. These references are recorded for attribution and technical context; no licence is declared here for work whose source does not state one.

## Stardrop Pool source reference

The project references the upstream Stardrop pool project:

- **stardew-valley-stardrop-pool-minigame** — <https://github.com/andyruwruw/stardew-valley-stardrop-pool-minigame>

The upstream repository does not declare a licence. This project was looking for a Stardew Valley pool-table minigame and found that repository, but could not use it as-is; its assets were used while this implementation was made for this codebase.

## Art and visual references

- **Stardew Valley** by ConcernedApe — visual reference for the Saloon pool-table minigame, UI behaviour and emote presentation style.
- **OwO Emote** (`9848`) — credited for the additional eye-emote variants used as optional row 8 replacements in `upsidedown_emotes.png`:
  - row 16: big-eye version;
  - row 17: small-eye version.

## Algorithm reference

- **Mating 8 Ball Pool** by Bruno Grbavac — <https://github.com/brunogrbavac/Mating-8-Ball-Pool>

This was used as a conceptual reference for NPC 8-ball shot evaluation and genetic-search-style shot selection. The implementation in this mod was reimplemented for this codebase rather than copied verbatim. The referenced repository does not declare a licence.

## Asset and implementation notes

- Rows 16 and 17 in `upsidedown_emotes.png` are configuration-only art variants for emote 8 and are not selectable emote indices.
- The fifth column of `upsidedown_emotes.png` is used for emote-menu button presentation.
- The player emote menu exposes rows 2–10 and 14 only.

## Licence status

[LICENSE](LICENSE) applies **only** to original source code authored for this project. Third-party software, game-derived material, artwork, icons, tilesheets and other assets are excluded. See [NOTICE.md](NOTICE.md) for provenance and credit information; it does not grant permission or a licence for third-party materials.
