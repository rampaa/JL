using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal sealed class EpwingNazekaRecord : IEpwingRecord, IGetFrequency, IEquatable<EpwingNazekaRecord>
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    public string[]? AlternativeSpellings { get; }
    public string[] Definitions { get; }

    public EpwingNazekaRecord(string primarySpelling, string? reading, string[]? alternativeSpellings, string[] definitions)
    {
        PrimarySpelling = primarySpelling;
        Reading = reading;
        AlternativeSpellings = alternativeSpellings;
        Definitions = definitions;
    }

    public string BuildFormattedDefinition(DictOptions options)
    {
        if (Definitions.Length is 1)
        {
            return Definitions[0];
        }

        char separator = options is { NewlineBetweenDefinitions.Value: false }
            ? '；'
            : '\n';

        StringBuilder defResult = new();
        for (int i = 0; i < Definitions.Length; i++)
        {
            _ = defResult.Append(CultureInfo.InvariantCulture, $"{i + 1}. {Definitions[i]}");
            if (i + 1 != Definitions.Length)
            {
                _ = defResult.Append(separator);
            }
        }

        return defResult.ToString();
    }

    public int GetFrequency(IDictionary<string, IList<FrequencyRecord>> freqDict)
    {
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out IList<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];
                if (freqResult.Spelling == PrimarySpelling || freqResult.Spelling == Reading)
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (Reading is not null)
        {
            if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading), out IList<FrequencyRecord>? readingFreqResults))
            {
                int readingFreqResultsCount = readingFreqResults.Count;
                for (int j = 0; j < readingFreqResultsCount; j++)
                {
                    FrequencyRecord readingFreqResult = readingFreqResults[j];
                    if (readingFreqResult.Spelling == PrimarySpelling
                        || (readingFreqResult.Spelling == Reading && JapaneseUtils.IsKatakana(Reading[0])))
                    {
                        return readingFreqResult.Frequency;
                    }
                }
            }
        }

        return int.MaxValue;
    }

    public int GetFrequency(Dictionary<string, List<FrequencyRecord>> freqDict)
    {
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out List<FrequencyRecord>? freqResults))
        {
            foreach (ref readonly FrequencyRecord freqResult in CollectionsMarshal.AsSpan(freqResults))
            {
                if (freqResult.Spelling == PrimarySpelling || freqResult.Spelling == Reading)
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (Reading is not null)
        {
            if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading), out List<FrequencyRecord>? readingFreqResults))
            {
                foreach (ref readonly FrequencyRecord readingFreqResult in CollectionsMarshal.AsSpan(readingFreqResults))
                {
                    if (readingFreqResult.Spelling == PrimarySpelling
                        || (readingFreqResult.Spelling == Reading && JapaneseUtils.IsKatakana(Reading[0])))
                    {
                        return readingFreqResult.Frequency;
                    }
                }
            }
        }

        return int.MaxValue;
    }

    public override bool Equals(object? obj)
    {
        return obj is EpwingNazekaRecord epwingNazekaRecord
               && PrimarySpelling == epwingNazekaRecord.PrimarySpelling
               && Reading == epwingNazekaRecord.Reading
               && epwingNazekaRecord.Definitions.SequenceEqual(Definitions);
    }

    public bool Equals(EpwingNazekaRecord? other)
    {
        return other is not null
               && PrimarySpelling == other.PrimarySpelling
               && Reading == other.Reading
               && other.Definitions.SequenceEqual(Definitions);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + PrimarySpelling.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + Reading?.GetHashCode(StringComparison.Ordinal) ?? 37;

            foreach (string definition in Definitions)
            {
                hash = (hash * 37) + definition.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }

    public static bool operator ==(EpwingNazekaRecord? left, EpwingNazekaRecord? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(EpwingNazekaRecord? left, EpwingNazekaRecord? right) => !left?.Equals(right) ?? right is not null;
}
