# Stardrop Pool Minigame — Rewrite Brief

A consolidated reference for rewriting this mod from scratch. The existing C#
source is a **half-finished prototype**: its scene rendering, input dispatch,
game rules, save/load, config, i18n, and pool-table detection were never
implemented (see §12). What *is* worth keeping is the **art assets** (delivered
via Content Patcher) and the **design intent** encoded in the constants, enums,
entity layout, and docs. This brief captures both, so a rewrite can reuse the
PNGs and reproduce the intended behaviour without re-deriving the sprite map.

All pixel rectangles below are taken verbatim from `TextureConstants.cs` /
`GenericTextureConstants.cs`. Tile size is 16 px throughout.

---

## 1. What the mod is

A Stardew Valley SMAPI mod that turns the pool table in the Stardrop Saloon
into a playable minigame. Right-click the table → launch an `IMinigame` with a
main menu and an 8-ball game. The rewrite scope is intentionally narrow: only
single-player mode and NPC-versus mode are in scope. `Gallery` is repurposed
into a records screen, `Settings` is removed from the in-minigame menu and
belongs in Generic Mod Config Menu, `SummaryScene` is cut, `DialogueScene` is
kept but deferred until late, multiplayer is shelved, non-8-ball modes are cut,
alternate table layouts are cut, and custom cue sticks remain in scope.

- **Mod type:** SMAPI C# mod (`StardropPoolMinigameRev.dll`) **+** a Content
  Patcher content pack that injects two custom tilesheets.
- **Target:** Stardew Valley 1.6, SMAPI 4.x, .NET 6.
- **UniqueID (DLL mod):** `soizo.StardropPoolMinigameRev`
- **UniqueID (CP pack):** `soizo.StardropPoolMinigameRevCP`

---

## 2. Reuse vs rebuild

| Area | Status | Decision |
|---|---|---|
| Art (PNGs, tilesheets, font) | Complete, CP-delivered | **Reuse as-is** |
| Tilesheet region map | Complete in constants | **Reuse the rectangles** |
| 8-ball rules | Documented, not coded | **Reimplement from doc** |
| Physics primitives | Partial (`Physics`, `Orientation`, collision helpers) | **Rewrite cleanly** |
| Scene framework (`Scene`, `Entity`, filters) | Skeleton only; `Draw`/input empty | **Rewrite** |
| `IMinigame` wiring | Works (loads, ticks, draws) | **Keep pattern, fix dispatch** |
| Config / save / i18n / GMCM | Stubs only | **Build fresh** |
| Pool-table detection | Commented out, class absent | **Build fresh** |

---

## 3. Mod packaging (keep as-is)

SMAPI stops descending into a folder once it finds a `manifest.json`, so the
DLL mod and CP pack must be **sibling subfolders under a parent with no
manifest**. `package.sh` already produces this layout:

```
output/StardropPoolMinigameRev/            ← parent, NO manifest
  StardropPoolMinigameRev/                 ← DLL mod
    manifest.json   (UniqueID soizo.StardropPoolMinigameRev, EntryDll StardropPoolMinigameRev.dll)
    StardropPoolMinigameRev.dll
  [CP] StardropPoolMinigameRev/            ← Content Patcher pack
    manifest.json   (UniqueID soizo.StardropPoolMinigameRevCP, ContentPackFor Pathoschild.ContentPatcher)
    content.json
    Assets/Tilesheets/stardropPool.png
    Assets/Tilesheets/stardropPoolFont.png
```

`content.json` loads two game assets from the PNGs:

| Game asset target | From file |
|---|---|
| `Minigames/stardropPool` | `Assets/Tilesheets/stardropPool.png` (512×512) |
| `Minigames/stardropPoolFont` | `Assets/Tilesheets/stardropPoolFont.png` (144×77) |

At runtime the mod loads them via `Game1.content.Load<Texture2D>("Minigames\\stardropPool")` and `"Minigames\\stardropPoolFont"`. Portraits come from vanilla: `Portraits\Abigail`, `Portraits\Gus`, `Portraits\Sam`, `Portraits\Sebastian`.

