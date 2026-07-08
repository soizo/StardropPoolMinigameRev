using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardropPoolMinigameRev
{
    public sealed class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            Monitor.Log("Stardrop Pool Minigame Rev loaded.", LogLevel.Info);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            Monitor.Log($"ButtonPressed: {e.Button}, IsWorldReady: {Context.IsWorldReady}", LogLevel.Info);

            if (!Context.IsWorldReady)
            {
                Monitor.Log("World not ready, ignoring input.", LogLevel.Info);
                return;
            }

            if (!e.Button.IsActionButton())
            {
                return;
            }

            Microsoft.Xna.Framework.Vector2 playerTile = Game1.player.Tile;
            Microsoft.Xna.Framework.Vector2 facingTile = Game1.player.GetGrabTile();
            string objectName = "(none)";
            if (Game1.currentLocation != null)
            {
                StardewValley.Object obj = Game1.currentLocation.getObjectAtTile((int)facingTile.X, (int)facingTile.Y);
                if (obj != null)
                {
                    objectName = obj.Name ?? obj.DisplayName ?? "(unnamed)";
                }
            }

            Monitor.Log($"Action button pressed. Player tile: {playerTile}, facing tile: {facingTile}, object: {objectName}", LogLevel.Info);
            StartGame();
        }

        private void StartGame()
        {
            Monitor.Log("Starting Stardrop Pool minigame.", LogLevel.Info);
            Game1.currentMinigame = new StardropPoolMinigameRev(Helper, Monitor);
        }
    }
}
