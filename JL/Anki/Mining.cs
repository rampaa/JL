using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JL.Utilities;

namespace JL.Anki
{
    public static class Mining
    {
        public static async Task<bool> Mine(MiningParams miningParams)
        {
            try
            {
                AnkiConfig ankiConfig = await AnkiConfig.ReadAnkiConfig().ConfigureAwait(false);
                if (ankiConfig == null) return false;

                string deckName = ankiConfig.DeckName;
                string modelName = ankiConfig.ModelName;

                var rawFields = ankiConfig.Fields;
                var fields = ConvertFields(rawFields, miningParams);

                Dictionary<string, object> options = new() { { "allowDuplicate", ConfigManager.AllowDuplicateCards }, };
                string[] tags = ankiConfig.Tags;

                // idk if this gets the right audio for every word
                string miningParamsReadingsClone = miningParams.Readings;
                miningParamsReadingsClone ??= "";
                string reading = miningParamsReadingsClone.Split(",")[0];
                if (reading == "") reading = miningParams.FoundSpelling;

                Dictionary<string, object>[] audio =
                {
                    new()
                    {
                        {
                            "url",
                            $"http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={miningParams.FoundSpelling}&kana={reading}"
                        },
                        { "filename", $"JL_audio_{reading}_{miningParams.FoundSpelling}.mp3" },
                        { "skipHash", "7e2c2f954ef6051373ba916f000168dc" },
                        { "fields", FindAudioFields(rawFields) },
                    }
                };
                Dictionary<string, object>[] video = null;
                Dictionary<string, object>[] picture = null;

                var note = new Note(deckName, modelName, fields, options, tags, audio, video, picture);
                Response response = await AnkiConnect.AddNoteToDeck(note).ConfigureAwait(false);

                if (response == null)
                {
                    Utils.Alert(AlertLevel.Error, $"Mining failed for {miningParams.FoundSpelling}");
                    Utils.Logger.Error($"Mining failed for {miningParams.FoundSpelling}");
                    return false;
                }
                else
                {
                    bool hasAudio = await AnkiConnect.CheckAudioField(Convert.ToInt64(response.Result.ToString()),
                        FindAudioFields(rawFields)[0]);

                    if (hasAudio)
                    {
                        Utils.Alert(AlertLevel.Success, $"Mined {miningParams.FoundSpelling}");
                        Utils.Logger.Information($"Mined {miningParams.FoundSpelling}");
                    }
                    else
                    {
                        Utils.Alert(AlertLevel.Warning, $"Mined {miningParams.FoundSpelling} (no audio)");
                        Utils.Logger.Information($"Mined {miningParams.FoundSpelling} (no audio)");
                    }

                    if (ConfigManager.ForceSyncAnki) await AnkiConnect.Sync();
                    return true;
                }
            }
            catch (Exception e)
            {
                Utils.Alert(AlertLevel.Error, $"Mining failed for {miningParams.FoundSpelling}");
                Utils.Logger.Information(e, $"Mining failed for {miningParams.FoundSpelling}");
                return false;
            }
        }

        private static Dictionary<string, object> ConvertFields(Dictionary<string, JLField> fields,
            MiningParams miningParams)
        {
            var dict = new Dictionary<string, object>();
            foreach ((string key, JLField value) in fields)
            {
                switch (value)
                {
                    case JLField.Nothing:
                        break;
                    case JLField.FoundSpelling:
                        dict.Add(key, miningParams.FoundSpelling);
                        break;
                    case JLField.Readings:
                        dict.Add(key, miningParams.Readings);
                        break;
                    case JLField.Definitions:
                        dict.Add(key, miningParams.Definitions);
                        break;
                    case JLField.FoundForm:
                        dict.Add(key, miningParams.FoundForm);
                        break;
                    case JLField.Context:
                        dict.Add(key, miningParams.Context);
                        break;
                    case JLField.Audio:
                        // needs to be handled separately (by FindAudioFields())
                        break;
                    case JLField.EdictID:
                        dict.Add(key, miningParams.EdictID);
                        break;
                    case JLField.TimeLocal:
                        dict.Add(key, miningParams.TimeLocal);
                        break;
                    case JLField.AlternativeSpellings:
                        dict.Add(key, miningParams.AlternativeSpellings);
                        break;
                    case JLField.Frequency:
                        dict.Add(key, miningParams.Frequency);
                        break;
                    case JLField.StrokeCount:
                        dict.Add(key, miningParams.StrokeCount);
                        break;
                    case JLField.Grade:
                        dict.Add(key, miningParams.Grade);
                        break;
                    case JLField.Composition:
                        dict.Add(key, miningParams.Composition);
                        break;
                    case JLField.DictType:
                        dict.Add(key, miningParams.DictType);
                        break;
                    case JLField.Process:
                        dict.Add(key, miningParams.Process);
                        break;
                    default:
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
