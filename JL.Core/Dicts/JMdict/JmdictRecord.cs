using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMdict;

internal sealed class JmdictRecord : IDictRecordWithMultipleReadings, IGetFrequency, IEquatable<JmdictRecord>
{
    public int Id { get; }
    public string PrimarySpelling { get; }
    public string[]? PrimarySpellingOrthographyInfo { get; }
    public string[]? AlternativeSpellings { get; }
    public string[]?[]? AlternativeSpellingsOrthographyInfo { get; }
    public string[]? Readings { get; }
    public string[]?[]? ReadingsOrthographyInfo { get; }
    public string[][] Definitions { get; }
    public string[]?[]? WordClasses { get; } //e.g. noun +
    public string[]? WordClassesSharedByAllSenses { get; }
    public string[]?[]? SpellingRestrictions { get; }
    public string[]?[]? ReadingRestrictions { get; }
    public string[]?[]? Fields { get; } // e.g. "martial arts"
    public string[]? FieldsSharedByAllSenses { get; }
    public string[]?[]? Misc { get; } // e.g. "abbr" +
    public string[]? MiscSharedByAllSenses { get; }
    public string?[]? DefinitionInfo { get; } // e.g. "often derog" +
    public string[]?[]? Dialects { get; } // e.g. ksb
    public string[]? DialectsSharedByAllSenses { get; }
    public LoanwordSource[]?[]? LoanwordEtymology { get; }
    public string[]?[]? RelatedTerms { get; }
    public string[]?[]? Antonyms { get; }
    //public string[] Priorities { get; } // e.g. gai1

    public JmdictRecord(int id,
        string primarySpelling,
        string[][] definitions,
        string[]?[]? wordClasses,
        string[]? wordClassesSharedByAllSenses,
        string[]? primarySpellingOrthographyInfo,
        string[]? alternativeSpellings,
        string[]?[]? alternativeSpellingsOrthographyInfo,
        string[]? readings,
        string[]?[]? readingsOrthographyInfo,
        string[]?[]? spellingRestrictions,
        string[]?[]? readingRestrictions,
        string[]?[]? fields,
        string[]? fieldsSharedByAllSenses,
        string[]?[]? misc,
        string[]? miscSharedByAllSenses,
        string?[]? definitionInfo,
        string[]?[]? dialects,
        string[]? dialectsSharedByAllSenses,
        LoanwordSource[]?[]? loanwordEtymology,
        string[]?[]? relatedTerms,
        string[]?[]? antonyms)
    {
        Id = id;
        PrimarySpelling = primarySpelling;
        PrimarySpellingOrthographyInfo = primarySpellingOrthographyInfo;
        AlternativeSpellings = alternativeSpellings;
        AlternativeSpellingsOrthographyInfo = alternativeSpellingsOrthographyInfo;
        Readings = readings;
        ReadingsOrthographyInfo = readingsOrthographyInfo;
        Definitions = definitions;
        WordClasses = wordClasses;
        WordClassesSharedByAllSenses = wordClassesSharedByAllSenses;
        SpellingRestrictions = spellingRestrictions;
        ReadingRestrictions = readingRestrictions;
        Fields = fields;
        FieldsSharedByAllSenses = fieldsSharedByAllSenses;
        Misc = misc;
        MiscSharedByAllSenses = miscSharedByAllSenses;
        DefinitionInfo = definitionInfo;
        Dialects = dialects;
        DialectsSharedByAllSenses = dialectsSharedByAllSenses;
        LoanwordEtymology = loanwordEtymology;
        RelatedTerms = relatedTerms;
        Antonyms = antonyms;
    }

