using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardropPoolMinigameRev.Assets;
using StardropPoolMinigameRev.Constants;
using StardropPoolMinigameRev.Rendering;

namespace StardropPoolMinigameRev.Scenes
{
	internal sealed partial class GameScene
	{
		private static void DrawBackground(SpriteBatch batch, StardropPoolAssets assets)
		{
			Rectangle floor = SpriteRects.Environment.FloorTiles;
			for (int y = 0; y < MinigameViewport.LogicalHeight; y += floor.Height)
			{
				int height = Math.Min(floor.Height, MinigameViewport.LogicalHeight - y);
				for (int x = 0; x < MinigameViewport.LogicalWidth; x += floor.Width)
				{
					int width = Math.Min(floor.Width, MinigameViewport.LogicalWidth - x);
					Rectangle source = new(floor.X, floor.Y, width, height);
					batch.Draw(assets.Tilesheet, new Rectangle(x, y, width, height), source, Color.White);
				}
			}
		}

		private static void DrawTableBack(SpriteBatch batch, StardropPoolAssets assets)
		{
			DrawTableLayer(batch, assets, TableBackSources, drawFelt: false);
			DrawTopBottomMiddlePockets(batch, assets, back: true);
		}

		private static void DrawTableFront(SpriteBatch batch, StardropPoolAssets assets)
		{
			DrawTableLayer(batch, assets, TableFrontSources, drawFelt: false);
			DrawTopBottomMiddlePockets(batch, assets, back: false);
		}

		private static void DrawFeltSurface(SpriteBatch batch, StardropPoolAssets assets)
		{
			Rectangle source = SpriteRects.Environment.Felt;
			for (int y = FeltTop; y < FeltBottom; y += source.Height)
			{
				int height = Math.Min(source.Height, FeltBottom - y);
				for (int x = FeltLeft; x < FeltRight; x += source.Width)
				{
					int width = Math.Min(source.Width, FeltRight - x);
					Rectangle clippedSource = new(source.X, source.Y, width, height);
					batch.Draw(assets.Tilesheet, new Rectangle(x, y, width, height), clippedSource, Color.White);
				}
			}
		}

		private static void DrawTableLayer(SpriteBatch batch, StardropPoolAssets assets, Rectangle[,] sources, bool drawFelt)
		{
			for (int row = 0; row < TableRows; row++)
			{
				for (int column = 0; column < TableColumns; column++)
				{
					Rectangle source = sources[row, column];
					if (!drawFelt && source == SpriteRects.Environment.Felt)
					{
						continue;
					}

					Rectangle destination = new(
						TableLeft + column * TableSegmentSize,
						TableTop + row * TableSegmentSize,
						source.Width,
						source.Height
					);
					batch.Draw(assets.Tilesheet, destination, source, Color.White);
				}
			}
		}

		private static void DrawTopBottomMiddlePockets(SpriteBatch batch, StardropPoolAssets assets, bool back)
		{
			Rectangle north = back ? SpriteRects.Environment.Pocket.Back.North : SpriteRects.Environment.Pocket.Front.North;
			Rectangle south = back ? SpriteRects.Environment.Pocket.Back.South : SpriteRects.Environment.Pocket.Front.South;
			Rectangle northEdge = back ? SpriteRects.Environment.Edge.Back.North : SpriteRects.Environment.Edge.Front.North;
			Rectangle southEdge = back ? SpriteRects.Environment.Edge.Back.South : SpriteRects.Environment.Edge.Front.South;
			Rectangle northInset = new(north.X + 1, north.Y, Math.Max(0, north.Width - 2), north.Height);
			Rectangle southInset = new(south.X + 1, south.Y, Math.Max(0, south.Width - 2), south.Height);

			DrawTopBottomPocketEdgeCaps(batch, assets, northEdge, southEdge);
			batch.Draw(assets.Tilesheet, new Rectangle(PocketMiddleX - northInset.Width / 2, TableTop, northInset.Width, northInset.Height), northInset, Color.White);
			batch.Draw(assets.Tilesheet, new Rectangle(PocketMiddleX - southInset.Width / 2, TableTop + TableHeight - southInset.Height, southInset.Width, southInset.Height), southInset, Color.White);
		}

