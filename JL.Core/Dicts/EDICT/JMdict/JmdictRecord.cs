using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EDICT.JMdict;

internal sealed class JmdictRecord : IDictRecord, IGetFrequency
{
    public string PrimarySpelling { get; }
    public int Id { get; set; }
    public List<string>? AlternativeSpellings { get; set; }
    public List<List<string>> Definitions { get; set; }
    public List<List<string>?>? ReadingRestrictions { get; set; }
    public List<List<string>?>? SpellingRestrictions { get; set; }
    public List<string>? Readings { get; set; }
    public List<string>? PrimarySpellingOrthographyInfoList { get; set; }
    public List<List<string>?>? AlternativeSpellingsOrthographyInfoList { get; set; }
    public List<List<string>?>? ReadingsOrthographyInfoList { get; set; }
    public List<List<string>?>? WordClasses { get; set; } //e.g. noun +
    public List<List<string>?>? FieldList { get; set; } // e.g. "martial arts"
    public List<List<string>?>? MiscList { get; set; } // e.g. "abbr" +
    public List<string?>? DefinitionInfo { get; set; } // e.g. "often derog" +
    public List<List<string>?>? Dialects { get; set; } // e.g. ksb
    //public List<string> PriorityList { get; set; } // e.g. gai1
    public List<List<string>?>? RelatedTerms { get; set; }
    public List<List<string>?>? Antonyms { get; set; }
    public List<List<LoanwordSource>?>? LoanwordEtymology { get; set; }

    public JmdictRecord(string primarySpelling)
    {
        PrimarySpelling = primarySpelling;
        Definitions = new List<List<string>>();
        ReadingRestrictions = new List<List<string>?>();
        SpellingRestrictions = new List<List<string>?>();
        Readings = new List<string>();
        AlternativeSpellings = new List<string>();
        PrimarySpellingOrthographyInfoList = new List<string>();
        AlternativeSpellingsOrthographyInfoList = new List<List<string>?>();
        ReadingsOrthographyInfoList = new List<List<string>?>();
        WordClasses = new List<List<string>?>();
        FieldList = new List<List<string>?>();
        MiscList = new List<List<string>?>();
        DefinitionInfo = new List<string?>();
        Dialects = new List<List<string>?>();
        //PriorityList = new List<string>();
        RelatedTerms = new List<List<string>?>();
        Antonyms = new List<List<string>?>();
        LoanwordEtymology = new List<List<LoanwordSource>?>();
    }

    public string BuildFormattedDefinition(DictOptions? options)
    {
        bool newlines = options?.NewlineBetweenDefinitions?.Value ?? true;

        string separator = newlines ? "\n" : "";

        StringBuilder defResult = new();

        bool multipleDefinitions = Definitions.Count > 1;

        for (int i = 0; i < Definitions.Count; i++)
        {
            if (newlines && multipleDefinitions)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            if (options?.WordClassInfo?.Value ?? true)
            {
                List<string>? wordClasses = WordClasses?[i];
                if (wordClasses?.Count > 0)
                {
                    _ = defResult.Append('(')
                        .Append(string.Join(", ", wordClasses))
                        .Append(") ");
                }
            }

            if (!newlines && multipleDefinitions)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            if (options?.DialectInfo?.Value ?? true)
            {
                List<string>? dialects = Dialects?[i];
                if (dialects?.Count > 0)
                {
                    _ = defResult.Append('(')
                        .Append(string.Join(", ", dialects))
                        .Append(") ");
                }
            }

            if ((options?.ExtraDefinitionInfo?.Value ?? true)
                && DefinitionInfo?.Count > 0
                && DefinitionInfo[i] is not null)
            {
                _ = defResult.Append('(')
                    .Append(DefinitionInfo[i])
                    .Append(") ");
            }

            if (options?.MiscInfo?.Value ?? true)
            {
                List<string>? misc = MiscList?[i];
                if (misc?.Count > 0)
                {
                    _ = defResult.Append('(')
                        .Append(string.Join(", ", misc))
                        .Append(") ");
                }
            }

            if (options?.WordTypeInfo?.Value ?? true)
            {
                List<string>? fields = FieldList?[i];
                if (fields?.Count > 0)
                {
                    _ = defResult.Append('(')
                    .Append(string.Join(", ", fields))
                    .Append(") ");
                }
            }

            _ = defResult.Append(string.Join("; ", Definitions[i]) + " ");

            if (options?.SpellingRestrictionInfo?.Value ?? true)
            {
                List<string>? spellingRestrictions = SpellingRestrictions?[i];
                List<string>? readingRestrictions = ReadingRestrictions?[i];

                if (spellingRestrictions?.Count > 0 || readingRestrictions?.Count > 0)
                {
                    _ = defResult.Append("(only applies to ");

                    if (spellingRestrictions?.Count > 0)
                    {
                        _ = defResult.Append(string.Join("; ", spellingRestrictions));
                    }

                    if (readingRestrictions?.Count > 0)
                    {
                        if (spellingRestrictions?.Count > 0)
                        {
                            _ = defResult.Append("; ");
                        }

                        _ = defResult.Append(string.Join("; ", readingRestrictions));
                    }

                    _ = defResult.Append(") ");
                }
            }

            if (options?.LoanwordEtymology?.Value ?? true)
            {
                List<LoanwordSource>? lSources = LoanwordEtymology?[i];
                if (lSources?.Count > 0)
                {
                    _ = defResult.Append('(');

                    int lSourceCount = lSources.Count;
                    for (int j = 0; j < lSourceCount; j++)
                    {
                        if (lSources[j].IsWasei)
                        {
                            _ = defResult.Append("Wasei ");
                        }

                        _ = defResult.Append(lSources[j].Language);

                        if (lSources[j].OriginalWord is not null)
                        {
                            _ = defResult.Append(": ").Append(lSources[j].OriginalWord);
                        }

                        if (j + 1 < lSourceCount)
                        {
                            _ = defResult.Append(lSources[j].IsPart ? " + " : ", ");
                        }
                    }

                    _ = defResult.Append(") ");
                }
            }

            if (options?.RelatedTerm?.Value ?? false)
            {
                List<string>? relatedTerms = RelatedTerms?[i];
                if (relatedTerms?.Count > 0)
                {
                    _ = defResult.Append("(related terms: ")
                        .Append(string.Join(", ", relatedTerms))
                        .Append(") ");
                }
            }

            if (options?.Antonym?.Value ?? false)
            {
                List<string>? antonyms = Antonyms?[i];
                if (antonyms?.Count > 0)
                {
                    _ = defResult.Append("(antonyms: ")
                        .Append(string.Join(", ", antonyms))
                        .Append(") ");
                }
            }

            _ = defResult.Append(separator);
        }

        return defResult.ToString().TrimEnd(' ', '\n');
    }

    public int GetFrequency(Freq freq)
    {
        int frequency = int.MaxValue;
        if (freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling),
                out List<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
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
                int alternativeSpellingsCount = AlternativeSpellings.Count;
                for (int i = 0; i < alternativeSpellingsCount; i++)
                {
                    if (freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(AlternativeSpellings[i]),
                            out List<FrequencyRecord>? alternativeSpellingFreqResults))
                    {
                        int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                        for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
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
            int readingCount = Readings.Count;
            for (int i = 0; i < readingCount; i++)
            {
                string reading = Readings[i];

                if (freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading),
                        out List<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultsCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultsCount; j++)
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
}