---

## 4. Asset inventory

### 4.1 Main tilesheet `stardropPool.png` — 512×512

All rectangles are `X, Y, W, H` in pixels. Source: `StardropPool/Constants/TextureConstants.cs`.

#### Environment
| Region | X | Y | W | H | Notes |
|---|---|---|---|---|---|
| BarShelves | 0 | 0 | 400 | 128 | Menu background (full width) |
| FloorTiles | 256 | 128 | 64 | 64 | Floor tile |
| GameTitle | 0 | 128 | 128 | 80 | "Stardrop Pool" logo |
| PortraitRays | 0 | 128 | 272 | 64 | Shine rays behind portraits |

#### Ball — base colours (16×16 each)
| Colour | X | Y |
|---|---|---|
| White | 320 | 144 |
| Yellow | 336 | 144 |
| Blue | 352 | 144 |
| Red | 368 | 144 |
| Purple | 384 | 144 |
| Orange | 320 | 160 |
| Green | 336 | 160 |
| Maroon | 352 | 160 |
| Black | 368 | 160 |

- Ball.Highlight: 368,176,16,16
- Ball.Shadow: 384,160,16,16

#### Ball — number→colour/type mapping (`Ball.GetBallColor`/`GetBallType`)
- Number 0 → White (cue ball)
- Numbers 1–7 → Solid (colour by `number % 8`: 1 Yellow, 2 Blue, 3 Red, 4 Purple, 5 Orange, 6 Green, 7 Maroon)
- Number 8 → Black
- Numbers 9–15 → Striped (same colour mapping as 1–7 via `number % 8`)

#### Ball — cores (the white circle bearing the number, oriented by roll)
Cores are selected by polar orientation `(longitude X, latitude Y)`. Latitude
values: 90, 60, 30, 0, −30, −60. Longitude values: 0, 30, 45, 60, 90, 120, 135,
150 (not all combinations exist — see `Textures.GetBallCoreBounds` for the exact
lookup). All core rects are 16×16. Full table (`TextureConstants.Ball.Core`):

| Core | X | Y | | Core | X | Y |
|---|---|---|---|---|---|---|
| 0_0 | 400 | 48 | | 90_0 | 448 | 48 |
| 0_30 | 400 | 32 | | 90_30 | 448 | 32 |
| 0_60 | 400 | 16 | | 90_60 | 432 | 16 |
| 0_90 | 400 | 0 | | 90_N30 | 448 | 64 |
| 0_N30 | 400 | 64 | | 90_N60 | 432 | 80 |
| 0_N60 | 400 | 80 | | 120_0 | 464 | 48 |
| 30_0 | 416 | 48 | | 120_30 | 464 | 32 |
| 30_30 | 416 | 32 | | 120_N30 | 464 | 64 |
| 30_N30 | 416 | 64 | | 135_60 | 448 | 16 |
| 45_60 | 416 | 16 | | 135_N60 | 448 | 80 |
| 45_N60 | 416 | 80 | | 150_0 | 480 | 48 |
| 60_0 | 432 | 48 | | 150_30 | 480 | 32 |
| 60_30 | 432 | 32 | | 150_N30 | 480 | 64 |
| 60_N30 | 432 | 64 | | | | |

#### Ball — stripes (only for striped balls; latitude-only selection)
All 16×16 at X=400:
| Stripes | Y |
|---|---|
| 0_90 | 96 |
| 0_60 | 112 |
| 0_30 | 128 |
| 0_0 | 144 |
| 0_N30 | 160 |
| 0_N60 | 176 |

#### Cue sticks (128×16 each)
| Cue | X | Y |
|---|---|---|
| Basic | 128 | 128 |
| Sam | 128 | 144 |
| Sebastian | 128 | 160 |
| Abigail | 128 | 176 |
| Gus | 128 | 192 |

#### Cursor (16×16)
Default 368,128; Frame1 320,128; Frame2 336,128; Frame3 352,128.

