using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardropPoolMinigameRev.Assets;
using StardropPoolMinigameRev.Constants;
using StardropPoolMinigameRev.Rendering;
using StardropPoolMinigameRev.Scenes;

namespace StardropPoolMinigameRev
{
    internal sealed class StardropPoolMinigameRev : IMinigame
    {
        private const float CapturedMouseDeadZone = 0.5f;
        private const float CapturedMouseWarpDeadZone = 2f;

        private readonly StardropPoolAssets _assets;
        private IMinigameScene _scene;
        private readonly IMonitor _monitor;
        private readonly MinigameViewport _viewport;
        private readonly Action<PoolTableSnapshot> _saveSnapshot;
        private readonly Action<PoolTableSnapshot>? _saveWatchSnapshot;
        private readonly Action<int> _savePlayerCueIndex;
        private readonly int _lastPlayerCueIndex;
        private readonly bool _isSveInstalled;
        private readonly string? _npcOpponentName;
        private readonly string? _npcPlayerName;
        private readonly PoolNpcProfiles _profiles;
        private readonly ModConfig _config;
        private readonly int? _randomSeedDay;
        private PoolTableSnapshot? _currentSnapshot;
        private readonly string? _previousMusicTrack;
        private readonly bool _previousMouseVisible;
        private readonly int _previousMouseCursor;
        private bool _wasCapturingMouse;
        private bool _mouseMovedToRelease;
        private Vector2 _captureAnchorLogical = new(MinigameViewport.LogicalWidth / 2f, MinigameViewport.LogicalHeight / 2f);
        private Vector2 _lastRawLogical = new(MinigameViewport.LogicalWidth / 2f, MinigameViewport.LogicalHeight / 2f);
        private Vector2 _capturedLogicalMouse = new(MinigameViewport.LogicalWidth / 2f, MinigameViewport.LogicalHeight / 2f);

        public StardropPoolMinigameRev(IModHelper helper, IMonitor monitor, PoolTableSnapshot? snapshot, Action<PoolTableSnapshot>? saveWatchSnapshot, Action<PoolTableSnapshot> saveSnapshot, Action<int> savePlayerCueIndex, int lastPlayerCueIndex, string? npcOpponentName = null, string? npcPlayerName = null, PoolNpcProfiles? profiles = null, ModConfig? config = null, int? randomSeedDay = null)
        {
            _monitor = monitor;
            _saveWatchSnapshot = saveWatchSnapshot;
            _saveSnapshot = saveSnapshot;
            _savePlayerCueIndex = savePlayerCueIndex;
            _lastPlayerCueIndex = lastPlayerCueIndex;
            _npcOpponentName = npcOpponentName;
            _npcPlayerName = npcPlayerName;
            _profiles = profiles ?? new PoolNpcProfiles();
            _config = config ?? new ModConfig();
            _randomSeedDay = randomSeedDay;
            _currentSnapshot = snapshot;
            _isSveInstalled = helper.ModRegistry.IsLoaded("FlashShifter.StardewValleyExpandedCP")
                || helper.ModRegistry.IsLoaded("FlashShifter.SVECode");
            _monitor.Log("Creating Stardrop Pool rewrite minigame.", LogLevel.Info);

            _viewport = new MinigameViewport();
            _viewport.Update();
            _previousMouseVisible = Game1.game1.IsMouseVisible;
            _previousMouseCursor = Game1.mouseCursor;

            _assets = new StardropPoolAssets(helper, _monitor);
            _assets.Load();

            _scene = new GameScene(_monitor, snapshot, _isSveInstalled, _npcOpponentName, _npcPlayerName, _profiles, _config, settleSnapshot: !IsWatchMode(), randomSeedDay: _randomSeedDay, lastPlayerCueIndex: _lastPlayerCueIndex, savePlayerCueIndex: _savePlayerCueIndex);
            _previousMusicTrack = Game1.currentSong?.Name;
            Game1.changeMusicTrack("movieTheater");

            _monitor.Log($"Stardrop Pool rewrite minigame ready. Viewport scale {_viewport.Scale}, top-left {_viewport.TopLeft}.", LogLevel.Info);
        }

