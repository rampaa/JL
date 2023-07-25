using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.CustomWordDict;

internal sealed class CustomWordRecord : IDictRecord, IGetFrequency
{
    public string PrimarySpelling { get; }
    public string[]? AlternativeSpellings { get; }
    public string[]? Readings { get; }
    private string[] Definitions { get; }
    public string[] WordClasses { get; }
    public bool HasUserDefinedWordClass { get; }

    public CustomWordRecord(string primarySpelling, string[]? alternativeSpellings, string[]? readings,
        string[] definitions, string[] wordClasses, bool hasUserDefinedWordClass)
    {
        PrimarySpelling = primarySpelling;
        AlternativeSpellings = alternativeSpellings;
        Readings = readings;
        Definitions = definitions;
        WordClasses = wordClasses;
        HasUserDefinedWordClass = hasUserDefinedWordClass;
    }

    public string BuildFormattedDefinition(DictOptions? options)
    {
        string separator = options is { NewlineBetweenDefinitions.Value: false }
            ? ""
            : "\n";

        int count = 1;
        StringBuilder defResult = new();

        if (WordClasses.Length > 0)
        {
            string tempWordClass;
            if (WordClasses.Contains("adj-i"))
            {
                tempWordClass = "adjective";
            }
            else if (WordClasses.Contains("noun"))
            {
                tempWordClass = "noun";
            }
            else if (WordClasses.Contains("other"))
            {
                tempWordClass = "other";
            }
            else if (HasUserDefinedWordClass)
            {
                tempWordClass = string.Join(", ", WordClasses);
            }
            else
            {
                tempWordClass = "verb";
            }

            _ = defResult.Append(CultureInfo.InvariantCulture, $"({tempWordClass}) ");
        }

        for (int i = 0; i < Definitions.Length; i++)
        {
            if (Definitions.Length > 0)
            {
                if (Definitions.Length > 1)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({count}) ");
                }

                _ = defResult.Append(CultureInfo.InvariantCulture, $"{string.Join("; ", Definitions[i])} {separator}");

                ++count;
            }
        }

        return defResult.Remove(defResult.Length - separator.Length - 1, separator.Length + 1).ToString();
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
                for (int i = 0; i < AlternativeSpellings.Length; i++)
                {
                    if (freq.Contents.TryGetValue(
                            JapaneseUtils.KatakanaToHiragana(AlternativeSpellings[i]),
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
            int readingCount = Readings.Length;
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

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        var customWordRecordObj = (CustomWordRecord)obj;

        return PrimarySpelling == customWordRecordObj.PrimarySpelling
               && (customWordRecordObj.AlternativeSpellings?.SequenceEqual(AlternativeSpellings ?? Enumerable.Empty<string>()) ?? AlternativeSpellings is null)
               && (customWordRecordObj.Readings?.SequenceEqual(Readings ?? Enumerable.Empty<string>()) ?? Readings is null)
               && customWordRecordObj.Definitions.SequenceEqual(Definitions)
               && customWordRecordObj.WordClasses.SequenceEqual(WordClasses);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            hash = (hash * 37) + PrimarySpelling.GetHashCode(StringComparison.Ordinal);

            if (AlternativeSpellings is not null)
            {
                foreach (string spelling in AlternativeSpellings)
                {
                    hash = (hash * 37) + spelling.GetHashCode(StringComparison.Ordinal);
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
                    hash = (hash * 37) + readings.GetHashCode(StringComparison.Ordinal);
                }
            }

            else
            {
                hash *= 37;
            }

            foreach (string definition in Definitions)
            {
                hash = (hash * 37) + definition.GetHashCode(StringComparison.Ordinal);
            }

            foreach (string wordClass in WordClasses)
            {
                hash = (hash * 37) + wordClass.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }
}
