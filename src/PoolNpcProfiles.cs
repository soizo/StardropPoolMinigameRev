namespace StardropPoolMinigameRev
{
    internal sealed class PoolNpcProfiles
    {
        public Dictionary<string, PoolNpcProfile> Npcs { get; set; } = new();
    }

    internal sealed class PoolNpcProfile
    {
        public int FavouriteCueIndex { get; set; }
    }
}
