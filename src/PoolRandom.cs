using StardewValley;

namespace StardropPoolMinigameRev
{
    internal static class PoolRandom
    {
        public static Random CreateForGameDate(int salt)
        {
            return CreateForGameDate(Game1.Date.TotalDays, salt);
        }

        public static Random CreateForGameDate(int totalDays, int salt)
        {
            return new Random(GetGameDateSeed(totalDays, salt));
        }

        private static int GetGameDateSeed(int totalDays, int salt)
        {
            unchecked
            {
                int seed = totalDays;
                seed = seed * 397 ^ salt;
                return seed;
            }
        }
    }
}