#### Particles (16×16)
| Particle | Frames (X,Y) |
|---|---|
| Glimmer | (320,176), (336,176) |
| Spark | (304,192), (320,192), (336,192) |
| Sparkle | (256,192), (272,192), (288,192) |
| PurpleWhisp | (352,192), (368,192), (384,192) |

#### Pocketed-balls UI
- BorderBox: 192,336,112,32
- Supports: 384,128,16,16
- TextPanelLeftCap: 192,341,5,24
- TextPanelFill: 197,340,1,24 (tile horizontally to fill width)
- TextPanelRightCap: 299,341,5,24
- ResetButton: 306,341,16,16

#### Top-row UI elements
- The row is composed of **row elements**.
- We currently have one row element.
- Row element types:
  - **Button** — clickable. Buttons should support a Galdora-style visual treatment. The implementation must detect whether the current theme is Galdora; if this depends on Stardew Valley Expanded, it must also detect whether the SVE mod is installed before checking theme state. On hover, a button expands linearly by 1 px on all sides.
  - **Idle** — non-interactive display content. It cannot be clicked; it simply exists in the row.
  - **Arrow** — directional control element. It has no hover reaction. While pressed, it shrinks by 1 px.

#### Portrait fire animation (64×64, 8 frames)
1: 192,272 · 2: 256,272 · 3: 320,272 · 4: 384,272 · 5: 448,272 · 6: 0,336 · 7: 64,336 · 8: 128,336

#### Table segments (all 32×32)
- Felt: 352,176,16,16
- Corners (Back/Front × NE/NW/SE/SW): Y=272 (SE/SW) or 304 (NE/NW); X = 0/64 (back) or 32/96 (front). See `TextureConstants.Table.Corner.{Back,Front}.{NorthEast,NorthWest,SouthEast,SouthWest}`.
- Edges (Back/Front × 8 directions): Y=208 (N/NE/NW/E/W rows) or 240 (S/SE/SW rows). X varies 0–480. See `TextureConstants.Table.Edge.{Back,Front}.*`.
- Pockets (Back/Front × 8 directions): Y=208 or 240; X 128–352. See `TextureConstants.Table.Pocket.{Back,Front}.*`.

Direction mapping (`TableSegmentType` enum): edges/corners/pockets exist for N, S, E, W plus the four diagonals (NE, NW, SE, SW). The table is built from a grid of these segments; `GetTableSegmentBackFromType`/`GetTableSegmentFrontFromType` map a segment type to its texture rect (back drawn first, front drawn over the balls).

### 4.2 Font `stardropPoolFont.png` — 144×77

Custom bitmap font. Glyphs are packed in rows of height 13 px (`CharacterHeight`),
with 1 px spacing on the sheet (`SpaceBetweenCharactersOnTileset`) and 1 px
between rendered glyphs (`SpaceBetweenCharacters`). Space width 4 px. Line
spacing 3 px. Y offset 1 px for descenders.

Every ASCII letter (upper + lower) plus `. , ! ? ' :` and a set of accented
Latin glyphs (à/á/â/ä/æ/ç/é/è/ê/ë/ì/í and uppercase) has its own `(X, Y, W, 13)`
rect in `GenericTextureConstants.Characters`. Widths vary per glyph (2–12 px).
For a rewrite, the simplest path is to keep `GenericTextureConstants.Characters`
as the glyph atlas index and render character-by-character via the existing
`Text`/`Character` approach (each glyph = one sprite draw).

### 4.3 Portraits (vanilla 64×64 sheets)

NPC portrait emotions are sub-rects of the vanilla portrait sheets. E.g. Sam
has Default, Embarrassed, Frustrated, Glare, Laugh, Oops, Sad, Shock,
StraightFace; Abigail has Default, Blush, Confused, Glare, Laugh, Sad,
Surprised; etc. Full rects in `GenericTextureConstants.Portrait.<NPC>.*`.
Silhouettes are rendered by tinting (not separate art).

### 4.4 Loose source assets (under `assets/`)

