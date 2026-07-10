using Microsoft.Xna.Framework;
using StardewValley;

namespace StardropPoolMinigameRev
{
    internal enum InteractionType { Solo, Watch, PlayAgainst, Leave }

    internal sealed record InteractionDecision(InteractionType Type, string? NpcName);

    internal static class PoolTableInteractionMenu
    {
        private static readonly string[] EligibleNpcs = { "Sam", "Sebastian", "Abigail", "Gus" };
        private const int MinHeartsForInvite = 2;
        private const string DialogKey = "StardropPool_Interaction";
        private const string ResponseSolo = "solo";
        private const string ResponseWatch = "watch";
        private const string ResponsePlayPrefix = "play_";
        private const string ResponseLeave = "leave";

        private static readonly Rectangle PoolTableZone = new(35, 17, 8, 7);

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

        private static StardewModdingAPI.IMonitor? _monitor;

        public static void SetMonitor(StardewModdingAPI.IMonitor monitor)
        {
            _monitor = monitor;
        }

        private static void Log(string msg)
        {
            _monitor?.Log(msg, StardewModdingAPI.LogLevel.Info);
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
                Log($"[PoolTable] {npcName}: loc='{locationName}', tile=({tile.X},{tile.Y}), inZone={PoolTableZone.Contains(tile.X, tile.Y)}");

                if (locationName == null || !locationName.Equals("Saloon", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!PoolTableZone.Contains(tile.X, tile.Y))
                {
                    continue;
                }

                bool isPlayingAnimation = npc.Sprite.CurrentAnimation != null
                    || npc.Sprite.currentAnimationIndex > 0;

                Log($"[PoolTable] {npcName}: animation={isPlayingAnimation}");

                if (!isPlayingAnimation)
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
                return new InteractionDecision(InteractionType.Watch, null);
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
                responses.Add(new Response(ResponseWatch, "Watch them play"));
                foreach (string npcName in npcsAtTable)
                {
                    string displayName = GetNpcDisplayName(npcName);
                    responses.Add(new Response($"{ResponsePlayPrefix}{npcName}", $"Play against {displayName}"));
                }
            }
            else if (npcsAtTable.Count == 1)
            {
                string npcName = npcsAtTable[0];
                string displayName = GetNpcDisplayName(npcName);
                responses.Add(new Response($"{ResponsePlayPrefix}{npcName}", $"Play against {displayName}"));
            }
            else
            {
                responses.Add(new Response(ResponseSolo, "Play solo"));
                foreach (NPC npc in eligibleInSaloon)
                {
                    string displayName = npc.displayName ?? npc.Name;
                    responses.Add(new Response($"{ResponsePlayPrefix}{npc.Name}", $"Invite {displayName}"));
                }
            }

            responses.Add(new Response(ResponseLeave, "Leave"));
            Game1.currentLocation.createQuestionDialogue("What would you like to do?", responses.ToArray(), DialogKey);
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
