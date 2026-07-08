using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardropPoolMinigameRev.Assets;
using StardropPoolMinigameRev.Constants;
using StardropPoolMinigameRev.Rendering;

namespace StardropPoolMinigameRev.Scenes
{
    internal interface IMinigameScene
    {
        void Update(GameTime time);

        void Draw(SpriteBatch batch, MinigameViewport viewport, StardropPoolAssets assets);

        void ReceiveLeftClick(Vector2 logicalPosition);

        void ReceiveRightClick(Vector2 logicalPosition);

        void ReceiveKeyPress(Keys key);
    }

    internal sealed class MainMenuScene : IMinigameScene
    {
        private static readonly MenuItem[] Items =
        {
            new("Play", SpriteRects.Ball.Base.Yellow),
            new("Multiplayer", SpriteRects.Ball.Base.Blue),
            new("Gallery", SpriteRects.Ball.Base.Red),
            new("Settings", SpriteRects.Ball.Base.Yellow)
        };

        private readonly IMonitor _monitor;
        private Vector2? _lastClick;

        public MainMenuScene(IMonitor monitor)
        {
            _monitor = monitor;
        }

        public void Update(GameTime time)
        {
        }

        public void Draw(SpriteBatch batch, MinigameViewport viewport, StardropPoolAssets assets)
        {
            DrawBackground(batch, assets);
            DrawTitle(batch, assets);
            DrawButtons(batch, assets);

            if (_lastClick.HasValue)
            {
                DrawDebugPoint(batch, _lastClick.Value, Color.GreenYellow);
            }
        }

        public void ReceiveLeftClick(Vector2 logicalPosition)
        {
            _lastClick = logicalPosition;
            _monitor.Log($"Main menu left click at logical {Format(logicalPosition)}.", LogLevel.Info);
        }

        public void ReceiveRightClick(Vector2 logicalPosition)
        {
            _lastClick = logicalPosition;
            _monitor.Log($"Main menu right click at logical {Format(logicalPosition)}.", LogLevel.Info);
        }

        public void ReceiveKeyPress(Keys key)
        {
            _monitor.Log($"Main menu key press: {key}.", LogLevel.Info);
        }

        private static void DrawBackground(SpriteBatch batch, StardropPoolAssets assets)
        {
            batch.Draw(Game1.staminaRect, new Rectangle(0, 0, MinigameViewport.LogicalWidth, MinigameViewport.LogicalHeight), Game1.staminaRect.Bounds, new Color(5, 3, 4));
            batch.Draw(assets.Tilesheet, new Rectangle(0, 0, 400, 128), SpriteRects.Environment.BarShelves, Color.White);

            Rectangle floor = SpriteRects.Environment.FloorTiles;
            for (int y = 128; y < MinigameViewport.LogicalHeight; y += floor.Height)
            {
                for (int x = 0; x < MinigameViewport.LogicalWidth; x += floor.Width)
                {
                    batch.Draw(assets.Tilesheet, new Rectangle(x, y, floor.Width, floor.Height), floor, Color.White);
                }
            }
        }

        private static void DrawTitle(SpriteBatch batch, StardropPoolAssets assets)
        {
            batch.Draw(assets.Tilesheet, new Rectangle(136, 6, 128, 80), SpriteRects.Environment.GameTitle, Color.White);
        }

        private static void DrawButtons(SpriteBatch batch, StardropPoolAssets assets)
        {
            const int buttonWidth = 84;
            const int buttonHeight = 13;
            const int startY = 168;
            const int gap = 4;
            int x = 204;

            for (int i = 0; i < Items.Length; i++)
            {
                int y = startY + i * (buttonHeight + gap);
                DrawButton(batch, assets, Items[i], new Rectangle(x, y, buttonWidth, buttonHeight));
            }
        }

        private static void DrawButton(SpriteBatch batch, StardropPoolAssets assets, MenuItem item, Rectangle bounds)
        {
            Rectangle shadow = new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width, bounds.Height);
            batch.Draw(Game1.staminaRect, shadow, Game1.staminaRect.Bounds, Color.Black * 0.35f);
            batch.Draw(Game1.staminaRect, bounds, Game1.staminaRect.Bounds, new Color(42, 24, 32));
            batch.Draw(Game1.staminaRect, new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2), Game1.staminaRect.Bounds, new Color(91, 53, 65));
            batch.Draw(assets.Tilesheet, new Rectangle(bounds.X - 16, bounds.Y - 1, 16, 16), item.BallSource, Color.White);
            batch.Draw(assets.Tilesheet, new Rectangle(bounds.X - 16, bounds.Y - 1, 16, 16), SpriteRects.Ball.Highlight, Color.White * 0.65f);
            DrawCentredText(batch, Game1.smallFont, item.Label, new Vector2(bounds.Center.X, bounds.Y + 1), Color.White, 0.34f, shadow: true);
        }

        private static void DrawCentredText(SpriteBatch batch, SpriteFont font, string text, Vector2 position, Color colour, float scale, bool shadow)
        {
            Vector2 size = font.MeasureString(text) * scale;
            Vector2 origin = new Vector2(size.X / 2f, 0f);
            Vector2 drawPosition = position - origin;

            if (shadow)
            {
                batch.DrawString(font, text, drawPosition + new Vector2(1, 1), Color.Black * 0.75f, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
            }

            batch.DrawString(font, text, drawPosition, colour, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
        }

        private static void DrawDebugPoint(SpriteBatch batch, Vector2 point, Color colour)
        {
            batch.Draw(
                Game1.staminaRect,
                new Rectangle((int)MathF.Round(point.X) - 1, (int)MathF.Round(point.Y) - 1, 3, 3),
                Game1.staminaRect.Bounds,
                colour
            );
        }

        private static string Format(Vector2 position)
        {
            return $"{{X:{position.X:0.##} Y:{position.Y:0.##}}}";
        }

        private readonly record struct MenuItem(string Label, Rectangle BallSource);
    }
}
