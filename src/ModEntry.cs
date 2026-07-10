using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardropPoolMinigameRev
{
    public sealed class ModEntry : Mod
    {
        private const string SaveDataKey = "pool-table-state";
        private static readonly string[] InteractionModeValues =
        {
            nameof(PoolTableInteractionMode.Default),
            nameof(PoolTableInteractionMode.AlwaysSolo),
            nameof(PoolTableInteractionMode.AlwaysVsNpc),
            nameof(PoolTableInteractionMode.AlwaysWatch)
        };

        private PoolTableSaveData _saveData = new();
        private ModConfig _config = new();

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            PoolTableInteractionMenu.SetMonitor(Monitor);
            PoolTableInteractionMenu.SetTranslationHelper(Helper.Translation);
            PoolTableInteractionMenu.SetDecisionHandler(HandleInteractionDecision);
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

            var decision = PoolTableInteractionMenu.DetermineInteraction(_config.InteractionMode);
            if (decision != null)
            {
                HandleInteractionDecision(decision);
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
            ExpirePreviousDayTable();
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            ExpirePreviousDayTable();
        }

        private void ExpirePreviousDayTable()
        {
            int today = GetCurrentDayId();
            if (_saveData.CurrentTable != null && _saveData.CurrentTableDay != today)
            {
                Monitor.Log("Resetting saved pool table state for the new day.", LogLevel.Info);
                _saveData.CurrentTable = null;
                _saveData.CurrentTableDay = 0;
            }
        }

        private static int GetCurrentDayId()
        {
            return Game1.Date.TotalDays;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            RegisterGenericModConfigMenu();
        }

        private void RegisterGenericModConfigMenu()
        {
            IGenericModConfigMenuApi? gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm == null)
            {
                Monitor.Log("Generic Mod Config Menu is not installed; skipping config menu registration.", LogLevel.Trace);
                return;
            }

            gmcm.Register(
                ModManifest,
                reset: () => _config = new ModConfig(),
                save: () => Helper.WriteConfig(_config)
            );

            gmcm.AddTextOption(
                ModManifest,
                getValue: () => _config.InteractionMode.ToString(),
                setValue: value =>
                {
                    if (Enum.TryParse(value, out PoolTableInteractionMode mode))
                    {
                        _config.InteractionMode = mode;
                    }
                },
                name: () => Helper.Translation.Get("config.interaction-mode.name").Default("Default table interaction mode"),
                tooltip: () => Helper.Translation.Get("config.interaction-mode.tooltip").Default("Choose what happens when you interact with the saloon pool table."),
                allowedValues: InteractionModeValues,
                formatAllowedValue: FormatInteractionMode
            );
        }

        private string FormatInteractionMode(string value)
        {
            return value switch
            {
                nameof(PoolTableInteractionMode.Default) => Helper.Translation.Get("config.interaction-mode.default").Default("Default pool table interaction"),
                nameof(PoolTableInteractionMode.AlwaysSolo) => Helper.Translation.Get("config.interaction-mode.always-solo").Default("Always solo"),
                nameof(PoolTableInteractionMode.AlwaysVsNpc) => Helper.Translation.Get("config.interaction-mode.always-vs-npc").Default("Always vs NPC (random person)"),
                nameof(PoolTableInteractionMode.AlwaysWatch) => Helper.Translation.Get("config.interaction-mode.always-watch").Default("Always watch (random people)"),
                _ => value
            };
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
            _saveData.CurrentTableDay = GetCurrentDayId();
        }
    }
}
