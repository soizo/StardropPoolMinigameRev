using Microsoft.Xna.Framework;
using StardropPoolMinigameRev;

namespace StardropPoolMinigameRev.Scenes
{
	internal sealed partial class GameScene
	{
		private const int NpcAiGenerations = 2;
		private const int NpcAiPopulationSize = 100;
		private const float NpcAiMutationProbability = 0.01f;
		private const float NpcAiCrossoverProbability = 0.2f;
		private const int NpcAiMatingPoolSize = NpcAiPopulationSize / 2;
		private const int NpcAiLowerBound = 0;
		private const int NpcAiUpperBound = 4000;
		private const int NpcAiBoundOffset = (NpcAiUpperBound - NpcAiLowerBound) / 2;
		private const float NpcAiMaximumVectorLength = 2000f;
		private const float NpcAiPocketedOwnBallScore = 2500f;
		private const float NpcAiPocketedWrongBallPenalty = 2500f;
		private const float NpcAiEarlyEightBallPenalty = 25000f;
		private const float NpcAiLegalEightBallScore = 25000f;
		private const float NpcAiScratchPenalty = 8000f;
		private const float NpcAiRayWidth = BallCollisionRadius * 2.2f;
		private const float NpcAiPocketLineWidth = BallCollisionRadius * 2.4f;
		private const float NpcAiCueBallTravelScale = 145f;
		private const float NpcAiTargetBallTravelScale = 120f;
		private const float NpcAiMinimumPowerRatio = 0.24f;
		private const float NpcAiMaximumPowerRatio = 0.95f;

		private readonly PoolRandom _npcAiRandom;

		private sealed record NpcShotCandidate(float X, float Y, float Fitness)
		{
			public Vector2 Direction => new Vector2(X, Y).LengthSquared() > 0 ? Vector2.Normalize(new Vector2(X, Y)) : Vector2.UnitX;

			public float PowerRatio => MathHelper.Clamp(new Vector2(X, Y).Length() / NpcAiMaximumVectorLength, NpcAiMinimumPowerRatio, NpcAiMaximumPowerRatio);
		}

		private NpcShotCandidate FindBestNpcShot()
		{
			List<NpcShotCandidate> population = GenerateInitialNpcPopulation();
			ScorePopulation(population);

			for (int generation = 0; generation < NpcAiGenerations; generation++)
			{
				population = CrossAndMutateNpcPopulation(population);
				ScorePopulation(population);
			}

			return population.MaxBy(candidate => candidate.Fitness) ?? new NpcShotCandidate(1000f, 0f, 0f);
		}

		private List<NpcShotCandidate> GenerateInitialNpcPopulation()
		{
			List<NpcShotCandidate> population = new(NpcAiPopulationSize);
			Vector2? seedDirection = GetSeedNpcDirection();
			for (int i = 0; i < NpcAiPopulationSize; i++)
			{
				if (seedDirection.HasValue && i < NpcAiPopulationSize / 4)
				{
					Vector2 direction = Rotate(seedDirection.Value, RandomFloat(-0.35f, 0.35f));
					float length = RandomFloat(NpcAiMaximumVectorLength * 0.35f, NpcAiMaximumVectorLength * 0.9f);
					population.Add(new NpcShotCandidate(direction.X * length, direction.Y * length, 0f));
					continue;
				}

				float x = RandomFloat(-NpcAiMaximumVectorLength, NpcAiMaximumVectorLength);
				float yLimit = MathF.Sqrt(Math.Max(0f, NpcAiMaximumVectorLength * NpcAiMaximumVectorLength - x * x));
				float y = RandomFloat(-yLimit, yLimit);
				population.Add(new NpcShotCandidate(x, y, 0f));
			}

			return population;
		}

		private Vector2? GetSeedNpcDirection()
		{
			PoolBall? cueBall = GetCueBall();
			if (cueBall == null)
			{
				return null;
			}

			PoolBall? targetBall = GetNpcTargetBalls().OrderBy(ball => Vector2.DistanceSquared(cueBall.Position, ball.Position)).FirstOrDefault();
			if (targetBall == null)
			{
				return null;
			}

			Vector2 direction = targetBall.Position - cueBall.Position;
			return direction.LengthSquared() > 1f ? Vector2.Normalize(direction) : null;
		}

		private void ScorePopulation(List<NpcShotCandidate> population)
		{
			for (int i = 0; i < population.Count; i++)
			{
				NpcShotCandidate candidate = population[i];
				population[i] = candidate with { Fitness = ScoreNpcShot(candidate) };
			}
		}

