using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace JapaneseLookup.Anki
{
    // TODO: Exception handling
    public static class Mining
    {
        // TODO: this needs to be refreshed after the user changes the config
        private static readonly AnkiConfig AnkiConfig = JsonSerializer.Deserialize<AnkiConfig>(
            File.ReadAllText(@"../net5.0-windows/Config/AnkiConfig.json"));

        // TODO: HTML + CSS for notes
        // TODO: Make sure that a word having multiple readings doesn't break grabbing audio
        // TODO: Check if audio was grabbed and tell the user if it was not
        public static async void Mine(string foundSpelling, string readings, string definitions, string context,
            string definitionsRaw, string foundText, string jmdictID, string timeLocal)
        {
            var deckName = AnkiConfig.deckName;
            var modelName = AnkiConfig.modelName;

            var rawFields = AnkiConfig.fields;
            var fields =
                ConvertFields(
                    rawFields,
                    foundSpelling,
                    readings,
                    definitions,
                    context,
                    definitionsRaw,
                    foundText,
                    jmdictID,
                    timeLocal
                );

            Dictionary<string, object> options = null;
            var tags = AnkiConfig.tags;
            Dictionary<string, object>[] audio =
            {
                new()
                {
                    {
                        "url",
                        $"http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={foundSpelling}&kana={readings}"
                    },
                    {
                        "filename",
                        $"JL_{foundSpelling}_{readings}.mp3"
                    },
                    {
                        "skipHash",
                        "7e2c2f954ef6051373ba916f000168dc"
                    },
                    {
                        "fields",
                        FindAudioFields(rawFields)
                    },
                }
            };
            Dictionary<string, object>[] video = null;
            Dictionary<string, object>[] picture = null;

            var note = new Note(deckName, modelName, fields, options, tags, audio, video, picture);
            var response = await AnkiConnect.AddNoteToDeck(note);
            Console.WriteLine(response == null ? $"Mining failed for {foundSpelling}" : $"Mined {foundSpelling}");
        }

        private static Dictionary<string, object> ConvertFields(Dictionary<string, JLField> fields,
            string foundSpelling, string readings, string definitions, string context, string definitionsRaw,
            string foundText, string jmdictID, string timeLocal)
        {
            var dict = new Dictionary<string, object>();
            foreach (var (key, value) in fields)
            {
                switch (value)
                {
                    case JLField.Nothing:
                        break;
                    case JLField.FoundSpelling:
                        dict.Add(key, foundSpelling);
                        break;
                    case JLField.Readings:
                        dict.Add(key, readings);
                        break;
                    case JLField.Definitions:
                        dict.Add(key, definitions);
                        break;
                    case JLField.DefinitionsRaw:
                        dict.Add(key, definitionsRaw);
                        break;
                    case JLField.FoundText:
                        dict.Add(key, foundText);
                        break;
                    case JLField.Context:
                        dict.Add(key, context);
                        break;
                    case JLField.Audio:
                        // needs to be handled separately (by FindAudioFields())
                        break;
                    case JLField.JMDictID:
                        dict.Add(key, jmdictID);
                        break;
                    case JLField.TimeLocal:
                        dict.Add(key, timeLocal);
                        break;
                    default:
                        // we should never reach here, but just in case
                        return null;
                }
            }

            return dict;
        }

        private static List<string> FindAudioFields(Dictionary<string, JLField> fields)
        {
            var audioFields = new List<string>();
            audioFields.AddRange(fields.Keys.Where(key => JLField.Audio.Equals(fields[key])));

            return audioFields;
        }
    }
}