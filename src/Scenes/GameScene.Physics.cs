using Microsoft.Xna.Framework;
using StardewValley;
using StardropPoolMinigameRev.Constants;

namespace StardropPoolMinigameRev.Scenes
{
	internal sealed partial class GameScene
	{
		private void ResetRack()
		{
			_balls.Clear();
			_pocketedBallEntries.Clear();
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
					_balls.Add(new PoolBall(RackBallSources[ballIndex], centre, isCueBall: false, isStriped: ballIndex >= 8, isHighlighted: false, number: ballIndex + 1));
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
			_activePlayerIndex = MathHelper.Clamp(snapshot.ActivePlayerIndex, 0, GetAvatarHudEntries().Count - 1);
			_playerAssignedBallType = MathHelper.Clamp(snapshot.PlayerAssignedBallType, -1, 1);
			_pocketedBallEntries.Clear();
			for (int i = 0; i < snapshot.Balls.Count; i++)
			{
				PoolBallSnapshot ballSnapshot = snapshot.Balls[i];
				PoolBall ball = _balls[ballSnapshot.Index];
				ball.Position = new Vector2(ballSnapshot.PositionX, ballSnapshot.PositionY);
				ball.Velocity = new Vector2(ballSnapshot.VelocityX, ballSnapshot.VelocityY);
				ball.Orientation = new Vector2(ballSnapshot.OrientationX, ballSnapshot.OrientationY);
				ball.IsPocketed = ballSnapshot.IsPocketed;
			}

			foreach (PoolPocketedBallSnapshot pocketedBall in snapshot.PocketedBalls)
			{
				if (pocketedBall.BallIndex > 0 && pocketedBall.BallIndex < _balls.Count)
				{
					Vector2 orientation = new(pocketedBall.OrientationX, pocketedBall.OrientationY);
					if (orientation.LengthSquared() <= 0)
					{
						orientation = _balls[pocketedBall.BallIndex].Orientation;
					}

					_pocketedBallEntries.Add(new PocketedBallEntry(pocketedBall.BallIndex, MathHelper.Clamp(pocketedBall.OwnerIndex, 0, GetAvatarHudEntries().Count - 1))
					{
						Orientation = orientation
					});
				}
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

			cueBall.Velocity = _strikeDirection * _strikePowerRatio * MaxPullDistance * _activeShotPower;
			_activeShotPower = ShotPower;
			_currentShotScored = false;
			_isWaitingForShotToSettle = true;
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
					int ballIndex = _balls.IndexOf(ball);
					if (ballIndex > 0)
					{
						AssignBallTypeIfNeeded(ball);
						_currentShotScored = true;
						_pocketedBallEntries.Add(new PocketedBallEntry(ballIndex, _activePlayerIndex)
						{
							Orientation = ball.Orientation,
							VisualXVelocity = AvatarBallPocketPush
						});
					}
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
	}
}
