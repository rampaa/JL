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
                "v1" => "verb",
                "n" => "noun",
                "other" => "other",
                _ => "other"
            };
        }

        if (Definitions.Length is 1)
        {
            return $"[{tempWordClass}] {Definitions[0]}";
        }

        char separator = options.NewlineBetweenDefinitions!.Value
            ? '\n'
            : '；';

        StringBuilder defResult = new();

        string[] definitions = Definitions;
        for (int i = 0; i < definitions.Length; i++)
        {
            int sequence = i + 1;
            _ = defResult.Append(CultureInfo.InvariantCulture, $"{sequence}. [{tempWordClass}] {definitions[i]}");
            if (sequence != definitions.Length)
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
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];
                if (freqResult.Spelling == PrimarySpelling
                    || (readingsExist && Readings!.AsSpan().Contains(freqResult.Spelling)))
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (readingsExist)
        {
            string[] readings = Readings!;
            for (int i = 0; i < readings.Length; i++)
            {
                string reading = readings[i];
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
            foreach (ref readonly FrequencyRecord freqResult in freqResults.AsSpan())
            {
                if (freqResult.Spelling == PrimarySpelling
                    || (readingsExist && Readings!.AsSpan().Contains(freqResult.Spelling)))
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (readingsExist)
        {
            string[] readings = Readings!;
            for (int i = 0; i < readings.Length; i++)
            {
                string reading = readings[i];
                if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out List<FrequencyRecord>? readingFreqResults))
                {
                    foreach (ref readonly FrequencyRecord readingFreqResult in readingFreqResults.AsSpan())
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

    public override bool Equals(object? obj)
    {
        return obj is CustomWordRecord customWordRecord
               && PrimarySpelling == customWordRecord.PrimarySpelling
               && customWordRecord.Definitions.AsSpan().SequenceEqual(Definitions)
               && ((AlternativeSpellings is not null && customWordRecord.AlternativeSpellings is not null && customWordRecord.AlternativeSpellings.AsSpan().SequenceEqual(AlternativeSpellings))
                   || (AlternativeSpellings is null && customWordRecord.AlternativeSpellings is null))
               && ((Readings is not null && customWordRecord.Readings is not null && customWordRecord.Readings.AsSpan().SequenceEqual(Readings))
                   || (Readings is null && customWordRecord.Readings is null))
               && customWordRecord.WordClasses.AsSpan().SequenceEqual(WordClasses);
    }

    public bool Equals(CustomWordRecord? other)
    {
        return other is not null
               && PrimarySpelling == other.PrimarySpelling
               && other.Definitions.AsSpan().SequenceEqual(Definitions)
               && ((AlternativeSpellings is not null && other.AlternativeSpellings is not null && other.AlternativeSpellings.AsSpan().SequenceEqual(AlternativeSpellings))
                   || (AlternativeSpellings is null && other.AlternativeSpellings is null))
               && ((Readings is not null && other.Readings is not null && other.Readings.AsSpan().SequenceEqual(Readings))
                   || (Readings is null && other.Readings is null))
               && other.WordClasses.AsSpan().SequenceEqual(WordClasses);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + PrimarySpelling.GetHashCode(StringComparison.Ordinal);

            string[]? alternativeSpellings = AlternativeSpellings;
            if (alternativeSpellings is not null)
            {
                foreach (string alternativeSpelling in alternativeSpellings)
                {
                    hash = (hash * 37) + alternativeSpelling.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            string[]? readings = Readings;
            if (readings is not null)
            {
                foreach (string reading in readings)
                {
                    hash = (hash * 37) + reading.GetHashCode(StringComparison.Ordinal);
                }
            }

            else
            {
                hash *= 37;
            }

            string[] definitions = Definitions;
            foreach (string definition in definitions)
            {
                hash = (hash * 37) + definition.GetHashCode(StringComparison.Ordinal);
            }

            string[] wordClasses = WordClasses;
            foreach (string wordClass in wordClasses)
            {
                hash = (hash * 37) + wordClass.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }

    public static bool operator ==(CustomWordRecord? left, CustomWordRecord? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(CustomWordRecord? left, CustomWordRecord? right) => !left?.Equals(right) ?? right is not null;
}
