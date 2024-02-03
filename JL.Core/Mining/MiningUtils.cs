using System.Globalization;
using System.Text;
using JL.Core.Audio;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.Mining.Anki;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;

namespace JL.Core.Mining;

public static class MiningUtils
{
    private static Dictionary<JLField, string> GetMiningParameters(LookupResult lookupResult, string currentText, string? selectedDefinitions, int currentCharPosition, bool replaceLineBreakWithBrTag)
    {
        Dictionary<JLField, string> miningParams = new()
        {
            [JLField.LocalTime] = DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
            [JLField.SourceText] = replaceLineBreakWithBrTag ? currentText.ReplaceLineEndings("<br/>") : currentText,
            [JLField.Sentence] = JapaneseUtils.FindSentence(currentText, currentCharPosition),
            [JLField.DictionaryName] = lookupResult.Dict.Name,
            [JLField.MatchedText] = lookupResult.MatchedText,
            [JLField.DeconjugatedMatchedText] = lookupResult.DeconjugatedMatchedText,
            [JLField.PrimarySpelling] = lookupResult.PrimarySpelling,
            [JLField.PrimarySpellingWithOrthographyInfo] = lookupResult.PrimarySpellingOrthographyInfoList is not null
        ? string.Create(CultureInfo.InvariantCulture, $"{lookupResult.PrimarySpelling} ({string.Join(", ", lookupResult.PrimarySpellingOrthographyInfoList)})")
        : lookupResult.PrimarySpelling
        };

        if (lookupResult.Readings is not null)
        {
            string readings = string.Join(", ", lookupResult.Readings);
            miningParams[JLField.Readings] = readings;

            miningParams[JLField.ReadingsWithOrthographyInfo] = lookupResult.ReadingsOrthographyInfoList is not null
                ? LookupResultUtils.ReadingsToText(lookupResult.Readings, lookupResult.ReadingsOrthographyInfoList)
                : readings;
        }

        if (lookupResult.AlternativeSpellings is not null)
        {
            string alternativeSpellings = string.Join(", ", lookupResult.AlternativeSpellings);
            miningParams[JLField.AlternativeSpellings] = alternativeSpellings;

            miningParams[JLField.AlternativeSpellingsWithOrthographyInfo] = lookupResult.AlternativeSpellingsOrthographyInfoList is not null
                ? LookupResultUtils.ReadingsToText(lookupResult.AlternativeSpellings, lookupResult.AlternativeSpellingsOrthographyInfoList)
                : alternativeSpellings;
        }

        if (lookupResult.Frequencies is not null)
        {
            string? formattedFreq = LookupResultUtils.FrequenciesToText(lookupResult.Frequencies, true);
            if (formattedFreq is not null)
            {
                miningParams[JLField.Frequencies] = formattedFreq;
                miningParams[JLField.RawFrequencies] = string.Join(", ", lookupResult.Frequencies
                    .Where(static f => f.Freq is > 0 and < int.MaxValue)
                    .Select(static f => f.Freq));
            }
        }

        if (lookupResult.FormattedDefinitions is not null)
        {
            string formattedDefinitions = replaceLineBreakWithBrTag
                ? lookupResult.FormattedDefinitions.ReplaceLineEndings("<br/>")
                : lookupResult.FormattedDefinitions;

            miningParams[JLField.Definitions] = formattedDefinitions;

            if (selectedDefinitions is null)
            {
                miningParams[JLField.SelectedDefinitions] = formattedDefinitions;
            }
        }

        if (selectedDefinitions is not null)
        {
            miningParams[JLField.SelectedDefinitions] = replaceLineBreakWithBrTag
                ? selectedDefinitions.ReplaceLineEndings("<br/>")
                : selectedDefinitions;
        }

        if (lookupResult.EdictId > 0)
        {
            miningParams[JLField.EdictId] = lookupResult.EdictId.ToString(CultureInfo.InvariantCulture);
        }

        if (lookupResult.DeconjugationProcess is not null)
        {
            miningParams[JLField.DeconjugationProcess] = lookupResult.DeconjugationProcess;
        }

        if (lookupResult.KanjiComposition is not null)
        {
            miningParams[JLField.KanjiComposition] = lookupResult.KanjiComposition;
        }

        if (lookupResult.KanjiStats is not null)
        {
            miningParams[JLField.KanjiStats] = lookupResult.KanjiStats;
        }

        if (lookupResult.StrokeCount > 0)
        {
            miningParams[JLField.StrokeCount] = lookupResult.StrokeCount.ToString(CultureInfo.InvariantCulture);
        }

        if (lookupResult.KanjiGrade > -1)
        {
            miningParams[JLField.KanjiGrade] = lookupResult.KanjiGrade.ToString(CultureInfo.InvariantCulture);
        }

        if (lookupResult.OnReadings is not null)
        {
            miningParams[JLField.OnReadings] = string.Join(", ", lookupResult.OnReadings);
        }

        if (lookupResult.KunReadings is not null)
        {
            miningParams[JLField.KunReadings] = string.Join(", ", lookupResult.KunReadings);
        }

        if (lookupResult.NanoriReadings is not null)
        {
            miningParams[JLField.NanoriReadings] = string.Join(", ", lookupResult.NanoriReadings);
        }

        if (lookupResult.RadicalNames is not null)
        {
            miningParams[JLField.RadicalNames] = string.Join(", ", lookupResult.RadicalNames);
        }

        return miningParams;
    }

