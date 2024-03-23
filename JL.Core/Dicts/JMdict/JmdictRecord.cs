using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMdict;

internal sealed class JmdictRecord : IDictRecord, IGetFrequency
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
        string[]? primarySpellingOrthographyInfo,
        string[]? alternativeSpellings,
        string[]?[]? alternativeSpellingsOrthographyInfo,
        string[]? readings,
        string[]?[]? readingsOrthographyInfo,
        string[][] definitions,
        string[][] wordClasses,
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

    public string BuildFormattedDefinition(DictOptions? options)
    {
        bool newlines = options?.NewlineBetweenDefinitions?.Value ?? true;

        string separator = newlines ? "\n" : "";

        StringBuilder defResult = new();

        bool multipleDefinitions = Definitions.Length > 1;
        bool showWordClassInfo = options?.WordClassInfo?.Value ?? true;
        bool showDialectInfo = options?.DialectInfo?.Value ?? true;
        bool showExtraDefinitionInfo = options?.ExtraDefinitionInfo?.Value ?? true;
        bool definitionInfoExists = DefinitionInfo?.Length > 0;
        bool showMiscInfo = options?.MiscInfo?.Value ?? true;
        bool showWordTypeInfo = options?.WordTypeInfo?.Value ?? true;
        bool showSpellingRestrictionInfo = options?.SpellingRestrictionInfo?.Value ?? true;
        bool showLoanwordEtymology = options?.LoanwordEtymology?.Value ?? true;
        bool showRelatedTerms = options?.RelatedTerm?.Value ?? false;
        bool showAntonyms = options?.Antonym?.Value ?? false;

        for (int i = 0; i < Definitions.Length; i++)
        {
            if (newlines && multipleDefinitions)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            if (showWordClassInfo)
            {
                string[]? wordClasses = WordClasses?[i];
                if (wordClasses?.Length > 0)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({string.Join(", ", wordClasses)}) ");
                }
            }

            if (!newlines && multipleDefinitions)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            if (showMiscInfo)
            {
                string[]? misc = Misc?[i];
                if (misc?.Length > 0)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({string.Join(", ", misc)}) ");
                }
            }

            if (showDialectInfo)
            {
                string[]? dialects = Dialects?[i];
                if (dialects?.Length > 0)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({string.Join(", ", dialects)}) ");
                }
            }

            if (showWordTypeInfo)
            {
                string[]? fields = Fields?[i];
                if (fields?.Length > 0)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({string.Join(", ", fields)}) ");
                }
            }

            _ = defResult.Append(CultureInfo.InvariantCulture, $"{string.Join("; ", Definitions[i])} ");

            if (showExtraDefinitionInfo && definitionInfoExists)
            {
                string? definitionInfo = DefinitionInfo![i];
                if (definitionInfo is not null)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({definitionInfo}) ");
                }
            }

            if (showSpellingRestrictionInfo)
            {
                string[]? spellingRestrictions = SpellingRestrictions?[i];
                string[]? readingRestrictions = ReadingRestrictions?[i];

                if (spellingRestrictions?.Length > 0 || readingRestrictions?.Length > 0)
                {
                    _ = defResult.Append("(only applies to ");

                    if (spellingRestrictions?.Length > 0)
                    {
                        _ = defResult.Append(string.Join("; ", spellingRestrictions));
                    }

                    if (readingRestrictions?.Length > 0)
                    {
                        if (spellingRestrictions?.Length > 0)
                        {
                            _ = defResult.Append("; ");
                        }

                        _ = defResult.Append(string.Join("; ", readingRestrictions));
                    }

                    _ = defResult.Append(") ");
                }
            }

            if (showLoanwordEtymology)
            {
                LoanwordSource[]? lSources = LoanwordEtymology?[i];
                if (lSources?.Length > 0)
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

                        if ((j + 1) < lSources.Length)
                        {
                            _ = defResult.Append(lSource.IsPart ? " + " : ", ");
                        }
                    }

                    _ = defResult.Append(") ");
                }
            }

            if (showRelatedTerms)
            {
                string[]? relatedTerms = RelatedTerms?[i];
                if (relatedTerms?.Length > 0)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"(related terms: {string.Join(", ", relatedTerms)}) ");
                }
            }

            if (showAntonyms)
            {
                string[]? antonyms = Antonyms?[i];
                if (antonyms?.Length > 0)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"(antonyms: {string.Join(", ", antonyms)}) ");
                }
            }

            _ = defResult.Append(separator);
        }

        return defResult.Remove(defResult.Length - separator.Length - 1, separator.Length + 1).ToString();
    }

    public int GetFrequency(Freq freq)
    {
        int frequency = int.MaxValue;
        if (freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling),
                out IList<FrequencyRecord>? freqResults))
        {
            int freqResultCount = freqResults.Count;
            for (int i = 0; i < freqResultCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];
                if (PrimarySpelling == freqResult.Spelling || (Readings?.Contains(freqResult.Spelling) ?? false))
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }

            if (frequency is int.MaxValue && AlternativeSpellings is not null)
            {
                for (int i = 0; i < AlternativeSpellings.Length; i++)
                {
                    if (freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(AlternativeSpellings[i]),
                            out IList<FrequencyRecord>? alternativeSpellingFreqResults))
                    {
                        int alternativeSpellingFreqResultCount = alternativeSpellingFreqResults.Count;
                        for (int j = 0; j < alternativeSpellingFreqResultCount; j++)
                        {
                            FrequencyRecord alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];
                            if (Readings?.Contains(alternativeSpellingFreqResult.Spelling) ?? false)
                            {
                                if (frequency > alternativeSpellingFreqResult.Frequency)
                                {
                                    frequency = alternativeSpellingFreqResult.Frequency;
                                }
                            }
                        }
                    }
                }
            }
        }

        else if (Readings is not null)
        {
            for (int i = 0; i < Readings.Length; i++)
            {
                string reading = Readings[i];

                if (freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading),
                        out IList<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultCount; j++)
                    {
                        FrequencyRecord readingFreqResult = readingFreqResults[j];
                        if ((reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(reading))
                            || (AlternativeSpellings?.Contains(readingFreqResult.Spelling) ?? false))
                        {
                            if (frequency > readingFreqResult.Frequency)
                            {
                                frequency = readingFreqResult.Frequency;
                            }
                        }
                    }
                }
            }
        }

        return frequency;
    }

    public int GetFrequencyFromDB(Dictionary<string, List<FrequencyRecord>> freqDict)
    {
        int frequency = int.MaxValue;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out List<FrequencyRecord>? freqResults))
        {
            int freqResultCount = freqResults.Count;
            for (int i = 0; i < freqResultCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];

                if (PrimarySpelling == freqResult.Spelling || (Readings?.Contains(freqResult.Spelling) ?? false))
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }

            if (frequency is int.MaxValue && AlternativeSpellings is not null)
            {
                for (int i = 0; i < AlternativeSpellings.Length; i++)
                {
                    if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(AlternativeSpellings[i]), out List<FrequencyRecord>? alternativeSpellingFreqResults))
                    {
                        int alternativeSpellingFreqResultCount = alternativeSpellingFreqResults.Count;
                        for (int j = 0; j < alternativeSpellingFreqResultCount; j++)
                        {
                            FrequencyRecord alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];
                            if (Readings?.Contains(alternativeSpellingFreqResult.Spelling) ?? false)
                            {
                                if (frequency > alternativeSpellingFreqResult.Frequency)
                                {
                                    frequency = alternativeSpellingFreqResult.Frequency;
                                }
                            }
                        }
                    }
                }
            }
        }

        else if (Readings is not null)
        {
            for (int i = 0; i < Readings.Length; i++)
            {
                string reading = Readings[i];
                if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out List<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultCount; j++)
                    {
                        FrequencyRecord readingFreqResult = readingFreqResults[j];
                        if ((reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(reading))
                            || (AlternativeSpellings?.Contains(readingFreqResult.Spelling) ?? false))
                        {
                            if (frequency > readingFreqResult.Frequency)
                            {
                                frequency = readingFreqResult.Frequency;
                            }
                        }
                    }
                }
            }
        }

        return frequency;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        JmdictRecord jmdictObj = (JmdictRecord)obj;
        return Id == jmdictObj.Id && PrimarySpelling == jmdictObj.PrimarySpelling;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Id * PrimarySpelling.GetHashCode(StringComparison.Ordinal);
        }
    }
}
