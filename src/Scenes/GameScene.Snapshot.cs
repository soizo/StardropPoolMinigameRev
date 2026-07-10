using Microsoft.Xna.Framework;

namespace StardropPoolMinigameRev.Scenes
{
	internal sealed partial class GameScene
	{
		public PoolTableSnapshot CreateSnapshot()
		{
			PoolTableSnapshot snapshot = new()
			{
				Shots = _shots,
				Pocketed = _pocketed,
				ActivePlayerIndex = _activePlayerIndex
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

			foreach (PocketedBallEntry entry in _pocketedBallEntries)
			{
				snapshot.PocketedBalls.Add(new PoolPocketedBallSnapshot
				{
					BallIndex = entry.BallIndex,
					OwnerIndex = entry.OwnerIndex
				});
			}

			return snapshot;
		}
	}
}