The repo also ships the individual source PNGs (and `.psd`s) that were
composited into the two tilesheets: `assets/png/ball/`, `assets/png/cue/`,
`assets/png/Table/`, `assets/png/effects/`, `assets/png/background/`,
`assets/png/ui/`, `assets/png/font.png`, plus Tiled files
`assets/tilesheet/StardropPoolMinigame.tmx` + `StardropPoolMinigameTileset.tsx`
(a 100×100-tile Tiled map used only for editor preview, not at runtime). These
are not needed by the running mod (the CP tilesheets contain everything) but
are useful reference if you ever regenerate the atlas.

---

## 5. Game design vision (from README)

### Gamemodes
- **8-Ball** — stripes vs solids, sink your group then the 8. (Primary.)
- **9-Ball** — must hit lowest ball first; sink the 9 to win.
- **Goalkeeper** (original) — each player has own cue ball + pockets to
  protect/score in, football-like; most points when all balls pocketed.
- **Tug of War** (original) — shared cue ball, designated pockets, fewer balls,
  turn-limited.
- **Chaos** (original) — non-standard physics; most balls pocketed wins.

### Table layouts
Classic, Crammed, Goalkeeper, Single Lane, Tug of War (see
`TableType` enum and `assets/png/Table/*-layout.png`). Only the segment art +
`TableSegmentType` grid differ between layouts.

### Opponents & progression
The rewrite only keeps **Sam** and **Sebastian** as NPC opponents. NPC-versus
mode is **not** a main-menu option. Instead, it is offered only through saloon
pool-table interaction:

- If neither Sam nor Sebastian is currently in the saloon, only the standard
  single-player path is available.
- If exactly one of them is present, interacting with the pool table should
  start an NPC-versus game against that character.
- If both are present, interacting with the pool table should first present a
  two-option opponent choice UI before entering the minigame.
- That opponent-choice UI must be internationalised from the start.

Wins/losses/records should be tracked for the supported NPC opponents. Custom
cue sticks remain in scope as unlockable/customisable content.

---

## 6. Rules — 8-ball (from `documentation/REVISED-8BALL-RULES.md`)

Based on UPA 8-ball, simplified:

1. **Object** — be the first to legally pocket the 8-ball, after clearing your
   group (solids 1–7 or stripes 9–15).
2. **Rack** — 15 balls, 8 centred, base parallel to the short rail, tip on the
   foot spot.
3. **Break** — random first breaker; must strike at least one ball. Foul on
   break → opponent gets ball-in-hand behind the head string and must shoot an
   object ball out of the "kitchen".
4. **Open table** — group choice is open until a player legally pockets a
   called object ball. While open, you may hit one group to pocket another.
5. **Continuation** — pocketing a legal ball = shoot again; illegal ball = scratch.
6. **Scratch** — cue ball pocketed = ball-in-hand foul. Scratching on the 8 is
   *not* loss of game unless the 8 was also pocketed.
7. **Bad hit** — first object ball contacted must be from your group (or the 8
   if your group is cleared) → else ball-in-hand foul.
8. **Call pocket** — only required for the 8-ball. Wrong pocket on the 8 = loss.

The `GameEvent` enum captures the events to emit: `ChoseBallType`,
`BallPocketed`, `Scratch`, `Win`. Turn flow (`TurnState`): Start →
PlacingBall (after scratch) → SelectingAngle → SelectingPower → MovingBalls →
Results → (PowerUpSelect optional). Game stage (`GameStage`): Title →
Prebattle (dialogue) → Play → Summary → Postbattle.

---

## 7. Physics & simulation

### Constants (`GameConstants.cs`)
**Ball:** radius 5.5, mass 1, minimum velocity 0.05, minimum bounce velocity
0.02, halt-begin velocity 0.08, friction acceleration −0.005 (per unit
distance), halt acceleration −0.03, distance-to-orientation-change 5, wall
bounce momentum loss 0.

**Cue:** momentum transfer 5 (cue power → ball speed), particle minimum power
0.85, particle rate per power 3, striking speed 10, wiggle frequency 800,
power→wiggle-amplitude scalar 75. Min/max distance from cue ball derived from
radius via `RenderConstants.TileScale()`.

