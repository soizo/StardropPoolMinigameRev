using Microsoft.Xna.Framework;
using StardewValley;

namespace StardropPoolMinigameRev.Rendering
{
    internal sealed class MinigameViewport
    {
        public const int LogicalWidth = 400;
        public const int LogicalHeight = 224;

        public float Scale { get; private set; } = 4f;

        public Vector2 TopLeft { get; private set; } = Vector2.Zero;

        public Matrix Transform { get; private set; } = Matrix.Identity;

        public void Update()
        {
            int viewportWidth = Game1.game1.localMultiplayerWindow.Width;
            int viewportHeight = Game1.game1.localMultiplayerWindow.Height;

            Scale = MathF.Max(1f, MathF.Min(
                viewportWidth / (float)LogicalWidth,
                viewportHeight / (float)LogicalHeight
            ));

            TopLeft = new Vector2(
                MathF.Floor((viewportWidth - LogicalWidth * Scale) / 2f),
                MathF.Floor((viewportHeight - LogicalHeight * Scale) / 2f)
            );

            Transform = Matrix.CreateScale(Scale, Scale, 1f)
                * Matrix.CreateTranslation(TopLeft.X, TopLeft.Y, 0f);
        }

        public Vector2 RawToLogical(int x, int y)
        {
            return new Vector2(
                (x - TopLeft.X) / Scale,
                (y - TopLeft.Y) / Scale
            );
        }

        public bool ContainsLogical(Vector2 point)
        {
            return point.X >= 0
                && point.Y >= 0
                && point.X < LogicalWidth
                && point.Y < LogicalHeight;
        }
    }
}
