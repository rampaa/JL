using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.Anki
{
    public class AnkiConfig
    {
        [JsonPropertyName("deckName")] public string DeckName { get; set; }

        [JsonPropertyName("modelName")] public string ModelName { get; set; }

        [JsonPropertyName("fields")] public Dictionary<string, JLField> Fields { get; set; }

        [JsonPropertyName("tags")] public string[] Tags { get; set; }

        public AnkiConfig(string deckName, string modelName, Dictionary<string, JLField> fields, string[] tags)
        {
            DeckName = deckName;
            ModelName = modelName;
            Fields = fields;
            Tags = tags;
        }

        public static async Task CreateDefaultAnkiConfig()
        {
            await WriteAnkiConfig(new AnkiConfig(
                    "JLDeck",
                    "Japanese JL-Basic",
                    new Dictionary<string, JLField>
                    {
                        { "Edict ID", JLField.EdictID },
                        { "Expression", JLField.FoundSpelling },
                        { "Reading", JLField.Readings },
                        { "Gloss", JLField.Definitions },
                        { "Sentence", JLField.Context },
                        { "Audio", JLField.Audio },
                        { "Time", JLField.TimeLocal },
                    },
                    new[] { "JL" }
                )
            ).ConfigureAwait(false);
        }

        public static async Task<bool> WriteAnkiConfig(AnkiConfig ankiConfig)
        {
            try
            {
                Directory.CreateDirectory(Path.Join(Storage.ApplicationPath, "Config"));
                await File.WriteAllTextAsync(Path.Join(Storage.ApplicationPath, "Config/AnkiConfig.json"),
                    JsonSerializer.Serialize(ankiConfig,
                        new JsonSerializerOptions
                        {
                            // Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                            WriteIndented = true,
                            Converters = { new JsonStringEnumConverter() }
                        })
                ).ConfigureAwait(false);

                return true;
            }
            catch (Exception e)
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Couldn't write AnkiConfig");
                Utils.Logger.Error(e, "Couldn't write AnkiConfig");
                return false;
            }
        }

        public static async Task<AnkiConfig> ReadAnkiConfig()
        {
            if (File.Exists(Path.Join(Storage.ApplicationPath, "Config/AnkiConfig.json")))
            {
                try
                {
                    return JsonSerializer.Deserialize<AnkiConfig>(
                        await File.ReadAllTextAsync(Path.Join(Storage.ApplicationPath, "Config/AnkiConfig.json"))
                            .ConfigureAwait(false),
                        new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } });
                }

                catch
                {
                    Storage.Frontend.Alert(AlertLevel.Error, "Couldn't read AnkiConfig");
                    Utils.Logger.Error("Couldn't read AnkiConfig");
                    return null;
                }
            }

            else
            {
                // Storage.FrontEnd.Alert(AlertLevel.Error, "AnkiConfig.json doesn't exist");
                Utils.Logger.Error("AnkiConfig.json doesn't exist");
                return null;
            }
        }
    }
}
