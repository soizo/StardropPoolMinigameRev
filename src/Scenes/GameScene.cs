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
		private const int TableTop = (MinigameViewport.LogicalHeight - TableHeight) / 2 + 7;
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
		private const int TopBottomPocketStraightEdgeWidth = 1;
		private const int PocketMiddleX = TableLeft + TableWidth / 2 + FlatPocketBorderOffset;
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
		private const float MinimumWallImpactSpeed = 12f;
		private const float MinimumBallImpactSpeed = 8f;
		private const float MediumImpactSpeed = 24f;
		private const float MinimumImpactVolume = 0.35f;
		private const float MaximumImpactVolume = 1f;
		private const float PocketRadius = 8f;
		private const float CueStickTouchDistance = BallCollisionRadius + 5f;
		private const float CueStickPullDistance = 32f;
		private const float CueStrikeMinimumMilliseconds = 150f;
		private const float CueStrikeMaximumMilliseconds = 300f;
		private const float CueStrikeContactPoint = 0.55f;
		private const float CueStickTipOvershoot = 3f;
		private const float CueStickRestOffset = 4f;
		private const float CueStickOriginX = 122f;
		private const float CueStickOriginY = 8f;
		private static readonly Point ButtonColumnOrigin = new(10, 6);
		private static readonly Point ButtonColumnItemSize = new(16, 16);
		private static readonly Point CuePreviewSize = new(38, 10);
		private static readonly Point ArrowSize = new(12, 11);
		private const int RowContainerHeight = 16;
		private const int ButtonColumnItemSpacing = 2;
		private const float ButtonHoverExpansion = 1.5f;
		private const float RowElementScaleStep = 0.02f;
		private const string RowResetId = "reset";
		private const string RowBackToMenuId = "backtomenu";
		private const string RowFlexibleSpaceId = "flexiblespace";
		private const string RowSpaceId = "space";
		private const string RowAvatarId = "avatar";
		private const string RowCueLeftId = "cue-left";
		private const string RowCuePreviewId = "cue-preview";
		private const string RowCueRightId = "cue-right";
		private static readonly Point AvatarSize = new(16, 16);
		private static readonly Point SpaceSize = new(8, 16);
		private static readonly string[] RowElementOrder =
		{
			RowBackToMenuId,
			RowResetId,
			RowFlexibleSpaceId,
			RowAvatarId,
			RowSpaceId,
			RowCueLeftId,
			RowCuePreviewId,
			RowCueRightId
		};

		private static readonly Color AimLineColour = new(255, 238, 209);
		private static readonly Color AimLineShadowColour = new(25, 11, 16);
		private static readonly Rectangle[] CueSources =
		{
			SpriteRects.Cue.Basic,
			SpriteRects.Cue.Sam,
			SpriteRects.Cue.Sebastian,
			SpriteRects.Cue.Abigail,
			SpriteRects.Cue.Gus
		};

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
		private readonly List<RowElement> _rowElements = new();
		private readonly Dictionary<int, float> _rowElementScales = new();
		private readonly bool _isSveInstalled;
		private MinigameViewport? _viewport;
		private bool _isGaldoraTheme;
		private bool _isPressingRowElement;
		private int _pressedRowElementIndex = -1;
		private int _selectedCueIndex;
		private bool _isAiming;
		private Vector2 _aimStartPosition;
		private Vector2 _aimPosition;
		private bool _isCueStriking;
		private bool _hasCueStruckBall;
		private bool _showCueAfterStrike;
		private Vector2 _strikeCueBallPosition;
		private Vector2 _strikeDirection;
		private float _strikePowerRatio;
		private double _strikeMilliseconds;
		private double _strikeDurationMilliseconds;
		private int _shots;
		private int _pocketed;
		private double _scratchMessageMilliseconds;

		public GameScene(IMonitor monitor, PoolTableSnapshot? snapshot, bool isSveInstalled)
		{
			_monitor = monitor;
			_isSveInstalled = isSveInstalled;
			_isGaldoraTheme = DetectGaldoraTheme();
			InitialiseRowElements();
			ResetRack();
			LoadSnapshot(snapshot);
		}

		public SceneId PendingTransition { get; private set; }

		public bool CapturesMouse => _isAiming || _isCueStriking || _showCueAfterStrike;

		public Vector2? MouseReleaseLogicalPosition => (_isCueStriking || _showCueAfterStrike) ? _strikeCueBallPosition : null;

		public void Update(GameTime time)
		{
			float dt = Math.Min((float)time.ElapsedGameTime.TotalSeconds, 1f / 30f);
			bool isGaldoraTheme = DetectGaldoraTheme();
			if (_isGaldoraTheme != isGaldoraTheme)
			{
				_isGaldoraTheme = isGaldoraTheme;
				InitialiseRowElements();
			}

			UpdateRowElementScales();
			if (_scratchMessageMilliseconds > 0)
			{
				_scratchMessageMilliseconds = Math.Max(0, _scratchMessageMilliseconds - time.ElapsedGameTime.TotalMilliseconds);
			}

			if (_isCueStriking)
			{
				_strikeMilliseconds += time.ElapsedGameTime.TotalMilliseconds;
				if (_strikeMilliseconds >= _strikeDurationMilliseconds)
				{
					FinishCueStrike();
				}
			}

			if (!AreBallsMoving())
			{
				_showCueAfterStrike = false;
				return;
			}

			StepPhysics(dt);
		}

		public void Draw(SpriteBatch batch, MinigameViewport viewport, StardropPoolAssets assets)
		{
			_viewport = viewport;
			DrawBackground(batch, assets);
			DrawFeltSurface(batch, assets);
			DrawTableBack(batch, assets);
			DrawBalls(batch, assets);
			DrawTableFront(batch, assets);
			DrawCueStick(batch, assets);
			DrawHud(batch, assets);
		}

		public void ReceiveLeftClick(Vector2 logicalPosition)
		{
			int rowElementIndex = GetRowElementIndexAt(logicalPosition);
			if (rowElementIndex >= 0)
			{
				RowElement rowElement = _rowElements[rowElementIndex];
				if (rowElement.Type == RowElementType.Button || rowElement.Type == RowElementType.Arrow)
				{
					_isPressingRowElement = true;
					_pressedRowElementIndex = rowElementIndex;
				}

				return;
			}

			PoolBall? cueBall = GetCueBall();
			if (cueBall == null || AreBallsMoving() || _isCueStriking || !IsWithinFelt(logicalPosition))
			{
				return;
			}

			_isAiming = true;
			_showCueAfterStrike = false;
			_aimStartPosition = logicalPosition;
			_aimPosition = logicalPosition;
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
			if (_isPressingRowElement)
			{
				int rowElementIndex = GetRowElementIndexAt(logicalPosition);
				if (rowElementIndex == _pressedRowElementIndex && rowElementIndex >= 0)
				{
					ActivateRowElement(_rowElements[rowElementIndex]);
				}

				_isPressingRowElement = false;
				_pressedRowElementIndex = -1;
				return;
			}

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

		public void SettleBalls()
		{
			_isAiming = false;
			_isCueStriking = false;
			_showCueAfterStrike = false;
			_hasCueStruckBall = false;
			_strikeMilliseconds = 0;
			_strikeDurationMilliseconds = 0;
			AdvanceUntilSettled();
		}

		public void ResetTable()
		{
			_isAiming = false;
			_isCueStriking = false;
			_showCueAfterStrike = false;
			_hasCueStruckBall = false;
			_strikeMilliseconds = 0;
			_strikeDurationMilliseconds = 0;
			_scratchMessageMilliseconds = 0;
			_shots = 0;
			_pocketed = 0;
			ResetRack();
		}

		public PoolTableSnapshot CreateSnapshot()
		{
			PoolTableSnapshot snapshot = new()
			{
				Shots = _shots,
				Pocketed = _pocketed
			};

			for (int i = 0; i < _balls.Count; i++)
			{
				PoolBall ball = _balls[i];
				snapshot.Balls.Add(new PoolBallSnapshot
				{
					Index = i,
					PositionX = ball.Position.X,
					PositionY = ball.Position.Y,
					VelocityX = ball.Velocity.X,
					VelocityY = ball.Velocity.Y,
					OrientationX = ball.Orientation.X,
					OrientationY = ball.Orientation.Y,
					IsPocketed = ball.IsPocketed
				});
			}

			return snapshot;
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

		private void LoadSnapshot(PoolTableSnapshot? snapshot)
		{
			if (snapshot == null || snapshot.Balls.Count != _balls.Count)
			{
				return;
			}

			foreach (PoolBallSnapshot ballSnapshot in snapshot.Balls)
			{
				if (ballSnapshot.Index < 0 || ballSnapshot.Index >= _balls.Count)
				{
					return;
				}
			}

			_shots = snapshot.Shots;
			_pocketed = snapshot.Pocketed;
			for (int i = 0; i < snapshot.Balls.Count; i++)
			{
				PoolBallSnapshot ballSnapshot = snapshot.Balls[i];
				PoolBall ball = _balls[ballSnapshot.Index];
				ball.Position = new Vector2(ballSnapshot.PositionX, ballSnapshot.PositionY);
				ball.Velocity = new Vector2(ballSnapshot.VelocityX, ballSnapshot.VelocityY);
				ball.Orientation = new Vector2(ballSnapshot.OrientationX, ballSnapshot.OrientationY);
				ball.IsPocketed = ballSnapshot.IsPocketed;
			}

			AdvanceUntilSettled();
		}

		private void AdvanceUntilSettled()
		{
			const int maxSteps = 7200;
			const float dt = 1f / 60f;

			for (int step = 0; step < maxSteps && AreBallsMoving(); step++)
			{
				StepPhysics(dt);
			}

			foreach (PoolBall ball in _balls)
			{
				ball.Velocity = Vector2.Zero;
			}
		}

		private void StepPhysics(float dt)
		{
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

		private void ShootCueBall()
		{
			PoolBall? cueBall = GetCueBall();
			if (cueBall == null)
			{
				return;
			}

			Vector2 pull = _aimPosition - _aimStartPosition;
			float pullDistance = pull.Length();
			if (pullDistance <= BallCollisionRadius)
			{
				return;
			}

			float effectivePull = Math.Min(pullDistance - BallCollisionRadius, MaxPullDistance);
			_strikeDirection = -Vector2.Normalize(pull);
			_strikeCueBallPosition = cueBall.Position;
			_strikePowerRatio = effectivePull / MaxPullDistance;
			_strikeDurationMilliseconds = MathHelper.Lerp(CueStrikeMaximumMilliseconds, CueStrikeMinimumMilliseconds, _strikePowerRatio);
			_strikeMilliseconds = 0;
			_hasCueStruckBall = false;
			_isCueStriking = true;
		}

		private void FinishCueStrike()
		{
			_isCueStriking = false;
			_showCueAfterStrike = true;
			if (!_hasCueStruckBall)
			{
				ApplyCueStrike();
			}
		}

		private void ApplyCueStrike()
		{
			if (_hasCueStruckBall)
			{
				return;
			}

			_hasCueStruckBall = true;
			PoolBall? cueBall = GetCueBall();
			if (cueBall == null)
			{
				return;
			}

			cueBall.Velocity = _strikeDirection * _strikePowerRatio * MaxPullDistance * ShotPower;
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
				float impactSpeed = Math.Abs(ball.Velocity.X);
				ball.Position = new Vector2(CollisionLeft + BallCollisionRadius, ball.Position.Y);
				ball.Velocity = new Vector2(Math.Abs(ball.Velocity.X) * WallRestitution, ball.Velocity.Y);
				if (impactSpeed >= MinimumWallImpactSpeed)
				{
					PlayImpactSound("thudStep", impactSpeed, MinimumWallImpactSpeed);
				}
			}
			else if (ball.Position.X + BallCollisionRadius > CollisionRight)
			{
				float impactSpeed = Math.Abs(ball.Velocity.X);
				ball.Position = new Vector2(CollisionRight - BallCollisionRadius, ball.Position.Y);
				ball.Velocity = new Vector2(-Math.Abs(ball.Velocity.X) * WallRestitution, ball.Velocity.Y);
				if (impactSpeed >= MinimumWallImpactSpeed)
				{
					PlayImpactSound("thudStep", impactSpeed, MinimumWallImpactSpeed);
				}
			}

			if (ball.Position.Y - BallCollisionRadius < CollisionTop)
			{
				float impactSpeed = Math.Abs(ball.Velocity.Y);
				ball.Position = new Vector2(ball.Position.X, CollisionTop + BallCollisionRadius);
				ball.Velocity = new Vector2(ball.Velocity.X, Math.Abs(ball.Velocity.Y) * WallRestitution);
				if (impactSpeed >= MinimumWallImpactSpeed)
				{
					PlayImpactSound("thudStep", impactSpeed, MinimumWallImpactSpeed);
				}
			}
			else if (ball.Position.Y + BallCollisionRadius > CollisionBottom)
			{
				float impactSpeed = Math.Abs(ball.Velocity.Y);
				ball.Position = new Vector2(ball.Position.X, CollisionBottom - BallCollisionRadius);
				ball.Velocity = new Vector2(ball.Velocity.X, -Math.Abs(ball.Velocity.Y) * WallRestitution);
				if (impactSpeed >= MinimumWallImpactSpeed)
				{
					PlayImpactSound("thudStep", impactSpeed, MinimumWallImpactSpeed);
				}
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
					if (impulseMagnitude >= MinimumBallImpactSpeed)
					{
						PlayImpactSound("stoneStep", impulseMagnitude, MinimumBallImpactSpeed);
					}
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
				|| Vector2.Distance(position, new Vector2(PocketEastX, PocketSouthY)) <= PocketRadius;
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








		private static float EaseOutBack(float progress)
		{
			const float overshoot = 1.35f;
			float shifted = progress - 1f;
			return 1f + shifted * shifted * ((overshoot + 1f) * shifted + overshoot);
		}

		private void DrawAim(SpriteBatch batch)
		{
			PoolBall? cueBall = GetCueBall();
			if (!_isAiming || cueBall == null || AreBallsMoving())
			{
				return;
			}

			Vector2 pull = _aimPosition - _aimStartPosition;
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

		private void DrawHud(SpriteBatch batch, StardropPoolAssets assets)
		{
			foreach (RowElement rowElement in _rowElements)
			{
				DrawRowElement(batch, assets, rowElement);
			}
		}

		private void InitialiseRowElements()
		{
			Dictionary<string, RowElement> elements = new()
			{
				[RowResetId] = new RowElement(RowResetId, RowElementType.Button, GetResetButtonSource(), ButtonColumnItemSize, ResetTable),
				[RowBackToMenuId] = new RowElement(RowBackToMenuId, RowElementType.Button, GetBackToMenuButtonSource(), ButtonColumnItemSize, ReturnToMainMenu),
				[RowFlexibleSpaceId] = new RowElement(RowFlexibleSpaceId, RowElementType.FlexibleSpace, Rectangle.Empty, Point.Zero, null),
				[RowSpaceId] = new RowElement(RowSpaceId, RowElementType.Space, Rectangle.Empty, SpaceSize, null),
				[RowAvatarId] = new RowElement(RowAvatarId, RowElementType.Avatar, Rectangle.Empty, AvatarSize, null),
				[RowCueLeftId] = new RowElement(RowCueLeftId, RowElementType.Arrow, SpriteRects.Ui.LeftArrow, ArrowSize, SelectPreviousCue),
				[RowCuePreviewId] = new RowElement(RowCuePreviewId, RowElementType.Idle, SpriteRects.Ui.ChibiCueStick, CuePreviewSize, null),
				[RowCueRightId] = new RowElement(RowCueRightId, RowElementType.Arrow, SpriteRects.Ui.RightArrow, ArrowSize, SelectNextCue)
			};

			_rowElements.Clear();
			foreach (string id in RowElementOrder)
			{
				if (elements.TryGetValue(id, out RowElement? element))
				{
					_rowElements.Add(element);
				}
			}

			_rowElementScales.Clear();
			for (int i = 0; i < _rowElements.Count; i++)
			{
				_rowElementScales[i] = 1f;
			}
		}

		private void UpdateRowElementScales()
		{
			Vector2 pointerLogicalPosition = GetCurrentPointerLogicalPosition();
			for (int i = 0; i < _rowElements.Count; i++)
			{
				RowElement rowElement = _rowElements[i];
				Rectangle bounds = GetRowElementBounds(rowElement);
				bool isHovered = rowElement.Type != RowElementType.FlexibleSpace && rowElement.Type != RowElementType.Space && rowElement.Type != RowElementType.Avatar && bounds.Contains(ToPoint(pointerLogicalPosition));
				bool isPressed = _isPressingRowElement && _pressedRowElementIndex == i;
				float targetScale = GetTargetRowElementScale(rowElement, isHovered, isPressed);
				float currentScale = _rowElementScales.TryGetValue(i, out float scale) ? scale : 1f;
				_rowElementScales[i] = Approach(currentScale, targetScale, RowElementScaleStep);
			}
		}

		private Vector2 GetCurrentPointerLogicalPosition()
		{
			MouseState mouse = Mouse.GetState();
			if (_viewport != null)
			{
				return _viewport.RawToLogical(mouse.X, mouse.Y);
			}

			return new Vector2(mouse.X, mouse.Y);
		}

		private Rectangle GetResetButtonSource()
		{
			return _isGaldoraTheme ? SpriteRects.Ui.ResetGaldora : SpriteRects.Ui.Reset;
		}

		private Rectangle GetBackToMenuButtonSource()
		{
			return _isGaldoraTheme ? SpriteRects.Ui.BackToMenuGaldora : SpriteRects.Ui.BackToMenu;
		}

		private void DrawRowElement(SpriteBatch batch, StardropPoolAssets assets, RowElement rowElement)
		{
			if (rowElement.Type == RowElementType.FlexibleSpace || rowElement.Type == RowElementType.Space)
			{
				return;
			}

			Rectangle bounds = GetRowElementBounds(rowElement);
			if (rowElement.Type == RowElementType.Avatar)
			{
				DrawAvatarRowElement(batch, bounds);
				return;
			}

			if (rowElement.Type == RowElementType.Idle && rowElement.Source == SpriteRects.Ui.ChibiCueStick)
			{
				DrawCuePreview(batch, assets, rowElement, bounds);
				return;
			}

			if (rowElement.Type == RowElementType.Arrow)
			{
				DrawArrowRowElement(batch, rowElement, bounds);
				return;
			}

			Color tint = GetRowElementTint(rowElement);
			float scale = GetRowElementScale(rowElement);
			Vector2 origin = new(rowElement.Source.Width / 2f, rowElement.Source.Height / 2f);
			Vector2 position = new(
				bounds.X + bounds.Width / 2f,
				bounds.Y + bounds.Height / 2f
			);
			batch.Draw(assets.Tilesheet, position, rowElement.Source, tint, 0f, origin, scale, SpriteEffects.None, 1f);
		}

		private void DrawArrowRowElement(SpriteBatch batch, RowElement rowElement, Rectangle bounds)
		{
			float scale = GetRowElementScale(rowElement);
			Vector2 origin = new(rowElement.Source.Width / 2f, rowElement.Source.Height / 2f);
			Vector2 position = new(
				bounds.X + bounds.Width / 2f,
				bounds.Y + bounds.Height / 2f
			);
			batch.Draw(Game1.mouseCursors, position, rowElement.Source, Color.White, 0f, origin, scale, SpriteEffects.None, 1f);
		}

		private static void DrawAvatarRowElement(SpriteBatch batch, Rectangle bounds)
		{
			if (Game1.player == null)
			{
				return;
			}

			Game1.player.FarmerRenderer.drawMiniPortrat(batch, new Vector2(bounds.X, bounds.Y - 3), 0.8f, 1f, 1, Game1.player);
		}

		private void DrawCuePreview(SpriteBatch batch, StardropPoolAssets assets, RowElement rowElement, Rectangle bounds)
		{
			Rectangle previewSource = new(
				rowElement.Source.X,
				rowElement.Source.Y + _selectedCueIndex * 2,
				rowElement.Source.Width,
				2
			);
			Rectangle destination = new(
				bounds.X,
				bounds.Y + (bounds.Height - previewSource.Height) / 2 - 1,
				bounds.Width,
				previewSource.Height
			);
			Rectangle shadowDestination = new(destination.X + 1, destination.Y + 1, destination.Width, destination.Height);
			batch.Draw(assets.Tilesheet, shadowDestination, previewSource, Color.Black * 0.35f);
			batch.Draw(assets.Tilesheet, destination, previewSource, Color.White);
		}

		private Color GetRowElementTint(RowElement rowElement)
		{
			if (rowElement.Type == RowElementType.Button && _isGaldoraTheme)
			{
				return new Color(255, 244, 214);
			}

			return Color.White;
		}

		private float GetRowElementScale(RowElement rowElement)
		{
			int index = _rowElements.IndexOf(rowElement);
			if (index < 0)
			{
				return 1f;
			}

			return _rowElementScales.TryGetValue(index, out float scale) ? scale : 1f;
		}

		private static float GetTargetRowElementScale(RowElement rowElement, bool isHovered, bool isPressed)
		{
			return rowElement.Type switch
			{
				RowElementType.Button when isHovered => 1f + ButtonHoverExpansion / rowElement.Size.X,
				RowElementType.Arrow when isPressed => Math.Max(0.1f, 1f - 1f / rowElement.Size.X),
				_ => 1f
			};
		}

		private int GetRowElementIndexAt(Vector2 logicalPosition)
		{
			Point point = ToPoint(logicalPosition);
			for (int i = 0; i < _rowElements.Count; i++)
			{
				if (_rowElements[i].Type == RowElementType.FlexibleSpace || _rowElements[i].Type == RowElementType.Space || _rowElements[i].Type == RowElementType.Avatar)
				{
					continue;
				}

				if (GetRowElementBounds(_rowElements[i]).Contains(point))
				{
					return i;
				}
			}

			return -1;
		}

		private void ActivateRowElement(RowElement rowElement)
		{
			rowElement.OnActivate?.Invoke();
			if (rowElement.Type == RowElementType.Button)
			{
				Game1.playSound("bigDeSelect");
			}
			else if (rowElement.Type == RowElementType.Arrow)
			{
				Game1.playSound("shwip");
			}
		}

		private Rectangle GetSelectedCueSource()
		{
			return CueSources[_selectedCueIndex];
		}

		private void SelectPreviousCue()
		{
			_selectedCueIndex = (_selectedCueIndex + CueSources.Length - 1) % CueSources.Length;
		}

		private void SelectNextCue()
		{
			_selectedCueIndex = (_selectedCueIndex + 1) % CueSources.Length;
		}

		private void ReturnToMainMenu()
		{
			PendingTransition = SceneId.MainMenu;
		}

		private bool DetectGaldoraTheme()
		{
			if (!_isSveInstalled)
			{
				return false;
			}

			const string sveConfigPath = "/Users/soizoktantas/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/Stardew Valley Expanded/[CP] Stardew Valley Expanded/config.json";
			try
			{
				if (!File.Exists(sveConfigPath))
				{
					return false;
				}

				string json = File.ReadAllText(sveConfigPath);
				return json.Contains("\"UseGaldoranThemeAllTimes\": \"true\"", StringComparison.OrdinalIgnoreCase)
					&& !json.Contains("\"DisableGaldoranTheme\": \"true\"", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		private static Point ToPoint(Vector2 logicalPosition)
		{
			return new Point((int)MathF.Floor(logicalPosition.X), (int)MathF.Floor(logicalPosition.Y));
		}

		private static bool IsWithinFelt(Vector2 logicalPosition)
		{
			return logicalPosition.X >= CollisionLeft
				&& logicalPosition.X < CollisionRight
				&& logicalPosition.Y >= CollisionTop
				&& logicalPosition.Y < CollisionBottom;
		}

		private Rectangle GetRowElementBounds(RowElement rowElement)
		{
			int spacerIndex = _rowElements.FindIndex(element => element.Type == RowElementType.FlexibleSpace);
			int rowElementIndex = _rowElements.IndexOf(rowElement);
			int leftX = ButtonColumnOrigin.X;
			int rightX = MinigameViewport.LogicalWidth - ButtonColumnOrigin.X;

			if (spacerIndex < 0 || rowElementIndex < spacerIndex)
			{
				for (int i = 0; i < rowElementIndex; i++)
				{
					RowElement element = _rowElements[i];
					if (element.Type != RowElementType.FlexibleSpace)
					{
						leftX += element.Size.X + ButtonColumnItemSpacing;
					}
				}

				return new Rectangle(leftX, ButtonColumnOrigin.Y + (RowContainerHeight - rowElement.Size.Y) / 2, rowElement.Size.X, rowElement.Size.Y);
			}

			for (int i = _rowElements.Count - 1; i > rowElementIndex; i--)
			{
				RowElement element = _rowElements[i];
				if (element.Type != RowElementType.FlexibleSpace)
				{
					rightX -= element.Size.X;
					rightX -= ButtonColumnItemSpacing;
				}
			}

			return new Rectangle(rightX - rowElement.Size.X, ButtonColumnOrigin.Y + (RowContainerHeight - rowElement.Size.Y) / 2, rowElement.Size.X, rowElement.Size.Y);
		}

		private static float Approach(float current, float target, float amount)
		{
			if (current < target)
			{
				return Math.Min(current + amount, target);
			}

			if (current > target)
			{
				return Math.Max(current - amount, target);
			}

			return target;
		}

		private static float GetImpactVolume(float impactSpeed, float minimumImpactSpeed)
		{
			float t = (impactSpeed - minimumImpactSpeed) / (MediumImpactSpeed - minimumImpactSpeed);
			return MathHelper.Lerp(MinimumImpactVolume, MaximumImpactVolume, MathHelper.Clamp(t, 0f, 1f));
		}

		private static void PlayImpactSound(string cueName, float impactSpeed, float minimumImpactSpeed)
		{
			float volume = GetImpactVolume(impactSpeed, minimumImpactSpeed);
			var cue = Game1.soundBank.GetCue(cueName);
			cue.SetVariable("Volume", MathHelper.Lerp(-12f, 0f, volume));
			cue.Play();
		}

		private sealed record RowElement(string Id, RowElementType Type, Rectangle Source, Point Size, Action? OnActivate);

		private enum RowElementType
		{
			Button,
			Idle,
			Arrow,
			Space,
			FlexibleSpace,
			Avatar
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

			public Vector2 Orientation { get; set; }

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
					Orientation.X - movement.X / circumference * 360f,
					Orientation.Y - movement.Y / circumference * 360f
				);
				LimitOrientation();
			}

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
