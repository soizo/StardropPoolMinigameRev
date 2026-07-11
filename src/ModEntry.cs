using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Text.Json;

namespace StardropPoolMinigameRev
{
    public sealed class ModEntry : Mod
    {
        private const string SaveDataKey = "pool-table-state";
        private const string ProfileFileName = "profile.json";
        private static readonly string[] ProfileNpcNames = { "Sam", "Sebastian", "Abigail", "Gus" };
        private static readonly JsonSerializerOptions ProfileJsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
        private static readonly string[] InteractionModeValues =
        {
            nameof(PoolTableInteractionMode.Default),
            nameof(PoolTableInteractionMode.AlwaysSolo),
            nameof(PoolTableInteractionMode.AlwaysVsNpc),
            nameof(PoolTableInteractionMode.AlwaysWatch)
        };

        private PoolTableSaveData _saveData = new();
        private ModConfig _config = new();
        private PoolNpcProfiles _profiles = new();

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();
            _profiles = LoadProfiles();

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
                _saveData.CurrentTableOpponentName = null;
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

        private PoolNpcProfiles LoadProfiles()
        {
            string path = Path.Combine(Helper.DirectoryPath, ProfileFileName);
            PoolNpcProfiles profiles = new();
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    profiles = JsonSerializer.Deserialize<PoolNpcProfiles>(json, ProfileJsonOptions) ?? new PoolNpcProfiles();
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to read {ProfileFileName}; using default NPC profiles. {ex.Message}", LogLevel.Warn);
                profiles = new PoolNpcProfiles();
            }

            bool changed = false;
            foreach (string npcName in ProfileNpcNames)
            {
                if (!profiles.Npcs.ContainsKey(npcName))
                {
                    profiles.Npcs[npcName] = new PoolNpcProfile { FavouriteCueIndex = 0 };
                    changed = true;
                }
            }

            if (!File.Exists(path) || changed)
            {
                try
                {
                    File.WriteAllText(path, JsonSerializer.Serialize(profiles, ProfileJsonOptions));
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Failed to write {ProfileFileName}. {ex.Message}", LogLevel.Warn);
                }
            }

            return profiles;
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
                    Monitor.Log($"Watching {decision.NpcName} play against {decision.OtherNpcName}.", LogLevel.Info);
                    StartGame(decision.OtherNpcName, decision.NpcName);
                    break;

                case InteractionType.Leave:
                    Monitor.Log("Leaving pool table.", LogLevel.Info);
                    break;
            }
        }

        private void StartGame(string? npcOpponentName, string? npcPlayerName = null)
        {
            Monitor.Log("Starting Stardrop Pool minigame from pool table interaction.", LogLevel.Info);
            string? tableContext = GetTableContext(npcOpponentName, npcPlayerName);
            PoolTableSnapshot? snapshot = IsCurrentTableCompatible(tableContext) ? _saveData.CurrentTable : null;
            Game1.currentMinigame = new StardropPoolMinigameRev(Helper, Monitor, snapshot, snapshot => SaveCurrentTable(snapshot, tableContext), npcOpponentName, npcPlayerName, _profiles);
        }

        private static string? GetTableContext(string? npcOpponentName, string? npcPlayerName)
        {
            return string.IsNullOrWhiteSpace(npcPlayerName)
                ? npcOpponentName
                : $"watch:{npcPlayerName}:{npcOpponentName}";
        }

        private bool IsCurrentTableCompatible(string? tableContext)
        {
            if (_saveData.CurrentTable == null)
            {
                return true;
            }

            return string.Equals(_saveData.CurrentTableOpponentName, tableContext, StringComparison.Ordinal);
        }

        private void SaveCurrentTable(PoolTableSnapshot snapshot, string? tableContext)
        {
            _saveData.CurrentTable = snapshot;
            _saveData.CurrentTableDay = GetCurrentDayId();
            _saveData.CurrentTableOpponentName = tableContext;
        }
    }
}
