using System.Text;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;

namespace JL.Core.Dicts.EDICT.JMdict;

public class JmdictRecord : IDictRecordWithGetFrequency
{
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

    public string PrimarySpelling { get; set; }
    //public List<string> PriorityList { get; set; } // e.g. gai1
    public List<List<string>?>? RelatedTerms { get; set; }
    public List<List<string>?>? Antonyms { get; set; }
    public List<List<LoanwordSource>?>? LoanwordEtymology { get; set; }

    public JmdictRecord()
    {
        PrimarySpelling = string.Empty;
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
        bool newlines = options is { NewlineBetweenDefinitions.Value: true };

        string separator = newlines ? "\n" : "";

        int count = 1;

        StringBuilder defResult = new();

        int definitionCount = Definitions.Count;

        for (int i = 0; i < definitionCount; i++)
        {
            if (newlines)
                defResult.Append($"({count}) ");

            if ((options?.WordClassInfo?.Value ?? true) && (WordClasses?[i]?.Any() ?? false))
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", WordClasses[i]!));
                defResult.Append(") ");
            }

            if (!newlines)
                defResult.Append($"({count}) ");

            if ((options?.DialectInfo?.Value ?? true) && (Dialects?[i]?.Any() ?? false))
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", Dialects[i]!));
                defResult.Append(") ");
            }

            if ((options?.ExtraDefinitionInfo?.Value ?? true)
                && (DefinitionInfo?.Any() ?? false)
                && DefinitionInfo[i] != null)
            {
                defResult.Append('(');
                defResult.Append(DefinitionInfo[i]);
                defResult.Append(") ");
            }

            if ((options?.MiscInfo?.Value ?? true) && (MiscList?[i]?.Any() ?? false))
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", MiscList[i]!));
                defResult.Append(") ");
            }

            if ((options?.WordTypeInfo?.Value ?? true) && (FieldList?[i]?.Any() ?? false))
            {
                defResult.Append('(');
                defResult.Append(string.Join(", ", FieldList[i]!));
                defResult.Append(") ");
            }

            defResult.Append(string.Join("; ", Definitions[i]) + " ");

            if ((options?.SpellingRestrictionInfo?.Value ?? true)
                && ((ReadingRestrictions?[i]?.Any() ?? false)
                    || (SpellingRestrictions?[i]?.Any() ?? false)))
            {
                defResult.Append("(only applies to ");

                if (SpellingRestrictions?[i]?.Any() ?? false)
                {
                    defResult.Append(string.Join("; ", SpellingRestrictions[i]!));
                }

                if (ReadingRestrictions?[i]?.Any() ?? false)
                {
                    if (SpellingRestrictions?[i]?.Any() ?? false)
                        defResult.Append("; ");

                    defResult.Append(string.Join("; ", ReadingRestrictions[i]!));
                }

                defResult.Append(") ");
            }

            if ((options?.LoanwordEtymology?.Value ?? true) && (LoanwordEtymology?[i]?.Any() ?? false))
            {
                defResult.Append('(');

                List<LoanwordSource> lSources = LoanwordEtymology[i]!;

                int lSourceCount = lSources.Count;
                for (int j = 0; j < lSourceCount; j++)
                {
                    if (lSources[j].IsWasei)
                        defResult.Append("Wasei ");

                    defResult.Append(lSources[j].Language);

                    if (lSources[j].OriginalWord != null)
                    {
                        defResult.Append(": ");
                        defResult.Append(lSources[j].OriginalWord);
                    }

                    if (j + 1 < lSourceCount)
                    {
                        defResult.Append(lSources[j].IsPart ? " + " : ", ");
                    }
                }

                defResult.Append(") ");
            }

            if ((options?.RelatedTerm?.Value ?? false) && (RelatedTerms?[i]?.Any() ?? false))
            {
                defResult.Append("(related terms: ");
                defResult.Append(string.Join(", ", RelatedTerms[i]!));
                defResult.Append(") ");
            }

            if ((options?.Antonym?.Value ?? false) && (Antonyms?[i]?.Any() ?? false))
            {
                defResult.Append("(antonyms: ");
                defResult.Append(string.Join(", ", Antonyms[i]!));
                defResult.Append(") ");
            }

            defResult.Append(separator);

            ++count;
        }

        return defResult.ToString().TrimEnd(' ', '\n');
    }

    public int GetFrequency(Freq freq)
    {
        int frequency = int.MaxValue;
        if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(PrimarySpelling),
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

            if (frequency == int.MaxValue && AlternativeSpellings != null)
            {
                int alternativeSpellingsCount = AlternativeSpellings.Count;
                for (int i = 0; i < alternativeSpellingsCount; i++)
                {
                    if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(AlternativeSpellings[i]),
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

        else if (Readings != null)
        {
            int readingCount = Readings.Count;
            for (int i = 0; i < readingCount; i++)
            {
                string reading = Readings[i];

                if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(reading),
                        out List<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultsCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultsCount; j++)
                    {
                        FrequencyRecord readingFreqResult = readingFreqResults[j];

                        if (reading == readingFreqResult.Spelling && Kana.IsKatakana(reading)
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
