using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Minigames;
using StardropPoolMinigameRev.Assets;
using StardropPoolMinigameRev.Rendering;
using StardropPoolMinigameRev.Scenes;

namespace StardropPoolMinigameRev
{
    internal sealed class StardropPoolMinigameRev : IMinigame
    {
        private readonly StardropPoolAssets _assets;
        private readonly IMinigameScene _scene;
        private readonly IMonitor _monitor;
        private readonly MinigameViewport _viewport;

        public StardropPoolMinigameRev(IModHelper helper, IMonitor monitor)
        {
            _monitor = monitor;
            _monitor.Log("Creating Stardrop Pool rewrite minigame.", LogLevel.Info);

            _viewport = new MinigameViewport();
            _viewport.Update();

            _assets = new StardropPoolAssets(helper, _monitor);
            _assets.Load();

            _scene = new MainMenuScene(_monitor);

            _monitor.Log($"Stardrop Pool rewrite minigame ready. Viewport scale {_viewport.Scale}, top-left {_viewport.TopLeft}.", LogLevel.Info);
        }

        public bool tick(GameTime time)
        {
            _viewport.Update();
            _scene.Update(time);
            return false;
        }

        public void draw(SpriteBatch batch)
        {
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
        }

        public void releaseLeftClick(int x, int y)
        {
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
