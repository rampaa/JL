using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.External.AnkiConnect;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Lookup;
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

    private static string GetLeadingSentencePart(LookupResult lookupResult, ReadOnlySpan<char> currentText, int currentCharPosition)
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

    private static string GetTrailingSentencePart(LookupResult lookupResult, ReadOnlySpan<char> currentText, int currentCharPosition)
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

    private static string GetSentence(LookupResult lookupResult, ReadOnlySpan<char> currentText, int currentCharPosition)
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

    private static string GetSourceText(LookupResult lookupResult, ReadOnlySpan<char> currentText, int currentCharPosition)
    {
        string leadingSourcePart = currentText[..currentCharPosition].ToString().ReplaceLineEndings("<br/>");
        string trailingSourcePart = currentText[(currentCharPosition + lookupResult.MatchedText.Length)..].ToString().ReplaceLineEndings("<br/>");
        return $"{leadingSourcePart}<b>{lookupResult.MatchedText}</b>{trailingSourcePart}".ReplaceLineEndings("<br/>");
    }

    private static string? GetPrimarySpellingAndReadings(LookupResult lookupResult)
    {
        if (lookupResult.Readings is null)
        {
            return null;
        }

        StringBuilder stringBuilder = ObjectPoolManager.StringBuilderPool.Get();
        for (int i = 0; i < lookupResult.Readings.Length; i++)
        {
            _ = stringBuilder.Append(JapaneseUtils.GetPrimarySpellingAndReadingMapping(lookupResult.PrimarySpelling, lookupResult.Readings[i]));
            if (i + 1 != lookupResult.Readings.Length)
            {
                _ = stringBuilder.Append('、');
            }
        }

        string str = stringBuilder.ToString();
        ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
        return str;
    }

    private static string? GetRawFrequencies(LookupResult lookupResult)
    {
        if (lookupResult.Frequencies is null)
        {
            return null;
        }

        StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
        foreach (LookupFrequencyResult lookupFrequencyResult in lookupResult.Frequencies.AsReadOnlySpan())
        {
            if (lookupFrequencyResult.Freq is > 0 and < int.MaxValue)
            {
                if (sb.Length > 0)
                {
                    _ = sb.Append(", ");
                }

                _ = sb.Append(lookupFrequencyResult.Freq);
            }
        }

        if (sb.Length is 0)
        {
            ObjectPoolManager.StringBuilderPool.Return(sb);
            return null;
        }

        string rawFrequencies = sb.ToString();
        ObjectPoolManager.StringBuilderPool.Return(sb);
        return rawFrequencies;
    }

    private static string? GetPreferredFrequency(LookupResult lookupResult)
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

    private static string? GetFrequencyHarmonicMean(LookupResult lookupResult)
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

    private static string? GetPitchAccents(LookupResult lookupResult)
    {
        if (lookupResult.PitchPositions is null)
        {
            return null;
        }

        string[] expressions = lookupResult.Readings ?? [lookupResult.PrimarySpelling];
        StringBuilder expressionsWithPitchAccentBuilder = ObjectPoolManager.StringBuilderPool.Get();
        _ = expressionsWithPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n");

        bool addSeparator = false;
        for (int i = 0; i < expressions.Length; i++)
        {
            byte pitchPosition = lookupResult.PitchPositions[i];
            if (pitchPosition is not byte.MaxValue)
            {
                if (addSeparator)
                {
                    _ = expressionsWithPitchAccentBuilder.Append('、');
                }

                StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
                _ = expressionsWithPitchAccentBuilder.Append(GetExpressionWithPitchAccent(expressions[i], sb, pitchPosition));
                ObjectPoolManager.StringBuilderPool.Return(sb);

                addSeparator = true;
            }
        }

        string expressionsWithPitchAccent = expressionsWithPitchAccentBuilder.ToString();
        ObjectPoolManager.StringBuilderPool.Return(expressionsWithPitchAccentBuilder);
        return expressionsWithPitchAccent;
    }

    private static string? GetNumericPitchAccents(LookupResult lookupResult)
    {
        if (lookupResult.PitchPositions is null)
        {
            return null;
        }

        string[] expressions = lookupResult.Readings ?? [lookupResult.PrimarySpelling];
        StringBuilder numericPitchAccentBuilder = ObjectPoolManager.StringBuilderPool.Get();

        bool addSeparator = false;
        for (int i = 0; i < expressions.Length; i++)
        {
            byte pitchPosition = lookupResult.PitchPositions[i];
            if (pitchPosition is not byte.MaxValue)
            {
                if (addSeparator)
                {
                    _ = numericPitchAccentBuilder.Append(", ");
                }

                _ = numericPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{expressions[i]}: {pitchPosition}");
                addSeparator = true;
            }
        }

        string numericPitchAccent = numericPitchAccentBuilder.ToString();
        ObjectPoolManager.StringBuilderPool.Return(numericPitchAccentBuilder);
        return numericPitchAccent;
    }

    private static string? GetPitchAccentForFirstReading(LookupResult lookupResult)
    {
        if (lookupResult.PitchPositions is not null)
        {
            byte firstPitchPosition = lookupResult.PitchPositions[0];
            if (firstPitchPosition is not byte.MaxValue)
            {
                string expression = lookupResult.Readings is not null
                    ? lookupResult.Readings[0]
                    : lookupResult.PrimarySpelling;

                StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
                string pitchAccentForFirstReading = string.Create(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n{GetExpressionWithPitchAccent(expression, sb, firstPitchPosition)}");
                ObjectPoolManager.StringBuilderPool.Return(sb);
                return pitchAccentForFirstReading;
            }
        }

        return null;
    }

    private static string? GetNumericPitchAccentForFirstReading(LookupResult lookupResult)
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

    private static string? GetPitchAccentCategories(LookupResult lookupResult)
    {
        if (lookupResult.PitchPositions is null)
        {
            return null;
        }

        string[] expressions = lookupResult.Readings ?? [lookupResult.PrimarySpelling];
        StringBuilder pitchAccentCategoriesBuilder = ObjectPoolManager.StringBuilderPool.Get();

        bool addSeparator = false;
        for (int i = 0; i < expressions.Length; i++)
        {
            byte pitchPosition = lookupResult.PitchPositions[i];
            if (pitchPosition is not byte.MaxValue)
            {
                if (addSeparator)
                {
                    _ = pitchAccentCategoriesBuilder.Append(", ");
                }

                string expression = expressions[i];
                _ = pitchAccentCategoriesBuilder.Append(CultureInfo.InvariantCulture, $"{expression}: {GetPitchAccentCategory(expression, pitchPosition)}");
                addSeparator = true;
            }
        }

        string pitchAccentCategories = pitchAccentCategoriesBuilder.ToString();
        ObjectPoolManager.StringBuilderPool.Return(pitchAccentCategoriesBuilder);
        return pitchAccentCategories;
    }

    private static string? GetPitchAccentCategoryForFirstReading(LookupResult lookupResult)
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

    private static string? GetReadingsWithOrthographyInfo(LookupResult lookupResult)
    {
        return lookupResult.Readings is not null
            ? lookupResult.JmdictLookupResult?.ReadingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.Readings, lookupResult.JmdictLookupResult.ReadingsOrthographyInfoList)
                : string.Join('、', lookupResult.Readings)
            : null;
    }

    private static string? GetReadings(LookupResult lookupResult)
    {
        return lookupResult.Readings is not null
            ? string.Join('、', lookupResult.Readings)
            : null;
    }

    private static string? GetPrimarySpellingAndFirstReading(LookupResult lookupResult)
    {
        return lookupResult.Readings is not null
            ? JapaneseUtils.GetPrimarySpellingAndReadingMapping(lookupResult.PrimarySpelling, lookupResult.Readings[0])
            : null;
    }

    private static string GetPrimarySpellingWithOrthographyInfo(LookupResult lookupResult)
    {
        return lookupResult.JmdictLookupResult?.PrimarySpellingOrthographyInfoList is not null
            ? $"{lookupResult.PrimarySpelling} ({string.Join(", ", lookupResult.JmdictLookupResult.PrimarySpellingOrthographyInfoList)})"
            : lookupResult.PrimarySpelling;
    }

    private static string? GetAlternativeSpellings(LookupResult lookupResult)
    {
        return lookupResult.AlternativeSpellings is not null
            ? string.Join('、', lookupResult.AlternativeSpellings)
            : null;
    }

    private static string? GetAlternativeSpellingsWithOrthographyInfo(LookupResult lookupResult)
    {
        return lookupResult.AlternativeSpellings is not null
            ? lookupResult.JmdictLookupResult?.AlternativeSpellingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.AlternativeSpellings, lookupResult.JmdictLookupResult.AlternativeSpellingsOrthographyInfoList)
                : string.Join('、', lookupResult.AlternativeSpellings)
            : null;
    }

    private static string? GetOnReadings(LookupResult lookupResult)
    {
        return lookupResult.KanjiLookupResult?.OnReadings is not null
            ? string.Join('、', lookupResult.KanjiLookupResult.OnReadings)
            : null;
    }

    private static string? GetKunReadings(LookupResult lookupResult)
    {
        return lookupResult.KanjiLookupResult?.KunReadings is not null
            ? string.Join('、', lookupResult.KanjiLookupResult.KunReadings)
            : null;
    }

    private static string? GetNanoriReadings(LookupResult lookupResult)
    {
        return lookupResult.KanjiLookupResult?.NanoriReadings is not null
            ? string.Join('、', lookupResult.KanjiLookupResult.NanoriReadings)
            : null;
    }

    private static string? GetEdictId(LookupResult lookupResult)
    {
        return lookupResult.EntryId > 0
            ? lookupResult.EntryId.ToString(CultureInfo.InvariantCulture)
            : null;
    }

    private static string? GetKanjiComposition(LookupResult lookupResult)
    {
        return lookupResult.KanjiLookupResult?.KanjiComposition is not null
            ? string.Join('、', lookupResult.KanjiLookupResult.KanjiComposition)
            : null;
    }

    private static string? GetStrokeCount(LookupResult lookupResult)
    {
        return lookupResult.KanjiLookupResult?.StrokeCount > 0
            ? lookupResult.KanjiLookupResult.StrokeCount.ToString(CultureInfo.InvariantCulture)
            : null;
    }

    private static string? GetKanjiGrade(LookupResult lookupResult)
    {
        return lookupResult.KanjiLookupResult is not null && lookupResult.KanjiLookupResult.KanjiGrade != byte.MaxValue
            ? lookupResult.KanjiLookupResult.KanjiGrade.ToString(CultureInfo.InvariantCulture)
            : null;
    }

    private static string? GetRadicalNames(LookupResult lookupResult)
    {
        return lookupResult.KanjiLookupResult?.RadicalNames is not null
            ? string.Join('、', lookupResult.KanjiLookupResult.RadicalNames)
            : null;
    }

    private static string? GetFrequency(LookupResult lookupResult)
    {
        return lookupResult.Frequencies is not null
            ? LookupResultUtils.FrequenciesToText(lookupResult.Frequencies.AsReadOnlySpan(), true, lookupResult.Frequencies.Count is 1)
            : null;
    }

    private static string? GetDefinitionsFromMultipleDictionaries(LookupResult[] lookupResults, int currentLookupResultIndex, LookupResult lookupResult)
    {
        return GetDefinitionsFromAllDictionaries(lookupResults, currentLookupResultIndex, lookupResult.PrimarySpelling, lookupResult.FormattedDefinitions, true);
    }

    private static string GetDeconjugatedMatchedText(LookupResult lookupResult)
    {
        return lookupResult.DeconjugatedMatchedText ?? lookupResult.MatchedText;
    }

    private static string? GetMiningParameter(JLField field, LookupResult[] lookupResults, int currentLookupResultIndex, ReadOnlySpan<char> currentText, int currentCharPosition)
    {
        return field switch
        {
            JLField.LeadingSentencePart => GetLeadingSentencePart(lookupResults[currentLookupResultIndex], currentText, currentCharPosition),
            JLField.TrailingSentencePart => GetTrailingSentencePart(lookupResults[currentLookupResultIndex], currentText, currentCharPosition),
            JLField.Sentence => GetSentence(lookupResults[currentLookupResultIndex], currentText, currentCharPosition),
            JLField.SourceText => GetSourceText(lookupResults[currentLookupResultIndex], currentText, currentCharPosition),
            JLField.Readings => GetReadings(lookupResults[currentLookupResultIndex]),
            JLField.ReadingsWithOrthographyInfo => GetReadingsWithOrthographyInfo(lookupResults[currentLookupResultIndex]),
            JLField.FirstReading => lookupResults[currentLookupResultIndex].Readings?[0],
            JLField.PrimarySpellingAndReadings => GetPrimarySpellingAndReadings(lookupResults[currentLookupResultIndex]),
            JLField.PrimarySpellingAndFirstReading => GetPrimarySpellingAndFirstReading(lookupResults[currentLookupResultIndex]),
            JLField.PrimarySpellingWithOrthographyInfo => GetPrimarySpellingWithOrthographyInfo(lookupResults[currentLookupResultIndex]),
            JLField.AlternativeSpellings => GetAlternativeSpellings(lookupResults[currentLookupResultIndex]),
            JLField.AlternativeSpellingsWithOrthographyInfo => GetAlternativeSpellingsWithOrthographyInfo(lookupResults[currentLookupResultIndex]),
            JLField.MatchedText => lookupResults[currentLookupResultIndex].MatchedText,
            JLField.DeconjugatedMatchedText => GetDeconjugatedMatchedText(lookupResults[currentLookupResultIndex]),
            JLField.KanjiStats => lookupResults[currentLookupResultIndex].KanjiLookupResult?.KanjiStats,
            JLField.OnReadings => GetOnReadings(lookupResults[currentLookupResultIndex]),
            JLField.KunReadings => GetKunReadings(lookupResults[currentLookupResultIndex]),
            JLField.NanoriReadings => GetNanoriReadings(lookupResults[currentLookupResultIndex]),
            JLField.EdictId => GetEdictId(lookupResults[currentLookupResultIndex]),
            JLField.DeconjugationProcess => lookupResults[currentLookupResultIndex].DeconjugationProcess,
            JLField.KanjiComposition => GetKanjiComposition(lookupResults[currentLookupResultIndex]),
            JLField.StrokeCount => GetStrokeCount(lookupResults[currentLookupResultIndex]),
            JLField.KanjiGrade => GetKanjiGrade(lookupResults[currentLookupResultIndex]),
            JLField.RadicalNames => GetRadicalNames(lookupResults[currentLookupResultIndex]),
            JLField.DefinitionsFromMultipleDictionaries => GetDefinitionsFromMultipleDictionaries(lookupResults, currentLookupResultIndex, lookupResults[currentLookupResultIndex]),
            JLField.LeadingSourceTextPart => currentText[..currentCharPosition].ToString().ReplaceLineEndings("<br/>"),
            JLField.TrailingSourceTextPart => currentText[(currentCharPosition + lookupResults[currentLookupResultIndex].MatchedText.Length)..].ToString().ReplaceLineEndings("<br/>"),
            JLField.DictionaryName => lookupResults[currentLookupResultIndex].Dict.Name,
            JLField.Frequencies => GetFrequency(lookupResults[currentLookupResultIndex]),
            JLField.RawFrequencies => GetRawFrequencies(lookupResults[currentLookupResultIndex]),
            JLField.PreferredFrequency => GetPreferredFrequency(lookupResults[currentLookupResultIndex]),
            JLField.FrequencyHarmonicMean => GetFrequencyHarmonicMean(lookupResults[currentLookupResultIndex]),
            JLField.WordClasses => GetWordClasses(lookupResults[currentLookupResultIndex]),
            JLField.PitchAccents => GetPitchAccents(lookupResults[currentLookupResultIndex]),
            JLField.NumericPitchAccents => GetNumericPitchAccents(lookupResults[currentLookupResultIndex]),
            JLField.PitchAccentForFirstReading => GetPitchAccentForFirstReading(lookupResults[currentLookupResultIndex]),
            JLField.NumericPitchAccentForFirstReading => GetNumericPitchAccentForFirstReading(lookupResults[currentLookupResultIndex]),
            JLField.PitchAccentCategories => GetPitchAccentCategories(lookupResults[currentLookupResultIndex]),
            JLField.PitchAccentCategoryForFirstReading => GetPitchAccentCategoryForFirstReading(lookupResults[currentLookupResultIndex]),
            JLField.SelectedSpelling or JLField.PrimarySpelling => lookupResults[currentLookupResultIndex].PrimarySpelling,
            JLField.SelectedDefinitions or JLField.Definitions => lookupResults[currentLookupResultIndex].FormattedDefinitions?.ReplaceLineEndings("<br/>"),
            JLField.Nothing or JLField.Audio or JLField.SentenceAudio or JLField.SourceTextAudio or JLField.Image or JLField.LocalTime => null,
            _ => null
        };
    }

    private static Dictionary<JLField, string> GetMiningParameters(LookupResult[] lookupResults, int currentLookupResultIndex, string currentText, string sentence, string? formattedDefinitions, string? selectedDefinitions, int currentCharPosition, string selectedSpelling, bool useHtmlTags, FrozenSet<JLField>? jlFields)
    {
        LookupResult lookupResult = lookupResults[currentLookupResultIndex];

        bool mineAllFields = jlFields is null;
        Dictionary<JLField, string> miningParams = new(mineAllFields ? JLFieldUtils.JLFieldsForWordDicts.Length - JLFieldUtils.s_jlFieldsToExcludeFromWhenMiningToFile.Count : jlFields!.Count);
        if (mineAllFields || jlFields!.Contains(JLField.LocalTime))
        {
            miningParams[JLField.LocalTime] = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
        }

        if (mineAllFields || jlFields!.Contains(JLField.DictionaryName))
        {
            miningParams[JLField.DictionaryName] = lookupResult.Dict.Name;
        }

        if (mineAllFields || jlFields!.Contains(JLField.MatchedText))
        {
            miningParams[JLField.MatchedText] = lookupResult.MatchedText;
        }

        if (mineAllFields || jlFields!.Contains(JLField.DeconjugatedMatchedText))
        {
            miningParams[JLField.DeconjugatedMatchedText] = lookupResult.DeconjugatedMatchedText ?? lookupResult.MatchedText;
        }

        if (mineAllFields || jlFields!.Contains(JLField.PrimarySpelling))
        {
            miningParams[JLField.PrimarySpelling] = lookupResult.PrimarySpelling;
        }

        if (mineAllFields || jlFields!.Contains(JLField.PrimarySpellingWithOrthographyInfo))
        {
            miningParams[JLField.PrimarySpellingWithOrthographyInfo] = GetPrimarySpellingWithOrthographyInfo(lookupResult);
        }

        if (mineAllFields || jlFields!.Contains(JLField.SelectedSpelling))
        {
            miningParams[JLField.SelectedSpelling] = selectedSpelling;
        }

        if (lookupResult.EntryId > 0 && (mineAllFields || jlFields!.Contains(JLField.EdictId)))
        {
            miningParams[JLField.EdictId] = lookupResult.EntryId.ToString(CultureInfo.InvariantCulture);
        }

        if (lookupResult.DeconjugationProcess is not null && (mineAllFields || jlFields!.Contains(JLField.DeconjugationProcess)))
        {
            miningParams[JLField.DeconjugationProcess] = lookupResult.DeconjugationProcess;
        }

        AddSourceTextFields(miningParams, jlFields, lookupResult, currentText, currentCharPosition, useHtmlTags);
        AddSentenceFields(miningParams, jlFields, lookupResult, sentence, currentText, currentCharPosition, useHtmlTags);
        AddWordClassesField(miningParams, jlFields, lookupResult);
        AddAlternativeSpellingFields(miningParams, jlFields, lookupResult);
        AddFrequencyFields(miningParams, jlFields, lookupResult);
        AddDefinitionFields(miningParams, jlFields, lookupResults, formattedDefinitions, selectedDefinitions, currentLookupResultIndex, selectedSpelling, useHtmlTags);
        AddKanjiFields(miningParams, jlFields, lookupResult.KanjiLookupResult, useHtmlTags);

        int selectedSpellingIndex = lookupResult.Readings.AsReadOnlySpan().IndexOf(selectedSpelling);
        if (selectedSpellingIndex is -1)
        {
            selectedSpellingIndex = 0;
        }

        AddReadingFields(miningParams, jlFields, lookupResult, selectedSpellingIndex);
        AddPitchPositionsFields(miningParams, jlFields, selectedSpellingIndex, lookupResult);

        return miningParams;
    }

    private static void AddSourceTextFields(Dictionary<JLField, string> miningParams, FrozenSet<JLField>? jlFields, LookupResult lookupResult, string currentText, int currentCharPosition, bool useHtmlTags)
    {
        bool mineAllFields = jlFields is null;
        if (!mineAllFields && !jlFields!.Contains(JLField.SourceText) && !jlFields.Contains(JLField.LeadingSourceTextPart) && !jlFields.Contains(JLField.TrailingSourceTextPart))
        {
            return;
        }

        ReadOnlySpan<char> currentTextSpan = currentText.AsSpan();
        string leadingSourcePart = currentTextSpan[..currentCharPosition].ToString();
        string trailingSourcePart = currentTextSpan[(currentCharPosition + lookupResult.MatchedText.Length)..].ToString();
        if (useHtmlTags)
        {
            leadingSourcePart = leadingSourcePart.ReplaceLineEndings("<br/>");
            trailingSourcePart = trailingSourcePart.ReplaceLineEndings("<br/>");
        }

        if (mineAllFields || jlFields!.Contains(JLField.LeadingSourceTextPart))
        {
            miningParams[JLField.LeadingSourceTextPart] = leadingSourcePart;
        }

        if (mineAllFields || jlFields!.Contains(JLField.TrailingSourceTextPart))
        {
            miningParams[JLField.TrailingSourceTextPart] = trailingSourcePart;
        }

        if (mineAllFields || jlFields!.Contains(JLField.SourceText))
        {
            miningParams[JLField.SourceText] = useHtmlTags
                ? $"{leadingSourcePart}<b>{lookupResult.MatchedText}</b>{trailingSourcePart}"
                : currentText;
        }
    }

    private static void AddSentenceFields(Dictionary<JLField, string> miningParams, FrozenSet<JLField>? jlFields, LookupResult lookupResult, string sentence, string currentText, int currentCharPosition, bool useHtmlTags)
    {
        bool mineAllFields = jlFields is null;
        if (!mineAllFields && !jlFields!.Contains(JLField.Sentence) && !jlFields.Contains(JLField.LeadingSentencePart) && !jlFields.Contains(JLField.TrailingSentencePart))
        {
            return;
        }

        ReadOnlySpan<char> currentTextSpan = currentText.AsSpan();
        int searchStartIndex = currentCharPosition + lookupResult.MatchedText.Length - sentence.Length;
        if (searchStartIndex < 0 || searchStartIndex >= currentText.Length)
        {
            searchStartIndex = 0;
        }

        int sentenceStartIndex = currentTextSpan.IndexOf(sentence, searchStartIndex);

        ReadOnlySpan<char> leadingSentencePart = currentTextSpan[sentenceStartIndex..currentCharPosition];
        if (mineAllFields || jlFields!.Contains(JLField.LeadingSentencePart))
        {
            miningParams[JLField.LeadingSentencePart] = leadingSentencePart.ToString();
        }

        ReadOnlySpan<char> trailingSentencePart = currentTextSpan[(lookupResult.MatchedText.Length + currentCharPosition)..(sentenceStartIndex + sentence.Length)];
        if (mineAllFields || jlFields!.Contains(JLField.TrailingSentencePart))
        {
            miningParams[JLField.TrailingSentencePart] = trailingSentencePart.ToString();
        }

        if (mineAllFields || jlFields!.Contains(JLField.Sentence))
        {
            miningParams[JLField.Sentence] = useHtmlTags
                ? $"{leadingSentencePart}<b>{lookupResult.MatchedText}</b>{trailingSentencePart}"
                : sentence;
        }
    }

    private static void AddReadingFields(Dictionary<JLField, string> miningParams, FrozenSet<JLField>? jlFields, LookupResult lookupResult, int selectedSpellingIndex)
    {
        if (lookupResult.Readings is null)
        {
            return;
        }

        bool mineAllFields = jlFields is null;

        if (mineAllFields || jlFields!.Contains(JLField.Readings) || jlFields.Contains(JLField.ReadingsWithOrthographyInfo))
        {
            string readings = string.Join('、', lookupResult.Readings);
            if (mineAllFields || jlFields!.Contains(JLField.Readings))
            {
                miningParams[JLField.Readings] = readings;
            }

            if (mineAllFields || jlFields!.Contains(JLField.ReadingsWithOrthographyInfo))
            {
                miningParams[JLField.ReadingsWithOrthographyInfo] = lookupResult.JmdictLookupResult?.ReadingsOrthographyInfoList is not null
                    ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.Readings, lookupResult.JmdictLookupResult.ReadingsOrthographyInfoList)
                    : readings;
            }
        }

        string firstReading = lookupResult.Readings[selectedSpellingIndex];
        if (mineAllFields || jlFields!.Contains(JLField.FirstReading))
        {
            miningParams[JLField.FirstReading] = firstReading;
        }

        if (mineAllFields || jlFields!.Contains(JLField.PrimarySpellingAndFirstReading))
        {
            miningParams[JLField.PrimarySpellingAndFirstReading] = JapaneseUtils.GetPrimarySpellingAndReadingMapping(lookupResult.PrimarySpelling, firstReading);
        }

        if (mineAllFields || jlFields!.Contains(JLField.PrimarySpellingAndReadings))
        {
            StringBuilder primarySpellingAndReadingStringBuilder = ObjectPoolManager.StringBuilderPool.Get();
            for (int i = 0; i < lookupResult.Readings.Length; i++)
            {
                _ = primarySpellingAndReadingStringBuilder.Append(JapaneseUtils.GetPrimarySpellingAndReadingMapping(lookupResult.PrimarySpelling, lookupResult.Readings[i]));
                if (i + 1 != lookupResult.Readings.Length)
                {
                    _ = primarySpellingAndReadingStringBuilder.Append('、');
                }
            }

            miningParams[JLField.PrimarySpellingAndReadings] = primarySpellingAndReadingStringBuilder.ToString();
            ObjectPoolManager.StringBuilderPool.Return(primarySpellingAndReadingStringBuilder);
        }
    }

    private static void AddWordClassesField(Dictionary<JLField, string> miningParams, FrozenSet<JLField>? jlFields, LookupResult lookupResult)
    {
        if (jlFields is not null && !jlFields.Contains(JLField.WordClasses))
        {
            return;
        }

        string? wordClasses = GetWordClasses(lookupResult);
        if (wordClasses is not null)
        {
            miningParams[JLField.WordClasses] = wordClasses;
        }
    }

    private static void AddAlternativeSpellingFields(Dictionary<JLField, string> miningParams, FrozenSet<JLField>? jlFields, LookupResult lookupResult)
    {
        if (lookupResult.AlternativeSpellings is null)
        {
            return;
        }

        bool mineAllFields = jlFields is null;
        if (!mineAllFields && !jlFields!.Contains(JLField.AlternativeSpellings) && !jlFields.Contains(JLField.AlternativeSpellingsWithOrthographyInfo))
        {
            return;
        }

        string alternativeSpellings = string.Join('、', lookupResult.AlternativeSpellings);
        if (mineAllFields || jlFields!.Contains(JLField.AlternativeSpellings))
        {
            miningParams[JLField.AlternativeSpellings] = alternativeSpellings;
        }

        if (mineAllFields || jlFields!.Contains(JLField.AlternativeSpellingsWithOrthographyInfo))
        {
            miningParams[JLField.AlternativeSpellingsWithOrthographyInfo] = lookupResult.JmdictLookupResult?.AlternativeSpellingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToText(lookupResult.AlternativeSpellings, lookupResult.JmdictLookupResult.AlternativeSpellingsOrthographyInfoList)
                : alternativeSpellings;
        }
    }

    private static void AddFrequencyFields(Dictionary<JLField, string> miningParams, FrozenSet<JLField>? jlFields, LookupResult lookupResult)
    {
        if (lookupResult.Frequencies is null)
        {
            return;
        }

        bool mineAllFields = jlFields is null;
        if (!mineAllFields && !jlFields!.Contains(JLField.Frequencies) && !jlFields.Contains(JLField.RawFrequencies) && !jlFields.Contains(JLField.FrequencyHarmonicMean) && !jlFields.Contains(JLField.PreferredFrequency))
        {
            return;
        }

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

        if (validFrequencies.Count is 0)
        {
            return;
        }

        if (mineAllFields || jlFields!.Contains(JLField.Frequencies))
        {
            miningParams[JLField.Frequencies] = LookupResultUtils.FrequenciesToText(lookupResult.Frequencies.AsReadOnlySpan(), true, lookupResult.Frequencies.Count is 1);
        }

        if (mineAllFields || jlFields!.Contains(JLField.RawFrequencies))
        {
            miningParams[JLField.RawFrequencies] = string.Join(", ", validFrequencyValues);
        }

        if (mineAllFields || jlFields!.Contains(JLField.FrequencyHarmonicMean))
        {
            miningParams[JLField.FrequencyHarmonicMean] = CalculateHarmonicMean(validFrequencies.AsReadOnlySpan()).ToString(CultureInfo.InvariantCulture);
        }

        int firstFrequency = lookupResult.Frequencies[0].Freq;
        if (firstFrequency is > 0 and < int.MaxValue && (mineAllFields || jlFields!.Contains(JLField.PreferredFrequency)))
        {
            miningParams[JLField.PreferredFrequency] = firstFrequency.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static void AddDefinitionFields(Dictionary<JLField, string> miningParams, FrozenSet<JLField>? jlFields, LookupResult[] lookupResults, string? formattedDefinitions, string? selectedDefinitions, int currentLookupResultIndex, string selectedSpelling, bool useHtmlTags)
    {
        bool mineAllFields = jlFields is null;
        if (formattedDefinitions is not null)
        {
            if (mineAllFields || jlFields!.Contains(JLField.Definitions) || jlFields.Contains(JLField.SelectedDefinitions))
            {
                formattedDefinitions = useHtmlTags
                    ? formattedDefinitions.ReplaceLineEndings("<br/>")
                    : formattedDefinitions;

                if (mineAllFields || jlFields!.Contains(JLField.Definitions))
                {
                    miningParams[JLField.Definitions] = formattedDefinitions;
                }

                if (selectedDefinitions is null && (mineAllFields || jlFields!.Contains(JLField.SelectedDefinitions)))
                {
                    miningParams[JLField.SelectedDefinitions] = formattedDefinitions;
                }
            }
        }

        if (selectedDefinitions is not null && (mineAllFields || jlFields!.Contains(JLField.SelectedDefinitions)))
        {
            miningParams[JLField.SelectedDefinitions] = useHtmlTags
                ? selectedDefinitions.ReplaceLineEndings("<br/>")
                : selectedDefinitions;
        }

        if (mineAllFields || jlFields!.Contains(JLField.DefinitionsFromMultipleDictionaries))
        {
            string? definitionsFromAllDictionaries = GetDefinitionsFromAllDictionaries(lookupResults, currentLookupResultIndex, selectedSpelling, formattedDefinitions, useHtmlTags);
            if (definitionsFromAllDictionaries is not null)
            {
                miningParams[JLField.DefinitionsFromMultipleDictionaries] = definitionsFromAllDictionaries;
            }
        }
    }

    private static void AddKanjiFields(Dictionary<JLField, string> miningParams, FrozenSet<JLField>? jlFields, KanjiLookupResult? kanjiLookupResult, bool useHtmlTags)
    {
        if (kanjiLookupResult is null)
        {
            return;
        }

        bool mineAllFields = jlFields is null;
        if (kanjiLookupResult.KanjiComposition is not null && (mineAllFields || jlFields!.Contains(JLField.KanjiComposition)))
        {
            miningParams[JLField.KanjiComposition] = string.Join('、', kanjiLookupResult.KanjiComposition);
        }

        if (kanjiLookupResult.KanjiStats is not null && (mineAllFields || jlFields!.Contains(JLField.KanjiStats)))
        {
            miningParams[JLField.KanjiStats] = useHtmlTags
                ? kanjiLookupResult.KanjiStats.ReplaceLineEndings("<br/>")
                : kanjiLookupResult.KanjiStats;
        }

        if (kanjiLookupResult.StrokeCount > 0 && (mineAllFields || jlFields!.Contains(JLField.StrokeCount)))
        {
            miningParams[JLField.StrokeCount] = kanjiLookupResult.StrokeCount.ToString(CultureInfo.InvariantCulture);
        }

        if (kanjiLookupResult.KanjiGrade is not byte.MaxValue && (mineAllFields || jlFields!.Contains(JLField.KanjiGrade)))
        {
            miningParams[JLField.KanjiGrade] = kanjiLookupResult.KanjiGrade.ToString(CultureInfo.InvariantCulture);
        }

        if (kanjiLookupResult.OnReadings is not null && (mineAllFields || jlFields!.Contains(JLField.OnReadings)))
        {
            miningParams[JLField.OnReadings] = string.Join('、', kanjiLookupResult.OnReadings);
        }

        if (kanjiLookupResult.KunReadings is not null && (mineAllFields || jlFields!.Contains(JLField.KunReadings)))
        {
            miningParams[JLField.KunReadings] = string.Join('、', kanjiLookupResult.KunReadings);
        }

        if (kanjiLookupResult.NanoriReadings is not null && (mineAllFields || jlFields!.Contains(JLField.NanoriReadings)))
        {
            miningParams[JLField.NanoriReadings] = string.Join('、', kanjiLookupResult.NanoriReadings);
        }

        if (kanjiLookupResult.RadicalNames is not null && (mineAllFields || jlFields!.Contains(JLField.RadicalNames)))
        {
            miningParams[JLField.RadicalNames] = string.Join('、', kanjiLookupResult.RadicalNames);
        }
    }

    private static void AddPitchPositionsFields(Dictionary<JLField, string> miningParams, FrozenSet<JLField>? jlFields, int selectedSpellingIndex, LookupResult lookupResult)
    {
        if (lookupResult.PitchPositions is null)
        {
            return;
        }

        bool mineAllFields = jlFields is null;
        if (!mineAllFields && !jlFields!.Contains(JLField.PitchAccents) && !jlFields.Contains(JLField.NumericPitchAccents) && !jlFields.Contains(JLField.PitchAccentCategories) && !jlFields.Contains(JLField.PitchAccentForFirstReading) && !jlFields.Contains(JLField.NumericPitchAccentForFirstReading) && !jlFields.Contains(JLField.PitchAccentCategoryForFirstReading))
        {
            return;
        }

        string[] expressions = lookupResult.Readings ?? [lookupResult.PrimarySpelling];

        bool addPitchAccents = mineAllFields || jlFields!.Contains(JLField.PitchAccents);
        bool addNumericPitchAccents = mineAllFields || jlFields!.Contains(JLField.NumericPitchAccents);
        bool addPitchAccentCategories = mineAllFields || jlFields!.Contains(JLField.PitchAccentCategories);

        if (addPitchAccents || addNumericPitchAccents || addPitchAccentCategories)
        {
            StringBuilder? expressionsWithPitchAccentBuilder = addPitchAccents ? ObjectPoolManager.StringBuilderPool.Get().Append(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n") : null;
            StringBuilder? numericPitchAccentBuilder = addNumericPitchAccents ? ObjectPoolManager.StringBuilderPool.Get() : null;
            StringBuilder? pitchAccentCategoriesBuilder = addPitchAccentCategories ? ObjectPoolManager.StringBuilderPool.Get() : null;

            bool addSeparator = false;
            for (int i = 0; i < expressions.Length; i++)
            {
                byte pitchPosition = lookupResult.PitchPositions[i];
                if (pitchPosition is not byte.MaxValue)
                {
                    string expression = expressions[i];
                    if (addNumericPitchAccents)
                    {
                        Debug.Assert(numericPitchAccentBuilder is not null);
                        if (addSeparator)
                        {
                            _ = numericPitchAccentBuilder.Append(", ");
                        }

                        _ = numericPitchAccentBuilder.Append(CultureInfo.InvariantCulture, $"{expression}: {pitchPosition}");
                    }

                    if (addPitchAccents)
                    {
                        Debug.Assert(expressionsWithPitchAccentBuilder is not null);
                        if (addSeparator)
                        {
                            _ = expressionsWithPitchAccentBuilder.Append('、');
                        }

                        StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
                        _ = expressionsWithPitchAccentBuilder.Append(GetExpressionWithPitchAccent(expression, sb, pitchPosition));
                        ObjectPoolManager.StringBuilderPool.Return(sb);
                    }

                    if (addPitchAccentCategories)
                    {
                        Debug.Assert(pitchAccentCategoriesBuilder is not null);
                        if (addSeparator)
                        {
                            _ = pitchAccentCategoriesBuilder.Append(", ");
                        }

                        _ = pitchAccentCategoriesBuilder.Append(CultureInfo.InvariantCulture, $"{expression}: {GetPitchAccentCategory(expression, pitchPosition)}");
                    }

                    addSeparator = true;
                }
            }

            if (addNumericPitchAccents)
            {
                Debug.Assert(numericPitchAccentBuilder is not null);
                miningParams[JLField.NumericPitchAccents] = numericPitchAccentBuilder.ToString();
                ObjectPoolManager.StringBuilderPool.Return(numericPitchAccentBuilder);
            }

            if (addPitchAccents)
            {
                Debug.Assert(expressionsWithPitchAccentBuilder is not null);
                miningParams[JLField.PitchAccents] = expressionsWithPitchAccentBuilder.ToString();
                ObjectPoolManager.StringBuilderPool.Return(expressionsWithPitchAccentBuilder);
            }

            if (addPitchAccentCategories)
            {
                Debug.Assert(pitchAccentCategoriesBuilder is not null);
                miningParams[JLField.PitchAccentCategories] = pitchAccentCategoriesBuilder.ToString();
                ObjectPoolManager.StringBuilderPool.Return(pitchAccentCategoriesBuilder);
            }
        }

        byte firstPitchPosition = lookupResult.PitchPositions[selectedSpellingIndex];
        if (firstPitchPosition is byte.MaxValue)
        {
            return;
        }

        string firstExpression = expressions[selectedSpellingIndex];
        if (mineAllFields || jlFields!.Contains(JLField.PitchAccentForFirstReading))
        {
            StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
            miningParams[JLField.PitchAccentForFirstReading] = string.Create(CultureInfo.InvariantCulture, $"{PitchAccentStyle}\n\n{GetExpressionWithPitchAccent(firstExpression, sb, firstPitchPosition)}");
            ObjectPoolManager.StringBuilderPool.Return(sb);
        }

        if (mineAllFields || jlFields!.Contains(JLField.NumericPitchAccentForFirstReading))
        {
            miningParams[JLField.NumericPitchAccentForFirstReading] = string.Create(CultureInfo.InvariantCulture, $"{firstExpression}: {firstPitchPosition}");
        }

        if (mineAllFields || jlFields!.Contains(JLField.PitchAccentCategoryForFirstReading))
        {
            miningParams[JLField.PitchAccentCategoryForFirstReading] = $"{firstExpression}: {GetPitchAccentCategory(firstExpression, firstPitchPosition)}";
        }
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

            StringBuilder singleDictStringBuilder = ObjectPoolManager.StringBuilderPool.Get();
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

            string singleDictDef = singleDictStringBuilder.ToString();
            ObjectPoolManager.StringBuilderPool.Return(singleDictStringBuilder);
            return singleDictDef;
        }

        StringBuilder stringBuilder = ObjectPoolManager.StringBuilderPool.Get();
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
                string? formattedDefinitions = otherLookupResults[0].FormattedDefinitions;
                Debug.Assert(formattedDefinitions is not null);
                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{formattedDefinitions.ReplaceLineEndings("<br/>")} </details>");
            }
            else
            {
                ReadOnlySpan<LookupResult> otherLookupResultsSpan = otherLookupResults.AsReadOnlySpan();
                for (int j = 0; j < otherLookupResultsSpan.Length; j++)
                {
                    string? formattedDefinitions = otherLookupResultsSpan[j].FormattedDefinitions;
                    Debug.Assert(formattedDefinitions is not null);
                    _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"<dt>{j + 1}.</dt> <dd>{formattedDefinitions.ReplaceLineEndings("<br/>")}</dd>");
                }

                _ = stringBuilder.Append(" </details>");
            }
        }

        string def = stringBuilder.ToString();
        ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
        return def;
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

            StringBuilder singleDictStringBuilder = ObjectPoolManager.StringBuilderPool.Get();
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

            string singleDictDef = singleDictStringBuilder.ToString();
            ObjectPoolManager.StringBuilderPool.Return(singleDictStringBuilder);
            return singleDictDef;
        }

        StringBuilder stringBuilder = ObjectPoolManager.StringBuilderPool.Get();
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
                    string? formattedDefinitions = otherLookupResultsSpan[j].FormattedDefinitions;
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

        string def = stringBuilder.ToString();
        ObjectPoolManager.StringBuilderPool.Return(stringBuilder);
        return def;
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

            StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
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

            string wordClasses = sb.ToString();
            ObjectPoolManager.StringBuilderPool.Return(sb);
            return wordClasses;
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

    private static StringBuilder GetExpressionWithPitchAccent(ReadOnlySpan<char> expression, StringBuilder expressionWithPitchAccentStringBuilder, byte position)
    {
        bool lowPitch = false;
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
            filePath = Path.Join(AppInfo.ResourcesPath, "mined_words.txt");
            jlFields = JLFieldUtils.JLFieldsForWordDicts;
        }
        else if (DictUtils.s_nameDictTypes.Contains(lookupResult.Dict.Type))
        {
            filePath = Path.Join(AppInfo.ResourcesPath, "mined_names.txt");
            jlFields = JLFieldUtils.JLFieldsForNameDicts;
        }
        else if (DictUtils.KanjiDictTypes.Contains(lookupResult.Dict.Type))
        {
            filePath = Path.Join(AppInfo.ResourcesPath, "mined_kanjis.txt");
            jlFields = JLFieldUtils.JLFieldsForKanjiDicts;
        }
        else
        {
            filePath = Path.Join(AppInfo.ResourcesPath, "mined_others.txt");
            jlFields = JLFieldUtils.JLFieldsForWordDicts;
        }

        string sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
        Dictionary<JLField, string> miningParameters = GetMiningParameters(lookupResults, currentLookupResultIndex, currentText, sentence, formattedDefinitions, selectedDefinitions, currentCharPosition, selectedSpelling, false, null);

        StringBuilder lineToMine = ObjectPoolManager.StringBuilderPool.Get();
        for (int i = 1; i < jlFields.Length; i++)
        {
            JLField jlField = jlFields[i];
            if (JLFieldUtils.s_jlFieldsToExcludeFromWhenMiningToFile.Contains(jlField))
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
        ObjectPoolManager.StringBuilderPool.Return(lineToMine);

        StatsUtils.IncrementStat(StatType.CardsMined);

        LoggerManager.Logger.Information("Mined {SelectedSpelling}", selectedSpelling);
        if (CoreConfigManager.Instance.NotifyWhenMiningSucceeds)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Success, $"Mined {selectedSpelling}");
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

            Dictionary<string, string> firstFieldDict = new(1, StringComparer.Ordinal)
            {
                { firstFieldName, firstFieldValue }
            };

            Note note = new(ankiConfig.DeckName, ankiConfig.ModelName, firstFieldDict, AnkiConnectUtils.CheckDuplicateOptions);
            notes.Add(note);
            positions.Add(i);
        }

        if (notes.Count is 0)
        {
            return null;
        }

        bool[]? canAddNoteList = await AnkiConnectUtils.CanAddNotes(notes).ConfigureAwait(false);
        if (canAddNoteList is null)
        {
            return null;
        }

        ReadOnlySpan<bool> canAddNoteSpan = canAddNoteList.AsReadOnlySpan();
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
            FrontendManager.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return;
        }

        Dictionary<MineType, AnkiConfig>? ankiConfigDict = await AnkiConfigUtils.ReadAnkiConfig().ConfigureAwait(false);
        if (ankiConfigDict is null)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
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
            FrontendManager.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return;
        }

        string sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
        Dictionary<JLField, string> miningParams = GetMiningParameters(lookupResults, currentLookupResultIndex, currentText, sentence, formattedDefinitions, selectedDefinitions, currentCharPosition, selectedSpelling, true, ankiConfig.UsedJLFields);
        OrderedDictionary<string, JLField> userFields = ankiConfig.Fields;
        Dictionary<string, string> fields = ConvertFields(userFields, miningParams);

        // Audio/Picture/Video shouldn't be set here
        // Otherwise AnkiConnect will place them under the "collection.media" folder even when it's a duplicate note
        Note note = new(ankiConfig.DeckName, ankiConfig.ModelName, fields, AnkiConnectUtils.CheckDuplicateOptions);
        bool? canAddNote = await AnkiConnectUtils.CanAddNote(note).ConfigureAwait(false);
        if (canAddNote is null)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, $"Mining failed for {selectedSpelling}");
            LoggerManager.Logger.Error("Mining failed for {SelectedSpelling}", selectedSpelling);
            return;
        }

        if (!coreConfigManager.AllowDuplicateCards && !canAddNote.Value)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, $"Cannot mine {selectedSpelling} because it is a duplicate card");
            LoggerManager.Logger.Information("Cannot mine {SelectedSpelling} because it is a duplicate card", selectedSpelling);
            return;
        }

        note.Tags = ankiConfig.Tags;
        note.Options = AnkiConnectUtils.AnkiOptions;

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
            audioData = FrontendManager.Frontend.GetAudioResponseFromTextToSpeech(selectedReading);
        }

        List<string> sentenceAudioFields = FindFields(JLField.SentenceAudio, userFields);
        bool needsSentenceAudio = sentenceAudioFields.Count > 0;
        bool sentenceAudioIsSameAsAudio = needsSentenceAudio && audioData is not null && sentence == lookupResult.PrimarySpelling;
        byte[]? sentenceAudioData = needsSentenceAudio
            ? sentenceAudioIsSameAsAudio
                ? audioData
                : FrontendManager.Frontend.GetAudioResponseFromTextToSpeech(sentence)
            : null;

        List<string> sourceTextAudioFields = FindFields(JLField.SourceTextAudio, userFields);
        bool needsSourceTextAudio = sourceTextAudioFields.Count > 0;
        bool sourceTextAudioIsSameAsSentenceAudio = needsSourceTextAudio && sentenceAudioData is not null && currentText == sentence;
        byte[]? sourceTextAudioData = needsSourceTextAudio
            ? sourceTextAudioIsSameAsSentenceAudio
                ? sentenceAudioData
                : FrontendManager.Frontend.GetAudioResponseFromTextToSpeech(currentText)
            : null;

        int totalAudioCount = 0;
        if (audioData is not null)
        {
            ++totalAudioCount;
        }
        if (sentenceAudioData is not null)
        {
            ++totalAudioCount;
        }
        if (sourceTextAudioData is not null)
        {
            ++totalAudioCount;
        }

        if (totalAudioCount > 0)
        {
            note.Audios = new Dictionary<string, object>[totalAudioCount];

            int audioIndex = 0;
            if (audioData is not null)
            {
                Debug.Assert(audioResponse is not null);
                note.Audios[audioIndex] = new Dictionary<string, object>(4, StringComparer.Ordinal)
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
                    };

                ++audioIndex;
            }

            string? sentenceAudioFormat = null;
            if (sentenceAudioData is not null)
            {
                Debug.Assert(!sentenceAudioIsSameAsAudio || audioResponse is not null);
                sentenceAudioFormat = sentenceAudioIsSameAsAudio
                    ? audioResponse!.AudioFormat
                    : AudioUtils.s_textToSpeechAudioResponse.AudioFormat;

                note.Audios[audioIndex] = new Dictionary<string, object>(4, StringComparer.Ordinal)
                    {
                        {
                            "data", sentenceAudioData
                        },
                        {
                            "filename", $"JL_sentence_audio_{selectedReading}_{lookupResult.PrimarySpelling}.{sentenceAudioFormat}"
                        },
                        {
                            "skipHash", NetworkUtils.Jpod101NoAudioMd5Hash
                        },
                        {
                            "fields", sentenceAudioFields
                        }
                    };

                ++audioIndex;
            }

            if (sourceTextAudioData is not null)
            {
                Debug.Assert(!sourceTextAudioIsSameAsSentenceAudio || sentenceAudioFormat is not null);
                string sourceTextAudioFormat = sourceTextAudioIsSameAsSentenceAudio
                    ? sentenceAudioFormat!
                    : AudioUtils.s_textToSpeechAudioResponse.AudioFormat;

                note.Audios[audioIndex] = new Dictionary<string, object>(4, StringComparer.Ordinal)
                    {
                        {
                            "data", sourceTextAudioData
                        },
                        {
                            "filename", $"JL_source_text_audio_{selectedReading}_{lookupResult.PrimarySpelling}.{sourceTextAudioFormat}"
                        },
                        {
                            "skipHash", NetworkUtils.Jpod101NoAudioMd5Hash
                        },
                        {
                            "fields", sourceTextAudioFields
                        }
                    };
            }
        }

        List<string> imageFields = FindFields(JLField.Image, userFields);
        bool needsImage = imageFields.Count > 0;
        byte[]? imageBytes = needsImage
            ? await FrontendManager.Frontend.GetImageFromClipboardAsByteArray().ConfigureAwait(false)
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

        Response? response = await AnkiConnectClient.AddNoteToDeck(note).ConfigureAwait(false);
        if (response is null)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, $"Mining failed for {selectedSpelling}");
            LoggerManager.Logger.Error("Mining failed for {SelectedSpelling}", selectedSpelling);
            return;
        }

        bool showNoAudioMessage = needsAudio && audioData is null;
        bool showDuplicateCardMessage = !canAddNote.Value;
        string message = $"Mined {selectedSpelling}{(showNoAudioMessage ? " (No Audio)" : "")}{(showDuplicateCardMessage ? " (Duplicate)" : "")}";

        LoggerManager.Logger.Information("{Message}", message);
        if (coreConfigManager.NotifyWhenMiningSucceeds)
        {
            FrontendManager.Frontend.Alert(showNoAudioMessage || showDuplicateCardMessage ? AlertLevel.Warning : AlertLevel.Success, message);
        }

        if (coreConfigManager.ForceSyncAnki)
        {
            await AnkiConnectClient.Sync().ConfigureAwait(false);
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