    public string BuildFormattedDefinition(DictOptions options)
    {
        bool newlines = options.NewlineBetweenDefinitions!.Value;
        char separator = newlines ? '\n' : '；';

        bool multipleDefinitions = Definitions.Length > 1;

        bool showWordClassInfoOptionValue = options.WordClassInfo!.Value;
        bool showWordClassInfo = showWordClassInfoOptionValue && WordClasses is not null;
        bool showWordClassesSharedByAllSenses = showWordClassInfoOptionValue && WordClassesSharedByAllSenses is not null;

        bool showMiscInfoOptionValue = options.MiscInfo!.Value;
        bool showMiscInfo = showMiscInfoOptionValue && Misc is not null;
        bool showMiscSharedByAllSenses = showMiscInfoOptionValue && MiscSharedByAllSenses is not null;

        bool showDialectInfoOptionValue = options.DialectInfo!.Value;
        bool showDialectInfo = showDialectInfoOptionValue && Dialects is not null;
        bool showDialectsSharedByAllSenses = showDialectInfoOptionValue && DialectsSharedByAllSenses is not null;

        bool showFieldInfoOptionValue = options.WordTypeInfo!.Value;
        bool showFieldsInfo = showFieldInfoOptionValue && Fields is not null;
        bool showFieldsSharedByAllSenses = showFieldInfoOptionValue && FieldsSharedByAllSenses is not null;

        bool showExtraDefinitionInfo = options.ExtraDefinitionInfo!.Value && DefinitionInfo is not null;

        bool showSpellingRestrictionInfo = options.SpellingRestrictionInfo!.Value;
        bool showSpellingRestrictions = showSpellingRestrictionInfo && SpellingRestrictions is not null;
        bool showReadingRestrictionss = showSpellingRestrictionInfo && ReadingRestrictions is not null;

        bool showLoanwordEtymology = options.LoanwordEtymology!.Value && LoanwordEtymology is not null;
        bool showRelatedTerms = options.RelatedTerm!.Value && RelatedTerms is not null;
        bool showAntonyms = options.Antonym!.Value && Antonyms is not null;

        StringBuilder defResult = new();
        if (showWordClassesSharedByAllSenses || showMiscSharedByAllSenses || showDialectsSharedByAllSenses || showFieldsSharedByAllSenses)
        {
            if (showWordClassesSharedByAllSenses)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"[{string.Join(", ", WordClassesSharedByAllSenses!)}] ");
            }

