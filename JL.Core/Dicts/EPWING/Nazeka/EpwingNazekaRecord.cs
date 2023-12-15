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

    public string BuildFormattedDefinition(DictOptions? options)
    {
        StringBuilder defResult = new();

        string separator = options is { NewlineBetweenDefinitions.Value: false }
            ? ""
            : "\n";

        for (int i = 0; i < Definitions.Length; i++)
        {
            _ = defResult.Append(CultureInfo.InvariantCulture, $"{Definitions[i]}{separator}");
        }

        return defResult.Remove(defResult.Length - separator.Length, separator.Length).ToString();
    }

    public int GetFrequency(Freq freq)
    {
        int frequency = int.MaxValue;

        if (freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling),
                out IList<FrequencyRecord>? freqResults))
        {
            for (int i = 0; i < freqResults.Count; i++)
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

            if (frequency is int.MaxValue && AlternativeSpellings is not null)
            {
                for (int i = 0; i < AlternativeSpellings.Length; i++)
                {
                    if (freq.Contents.TryGetValue(
                            JapaneseUtils.KatakanaToHiragana(AlternativeSpellings[i]),
                            out IList<FrequencyRecord>? alternativeSpellingFreqResults))
                    {
                        for (int j = 0; j < alternativeSpellingFreqResults.Count; j++)
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
        }

        else if (Reading is not null)
        {
            if (freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading),
                    out IList<FrequencyRecord>? readingFreqResults))
            {
                for (int i = 0; i < readingFreqResults.Count; i++)
                {
                    FrequencyRecord readingFreqResult = readingFreqResults[i];
                    if ((Reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(Reading))
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

        return frequency;
    }

    public int GetFrequencyFromDB(Dictionary<string, List<FrequencyRecord>> freqDict)
    {
        int frequency = int.MaxValue;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out List<FrequencyRecord>? freqResults))
        {
            for (int i = 0; i < freqResults.Count; i++)
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

            if (frequency is int.MaxValue && AlternativeSpellings is not null)
            {
                for (int i = 0; i < AlternativeSpellings.Length; i++)
                {
                    if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(AlternativeSpellings[i]), out List<FrequencyRecord>? alternativeSpellingFreqResults))
                    {
                        for (int j = 0; j < alternativeSpellingFreqResults.Count; j++)
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
        }

        else if (Reading is not null)
        {
            if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading), out List<FrequencyRecord>? readingFreqResults))
            {
                for (int i = 0; i < readingFreqResults.Count; i++)
                {
                    FrequencyRecord readingFreqResult = readingFreqResults[i];

                    if ((Reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(Reading))
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

        return frequency;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        EpwingNazekaRecord epwingNazekaRecordObj = (EpwingNazekaRecord)obj;
        return PrimarySpelling == epwingNazekaRecordObj.PrimarySpelling
               && Reading == epwingNazekaRecordObj.Reading
               && epwingNazekaRecordObj.Definitions.SequenceEqual(Definitions);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 37) + PrimarySpelling.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + Reading?.GetHashCode(StringComparison.Ordinal) ?? 37;

            foreach (string definition in Definitions)
            {
                hash = (hash * 37) + definition.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }
}