        public bool tick(GameTime time)
        {
            _viewport.Update();
            UpdateCapturedMouse();
            UpdateActiveMenu(time);
            if (Game1.game1.IsActive || !IsWatchMode())
            {
                if (IsWatchMode() && _scene is GameScene gameScene && gameScene.IsWatchCatchUpPending)
                {
                    gameScene.ProcessWatchCatchUp(time.ElapsedGameTime.TotalSeconds);
                }
                else
                {
                    _scene.Update(time);
                }
            }

            if (_scene.PendingTransition != SceneId.None)
            {
                TransitionTo(_scene.PendingTransition);
            }

            return false;
        }

        private bool IsWatchMode()
        {
            return !string.IsNullOrWhiteSpace(_npcPlayerName) && !string.IsNullOrWhiteSpace(_npcOpponentName);
        }

        public void FastForwardWatch(double seconds)
        {
            if (IsWatchMode() && _scene is GameScene gameScene && seconds > 0)
            {
                gameScene.FastForwardWatch(seconds);
            }
        }

        private void TransitionTo(SceneId target)
        {
            switch (target)
            {
                case SceneId.Game:
                    _monitor.Log("Transitioning to game scene.", LogLevel.Info);
                    _scene = new GameScene(_monitor, _currentSnapshot, _isSveInstalled, _npcOpponentName, _npcPlayerName, _profiles, settleSnapshot: !IsWatchMode(), randomSeedDay: _randomSeedDay, lastPlayerCueIndex: _lastPlayerCueIndex, savePlayerCueIndex: _savePlayerCueIndex);
                    break;
                case SceneId.Quit:
                    QuitMinigame();
                    break;
            }
        }

        public void draw(SpriteBatch batch)
        {
            if (_scene is GameScene gameScene && gameScene.IsWatchCatchUpPending)
            {
                return;
            }

            DrawRawFloorBackground(batch);

            batch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null,
                null,
                null,
                _viewport.Transform
            );
            _scene.Draw(batch, _viewport, _assets);
            batch.End();
        }

        private void DrawRawFloorBackground(SpriteBatch batch)
        {
            Rectangle floor = SpriteRects.Environment.FloorTiles;
            int viewportWidth = Game1.game1.localMultiplayerWindow.Width;
            int viewportHeight = Game1.game1.localMultiplayerWindow.Height;
            int tileWidth = Math.Max(1, (int)MathF.Ceiling(floor.Width * _viewport.Scale));
            int tileHeight = Math.Max(1, (int)MathF.Ceiling(floor.Height * _viewport.Scale));

            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            for (int y = 0; y < viewportHeight; y += tileHeight)
            {
                for (int x = 0; x < viewportWidth; x += tileWidth)
                {
                    batch.Draw(_assets.Tilesheet, new Rectangle(x, y, tileWidth, tileHeight), floor, Color.White);
                }
            }

            batch.End();
        }

        public void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (TryDismissMatchEndDialogue())
            {
                return;
            }

            if (TryGetActiveMenu(out IClickableMenu? menu) && menu != null)
            {
                _monitor.Log($"[DEBUG-endmenu] Forwarding left click ({x}, {y}) to {menu.GetType().FullName}.", LogLevel.Trace);
                menu.receiveLeftClick(x, y, playSound);
                _monitor.Log($"[DEBUG-endmenu] After left click activeMenu={Game1.activeClickableMenu?.GetType().FullName ?? "null"}.", LogLevel.Trace);
                return;
            }

            Vector2 logical = _viewport.RawToLogical(x, y);
            _monitor.Log($"Left click raw {{X:{x} Y:{y}}} -> logical {Format(logical)}.", LogLevel.Info);

