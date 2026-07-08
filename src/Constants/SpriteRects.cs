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
            public static readonly Microsoft.Xna.Framework.Rectangle Felt = new(352, 176, 16, 16);
            public static class Pocket
            {
                public static class Back
                {
                    public static readonly Microsoft.Xna.Framework.Rectangle NorthWest = new(128, 208, 32, 32);
                    public static readonly Microsoft.Xna.Framework.Rectangle North = new(256, 208, 32, 32);
                    public static readonly Microsoft.Xna.Framework.Rectangle NorthEast = new(192, 208, 32, 32);
                    public static readonly Microsoft.Xna.Framework.Rectangle SouthWest = new(128, 240, 32, 32);
                    public static readonly Microsoft.Xna.Framework.Rectangle South = new(256, 240, 32, 32);
                    public static readonly Microsoft.Xna.Framework.Rectangle SouthEast = new(192, 240, 32, 32);
                }

                public static class Front
                {
                    public static readonly Microsoft.Xna.Framework.Rectangle NorthWest = new(160, 208, 32, 32);
                    public static readonly Microsoft.Xna.Framework.Rectangle North = new(288, 208, 32, 32);
                    public static readonly Microsoft.Xna.Framework.Rectangle NorthEast = new(224, 208, 32, 32);
                    public static readonly Microsoft.Xna.Framework.Rectangle SouthWest = new(160, 240, 32, 32);
                    public static readonly Microsoft.Xna.Framework.Rectangle South = new(288, 240, 32, 32);
                    public static readonly Microsoft.Xna.Framework.Rectangle SouthEast = new(224, 240, 32, 32);
                }
            }
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
                public static readonly Microsoft.Xna.Framework.Rectangle Purple = new(384, 144, 16, 16);
                public static readonly Microsoft.Xna.Framework.Rectangle Orange = new(320, 160, 16, 16);
                public static readonly Microsoft.Xna.Framework.Rectangle Green = new(336, 160, 16, 16);
                public static readonly Microsoft.Xna.Framework.Rectangle Maroon = new(352, 160, 16, 16);
                public static readonly Microsoft.Xna.Framework.Rectangle Black = new(368, 160, 16, 16);
            }
        }
    }
}
