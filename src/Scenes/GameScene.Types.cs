using Microsoft.Xna.Framework;
using StardewValley;

namespace StardropPoolMinigameRev.Scenes
{
	internal sealed partial class GameScene
	{
		private sealed record RowElement(string Id, RowElementType Type, Rectangle Source, Point Size, Action? OnActivate);

		private sealed record AvatarHudEntry(int PlayerIndex, Farmer? Farmer, NPC? Npc);

		private sealed record PocketedBallEntry(int BallIndex, int OwnerIndex)
		{
			public Vector2 Orientation { get; init; }

			public float VisualXOffset { get; set; }

			public float VisualXVelocity { get; set; }
		}

		private enum RowElementType
		{
			Button,
			Idle,
			Arrow,
			Space,
			FlexibleSpace,
			Avatar
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
