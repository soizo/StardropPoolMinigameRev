namespace StardropPoolMinigameRev
{
    internal sealed class PoolRandom
    {
        private ulong _state;

        public PoolRandom(ulong seed)
        {
            _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
        }

        public ulong State
        {
            get => _state;
            set => _state = value == 0 ? 0x9E3779B97F4A7C15UL : value;
        }

        public static PoolRandom CreateForGameDate(int salt)
        {
            return CreateForGameDate(StardewValley.Game1.Date.TotalDays, salt);
        }

        public static PoolRandom CreateForGameDate(int totalDays, int salt)
        {
            unchecked
            {
                ulong seed = ((ulong)(uint)totalDays << 32) | (uint)salt;
                return new PoolRandom(seed ^ 0xD1B54A32D192ED03UL);
            }
        }

        public int Next(int maxValue)
        {
            if (maxValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            return (int)(NextUInt64() % (uint)maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue));
            }

            return minValue == maxValue ? minValue : minValue + Next(maxValue - minValue);
        }

        public double NextDouble()
        {
            return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
        }

        private ulong NextUInt64()
        {
            ulong value = _state;
            value ^= value >> 12;
            value ^= value << 25;
            value ^= value >> 27;
            _state = value;
            return value * 2685821657736338717UL;
        }
    }
}
