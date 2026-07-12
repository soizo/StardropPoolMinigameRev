using System.Text.Json;
using System.Text.Json.Serialization;

namespace StardropPoolMinigameRev
{
    internal sealed class PoolNpcProfiles
    {
        public Dictionary<string, PoolNpcProfile> Npcs { get; set; } = new();
    }

    internal sealed class PoolNpcProfile
    {
        public int FavouriteCueIndex { get; set; }

        public PoolNpcEmotes Emotes { get; set; } = new();
    }

    internal sealed class PoolNpcEmotes
    {
        public const int DisabledIndex = 0;
        public const int MinimumContentIndex = 1;
        public const int MaximumContentIndex = 15;

        public PoolNpcEmoteOption ExpectedPotMissed { get; set; } = new();

        public PoolNpcEmoteOption GiveUpScratch { get; set; } = new();

        public PoolNpcEmoteOption AccidentalScratch { get; set; } = new();

        public PoolNpcEmoteOption UnexpectedOpponentPot { get; set; } = new();

        public PoolNpcEmoteOption HappyPot { get; set; } = new();

        public PoolNpcEmoteOption MultiPot { get; set; } = new();

        public PoolNpcEmoteOption StreakPot { get; set; } = new();

        public PoolNpcEmoteOption Won { get; set; } = new();

        public PoolNpcEmoteOption Lost { get; set; } = new();

        public void ClampContentIndices()
        {
            ExpectedPotMissed.Clamp();
            GiveUpScratch.Clamp();
            AccidentalScratch.Clamp();
            UnexpectedOpponentPot.Clamp();
            HappyPot.Clamp();
            MultiPot.Clamp();
            StreakPot.Clamp();
            Won.Clamp();
            Lost.Clamp();
        }
    }

    [JsonConverter(typeof(PoolNpcEmoteOptionConverter))]
    internal sealed class PoolNpcEmoteOption
    {
        public int Index { get; set; } = PoolNpcEmotes.DisabledIndex;

        public float Chance { get; set; } = 1f;

        public void Clamp()
        {
            Index = Math.Clamp(Index, PoolNpcEmotes.DisabledIndex, PoolNpcEmotes.MaximumContentIndex);
            Chance = Math.Clamp(Chance, 0f, 1f);
        }
    }

    internal sealed class PoolNpcEmoteOptionConverter : JsonConverter<PoolNpcEmoteOption>
    {
        public override PoolNpcEmoteOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                PoolNpcEmoteOption option = new() { Index = reader.GetInt32() };
                option.Clamp();
                return option;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected emote option object or index number.");
            }

            PoolNpcEmoteOption result = new();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    result.Clamp();
                    return result;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected emote option property name.");
                }

                string? propertyName = reader.GetString();
                reader.Read();
                if (string.Equals(propertyName, "index", StringComparison.OrdinalIgnoreCase))
                {
                    result.Index = reader.GetInt32();
                }
                else if (string.Equals(propertyName, "chance", StringComparison.OrdinalIgnoreCase))
                {
                    result.Chance = reader.GetSingle();
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Unclosed emote option object.");
        }

        public override void Write(Utf8JsonWriter writer, PoolNpcEmoteOption value, JsonSerializerOptions options)
        {
            value.Clamp();
            writer.WriteStartObject();
            writer.WriteNumber("index", value.Index);
            writer.WriteNumber("chance", value.Chance);
            writer.WriteEndObject();
        }
    }
}