            if (_viewport.ContainsLogical(logical))
            {
                _scene.ReceiveLeftClick(logical);
            }
        }

        public void leftClickHeld(int x, int y)
        {
            if (TryGetActiveMenu(out IClickableMenu? menu) && menu != null)
            {
                menu.leftClickHeld(x, y);
                return;
            }

            Vector2 logical = GetInputLogicalPosition(x, y);

            if (_viewport.ContainsLogical(logical))
            {
                _scene.LeftClickHeld(logical);
            }
        }

        public void releaseLeftClick(int x, int y)
        {
            if (TryGetActiveMenu(out IClickableMenu? menu) && menu != null)
            {
                menu.releaseLeftClick(x, y);
                return;
            }

            Vector2 logical = GetInputLogicalPosition(x, y);

            if (_viewport.ContainsLogical(logical))
            {
                _scene.ReleaseLeftClick(logical);
            }
        }

        public void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (TryDismissMatchEndDialogue())
            {
                return;
            }

            if (TryGetActiveMenu(out IClickableMenu? menu) && menu != null)
            {
                menu.receiveRightClick(x, y, playSound);
                return;
            }

            Vector2 logical = _viewport.RawToLogical(x, y);
            _monitor.Log($"Right click raw {{X:{x} Y:{y}}} -> logical {Format(logical)}.", LogLevel.Info);

            if (_viewport.ContainsLogical(logical))
            {
                _scene.ReceiveRightClick(logical);
            }
        }

        public void releaseRightClick(int x, int y)
        {
        }

        public void receiveKeyPress(Keys key)
        {
            _monitor.Log($"Minigame key press: {key}.", LogLevel.Info);

            if (TryDismissMatchEndDialogue())
            {
                return;
            }

            if (TryGetActiveMenu(out IClickableMenu? menu) && menu != null)
            {
                _monitor.Log($"[DEBUG-endmenu] Forwarding key {key} to {menu.GetType().FullName}.", LogLevel.Trace);
                menu.receiveKeyPress(key);
                _monitor.Log($"[DEBUG-endmenu] After key {key} activeMenu={Game1.activeClickableMenu?.GetType().FullName ?? "null"}.", LogLevel.Trace);
                return;
            }

            if (key == Keys.Escape)
            {
                QuitMinigame();
                return;
            }

            _scene.ReceiveKeyPress(key);
        }

        public void receiveKeyRelease(Keys key)
        {
        }

        private bool TryDismissMatchEndDialogue()
        {
            return _scene is GameScene gameScene && gameScene.TryDismissMatchEndDialogue();
        }

        private void UpdateActiveMenu(GameTime time)
        {
            if (TryGetActiveMenu(out IClickableMenu? menu) && menu != null)
            {
                MouseState mouse = Mouse.GetState();
                menu.performHoverAction(mouse.X, mouse.Y);
                menu.update(time);
            }
        }

        private bool TryGetActiveMenu(out IClickableMenu? menu)
        {
            menu = Game1.activeClickableMenu;
            return menu != null;
        }

        public bool overrideFreeMouseMovement()
        {
            return true;
        }

        public bool doMainGameUpdates()
        {
            return false;
        }

        public void changeScreenSize()
        {
            _viewport.Update();
            _monitor.Log($"Screen size changed. Viewport scale {_viewport.Scale}, top-left {_viewport.TopLeft}.", LogLevel.Info);
        }

        public void unload()
        {
            _monitor.Log("Unloading Stardrop Pool rewrite minigame.", LogLevel.Info);
            PersistSceneState();
            RestoreMouseState();
            Game1.changeMusicTrack(_previousMusicTrack ?? "none");
        }

        public void receiveEventPoke(int data)
        {
        }

        public string minigameId()
        {
            return "StardropPoolMinigameRev";
        }

        public bool forceQuit()
        {
            QuitMinigame();
            return true;
        }

        private void QuitMinigame()
        {
            _monitor.Log("Quitting minigame.", LogLevel.Info);
            PersistSceneState();
            RestoreMouseState();
            unload();
            Game1.currentMinigame = null;
        }

        private Vector2 GetInputLogicalPosition(int x, int y)
        {
            if (!_scene.CapturesMouse)
            {
                return _viewport.RawToLogical(x, y);
            }

            return _capturedLogicalMouse;
        }

        private void UpdateCapturedMouse()
        {
            if (!_scene.CapturesMouse)
            {
                if (_wasCapturingMouse)
                {
                    Vector2? releasePosition = _scene.MouseReleaseLogicalPosition;
                    if (releasePosition.HasValue && !_mouseMovedToRelease)
                    {
                        MoveMouseToLogical(releasePosition.Value);
                    }

                    _wasCapturingMouse = false;
                    _mouseMovedToRelease = false;
                }

                Game1.game1.IsMouseVisible = _previousMouseVisible;
                Game1.mouseCursor = _previousMouseCursor;
                return;
            }

            if (!_wasCapturingMouse)
            {
                Vector2 raw = _viewport.RawToLogical(Mouse.GetState().X, Mouse.GetState().Y);
                _captureAnchorLogical = raw;
                _lastRawLogical = raw;
                _capturedLogicalMouse = raw;
                _mouseMovedToRelease = false;
                _wasCapturingMouse = true;
            }

            Game1.game1.IsMouseVisible = false;
            Game1.mouseCursor = Game1.cursor_none;

            Vector2 lockPosition = _scene.MouseReleaseLogicalPosition ?? _captureAnchorLogical;
            Vector2 rawNow = _viewport.RawToLogical(Mouse.GetState().X, Mouse.GetState().Y);
            Vector2 delta = rawNow - _lastRawLogical;
            if (delta.LengthSquared() >= CapturedMouseDeadZone * CapturedMouseDeadZone)
            {
                _capturedLogicalMouse += delta;
                ClampCapturedLogicalMouse();
            }

            _lastRawLogical = rawNow;

            if (Vector2.DistanceSquared(rawNow, lockPosition) > CapturedMouseWarpDeadZone * CapturedMouseWarpDeadZone)
            {
                MoveMouseToLogical(lockPosition);
                _lastRawLogical = lockPosition;
            }

            if (_scene.MouseReleaseLogicalPosition.HasValue)
            {
                _mouseMovedToRelease = true;
            }
        }

        private void RestoreMouseState()
        {
            Game1.game1.IsMouseVisible = _previousMouseVisible;
            Game1.mouseCursor = _previousMouseCursor;
            _wasCapturingMouse = false;
            _mouseMovedToRelease = false;
        }

        private void PersistSceneState()
        {
            if (_scene is GameScene gameScene)
            {
                gameScene.SettleBalls();

                PoolTableSnapshot snapshot = gameScene.CreateSnapshot();
                _currentSnapshot = snapshot;
                if (IsWatchMode())
                {
                    _saveWatchSnapshot?.Invoke(snapshot);
                }
                else
                {
                    _saveSnapshot(snapshot);
                }
            }
        }

        private void MoveMouseToLogical(Vector2 logicalPosition)
        {
            Mouse.SetPosition(
                (int)MathF.Round(_viewport.TopLeft.X + logicalPosition.X * _viewport.Scale),
                (int)MathF.Round(_viewport.TopLeft.Y + logicalPosition.Y * _viewport.Scale)
            );
        }

        private void ClampCapturedLogicalMouse()
        {
            _capturedLogicalMouse = new Vector2(
                MathHelper.Clamp(_capturedLogicalMouse.X, 0, MinigameViewport.LogicalWidth - 1),
                MathHelper.Clamp(_capturedLogicalMouse.Y, 0, MinigameViewport.LogicalHeight - 1)
            );
        }

        private static Vector2 GetViewportCentreLogical()
        {
            return new Vector2(MinigameViewport.LogicalWidth / 2f, MinigameViewport.LogicalHeight / 2f);
        }

        private static string Format(Vector2 position)
        {
            return $"{{X:{position.X:0.##} Y:{position.Y:0.##}}}";
        }
    }
}