**Particles (boids-like):** alignment/cohesion/separation strengths, perception
radius, max force/velocity, lifespan 50. Glimmer/PurpleWisp/Sparkle share
values (align 0.04, cohere 0.005, separate 0.08, perception 5, maxForce 0.1,
maxVel 1.5); Spark is denser (align 0.02, cohere 0.03, separate 0.04,
perception 8, maxForce 0.2).

### Integration (`Physics.Update`)
Simple Euler, per tick: `position += velocity; velocity += acceleration; acceleration = Vector2.Zero;`

### Ball rolling (`Orientation`)
- Circumference = `2π × radius`.
- On move: `orientation += (velocity / circumference) × 360°` (X and Y axes).
- `GetFace()` snaps latitude to 30° steps and longitude to 30° (or 45° at
  ±60° latitude) → picks the matching core/stripe texture rect.
- Latitude clamped to ±90°, longitude to 0–180° (0–150° at ±60° lat).

### Collision
`MinigameFramework/Helpers/RangeIntersection.cs` + `DistanceHelpers.cs`
implement circle–circle, circle–rectangle, and line–circle intersection. Ball
vs table segment uses the segment's bounceable surface geometry
(`RenderConstants.Entities.TableSegment`: Border 10, Lip 5, Margin 3,
PocketRadius 7, etc.; `SpaceToBounceableSurface = Margin + Border + UnpassableLip = 16`).

### Spatial query
Originally intended as a `QuadTree<T>` (capacity 4, subdivides). The current
clone has a degenerate list-based stub. For a rewrite, a simple list is fine
for 16 balls; only add a real quadtree if particles become a bottleneck.

---

## 8. Coordinate system & rendering

This is the part the original got wrong, so decide it up front in the rewrite.

- **Logical minigame screen:** 400×224 (`GenericRenderConstants.MinigameScreen`).
- **Tile size:** 16 px. **Tile scale:** `Game1.pixelZoom` (4 at default zoom).
- The minigame window is centred in the SDV viewport with a margin:
  `AdjustedScreen.GetNorthWest() = (Margin.Width, Margin.Height)` where
  `Margin = (Viewport − AdjustedScreen) / 2`.
- **Convert logical→raw pixels:** `point × TileScale + AdjustedScreen.GetNorthWest()`.
- **Convert raw→logical:** `(point − GetNorthWest()) / TileScale`.

**Recommended render strategy:** in `Scene.Draw`, `SpriteBatch.Begin` with a
transform **matrix** = `CreateScale(TileScale) × CreateTranslation(margin)`.
Then draw every entity at its **logical** anchor with **scale 1** (the matrix
supplies the zoom). This makes all entity anchors, layout constants, and mouse
coordinates work in the 400×224 logical space — no per-entity conversion.
Mouse input from SDV (`receiveLeftClick(x,y)` in raw pixels) must be converted
to logical via `ConvertRawToAdjustedScreen` before hit-testing entities.

(The original `Entity.Draw` doubled up — it returned the unscaled anchor as the
destination *and* multiplied scale by TileScale — which is why nothing lined
up. A matrix-based approach avoids that entirely.)

**Layering:** use small layer-depth increments (0.0001–0.0003) as the original
does: base < core < stripes < shadow < highlight; backgrounds ~0.001, portraits
~0.0011, buttons ~0.004, title ~0.005, popups 1.0.

---

## 9. Scene & flow architecture

`IMinigame` (`StardropPoolMinigameRev`) owns the current `Scene` and delegates
`tick`→`Scene.Update`, `draw`→`Scene.Draw`, and all input handlers. Scenes run
a transition state machine: `Entering → Present → Exiting → Dead`. A scene can
stage a replacement via `HasNewScene()`/`GetNewScene()` (the `tick` loop must
actually swap `_scene` — currently it fetches but discards the new scene; fix
this).

**Scenes:**
- **MainMenuScene** — BarShelves bg, 4 NPC portraits (silhouettes until
  unlocked), 4 BallButtons (Play, Multiplayer, Gallery, Settings), GameTitle.
  Plays `SoundConstants.GameTheme`.
