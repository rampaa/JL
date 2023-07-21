using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.Anki;

public sealed class AnkiConfig
{
    [JsonPropertyName("deckName")] public string DeckName { get; }

    [JsonPropertyName("modelName")] public string ModelName { get; }

    [JsonPropertyName("fields")] public Dictionary<string, JLField> Fields { get; }

    [JsonPropertyName("tags")] public ImmutableArray<string> Tags { get; }

    [JsonIgnore] private static Dictionary<MineType, AnkiConfig>? s_ankiConfigDict;

    public AnkiConfig(string deckName, string modelName, Dictionary<string, JLField> fields, ImmutableArray<string> tags)
    {
        DeckName = deckName;
        ModelName = modelName;
        Fields = fields;
        Tags = tags;
    }

    //public static async Task CreateDefaultAnkiConfig()
    //{
    //    Dictionary<string, AnkiConfig> defaultAnkiConfigDict = new();
    //    AnkiConfig wordAnkiConfig = new(
    //            "JLDeck",
    //            "Japanese JL-Basic",
    //            new Dictionary<string, JLField>
    //            {
    //                { "Edict ID", JLField.EdictId },
    //                { "Expression", JLField.FoundSpelling },
    //                { "Reading", JLField.Readings },
    //                { "Gloss", JLField.Definitions },
    //                { "Sentence", JLField.Context },
    //                { "Audio", JLField.Audio },
    //                { "Time", JLField.TimeLocal },
    //            },
    //            new[] { "JL", "Word" }
    //        );

    //    await WriteAnkiConfig(defaultAnkiConfigDict
    //    ).ConfigureAwait(false);
    //}

    public static async Task<bool> WriteAnkiConfig(Dictionary<MineType, AnkiConfig> ankiConfig)
    {
        try
        {
            _ = Directory.CreateDirectory(Utils.ConfigPath);
            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "AnkiConfig.json"),
                JsonSerializer.Serialize(ankiConfig, Utils.s_jsoWithEnumConverterAndIndentation)).ConfigureAwait(false);

            s_ankiConfigDict = ankiConfig;

            return true;
        }

        catch (Exception ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't write AnkiConfig");
            Utils.Logger.Error(ex, "Couldn't write AnkiConfig");
            return false;
        }
    }

    public static async ValueTask<Dictionary<MineType, AnkiConfig>?> ReadAnkiConfig()
    {
        if (s_ankiConfigDict is null)
        {
            string filePath = Path.Join(Utils.ConfigPath, "AnkiConfig.json");
            if (File.Exists(filePath))
            {
                try
                {
                    FileStream ankiConfigStream = File.OpenRead(filePath);
                    await using (ankiConfigStream.ConfigureAwait(false))
                    {
                        s_ankiConfigDict = await JsonSerializer.DeserializeAsync<Dictionary<MineType, AnkiConfig>>(ankiConfigStream,
                            Utils.s_jsoWithEnumConverter).ConfigureAwait(false);
                    }
                }

                catch (Exception ex)
                {
                    Utils.Frontend.Alert(AlertLevel.Error, "Couldn't read AnkiConfig");
                    Utils.Logger.Error(ex, "Couldn't read AnkiConfig");
                    return null;
                }
            }

            else
            {
                // Utils.Frontend.Alert(AlertLevel.Error, "AnkiConfig.json doesn't exist");
                Utils.Logger.Warning("AnkiConfig.json doesn't exist");
                return null;
            }
        }

        return s_ankiConfigDict;
    }
}