		private static void DrawTopBottomPocketEdgeCaps(SpriteBatch batch, StardropPoolAssets assets, Rectangle northEdge, Rectangle southEdge)
		{
			int skippedLeft = TableLeft + (TableColumns / 2 - 1) * TableSegmentSize;
			int skippedRight = TableLeft + (TableColumns / 2 + 1) * TableSegmentSize;
			int pocketLeft = PocketMiddleX - TableSegmentSize / 2;
			int pocketRight = PocketMiddleX + TableSegmentSize / 2;
			int leftWidth = Math.Max(0, pocketLeft - skippedLeft + TopBottomPocketStraightEdgeWidth);
			int rightWidth = Math.Max(0, skippedRight - pocketRight + TopBottomPocketStraightEdgeWidth);

			if (leftWidth > 0)
			{
				batch.Draw(assets.Tilesheet, new Rectangle(skippedLeft, TableTop, leftWidth, northEdge.Height), new Rectangle(northEdge.X, northEdge.Y, leftWidth, northEdge.Height), Color.White);
				batch.Draw(assets.Tilesheet, new Rectangle(skippedLeft, TableTop + TableHeight - southEdge.Height, leftWidth, southEdge.Height), new Rectangle(southEdge.X, southEdge.Y, leftWidth, southEdge.Height), Color.White);
			}

			if (rightWidth > 0)
			{
				batch.Draw(assets.Tilesheet, new Rectangle(pocketRight - TopBottomPocketStraightEdgeWidth, TableTop, rightWidth, northEdge.Height), new Rectangle(northEdge.X + northEdge.Width - rightWidth, northEdge.Y, rightWidth, northEdge.Height), Color.White);
				batch.Draw(assets.Tilesheet, new Rectangle(pocketRight - TopBottomPocketStraightEdgeWidth, TableTop + TableHeight - southEdge.Height, rightWidth, southEdge.Height), new Rectangle(southEdge.X + southEdge.Width - rightWidth, southEdge.Y, rightWidth, southEdge.Height), Color.White);
			}
		}

		private static Rectangle[,] CreateTableSources(bool back)
		{
			Rectangle[,] sources = new Rectangle[TableRows, TableColumns];
			Rectangle felt = SpriteRects.Environment.Felt;
			int lastColumn = TableColumns - 1;
			int lastRow = TableRows - 1;
			int firstMiddleColumn = TableColumns / 2 - 1;
			int secondMiddleColumn = TableColumns / 2;

			for (int row = 0; row < TableRows; row++)
			{
				for (int column = 0; column < TableColumns; column++)
				{
					sources[row, column] = felt;
				}
			}

			sources[0, 0] = back ? SpriteRects.Environment.Pocket.Back.NorthWest : SpriteRects.Environment.Pocket.Front.NorthWest;
			sources[0, lastColumn] = back ? SpriteRects.Environment.Pocket.Back.NorthEast : SpriteRects.Environment.Pocket.Front.NorthEast;
			sources[lastRow, 0] = back ? SpriteRects.Environment.Pocket.Back.SouthWest : SpriteRects.Environment.Pocket.Front.SouthWest;
			sources[lastRow, lastColumn] = back ? SpriteRects.Environment.Pocket.Back.SouthEast : SpriteRects.Environment.Pocket.Front.SouthEast;

			for (int column = 1; column < lastColumn; column++)
			{
				if (column == firstMiddleColumn || column == secondMiddleColumn)
				{
					continue;
				}

				sources[0, column] = back ? SpriteRects.Environment.Edge.Back.North : SpriteRects.Environment.Edge.Front.North;
				sources[lastRow, column] = back ? SpriteRects.Environment.Edge.Back.South : SpriteRects.Environment.Edge.Front.South;
			}

			for (int row = 1; row < lastRow; row++)
			{
				sources[row, 0] = back ? SpriteRects.Environment.Edge.Back.West : SpriteRects.Environment.Edge.Front.West;
				sources[row, lastColumn] = back ? SpriteRects.Environment.Edge.Back.East : SpriteRects.Environment.Edge.Front.East;
			}

			return sources;
		}

