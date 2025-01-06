using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMdict;

internal sealed class JmdictRecord : IDictRecord, IGetFrequency, IEquatable<JmdictRecord>
{
    public int Id { get; }
    public string PrimarySpelling { get; }
    public string[]? PrimarySpellingOrthographyInfo { get; }
    public string[]? AlternativeSpellings { get; }
    public string[]?[]? AlternativeSpellingsOrthographyInfo { get; }
    public string[]? Readings { get; }
    public string[]?[]? ReadingsOrthographyInfo { get; }
    public string[][] Definitions { get; }
    public string[][] WordClasses { get; } //e.g. noun +
    public string[]?[]? SpellingRestrictions { get; }
    public string[]?[]? ReadingRestrictions { get; }
    public string[]?[]? Fields { get; } // e.g. "martial arts"
    public string[]?[]? Misc { get; } // e.g. "abbr" +
    public string?[]? DefinitionInfo { get; } // e.g. "often derog" +
    public string[]?[]? Dialects { get; } // e.g. ksb
    public LoanwordSource[]?[]? LoanwordEtymology { get; }
    public string[]?[]? RelatedTerms { get; }
    public string[]?[]? Antonyms { get; }
    //public string[] Priorities { get; } // e.g. gai1

    public JmdictRecord(int id,
        string primarySpelling,
        string[][] definitions,
        string[][] wordClasses,
        string[]? primarySpellingOrthographyInfo,
        string[]? alternativeSpellings,
        string[]?[]? alternativeSpellingsOrthographyInfo,
        string[]? readings,
        string[]?[]? readingsOrthographyInfo,
        string[]?[]? spellingRestrictions,
        string[]?[]? readingRestrictions,
        string[]?[]? fields,
        string[]?[]? misc,
        string?[]? definitionInfo,
        string[]?[]? dialects,
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
        SpellingRestrictions = spellingRestrictions;
        ReadingRestrictions = readingRestrictions;
        Fields = fields;
        Misc = misc;
        DefinitionInfo = definitionInfo;
        Dialects = dialects;
        LoanwordEtymology = loanwordEtymology;
        RelatedTerms = relatedTerms;
        Antonyms = antonyms;
    }

    public string BuildFormattedDefinition(DictOptions options)
    {
        bool newlines = options.NewlineBetweenDefinitions!.Value;
        char separator = newlines ? '\n' : '；';

        bool multipleDefinitions = Definitions.Length > 1;
        bool showWordClassInfo = options.WordClassInfo!.Value;
        bool showDialectInfo = options.DialectInfo!.Value && Dialects is not null;
        bool showExtraDefinitionInfo = options.ExtraDefinitionInfo!.Value && DefinitionInfo is not null;
        bool showMiscInfo = options.MiscInfo!.Value && Misc is not null;
        bool showWordTypeInfo = options.WordTypeInfo!.Value && Fields is not null;
        bool showSpellingRestrictionInfo = options.SpellingRestrictionInfo!.Value;
        bool showSpellingRestrictions = showSpellingRestrictionInfo && SpellingRestrictions is not null;
        bool showReadingRestrictionss = showSpellingRestrictionInfo && ReadingRestrictions is not null;
        bool showLoanwordEtymology = options.LoanwordEtymology!.Value && LoanwordEtymology is not null;
        bool showRelatedTerms = options.RelatedTerm!.Value && RelatedTerms is not null;
        bool showAntonyms = options.Antonym!.Value && Antonyms is not null;

        StringBuilder defResult = new();
        for (int i = 0; i < Definitions.Length; i++)
        {
            if (newlines && multipleDefinitions)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            if (showWordClassInfo)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({string.Join(", ", WordClasses[i])}) ");
            }

            if (!newlines && multipleDefinitions)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            if (showMiscInfo)
            {
                string[]? misc = Misc![i];
                if (misc is not null)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({string.Join(", ", misc)}) ");
                }
            }

            if (showDialectInfo)
            {
                string[]? dialects = Dialects![i];
                if (dialects is not null)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({string.Join(", ", dialects)}) ");
                }
            }

            if (showWordTypeInfo)
            {
                string[]? fields = Fields![i];
                if (fields is not null)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({string.Join(", ", fields)}) ");
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
                LoanwordSource[]? lSources = LoanwordEtymology![i];
                if (lSources is not null)
                {
                    _ = defResult.Append('(');

                    for (int j = 0; j < lSources.Length; j++)
                    {
                        LoanwordSource lSource = lSources[j];
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
        int frequency = int.MaxValue;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out IList<FrequencyRecord>? freqResults))
        {
            int freqResultCount = freqResults.Count;
            for (int i = 0; i < freqResultCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];
                if (PrimarySpelling == freqResult.Spelling
                    || (readingsExist && Readings!.Contains(freqResult.Spelling)))
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (readingsExist)
        {
            bool alternativeSpellingsExist = AlternativeSpellings is not null;
            for (int i = 0; i < Readings!.Length; i++)
            {
                string reading = Readings[i];
                if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out IList<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultCount; j++)
                    {
                        FrequencyRecord readingFreqResult = readingFreqResults[j];
                        if ((reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(reading[0]))
                            || (alternativeSpellingsExist && AlternativeSpellings!.Contains(readingFreqResult.Spelling)))
                        {
                            return readingFreqResult.Frequency;
                        }
                    }
                }
            }
        }

        return frequency;
    }

    public int GetFrequencyFromDB(Dictionary<string, List<FrequencyRecord>> freqDict)
    {
        bool readingsExist = Readings is not null;
        int frequency = int.MaxValue;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out List<FrequencyRecord>? freqResults))
        {
            int freqResultCount = freqResults.Count;
            for (int i = 0; i < freqResultCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];
                if (PrimarySpelling == freqResult.Spelling
                    || (readingsExist && Readings!.Contains(freqResult.Spelling)))
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (readingsExist)
        {
            bool alternativeSpellingsExist = AlternativeSpellings is not null;
            for (int i = 0; i < Readings!.Length; i++)
            {
                string reading = Readings[i];
                if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out List<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultCount; j++)
                    {
                        FrequencyRecord readingFreqResult = readingFreqResults[j];
                        if ((reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(reading[0]))
                            || (alternativeSpellingsExist && AlternativeSpellings!.Contains(readingFreqResult.Spelling)))
                        {
                            return readingFreqResult.Frequency;
                        }
                    }
                }
            }
        }

        return frequency;
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

    public override int GetHashCode()
    {
        unchecked
        {
            return Id * PrimarySpelling.GetHashCode(StringComparison.Ordinal);
        }
    }
}
