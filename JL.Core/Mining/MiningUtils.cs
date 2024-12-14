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

    private static string? GetMiningParameter(JLField field, LookupResult lookupResult, string currentText, int currentCharPosition)
    {
        switch (field)
        {
            case JLField.LeadingSentencePart:
            {
                string sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
                int searchStartIndex = currentCharPosition + lookupResult.MatchedText.Length - sentence.Length;
                if (searchStartIndex < 0 || searchStartIndex >= currentText.Length)
                {
                    searchStartIndex = 0;
                }

                int sentenceStartIndex = currentText.IndexOf(sentence, searchStartIndex, StringComparison.Ordinal);
                return currentText[sentenceStartIndex..currentCharPosition];
            }

            case JLField.TrailingSentencePart:
            {
                string sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
                int searchStartIndex = currentCharPosition + lookupResult.MatchedText.Length - sentence.Length;
                if (searchStartIndex < 0 || searchStartIndex >= currentText.Length)
                {
                    searchStartIndex = 0;
                }

                int sentenceStartIndex = currentText.IndexOf(sentence, searchStartIndex, StringComparison.Ordinal);
                return currentText[(lookupResult.MatchedText.Length + currentCharPosition)..(sentenceStartIndex + sentence.Length)];
            }

            case JLField.Sentence:
            {
                string sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
                int searchStartIndex = currentCharPosition + lookupResult.MatchedText.Length - sentence.Length;
                if (searchStartIndex < 0 || searchStartIndex >= currentText.Length)
                {
                    searchStartIndex = 0;
                }

                int sentenceStartIndex = currentText.IndexOf(sentence, searchStartIndex, StringComparison.Ordinal);
                string leadingSentencePart = currentText[sentenceStartIndex..currentCharPosition];
                string trailingSentencePart = currentText[(lookupResult.MatchedText.Length + currentCharPosition)..(sentenceStartIndex + sentence.Length)];
                return $"{leadingSentencePart}<b>{lookupResult.MatchedText}</b>{trailingSentencePart}";
            }

            case JLField.SourceText:
            {
                string leadingSourcePart = currentText[..currentCharPosition].ReplaceLineEndings("<br/>");
                string trailingSourcePart = currentText[(currentCharPosition + lookupResult.MatchedText.Length)..].ReplaceLineEndings("<br/>");
                return $"{leadingSourcePart}<b>{lookupResult.MatchedText}</b>{trailingSourcePart}".ReplaceLineEndings("<br/>");
            }

            case JLField.Readings:
                return lookupResult.Readings is not null
                    ? string.Join('、', lookupResult.Readings)
                    : null;

            case JLField.ReadingsWithOrthographyInfo:
                return lookupResult.ReadingsOrthographyInfoList is not null && lookupResult.Readings is not null
                    ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.Readings, lookupResult.ReadingsOrthographyInfoList)
                    : lookupResult.Readings is not null
                        ? string.Join('、', lookupResult.Readings)
                        : null;

            case JLField.FirstReading:
                return lookupResult.Readings?[0];

            case JLField.PrimarySpellingAndReadings:
                return lookupResult.Readings is not null
                    ? $"{lookupResult.PrimarySpelling}[{string.Join('、', lookupResult.Readings)}]"
                    : null;

            case JLField.PrimarySpellingAndFirstReading:
                return lookupResult.Readings is not null
                    ? $"{lookupResult.PrimarySpelling}[{lookupResult.Readings[0]}]"
                    : null;

            case JLField.PrimarySpellingWithOrthographyInfo:
                return lookupResult.PrimarySpellingOrthographyInfoList is not null
                    ? $"{lookupResult.PrimarySpelling} ({string.Join(", ", lookupResult.PrimarySpellingOrthographyInfoList)})"
                    : lookupResult.PrimarySpelling;

            case JLField.AlternativeSpellings:
                return lookupResult.AlternativeSpellings is not null
                    ? string.Join('、', lookupResult.AlternativeSpellings)
                    : null;

            case JLField.AlternativeSpellingsWithOrthographyInfo:
                return lookupResult.AlternativeSpellings is not null
                    ? lookupResult.AlternativeSpellingsOrthographyInfoList is not null
                        ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.AlternativeSpellings, lookupResult.AlternativeSpellingsOrthographyInfoList)
                        : string.Join('、', lookupResult.AlternativeSpellings)
                    : null;

            case JLField.MatchedText:
                return lookupResult.MatchedText;

            case JLField.PrimarySpelling:
                return lookupResult.PrimarySpelling;

            case JLField.DeconjugatedMatchedText:
                return lookupResult.DeconjugatedMatchedText;

            case JLField.KanjiStats:
                return lookupResult.KanjiStats;

            case JLField.OnReadings:
                return lookupResult.OnReadings is not null
                    ? string.Join('、', lookupResult.OnReadings)
                    : null;

            case JLField.KunReadings:
                return lookupResult.KunReadings is not null
                    ? string.Join('、', lookupResult.KunReadings)
                    : null;

            case JLField.NanoriReadings:
                return lookupResult.NanoriReadings is not null
                    ? string.Join('、', lookupResult.NanoriReadings)
                    : null;

            case JLField.EdictId:
                return lookupResult.EdictId > 0
                    ? lookupResult.EdictId.ToString(CultureInfo.InvariantCulture)
                    : null;

            case JLField.DeconjugationProcess:
                return lookupResult.DeconjugationProcess;

            case JLField.KanjiComposition:
                return lookupResult.KanjiComposition;

            case JLField.StrokeCount:
                return lookupResult.StrokeCount > 0
                    ? lookupResult.StrokeCount.ToString(CultureInfo.InvariantCulture)
                    : null;

            case JLField.KanjiGrade:
                return lookupResult.KanjiGrade != byte.MaxValue
                    ? lookupResult.KanjiGrade.ToString(CultureInfo.InvariantCulture)
                    : null;

            case JLField.RadicalNames:
                return lookupResult.RadicalNames is not null
                    ? string.Join('、', lookupResult.RadicalNames)
                    : null;

            case JLField.SelectedDefinitions:
            case JLField.Definitions:
                return lookupResult.FormattedDefinitions?.ReplaceLineEndings("<br/>");

            case JLField.LeadingSourceTextPart:
                return currentText[..currentCharPosition].ReplaceLineEndings("<br/>");

            case JLField.TrailingSourceTextPart:
                return currentText[(currentCharPosition + lookupResult.MatchedText.Length)..].ReplaceLineEndings("<br/>");

            case JLField.DictionaryName:
                return lookupResult.Dict.Name;

            case JLField.Frequencies:
                return lookupResult.Frequencies is not null
                    ? LookupResultUtils.FrequenciesToText(lookupResult.Frequencies, true, lookupResult.Frequencies.Count is 1)
                    : null;

            case JLField.RawFrequencies:
                if (lookupResult.Frequencies is not null)
                {
                    List<LookupFrequencyResult> validFrequencies = lookupResult.Frequencies.Where(static f => f.Freq is > 0 and < int.MaxValue).ToList();
                    return string.Join(", ", validFrequencies.Select(static f => f.Freq).ToList());
                }
                return null;

            case JLField.PreferredFrequency:
                if (lookupResult.Frequencies is not null)
                {
                    int firstFrequency = lookupResult.Frequencies[0].Freq;
                    if (firstFrequency is > 0 and < int.MaxValue)
                    {
                        return firstFrequency.ToString(CultureInfo.InvariantCulture);
                    }
                }
                return null;

            case JLField.FrequencyHarmonicMean:
                if (lookupResult.Frequencies is not null)
                {
                    List<LookupFrequencyResult> validFrequencies = lookupResult.Frequencies.Where(static f => f.Freq is > 0 and < int.MaxValue).ToList();
                    return CalculateHarmonicMean(validFrequencies).ToString(CultureInfo.InvariantCulture);
                }
                return null;

            case JLField.PitchAccents:
            {
                if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict) && pitchDict.Active)
                {
                    List<KeyValuePair<string, byte>>? pitchAccents = GetPitchAccents(lookupResult.PitchAccentDict ?? pitchDict.Contents, lookupResult);
                    if (pitchAccents is not null)
                    {
                        StringBuilder expressionsWithPitchAccentBuilder = new();
                        _ = expressionsWithPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n");

                        int pitchAccentCount = pitchAccents.Count;
                        for (int i = 0; i < pitchAccentCount; i++)
                        {
                            (string expression, byte position) = pitchAccents[i];
                            _ = expressionsWithPitchAccentBuilder.Append(GetExpressionWithPitchAccent(expression, position));

                            if (i + 1 != pitchAccentCount)
                            {
                                _ = expressionsWithPitchAccentBuilder.Append('、');
                            }
                        }

                        return expressionsWithPitchAccentBuilder.ToString();
                    }
                }
                return null;
            }

            case JLField.NumericPitchAccents:
            {
                if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict) && pitchDict.Active)
                {
                    List<KeyValuePair<string, byte>>? pitchAccents = GetPitchAccents(lookupResult.PitchAccentDict ?? pitchDict.Contents, lookupResult);
                    if (pitchAccents is not null)
                    {
                        StringBuilder numericPitchAccentBuilder = new();
                        int pitchAccentCount = pitchAccents.Count;
                        for (int i = 0; i < pitchAccentCount; i++)
                        {
                            (string expression, byte position) = pitchAccents[i];
                            _ = numericPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{expression}: {position}");

                            if (i + 1 != pitchAccentCount)
                            {
                                _ = numericPitchAccentBuilder.Append(", ");
                            }
                        }

                        return numericPitchAccentBuilder.ToString();
                    }
                }
                return null;
            }

            case JLField.PitchAccentForFirstReading:
            {
                if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict) && pitchDict.Active)
                {
                    List<KeyValuePair<string, byte>>? pitchAccents = GetPitchAccents(lookupResult.PitchAccentDict ?? pitchDict.Contents, lookupResult);
                    if (pitchAccents is not null)
                    {
                        (string expression, byte position) = pitchAccents[0];
                        if ((lookupResult.Readings is not null && expression == lookupResult.Readings[0])
                            || (lookupResult.Readings is null && expression == lookupResult.PrimarySpelling))
                        {
                            return string.Create(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n{GetExpressionWithPitchAccent(expression, position)}");
                        }
                    }
                }
                return null;
            }

            case JLField.NumericPitchAccentForFirstReading:
            {
                if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict) && pitchDict.Active)
                {
                    List<KeyValuePair<string, byte>>? pitchAccents = GetPitchAccents(lookupResult.PitchAccentDict ?? pitchDict.Contents, lookupResult);
                    if (pitchAccents is not null)
                    {
                        (string expression, byte position) = pitchAccents[0];
                        if ((lookupResult.Readings is not null && expression == lookupResult.Readings[0])
                            || (lookupResult.Readings is null && expression == lookupResult.PrimarySpelling))
                        {
                            return string.Create(CultureInfo.InvariantCulture, $"{expression}: {position}");
                        }
                    }
                }
                return null;
            }

            case JLField.Nothing:
            case JLField.Audio:
            case JLField.Image:
            case JLField.LocalTime:
            default:
                return null;
        }
    }

    private static Dictionary<JLField, string> GetMiningParameters(LookupResult lookupResult, string currentText, string? formattedDefinitions, string? selectedDefinitions, int currentCharPosition, bool useHtmlTags)
    {
        Dictionary<JLField, string> miningParams = new()
        {
            [JLField.LocalTime] = DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
            [JLField.DictionaryName] = lookupResult.Dict.Name,
            [JLField.MatchedText] = lookupResult.MatchedText,
            [JLField.DeconjugatedMatchedText] = lookupResult.DeconjugatedMatchedText,
            [JLField.PrimarySpelling] = lookupResult.PrimarySpelling,
            [JLField.PrimarySpellingWithOrthographyInfo] = lookupResult.PrimarySpellingOrthographyInfoList is not null
                ? $"{lookupResult.PrimarySpelling} ({string.Join(", ", lookupResult.PrimarySpellingOrthographyInfoList)})"
                : lookupResult.PrimarySpelling
        };

        string leadingSourcePart = currentText[..currentCharPosition];
        string trailingSourcePart = currentText[(currentCharPosition + lookupResult.MatchedText.Length)..];
        if (useHtmlTags)
        {
            leadingSourcePart = leadingSourcePart.ReplaceLineEndings("<br/>");
            trailingSourcePart = trailingSourcePart.ReplaceLineEndings("<br/>");
        }

        miningParams[JLField.LeadingSourceTextPart] = leadingSourcePart;
        miningParams[JLField.TrailingSourceTextPart] = trailingSourcePart;

        miningParams[JLField.SourceText] = useHtmlTags
                ? $"{leadingSourcePart}<b>{lookupResult.MatchedText}</b>{trailingSourcePart}".ReplaceLineEndings("<br/>")
                : currentText;

        string sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
        int searchStartIndex = currentCharPosition + lookupResult.MatchedText.Length - sentence.Length;
        if (searchStartIndex < 0 || searchStartIndex >= currentText.Length)
        {
            searchStartIndex = 0;
        }

        int sentenceStartIndex = currentText.IndexOf(sentence, searchStartIndex, StringComparison.Ordinal);
        string leadingSentencePart = currentText[sentenceStartIndex..currentCharPosition];
        miningParams[JLField.LeadingSentencePart] = leadingSentencePart;
        string trailingSentencePart = currentText[(lookupResult.MatchedText.Length + currentCharPosition)..(sentenceStartIndex + sentence.Length)];
        miningParams[JLField.TrailingSentencePart] = trailingSentencePart;

        miningParams[JLField.Sentence] = useHtmlTags
            ? $"{leadingSentencePart}<b>{lookupResult.MatchedText}</b>{trailingSentencePart}"
            : sentence;

        if (lookupResult.Readings is not null)
        {
            string readings = string.Join('、', lookupResult.Readings);
            miningParams[JLField.Readings] = readings;

            miningParams[JLField.ReadingsWithOrthographyInfo] = lookupResult.ReadingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.Readings, lookupResult.ReadingsOrthographyInfoList)
                : readings;

            miningParams[JLField.PrimarySpellingAndReadings] = $"{lookupResult.PrimarySpelling}[{readings}]";

            string firstReading = lookupResult.Readings[0];
            miningParams[JLField.FirstReading] = firstReading;
            miningParams[JLField.PrimarySpellingAndFirstReading] = $"{lookupResult.PrimarySpelling}[{firstReading}]";
        }

        if (lookupResult.AlternativeSpellings is not null)
        {
            string alternativeSpellings = string.Join('、', lookupResult.AlternativeSpellings);
            miningParams[JLField.AlternativeSpellings] = alternativeSpellings;

            miningParams[JLField.AlternativeSpellingsWithOrthographyInfo] = lookupResult.AlternativeSpellingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.AlternativeSpellings, lookupResult.AlternativeSpellingsOrthographyInfoList)
                : alternativeSpellings;
        }

        if (lookupResult.Frequencies is not null)
        {
            List<LookupFrequencyResult> validFrequencies = lookupResult.Frequencies
                .Where(static f => f.Freq is > 0 and < int.MaxValue).ToList();

            if (validFrequencies.Count > 0)
            {
                miningParams[JLField.Frequencies] = LookupResultUtils.FrequenciesToText(lookupResult.Frequencies, true, lookupResult.Frequencies.Count is 1);
                miningParams[JLField.RawFrequencies] = string.Join(", ", validFrequencies.Select(static f => f.Freq));
                miningParams[JLField.FrequencyHarmonicMean] = CalculateHarmonicMean(validFrequencies).ToString(CultureInfo.InvariantCulture);

                int firstFrequency = lookupResult.Frequencies[0].Freq;
                if (firstFrequency is > 0 and < int.MaxValue)
                {
                    miningParams[JLField.PreferredFrequency] = firstFrequency.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        if (formattedDefinitions is not null)
        {
            formattedDefinitions = useHtmlTags
                ? formattedDefinitions.ReplaceLineEndings("<br/>")
                : formattedDefinitions;

            miningParams[JLField.Definitions] = formattedDefinitions;

            if (selectedDefinitions is null)
            {
                miningParams[JLField.SelectedDefinitions] = formattedDefinitions;
            }
        }

        if (selectedDefinitions is not null)
        {
            miningParams[JLField.SelectedDefinitions] = useHtmlTags
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
            miningParams[JLField.OnReadings] = string.Join('、', lookupResult.OnReadings);
        }

        if (lookupResult.KunReadings is not null)
        {
            miningParams[JLField.KunReadings] = string.Join('、', lookupResult.KunReadings);
        }

        if (lookupResult.NanoriReadings is not null)
        {
            miningParams[JLField.NanoriReadings] = string.Join('、', lookupResult.NanoriReadings);
        }

        if (lookupResult.RadicalNames is not null)
        {
            miningParams[JLField.RadicalNames] = string.Join('、', lookupResult.RadicalNames);
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
                    (string expression, byte position) = pitchAccents[i];
                    _ = numericPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{expression}: {position}");
                    _ = expressionsWithPitchAccentBuilder.Append(GetExpressionWithPitchAccent(expression, position));

                    if (i + 1 != pitchAccentCount)
                    {
                        _ = numericPitchAccentBuilder.Append(", ");
                        _ = expressionsWithPitchAccentBuilder.Append('、');
                    }
                }

                miningParams[JLField.NumericPitchAccents] = numericPitchAccentBuilder.ToString();
                miningParams[JLField.PitchAccents] = expressionsWithPitchAccentBuilder.ToString();

                (string firstExpression, byte firstPosition) = pitchAccents[0];
                if ((lookupResult.Readings is not null && firstExpression == lookupResult.Readings[0])
                    || (lookupResult.Readings is null && firstExpression == lookupResult.PrimarySpelling))
                {
                    miningParams[JLField.PitchAccentForFirstReading] = string.Create(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n{GetExpressionWithPitchAccent(firstExpression, firstPosition)}");
                    miningParams[JLField.NumericPitchAccentForFirstReading] = string.Create(CultureInfo.InvariantCulture, $"{firstExpression}: {firstPosition}");
                }
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

    private static StringBuilder GetExpressionWithPitchAccent(ReadOnlySpan<char> expression, byte position)
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

    public static async Task MineToFile(LookupResult lookupResult, string currentText, string? formattedDefinitions, string? selectedDefinitions, int currentCharPosition)
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

        Dictionary<JLField, string> miningParameters = GetMiningParameters(lookupResult, currentText, formattedDefinitions, selectedDefinitions, currentCharPosition, false);
        StringBuilder lineToMine = new();
        for (int i = 1; i < jlFields.Length; i++)
        {
            JLField jlField = jlFields[i];
            if (jlField is JLField.Audio or JLField.Image or JLField.PitchAccents or JLField.PitchAccentForFirstReading)
            {
                continue;
            }

            if (miningParameters.TryGetValue(jlField, out string? value))
            {
                string jlFieldContent = value.ReplaceLineEndings("\\n").Replace("\t", "  ", StringComparison.Ordinal).Trim();
                if (!string.IsNullOrEmpty(jlFieldContent))
                {
                    _ = lineToMine.Append(CultureInfo.InvariantCulture, $"{jlField.GetDescription()}: ")
                        .Append(jlFieldContent)
                        .Append(i < jlFields.Length - 1 ? '\t' : '\n');
                }
            }
        }

        await File.AppendAllTextAsync(filePath, lineToMine.ToString()).ConfigureAwait(false);

        Stats.IncrementStat(StatType.CardsMined);

        Utils.Frontend.Alert(AlertLevel.Success, $"Mined {lookupResult.PrimarySpelling}");
        Utils.Logger.Information("Mined {PrimarySpelling}", lookupResult.PrimarySpelling);
    }

    public static async ValueTask<bool[]?> CheckDuplicates(LookupResult[] lookupResults, string currentText, int currentCharPosition)
    {
        Dictionary<MineType, AnkiConfig>? ankiConfigDict = await AnkiConfig.ReadAnkiConfig().ConfigureAwait(false);
        if (ankiConfigDict is null)
        {
            return null;
        }

        List<Note> notes = [];
        List<int> positions = [];
        bool[] results = new bool[lookupResults.Length];

        for (int i = 0; i < lookupResults.Length; i++)
        {
            LookupResult lookupResult = lookupResults[i];
            DictType dictType = lookupResult.Dict.Type;

            AnkiConfig? ankiConfig;
            if (DictUtils.s_wordDictTypes.Contains(dictType))
            {
                ankiConfig = ankiConfigDict.GetValueOrDefault(MineType.Word);
            }
            else if (DictUtils.s_kanjiDictTypes.Contains(dictType))
            {
                ankiConfig = ankiConfigDict.GetValueOrDefault(MineType.Kanji);
            }
            else if (DictUtils.s_nameDictTypes.Contains(dictType))
            {
                ankiConfig = ankiConfigDict.GetValueOrDefault(MineType.Name);
            }
            else
            {
                ankiConfig = ankiConfigDict.GetValueOrDefault(MineType.Other);
            }

            if (ankiConfig is null)
            {
                continue;
            }

            Dictionary<string, JLField> userFields = ankiConfig.Fields;
            (string firstFieldName, JLField firstField) = userFields.First();
            string? firstFieldValue = GetMiningParameter(firstField, lookupResult, currentText, currentCharPosition);
            if (string.IsNullOrEmpty(firstFieldValue))
            {
                continue;
            }

            Dictionary<string, string> fields = new(1, StringComparer.Ordinal)
            {
                { firstFieldName, firstFieldValue }
            };

            Note note = new(ankiConfig.DeckName, ankiConfig.ModelName, fields, null, null, null, null, null);
            notes.Add(note);
            positions.Add(i);
        }

        if (notes.Count is 0)
        {
            return null;
        }

        List<bool>? canAddNoteList = await AnkiUtils.CanAddNotes(notes).ConfigureAwait(false);
        if (canAddNoteList is null)
        {
            return null;
        }

        for (int i = 0; i < canAddNoteList.Count; i++)
        {
            results[positions[i]] = !canAddNoteList[i];
        }

        return results;
    }

    public static async Task Mine(LookupResult lookupResult, string currentText, string? formattedDefinitions, string? selectedDefinitions, int currentCharPosition)
    {
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        if (!coreConfigManager.AnkiIntegration)
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
            ankiConfig = ankiConfigDict.GetValueOrDefault(MineType.Word);
        }
        else if (DictUtils.s_kanjiDictTypes.Contains(lookupResult.Dict.Type))
        {
            ankiConfig = ankiConfigDict.GetValueOrDefault(MineType.Kanji);
        }
        else if (DictUtils.s_nameDictTypes.Contains(lookupResult.Dict.Type))
        {
            ankiConfig = ankiConfigDict.GetValueOrDefault(MineType.Name);
        }
        else
        {
            ankiConfig = ankiConfigDict.GetValueOrDefault(MineType.Other);
        }

        if (ankiConfig is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return;
        }

        Dictionary<string, JLField> userFields = ankiConfig.Fields;
        Dictionary<JLField, string> miningParams = GetMiningParameters(lookupResult, currentText, formattedDefinitions, selectedDefinitions, currentCharPosition, true);
        Dictionary<string, string> fields = ConvertFields(userFields, miningParams);

        // Audio/Picture/Video shouldn't be set here
        // Otherwise AnkiConnect will place them under the "collection.media" folder even when it's a duplicate note
        Note note = new(ankiConfig.DeckName, ankiConfig.ModelName, fields, ankiConfig.Tags, null, null, null, null);
        bool? canAddNote = await AnkiUtils.CanAddNote(note).ConfigureAwait(false);
        if (canAddNote is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, $"Mining failed for {lookupResult.PrimarySpelling}");
            Utils.Logger.Error("Mining failed for {PrimarySpelling}", lookupResult.PrimarySpelling);
            return;
        }

        if (!coreConfigManager.AllowDuplicateCards && !canAddNote.Value)
        {
            Utils.Frontend.Alert(AlertLevel.Error, $"Cannot mine {lookupResult.PrimarySpelling} because it is a duplicate card");
            Utils.Logger.Information("Cannot mine {PrimarySpelling} because it is a duplicate card", lookupResult.PrimarySpelling);
            return;
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
            await Utils.Frontend.StopTextToSpeech().ConfigureAwait(false);
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
            ? await Utils.Frontend.GetImageFromClipboardAsByteArray().ConfigureAwait(false)
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
                "allowDuplicate", coreConfigManager.AllowDuplicateCards
            }
        };

        Response? response = await AnkiConnect.AddNoteToDeck(note).ConfigureAwait(false);
        if (response is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, $"Mining failed for {lookupResult.PrimarySpelling}");
            Utils.Logger.Error("Mining failed for {PrimarySpelling}", lookupResult.PrimarySpelling);
            return;
        }

        bool showNoAudioMessage = needsAudio && (audioData is null || Utils.GetMd5String(audioData) is Networking.Jpod101NoAudioMd5Hash);
        string message = $"Mined {lookupResult.PrimarySpelling}{(showNoAudioMessage ? " (No Audio)" : "")}{(!canAddNote.Value ? " (Duplicate)" : "")}";
        Utils.Frontend.Alert(AlertLevel.Warning, message);
        Utils.Logger.Information(message);

        if (coreConfigManager.ForceSyncAnki)
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
    private static Dictionary<string, string> ConvertFields(Dictionary<string, JLField> userFields, Dictionary<JLField, string> miningParams)
    {
        Dictionary<string, string> dict = new(StringComparer.Ordinal);
        foreach ((string key, JLField value) in userFields)
        {
            if (miningParams.TryGetValue(value, out string? fieldValue) && !string.IsNullOrEmpty(fieldValue))
            {
                dict.Add(key, fieldValue);
            }
        }

        return dict;
    }

    private static List<string> FindFields(JLField jlField, Dictionary<string, JLField> userFields)
    {
        return userFields.Keys.Where(key => userFields[key] == jlField).ToList();
    }
}
