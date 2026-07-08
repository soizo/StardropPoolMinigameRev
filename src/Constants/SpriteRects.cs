namespace StardropPoolMinigameRev.Constants
{
    internal static class SpriteRects
    {
        public const int TileSize = 16;

        public static class Environment
        {
            public static readonly Microsoft.Xna.Framework.Rectangle BarShelves = new(0, 0, 400, 128);
            public static readonly Microsoft.Xna.Framework.Rectangle FloorTiles = new(256, 128, 64, 64);
            public static readonly Microsoft.Xna.Framework.Rectangle GameTitle = new(0, 128, 128, 80);
        }

        public static class Ball
        {
            public static readonly Microsoft.Xna.Framework.Rectangle Highlight = new(368, 176, 16, 16);

            public static class Base
            {
                public static readonly Microsoft.Xna.Framework.Rectangle White = new(320, 144, 16, 16);
                public static readonly Microsoft.Xna.Framework.Rectangle Yellow = new(336, 144, 16, 16);
                public static readonly Microsoft.Xna.Framework.Rectangle Blue = new(352, 144, 16, 16);
                public static readonly Microsoft.Xna.Framework.Rectangle Red = new(368, 144, 16, 16);
                public static readonly Microsoft.Xna.Framework.Rectangle Orange = new(320, 160, 16, 16);
                public static readonly Microsoft.Xna.Framework.Rectangle Maroon = new(352, 160, 16, 16);
            }
        }
    }
}
