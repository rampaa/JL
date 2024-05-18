using System.Globalization;
using System.Text;
using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Freqs;
using JL.Core.Lookup;
using JL.Core.Mining.Anki;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;

namespace JL.Core.Mining;

public static class MiningUtils
{
    private const string PitchAccentStyle =
        """
        <style>
          .dotted-line-on-bottom,
          .dotted-line-on-top,
          .dotted-line-on-bottom-right,
          .dotted-line-on-top-right {
            position: relative;
            display: inline-block;
          }

          .dotted-line-on-bottom:after {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: calc(100% - 0.7px);
            height: 100%;
            border-bottom: 1px dotted currentColor;
            pointer-events: none;
          }

          .dotted-line-on-top:after {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: calc(100% - 0.7px);
            height: 100%;
            border-top: 1px dotted currentColor;
            pointer-events: none;
          }

          .dotted-line-on-bottom-right:after {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: calc(100% - 0.7px);
            height: 100%;
            border-bottom: 1px dotted currentColor;
            border-right: 1px dotted currentColor;
            pointer-events: none;
          }

          .dotted-line-on-top-right:after {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: calc(100% - 0.7px);
            height: 100%;
            border-top: 1px dotted currentColor;
            border-right: 1px dotted currentColor;
            pointer-events: none;
          }
        </style>
        """;

    private static Dictionary<JLField, string> GetMiningParameters(LookupResult lookupResult, string currentText, string? selectedDefinitions, int currentCharPosition, bool replaceLineBreakWithBrTag)
    {
        Dictionary<JLField, string> miningParams = new()
        {
            [JLField.LocalTime] = DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
            [JLField.SourceText] = replaceLineBreakWithBrTag ? currentText.ReplaceLineEndings("<br/>") : currentText,
            [JLField.DictionaryName] = lookupResult.Dict.Name,
            [JLField.MatchedText] = lookupResult.MatchedText,
            [JLField.DeconjugatedMatchedText] = lookupResult.DeconjugatedMatchedText,
            [JLField.PrimarySpelling] = lookupResult.PrimarySpelling,
            [JLField.PrimarySpellingWithOrthographyInfo] = lookupResult.PrimarySpellingOrthographyInfoList is not null
                ? $"{lookupResult.PrimarySpelling} ({string.Join(", ", lookupResult.PrimarySpellingOrthographyInfoList)})"
                : lookupResult.PrimarySpelling
        };

        string sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
        miningParams[JLField.Sentence] = sentence;

        int searchStartIndex = currentCharPosition + lookupResult.MatchedText.Length - sentence.Length;
        if (searchStartIndex < 0 || searchStartIndex >= currentText.Length)
        {
            searchStartIndex = 0;
        }

        int sentenceStartIndex = currentText.IndexOf(sentence, searchStartIndex, StringComparison.Ordinal);
        miningParams[JLField.LeadingSentencePart] = currentText[sentenceStartIndex..currentCharPosition];
        miningParams[JLField.TrailingSentencePart] = currentText[(lookupResult.MatchedText.Length + currentCharPosition)..(sentenceStartIndex + sentence.Length)];

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
            List<LookupFrequencyResult> validFrequencies = lookupResult.Frequencies
                .Where(static f => f.Freq is > 0 and < int.MaxValue).ToList();

            if (validFrequencies.Count > 0)
            {
                miningParams[JLField.Frequencies] = LookupResultUtils.FrequenciesToText(lookupResult.Frequencies, true, lookupResult.Frequencies.Count is 1);
                miningParams[JLField.RawFrequencies] = string.Join(", ", validFrequencies.Select(static f => f.Freq).ToList());
                miningParams[JLField.FrequencyHarmonicMean] = CalculateHarmonicMean(validFrequencies).ToString(CultureInfo.InvariantCulture);

                int firstFrequency = lookupResult.Frequencies[0].Freq;
                if (firstFrequency is > 0 and < int.MaxValue)
                {
                    miningParams[JLField.Frequencies] = firstFrequency.ToString(CultureInfo.InvariantCulture);
                }
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

        if (lookupResult.KanjiGrade is not byte.MaxValue)
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

        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict) && pitchDict.Active)
        {
            List<KeyValuePair<string, byte>>? pitchAccents = GetPitchAccents(lookupResult.PitchAccentDict ?? pitchDict.Contents, lookupResult);
            if (pitchAccents is not null)
            {
                StringBuilder expressionsWithPitchAccentBuilder = new();
                _ = expressionsWithPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n");

                StringBuilder numericPitchAccentBuilder = new();
                int pitchAccentCount = pitchAccents.Count;
                for (int i = 0; i < pitchAccentCount; i++)
                {
                    KeyValuePair<string, byte> pitchAccent = pitchAccents[i];
                    _ = numericPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{pitchAccent.Key}: {pitchAccent.Value}");
                    _ = expressionsWithPitchAccentBuilder.Append(GetExpressionWithPitchAccent(pitchAccent.Key, pitchAccent.Value));

                    if (i + 1 != pitchAccentCount)
                    {
                        _ = numericPitchAccentBuilder.Append(", ");
                        _ = expressionsWithPitchAccentBuilder.Append(", ");
                    }
                }

                miningParams[JLField.NumericPitchAccents] = numericPitchAccentBuilder.ToString();
                miningParams[JLField.PitchAccents] = expressionsWithPitchAccentBuilder.ToString();
            }
        }