- **GameScene** — table, balls, cue, HUD, popups. **Stub in the original** —
  must be built.
- **DialogueScene** — pre/post-battle NPC dialogue. **Stub.**
- **SummaryScene** — post-game results, then back to menu. **Stub.**

### MainMenu layout (exact, from `RenderConstants.MainMenuScene`)
- Screen centre = `GenericTextures.GetMinigameScreenCenter()` = (200, 112).
- Portraits: `TopMargin = BarShelves.Height − Portrait.Height` = 128 − 64 = 64;
  `Gap = Portrait.Width` = 64. Portraits at X = centre − 2·Gap, −Gap, +0, +Gap.
- Buttons: `TopMargin = ((224 − 128) − 4·13 − 3·4) + 128` = 168; `Gap = 4`;
  `Height = 13 + 4 = 17`. Buttons stacked at Y = 168, 185, 202, 219; X = centre
  + `BallButton.LeftOffset` (4). Each BallButton is `Origin.TopCenter`,
  maxWidth 80, ball numbers 1/10/3/12 for Play/Multiplayer/Gallery/Settings.
- GameTitle: `TopMargin = 6`, `Origin.TopCenter`, SlideIn difference (0,−25).
- BallButton internal layout: ball (radius 5.5) at left, text button to the
  right with `InnerPadding = 8`.

### Transitions/filters
`FilterGenerator.CreateSlideTransition(...)` (and `ScaleUp`) produce
`IFilter`s that modify an entity's draw params each frame (position offset,
scale, opacity) with ease-out. `BallFlashingAnimation` is a looping animation
that pulses colour. Keep these — they're the polish layer.

---

## 10. UI entities

- **Entity / IEntity** — base: anchor, origin, layer depth, transition state,
  filters, draw/update/hover/click. Provides `GetTopLeft`/`GetCenter`/
  `GetBoundary` based on origin + width/height.
- **Button** — text label (`Text` entity), disabled state, click sound.
- **BallButton** — composite: a `Ball` + a `Button` (menu item).
- **Text / Character** — bitmap-font renderer (see §4.2).
- **Portrait** — NPC portrait with emotions, silhouette tint, fire/ray effects.
- **Backgrounds** — BarShelves, FloorTiles.
- **Popups** — Victory, Defeat, YourTurn, OpponentTurn, Scratch, BallPocketed.
  Auto-dismiss after ~100 frames, layer depth 1.0.
- **Particles** — Glitter, Sparkle, PurpleWisp, Spark; boids behaviour via
  `IParticleRules`; emitter covers the 400×224 screen.

---

## 11. Supporting systems

### Config (`ModConfig.cs`) — build with GMCM
- `PlayAnyTime` (bool, false) — play without NPC present.
- `ShowParticles` (bool, true).
- `ShowTransitions` (bool, false).
Currently unused. Wire to GMCM and respect in the renderer/physics.

### Save data (`SaveJson.cs`) — persist via `helper.Data`/JSON
- `ArcadeTokens`, `CurrentCue` ("basic"), `UnlockedCues` (comma list, "basic").
- Per-opponent `Wins`/`Losses`/`Highscore` for Abigail, Gus, Sam, Sebastian
  (highscore default −1).
Load on minigame start, save on game end.

### Sound (`SoundConstants.cs` + `Sounds.cs`)
Cue lock-angle `give_gift`, full-charge `furnace`, lock-power `FishHit`,
strike `thudStep`, ball bounce `thudStep`, balls colliding `stoneStep`,
pocketed `coin`, scratch `cancel`, victory `getNewSpecialItem`. Music:
game theme `movieTheater`; Sam `ragtime`, Sebastian `crane_game`,
Abigail `cowboy_boss`, Gus `PIRATE_THEME`. Play via
`Game1.playSound(id)` / `Game1.changeMusicTrack(id, false, MusicContext.MiniGame)`.

