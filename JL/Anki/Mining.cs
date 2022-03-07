using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JL.Utilities;

namespace JL.Anki
{
    public static class Mining
    {
        public static async Task<bool> Mine(Dictionary<JLField, string> miningParams)
        {
            string foundSpelling = miningParams[JLField.FoundSpelling];

            try
            {
                AnkiConfig ankiConfig = await AnkiConfig.ReadAnkiConfig().ConfigureAwait(false);
                if (ankiConfig == null) return false;

                string deckName = ankiConfig.DeckName;
                string modelName = ankiConfig.ModelName;

                Dictionary<string, JLField> userFields = ankiConfig.Fields;
                Dictionary<string, object> fields = ConvertFields(userFields, miningParams);

                Dictionary<string, object> options = new() { { "allowDuplicate", ConfigManager.AllowDuplicateCards }, };
                string[] tags = ankiConfig.Tags;

                // idk if this gets the right audio for every word
                string reading = miningParams[JLField.Readings].Split(",")[0];
                if (reading == "")
                    reading = foundSpelling;

                Dictionary<string, object>[] audio =
                {
                    new()
                    {
                        {
                            "url",
                            $"http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={foundSpelling}&kana={reading}"
                        },
                        { "filename", $"JL_audio_{reading}_{foundSpelling}.mp3" },
                        { "skipHash", "7e2c2f954ef6051373ba916f000168dc" },
                        { "fields", FindAudioFields(userFields) },
                    }
                };
                Dictionary<string, object>[] video = null;
                Dictionary<string, object>[] picture = null;

                Note note = new(deckName, modelName, fields, options, tags, audio, video, picture);
                Response response = await AnkiConnect.AddNoteToDeck(note).ConfigureAwait(false);

                if (response == null)
                {
                    Utils.Alert(AlertLevel.Error, $"Mining failed for {foundSpelling}");
                    Utils.Logger.Error($"Mining failed for {foundSpelling}");
                    return false;
                }
                else
                {
                    bool hasAudio = await AnkiConnect.CheckAudioField(Convert.ToInt64(response.Result.ToString()),
                        FindAudioFields(userFields)[0]);

                    if (hasAudio)
                    {
                        Utils.Alert(AlertLevel.Success, $"Mined {foundSpelling}");
                        Utils.Logger.Information($"Mined {foundSpelling}");
                    }
                    else
                    {
                        Utils.Alert(AlertLevel.Warning, $"Mined {foundSpelling} (no audio)");
                        Utils.Logger.Information($"Mined {foundSpelling} (no audio)");
                    }

                    if (ConfigManager.ForceSyncAnki) await AnkiConnect.Sync();
                    return true;
                }
            }
            catch (Exception e)
            {
                Utils.Alert(AlertLevel.Error, $"Mining failed for {foundSpelling}");
                Utils.Logger.Information(e, $"Mining failed for {foundSpelling}");
                return false;
            }
        }

        /// <summary>
        /// Converts JLField,Value pairs to UserField,Value pairs <br/>
        /// JLField is our internal name of a mining field <br/>
        /// Value is the actual content of a mining field (e.g. if the field name is TimeLocal, then it should contain the current time) <br/>
        /// UserField is the name of the user's field in Anki (e.g. Expression) <br/>
        /// </summary>
        private static Dictionary<string, object> ConvertFields(Dictionary<string, JLField> userFields,
            Dictionary<JLField, string> miningParams)
        {
            Dictionary<string, object> dict = new();
            foreach ((string key, JLField value) in userFields)
            {
                if (!string.IsNullOrEmpty(miningParams[value]))
                {
                    dict.Add(key, miningParams[value]);
                }
            }

            return dict;
        }

        private static List<string> FindAudioFields(Dictionary<string, JLField> fields)
        {
            List<string> audioFields = new();
            audioFields.AddRange(fields.Keys.Where(key => JLField.Audio.Equals(fields[key])));

            return audioFields;
        }
    }
}