    public static async Task MineToFile(LookupResult lookupResult, string currentText, string? selectedDefinitions, int currentCharPosition)
    {
        string filePath;
        JLField[] jlFields;
        if (DictUtils.s_wordDictTypes.Contains(lookupResult.Dict.Type))
        {
            filePath = Path.Join(Utils.ResourcesPath, "mined_words.txt");
            jlFields = JLFieldUtils.JLFieldsForWordDicts;
        }
        else if (DictUtils.s_nameDictTypes.Contains(lookupResult.Dict.Type))
        {
            filePath = Path.Join(Utils.ResourcesPath, "mined_names.txt");
            jlFields = JLFieldUtils.JLFieldsForNameDicts;
        }
        else if (DictUtils.s_kanjiDictTypes.Contains(lookupResult.Dict.Type))
        {
            filePath = Path.Join(Utils.ResourcesPath, "mined_kanjis.txt");
            jlFields = JLFieldUtils.JLFieldsForKanjiDicts;
        }
        else
        {
            filePath = Path.Join(Utils.ResourcesPath, "mined_others.txt");
            jlFields = JLFieldUtils.JLFieldsForWordDicts;
        }

        Dictionary<JLField, string> miningParameters = GetMiningParameters(lookupResult, currentText, selectedDefinitions, currentCharPosition, false);
        StringBuilder lineToMine = new();
        for (int i = 1; i < jlFields.Length; i++)
        {
            JLField jlField = jlFields[i];
            if (jlField is JLField.Audio or JLField.Image)
            {
                continue;
            }

            string? jlFieldContent = miningParameters.GetValueOrDefault(jlField)?.ReplaceLineEndings("\\n").Replace("\t", "  ", StringComparison.Ordinal).Trim();
            if (!string.IsNullOrEmpty(jlFieldContent))
            {
                _ = lineToMine.Append(CultureInfo.InvariantCulture, $"{jlField.GetDescription()}: ")
                    .Append(jlFieldContent)
                    .Append(i < (jlFields.Length - 1) ? '\t' : '\n');
            }
        }

        await File.AppendAllTextAsync(filePath, lineToMine.ToString()).ConfigureAwait(false);

        Stats.IncrementStat(StatType.CardsMined);

        Utils.Frontend.Alert(AlertLevel.Success, string.Create(CultureInfo.InvariantCulture, $"Mined {lookupResult.PrimarySpelling}"));
        Utils.Logger.Information("Mined {FoundSpelling}", lookupResult.PrimarySpelling);
    }

    public static async Task Mine(LookupResult lookupResult, string currentText, string? selectedDefinitions, int currentCharPosition)
    {
        if (!CoreConfig.AnkiIntegration)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return;
        }

        Dictionary<MineType, AnkiConfig>? ankiConfigDict = await AnkiConfig.ReadAnkiConfig().ConfigureAwait(false);

        if (ankiConfigDict is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return;
        }

        AnkiConfig? ankiConfig;
        if (DictUtils.s_wordDictTypes.Contains(lookupResult.Dict.Type))
        {
            _ = ankiConfigDict.TryGetValue(MineType.Word, out ankiConfig);
        }