### i18n — needs creating
Translation helper is wired (`Translations.SetHelper`), keys exist
(`menu.button.main.play`, etc.), but **no `i18n/` folder or JSON files**.
Create `i18n/default.json` (and per-language) for all `EntityKeys` strings.

### Input
`overrideFreeMouseMovement()` returns true (minigame captures the cursor).
`receiveLeftClick`/`leftClickHeld`/`receiveRightClick`/`release*`/`receiveKeyPress`/
`receiveKeyRelease` arrive with **raw pixel** coordinates; convert to logical
before dispatch. Escape → `forceQuit()`.

---

## 12. Gap analysis — what's stubbed or missing

| Component | State |
|---|---|
| `Scene.Draw` | Empty base; no scene overrides it → black screen |
| `Scene.HandleLeftClick/RightClick/KeyPress…` | Empty base; no overrides → no input |
| `Scene` swap on `HasNewScene()` | Fetched but not assigned in `tick` |
| `GameScene.AddEntities` | Empty — no table/balls/cue/HUD |
| `DialogueScene` / `SummaryScene` | Empty stubs |
| `EightBall` rules class | Empty |
| `IRules` methods | All commented out |
| `PoolTableDetector` | Referenced in a comment; class absent |
| Config usage | Fields defined, never read |
| Save load/save | Structure defined, never persisted |
| i18n files | Absent |
| GMCM integration | Absent |
| Multiplayer | Commented out; no message handling |
| `Entity.Draw` coordinate handling | Inconsistent (see §8) |
| `QuadTree` | Degenerate list stub (acceptable) |
| `Scene._hasNewScene` field | Never assigned (compiler warning) |

---

## 13. Rewrite plan / recommendations

**Stack:** SMAPI C# mod (.NET 6), one DLL + one CP pack (keep current
packaging). Use the CP-provided tilesheets unchanged; do not touch the PNGs.

**Order of work:**
1. **Skeleton that draws** — `ModEntry` (launch on pool-table interact, see
   below), `IMinigame` shell, and a `Scene` base whose `Draw` begins a
   transform-matrix batch and draws all entities at logical coords with scale 1.
   Input handlers convert raw→logical and dispatch to entities. Goal:MainMenu
   renders and buttons hover/click.
2. **Pool-table detection** — start the minigame only when the player
   right-clicks while facing the saloon pool table. Detect by facing-tile
   object name / tile index (the saloon pool table is a known furniture tile;
   check `Game1.currentLocation.getObjectAtTile` against the table's
   name/parent-sheet index, or by tile coordinate in the Saloon map).
3. **8-ball core** — `GameScene` with table grid (`TableSegmentType`), 16 balls
   racked, cue with Angle→Power→Shooting states, Euler physics + collision,
   pocket detection, turn/rules engine emitting `GameEvent`s.
4. **Flow** — MainMenu → Dialogue → Game → Summary → back to menu, with scene
   swaps actually assigned in `tick`.
5. **Polish** — transitions/filters, particles, popups, sounds, portraits.
6. **Systems** — GMCM config, save/load, i18n files, per-opponent progression
   and cue unlocks.

**Conventions for the rewrite:**
- Namespace: `StardropPoolMinigameRev` (already unified).
- Logical coordinate space everywhere; one transform matrix at draw time.
- Keep the existing constant values (rectangles, physics, layout) — they're
  tuned to the art.
- British English in all docs and user-facing strings; English in code
  comments.

**Quick wins to verify first:** the two CP tilesheets load (confirmed working),
portraits load from vanilla (confirmed), and the `IMinigame` lifecycle runs
(confirmed — it ticks and draws without crashing once `Scene.Draw` is
implemented). The black screen is *only* because `Scene.Draw` is empty.

---

*Source files of record for the rectangles: `StardropPool/Constants/TextureConstants.cs`,
`MinigameFramework/Constants/GenericTextureConstants.cs`, `StardropPool/Constants/GameConstants.cs`,
`StardropPool/Constants/RenderConstants.cs`, `MinigameFramework/Constants/GenericRenderConstants.cs`.
If anything here disagrees with those files, the files win.*
