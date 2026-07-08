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
    internal sealed class GameScene : IMinigameScene
    {
        private const int RailThickness = 16;
        private const int FeltLeft = 40;
        private const int FeltTop = 32;
        private const int FeltWidth = 320;
        private const int FeltHeight = 160;
        private const int FeltRight = FeltLeft + FeltWidth;
        private const int FeltBottom = FeltTop + FeltHeight;
        private const int FeltCentreX = FeltLeft + FeltWidth / 2;
        private const int FeltCentreY = FeltTop + FeltHeight / 2;
        private const int BallSize = 16;
        private const int RackStepX = 15;
        private const int RackStepY = 16;

        private static readonly Color RailColour = new(54, 32, 22);
        private static readonly Color RailHighlightColour = new(86, 56, 38);
        private static readonly Color RailShadowColour = new(30, 17, 12);
        private static readonly Color CushionColour = new(28, 84, 56);

        private static readonly Rectangle[] RackBallSources =
        {
            SpriteRects.Ball.Base.Yellow,
            SpriteRects.Ball.Base.Blue,
            SpriteRects.Ball.Base.Red,
            SpriteRects.Ball.Base.Purple,
            SpriteRects.Ball.Base.Orange,
            SpriteRects.Ball.Base.Green,
            SpriteRects.Ball.Base.Maroon,
            SpriteRects.Ball.Base.Black,
            SpriteRects.Ball.Base.Yellow,
            SpriteRects.Ball.Base.Blue,
            SpriteRects.Ball.Base.Red,
            SpriteRects.Ball.Base.Purple,
            SpriteRects.Ball.Base.Orange,
            SpriteRects.Ball.Base.Green,
            SpriteRects.Ball.Base.Maroon
        };

        private readonly IMonitor _monitor;

        public GameScene(IMonitor monitor)
        {
            _monitor = monitor;
        }

        public SceneId PendingTransition { get; private set; }

        public void Update(GameTime time)
        {
        }

        public void Draw(SpriteBatch batch, MinigameViewport viewport, StardropPoolAssets assets)
        {
            DrawBackground(batch);
            DrawTable(batch, assets);
            DrawBalls(batch, assets);
        }

        public void ReceiveLeftClick(Vector2 logicalPosition)
        {
            _monitor.Log($"Game scene left click at logical {{{logicalPosition.X:0.##},{logicalPosition.Y:0.##}}}.", LogLevel.Info);
        }

        public void ReleaseLeftClick(Vector2 logicalPosition)
        {
        }

        public void ReceiveRightClick(Vector2 logicalPosition)
        {
            _monitor.Log($"Game scene right click at logical {{{logicalPosition.X:0.##},{logicalPosition.Y:0.##}}}.", LogLevel.Info);
        }

        public void ReceiveKeyPress(Keys key)
        {
            _monitor.Log($"Game scene key press: {key}.", LogLevel.Info);
        }

        private static void DrawBackground(SpriteBatch batch)
        {
            batch.Draw(Game1.staminaRect, new Rectangle(0, 0, MinigameViewport.LogicalWidth, MinigameViewport.LogicalHeight), Game1.staminaRect.Bounds, new Color(5, 3, 4));
        }

        private static void DrawTable(SpriteBatch batch, StardropPoolAssets assets)
        {
            Rectangle tableBounds = new(FeltLeft - RailThickness, FeltTop - RailThickness, FeltWidth + RailThickness * 2, FeltHeight + RailThickness * 2);

            batch.Draw(Game1.staminaRect, tableBounds, Game1.staminaRect.Bounds, RailColour);
            batch.Draw(Game1.staminaRect, new Rectangle(tableBounds.X, tableBounds.Y, tableBounds.Width, 2), Game1.staminaRect.Bounds, RailHighlightColour);
            batch.Draw(Game1.staminaRect, new Rectangle(tableBounds.X, tableBounds.Y, 2, tableBounds.Height), Game1.staminaRect.Bounds, RailHighlightColour);
            batch.Draw(Game1.staminaRect, new Rectangle(tableBounds.X, tableBounds.Bottom - 2, tableBounds.Width, 2), Game1.staminaRect.Bounds, RailShadowColour);
            batch.Draw(Game1.staminaRect, new Rectangle(tableBounds.Right - 2, tableBounds.Y, 2, tableBounds.Height), Game1.staminaRect.Bounds, RailShadowColour);

            DrawFelt(batch, assets);
            DrawCushions(batch);
            DrawPockets(batch, assets);
        }

        private static void DrawFelt(SpriteBatch batch, StardropPoolAssets assets)
        {
            Rectangle feltSource = SpriteRects.Environment.Felt;
            for (int y = FeltTop; y < FeltBottom; y += feltSource.Height)
            {
                int height = Math.Min(feltSource.Height, FeltBottom - y);
                for (int x = FeltLeft; x < FeltRight; x += feltSource.Width)
                {
                    int width = Math.Min(feltSource.Width, FeltRight - x);
                    Rectangle source = new(feltSource.X, feltSource.Y, width, height);
                    batch.Draw(assets.Tilesheet, new Rectangle(x, y, width, height), source, Color.White);
                }
            }
        }

        private static void DrawCushions(SpriteBatch batch)
        {
            int cushionThickness = 4;

            // Top and bottom cushions run between the corner pockets, leaving room for the side pockets.
            int horizontalCushionLeft = FeltLeft + 16;
            int horizontalCushionRight = FeltRight - 16;
            batch.Draw(Game1.staminaRect, new Rectangle(horizontalCushionLeft, FeltTop, horizontalCushionRight - horizontalCushionLeft, cushionThickness), Game1.staminaRect.Bounds, CushionColour);
            batch.Draw(Game1.staminaRect, new Rectangle(horizontalCushionLeft, FeltBottom - cushionThickness, horizontalCushionRight - horizontalCushionLeft, cushionThickness), Game1.staminaRect.Bounds, CushionColour);

            // Left and right cushions run between the corner pockets, leaving room for the top/bottom pockets.
            int verticalCushionTop = FeltTop + 16;
            int verticalCushionBottom = FeltBottom - 16;
            batch.Draw(Game1.staminaRect, new Rectangle(FeltLeft, verticalCushionTop, cushionThickness, verticalCushionBottom - verticalCushionTop), Game1.staminaRect.Bounds, CushionColour);
            batch.Draw(Game1.staminaRect, new Rectangle(FeltRight - cushionThickness, verticalCushionTop, cushionThickness, verticalCushionBottom - verticalCushionTop), Game1.staminaRect.Bounds, CushionColour);
        }

        private static void DrawPockets(SpriteBatch batch, StardropPoolAssets assets)
        {
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.NorthWest, new Point(FeltLeft - 16, FeltTop - 16));
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.North, new Point(FeltCentreX - 16, FeltTop - 16));
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.NorthEast, new Point(FeltRight - 16, FeltTop - 16));
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.SouthWest, new Point(FeltLeft - 16, FeltBottom - 16));
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.South, new Point(FeltCentreX - 16, FeltBottom - 16));
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.SouthEast, new Point(FeltRight - 16, FeltBottom - 16));
        }

        private static void DrawPocket(SpriteBatch batch, StardropPoolAssets assets, Rectangle source, Point topLeft)
        {
            batch.Draw(assets.Tilesheet, new Rectangle(topLeft.X, topLeft.Y, source.Width, source.Height), source, Color.White);
        }

        private static void DrawBalls(SpriteBatch batch, StardropPoolAssets assets)
        {
            // Cue ball on the left quarter line.
            Vector2 cueBallCentre = new(FeltLeft + FeltWidth * 0.25f, FeltCentreY);
            DrawBall(batch, assets, SpriteRects.Ball.Base.White, cueBallCentre);

            // 15-ball rack as a triangle whose apex points toward the cue ball.
            Vector2 apexCentre = new(FeltLeft + FeltWidth * 0.7f, FeltCentreY);
            int index = 0;
            for (int row = 0; row < 5; row++)
            {
                int ballsInRow = row + 1;
                float rowCentreX = apexCentre.X + row * RackStepX;
                float startY = apexCentre.Y - (ballsInRow - 1) * RackStepY * 0.5f;

                for (int slot = 0; slot < ballsInRow; slot++)
                {
                    Vector2 centre = new(rowCentreX, startY + slot * RackStepY);
                    DrawBall(batch, assets, RackBallSources[index], centre);
                    index++;
                }
            }
        }

        private static void DrawBall(SpriteBatch batch, StardropPoolAssets assets, Rectangle source, Vector2 centre)
        {
            Rectangle destination = new((int)MathF.Round(centre.X) - BallSize / 2, (int)MathF.Round(centre.Y) - BallSize / 2, BallSize, BallSize);
            batch.Draw(assets.Tilesheet, destination, source, Color.White);
            batch.Draw(assets.Tilesheet, destination, SpriteRects.Ball.Highlight, Color.White * 0.7f);
        }
    }
}
