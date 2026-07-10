namespace StardropPoolMinigameRev
{
    internal sealed class PoolTableSaveData
    {
        public PoolTableSnapshot? CurrentTable { get; set; }

        public List<string> NpcsAtTable { get; set; } = new();
    }

    internal sealed class PoolTableSnapshot
    {
        public List<PoolBallSnapshot> Balls { get; set; } = new();

        public int Shots { get; set; }

        public int Pocketed { get; set; }
    }

    internal sealed class PoolBallSnapshot
    {
        public int Index { get; set; }

        public float PositionX { get; set; }

        public float PositionY { get; set; }

        public float VelocityX { get; set; }

        public float VelocityY { get; set; }

        public float OrientationX { get; set; }

        public float OrientationY { get; set; }

        public bool IsPocketed { get; set; }
    }
}
