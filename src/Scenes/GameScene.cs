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
		private const int TableSegmentSize = 32;
		private const int TableColumns = 12;
		private const int TableRows = 6;
		private const int TableLeft = (MinigameViewport.LogicalWidth - TableColumns * TableSegmentSize) / 2;
		private const int TableTop = 16;
		private const int TableWidth = TableColumns * TableSegmentSize;
		private const int TableHeight = TableRows * TableSegmentSize;
		private const int TableCollisionInset = 16;
		private const int FeltLeft = TableLeft + TableSegmentSize;
		private const int FeltTop = TableTop + TableSegmentSize;
		private const int FeltWidth = (TableColumns - 2) * TableSegmentSize;
		private const int FeltHeight = (TableRows - 2) * TableSegmentSize;
		private const int CollisionLeft = TableLeft + TableCollisionInset;
		private const int CollisionTop = TableTop + TableCollisionInset;
		private const int CollisionRight = TableLeft + TableWidth - TableCollisionInset;
		private const int CollisionBottom = TableTop + TableHeight - TableCollisionInset;
		private const int CollisionCentreX = CollisionLeft + (CollisionRight - CollisionLeft) / 2;
		private const int CollisionCentreY = CollisionTop + (CollisionBottom - CollisionTop) / 2;
		private const int PocketWestX = TableLeft + TableSegmentSize / 2;
		private const int PocketEastX = TableLeft + (TableColumns - 1) * TableSegmentSize + TableSegmentSize / 2;
		private const int PocketNorthY = TableTop + TableSegmentSize / 2;
		private const int PocketSouthY = TableTop + (TableRows - 1) * TableSegmentSize + TableSegmentSize / 2;
		private const int FlatPocketBorderOffset = 0;
		private const int VerticalPocketStraightEdgeHeight = 1;
		private const int PocketMiddleX = TableLeft + TableWidth / 2 + FlatPocketBorderOffset;
		private const int PocketMiddleY = TableTop + TableHeight / 2 + VerticalPocketStraightEdgeHeight;
		private const int FeltRight = FeltLeft + FeltWidth;
		private const int FeltBottom = FeltTop + FeltHeight;
		private const int BallSize = 16;
		private const float BallCollisionRadius = 5.5f;
		private const int RackStepX = 12;
		private const int RackStepY = 12;
		private const float AimGrabRadius = 18f;
		private const float MaxPullDistance = 72f;
		private const float ShotPower = 7.5f;
		private const float FrictionPerSecond = 150f;
		private const float WallRestitution = 0.92f;
		private const float BallRestitution = 0.96f;
		private const float StopSpeed = 5f;
		private const float PocketRadius = 8f;

		private static readonly Color AimLineColour = new(255, 238, 209);
		private static readonly Color AimLineShadowColour = new(25, 11, 16);

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

		private static readonly int[] RackBallOrder = { 8, 11, 6, 0, 7, 14, 13, 2, 9, 5, 4, 3, 12, 1, 10 };

		private static readonly Rectangle[,] TableBackSources = CreateTableSources(back: true);
		private static readonly Rectangle[,] TableFrontSources = CreateTableSources(back: false);

		private static readonly Vector2 CueBallStart = new(CollisionLeft + (CollisionRight - CollisionLeft) * 0.25f, CollisionCentreY);

		private readonly IMonitor _monitor;
		private readonly List<PoolBall> _balls = new();
		private bool _isAiming;
		private Vector2 _aimPosition;
		private int _shots;
		private int _pocketed;
		private double _scratchMessageMilliseconds;

		public GameScene(IMonitor monitor)
		{
			_monitor = monitor;
			ResetRack();
		}

		public SceneId PendingTransition { get; private set; }

		public void Update(GameTime time)
		{
			float dt = Math.Min((float)time.ElapsedGameTime.TotalSeconds, 1f / 30f);
			if (_scratchMessageMilliseconds > 0)
			{
				_scratchMessageMilliseconds = Math.Max(0, _scratchMessageMilliseconds - time.ElapsedGameTime.TotalMilliseconds);
			}

			if (!AreBallsMoving())
			{
				return;
			}

			foreach (PoolBall ball in _balls)
			{
				if (ball.IsPocketed)
				{
					continue;
				}

				Vector2 movement = ball.Velocity * dt;
				ball.Position += movement;
				ball.Roll(movement);
				ApplyFriction(ball, dt);
				ResolveWallCollision(ball);
			}

			ResolveBallCollisions();
			ResolvePockets();
		}

		public void Draw(SpriteBatch batch, MinigameViewport viewport, StardropPoolAssets assets)
		{
			DrawBackground(batch, assets);
			DrawFeltSurface(batch, assets);
			DrawTableBack(batch, assets);
			DrawBalls(batch, assets);
			DrawTableFront(batch, assets);
			DrawAim(batch);
			DrawHud(batch);
		}

		public void ReceiveLeftClick(Vector2 logicalPosition)
		{
			PoolBall? cueBall = GetCueBall();
			if (cueBall == null || AreBallsMoving())
			{
				return;
			}

			if (Vector2.Distance(cueBall.Position, logicalPosition) <= AimGrabRadius)
			{
				_isAiming = true;
				_aimPosition = logicalPosition;
			}
		}

		public void LeftClickHeld(Vector2 logicalPosition)
		{
			if (_isAiming)
			{
				_aimPosition = logicalPosition;
			}
		}

		public void ReleaseLeftClick(Vector2 logicalPosition)
		{
			if (!_isAiming)
			{
				return;
			}

			_aimPosition = logicalPosition;
			_isAiming = false;
			ShootCueBall();
		}

		public void ReceiveRightClick(Vector2 logicalPosition)
		{
		}

		public void ReceiveKeyPress(Keys key)
		{
			_monitor.Log($"Game scene key press: {key}.", LogLevel.Info);
		}

		private void ResetRack()
		{
			_balls.Clear();
			_balls.Add(new PoolBall(SpriteRects.Ball.Base.White, CueBallStart, isCueBall: true, isStriped: false, isHighlighted: false));

			Vector2 apexCentre = new(CollisionLeft + (CollisionRight - CollisionLeft) * 0.7f, CollisionCentreY);
			int index = 0;
			for (int row = 0; row < 5; row++)
			{
				int ballsInRow = row + 1;
				float rowCentreX = apexCentre.X + row * RackStepX;
				float startY = apexCentre.Y - (ballsInRow - 1) * RackStepY * 0.5f;

				for (int slot = 0; slot < ballsInRow; slot++)
				{
					int ballIndex = RackBallOrder[index];
					Vector2 centre = new(rowCentreX, startY + slot * RackStepY);
					_balls.Add(new PoolBall(RackBallSources[ballIndex], centre, isCueBall: false, isStriped: ballIndex >= 8, isHighlighted: false));
					index++;
				}
			}
		}

		private void ShootCueBall()
		{
			PoolBall? cueBall = GetCueBall();
			if (cueBall == null)
			{
				return;
			}

			Vector2 pull = _aimPosition - cueBall.Position;
			float pullDistance = pull.Length();
			if (pullDistance < 4f)
			{
				return;
			}

			float clampedPull = Math.Min(pullDistance, MaxPullDistance);
			Vector2 direction = -Vector2.Normalize(pull);
			cueBall.Velocity = direction * clampedPull * ShotPower;
			_shots++;
			Game1.playSound("thudStep");
		}

		private static void ApplyFriction(PoolBall ball, float dt)
		{
			float speed = ball.Velocity.Length();
			if (speed <= 0)
			{
				return;
			}

			speed = Math.Max(0, speed - FrictionPerSecond * dt);
			if (speed < StopSpeed)
			{
				ball.Velocity = Vector2.Zero;
				return;
			}

			ball.Velocity = Vector2.Normalize(ball.Velocity) * speed;
		}

		private static void ResolveWallCollision(PoolBall ball)
		{
			if (ball.Position.X - BallCollisionRadius < CollisionLeft)
			{
				ball.Position = new Vector2(CollisionLeft + BallCollisionRadius, ball.Position.Y);
				ball.Velocity = new Vector2(Math.Abs(ball.Velocity.X) * WallRestitution, ball.Velocity.Y);
				Game1.playSound("thudStep");
			}
			else if (ball.Position.X + BallCollisionRadius > CollisionRight)
			{
				ball.Position = new Vector2(CollisionRight - BallCollisionRadius, ball.Position.Y);
				ball.Velocity = new Vector2(-Math.Abs(ball.Velocity.X) * WallRestitution, ball.Velocity.Y);
				Game1.playSound("thudStep");
			}

			if (ball.Position.Y - BallCollisionRadius < CollisionTop)
			{
				ball.Position = new Vector2(ball.Position.X, CollisionTop + BallCollisionRadius);
				ball.Velocity = new Vector2(ball.Velocity.X, Math.Abs(ball.Velocity.Y) * WallRestitution);
				Game1.playSound("thudStep");
			}
			else if (ball.Position.Y + BallCollisionRadius > CollisionBottom)
			{
				ball.Position = new Vector2(ball.Position.X, CollisionBottom - BallCollisionRadius);
				ball.Velocity = new Vector2(ball.Velocity.X, -Math.Abs(ball.Velocity.Y) * WallRestitution);
				Game1.playSound("thudStep");
			}
		}

		private void ResolveBallCollisions()
		{
			float minDistance = BallCollisionRadius * 2f;
			float minDistanceSquared = minDistance * minDistance;

			for (int i = 0; i < _balls.Count; i++)
			{
				PoolBall first = _balls[i];
				if (first.IsPocketed)
				{
					continue;
				}

				for (int j = i + 1; j < _balls.Count; j++)
				{
					PoolBall second = _balls[j];
					if (second.IsPocketed)
					{
						continue;
					}

					Vector2 delta = second.Position - first.Position;
					float distanceSquared = delta.LengthSquared();
					if (distanceSquared <= 0 || distanceSquared >= minDistanceSquared)
					{
						continue;
					}

					float distance = MathF.Sqrt(distanceSquared);
					Vector2 normal = delta / distance;
					float overlap = minDistance - distance;
					first.Position -= normal * (overlap / 2f);
					second.Position += normal * (overlap / 2f);

					Vector2 relativeVelocity = second.Velocity - first.Velocity;
					float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);
					if (velocityAlongNormal > 0)
					{
						continue;
					}

					float impulseMagnitude = -(1f + BallRestitution) * velocityAlongNormal / 2f;
					Vector2 impulse = impulseMagnitude * normal;
					first.Velocity -= impulse;
					second.Velocity += impulse;
					Game1.playSound("stoneStep");
				}
			}
		}

		private void ResolvePockets()
		{
			foreach (PoolBall ball in _balls)
			{
				if (ball.IsPocketed || !IsInPocket(ball.Position))
				{
					continue;
				}

				ball.Velocity = Vector2.Zero;
				if (ball.IsCueBall)
				{
					ball.Position = CueBallStart;
					_scratchMessageMilliseconds = 1500;
					Game1.playSound("cancel");
				}
				else
				{
					ball.IsPocketed = true;
					_pocketed++;
					Game1.playSound("coin");
				}
			}
		}

		private static bool IsInPocket(Vector2 position)
		{
			return Vector2.Distance(position, new Vector2(PocketWestX, PocketNorthY)) <= PocketRadius
				|| Vector2.Distance(position, new Vector2(PocketMiddleX, PocketNorthY)) <= PocketRadius
				|| Vector2.Distance(position, new Vector2(PocketEastX, PocketNorthY)) <= PocketRadius
				|| Vector2.Distance(position, new Vector2(PocketWestX, PocketSouthY)) <= PocketRadius
				|| Vector2.Distance(position, new Vector2(PocketMiddleX, PocketSouthY)) <= PocketRadius
				|| Vector2.Distance(position, new Vector2(PocketEastX, PocketSouthY)) <= PocketRadius
				|| Vector2.Distance(position, new Vector2(PocketWestX, PocketMiddleY)) <= PocketRadius
				|| Vector2.Distance(position, new Vector2(PocketEastX, PocketMiddleY)) <= PocketRadius;
		}

		private bool AreBallsMoving()
		{
			return _balls.Any(ball => !ball.IsPocketed && ball.Velocity.LengthSquared() > StopSpeed * StopSpeed);
		}

		private PoolBall? GetCueBall()
		{
			return _balls.FirstOrDefault(ball => ball.IsCueBall);
		}

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
			DrawCentredMiddlePockets(batch, assets, back: true);
		}

		private static void DrawTableFront(SpriteBatch batch, StardropPoolAssets assets)
		{
			DrawTableLayer(batch, assets, TableFrontSources, drawFelt: false);
			DrawCentredMiddlePockets(batch, assets, back: false);
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

		private static void DrawCentredMiddlePockets(SpriteBatch batch, StardropPoolAssets assets, bool back)
		{
			Rectangle north = back ? SpriteRects.Environment.Pocket.Back.North : SpriteRects.Environment.Pocket.Front.North;
			Rectangle south = back ? SpriteRects.Environment.Pocket.Back.South : SpriteRects.Environment.Pocket.Front.South;
			Rectangle west = back ? SpriteRects.Environment.Pocket.Back.West : SpriteRects.Environment.Pocket.Front.West;
			Rectangle east = back ? SpriteRects.Environment.Pocket.Back.East : SpriteRects.Environment.Pocket.Front.East;

			DrawMiddlePocketEdgeCaps(batch, assets, back);

			batch.Draw(assets.Tilesheet, new Rectangle(PocketMiddleX - north.Width / 2, TableTop, north.Width, north.Height), north, Color.White);
			batch.Draw(assets.Tilesheet, new Rectangle(PocketMiddleX - south.Width / 2, TableTop + TableHeight - south.Height, south.Width, south.Height), south, Color.White);
			batch.Draw(assets.Tilesheet, new Rectangle(TableLeft, PocketMiddleY - west.Height / 2, west.Width, west.Height), west, Color.White);
			batch.Draw(assets.Tilesheet, new Rectangle(TableLeft + TableWidth - east.Width, PocketMiddleY - east.Height / 2, east.Width, east.Height), east, Color.White);
		}

		private static void DrawMiddlePocketEdgeCaps(SpriteBatch batch, StardropPoolAssets assets, bool back)
		{
			Rectangle north = back ? SpriteRects.Environment.Edge.Back.North : SpriteRects.Environment.Edge.Front.North;
			Rectangle south = back ? SpriteRects.Environment.Edge.Back.South : SpriteRects.Environment.Edge.Front.South;
			Rectangle west = back ? SpriteRects.Environment.Edge.Back.West : SpriteRects.Environment.Edge.Front.West;
			Rectangle east = back ? SpriteRects.Environment.Edge.Back.East : SpriteRects.Environment.Edge.Front.East;

			int skippedLeft = TableLeft + (TableColumns / 2 - 1) * TableSegmentSize;
			int skippedRight = TableLeft + (TableColumns / 2 + 1) * TableSegmentSize;
			int pocketLeft = PocketMiddleX - TableSegmentSize / 2;
			int pocketRight = PocketMiddleX + TableSegmentSize / 2;
			int leftWidth = Math.Max(0, pocketLeft - skippedLeft);
			int rightWidth = Math.Max(0, skippedRight - pocketRight);

			int skippedTop = TableTop + (TableRows / 2 - 1) * TableSegmentSize;
			int skippedBottom = TableTop + (TableRows / 2 + 1) * TableSegmentSize;
			int pocketTop = PocketMiddleY - TableSegmentSize / 2;
			int pocketBottom = PocketMiddleY + TableSegmentSize / 2;
			int topHeight = Math.Max(0, pocketTop - skippedTop);
			int bottomHeight = Math.Max(0, skippedBottom - pocketBottom);

			if (leftWidth > 0)
			{
				batch.Draw(assets.Tilesheet, new Rectangle(skippedLeft, TableTop, leftWidth, north.Height), new Rectangle(north.X, north.Y, leftWidth, north.Height), Color.White);
				batch.Draw(assets.Tilesheet, new Rectangle(skippedLeft, TableTop + TableHeight - south.Height, leftWidth, south.Height), new Rectangle(south.X, south.Y, leftWidth, south.Height), Color.White);
			}

			if (rightWidth > 0)
			{
				batch.Draw(assets.Tilesheet, new Rectangle(pocketRight, TableTop, rightWidth, north.Height), new Rectangle(north.X + north.Width - rightWidth, north.Y, rightWidth, north.Height), Color.White);
				batch.Draw(assets.Tilesheet, new Rectangle(pocketRight, TableTop + TableHeight - south.Height, rightWidth, south.Height), new Rectangle(south.X + south.Width - rightWidth, south.Y, rightWidth, south.Height), Color.White);
			}

			if (topHeight > 0)
			{
				batch.Draw(assets.Tilesheet, new Rectangle(TableLeft, skippedTop, west.Width, topHeight), new Rectangle(west.X, west.Y, west.Width, topHeight), Color.White);
				batch.Draw(assets.Tilesheet, new Rectangle(TableLeft + TableWidth - east.Width, skippedTop, east.Width, topHeight), new Rectangle(east.X, east.Y, east.Width, topHeight), Color.White);
			}

			if (bottomHeight > 0)
			{
				batch.Draw(assets.Tilesheet, new Rectangle(TableLeft, pocketBottom, west.Width, bottomHeight), new Rectangle(west.X, west.Y + west.Height - bottomHeight, west.Width, bottomHeight), Color.White);
				batch.Draw(assets.Tilesheet, new Rectangle(TableLeft + TableWidth - east.Width, pocketBottom, east.Width, bottomHeight), new Rectangle(east.X, east.Y + east.Height - bottomHeight, east.Width, bottomHeight), Color.White);
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
			int firstMiddleRow = TableRows / 2 - 1;
			int secondMiddleRow = TableRows / 2;

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
				if (row == firstMiddleRow || row == secondMiddleRow)
				{
					continue;
				}

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

		private void DrawAim(SpriteBatch batch)
		{
			PoolBall? cueBall = GetCueBall();
			if (!_isAiming || cueBall == null || AreBallsMoving())
			{
				return;
			}

			Vector2 pull = _aimPosition - cueBall.Position;
			if (pull.LengthSquared() < 1f)
			{
				return;
			}

			float distance = Math.Min(pull.Length(), MaxPullDistance);
			Vector2 direction = -Vector2.Normalize(pull);
			Vector2 end = cueBall.Position + direction * distance;
			DrawLine(batch, cueBall.Position, end, AimLineShadowColour, 3);
			DrawLine(batch, cueBall.Position, end, AimLineColour, 1);
		}

		private void DrawHud(SpriteBatch batch)
		{
			string text = $"Shots: {_shots}  Pocketed: {_pocketed}";
			batch.DrawString(Game1.smallFont, text, new Vector2(8, 6), Color.Black * 0.75f, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 1f);
			batch.DrawString(Game1.smallFont, text, new Vector2(7, 5), new Color(255, 238, 209), 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 1f);

			if (_scratchMessageMilliseconds > 0)
			{
				string scratch = "Scratch!";
				Vector2 size = Game1.dialogueFont.MeasureString(scratch) * 0.35f;
				Vector2 position = new((MinigameViewport.LogicalWidth - size.X) / 2f, 8);
				batch.DrawString(Game1.dialogueFont, scratch, position + new Vector2(1, 1), Color.Black * 0.8f, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 1f);
				batch.DrawString(Game1.dialogueFont, scratch, position, Color.OrangeRed, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 1f);
			}
		}

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

		private sealed class PoolBall
		{
			public PoolBall(Rectangle source, Vector2 position, bool isCueBall, bool isStriped, bool isHighlighted)
			{
				Source = source;
				Position = position;
				IsCueBall = isCueBall;
				IsStriped = isStriped;
				IsHighlighted = isHighlighted;
			}

			public Rectangle Source { get; }

			public Vector2 Position { get; set; }

			public Vector2 Velocity { get; set; }

			public bool IsCueBall { get; }

			public bool IsStriped { get; }

			public bool IsHighlighted { get; }

			public Vector2 OrientationFace => GetOrientationFace();

			public bool IsPocketed { get; set; }

			public void Roll(Vector2 movement)
			{
				if (movement.LengthSquared() <= 0)
				{
					return;
				}

				float circumference = MathF.PI * BallCollisionRadius * 2f;
				Orientation = new Vector2(
					Orientation.X + movement.X / circumference * 360f,
					Orientation.Y + movement.Y / circumference * 360f
				);
				LimitOrientation();
			}

			private Vector2 Orientation { get; set; }

			private Vector2 GetOrientationFace()
			{
				float latitude = MathF.Round(Orientation.Y / 30f) * 30f;
				float longitudeDiff = Math.Abs(latitude) == 60f ? 45f : 30f;
				float longitude = MathF.Round(Orientation.X / longitudeDiff) * longitudeDiff;

				return new Vector2(longitude == 180f ? 0f : longitude, latitude);
			}

			private void LimitOrientation()
			{
				float latitudeChanges = 0f;
				float simplifiedLatitude = MathF.Round(Orientation.Y / 30f) * 30f;
				while (simplifiedLatitude > 90f)
				{
					simplifiedLatitude -= 180f;
					latitudeChanges -= 180f;
				}

				while (simplifiedLatitude < -90f)
				{
					simplifiedLatitude += 180f;
					latitudeChanges += 180f;
				}

				float longitudeDiff = Math.Abs(simplifiedLatitude) == 60f ? 45f : 30f;
				float longitudeChanges = 0f;
				float simplifiedLongitude = MathF.Round(Orientation.X / longitudeDiff) * longitudeDiff;
				float maxLongitude = Math.Abs(simplifiedLatitude) == 60f ? 135f : 150f;
				while (simplifiedLongitude > maxLongitude)
				{
					simplifiedLongitude -= 180f;
					longitudeChanges -= 180f;
				}

				while (simplifiedLongitude <= 0f)
				{
					simplifiedLongitude += 180f;
					longitudeChanges += 180f;
				}

				if (latitudeChanges != 0f || longitudeChanges != 0f)
				{
					Orientation = new Vector2(Orientation.X + longitudeChanges, Orientation.Y + latitudeChanges);
				}
			}
		}
	}
}
