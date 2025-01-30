using System.Globalization;
using System.Text;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.CustomWordDict;

internal sealed class CustomWordRecord : IDictRecordWithMultipleReadings, IGetFrequency, IEquatable<CustomWordRecord>
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

    public string BuildFormattedDefinition(DictOptions options)
    {
        string tempWordClass;
        if (HasUserDefinedWordClass)
        {
            tempWordClass = string.Join(", ", WordClasses);
        }
        else
        {
            string wordClass = WordClasses[0];
            tempWordClass = wordClass switch
            {
                "adj-i" => "adjective",
                "noun" => "noun",
                "other" => "other",
                "v1" => "verb",
                _ => "other"
            };
        }

        if (Definitions.Length is 1)
        {
            return $"({tempWordClass}) {Definitions[0]}";
        }

        bool newlines = options.NewlineBetweenDefinitions!.Value;

        StringBuilder defResult = new();

        char separator = newlines
            ? '\n'
            : '；';

        for (int i = 0; i < Definitions.Length; i++)
        {
            if (newlines)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            _ = defResult.Append(CultureInfo.InvariantCulture, $"({tempWordClass}) ");

            if (!newlines)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            _ = defResult.Append(Definitions[i]);

            if (i + 1 != Definitions.Length)
            {
                _ = defResult.Append(separator);
            }
        }

        return defResult.ToString();
    }

    public int GetFrequency(IDictionary<string, IList<FrequencyRecord>> freqDict)
    {
        bool readingsExist = Readings is not null;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out IList<FrequencyRecord>? freqResults))
        {
            int freqResultCount = freqResults.Count;
            for (int i = 0; i < freqResultCount; i++)
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
                    int readingFreqResultCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultCount; j++)
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
            int freqResultCount = freqResults.Count;
            for (int i = 0; i < freqResultCount; i++)
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
                if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out List<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultCount; j++)
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

    public override bool Equals(object? obj)
    {
        return obj is CustomWordRecord customWordRecord
               && PrimarySpelling == customWordRecord.PrimarySpelling
               && customWordRecord.Definitions.SequenceEqual(Definitions)
               && ((AlternativeSpellings is not null && customWordRecord.AlternativeSpellings is not null && customWordRecord.AlternativeSpellings.SequenceEqual(AlternativeSpellings))
                   || (AlternativeSpellings is null && customWordRecord.AlternativeSpellings is null))
               && ((Readings is not null && customWordRecord.Readings is not null && customWordRecord.Readings.SequenceEqual(Readings))
                   || (Readings is null && customWordRecord.Readings is null))
               && customWordRecord.WordClasses.SequenceEqual(WordClasses);
    }

    public bool Equals(CustomWordRecord? other)
    {
        return other is not null
               && PrimarySpelling == other.PrimarySpelling
               && other.Definitions.SequenceEqual(Definitions)
               && ((AlternativeSpellings is not null && other.AlternativeSpellings is not null && other.AlternativeSpellings.SequenceEqual(AlternativeSpellings))
                   || (AlternativeSpellings is null && other.AlternativeSpellings is null))
               && ((Readings is not null && other.Readings is not null && other.Readings.SequenceEqual(Readings))
                   || (Readings is null && other.Readings is null))
               && other.WordClasses.SequenceEqual(WordClasses);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + PrimarySpelling.GetHashCode(StringComparison.Ordinal);
            if (AlternativeSpellings is not null)
            {
                foreach (string alternativeSpelling in AlternativeSpellings)
                {
                    hash = (hash * 37) + alternativeSpelling.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }


            if (Readings is not null)
            {
                foreach (string reading in Readings)
                {
                    hash = (hash * 37) + reading.GetHashCode(StringComparison.Ordinal);
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
