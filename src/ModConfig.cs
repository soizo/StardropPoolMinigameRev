namespace StardropPoolMinigameRev
{
    internal enum PoolTableInteractionMode
    {
        Default,
        AlwaysSolo,
        AlwaysVsNpc,
        AlwaysWatch
    }

    internal enum EmoteEightAppearance
    {
        Default,
        BigEyes,
        SmallEyes
    }

    internal sealed class ModConfig
    {
        public PoolTableInteractionMode InteractionMode { get; set; } = PoolTableInteractionMode.Default;

        public EmoteEightAppearance EmoteEightAppearance { get; set; } = EmoteEightAppearance.Default;
    }
}
