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
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            Monitor.Log("Stardrop Pool Minigame Rev loaded.", LogLevel.Info);
        }

        [EventPriority(EventPriority.High)]
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (Game1.currentMinigame is StardropPoolMinigameRev)
            {
                if (IsShortcutButton(e.Button))
                {
                    Helper.Input.Suppress(e.Button);
                }

                return;
            }

            if (!Context.IsWorldReady)
            {
                return;
            }

            if (Game1.currentMinigame != null)
            {
                return;
            }

            if (!e.Button.IsActionButton())
            {
                return;
            }

            if (Game1.currentLocation == null || !PoolTableDetector.IsInteractingWithPoolTable(Game1.player, Game1.currentLocation))
            {
                return;
            }

            Helper.Input.Suppress(e.Button);
            StartGame();
        }

        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (Game1.currentMinigame is not StardropPoolMinigameRev)
            {
                return;
            }

            foreach (SButton button in e.Pressed)
            {
                if (IsShortcutButton(button))
                {
                    Helper.Input.Suppress(button);
                }
            }
        }

        private static bool IsShortcutButton(SButton button)
        {
            return button != SButton.Escape
                && !button.IsUseToolButton()
                && !button.IsActionButton();
        }

        private void StartGame()
        {
            Monitor.Log("Starting Stardrop Pool minigame from pool table interaction.", LogLevel.Info);
            Game1.currentMinigame = new StardropPoolMinigameRev(Helper, Monitor);
        }
    }
}
