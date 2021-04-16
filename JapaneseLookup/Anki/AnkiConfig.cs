using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JapaneseLookup.Anki
{
    public class AnkiConfig
    {
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
            WriteConfig(new AnkiConfig(
                    "JLDeck",
                    "JL-Basic",
                    new Dictionary<string, JLField>
                    {
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

        public static void WriteConfig(AnkiConfig ankiConfig)
        {
            File.WriteAllText(@"../net5.0-windows/Config/AnkiConfig.json",
                JsonSerializer.Serialize(ankiConfig,
                    new JsonSerializerOptions
                    {
                        // Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = true,
                    })
            );
        }

        // await AnkiConnect.GetModelFieldNames(modelName);
    }
}