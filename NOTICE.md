# Notice

This notice records provenance and credit information for third-party software, game-derived material, artwork, icons, tilesheets and other assets referenced, used or reimplemented by this project. These credits do not assert ownership of third-party materials, grant permission to use them, or provide a licence for them. Refer to the relevant rights holders and source repositories for applicable terms.

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
