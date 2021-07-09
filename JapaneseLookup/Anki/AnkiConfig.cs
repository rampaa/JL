using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        [JsonPropertyName("tags")]  public string[] Tags { get; set; }

        public AnkiConfig(string deckName, string modelName, Dictionary<string, JLField> fields, string[] tags)
        {
            DeckName = deckName;
            ModelName = modelName;
            Fields = fields;
            Tags = tags;
        }

        public static async void CreateDefaultConfig()
        {
            await WriteAnkiConfig(new AnkiConfig(
                    "JLDeck",
                    "Japanese JL-Basic",
                    new Dictionary<string, JLField>
                    {
                        {
                            "JMDict ID",
                            JLField.JMDictID
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
                    new[] {"JL"}
                )
            );
        }

        public static async Task<string> WriteAnkiConfig(AnkiConfig ankiConfig)
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
                );

                return "ok";
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't write AnkiConfig");
                Debug.WriteLine(e);

                return null;
            }
        }

        public static async Task<AnkiConfig> ReadAnkiConfig()
        {
            try
            {
                return JsonSerializer.Deserialize<AnkiConfig>(
                    await File.ReadAllTextAsync(Path.Join(ConfigManager.ApplicationPath, "Config/AnkiConfig.json")), new JsonSerializerOptions
                    {
                        Converters =
                        {
                            new JsonStringEnumConverter()
                        }
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't read AnkiConfig");
                Debug.WriteLine(e);

                return null;
            }
        }
    }
}