		private void DrawBalls(SpriteBatch batch, StardropPoolAssets assets)
		{
			foreach (PoolBall ball in _balls)
			{
				if (!ball.IsPocketed)
				{
					DrawBall(batch, assets, ball);
				}
			}
		}

		private void DrawCueStick(SpriteBatch batch, StardropPoolAssets assets)
		{
			PoolBall? cueBall = GetCueBall();
			if (cueBall == null)
			{
				return;
			}

			Vector2 direction;
			float powerRatio;
			float strikeProgress = 0f;
			Vector2 cueBallPosition = cueBall.Position;
			if (_isCueStriking)
			{
				direction = _strikeDirection;
				powerRatio = _strikePowerRatio;
				strikeProgress = (float)Math.Min(1, _strikeMilliseconds / _strikeDurationMilliseconds);
				cueBallPosition = _strikeCueBallPosition;
			}
			else if (_showCueAfterStrike)
			{
				direction = _strikeDirection;
				powerRatio = _strikePowerRatio;
				strikeProgress = 1f;
				cueBallPosition = _strikeCueBallPosition;
			}
			else if (_isAiming)
			{
				Vector2 pull = _aimPosition - _aimStartPosition;
				float pullDistance = pull.Length();
				if (pullDistance < 1f)
				{
					return;
				}

				float effectivePull = Math.Max(0f, pullDistance - BallCollisionRadius);
				powerRatio = Math.Min(effectivePull, MaxPullDistance) / MaxPullDistance;
				direction = -Vector2.Normalize(pull);
			}
			else
			{
				return;
			}

			float pullBackDistance = CueStickPullDistance * powerRatio;
			float strikeOffset;
			if (_isCueStriking)
			{
				if (strikeProgress < CueStrikeContactPoint)
				{
					float t = strikeProgress / CueStrikeContactPoint;
					strikeOffset = MathHelper.Lerp(pullBackDistance, -CueStickTipOvershoot, t * t);
				}
				else
				{
					if (!_hasCueStruckBall)
					{
						ApplyCueStrike();
					}

					float t = (strikeProgress - CueStrikeContactPoint) / (1f - CueStrikeContactPoint);
					float eased = 1f - (1f - t) * (1f - t);
					strikeOffset = MathHelper.Lerp(-CueStickTipOvershoot, CueStickRestOffset, eased);
				}
			}
			else if (_showCueAfterStrike)
			{
				strikeOffset = CueStickRestOffset;
			}
			else
			{
				strikeOffset = pullBackDistance;
			}

			Vector2 cuePosition = cueBallPosition - direction * (CueStickTouchDistance + strikeOffset);
			float rotation = MathF.Atan2(direction.Y, direction.X);
			Vector2 origin = new(CueStickOriginX, CueStickOriginY);
			SpriteEffects effects = SpriteEffects.FlipHorizontally;
			batch.Draw(
				assets.Tilesheet,
				cuePosition + new Vector2(1.5f, 2f),
				GetSelectedCueSource(),
				Color.Black * 0.35f,
				rotation,
				origin,
				1f,
				effects,
				1f
			);
			batch.Draw(
				assets.Tilesheet,
				cuePosition,
				GetSelectedCueSource(),
				Color.White,
				rotation,
				origin,
				1f,
				effects,
				1f
			);
		}
	}
}