        return miningParams;
    }

    private static int CalculateHarmonicMean(List<LookupFrequencyResult> lookupFrequencyResults)
    {
        double sumOfReciprocalOfFreqs = 0;
        for (int i = 0; i < lookupFrequencyResults.Count; i++)
        {
            LookupFrequencyResult lookupFrequencyResult = lookupFrequencyResults[i];

            int freq = lookupFrequencyResult.HigherValueMeansHigherFrequency
                ? FreqUtils.FreqDicts[lookupFrequencyResult.Name].MaxValue - lookupFrequencyResult.Freq + 1
                : lookupFrequencyResult.Freq;

            sumOfReciprocalOfFreqs += 1d / freq;
        }

        return (int)Math.Round(lookupFrequencyResults.Count / sumOfReciprocalOfFreqs);
    }

    private static List<KeyValuePair<string, byte>>? GetPitchAccents(IDictionary<string, IList<IDictRecord>> pitchDict, LookupResult lookupResult)
    {
        List<KeyValuePair<string, byte>> pitchAccents = [];

        if (lookupResult.Readings is not null)
        {
            for (int i = 0; i < lookupResult.Readings.Length; i++)
            {
                string reading = lookupResult.Readings[i];
                string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading);

                if (pitchDict.TryGetValue(readingInHiragana, out IList<IDictRecord>? pitchResult))
                {
                    int pitchResultCount = pitchResult.Count;
                    for (int j = 0; j < pitchResultCount; j++)
                    {
                        PitchAccentRecord pitchAccentRecord = (PitchAccentRecord)pitchResult[j];
                        if (lookupResult.PrimarySpelling == pitchAccentRecord.Spelling
                            || (lookupResult.AlternativeSpellings?.Contains(pitchAccentRecord.Spelling) ?? false))
                        {
                            pitchAccents.Add(KeyValuePair.Create(reading, pitchAccentRecord.Position));
                            break;
                        }
                    }
                }
            }
        }

        else
        {
            string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(lookupResult.PrimarySpelling);
            if (pitchDict.TryGetValue(primarySpellingInHiragana, out IList<IDictRecord>? pitchResult))
            {
                int pitchResultCount = pitchResult.Count;
                for (int i = 0; i < pitchResultCount; i++)
                {
                    PitchAccentRecord pitchAccentRecord = (PitchAccentRecord)pitchResult[i];
                    if (pitchAccentRecord.Reading is null)
                    {
                        pitchAccents.Add(KeyValuePair.Create(lookupResult.PrimarySpelling, pitchAccentRecord.Position));
                        break;
                    }
                }
            }

            else if (lookupResult.AlternativeSpellings is not null)
            {
                for (int i = 0; i < lookupResult.AlternativeSpellings.Length; i++)
                {
                    string alternativeSpellingInHiragana = JapaneseUtils.KatakanaToHiragana(lookupResult.AlternativeSpellings[i]);
                    if (pitchDict.TryGetValue(alternativeSpellingInHiragana, out pitchResult))
                    {
                        int pitchResultCount = pitchResult.Count;
                        for (int j = 0; j < pitchResultCount; j++)
                        {
                            PitchAccentRecord pitchAccentRecord = (PitchAccentRecord)pitchResult[j];
                            if (pitchAccentRecord.Reading is null)
                            {
                                pitchAccents.Add(KeyValuePair.Create(lookupResult.PrimarySpelling, pitchAccentRecord.Position));
                                break;
                            }
                        }
                    }
                }
            }
        }

        return pitchAccents.Count > 0 ? pitchAccents : null;
    }

    private static StringBuilder GetExpressionWithPitchAccent(string expression, byte position)
    {
        bool lowPitch = false;
        StringBuilder expressionWithPitchAccentStringBuilder = new();
        List<string> combinedFormList = JapaneseUtils.CreateCombinedForm(expression);
        int combinedFormListCount = combinedFormList.Count;
        for (int i = 0; i < combinedFormListCount; i++)
        {
            if (i == position - 1)
            {
                _ = expressionWithPitchAccentStringBuilder.Append(CultureInfo.InvariantCulture, $"<span class=\"dotted-line-on-top-right\">{combinedFormList[i]}</span>");
                lowPitch = true;
            }
            else if (i is 0)
            {
                _ = expressionWithPitchAccentStringBuilder.Append(CultureInfo.InvariantCulture, $"<span class=\"dotted-line-on-bottom-right\">{combinedFormList[i]}</span>");
            }
            else
            {
                _ = expressionWithPitchAccentStringBuilder.Append(CultureInfo.InvariantCulture, $"<span class=\"dotted-line-on-{(lowPitch ? "bottom" : "top")}\">{combinedFormList[i]}</span>");
            }
        }

        return expressionWithPitchAccentStringBuilder;
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
            if (jlField is JLField.Audio or JLField.Image or JLField.PitchAccents)
            {
                continue;
            }

            string? jlFieldContent = miningParameters.GetValueOrDefault(jlField)?.ReplaceLineEndings("\\n").Replace("\t", "  ", StringComparison.Ordinal).Trim();
            if (!string.IsNullOrEmpty(jlFieldContent))
            {
                _ = lineToMine.Append(CultureInfo.InvariantCulture, $"{jlField.GetDescription()}: ")
                    .Append(jlFieldContent)
                    .Append(i < jlFields.Length - 1 ? '\t' : '\n');
            }
        }

        await File.AppendAllTextAsync(filePath, lineToMine.ToString()).ConfigureAwait(false);

        Stats.IncrementStat(StatType.CardsMined);

        Utils.Frontend.Alert(AlertLevel.Success, $"Mined {lookupResult.PrimarySpelling}");
        Utils.Logger.Information("Mined {PrimarySpelling}", lookupResult.PrimarySpelling);
    }

    public static async Task Mine(LookupResult lookupResult, string currentText, string? selectedDefinitions, int currentCharPosition)
    {
        if (!CoreConfigManager.AnkiIntegration)
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
        Dictionary<JLField, string> miningParams = GetMiningParameters(lookupResult, currentText, selectedDefinitions, currentCharPosition, true);
        Dictionary<string, object> fields = ConvertFields(userFields, miningParams);

        // Audio/Picture/Video shouldn't be set here
        // Otherwise AnkiConnect will place them under the "collection.media" folder even when it's a duplicate note
        Note note = new(ankiConfig.DeckName, ankiConfig.ModelName, fields, ankiConfig.Tags, null, null, null, null);
        if (!CoreConfigManager.AllowDuplicateCards)
        {
            bool? canAddNote = await AnkiUtils.CanAddNote(note).ConfigureAwait(false);
            if (canAddNote is null)
            {
                Utils.Frontend.Alert(AlertLevel.Error, $"Mining failed for {lookupResult.PrimarySpelling}");
                Utils.Logger.Error("Mining failed for {PrimarySpelling}", lookupResult.PrimarySpelling);
                return;
            }

            if (!canAddNote.Value)
            {
                Utils.Frontend.Alert(AlertLevel.Error, $"Cannot mine {lookupResult.PrimarySpelling} because it is a duplicate card");
                Utils.Logger.Information("Cannot mine {PrimarySpelling} because it is a duplicate card", lookupResult.PrimarySpelling);
                return;
            }
        }

        List<string> audioFields = FindFields(JLField.Audio, userFields);
        bool needsAudio = audioFields.Count > 0;
        string reading = lookupResult.Readings?[0] ?? lookupResult.PrimarySpelling;

        AudioResponse? audioResponse = needsAudio
            ? await AudioUtils.GetPrioritizedAudio(lookupResult.PrimarySpelling, reading).ConfigureAwait(false)
            : null;

        byte[]? audioData = audioResponse?.AudioData;
        if (audioResponse?.AudioSource is AudioSourceType.TextToSpeech)
        {
            audioData = Utils.Frontend.GetAudioResponseFromTextToSpeech(reading);
        }

        if (audioData is not null)
        {
            note.Audio = new Dictionary<string, object>(4, StringComparer.Ordinal)
            {
                {
                    "data", audioData
                },
                {
                    "filename", $"JL_audio_{reading}_{lookupResult.PrimarySpelling}.{audioResponse!.AudioFormat}"
                },
                {
                    "skipHash", Networking.Jpod101NoAudioMd5Hash
                },
                {
                    "fields", audioFields
                }
            };
        }

        List<string> imageFields = FindFields(JLField.Image, userFields);
        bool needsImage = imageFields.Count > 0;
        byte[]? imageBytes = needsImage
            ? Utils.Frontend.GetImageFromClipboardAsByteArray()
            : null;

        if (imageBytes is not null)
        {
            note.Picture = new Dictionary<string, object>(3, StringComparer.Ordinal)
            {
                {
                    "data", imageBytes
                },
                {
                    "filename", $"JL_image_{reading}_{lookupResult.PrimarySpelling}.png"
                },
                {
                    "fields", imageFields
                }
            };
        }

        note.Options = new Dictionary<string, object>(1, StringComparer.Ordinal)
        {
            {
                "allowDuplicate", CoreConfigManager.AllowDuplicateCards
            }
        };

        Response? response = await AnkiConnect.AddNoteToDeck(note).ConfigureAwait(false);
        if (response is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, $"Mining failed for {lookupResult.PrimarySpelling}");
            Utils.Logger.Error("Mining failed for {PrimarySpelling}", lookupResult.PrimarySpelling);
            return;
        }

        if (needsAudio && (audioData is null || Utils.GetMd5String(audioData) is Networking.Jpod101NoAudioMd5Hash))
        {
            Utils.Frontend.Alert(AlertLevel.Warning, $"Mined {lookupResult.PrimarySpelling} (no audio)");
            Utils.Logger.Information("Mined {PrimarySpelling} (no audio)", lookupResult.PrimarySpelling);
        }

        else
        {
            Utils.Frontend.Alert(AlertLevel.Success, $"Mined {lookupResult.PrimarySpelling}");
            Utils.Logger.Information("Mined {PrimarySpelling}", lookupResult.PrimarySpelling);
        }

        if (CoreConfigManager.ForceSyncAnki)
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
    private static Dictionary<string, object> ConvertFields(Dictionary<string, JLField> userFields, IReadOnlyDictionary<JLField, string> miningParams)
    {
        Dictionary<string, object> dict = new(StringComparer.Ordinal);
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
