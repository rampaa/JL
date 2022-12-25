using JL.Core.Dicts;
using JL.Core.Network;
using JL.Core.Utilities;

namespace JL.Core.Anki;

public static class Mining
{
    public static async Task<bool> Mine(Dictionary<JLField, string> miningParams)
    {
        string primarySpelling = miningParams[JLField.PrimarySpelling];

        try
        {
            Dictionary<MineType, AnkiConfig>? ankiConfigDict = await AnkiConfig.ReadAnkiConfig().ConfigureAwait(false);

            if (ankiConfigDict == null)
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
                return false;
            }

            AnkiConfig? ankiConfig;

            DictType dictType = Storage.Dicts[miningParams[JLField.DictionaryName]].Type;

            if (Storage.WordDictTypes.Contains(dictType))
            {
                ankiConfigDict.TryGetValue(MineType.Word, out ankiConfig);
            }

            else if (Storage.KanjiDictTypes.Contains(dictType))
            {
                ankiConfigDict.TryGetValue(MineType.Kanji, out ankiConfig);
            }

            else if (Storage.NameDictTypes.Contains(dictType))
            {
                ankiConfigDict.TryGetValue(MineType.Name, out ankiConfig);
            }

            else
            {
                ankiConfigDict.TryGetValue(MineType.Other, out ankiConfig);
            }

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
            string? reading = miningParams.GetValueOrDefault(JLField.Readings)?.Split(",")[0];
            if (string.IsNullOrEmpty(reading))
                reading = primarySpelling;

            byte[]? audioRes = null;

            bool needsAudio = userFields.Values.Any(jlField => jlField == JLField.Audio);
            if (needsAudio)
            {
                audioRes = await Networking.GetAudioFromJpod101(primarySpelling, reading).ConfigureAwait(false);
            }

            Dictionary<string, object?>[] audio =
            {
                new()
                {
                    { "data", audioRes },
                    { "filename", $"JL_audio_{reading}_{primarySpelling}.mp3" },
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
                Storage.Frontend.Alert(AlertLevel.Error, $"Mining failed for {primarySpelling}");
                Utils.Logger.Error("Mining failed for {FoundSpelling}", primarySpelling);
                return false;
            }
            else
            {
                if (needsAudio && (audioRes == null || Utils.GetMd5String(audioRes) == Storage.Jpod101NoAudioMd5Hash))
                {
                    Storage.Frontend.Alert(AlertLevel.Warning, $"Mined {primarySpelling} (no audio)");
                    Utils.Logger.Information("Mined {FoundSpelling} (no audio)", primarySpelling);
                }

                else
                {
                    Storage.Frontend.Alert(AlertLevel.Success, $"Mined {primarySpelling}");
                    Utils.Logger.Information("Mined {FoundSpelling}", primarySpelling);
                }

                if (Storage.Frontend.CoreConfig.ForceSyncAnki)
                    await AnkiConnect.Sync().ConfigureAwait(false);

                return true;
            }
        }
        catch (Exception ex)
        {
            Storage.Frontend.Alert(AlertLevel.Error, $"Mining failed for {primarySpelling}");
            Utils.Logger.Error(ex, "Mining failed for {FoundSpelling}", primarySpelling);
            return false;
        }
    }

    /// <summary>
    /// Converts JLField,Value pairs to UserField,Value pairs <br/>
    /// JLField is our internal name of a mining field <br/>
    /// Value is the actual content of a mining field (e.g. if the field name is LocalTime, then it should contain the current time) <br/>
    /// UserField is the name of the user's field in Anki (e.g. Expression) <br/>
    /// </summary>
    private static Dictionary<string, object> ConvertFields(Dictionary<string, JLField> userFields,
        Dictionary<JLField, string> miningParams)
    {
        Dictionary<string, object> dict = new();
        foreach ((string key, JLField value) in userFields)
        {
            string? fieldName = miningParams.GetValueOrDefault(value);
            if (!string.IsNullOrEmpty(fieldName))
            {
                dict.Add(key, fieldName);
            }
        }

        return dict;
    }

    private static List<string> FindAudioFields(Dictionary<string, JLField> userFields)
    {
        return userFields.Keys.Where(key => JLField.Audio == userFields[key]).ToList();
    }
}
