namespace StardropPoolMinigameRev
{
    internal sealed class PoolTableSaveData
    {
        public PoolTableSnapshot? CurrentTable { get; set; }

        public int CurrentTableDay { get; set; }

        public string? CurrentTableOpponentName { get; set; }

        public int CurrentTableTimeOfDay { get; set; }

        public int LastPlayerCueIndex { get; set; } = -1;

        public List<string> NpcsAtTable { get; set; } = new();

        public PoolWatchSession? WatchSession { get; set; }
    }

    internal sealed class PoolWatchSession
    {
        public int Day { get; set; }

        public int TimeSlot { get; set; }

        public string FirstNpcName { get; set; } = string.Empty;

        public string SecondNpcName { get; set; } = string.Empty;

        public PoolTableSnapshot Snapshot { get; set; } = new();
    }

    internal sealed class PoolTableSnapshot
    {
        public List<PoolBallSnapshot> Balls { get; set; } = new();

        public int Shots { get; set; }

        public int Pocketed { get; set; }

        public int ActivePlayerIndex { get; set; }

        public int PlayerAssignedBallType { get; set; }

        public ulong CueRandomState { get; set; }

        public ulong NpcAiRandomState { get; set; }

        public double WatchCatchUpSeconds { get; set; }

        public bool IsMatchEnded { get; set; }

        public int MatchWinnerIndex { get; set; } = -1;

        public ulong WatchTimingRandomState { get; set; }

        public double WatchNpcPendingWaitMilliseconds { get; set; }

        public float CommittedNpcShotDirectionX { get; set; }

        public float CommittedNpcShotDirectionY { get; set; }

        public float CommittedNpcShotPowerRatio { get; set; }

        public float CommittedNpcShotFitness { get; set; }

        public bool CommittedNpcShotExpectedPot { get; set; }

        public bool CommittedNpcShotWasGiveUp { get; set; }

        public List<PoolPocketedBallSnapshot> PocketedBalls { get; set; } = new();
    }

    internal sealed class PoolPocketedBallSnapshot
    {
        public int BallIndex { get; set; }

        public int OwnerIndex { get; set; }

        public float OrientationX { get; set; }

        public float OrientationY { get; set; }
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
