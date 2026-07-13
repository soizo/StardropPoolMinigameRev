using StardewModdingAPI;
using StardewModdingAPI.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using StardewValley;
using StardropPoolMinigameRev.Scenes;
using System.Text.Json;

namespace StardropPoolMinigameRev
{
    public sealed class ModEntry : Mod
    {
        private const string SaveDataKey = "pool-table-state";
        private const string ProfileFileName = "profile.json";
        private const string WatchPresenceKey = "StardropPoolMinigameRev/WatchPresence";
        private const string WatchPresenceMessageType = "watch-presence";
        private const int WatchStartTime = 1910;
        private const int WatchSlotMinutes = 10;
        private static readonly string[] ProfileNpcNames = { "Sam", "Sebastian", "Abigail", "Gus" };
        private static readonly JsonSerializerOptions ProfileJsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
        private static readonly string[] InteractionModeValues =
        {
            nameof(PoolTableInteractionMode.Default),
            nameof(PoolTableInteractionMode.AlwaysSolo),
            nameof(PoolTableInteractionMode.AlwaysVsNpc),
            nameof(PoolTableInteractionMode.AlwaysWatch)
        };
        private static readonly string[] EmoteEightAppearanceValues =
        {
            nameof(EmoteEightAppearance.Default),
            nameof(EmoteEightAppearance.BigEyes),
            nameof(EmoteEightAppearance.SmallEyes)
        };

        private PoolTableSaveData _saveData = new();
        private ModConfig _config = new();
        private PoolNpcProfiles _profiles = new();
        private readonly Stopwatch _watchClock = new();
        private bool _isWatching;
        private long _watchLastElapsedMilliseconds;
        private bool _wasWatchActive;
        private int _watchDay;
        private int _watchSlot;
        private string? _watchFirstNpcName;
        private string? _watchSecondNpcName;

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();
            _profiles = LoadProfiles();

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            PoolTableInteractionMenu.SetMonitor(Monitor);
            PoolTableInteractionMenu.SetTranslationHelper(Helper.Translation);
            PoolTableInteractionMenu.SetDecisionHandler(HandleInteractionDecision);
            Monitor.Log("Stardrop Pool Minigame Rev loaded.", LogLevel.Info);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!_isWatching || Game1.currentMinigame is not StardropPoolMinigameRev)
            {
                if (_isWatching)
                {
                    ClearWatchPresence();
                }
                return;
            }

            if (!Game1.game1.IsActive)
            {
                _wasWatchActive = false;
                return;
            }

            long elapsedMilliseconds = _watchClock.ElapsedMilliseconds;
            if (!_wasWatchActive)
            {
                long inactiveMilliseconds = Math.Max(0, elapsedMilliseconds - _watchLastElapsedMilliseconds);
                if (inactiveMilliseconds > 0 && Game1.currentMinigame is StardropPoolMinigameRev minigame)
                {
                    minigame.FastForwardWatch(TimeSpan.FromMilliseconds(inactiveMilliseconds).TotalSeconds);
                }
            }

