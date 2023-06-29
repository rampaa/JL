using System.Globalization;
using JL.Core.Audio;
using JL.Core.Dicts;
using JL.Core.Network;
using JL.Core.Utilities;

namespace JL.Core.Anki;

public static class Mining
{
    public static async Task<bool> Mine(Dictionary<JLField, string> miningParams)
    {
        string primarySpelling = miningParams[JLField.PrimarySpelling];

        Dictionary<MineType, AnkiConfig>? ankiConfigDict = await AnkiConfig.ReadAnkiConfig().ConfigureAwait(false);

        if (ankiConfigDict is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return false;
        }

        AnkiConfig? ankiConfig;

        DictType? dictType = null;

        if (miningParams.TryGetValue(JLField.DictionaryName, out string? dictionaryName))
        {
            if (DictUtils.Dicts.TryGetValue(dictionaryName, out Dict? dict))
            {
                dictType = dict.Type;
            }
        }

        if (dictType is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Mining failed for {primarySpelling}. Cannot find the type of {JLField.DictionaryName.GetDescription()}"));
            Utils.Logger.Error("Mining failed for {FoundSpelling}. Cannot find the type of {DictionaryName}", primarySpelling, JLField.DictionaryName.GetDescription());
            return false;
        }


        if (DictUtils.s_wordDictTypes.Contains(dictType.Value))
        {
            _ = ankiConfigDict.TryGetValue(MineType.Word, out ankiConfig);
        }

        else if (DictUtils.s_kanjiDictTypes.Contains(dictType.Value))
        {
            _ = ankiConfigDict.TryGetValue(MineType.Kanji, out ankiConfig);
        }

        else if (DictUtils.s_nameDictTypes.Contains(dictType.Value))
        {
            _ = ankiConfigDict.TryGetValue(MineType.Name, out ankiConfig);
        }

        else
        {
            _ = ankiConfigDict.TryGetValue(MineType.Other, out ankiConfig);
        }

        if (ankiConfig is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return false;
        }

        // idk if this gets the right audio for every word
        string? reading = miningParams.GetValueOrDefault(JLField.Readings)?.Split(',')[0];
        if (string.IsNullOrEmpty(reading))
        {
            reading = primarySpelling;
        }

        Dictionary<string, JLField> userFields = ankiConfig.Fields;
        Dictionary<string, object> fields = ConvertFields(userFields, miningParams);

        List<string> audioFields = FindFields(JLField.Audio, userFields);
        bool needsAudio = audioFields.Count > 0;
        AudioResponse? audioResponse = needsAudio
            ? await AudioUtils.GetPrioritizedAudio(primarySpelling, reading).ConfigureAwait(false)
            : null;

        Dictionary<string, object>? audio = audioResponse is null
            ? null
            : new Dictionary<string, object>
            {
                { "data", audioResponse.AudioData },
                { "filename", string.Create(CultureInfo.InvariantCulture, $"JL_audio_{reading}_{primarySpelling}.{audioResponse.AudioFormat}") },
                { "skipHash", Networking.Jpod101NoAudioMd5Hash },
                { "fields", audioFields }
            };

        List<string> imageFields = FindFields(JLField.Image, userFields);
        bool needsImage = imageFields.Count > 0;
        byte[]? imageBytes = needsImage
            ? Utils.Frontend.GetImageFromClipboardAsByteArray()
            : null;

        Dictionary<string, object>? image = imageBytes is null
            ? null
            : new Dictionary<string, object>
            {
                { "data", imageBytes },
                { "filename", string.Create(CultureInfo.InvariantCulture, $"JL_image_{reading}_{primarySpelling}.png") },
                { "fields", imageFields }
            };

        Dictionary<string, object> options = new()
        {
            { "allowDuplicate", CoreConfig.AllowDuplicateCards }
        };

        Note note = new(ankiConfig.DeckName, ankiConfig.ModelName, fields, options, ankiConfig.Tags, audio, null, image);
        Response? response = await AnkiConnect.AddNoteToDeck(note).ConfigureAwait(false);

        if (response is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Mining failed for {primarySpelling}"));
            Utils.Logger.Error("Mining failed for {FoundSpelling}", primarySpelling);
            return false;
        }

        if (needsAudio && (audioResponse is null || Utils.GetMd5String(audioResponse.AudioData) is Networking.Jpod101NoAudioMd5Hash))
        {
            Utils.Frontend.Alert(AlertLevel.Warning, string.Create(CultureInfo.InvariantCulture, $"Mined {primarySpelling} (no audio)"));
            Utils.Logger.Information("Mined {FoundSpelling} (no audio)", primarySpelling);
        }

        else
        {
            Utils.Frontend.Alert(AlertLevel.Success, string.Create(CultureInfo.InvariantCulture, $"Mined {primarySpelling}"));
            Utils.Logger.Information("Mined {FoundSpelling}", primarySpelling);
        }

        if (CoreConfig.ForceSyncAnki)
        {
            await AnkiConnect.Sync().ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>
    /// Converts JLField,Value pairs to UserField,Value pairs <br/>
    /// JLField is our internal name of a mining field <br/>
    /// Value is the actual content of a mining field (e.g. if the field name is LocalTime, then it should contain the current time) <br/>
    /// UserField is the name of the user's field in Anki (e.g. Expression) <br/>
    /// </summary>
    private static Dictionary<string, object> ConvertFields(Dictionary<string, JLField> userFields,
        IReadOnlyDictionary<JLField, string> miningParams)
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

    private static List<string> FindFields(JLField jlField, Dictionary<string, JLField> userFields)
    {
        return userFields.Keys.Where(key => userFields[key] == jlField).ToList();
    }
}
