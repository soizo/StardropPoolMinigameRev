using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace StardropPoolMinigameRev
{
    public sealed class ModEntry : Mod
    {
        private const string SaveDataKey = "pool-table-state";

        private PoolTableSaveData _saveData = new();
        private bool _waitingForMenuAnswer;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            PoolTableInteractionMenu.SetMonitor(Monitor);
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

            if (Game1.currentLocation == null || !PoolTableDetector.IsInteractingWithPoolTable(Game1.player, Game1.currentLocation, Helper.Input.GetCursorPosition().GrabTile))
            {
                return;
            }

            Helper.Input.Suppress(e.Button);

            if (_waitingForMenuAnswer)
            {
                return;
            }

            var decision = PoolTableInteractionMenu.DetermineInteraction();
            if (decision != null)
            {
                HandleInteractionDecision(decision);
            }
            else
            {
                _waitingForMenuAnswer = true;
            }
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

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            _saveData = Helper.Data.ReadSaveData<PoolTableSaveData>(SaveDataKey) ?? new PoolTableSaveData();
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            Helper.Data.WriteSaveData(SaveDataKey, _saveData);
        }

        private static bool IsShortcutButton(SButton button)
        {
            return button != SButton.Escape
                && !button.IsUseToolButton()
                && !button.IsActionButton();
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (!_waitingForMenuAnswer)
            {
                return;
            }

            if (e.NewMenu != null || e.OldMenu is not DialogueBox dialogueBox)
            {
                return;
            }

            _waitingForMenuAnswer = false;

            if (Game1.currentLocation.lastQuestionKey != "StardropPool_Interaction")
            {
                return;
            }

            int selectedIndex = Helper.Reflection.GetField<int>(dialogueBox, "selectedResponse").GetValue();
            if (selectedIndex < 0)
            {
                return;
            }

            Response[] responses = dialogueBox.responses;
            if (selectedIndex >= responses.Length)
            {
                return;
            }

            string answerKey = responses[selectedIndex].responseKey;
            List<string> npcsAtTable = PoolTableInteractionMenu.GetNpcsAtTable();
            List<string> eligibleInSaloon = PoolTableInteractionMenu.GetEligibleNpcNamesInSaloon();

            var decision = PoolTableInteractionMenu.ParseAnswer(answerKey, npcsAtTable, eligibleInSaloon);
            if (decision != null)
            {
                HandleInteractionDecision(decision);
            }
        }

        private void HandleInteractionDecision(InteractionDecision decision)
        {
            switch (decision.Type)
            {
                case InteractionType.Solo:
                    Monitor.Log("Starting solo game.", LogLevel.Info);
                    StartGame(null);
                    break;

                case InteractionType.PlayAgainst:
                    Monitor.Log($"Starting game against {decision.NpcName}.", LogLevel.Info);
                    StartGame(decision.NpcName);
                    break;

                case InteractionType.Watch:
                    Monitor.Log("Watching NPCs play.", LogLevel.Info);
                    StartGame(null);
                    break;

                case InteractionType.Leave:
                    Monitor.Log("Leaving pool table.", LogLevel.Info);
                    break;
            }
        }

        private void StartGame(string? npcOpponentName)
        {
            Monitor.Log("Starting Stardrop Pool minigame from pool table interaction.", LogLevel.Info);
            Game1.currentMinigame = new StardropPoolMinigameRev(Helper, Monitor, _saveData.CurrentTable, SaveCurrentTable, npcOpponentName);
        }

        private void SaveCurrentTable(PoolTableSnapshot snapshot)
        {
            _saveData.CurrentTable = snapshot;
        }
    }
}
