using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal sealed class EpwingNazekaRecord : IEpwingRecord, IGetFrequency
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

        string separator = options is { NewlineBetweenDefinitions.Value: false }
            ? "; "
            : "\n";

        StringBuilder defResult = new();
        for (int i = 0; i < Definitions.Length; i++)
        {
            _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) {Definitions[i]}");
            if (i + 1 != Definitions.Length)
            {
                _ = defResult.Append(separator);
            }
        }

        return defResult.ToString();
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
                if (Reading == freqResult.Spelling || PrimarySpelling == freqResult.Spelling)
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }

            if (frequency is not int.MaxValue || AlternativeSpellings is null)
            {
                return frequency;
            }

            for (int i = 0; i < AlternativeSpellings.Length; i++)
            {
                if (freq.Contents.TryGetValue(
                        JapaneseUtils.KatakanaToHiragana(AlternativeSpellings[i]),
                        out IList<FrequencyRecord>? alternativeSpellingFreqResults))
                {
                    int alternativeSpellingFreqResultCount = alternativeSpellingFreqResults.Count;
                    for (int j = 0; j < alternativeSpellingFreqResultCount; j++)
                    {
                        FrequencyRecord alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];
                        if (Reading == alternativeSpellingFreqResult.Spelling)
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
        else if (Reading is not null
            && freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading), out IList<FrequencyRecord>? readingFreqResults))
        {
            int readingFreqResultCount = readingFreqResults.Count;
            for (int i = 0; i < readingFreqResultCount; i++)
            {
                FrequencyRecord readingFreqResult = readingFreqResults[i];
                if ((Reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(Reading[0]))
                    || (AlternativeSpellings?.Contains(readingFreqResult.Spelling) ?? false))
                {
                    if (frequency > readingFreqResult.Frequency)
                    {
                        frequency = readingFreqResult.Frequency;
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

                if (Reading == freqResult.Spelling || PrimarySpelling == freqResult.Spelling)
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }

            if (frequency is not int.MaxValue || AlternativeSpellings is null)
            {
                return frequency;
            }

            for (int i = 0; i < AlternativeSpellings.Length; i++)
            {
                if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(AlternativeSpellings[i]), out List<FrequencyRecord>? alternativeSpellingFreqResults))
                {
                    int alternativeSpellingFreqResultCount = alternativeSpellingFreqResults.Count;
                    for (int j = 0; j < alternativeSpellingFreqResultCount; j++)
                    {
                        FrequencyRecord alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];
                        if (Reading == alternativeSpellingFreqResult.Spelling)
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

        else if (Reading is not null
                 && freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading), out List<FrequencyRecord>? readingFreqResults))
        {
            int readingFreqResultCount = readingFreqResults.Count;
            for (int i = 0; i < readingFreqResultCount; i++)
            {
                FrequencyRecord readingFreqResult = readingFreqResults[i];
                if ((Reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(Reading[0]))
                    || (AlternativeSpellings?.Contains(readingFreqResult.Spelling) ?? false))
                {
                    if (frequency > readingFreqResult.Frequency)
                    {
                        frequency = readingFreqResult.Frequency;
                    }
                }
            }
        }

        return frequency;
    }

    public override bool Equals(object? obj)
    {
        return obj is EpwingNazekaRecord epwingNazekaRecord
               && PrimarySpelling == epwingNazekaRecord.PrimarySpelling
               && Reading == epwingNazekaRecord.Reading
               && epwingNazekaRecord.Definitions.SequenceEqual(Definitions);
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
}