		private List<NpcShotCandidate> CrossAndMutateNpcPopulation(List<NpcShotCandidate> population)
		{
			List<NpcShotCandidate> matingPool = LinearRankSelection(population, NpcAiMatingPoolSize);
			List<NpcShotCandidate> nextPopulation = new(NpcAiPopulationSize);
			for (int i = 0; i < NpcAiPopulationSize; i++)
			{
				int firstIndex = _npcAiRandom.Next(matingPool.Count);
				int secondIndex;
				do
				{
					secondIndex = _npcAiRandom.Next(matingPool.Count);
				}
				while (secondIndex == firstIndex && matingPool.Count > 1);

				NpcShotCandidate child;
				if (RandomFloat(0f, 1f) <= NpcAiCrossoverProbability)
				{
					float ratio = RandomFloat(0f, 1f);
					NpcShotCandidate first = matingPool[firstIndex];
					NpcShotCandidate second = matingPool[secondIndex];
					child = new NpcShotCandidate(
						first.X + (second.X - first.X) * ratio,
						first.Y + (second.Y - first.Y) * ratio,
						0f
					);
				}
				else
				{
					NpcShotCandidate parent = matingPool[firstIndex];
					child = parent with { Fitness = 0f };
				}

				nextPopulation.Add(MutateNpcShot(child));
			}

			return nextPopulation;
		}

		private List<NpcShotCandidate> LinearRankSelection(List<NpcShotCandidate> population, int matingPoolSize)
		{
			List<NpcShotCandidate> sorted = population.OrderBy(candidate => candidate.Fitness).ToList();
			List<float> cumulativeWeights = new(sorted.Count);
			float cumulative = 0f;
			float total = sorted.Count * (sorted.Count + 1) / 2f;
			for (int i = 0; i < sorted.Count; i++)
			{
				cumulative += (i + 1) / total;
				cumulativeWeights.Add(cumulative);
			}

			List<NpcShotCandidate> matingPool = new(matingPoolSize);
			for (int i = 0; i < matingPoolSize; i++)
			{
				float roll = RandomFloat(0f, 1f);
				int selectedIndex = cumulativeWeights.FindIndex(weight => roll <= weight);
				matingPool.Add(sorted[Math.Max(0, selectedIndex)]);
			}

			return matingPool;
		}

		private NpcShotCandidate MutateNpcShot(NpcShotCandidate candidate)
		{
			float x = MutateComponent(candidate.X);
			float y = MutateComponent(candidate.Y);
			Vector2 vector = new(x, y);
			if (vector.LengthSquared() > NpcAiMaximumVectorLength * NpcAiMaximumVectorLength)
			{
				vector.Normalize();
				vector *= NpcAiMaximumVectorLength;
			}

			return new NpcShotCandidate(vector.X, vector.Y, 0f);
		}

		private float MutateComponent(float component)
		{
			int shifted = Math.Clamp((int)MathF.Round(component + NpcAiBoundOffset), NpcAiLowerBound + 1, NpcAiUpperBound - 1);
			int mutated;
			do
			{
				int grayCode = DecimalToGray(shifted);
				int mutatedGrayCode = MutateGrayCode(grayCode, NpcAiMutationProbability);
				mutated = GrayToDecimal(mutatedGrayCode);
			}
			while (mutated <= NpcAiLowerBound || mutated >= NpcAiUpperBound);

			return mutated - NpcAiBoundOffset;
		}

		private int MutateGrayCode(int grayCode, float mutationProbability)
		{
			int result = grayCode;
			for (int bit = 0; bit < 12; bit++)
			{
				if (RandomFloat(0f, 1f) <= mutationProbability)
				{
					result ^= 1 << bit;
				}
			}

			return result;
		}

		private static int DecimalToGray(int value)
		{
			return value ^ (value >> 1);
		}

		private static int GrayToDecimal(int value)
		{
			int result = 0;
			for (; value > 0; value >>= 1)
			{
				result ^= value;
			}

			return result;
		}

