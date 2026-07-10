using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace StardropPoolMinigameRev.Assets
{
    internal sealed class StardropPoolAssets
    {
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;

        public StardropPoolAssets(IModHelper helper, IMonitor monitor)
        {
            _helper = helper;
            _monitor = monitor;
        }

        public Texture2D Tilesheet { get; private set; } = null!;

        public void Load()
        {
            Tilesheet = LoadTexture("Minigames/stardropPool", "Assets/Tilesheets/stardropPool.png");
        }

        private Texture2D LoadTexture(string gameAssetName, string modAssetPath)
        {
            try
            {
                Texture2D texture = Game1.content.Load<Texture2D>(gameAssetName);
                _monitor.Log($"Loaded {gameAssetName} from game content.", LogLevel.Info);
                return texture;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Could not load {gameAssetName} from game content ({ex.GetType().Name}). Falling back to bundled PNG {modAssetPath}.", LogLevel.Trace);
                return _helper.ModContent.Load<Texture2D>(modAssetPath);
            }
        }
    }
}
