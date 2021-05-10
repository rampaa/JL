using System;
using System.Collections.Generic;
using System.Linq;

namespace JapaneseLookup.Anki
{
    // TODO: Exception handling
    public static class Mining
    {
        // TODO: Check if audio was grabbed and tell the user if it was not
        public static async void Mine(string foundSpelling, string readings, string definitions, string context,
            string foundForm, string jmdictID, string timeLocal, string alternativeSpellings,
            string frequency)
        {
            var ankiConfig = await AnkiConfig.ReadAnkiConfig();
            if (ankiConfig == null) return;

            var deckName = ankiConfig.deckName;
            var modelName = ankiConfig.modelName;

            var rawFields = ankiConfig.fields;
            var fields =
                ConvertFields(
                    rawFields,
                    foundSpelling,
                    readings,
                    definitions,
                    context,
                    foundForm,
                    jmdictID,
                    timeLocal,
                    alternativeSpellings,
                    frequency
                );

            Dictionary<string, object> options = null;
            var tags = ankiConfig.tags;

            // idk if this gets the right audio for every word
            var reading = readings.Split(",")[0];
            if (reading == "") reading = foundSpelling;

            Dictionary<string, object>[] audio =
            {
                new()
                {
                    {
                        "url",
                        $"http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={foundSpelling}&kana={reading}"
                    },
                    {
                        "filename",
                        $"JL_audio_{foundSpelling}_{reading}.mp3"
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

            if (ConfigManager.ForceSync) await AnkiConnect.Sync();
        }

        private static Dictionary<string, object> ConvertFields(Dictionary<string, JLField> fields,
            string foundSpelling, string readings, string definitions, string context,
            string foundForm, string jmdictID, string timeLocal, string alternativeSpellings, string frequency)
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
                    case JLField.FoundForm:
                        dict.Add(key, foundForm);
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
                    case JLField.AlternativeSpellings:
                        dict.Add(key, alternativeSpellings);
                        break;
                    case JLField.Frequency:
                        dict.Add(key, frequency);
                        break;
                    default:
                        // we should never reach here, but just in case
                        return null;
                }
            }

            return dict
                .Where(kvp => kvp.Value != null)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        private static List<string> FindAudioFields(Dictionary<string, JLField> fields)
        {
            var audioFields = new List<string>();
            audioFields.AddRange(fields.Keys.Where(key => JLField.Audio.Equals(fields[key])));

            return audioFields;
        }
    }
}