            _watchLastElapsedMilliseconds = elapsedMilliseconds;
            _wasWatchActive = true;
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            ClearWatchPresence();
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != ModManifest.UniqueID || e.Type != WatchPresenceMessageType)
            {
                return;
            }

            // Presence is stored in farmer modData; the immediate message prompts installed clients to redraw.
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }

            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer.UniqueMultiplayerID == Game1.player.UniqueMultiplayerID || !farmer.modData.TryGetValue(WatchPresenceKey, out string? value) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                Vector2 position = farmer.getStandingPosition() + new Vector2(-32f - Game1.viewport.X, -96f - Game1.viewport.Y);
                string message = Helper.Translation.Get("presence.watching-pool").Default("Watching pool");
                StardewValley.BellsAndWhistles.SpriteText.drawString(e.SpriteBatch, message, (int)position.X, (int)position.Y, alpha: 0.8f, layerDepth: 1f);
            }
        }

        private void BeginWatchPresence(string firstNpcName, string secondNpcName, int day, int slot)
        {
            _isWatching = true;
            _watchFirstNpcName = firstNpcName;
            _watchSecondNpcName = secondNpcName;
            _watchDay = day;
            _watchSlot = slot;
            _watchLastElapsedMilliseconds = 0;
            _wasWatchActive = Game1.game1.IsActive;
            _watchClock.Restart();
            PublishWatchPresence();
        }

        private void PublishWatchPresence()
        {
            if (!_isWatching || !Context.IsWorldReady)
            {
                return;
            }

            string value = $"{_watchDay}|{_watchSlot}|{_watchFirstNpcName}|{_watchSecondNpcName}";
            Game1.player.modData[WatchPresenceKey] = value;
            Helper.Multiplayer.SendMessage(value, WatchPresenceMessageType, new[] { ModManifest.UniqueID });
        }

        private void ClearWatchPresence()
        {
            if (Context.IsWorldReady)
            {
                Game1.player.modData.Remove(WatchPresenceKey);
                Helper.Multiplayer.SendMessage(string.Empty, WatchPresenceMessageType, new[] { ModManifest.UniqueID });
            }

            _isWatching = false;
            _watchClock.Reset();
            _watchLastElapsedMilliseconds = 0;
            _wasWatchActive = false;
            _watchFirstNpcName = null;
            _watchSecondNpcName = null;
        }

        [EventPriority(EventPriority.High)]
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (Game1.currentMinigame is StardropPoolMinigameRev)
            {
                bool hasActiveMenu = Game1.activeClickableMenu != null;
                Monitor.Log($"[DEBUG-endmenu] SMAPI pressed {e.Button}; activeMenu={Game1.activeClickableMenu?.GetType().FullName ?? "null"}; suppress={(!hasActiveMenu && IsShortcutButton(e.Button))}.", LogLevel.Trace);
                if (!hasActiveMenu && IsShortcutButton(e.Button))
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
                if (Game1.activeClickableMenu == null && IsShortcutButton(button))
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
                _saveData.CurrentTableTimeOfDay = 0;
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

                if (profiles.Npcs[npcName].Emotes == null)
                {
                    profiles.Npcs[npcName].Emotes = new PoolNpcEmotes();
                    changed = true;
                }

                PoolNpcEmotes emotes = profiles.Npcs[npcName].Emotes;
                string beforeEmotes = JsonSerializer.Serialize(emotes, ProfileJsonOptions);
                emotes.ClampContentIndices();
                if (JsonSerializer.Serialize(emotes, ProfileJsonOptions) != beforeEmotes)
                {
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

            gmcm.AddTextOption(
                ModManifest,
                getValue: () => _config.EmoteEightAppearance.ToString(),
                setValue: value =>
                {
                    if (Enum.TryParse(value, out EmoteEightAppearance appearance))
                    {
                        _config.EmoteEightAppearance = appearance;
                    }
                },
                name: () => Helper.Translation.Get("config.emote-eight-appearance.name").Default("Eye emote appearance"),
                tooltip: () => Helper.Translation.Get("config.emote-eight-appearance.tooltip").Default("Choose which eye emote art is used when emote 8 is shown."),
                allowedValues: EmoteEightAppearanceValues,
                formatAllowedValue: FormatEmoteEightAppearance
            );
        }

        private string FormatEmoteEightAppearance(string value)
        {
            return value switch
            {
                nameof(EmoteEightAppearance.Default) => Helper.Translation.Get("config.emote-eight-appearance.default").Default("Default"),
                nameof(EmoteEightAppearance.BigEyes) => Helper.Translation.Get("config.emote-eight-appearance.big-eyes").Default("Big eyes"),
                nameof(EmoteEightAppearance.SmallEyes) => Helper.Translation.Get("config.emote-eight-appearance.small-eyes").Default("Small eyes"),
                _ => value
            };
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
                && button != SButton.Y
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
            bool isWatchMode = IsWatchMode(npcOpponentName, npcPlayerName);
            int? randomSeedDay = null;
            PoolTableSnapshot? snapshot;
            if (isWatchMode)
            {
                int day = GetWatchSeedDay();
                int slot = GetWatchTimeSlot();
                snapshot = GetOrCreateWatchSnapshot(npcPlayerName!, npcOpponentName!, day, slot);
                randomSeedDay = GetWatchRandomSeed(day, slot, npcPlayerName!, npcOpponentName!);
                BeginWatchPresence(npcPlayerName!, npcOpponentName!, day, slot);
            }
            else
            {
                snapshot = IsCurrentTableCompatible(tableContext) ? _saveData.CurrentTable : null;
            }

            Game1.currentMinigame = new StardropPoolMinigameRev(Helper, Monitor, snapshot, watchSnapshot => SaveWatchSnapshot(npcPlayerName!, npcOpponentName!, watchSnapshot), snapshot => SaveCurrentTable(snapshot, tableContext), SavePlayerCueIndex, _saveData.LastPlayerCueIndex, npcOpponentName, npcPlayerName, _profiles, _config, randomSeedDay);
        }

        private PoolTableSnapshot GetOrCreateWatchSnapshot(string firstNpcName, string secondNpcName, int day, int slot)
        {
            PoolWatchSession? session = _saveData.WatchSession;
            if (session != null
                && session.Day == day
                && session.TimeSlot == slot
                && string.Equals(session.FirstNpcName, firstNpcName, StringComparison.Ordinal)
                && string.Equals(session.SecondNpcName, secondNpcName, StringComparison.Ordinal))
            {
                return session.Snapshot;
            }

            GameScene scene = new(Monitor, null, IsSveInstalled(), secondNpcName, firstNpcName, _profiles, _config, randomSeedDay: GetWatchRandomSeed(day, slot, firstNpcName, secondNpcName));
            PoolTableSnapshot snapshot = scene.CreateSnapshot();
            SaveWatchSnapshot(firstNpcName, secondNpcName, snapshot, day, slot);
            return snapshot;
        }

        private void SaveWatchSnapshot(string firstNpcName, string secondNpcName, PoolTableSnapshot snapshot)
        {
            SaveWatchSnapshot(firstNpcName, secondNpcName, snapshot, _watchDay, _watchSlot);
        }

        private void SaveWatchSnapshot(string firstNpcName, string secondNpcName, PoolTableSnapshot snapshot, int day, int slot)
        {
            _saveData.WatchSession = new PoolWatchSession
            {
                Day = day,
                TimeSlot = slot,
                FirstNpcName = firstNpcName,
                SecondNpcName = secondNpcName,
                Snapshot = snapshot
            };
        }

        private static int GetWatchTimeSlot()
        {
            int minutes = TimeOfDayToMinutes(Game1.timeOfDay);
            if (minutes < TimeOfDayToMinutes(WatchStartTime))
            {
                minutes += 24 * 60;
            }

            return Math.Max(0, (minutes - TimeOfDayToMinutes(WatchStartTime)) / WatchSlotMinutes);
        }

        private static int GetWatchRandomSeed(int day, int slot, string firstNpcName, string secondNpcName)
        {
            unchecked
            {
                int seed = day;
                seed = seed * 397 ^ slot;
                seed = seed * 397 ^ GetStableNameHash(firstNpcName);
                return seed * 397 ^ GetStableNameHash(secondNpcName);
            }
        }

        private static int GetStableNameHash(string value)
        {
            unchecked
            {
                int hash = 17;
                foreach (char character in value)
                {
                    hash = hash * 31 + character;
                }

                return hash;
            }
        }

        private bool IsSveInstalled()
        {
            return Helper.ModRegistry.IsLoaded("FlashShifter.StardewValleyExpandedCP")
                || Helper.ModRegistry.IsLoaded("FlashShifter.SVECode");
        }

        private static bool IsWatchMode(string? npcOpponentName, string? npcPlayerName)
        {
            return !string.IsNullOrWhiteSpace(npcOpponentName) && !string.IsNullOrWhiteSpace(npcPlayerName);
        }

        private static int GetWatchSeedDay()
        {
            return TimeOfDayToMinutes(Game1.timeOfDay) < TimeOfDayToMinutes(1910)
                ? Game1.Date.TotalDays - 1
                : Game1.Date.TotalDays;
        }

        private static int TimeOfDayToMinutes(int timeOfDay)
        {
            int hours = timeOfDay / 100;
            int minutes = timeOfDay % 100;
            return hours * 60 + minutes;
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
            _saveData.CurrentTableTimeOfDay = Game1.timeOfDay;
        }

        private void SavePlayerCueIndex(int cueIndex)
        {
            _saveData.LastPlayerCueIndex = cueIndex;
        }
    }
}
