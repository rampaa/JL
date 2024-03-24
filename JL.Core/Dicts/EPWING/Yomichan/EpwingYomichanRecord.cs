using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal sealed class EpwingYomichanRecord : IEpwingRecord, IGetFrequency
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    public string[] Definitions { get; }
    public string[]? WordClasses { get; }
    public string[]? DefinitionTags { get; }
    //public int Score { get; }
    //public int Sequence { get; }
    //public string TermTags { get; }

    public EpwingYomichanRecord(string primarySpelling, string? reading, string[] definitions, string[]? wordClasses, string[]? definitionTags)
    {
        PrimarySpelling = primarySpelling;
        Reading = reading;
        Definitions = definitions;
        WordClasses = wordClasses;
        DefinitionTags = definitionTags;
    }

    public string BuildFormattedDefinition(DictOptions? options)
    {
        StringBuilder defResult = new();

        string separator = options is { NewlineBetweenDefinitions.Value: false }
            ? ""
            : "\n";

        for (int i = 0; i < Definitions.Length; i++)
        {
            if (DefinitionTags?.Length > i)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({DefinitionTags[i]}) ");
            }

            _ = defResult.Append(Definitions[i]).Append(separator);
        }

        return defResult.Remove(defResult.Length - separator.Length, separator.Length).ToString();
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
        }

        else if (Reading is not null
                 && freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading),
                     out IList<FrequencyRecord>? readingFreqResults))
        {
            int readingFreqResultCount = readingFreqResults.Count;
            for (int i = 0; i < readingFreqResultCount; i++)
            {
                FrequencyRecord readingFreqResult = readingFreqResults[i];
                if (Reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(Reading))
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

    public int GetFrequencyFromDB(IDictionary<string, List<FrequencyRecord>> freqDict)
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
        }

        else if (Reading is not null)
        {
            if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading), out List<FrequencyRecord>? readingFreqResults))
            {
                int readingFreqResultCount = readingFreqResults.Count;
                for (int i = 0; i < readingFreqResultCount; i++)
                {
                    FrequencyRecord readingFreqResult = readingFreqResults[i];
                    if (Reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(Reading))
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

        EpwingYomichanRecord epwingYomichanRecordObj = (EpwingYomichanRecord)obj;
        return PrimarySpelling == epwingYomichanRecordObj.PrimarySpelling
               && Reading == epwingYomichanRecordObj.Reading
               && epwingYomichanRecordObj.Definitions.SequenceEqual(Definitions);
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