            if (showMiscSharedByAllSenses)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"[{string.Join(", ", MiscSharedByAllSenses!)}] ");
            }

            if (showDialectsSharedByAllSenses)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"[{string.Join(", ", DialectsSharedByAllSenses!)}] ");
            }

            if (showFieldsSharedByAllSenses)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"[{string.Join(", ", FieldsSharedByAllSenses!)}] ");
            }

            if (multipleDefinitions && newlines)
            {
                _ = defResult.Replace(" ", "\n", defResult.Length - 1, 1);
            }
        }

        for (int i = 0; i < Definitions.Length; i++)
        {
            if (multipleDefinitions)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"{i + 1}. ");

                if (showWordClassInfo)
                {
                    string[]? wordClasses = WordClasses![i];
                    if (wordClasses is not null)
                    {
                        _ = defResult.Append(CultureInfo.InvariantCulture, $"[{string.Join(", ", wordClasses)}] ");
                    }
                }

                if (showMiscInfo)
                {
                    string[]? misc = Misc![i];
                    if (misc is not null)
                    {
                        _ = defResult.Append(CultureInfo.InvariantCulture, $"[{string.Join(", ", misc)}] ");
                    }
                }

                if (showDialectInfo)
                {
                    string[]? dialects = Dialects![i];
                    if (dialects is not null)
                    {
                        _ = defResult.Append(CultureInfo.InvariantCulture, $"[{string.Join(", ", dialects)}] ");
                    }
                }

                if (showFieldsInfo)
                {
                    string[]? fields = Fields![i];
                    if (fields is not null)
                    {
                        _ = defResult.Append(CultureInfo.InvariantCulture, $"[{string.Join(", ", fields)}] ");
                    }
                }
            }

            _ = defResult.Append(CultureInfo.InvariantCulture, $"{string.Join("; ", Definitions[i])} ");

            if (showExtraDefinitionInfo)
            {
                string? definitionInfo = DefinitionInfo![i];
                if (definitionInfo is not null)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({definitionInfo}) ");
                }
            }

            if (showSpellingRestrictionInfo)
            {
                string[]? spellingRestrictions = showSpellingRestrictions ? SpellingRestrictions![i] : null;
                string[]? readingRestrictions = showReadingRestrictionss ? ReadingRestrictions![i] : null;

                bool spellingRestrictionsExist = spellingRestrictions is not null;
                bool readingRestrictionsExist = readingRestrictions is not null;
                if (spellingRestrictionsExist || readingRestrictionsExist)
                {
                    _ = defResult.Append("(only applies to ");

                    if (spellingRestrictionsExist)
                    {
                        _ = defResult.Append(string.Join("; ", spellingRestrictions!));
                    }

                    if (readingRestrictionsExist)
                    {
                        if (spellingRestrictionsExist)
                        {
                            _ = defResult.Append("; ");
                        }

                        _ = defResult.Append(string.Join("; ", readingRestrictions!));
                    }

                    _ = defResult.Append(") ");
                }
            }

            if (showLoanwordEtymology)
            {
                ref readonly LoanwordSource[]? lSources = ref LoanwordEtymology![i];
                if (lSources is not null)
                {
                    _ = defResult.Append('(');

                    for (int j = 0; j < lSources.Length; j++)
                    {
                        ref readonly LoanwordSource lSource = ref lSources[j];
                        if (lSource.IsWasei)
                        {
                            _ = defResult.Append("wasei ");
                        }
                        else if (j is 0)
                        {
                            _ = defResult.Append("from ");
                        }

                        _ = defResult.Append(lSource.Language);

                        if (lSource.OriginalWord is not null)
                        {
                            _ = defResult.Append(CultureInfo.InvariantCulture, $": {lSource.OriginalWord}");
                        }

                        if (j + 1 < lSources.Length)
                        {
                            _ = defResult.Append(lSource.IsPart ? " + " : ", ");
                        }
                    }

                    _ = defResult.Append(") ");
                }
            }

            if (showRelatedTerms)
            {
                string[]? relatedTerms = RelatedTerms![i];
                if (relatedTerms is not null)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"(related terms: {string.Join(", ", relatedTerms)}) ");
                }
            }

            if (showAntonyms)
            {
                string[]? antonyms = Antonyms![i];
                if (antonyms is not null)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"(antonyms: {string.Join(", ", antonyms)}) ");
                }
            }

            if (i + 1 != Definitions.Length)
            {
                _ = defResult.Replace(' ', separator, defResult.Length - 1, 1);
            }
        }

        return defResult.ToString(0, defResult.Length - 1);
    }

    public int GetFrequency(IDictionary<string, IList<FrequencyRecord>> freqDict)
    {
        bool readingsExist = Readings is not null;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out IList<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];
                if (freqResult.Spelling == PrimarySpelling
                    || (readingsExist && Readings!.Contains(freqResult.Spelling)))
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (readingsExist)
        {
            for (int i = 0; i < Readings!.Length; i++)
            {
                string reading = Readings[i];
                if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out IList<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultsCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultsCount; j++)
                    {
                        FrequencyRecord readingFreqResult = readingFreqResults[j];
                        if (readingFreqResult.Spelling == PrimarySpelling
                            || (reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(reading[0])))
                        {
                            return readingFreqResult.Frequency;
                        }
                    }
                }
            }
        }

        return int.MaxValue;
    }

    public int GetFrequency(Dictionary<string, List<FrequencyRecord>> freqDict)
    {
        bool readingsExist = Readings is not null;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out List<FrequencyRecord>? freqResults))
        {
            foreach (ref readonly FrequencyRecord freqResult in CollectionsMarshal.AsSpan(freqResults))
            {
                if (freqResult.Spelling == PrimarySpelling
                    || (readingsExist && Readings!.Contains(freqResult.Spelling)))
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (readingsExist)
        {
            for (int i = 0; i < Readings!.Length; i++)
            {
                string reading = Readings[i];
                if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out List<FrequencyRecord>? readingFreqResults))
                {
                    foreach (ref readonly FrequencyRecord readingFreqResult in CollectionsMarshal.AsSpan(readingFreqResults))
                    {
                        if (readingFreqResult.Spelling == PrimarySpelling
                            || (reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(reading[0])))
                        {
                            return readingFreqResult.Frequency;
                        }
                    }
                }
            }
        }

        return int.MaxValue;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, PrimarySpelling.GetHashCode(StringComparison.Ordinal));
    }

    public override bool Equals(object? obj)
    {
        return obj is JmdictRecord jmdictRecord
               && Id == jmdictRecord.Id
               && PrimarySpelling == jmdictRecord.PrimarySpelling;
    }

    public bool Equals(JmdictRecord? other)
    {
        return other is not null
            && Id == other.Id
            && PrimarySpelling == other.PrimarySpelling;
    }

    public static bool operator ==(JmdictRecord? left, JmdictRecord? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(JmdictRecord? left, JmdictRecord? right) => !left?.Equals(right) ?? right is not null;
}