        else if (DictUtils.s_kanjiDictTypes.Contains(lookupResult.Dict.Type))
        {
            _ = ankiConfigDict.TryGetValue(MineType.Kanji, out ankiConfig);
        }

        else if (DictUtils.s_nameDictTypes.Contains(lookupResult.Dict.Type))
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
            return;
        }

        Dictionary<string, JLField> userFields = ankiConfig.Fields;
        List<string> audioFields = FindFields(JLField.Audio, userFields);
        bool needsAudio = audioFields.Count > 0;
        string reading = lookupResult.Readings?[0] ?? lookupResult.PrimarySpelling;

        AudioResponse? audioResponse = needsAudio
            ? await AudioUtils.GetPrioritizedAudio(lookupResult.PrimarySpelling, reading).ConfigureAwait(false)
            : null;

        byte[]? audioData = audioResponse?.AudioData;

        if (audioResponse?.AudioSource is AudioSourceType.TextToSpeech)
        {
            string voiceName = AudioUtils.AudioSources
                .Where(static a => a.Value is { Active: true, Type: AudioSourceType.TextToSpeech })
                .Aggregate(static (a1, a2) => a1.Value.Priority < a2.Value.Priority ? a1 : a2).Key;

            audioData = Utils.Frontend.GetAudioResponseFromTextToSpeech(voiceName, reading);
        }

        Dictionary<string, object>? audio = audioData is null
            ? null
            : new Dictionary<string, object>(4)
            {
                { "data", audioData },
                { "filename", string.Create(CultureInfo.InvariantCulture, $"JL_audio_{reading}_{lookupResult.PrimarySpelling}.{audioResponse!.AudioFormat}") },
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
            : new Dictionary<string, object>(3)
            {
                { "data", imageBytes },
                { "filename", string.Create(CultureInfo.InvariantCulture, $"JL_image_{reading}_{lookupResult.PrimarySpelling}.png") },
                { "fields", imageFields }
            };

        Dictionary<string, object> options = new(1)
        {
            { "allowDuplicate", CoreConfig.AllowDuplicateCards }
        };

        Dictionary<JLField, string> miningParams = GetMiningParameters(lookupResult, currentText, selectedDefinitions, currentCharPosition, true);

        Dictionary<string, object> fields = ConvertFields(userFields, miningParams);

        Note note = new(ankiConfig.DeckName, ankiConfig.ModelName, fields, options, ankiConfig.Tags, audio, null, image);
        Response? response = await AnkiConnect.AddNoteToDeck(note).ConfigureAwait(false);

        if (response is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Mining failed for {lookupResult.PrimarySpelling}"));
            Utils.Logger.Error("Mining failed for {FoundSpelling}", lookupResult.PrimarySpelling);
            return;
        }

        if (needsAudio && (audioData is null || Utils.GetMd5String(audioData) is Networking.Jpod101NoAudioMd5Hash))
        {
            Utils.Frontend.Alert(AlertLevel.Warning, string.Create(CultureInfo.InvariantCulture, $"Mined {lookupResult.PrimarySpelling} (no audio)"));
            Utils.Logger.Information("Mined {FoundSpelling} (no audio)", lookupResult.PrimarySpelling);
        }

        else
        {
            Utils.Frontend.Alert(AlertLevel.Success, string.Create(CultureInfo.InvariantCulture, $"Mined {lookupResult.PrimarySpelling}"));
            Utils.Logger.Information("Mined {FoundSpelling}", lookupResult.PrimarySpelling);
        }

        if (CoreConfig.ForceSyncAnki)
        {
            await AnkiConnect.Sync().ConfigureAwait(false);
        }

        Stats.IncrementStat(StatType.CardsMined);
    }

    /// <summary>
    /// Converts JLField,Value pairs to UserField,Value pairs <br/>
    /// JLField is our internal name of a mining field <br/>
    /// Value is the actual content of a mining field (e.g. if the field name is LocalTime, then it should contain the current time) <br/>
    /// UserField is the name of the user's field in Anki (e.g. Expression) <br/>
    /// </summary>
    private static Dictionary<string, object> ConvertFields(Dictionary<string, JLField> userFields, Dictionary<JLField, string> miningParams)
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
