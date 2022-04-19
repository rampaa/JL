using JL.Core.Network;
using JL.Core.Utilities;

namespace JL.Core.Anki
{
    public static class Mining
    {
        public static async Task<bool> Mine(Dictionary<JLField, string> miningParams)
        {
            string foundSpelling = miningParams[JLField.FoundSpelling];

            try
            {
                AnkiConfig? ankiConfig = await AnkiConfig.ReadAnkiConfig().ConfigureAwait(false);
                if (ankiConfig == null)
                {
                    Storage.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
                    return false;
                }

                string deckName = ankiConfig.DeckName;
                string modelName = ankiConfig.ModelName;

                Dictionary<string, JLField> userFields = ankiConfig.Fields;
                Dictionary<string, object> fields = ConvertFields(userFields, miningParams);

                Dictionary<string, object> options = new()
                {
                    { "allowDuplicate", Storage.Frontend.CoreConfig.AllowDuplicateCards },
                };
                string[] tags = ankiConfig.Tags;

                // idk if this gets the right audio for every word
                string reading = miningParams[JLField.Readings].Split(",")[0];
                if (string.IsNullOrEmpty(reading))
                    reading = foundSpelling;

                byte[]? audioRes = await Networking.GetAudioFromJpod101(foundSpelling, reading);

                Dictionary<string, object?>[] audio =
                {
                    new()
                    {
                        { "data", audioRes },
                        { "filename", $"JL_audio_{reading}_{foundSpelling}.mp3" },
                        { "skipHash", Storage.Jpod101NoAudioMd5Hash },
                        { "fields", FindAudioFields(userFields) },
                    }
                };
                Dictionary<string, object>[]? video = null;
                Dictionary<string, object>[]? picture = null;

                Note note = new(deckName, modelName, fields, options, tags, audio, video, picture);
                Response? response = await AnkiConnect.AddNoteToDeck(note).ConfigureAwait(false);

                if (response == null)
                {
                    Storage.Frontend.Alert(AlertLevel.Error, $"Mining failed for {foundSpelling}");
                    Utils.Logger.Error("Mining failed for {FoundSpelling}", foundSpelling);
                    return false;
                }
                else
                {
                    if (audioRes == null || Utils.GetMd5String(audioRes) == Storage.Jpod101NoAudioMd5Hash)
                    {
                        Storage.Frontend.Alert(AlertLevel.Warning, $"Mined {foundSpelling} (no audio)");
                        Utils.Logger.Information("Mined {FoundSpelling} (no audio)", foundSpelling);
                    }

                    else
                    {
                        Storage.Frontend.Alert(AlertLevel.Success, $"Mined {foundSpelling}");
                        Utils.Logger.Information("Mined {FoundSpelling}", foundSpelling);
                    }

                    if (Storage.Frontend.CoreConfig.ForceSyncAnki)
                        await AnkiConnect.Sync();

                    return true;
                }
            }
            catch (Exception e)
            {
                Storage.Frontend.Alert(AlertLevel.Error, $"Mining failed for {foundSpelling}");
                Utils.Logger.Information(e, "Mining failed for {FoundSpelling}", foundSpelling);
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

        private static List<string> FindAudioFields(Dictionary<string, JLField> userFields)
        {
            List<string> audioFields = new();
            audioFields.AddRange(userFields.Keys.Where(key => JLField.Audio.Equals(userFields[key])));

            return audioFields;
        }
    }
}
