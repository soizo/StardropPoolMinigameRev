namespace StardropPoolMinigameRev
{
    internal enum PoolTableInteractionMode
    {
        Default,
        AlwaysSolo,
        AlwaysVsNpc,
        AlwaysWatch
    }

    internal sealed class ModConfig
    {
        public PoolTableInteractionMode InteractionMode { get; set; } = PoolTableInteractionMode.Default;
    }
}
