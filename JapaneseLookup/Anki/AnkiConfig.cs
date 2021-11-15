using JapaneseLookup.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JapaneseLookup.Anki
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

        public static async Task CreateDefaultConfig()
        {
            await WriteAnkiConfig(new AnkiConfig(
                    "JLDeck",
                    "Japanese JL-Basic",
                    new Dictionary<string, JLField>
                    {
                        {
                            "Edict ID",
                            JLField.EdictID
                        },
                        {
                            "Expression",
                            JLField.FoundSpelling
                        },
                        {
                            "Reading",
                            JLField.Readings
                        },
                        {
                            "Gloss",
                            JLField.Definitions
                        },
                        {
                            "Sentence",
                            JLField.Context
                        },
                        {
                            "Audio",
                            JLField.Audio
                        },
                        {
                            "Time",
                            JLField.TimeLocal
                        },
                    },
                    new[] { "JapaneseLookup" }
                )
            ).ConfigureAwait(false);
        }

        public static async Task<bool> WriteAnkiConfig(AnkiConfig ankiConfig)
        {
            try
            {
                Directory.CreateDirectory(Path.Join(ConfigManager.ApplicationPath, "Config"));
                await File.WriteAllTextAsync(Path.Join(ConfigManager.ApplicationPath, "Config/AnkiConfig.json"),
                    JsonSerializer.Serialize(ankiConfig,
                        new JsonSerializerOptions
                        {
                            // Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                            WriteIndented = true,
                            Converters =
                            {
                                new JsonStringEnumConverter()
                            }
                        })
                ).ConfigureAwait(false);

                return true;
            }
            catch (Exception e)
            {
                Utils.logger.Information(e, "Couldn't write AnkiConfig");
                return false;
            }
        }

        public static async Task<AnkiConfig> ReadAnkiConfig()
        {
            try
            {
                return JsonSerializer.Deserialize<AnkiConfig>(
                    await File.ReadAllTextAsync(Path.Join(ConfigManager.ApplicationPath, "Config/AnkiConfig.json"))
                        .ConfigureAwait(false),
                    new JsonSerializerOptions
                    {
                        Converters =
                        {
                            new JsonStringEnumConverter()
                        }
                    });
            }
            catch (Exception e)
            {
                Utils.logger.Information(e, "Couldn't read AnkiConfig");
                return null;
            }
        }
    }
}