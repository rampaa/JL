using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;

namespace JL.Core.Dicts.CustomWordDict;

public class CustomWordRecord : IDictRecordWithGetFrequency
{
    public string PrimarySpelling { get; }
    public List<string>? AlternativeSpellings { get; }
    public List<string>? Readings { get; }
    private List<string> Definitions { get; }
    public List<string> WordClasses { get; }

    public CustomWordRecord(string primarySpelling, List<string>? alternativeSpellings, List<string>? readings,
        List<string> definitions, List<string> wordClasses)
    {
        PrimarySpelling = primarySpelling;
        AlternativeSpellings = alternativeSpellings;
        Readings = readings;
        Definitions = definitions;
        WordClasses = wordClasses;
    }

    public string BuildFormattedDefinition(DictOptions? options)
    {
        string separator = options is { NewlineBetweenDefinitions.Value: true }
            ? "\n"
            : "";

        int count = 1;
        StringBuilder defResult = new();

        if (WordClasses.Any())
        {
            string tempWordClass;
            if (WordClasses.Contains("adj-i"))
            {
                tempWordClass = "adjective";
            }
            else if (WordClasses.Contains("v1"))
            {
                tempWordClass = "verb";
            }
            else if (WordClasses.Contains("noun"))
            {
                tempWordClass = "noun";
            }
            else
            {
                tempWordClass = "other";
            }

            _ = defResult.Append(CultureInfo.InvariantCulture, $"({tempWordClass}) ");
        }

        for (int i = 0; i < Definitions.Count; i++)
        {
            if (Definitions.Any())
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({count}) ")
                    .Append(string.Join("; ", Definitions[i]) + " ")
                    .Append(separator);

                ++count;
            }
        }

        return defResult.ToString().TrimEnd(' ', '\n');
    }

    public int GetFrequency(Freq freq)
    {
        int frequency = int.MaxValue;

        if (freq.Contents.TryGetValue(Kana.KatakanaToHiragana(PrimarySpelling),
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

            if (frequency == int.MaxValue && AlternativeSpellings is not null)
            {
                int alternativeSpellingsCount = AlternativeSpellings.Count;
                for (int i = 0; i < alternativeSpellingsCount; i++)
                {
                    if (freq.Contents.TryGetValue(
                            Kana.KatakanaToHiragana(AlternativeSpellings[i]),
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

                if (freq.Contents.TryGetValue(Kana.KatakanaToHiragana(reading),
                        out List<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultsCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultsCount; j++)
                    {
                        FrequencyRecord readingFreqResult = readingFreqResults[j];

                        if ((reading == readingFreqResult.Spelling && Kana.IsKatakana(reading))
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
        if (obj == null)
        {
            return false;
        }

        CustomWordRecord customWordRecordObj = (obj as CustomWordRecord)!;

        return PrimarySpelling == customWordRecordObj.PrimarySpelling
               && ((customWordRecordObj.AlternativeSpellings?.SequenceEqual(AlternativeSpellings ?? new())) ?? AlternativeSpellings == null)
               && ((customWordRecordObj.Readings?.SequenceEqual(Readings ?? new())) ?? Readings == null)
               && customWordRecordObj.Definitions.SequenceEqual(Definitions)
               && customWordRecordObj.WordClasses.SequenceEqual(WordClasses);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            hash = (hash * 37) + PrimarySpelling.GetHashCode();

            if (AlternativeSpellings is not null)
            {
                foreach (string spelling in AlternativeSpellings)
                {
                    hash = (hash * 37) + spelling.GetHashCode();
                }
            }
            else
            {
                hash *= 37;
            }


            if (Readings is not null)
            {
                foreach (string readings in Readings)
                {
                    hash = (hash * 37) + readings.GetHashCode();
                }
            }

            else
            {
                hash *= 37;
            }

            foreach (string definition in Definitions)
            {
                hash = (hash * 37) + definition.GetHashCode();
            }

            foreach (string wordClass in WordClasses)
            {
                hash = (hash * 37) + wordClass.GetHashCode();
            }

            return hash;
        }
    }
}
