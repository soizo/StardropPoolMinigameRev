using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Minigames;
using StardropPoolMinigameRev.Assets;
using StardropPoolMinigameRev.Constants;
using StardropPoolMinigameRev.Rendering;
using StardropPoolMinigameRev.Scenes;

namespace StardropPoolMinigameRev
{
    internal sealed class StardropPoolMinigameRev : IMinigame
    {
        private readonly StardropPoolAssets _assets;
        private IMinigameScene _scene;
        private readonly IMonitor _monitor;
        private readonly MinigameViewport _viewport;
        private readonly string? _previousMusicTrack;

        public StardropPoolMinigameRev(IModHelper helper, IMonitor monitor)
        {
            _monitor = monitor;
            _monitor.Log("Creating Stardrop Pool rewrite minigame.", LogLevel.Info);

            _viewport = new MinigameViewport();
            _viewport.Update();

            _assets = new StardropPoolAssets(helper, _monitor);
            _assets.Load();

            _scene = new MainMenuScene(_monitor);
            _previousMusicTrack = Game1.currentSong?.Name;
            Game1.changeMusicTrack("movieTheater");

            _monitor.Log($"Stardrop Pool rewrite minigame ready. Viewport scale {_viewport.Scale}, top-left {_viewport.TopLeft}.", LogLevel.Info);
        }

        public bool tick(GameTime time)
        {
            _viewport.Update();
            _scene.Update(time);

            if (_scene.PendingTransition != SceneId.None)
            {
                TransitionTo(_scene.PendingTransition);
            }

            return false;
        }

        private void TransitionTo(SceneId target)
        {
            switch (target)
            {
                case SceneId.Game:
                    _monitor.Log("Transitioning to game scene.", LogLevel.Info);
                    _scene = new GameScene(_monitor);
                    break;
                case SceneId.MainMenu:
                    _monitor.Log("Transitioning to main menu.", LogLevel.Info);
                    _scene = new MainMenuScene(_monitor);
                    break;
            }
        }

        public void draw(SpriteBatch batch)
        {
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
            Vector2 logical = _viewport.RawToLogical(x, y);
            _monitor.Log($"Left click raw {{X:{x} Y:{y}}} -> logical {Format(logical)}.", LogLevel.Info);

            if (_viewport.ContainsLogical(logical))
            {
                _scene.ReceiveLeftClick(logical);
            }
        }

        public void leftClickHeld(int x, int y)
        {
            Vector2 logical = _viewport.RawToLogical(x, y);

            if (_viewport.ContainsLogical(logical))
            {
                _scene.LeftClickHeld(logical);
            }
        }

        public void releaseLeftClick(int x, int y)
        {
            Vector2 logical = _viewport.RawToLogical(x, y);

            if (_viewport.ContainsLogical(logical))
            {
                _scene.ReleaseLeftClick(logical);
            }
        }

        public void receiveRightClick(int x, int y, bool playSound = true)
        {
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

            if (key == Keys.Escape)
            {
                forceQuit();
                return;
            }

            _scene.ReceiveKeyPress(key);
        }

        public void receiveKeyRelease(Keys key)
        {
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
            unload();
            Game1.currentMinigame = null;
            return true;
        }

        private static string Format(Vector2 position)
        {
            return $"{{X:{position.X:0.##} Y:{position.Y:0.##}}}";
        }
    }
}
