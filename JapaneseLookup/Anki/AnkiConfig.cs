using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        public static void CreateDefaultConfig()
        {
            WriteAnkiConfig(new AnkiConfig(
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
                    },
                    new[] {"JL"}
                )
            );
        }

        public static string WriteAnkiConfig(AnkiConfig ankiConfig)
        {
            try
            {
                Directory.CreateDirectory(@"../net5.0-windows/Config");
                File.WriteAllText(@"../net5.0-windows/Config/AnkiConfig.json",
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
                Debug.WriteLine(e);

                return null;
            }
        }
    }
}