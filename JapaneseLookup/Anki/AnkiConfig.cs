using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace JapaneseLookup.Anki
{
    public class AnkiConfig
    {
        // camelCase property names because AnkiConnect
        #pragma warning disable IDE1006

        public string deckName { get; set; }

        public string modelName { get; set; }

        public Dictionary<string, JLField> fields { get; set; }

        public string[] tags { get; set; }

        public AnkiConfig(string deckName, string modelName, Dictionary<string, JLField> fields, string[] tags)
        {
            this.deckName = deckName;
            this.modelName = modelName;
            this.fields = fields;
            this.tags = tags;
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