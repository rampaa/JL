using System.Diagnostics;
using System.Globalization;
using System.Text;
using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Lookup;
using JL.Core.Mining.Anki;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Core.WordClass;

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

    private static string? GetMiningParameter(JLField field, LookupResult[] lookupResults, int currentLookupResultIndex, ReadOnlySpan<char> currentText, int currentCharPosition)
    {
        LookupResult lookupResult = lookupResults[currentLookupResultIndex];
        switch (field)
        {
            case JLField.LeadingSentencePart:
            {
                ReadOnlySpan<char> sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
                int searchStartIndex = currentCharPosition + lookupResult.MatchedText.Length - sentence.Length;
                if (searchStartIndex < 0 || searchStartIndex >= currentText.Length)
                {
                    searchStartIndex = 0;
                }

                int sentenceStartIndex = currentText.IndexOf(sentence, searchStartIndex);
                return currentText[sentenceStartIndex..currentCharPosition].ToString();
            }

            case JLField.TrailingSentencePart:
            {
                ReadOnlySpan<char> sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
                int searchStartIndex = currentCharPosition + lookupResult.MatchedText.Length - sentence.Length;
                if (searchStartIndex < 0 || searchStartIndex >= currentText.Length)
                {
                    searchStartIndex = 0;
                }

                int sentenceStartIndex = currentText.IndexOf(sentence, searchStartIndex);
                return currentText[(lookupResult.MatchedText.Length + currentCharPosition)..(sentenceStartIndex + sentence.Length)].ToString();
            }

            case JLField.Sentence:
            {
                ReadOnlySpan<char> sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
                int searchStartIndex = currentCharPosition + lookupResult.MatchedText.Length - sentence.Length;
                if (searchStartIndex < 0 || searchStartIndex >= currentText.Length)
                {
                    searchStartIndex = 0;
                }

                int sentenceStartIndex = currentText.IndexOf(sentence, searchStartIndex);
                ReadOnlySpan<char> leadingSentencePart = currentText[sentenceStartIndex..currentCharPosition];
                ReadOnlySpan<char> trailingSentencePart = currentText[(lookupResult.MatchedText.Length + currentCharPosition)..(sentenceStartIndex + sentence.Length)];
                return $"{leadingSentencePart}<b>{lookupResult.MatchedText}</b>{trailingSentencePart}";
            }

            case JLField.SourceText:
            {
                string leadingSourcePart = currentText[..currentCharPosition].ToString().ReplaceLineEndings("<br/>");
                string trailingSourcePart = currentText[(currentCharPosition + lookupResult.MatchedText.Length)..].ToString().ReplaceLineEndings("<br/>");
                return $"{leadingSourcePart}<b>{lookupResult.MatchedText}</b>{trailingSourcePart}".ReplaceLineEndings("<br/>");
            }

            case JLField.Readings:
                return lookupResult.Readings is not null
                    ? string.Join('、', lookupResult.Readings)
                    : null;

            case JLField.ReadingsWithOrthographyInfo:
                return lookupResult.JmdictLookupResult?.ReadingsOrthographyInfoList is not null && lookupResult.Readings is not null
                    ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.Readings, lookupResult.JmdictLookupResult.ReadingsOrthographyInfoList)
                    : lookupResult.Readings is not null
                        ? string.Join('、', lookupResult.Readings)
                        : null;

            case JLField.FirstReading:
                return lookupResult.Readings?[0];

            case JLField.PrimarySpellingAndReadings:
            {
                if (lookupResult.Readings is not null)
                {
                    StringBuilder stringBuilder = new();
                    for (int i = 0; i < lookupResult.Readings.Length; i++)
                    {
                        _ = stringBuilder.Append(JapaneseUtils.GetPrimarySpellingAndReadingMapping(lookupResult.PrimarySpelling, lookupResult.Readings[i]));
                        if (i + 1 != lookupResult.Readings.Length)
                        {
                            _ = stringBuilder.Append('、');
                        }
                    }

                    return stringBuilder.ToString();
                }

                return null;
            }

            case JLField.PrimarySpellingAndFirstReading:
                return lookupResult.Readings is not null
                    ? JapaneseUtils.GetPrimarySpellingAndReadingMapping(lookupResult.PrimarySpelling, lookupResult.Readings[0])
                    : null;

            case JLField.PrimarySpellingWithOrthographyInfo:
                return lookupResult.JmdictLookupResult?.PrimarySpellingOrthographyInfoList is not null
                    ? $"{lookupResult.PrimarySpelling} ({string.Join(", ", lookupResult.JmdictLookupResult.PrimarySpellingOrthographyInfoList)})"
                    : lookupResult.PrimarySpelling;

            case JLField.AlternativeSpellings:
                return lookupResult.AlternativeSpellings is not null
                    ? string.Join('、', lookupResult.AlternativeSpellings)
                    : null;

            case JLField.AlternativeSpellingsWithOrthographyInfo:
                return lookupResult.AlternativeSpellings is not null
                    ? lookupResult.JmdictLookupResult?.AlternativeSpellingsOrthographyInfoList is not null
                        ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.AlternativeSpellings, lookupResult.JmdictLookupResult.AlternativeSpellingsOrthographyInfoList)
                        : string.Join('、', lookupResult.AlternativeSpellings)
                    : null;

            case JLField.MatchedText:
                return lookupResult.MatchedText;

            case JLField.SelectedSpelling:
            case JLField.PrimarySpelling:
                return lookupResult.PrimarySpelling;

            case JLField.DeconjugatedMatchedText:
                return lookupResult.DeconjugatedMatchedText ?? lookupResult.MatchedText;

            case JLField.KanjiStats:
                return lookupResult.KanjiLookupResult?.KanjiStats;

            case JLField.OnReadings:
                return lookupResult.KanjiLookupResult?.OnReadings is not null
                    ? string.Join('、', lookupResult.KanjiLookupResult.OnReadings)
                    : null;

            case JLField.KunReadings:
                return lookupResult.KanjiLookupResult?.KunReadings is not null
                    ? string.Join('、', lookupResult.KanjiLookupResult.KunReadings)
                    : null;

            case JLField.NanoriReadings:
                return lookupResult.KanjiLookupResult?.NanoriReadings is not null
                    ? string.Join('、', lookupResult.KanjiLookupResult.NanoriReadings)
                    : null;

            case JLField.EdictId:
                return lookupResult.EntryId > 0
                    ? lookupResult.EntryId.ToString(CultureInfo.InvariantCulture)
                    : null;

            case JLField.DeconjugationProcess:
                return lookupResult.DeconjugationProcess;

            case JLField.KanjiComposition:
                return lookupResult.KanjiLookupResult?.KanjiComposition is not null ? string.Join('、', lookupResult.KanjiLookupResult.KanjiComposition) : null;

            case JLField.StrokeCount:
                return lookupResult.KanjiLookupResult?.StrokeCount > 0
                    ? lookupResult.KanjiLookupResult.StrokeCount.ToString(CultureInfo.InvariantCulture)
                    : null;

            case JLField.KanjiGrade:
                return lookupResult.KanjiLookupResult is not null && lookupResult.KanjiLookupResult.KanjiGrade != byte.MaxValue
                    ? lookupResult.KanjiLookupResult.KanjiGrade.ToString(CultureInfo.InvariantCulture)
                    : null;

            case JLField.RadicalNames:
                return lookupResult.KanjiLookupResult?.RadicalNames is not null
                    ? string.Join('、', lookupResult.KanjiLookupResult.RadicalNames)
                    : null;

            case JLField.SelectedDefinitions:
            case JLField.Definitions:
                return lookupResult.FormattedDefinitions?.ReplaceLineEndings("<br/>");

            case JLField.DefinitionsFromMultipleDictionaries:
                return GetDefinitionsFromAllDictionaries(lookupResults, currentLookupResultIndex, lookupResult.PrimarySpelling, lookupResult.FormattedDefinitions, true);

            case JLField.LeadingSourceTextPart:
                return currentText[..currentCharPosition].ToString().ReplaceLineEndings("<br/>");

            case JLField.TrailingSourceTextPart:
                return currentText[(currentCharPosition + lookupResult.MatchedText.Length)..].ToString().ReplaceLineEndings("<br/>");

            case JLField.DictionaryName:
                return lookupResult.Dict.Name;

            case JLField.Frequencies:
                return lookupResult.Frequencies is not null
                    ? LookupResultUtils.FrequenciesToText(lookupResult.Frequencies.AsReadOnlySpan(), true, lookupResult.Frequencies.Count is 1)
                    : null;

            case JLField.RawFrequencies:
            {
                if (lookupResult.Frequencies is null)
                {
                    return null;
                }

                List<int> validFrequencies = new(lookupResult.Frequencies.Count);
                foreach (LookupFrequencyResult lookupFrequencyResult in lookupResult.Frequencies.AsReadOnlySpan())
                {
                    if (lookupFrequencyResult.Freq is > 0 and < int.MaxValue)
                    {
                        validFrequencies.Add(lookupFrequencyResult.Freq);
                    }
                }
                return string.Join(", ", validFrequencies);
            }

            case JLField.PreferredFrequency:
            {
                if (lookupResult.Frequencies is not null)
                {
                    int firstFrequency = lookupResult.Frequencies[0].Freq;
                    if (firstFrequency is > 0 and < int.MaxValue)
                    {
                        return firstFrequency.ToString(CultureInfo.InvariantCulture);
                    }
                }

                return null;
            }

            case JLField.FrequencyHarmonicMean:
            {
                if (lookupResult.Frequencies is null)
                {
                    return null;
                }

                ReadOnlySpan<LookupFrequencyResult> allFrequencies = lookupResult.Frequencies.AsReadOnlySpan();
                List<LookupFrequencyResult> filteredFrequencies = new(allFrequencies.Length);
                foreach (ref readonly LookupFrequencyResult frequency in allFrequencies)
                {
                    if (frequency.Freq is > 0 and < int.MaxValue)
                    {
                        filteredFrequencies.Add(frequency);
                    }
                }

                return CalculateHarmonicMean(filteredFrequencies.AsReadOnlySpan()).ToString(CultureInfo.InvariantCulture);
            }

            case JLField.WordClasses:
            {
                return GetWordClasses(lookupResult);
            }

            case JLField.PitchAccents:
            {
                if (lookupResult.PitchPositions is null)
                {
                    return null;
                }

                string[] expressions = lookupResult.Readings ?? [lookupResult.PrimarySpelling];
                StringBuilder expressionsWithPitchAccentBuilder = new();
                _ = expressionsWithPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n");

                for (int i = 0; i < expressions.Length; i++)
                {
                    byte pitchPosition = lookupResult.PitchPositions[i];
                    if (pitchPosition is not byte.MaxValue)
                    {
                        _ = expressionsWithPitchAccentBuilder.Append(GetExpressionWithPitchAccent(expressions[i], pitchPosition)).Append('、');
                    }
                }

                return expressionsWithPitchAccentBuilder.ToString(0, expressionsWithPitchAccentBuilder.Length - 1);
            }

            case JLField.NumericPitchAccents:
            {
                if (lookupResult.PitchPositions is null)
                {
                    return null;
                }

                string[] expressions = lookupResult.Readings ?? [lookupResult.PrimarySpelling];
                StringBuilder numericPitchAccentBuilder = new();
                for (int i = 0; i < expressions.Length; i++)
                {
                    byte pitchPosition = lookupResult.PitchPositions[i];
                    if (pitchPosition is not byte.MaxValue)
                    {
                        _ = numericPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{expressions[i]}: {pitchPosition}, ");
                    }
                }

                return numericPitchAccentBuilder.ToString(0, numericPitchAccentBuilder.Length - 2);
            }

            case JLField.PitchAccentForFirstReading:
            {
                if (lookupResult.PitchPositions is not null)
                {
                    byte firstPitchPosition = lookupResult.PitchPositions[0];
                    if (firstPitchPosition is not byte.MaxValue)
                    {
                        string expression = lookupResult.Readings is not null
                            ? lookupResult.Readings[0]
                            : lookupResult.PrimarySpelling;

                        return string.Create(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n{GetExpressionWithPitchAccent(expression, firstPitchPosition)}");
                    }
                }

                return null;
            }

            case JLField.NumericPitchAccentForFirstReading:
            {
                if (lookupResult.PitchPositions is not null)
                {
                    byte firstPitchPosition = lookupResult.PitchPositions[0];
                    if (firstPitchPosition is not byte.MaxValue)
                    {
                        string expression = lookupResult.Readings is not null
                            ? lookupResult.Readings[0]
                            : lookupResult.PrimarySpelling;

                        return string.Create(CultureInfo.InvariantCulture, $"{expression}: {firstPitchPosition}");
                    }
                }

                return null;
            }

            case JLField.PitchAccentCategories:
            {
                if (lookupResult.PitchPositions is null)
                {
                    return null;
                }

                string[] expressions = lookupResult.Readings ?? [lookupResult.PrimarySpelling];
                StringBuilder pitchAccentCategoriesBuilder = new();
                for (int i = 0; i < expressions.Length; i++)
                {
                    byte pitchPosition = lookupResult.PitchPositions[i];
                    if (pitchPosition is not byte.MaxValue)
                    {
                        string expression = expressions[i];
                        _ = pitchAccentCategoriesBuilder.Append(CultureInfo.InvariantCulture, $"{expression}: {GetPitchAccentCategory(expression, pitchPosition)}, ");
                    }
                }

                return pitchAccentCategoriesBuilder.ToString(0, pitchAccentCategoriesBuilder.Length - 2);
            }

            case JLField.PitchAccentCategoryForFirstReading:
            {
                if (lookupResult.PitchPositions is not null)
                {
                    byte firstPitchPosition = lookupResult.PitchPositions[0];
                    if (firstPitchPosition is not byte.MaxValue)
                    {
                        string firstExpression = lookupResult.Readings is not null
                                ? lookupResult.Readings[0]
                                : lookupResult.PrimarySpelling;

                        return $"{firstExpression}: {GetPitchAccentCategory(firstExpression, firstPitchPosition)}";
                    }
                }

                return null;
            }

            case JLField.Nothing:
            case JLField.Audio:
            case JLField.Image:
            case JLField.LocalTime:
                return null;

            default:
            {
                Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(JLField), nameof(MiningUtils), nameof(GetMiningParameter), field);
                Utils.Frontend.Alert(AlertLevel.Error, $"Invalid JLField: {field}");
                return null;
            }
        }
    }

    private static Dictionary<JLField, string> GetMiningParameters(LookupResult[] lookupResults, int currentLookupResultIndex, ReadOnlySpan<char> currentText, string? formattedDefinitions, string? selectedDefinitions, int currentCharPosition, string selectedSpelling, bool useHtmlTags, OrderedDictionary<string, JLField>? userFields)
    {
        LookupResult lookupResult = lookupResults[currentLookupResultIndex];
        Dictionary<JLField, string> miningParams = new()
        {
            [JLField.LocalTime] = DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
            [JLField.DictionaryName] = lookupResult.Dict.Name,
            [JLField.MatchedText] = lookupResult.MatchedText,
            [JLField.DeconjugatedMatchedText] = lookupResult.DeconjugatedMatchedText ?? lookupResult.MatchedText,
            [JLField.PrimarySpelling] = lookupResult.PrimarySpelling,
            [JLField.PrimarySpellingWithOrthographyInfo] = lookupResult.JmdictLookupResult?.PrimarySpellingOrthographyInfoList is not null
                ? $"{lookupResult.PrimarySpelling} ({string.Join(", ", lookupResult.JmdictLookupResult.PrimarySpellingOrthographyInfoList)})"
                : lookupResult.PrimarySpelling,
            [JLField.SelectedSpelling] = selectedSpelling
        };

        string leadingSourcePart = currentText[..currentCharPosition].ToString();
        string trailingSourcePart = currentText[(currentCharPosition + lookupResult.MatchedText.Length)..].ToString();
        if (useHtmlTags)
        {
            leadingSourcePart = leadingSourcePart.ReplaceLineEndings("<br/>");
            trailingSourcePart = trailingSourcePart.ReplaceLineEndings("<br/>");
        }

        miningParams[JLField.LeadingSourceTextPart] = leadingSourcePart;
        miningParams[JLField.TrailingSourceTextPart] = trailingSourcePart;

        miningParams[JLField.SourceText] = useHtmlTags
                ? $"{leadingSourcePart}<b>{lookupResult.MatchedText}</b>{trailingSourcePart}"
                : currentText.ToString();

        string sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
        int searchStartIndex = currentCharPosition + lookupResult.MatchedText.Length - sentence.Length;
        if (searchStartIndex < 0 || searchStartIndex >= currentText.Length)
        {
            searchStartIndex = 0;
        }

        int sentenceStartIndex = currentText.IndexOf(sentence, searchStartIndex);
        ReadOnlySpan<char> leadingSentencePart = currentText[sentenceStartIndex..currentCharPosition];
        miningParams[JLField.LeadingSentencePart] = leadingSentencePart.ToString();
        ReadOnlySpan<char> trailingSentencePart = currentText[(lookupResult.MatchedText.Length + currentCharPosition)..(sentenceStartIndex + sentence.Length)];
        miningParams[JLField.TrailingSentencePart] = trailingSentencePart.ToString();

        miningParams[JLField.Sentence] = useHtmlTags
            ? $"{leadingSentencePart}<b>{lookupResult.MatchedText}</b>{trailingSentencePart}"
            : sentence;

        string? wordClasses = GetWordClasses(lookupResult);
        if (wordClasses is not null)
        {
            miningParams[JLField.WordClasses] = wordClasses;
        }

        int selectedSpellingIndex = lookupResult.Readings.AsReadOnlySpan().IndexOf(selectedSpelling);
        if (selectedSpellingIndex is -1)
        {
            selectedSpellingIndex = 0;
        }

        if (lookupResult.Readings is not null)
        {
            string readings = string.Join('、', lookupResult.Readings);
            miningParams[JLField.Readings] = readings;

            miningParams[JLField.ReadingsWithOrthographyInfo] = lookupResult.JmdictLookupResult?.ReadingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.Readings, lookupResult.JmdictLookupResult.ReadingsOrthographyInfoList)
                : readings;

            string firstReading = lookupResult.Readings[selectedSpellingIndex];
            miningParams[JLField.FirstReading] = firstReading;
            miningParams[JLField.PrimarySpellingAndFirstReading] = JapaneseUtils.GetPrimarySpellingAndReadingMapping(lookupResult.PrimarySpelling, firstReading);

            StringBuilder primarySpellingAndReadingStringBuilder = new();
            for (int i = 0; i < lookupResult.Readings.Length; i++)
            {
                _ = primarySpellingAndReadingStringBuilder.Append(JapaneseUtils.GetPrimarySpellingAndReadingMapping(lookupResult.PrimarySpelling, lookupResult.Readings[i]));
                if (i + 1 != lookupResult.Readings.Length)
                {
                    _ = primarySpellingAndReadingStringBuilder.Append('、');
                }
            }
            miningParams[JLField.PrimarySpellingAndReadings] = primarySpellingAndReadingStringBuilder.ToString();
        }

        if (lookupResult.AlternativeSpellings is not null)
        {
            string alternativeSpellings = string.Join('、', lookupResult.AlternativeSpellings);
            miningParams[JLField.AlternativeSpellings] = alternativeSpellings;

            miningParams[JLField.AlternativeSpellingsWithOrthographyInfo] = lookupResult.JmdictLookupResult?.AlternativeSpellingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.AlternativeSpellings, lookupResult.JmdictLookupResult.AlternativeSpellingsOrthographyInfoList)
                : alternativeSpellings;
        }

        if (lookupResult.Frequencies is not null)
        {
            List<LookupFrequencyResult> validFrequencies = new(lookupResult.Frequencies.Count);
            List<int> validFrequencyValues = new(lookupResult.Frequencies.Count);
            foreach (LookupFrequencyResult lookupFrequencyResult in lookupResult.Frequencies.AsReadOnlySpan())
            {
                if (lookupFrequencyResult.Freq is > 0 and < int.MaxValue)
                {
                    validFrequencies.Add(lookupFrequencyResult);
                    validFrequencyValues.Add(lookupFrequencyResult.Freq);
                }
            }

            if (validFrequencies.Count > 0)
            {
                miningParams[JLField.Frequencies] = LookupResultUtils.FrequenciesToText(lookupResult.Frequencies.AsReadOnlySpan(), true, lookupResult.Frequencies.Count is 1);
                miningParams[JLField.RawFrequencies] = string.Join(", ", validFrequencyValues);
                miningParams[JLField.FrequencyHarmonicMean] = CalculateHarmonicMean(validFrequencies.AsReadOnlySpan()).ToString(CultureInfo.InvariantCulture);

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

        if (lookupResult.EntryId > 0)
        {
            miningParams[JLField.EdictId] = lookupResult.EntryId.ToString(CultureInfo.InvariantCulture);
        }

        if (lookupResult.DeconjugationProcess is not null)
        {
            miningParams[JLField.DeconjugationProcess] = lookupResult.DeconjugationProcess;
        }

        KanjiLookupResult? kanjiLookupResult = lookupResult.KanjiLookupResult;
        if (kanjiLookupResult is not null)
        {
            if (kanjiLookupResult.KanjiComposition is not null)
            {
                miningParams[JLField.KanjiComposition] = string.Join('、', kanjiLookupResult.KanjiComposition);
            }

            if (kanjiLookupResult.KanjiStats is not null)
            {
                miningParams[JLField.KanjiStats] = kanjiLookupResult.KanjiStats;
            }

            if (kanjiLookupResult.StrokeCount > 0)
            {
                miningParams[JLField.StrokeCount] = kanjiLookupResult.StrokeCount.ToString(CultureInfo.InvariantCulture);
            }

            if (kanjiLookupResult.KanjiGrade is not byte.MaxValue)
            {
                miningParams[JLField.KanjiGrade] = kanjiLookupResult.KanjiGrade.ToString(CultureInfo.InvariantCulture);
            }

            if (kanjiLookupResult.OnReadings is not null)
            {
                miningParams[JLField.OnReadings] = string.Join('、', kanjiLookupResult.OnReadings);
            }

            if (kanjiLookupResult.KunReadings is not null)
            {
                miningParams[JLField.KunReadings] = string.Join('、', kanjiLookupResult.KunReadings);
            }

            if (kanjiLookupResult.NanoriReadings is not null)
            {
                miningParams[JLField.NanoriReadings] = string.Join('、', kanjiLookupResult.NanoriReadings);
            }

            if (kanjiLookupResult.RadicalNames is not null)
            {
                miningParams[JLField.RadicalNames] = string.Join('、', kanjiLookupResult.RadicalNames);
            }
        }

        if (lookupResult.PitchPositions is not null)
        {
            string[] expressions = lookupResult.Readings ?? [lookupResult.PrimarySpelling];
            StringBuilder expressionsWithPitchAccentBuilder = new();
            _ = expressionsWithPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n");

            StringBuilder numericPitchAccentBuilder = new();
            StringBuilder pitchAccentCategoriesBuilder = new();
            for (int i = 0; i < expressions.Length; i++)
            {
                byte pitchPosition = lookupResult.PitchPositions[i];
                if (pitchPosition is not byte.MaxValue)
                {
                    string expression = expressions[i];
                    _ = numericPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{expression}: {pitchPosition}, ");

                    _ = expressionsWithPitchAccentBuilder.Append(GetExpressionWithPitchAccent(expression, pitchPosition)).Append('、');

                    _ = pitchAccentCategoriesBuilder.Append(CultureInfo.InvariantCulture, $"{expression}: {GetPitchAccentCategory(expression, pitchPosition)}, ");
                }
            }

            miningParams[JLField.NumericPitchAccents] = numericPitchAccentBuilder.ToString(0, numericPitchAccentBuilder.Length - 2);
            miningParams[JLField.PitchAccents] = expressionsWithPitchAccentBuilder.ToString(0, expressionsWithPitchAccentBuilder.Length - 1);
            miningParams[JLField.PitchAccentCategories] = pitchAccentCategoriesBuilder.ToString(0, pitchAccentCategoriesBuilder.Length - 2);

            byte firstPitchPosition = lookupResult.PitchPositions[selectedSpellingIndex];
            if (firstPitchPosition is not byte.MaxValue)
            {
                string firstExpression = expressions[selectedSpellingIndex];
                miningParams[JLField.PitchAccentForFirstReading] = string.Create(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n{GetExpressionWithPitchAccent(firstExpression, firstPitchPosition)}");
                miningParams[JLField.NumericPitchAccentForFirstReading] = string.Create(CultureInfo.InvariantCulture, $"{firstExpression}: {firstPitchPosition}");
                miningParams[JLField.PitchAccentCategoryForFirstReading] = $"{firstExpression}: {GetPitchAccentCategory(firstExpression, firstPitchPosition)}";
            }
        }

        if (userFields is null || userFields.ContainsValue(JLField.DefinitionsFromMultipleDictionaries))
        {
            string? definitionsFromAllDictionaries = GetDefinitionsFromAllDictionaries(lookupResults, currentLookupResultIndex, selectedSpelling, formattedDefinitions, useHtmlTags);
            if (definitionsFromAllDictionaries is not null)
            {
                miningParams[JLField.DefinitionsFromMultipleDictionaries] = definitionsFromAllDictionaries;
            }
        }

        return miningParams;
    }

    private static string? GetDefinitionsFromAllDictionaries(LookupResult[] lookupResults, int currentLookupResultsIndex, string selectedSpelling, string? formattedDefinitions, bool useHtmlTags)
    {
        LookupResult selectedLookupResult = lookupResults[currentLookupResultsIndex];
        bool readingIsSelected = selectedLookupResult.PrimarySpelling != selectedSpelling;
        DictType[] dictTypes;
        if (DictUtils.s_wordDictTypes.Contains(selectedLookupResult.Dict.Type))
        {
            dictTypes = DictUtils.s_wordDictTypes;
        }
        else if (DictUtils.KanjiDictTypes.Contains(selectedLookupResult.Dict.Type))
        {
            dictTypes = DictUtils.KanjiDictTypes;
        }
        else if (DictUtils.s_nameDictTypes.Contains(selectedLookupResult.Dict.Type))
        {
            dictTypes = DictUtils.s_nameDictTypes;
        }
        else
        {
            dictTypes = DictUtils.s_otherDictTypes;
        }

        OrderedDictionary<string, List<LookupResult>> validLookupResults = [];
        validLookupResults[selectedLookupResult.Dict.Name] = [selectedLookupResult];

        for (int i = 0; i < lookupResults.Length; i++)
        {
            if (currentLookupResultsIndex == i)
            {
                continue;
            }

            LookupResult otherLookupResult = lookupResults[i];
            if (otherLookupResult.MatchedText.Length < selectedLookupResult.MatchedText.Length)
            {
                break;
            }

            if (!dictTypes.Contains(otherLookupResult.Dict.Type))
            {
                continue;
            }

            if (selectedLookupResult.PrimarySpelling == otherLookupResult.PrimarySpelling
                && otherLookupResult.FormattedDefinitions is not null
                && ((readingIsSelected && otherLookupResult.Readings is not null && otherLookupResult.Readings.AsReadOnlySpan().Contains(selectedSpelling))
                    || (!readingIsSelected
                        && ((selectedLookupResult.Readings is null && otherLookupResult.Readings is null)
                            || (selectedLookupResult.Readings is not null && otherLookupResult.Readings is not null && selectedLookupResult.Readings.Any(otherLookupResult.Readings.Contains))))))
            {
                if (validLookupResults.TryGetValue(otherLookupResult.Dict.Name, out List<LookupResult>? results))
                {
                    results.Add(otherLookupResult);
                }
                else
                {
                    validLookupResults[otherLookupResult.Dict.Name] = [otherLookupResult];
                }
            }
        }

        return useHtmlTags
            ? GetDefinitionsFromAllDictionariesWithHtmlTags(validLookupResults, selectedLookupResult.Dict.Name, formattedDefinitions)
            : GetDefinitionsFromAllDictionariesWithoutHtmlTags(validLookupResults, selectedLookupResult.Dict.Name, formattedDefinitions);
    }

    private static string? GetDefinitionsFromAllDictionariesWithHtmlTags(OrderedDictionary<string, List<LookupResult>> validLookupResults, string selectedRecordDictName, string? selectedRecordDefinitions)
    {
        ReadOnlySpan<LookupResult> firstLookupResults = validLookupResults.GetAt(0).Value.AsReadOnlySpan();
        if (validLookupResults.Count is 1)
        {
            if (firstLookupResults.Length is 1)
            {
                return selectedRecordDefinitions;
            }

            StringBuilder singleDictStringBuilder = new();
            int count = 1;

            if (selectedRecordDefinitions is not null)
            {
                _ = singleDictStringBuilder.Append(CultureInfo.InvariantCulture, $"<dt>1.</dt> <dd>{selectedRecordDefinitions}</dd>");
                ++count;
            }

            for (int i = 1; i < firstLookupResults.Length; i++)
            {
                string? formattedDefinitions = firstLookupResults[i].FormattedDefinitions;
                Debug.Assert(formattedDefinitions is not null);
                _ = singleDictStringBuilder.Append(CultureInfo.InvariantCulture, $" <dt>{count}.</dt> <dd>{formattedDefinitions.ReplaceLineEndings("<br/>")}</dd>");

                ++count;
            }

            return singleDictStringBuilder.ToString();
        }

        StringBuilder stringBuilder = new();
        if (firstLookupResults.Length is 1)
        {
            if (selectedRecordDefinitions is not null)
            {
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"<details open> <summary>{selectedRecordDictName}</summary> {selectedRecordDefinitions} </details>");
            }
        }
        else
        {
            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"<details open> <summary>{selectedRecordDictName}</summary>");

            int count = 1;
            if (selectedRecordDefinitions is not null)
            {
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $" <dt>1.</dt> <dd>{selectedRecordDefinitions}</dd>");
                ++count;
            }

            for (int i = 1; i < firstLookupResults.Length; i++)
            {
                string? formattedDefinitions = firstLookupResults[i].FormattedDefinitions;
                Debug.Assert(formattedDefinitions is not null);
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $" <dt>{count}.</dt> <dd>{formattedDefinitions.ReplaceLineEndings("<br/>")}</dd>");
                ++count;
            }

            _ = stringBuilder.Append(" </details>");
        }

        int validLookupResultsCount = validLookupResults.Count;
        for (int i = 1; i < validLookupResultsCount; i++)
        {
            (string dictName, List<LookupResult> otherLookupResults) = validLookupResults.GetAt(i);
            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $" <details> <summary>{dictName}</summary> ");
            if (otherLookupResults.Count is 1)
            {
                string? formattedDefinitions = firstLookupResults[0].FormattedDefinitions;
                Debug.Assert(formattedDefinitions is not null);
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{formattedDefinitions.ReplaceLineEndings("<br/>")} </details>");
            }
            else
            {
                ReadOnlySpan<LookupResult> otherLookupResultsSpan = otherLookupResults.AsReadOnlySpan();
                for (int j = 0; j < otherLookupResultsSpan.Length; j++)
                {
                    string? formattedDefinitions = firstLookupResults[j].FormattedDefinitions;
                    Debug.Assert(formattedDefinitions is not null);
                    _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"<dt>{j + 1}.</dt> <dd>{formattedDefinitions.ReplaceLineEndings("<br/>")}</dd>");
                }

                _ = stringBuilder.Append(" </details>");
            }
        }

        return stringBuilder.ToString();
    }

    private static string? GetDefinitionsFromAllDictionariesWithoutHtmlTags(OrderedDictionary<string, List<LookupResult>> validLookupResults, string selectedRecordDictName, string? selectedRecordDefinitions)
    {
        ReadOnlySpan<LookupResult> firstLookupResults = validLookupResults.GetAt(0).Value.AsReadOnlySpan();
        if (validLookupResults.Count is 1)
        {
            if (firstLookupResults.Length is 1)
            {
                return selectedRecordDefinitions;
            }

            StringBuilder singleDictStringBuilder = new();
            int count = 1;

            if (selectedRecordDefinitions is not null)
            {
                _ = singleDictStringBuilder.Append(CultureInfo.InvariantCulture, $"1.\n\t{selectedRecordDefinitions.ReplaceLineEndings("\n\t")}\n");
                ++count;
            }

            for (int i = 1; i < firstLookupResults.Length; i++)
            {
                string? formattedDefinitions = firstLookupResults[i].FormattedDefinitions;
                Debug.Assert(formattedDefinitions is not null);
                _ = singleDictStringBuilder.Append(CultureInfo.InvariantCulture, $"{count}.\n\t{formattedDefinitions.ReplaceLineEndings("\n\t")}");
                ++count;
            }

            return singleDictStringBuilder.ToString();
        }

        StringBuilder stringBuilder = new();

        if (firstLookupResults.Length is 1)
        {
            if (selectedRecordDefinitions is not null)
            {
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{selectedRecordDictName}:\n{selectedRecordDefinitions.ReplaceLineEndings("\n\t")}\n");
            }
        }
        else
        {
            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{selectedRecordDictName}:\n");

            int count = 1;
            if (selectedRecordDefinitions is not null)
            {
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"1.\n\t{selectedRecordDefinitions.ReplaceLineEndings("\n\t")}\n");
                ++count;
            }

            for (int i = 1; i < firstLookupResults.Length; i++)
            {
                string? formattedDefinitions = firstLookupResults[i].FormattedDefinitions;
                Debug.Assert(formattedDefinitions is not null);
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{count}.\n\t{formattedDefinitions.ReplaceLineEndings("\n\t")}\n");
                ++count;
            }
        }

        int validLookupResultsCount = validLookupResults.Count;
        for (int i = 1; i < validLookupResultsCount; i++)
        {
            (string dictName, List<LookupResult> otherLookupResults) = validLookupResults.GetAt(i);
            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{dictName}:\n");
            if (otherLookupResults.Count is 1)
            {
                string? formattedDefinitions = otherLookupResults[0].FormattedDefinitions;
                Debug.Assert(formattedDefinitions is not null);
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{formattedDefinitions.ReplaceLineEndings("\n\t")}");
            }
            else
            {
                int count = 1;
                ReadOnlySpan<LookupResult> otherLookupResultsSpan = otherLookupResults.AsReadOnlySpan();
                for (int j = 0; j < otherLookupResultsSpan.Length; j++)
                {
                    string? formattedDefinitions = firstLookupResults[j].FormattedDefinitions;
                    Debug.Assert(formattedDefinitions is not null);
                    _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{count}.\n\t{formattedDefinitions.ReplaceLineEndings("\n\t")}");
                    if (j + 1 < otherLookupResultsSpan.Length)
                    {
                        _ = stringBuilder.Append('\n');
                    }

                    ++count;
                }
            }

            if (i + 1 < validLookupResults.Count)
            {
                _ = stringBuilder.Append("\n\n");
            }
        }

        return stringBuilder.ToString();
    }

    private static int CalculateHarmonicMean(ReadOnlySpan<LookupFrequencyResult> lookupFrequencyResults)
    {
        double sumOfReciprocalOfFreqs = 0;
        foreach (ref readonly LookupFrequencyResult lookupFrequencyResult in lookupFrequencyResults)
        {
            int freq = lookupFrequencyResult.HigherValueMeansHigherFrequency
                ? FreqUtils.FreqDicts[lookupFrequencyResult.Name].MaxValue - lookupFrequencyResult.Freq + 1
                : lookupFrequencyResult.Freq;

            sumOfReciprocalOfFreqs += 1d / freq;
        }

        return double.ConvertToIntegerNative<int>(Math.Round(lookupFrequencyResults.Length / sumOfReciprocalOfFreqs));
    }

    private static string GetPitchAccentCategory(string expression, byte pitchPosition)
    {
        return pitchPosition is 0
            ? "Heiban"
            : pitchPosition is 1
                ? "Atamadaka"
                : JapaneseUtils.GetCombinedFormLength(expression) > pitchPosition
                    ? "Nakadaka"
                    : "Odaka";
    }

    private static string? GetWordClasses(LookupResult lookupResult)
    {
        if (lookupResult.WordClasses is not null || lookupResult.JmdictLookupResult?.WordClassesForSenses is not null)
        {
            if (lookupResult.WordClasses is not null && lookupResult.JmdictLookupResult?.WordClassesForSenses is null)
            {
                return string.Join(", ", lookupResult.WordClasses);
            }

            StringBuilder sb = new();
            if (lookupResult.WordClasses is not null)
            {
                _ = sb.AppendJoin(", ", lookupResult.WordClasses);
            }

            Debug.Assert(lookupResult.JmdictLookupResult is not null);
            Debug.Assert(lookupResult.JmdictLookupResult.WordClassesForSenses is not null);
            string[]?[] wordClassesForSenses = lookupResult.JmdictLookupResult.WordClassesForSenses;
            foreach (string[]? wordClassesForSense in wordClassesForSenses)
            {
                if (wordClassesForSense is not null)
                {
                    if (sb.Length > 0)
                    {
                        _ = sb.Append(", ");
                    }

                    _ = sb.AppendJoin(", ", wordClassesForSense);
                }
            }

            return sb.ToString();
        }

        if (lookupResult.Dict.Type is DictType.NonspecificWordNazeka
            or DictType.NonspecificNazeka
            or DictType.NonspecificWordYomichan
            or DictType.NonspecificYomichan)
        {
            string[]? wordClasses = GetWordClassesFromWordClassDictionary(lookupResult.PrimarySpelling, lookupResult.Readings?[0]);
            if (wordClasses is not null)
            {
                return string.Join(", ", wordClasses);
            }
        }

        return null;
    }

    private static string[]? GetWordClassesFromWordClassDictionary(string primarySpelling, string? reading)
    {
        if (DictUtils.WordClassDictionary.TryGetValue(primarySpelling, out IList<JmdictWordClass>? jmdictWcResults))
        {
            JmdictWordClass? foundRecord = null;
            int jmdictWcResultsCount = jmdictWcResults.Count;
            for (int i = 0; i < jmdictWcResultsCount; i++)
            {
                JmdictWordClass result = jmdictWcResults[i];
                if (primarySpelling == result.Spelling
                    && ((reading is null && result.Readings is null)
                        || (reading is not null && result.Readings is not null && result.Readings.AsReadOnlySpan().Contains(reading))))
                {
                    // If there is more than one valid result, we can't be sure which one applies to the current record, so we return null.
                    // See the entries for 駆ける and 振りかえる in JMdict as examples, where the spelling and reading are the same, but the word classes differ.
                    if (foundRecord is not null)
                    {
                        return null;
                    }

                    foundRecord = result;
                }
            }

            return foundRecord?.WordClasses;
        }

        return null;
    }

    private static StringBuilder GetExpressionWithPitchAccent(ReadOnlySpan<char> expression, byte position)
    {
        bool lowPitch = false;
        StringBuilder expressionWithPitchAccentStringBuilder = new();
        ReadOnlySpan<string> combinedFormList = JapaneseUtils.CreateCombinedForm(expression);
        for (int i = 0; i < combinedFormList.Length; i++)
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

    public static async Task MineToFile(LookupResult[] lookupResults, int currentLookupResultIndex, string currentText, string? formattedDefinitions, string? selectedDefinitions, int currentCharPosition, string selectedSpelling)
    {
        string filePath;
        JLField[] jlFields;
        LookupResult lookupResult = lookupResults[currentLookupResultIndex];
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
        else if (DictUtils.KanjiDictTypes.Contains(lookupResult.Dict.Type))
        {
            filePath = Path.Join(Utils.ResourcesPath, "mined_kanjis.txt");
            jlFields = JLFieldUtils.JLFieldsForKanjiDicts;
        }
        else
        {
            filePath = Path.Join(Utils.ResourcesPath, "mined_others.txt");
            jlFields = JLFieldUtils.JLFieldsForWordDicts;
        }

        Dictionary<JLField, string> miningParameters = GetMiningParameters(lookupResults, currentLookupResultIndex, currentText, formattedDefinitions, selectedDefinitions, currentCharPosition, selectedSpelling, false, null);
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
                string jlFieldContent = value.ReplaceLineEndings("\\n").Replace("\t", "  ", StringComparison.Ordinal);
                _ = lineToMine.Append(CultureInfo.InvariantCulture, $"{jlField.GetDescription()}: ")
                    .Append(jlFieldContent)
                    .Append(i < jlFields.Length - 1 ? '\t' : '\n');
            }
        }

        await File.AppendAllTextAsync(filePath, lineToMine.ToString()).ConfigureAwait(false);

        StatsUtils.IncrementStat(StatType.CardsMined);

        Utils.Logger.Information("Mined {SelectedSpelling}", selectedSpelling);
        if (CoreConfigManager.Instance.NotifyWhenMiningSucceeds)
        {
            Utils.Frontend.Alert(AlertLevel.Success, $"Mined {selectedSpelling}");
        }
    }

    public static async ValueTask<bool[]?> CheckDuplicates(LookupResult[] lookupResults, string currentText, int currentCharPosition)
    {
        Dictionary<MineType, AnkiConfig>? ankiConfigDict = await AnkiConfigUtils.ReadAnkiConfig().ConfigureAwait(false);
        if (ankiConfigDict is null)
        {
            return null;
        }

        List<Note> notes = new(lookupResults.Length);
        List<int> positions = new(lookupResults.Length);
        bool[] results = new bool[lookupResults.Length];

        for (int i = 0; i < lookupResults.Length; i++)
        {
            LookupResult lookupResult = lookupResults[i];
            DictType dictType = lookupResult.Dict.Type;

            AnkiConfig? ankiConfig;
            if (DictUtils.s_wordDictTypes.Contains(dictType))
            {
                _ = ankiConfigDict.TryGetValue(MineType.Word, out ankiConfig);
            }
            else if (DictUtils.KanjiDictTypes.Contains(dictType))
            {
                _ = ankiConfigDict.TryGetValue(MineType.Kanji, out ankiConfig);
            }
            else if (DictUtils.s_nameDictTypes.Contains(dictType))
            {
                _ = ankiConfigDict.TryGetValue(MineType.Name, out ankiConfig);
            }
            else
            {
                _ = ankiConfigDict.TryGetValue(MineType.Other, out ankiConfig);
            }

            if (ankiConfig is null || ankiConfig.Fields.Count is 0)
            {
                continue;
            }

            (string firstFieldName, JLField firstField) = ankiConfig.Fields.GetAt(0);
            string? firstFieldValue = GetMiningParameter(firstField, lookupResults, i, currentText, currentCharPosition);
            if (firstFieldValue is null)
            {
                continue;
            }

            Dictionary<string, string> fields = new(1, StringComparer.Ordinal)
            {
                { firstFieldName, firstFieldValue }
            };

            Note note = new(ankiConfig.DeckName, ankiConfig.ModelName, fields);
            notes.Add(note);
            positions.Add(i);
        }

        if (notes.Count is 0)
        {
            return null;
        }

        ReadOnlyMemory<bool> canAddNoteList = await AnkiUtils.CanAddNotes(notes).ConfigureAwait(false);
        if (canAddNoteList.IsEmpty)
        {
            return null;
        }

        ReadOnlySpan<bool> canAddNoteSpan = canAddNoteList.Span;
        for (int i = 0; i < canAddNoteSpan.Length; i++)
        {
            results[positions[i]] = !canAddNoteSpan[i];
        }

        return results;
    }

    public static async Task Mine(LookupResult[] lookupResults, int currentLookupResultIndex, string currentText, string? formattedDefinitions, string? selectedDefinitions, int currentCharPosition, string selectedSpelling)
    {
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        if (!coreConfigManager.AnkiIntegration)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return;
        }

        Dictionary<MineType, AnkiConfig>? ankiConfigDict = await AnkiConfigUtils.ReadAnkiConfig().ConfigureAwait(false);
        if (ankiConfigDict is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return;
        }

        AnkiConfig? ankiConfig;
        LookupResult lookupResult = lookupResults[currentLookupResultIndex];
        if (DictUtils.s_wordDictTypes.Contains(lookupResult.Dict.Type))
        {
            _ = ankiConfigDict.TryGetValue(MineType.Word, out ankiConfig);
        }
        else if (DictUtils.KanjiDictTypes.Contains(lookupResult.Dict.Type))
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

        OrderedDictionary<string, JLField> userFields = ankiConfig.Fields;
        Dictionary<JLField, string> miningParams = GetMiningParameters(lookupResults, currentLookupResultIndex, currentText, formattedDefinitions, selectedDefinitions, currentCharPosition, selectedSpelling, true, userFields);
        Dictionary<string, string> fields = ConvertFields(userFields, miningParams);

        // Audio/Picture/Video shouldn't be set here
        // Otherwise AnkiConnect will place them under the "collection.media" folder even when it's a duplicate note
        Note note = new(ankiConfig.DeckName, ankiConfig.ModelName, fields);
        bool? canAddNote = await AnkiUtils.CanAddNote(note).ConfigureAwait(false);
        if (canAddNote is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, $"Mining failed for {selectedSpelling}");
            Utils.Logger.Error("Mining failed for {SelectedSpelling}", selectedSpelling);
            return;
        }

        if (!coreConfigManager.AllowDuplicateCards && !canAddNote.Value)
        {
            Utils.Frontend.Alert(AlertLevel.Error, $"Cannot mine {selectedSpelling} because it is a duplicate card");
            Utils.Logger.Information("Cannot mine {SelectedSpelling} because it is a duplicate card", selectedSpelling);
            return;
        }

        note.Tags = ankiConfig.Tags;
        note.Options = new Dictionary<string, object>(1, StringComparer.Ordinal)
        {
            {
                "allowDuplicate", coreConfigManager.AllowDuplicateCards
            }
        };

        List<string> audioFields = FindFields(JLField.Audio, userFields);
        bool needsAudio = audioFields.Count > 0;
        string selectedReading = selectedSpelling == lookupResult.PrimarySpelling && lookupResult.Readings is not null
            ? lookupResult.Readings[0]
            : selectedSpelling;

        AudioResponse? audioResponse = needsAudio
            ? await AudioUtils.GetPrioritizedAudio(lookupResult.PrimarySpelling, selectedReading).ConfigureAwait(false)
            : null;

        byte[]? audioData = audioResponse?.AudioData;
        if (audioResponse?.AudioSource is AudioSourceType.TextToSpeech)
        {
            audioData = await Utils.Frontend.GetAudioResponseFromTextToSpeech(selectedReading).ConfigureAwait(false);
        }

        if (audioData is not null)
        {
            Debug.Assert(audioResponse is not null);
            note.Audios =
                [
                    new Dictionary<string, object>(4, StringComparer.Ordinal)
                    {
                        {
                            "data", audioData
                        },
                        {
                            "filename", $"JL_audio_{selectedReading}_{lookupResult.PrimarySpelling}.{audioResponse.AudioFormat}"
                        },
                        {
                            "skipHash", NetworkUtils.Jpod101NoAudioMd5Hash
                        },
                        {
                            "fields", audioFields
                        }
                    }
                ];
        }

        List<string> imageFields = FindFields(JLField.Image, userFields);
        bool needsImage = imageFields.Count > 0;
        byte[]? imageBytes = needsImage
            ? await Utils.Frontend.GetImageFromClipboardAsByteArray().ConfigureAwait(false)
            : null;

        if (imageBytes is not null)
        {
            note.Pictures =
                [
                    new Dictionary<string, object>(3, StringComparer.Ordinal)
                    {
                        {
                            "data", imageBytes
                        },
                        {
                            "filename", $"JL_image_{selectedReading}_{lookupResult.PrimarySpelling}.png"
                        },
                        {
                            "fields", imageFields
                        }
                    }
                ];
        }

        Response? response = await AnkiConnect.AddNoteToDeck(note).ConfigureAwait(false);
        if (response is null)
        {
            Utils.Frontend.Alert(AlertLevel.Error, $"Mining failed for {selectedSpelling}");
            Utils.Logger.Error("Mining failed for {SelectedSpelling}", selectedSpelling);
            return;
        }

        bool showNoAudioMessage = needsAudio && (audioData is null || Utils.GetMd5String(audioData) is NetworkUtils.Jpod101NoAudioMd5Hash);
        bool showDuplicateCardMessage = !canAddNote.Value;
        string message = $"Mined {selectedSpelling}{(showNoAudioMessage ? " (No Audio)" : "")}{(showDuplicateCardMessage ? " (Duplicate)" : "")}";

        Utils.Logger.Information("{Message}", message);
        if (coreConfigManager.NotifyWhenMiningSucceeds)
        {
            Utils.Frontend.Alert(showNoAudioMessage || showDuplicateCardMessage ? AlertLevel.Warning : AlertLevel.Success, message);
        }

        if (coreConfigManager.ForceSyncAnki)
        {
            await AnkiConnect.Sync().ConfigureAwait(false);
        }

        StatsUtils.IncrementStat(StatType.CardsMined);
    }

    /// <summary>
    /// Converts JLField,Value pairs to UserField,Value pairs <br/>
    /// JLField is our internal name of a mining field <br/>
    /// Value is the actual content of a mining field (e.g. if the field name is LocalTime, then it should contain the current time) <br/>
    /// UserField is the name of the user's field in Anki (e.g. Expression) <br/>
    /// </summary>
    private static Dictionary<string, string> ConvertFields(OrderedDictionary<string, JLField> userFields, Dictionary<JLField, string> miningParams)
    {
        Dictionary<string, string> dict = new(userFields.Count, StringComparer.Ordinal);
        int userFieldsCount = userFields.Count;
        for (int i = 0; i < userFieldsCount; i++)
        {
            (string key, JLField value) = userFields.GetAt(i);
            if (miningParams.TryGetValue(value, out string? fieldValue))
            {
                dict.Add(key, fieldValue);
            }
        }

        return dict;
    }

    private static List<string> FindFields(JLField jlField, OrderedDictionary<string, JLField> userFields)
    {
        List<string> matchingFieldNames = [];
        foreach ((string fieldName, JLField fieldValue) in userFields)
        {
            if (fieldValue == jlField)
            {
                matchingFieldNames.Add(fieldName);
            }
        }

        return matchingFieldNames;
    }
}
