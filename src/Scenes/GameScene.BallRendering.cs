using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardropPoolMinigameRev.Assets;
using StardropPoolMinigameRev.Constants;

namespace StardropPoolMinigameRev.Scenes
{
	internal sealed partial class GameScene
	{
		private static void DrawBall(SpriteBatch batch, StardropPoolAssets assets, PoolBall ball)
		{
			Rectangle destination = new((int)MathF.Round(ball.Position.X) - BallSize / 2, (int)MathF.Round(ball.Position.Y) - BallSize / 2, BallSize, BallSize);
			batch.Draw(assets.Tilesheet, destination, ball.Source, Color.White);

			if (!ball.IsCueBall)
			{
				batch.Draw(assets.Tilesheet, destination, GetBallCoreSource(ball.OrientationFace), Color.White);
				if (ball.IsStriped)
				{
					batch.Draw(assets.Tilesheet, destination, GetBallStripeSource(ball.OrientationFace), Color.White);
				}
			}

			batch.Draw(assets.Tilesheet, destination, SpriteRects.Ball.Shadow, Color.White);
			if (ball.IsHighlighted)
			{
				batch.Draw(assets.Tilesheet, destination, SpriteRects.Ball.Highlight, Color.White);
			}
		}

		private static Rectangle GetBallCoreSource(Vector2 face)
		{
			int y = face.Y switch
			{
				90 => 0,
				60 => 1,
				30 => 2,
				0 => 3,
				-30 => 4,
				-60 => 5,
				_ => 3
			};

			int x = 25;
			if (Math.Abs(face.Y) == 60)
			{
				x += face.X switch
				{
					45 => 1,
					90 => 2,
					135 => 3,
					_ => 0
				};
			}
			else if (face.Y != 90)
			{
				x += face.X switch
				{
					30 => 1,
					60 => 2,
					90 => 3,
					120 => 4,
					150 => 5,
					_ => 0
				};
			}

			return new Rectangle(x * SpriteRects.TileSize, y * SpriteRects.TileSize, SpriteRects.TileSize, SpriteRects.TileSize);
		}

		private static Rectangle GetBallStripeSource(Vector2 face)
		{
			int y = face.Y switch
			{
				90 => 6,
				60 => 7,
				30 => 8,
				0 => 9,
				-30 => 10,
				-60 => 11,
				_ => 9
			};

			return new Rectangle(25 * SpriteRects.TileSize, y * SpriteRects.TileSize, SpriteRects.TileSize, SpriteRects.TileSize);
		}

		private static void DrawLine(SpriteBatch batch, Vector2 start, Vector2 end, Color colour, int thickness)
		{
			Vector2 edge = end - start;
			float angle = MathF.Atan2(edge.Y, edge.X);
			batch.Draw(Game1.staminaRect, start, Game1.staminaRect.Bounds, colour, angle, Vector2.Zero, new Vector2(edge.Length(), thickness), SpriteEffects.None, 1f);
		}
	}
}