		private float ScoreNpcShot(NpcShotCandidate candidate)
		{
			PoolBall? cueBall = GetCueBall();
			if (cueBall == null)
			{
				return float.MinValue;
			}

			Vector2 direction = candidate.Direction;
			float powerRatio = candidate.PowerRatio;
			Vector2 cueEnd = cueBall.Position + direction * NpcAiCueBallTravelScale * powerRatio;
			float score = -DistanceToNearestPocket(cueEnd) * 0.15f;
			if (IsPocketPath(cueBall.Position, cueEnd))
			{
				score -= NpcAiScratchPenalty;
			}

			PoolBall? hitBall = FindFirstBallOnShotPath(cueBall.Position, direction);
			if (hitBall == null)
			{
				return score - 1000f;
			}

			int npcBallType = GetAssignedBallTypeForPlayer(_activePlayerIndex);
			int hitBallType = GetBallType(hitBall);
			bool isOwnBall = npcBallType == 0
				? hitBall.Number != 8
				: hitBallType == npcBallType || (hitBall.Number == 8 && !HasRemainingBallsOfType(npcBallType));
			score += isOwnBall ? 600f : -900f;
			score -= DistanceFromRay(cueBall.Position, direction, hitBall.Position) * 60f;

			Vector2 targetDirection = hitBall.Position - cueBall.Position;
			if (targetDirection.LengthSquared() > 1f)
			{
				targetDirection.Normalize();
			}
			else
			{
				targetDirection = direction;
			}

			Vector2 targetEnd = hitBall.Position + targetDirection * NpcAiTargetBallTravelScale * powerRatio;
			float distanceImprovement = DistanceToNearestPocket(hitBall.Position) - DistanceToNearestPocket(targetEnd);
			score += isOwnBall ? distanceImprovement : -distanceImprovement;

			bool pocketsHitBall = IsPocketPath(hitBall.Position, targetEnd);
			if (pocketsHitBall)
			{
				if (hitBall.Number == 8)
				{
					score += HasRemainingBallsOfType(npcBallType) ? -NpcAiEarlyEightBallPenalty : NpcAiLegalEightBallScore;
				}
				else if (isOwnBall)
				{
					score += NpcAiPocketedOwnBallScore;
				}
				else
				{
					score -= NpcAiPocketedWrongBallPenalty;
				}
			}

			return score;
		}

		private PoolBall? FindFirstBallOnShotPath(Vector2 origin, Vector2 direction)
		{
			PoolBall? bestBall = null;
			float bestProjection = float.MaxValue;
			foreach (PoolBall ball in _balls)
			{
				if (ball.IsCueBall || ball.IsPocketed)
				{
					continue;
				}

				Vector2 toBall = ball.Position - origin;
				float projection = Vector2.Dot(toBall, direction);
				if (projection <= 0 || projection >= bestProjection)
				{
					continue;
				}

				if (DistanceFromRay(origin, direction, ball.Position) <= NpcAiRayWidth)
				{
					bestProjection = projection;
					bestBall = ball;
				}
			}

			return bestBall;
		}

		private static float DistanceFromRay(Vector2 origin, Vector2 direction, Vector2 point)
		{
			Vector2 toPoint = point - origin;
			float projection = Math.Max(0f, Vector2.Dot(toPoint, direction));
			Vector2 closestPoint = origin + direction * projection;
			return Vector2.Distance(point, closestPoint);
		}

		private static float DistanceToNearestPocket(Vector2 point)
		{
			return PocketCentres.Min(pocket => Vector2.Distance(point, pocket));
		}

		private static bool IsPocketPath(Vector2 start, Vector2 end)
		{
			Vector2 movement = end - start;
			if (movement.LengthSquared() <= 1f)
			{
				return false;
			}

			Vector2 direction = Vector2.Normalize(movement);
			float length = movement.Length();
			foreach (Vector2 pocket in PocketCentres)
			{
				float projection = Vector2.Dot(pocket - start, direction);
				if (projection < 0 || projection > length)
				{
					continue;
				}

				Vector2 closestPoint = start + direction * projection;
				if (Vector2.Distance(closestPoint, pocket) <= NpcAiPocketLineWidth)
				{
					return true;
				}
			}

			return false;
		}

		private bool HasRemainingBallsOfType(int ballType)
		{
			return ballType != 0 && _balls.Any(ball => !ball.IsPocketed && GetBallType(ball) == ballType);
		}

		private float RandomFloat(float min, float max)
		{
			return min + (float)_npcAiRandom.NextDouble() * (max - min);
		}

		private static Vector2 Rotate(Vector2 vector, float radians)
		{
			float cos = MathF.Cos(radians);
			float sin = MathF.Sin(radians);
			return new Vector2(vector.X * cos - vector.Y * sin, vector.X * sin + vector.Y * cos);
		}
	}
}
