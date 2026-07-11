using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace StardropPoolMinigameRev
{
    internal enum InteractionType { Solo, Watch, PlayAgainst, Leave }

    internal sealed record InteractionDecision(InteractionType Type, string? NpcName, string? OtherNpcName = null);

    internal static class PoolTableInteractionMenu
    {
        private static readonly string[] EligibleNpcs = { "Sam", "Sebastian", "Abigail", "Gus" };
        private const int MinHeartsForInvite = 2;
        private const string DialogKey = "StardropPool_Interaction";
        private const string ResponseSolo = "solo";
        private const string ResponseWatch = "watch";
        private const string ResponsePlayPrefix = "play_";
        private const string ResponseLeave = "leave";

        private static IMonitor? _monitor;
        private static ITranslationHelper? _i18n;
        private static Action<InteractionDecision>? _onDecision;
        private static readonly Random Random = new();

        public static void SetMonitor(IMonitor monitor)
        {
            _monitor = monitor;
        }

        public static void SetTranslationHelper(ITranslationHelper i18n)
        {
            _i18n = i18n;
        }

        public static void SetDecisionHandler(Action<InteractionDecision> onDecision)
        {
            _onDecision = onDecision;
        }

        private static void Log(string msg)
        {
            _monitor?.Log(msg, LogLevel.Info);
        }

        private static string T(string key, object? tokens = null)
        {
            return _i18n?.Get(key, tokens) ?? key;
        }

        public static InteractionDecision? DetermineInteraction()
        {
            List<string> npcsAtTable = DetectNpcsNearPoolTable();
            List<NPC> eligibleInSaloon = GetEligibleNpcsInSaloon(npcNamesAtTable: npcsAtTable);

            if (npcsAtTable.Count == 0 && eligibleInSaloon.Count == 0)
            {
                return new InteractionDecision(InteractionType.Solo, null);
            }

            ShowMenu(npcsAtTable, eligibleInSaloon);
            return null;
        }

        public static InteractionDecision? DetermineInteraction(PoolTableInteractionMode mode)
        {
            List<string> npcsAtTable = DetectNpcsNearPoolTable();
            List<NPC> eligibleInSaloon = GetEligibleNpcsInSaloon(npcNamesAtTable: npcsAtTable);

            InteractionDecision? configuredDecision = TryGetConfiguredDecision(mode, npcsAtTable, eligibleInSaloon);
            if (configuredDecision != null)
            {
                return configuredDecision;
            }

            if (npcsAtTable.Count == 0 && eligibleInSaloon.Count == 0)
            {
                return new InteractionDecision(InteractionType.Solo, null);
            }

            ShowMenu(npcsAtTable, eligibleInSaloon);
            return null;
        }

        private static InteractionDecision? TryGetConfiguredDecision(PoolTableInteractionMode mode, List<string> npcsAtTable, List<NPC> eligibleInSaloon)
        {
            return mode switch
            {
                PoolTableInteractionMode.Default => null,
                PoolTableInteractionMode.AlwaysSolo => new InteractionDecision(InteractionType.Solo, null),
                PoolTableInteractionMode.AlwaysVsNpc => TryGetRandomNpcDecision(npcsAtTable, eligibleInSaloon),
                PoolTableInteractionMode.AlwaysWatch => npcsAtTable.Count >= 2
                    ? new InteractionDecision(InteractionType.Watch, npcsAtTable[0], npcsAtTable[1])
                    : null,
                _ => null
            };
        }

        private static InteractionDecision? TryGetRandomNpcDecision(List<string> npcsAtTable, List<NPC> eligibleInSaloon)
        {
            if (npcsAtTable.Count > 0)
            {
                return new InteractionDecision(InteractionType.PlayAgainst, PickRandom(npcsAtTable));
            }

            if (eligibleInSaloon.Count > 0)
            {
                return new InteractionDecision(InteractionType.PlayAgainst, PickRandom(eligibleInSaloon).Name);
            }

            return null;
        }

        private static T PickRandom<T>(IReadOnlyList<T> values)
        {
            return values[Random.Next(values.Count)];
        }

        public static List<string> DetectNpcsNearPoolTable()
        {
            List<string> result = new();

            Log("[PoolTable] === Scanning for NPCs near table ===");

            foreach (string npcName in EligibleNpcs)
            {
                NPC? npc = Game1.getCharacterFromName(npcName, mustBeVillager: true);
                if (npc == null)
                {
                    Log($"[PoolTable] {npcName}: getCharacterFromName returned null");
                    continue;
                }

                if (npc.currentLocation == null)
                {
                    Log($"[PoolTable] {npcName}: currentLocation is null");
                    continue;
                }

                string? locationName = npc.currentLocation.NameOrUniqueName;
                Point tile = npc.TilePoint;
                bool isNearTable = PoolTableDetector.IsNearPoolTable(npc.currentLocation, tile);
                bool isPlayingAnimation = npc.Sprite.CurrentAnimation != null
                    || npc.Sprite.currentAnimationIndex > 0;

                Log($"[PoolTable] {npcName}: loc='{locationName}', tile=({tile.X},{tile.Y}), nearTable={isNearTable}, animation={isPlayingAnimation}, facing={npc.FacingDirection}");

                if (locationName == null || !locationName.Equals("Saloon", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!isNearTable)
                {
                    continue;
                }

                result.Add(npcName);
            }

            return result;
        }

        public static List<string> GetNpcsAtTable()
        {
            return DetectNpcsNearPoolTable();
        }

        public static InteractionDecision? ParseAnswer(string answerKey, List<string> npcsAtTable, List<string> eligibleInSaloon)
        {
            if (answerKey == ResponseSolo)
            {
                return new InteractionDecision(InteractionType.Solo, null);
            }

            if (answerKey == ResponseWatch)
            {
                return npcsAtTable.Count >= 2
                    ? new InteractionDecision(InteractionType.Watch, npcsAtTable[0], npcsAtTable[1])
                    : null;
            }

            if (answerKey == ResponseLeave)
            {
                return new InteractionDecision(InteractionType.Leave, null);
            }

            if (answerKey.StartsWith(ResponsePlayPrefix))
            {
                string npcName = answerKey[ResponsePlayPrefix.Length..];
                return new InteractionDecision(InteractionType.PlayAgainst, npcName);
            }

            return null;
        }

        private static void ShowMenu(List<string> npcsAtTable, List<NPC> eligibleInSaloon)
        {
            List<Response> responses = new();

            if (npcsAtTable.Count >= 2)
            {
                responses.Add(new Response(ResponseWatch, T("menu.watch")));
                foreach (string npcName in npcsAtTable)
                {
                    string displayName = GetNpcDisplayName(npcName);
                    responses.Add(new Response($"{ResponsePlayPrefix}{npcName}", T("menu.play-against", new { npcName = displayName })));
                }
            }
            else if (npcsAtTable.Count == 1)
            {
                string npcName = npcsAtTable[0];
                string displayName = GetNpcDisplayName(npcName);
                responses.Add(new Response($"{ResponsePlayPrefix}{npcName}", T("menu.play-against", new { npcName = displayName })));
            }
            else
            {
                responses.Add(new Response(ResponseSolo, T("menu.play-solo")));
                foreach (NPC npc in eligibleInSaloon)
                {
                    string displayName = npc.displayName ?? npc.Name;
                    responses.Add(new Response($"{ResponsePlayPrefix}{npc.Name}", T("menu.invite", new { npcName = displayName })));
                }
            }

            responses.Add(new Response(ResponseLeave, T("menu.leave")));
            Game1.currentLocation.createQuestionDialogue(T("menu.question"), responses.ToArray(), OnMenuDecision);
        }

        private static void OnMenuDecision(Farmer who, string answerKey)
        {
            List<string> npcsAtTable = GetNpcsAtTable();
            List<string> eligibleInSaloon = GetEligibleNpcNamesInSaloon();
            InteractionDecision? decision = ParseAnswer(answerKey, npcsAtTable, eligibleInSaloon);
            if (decision != null)
            {
                _onDecision?.Invoke(decision);
            }
        }

        private static List<NPC> GetEligibleNpcsInSaloon(List<string>? npcNamesAtTable = null)
        {
            HashSet<string> atTable = new(npcNamesAtTable ?? new List<string>());
            List<NPC> result = new();

            Log("[PoolTable] === Scanning for eligible NPCs in Saloon ===");

            foreach (string npcName in EligibleNpcs)
            {
                if (atTable.Contains(npcName))
                {
                    Log($"[PoolTable] {npcName}: skipped (at table)");
                    continue;
                }

                NPC? npc = Game1.getCharacterFromName(npcName, mustBeVillager: true);
                if (npc == null)
                {
                    Log($"[PoolTable] {npcName}: getCharacterFromName returned null");
                    continue;
                }

                if (npc.currentLocation == null)
                {
                    Log($"[PoolTable] {npcName}: currentLocation is null");
                    continue;
                }

                string? locationName = npc.currentLocation.NameOrUniqueName;
                if (locationName == null || !locationName.Equals("Saloon", StringComparison.OrdinalIgnoreCase))
                {
                    Log($"[PoolTable] {npcName}: wrong location '{locationName}'");
                    continue;
                }

                bool hasFriendship = Game1.player.friendshipData.TryGetValue(npcName, out Friendship? friendship);
                int points = friendship?.Points ?? 0;
                Log($"[PoolTable] {npcName}: in Saloon, friendship={points} (need {MinHeartsForInvite * 250})");

                if (hasFriendship && points >= MinHeartsForInvite * 250)
                {
                    result.Add(npc);
                }
                else
                {
                    Log($"[PoolTable] {npcName}: skipped (friendship too low or no data)");
                }
            }

            return result;
        }

        public static List<string> GetEligibleNpcNamesInSaloon()
        {
            List<string> result = new();
            foreach (NPC npc in GetEligibleNpcsInSaloon())
            {
                result.Add(npc.Name);
            }

            return result;
        }

        private static string GetNpcDisplayName(string internalName)
        {
            NPC? npc = Game1.getCharacterFromName(internalName);
            return npc != null ? (npc.displayName ?? npc.Name) : internalName;
        }
    }
}