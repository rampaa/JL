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

    public string BuildFormattedDefinition(DictOptions options)
    {
        bool definitionTagsExist = DefinitionTags is not null;
        if (Definitions.Length is 1)
        {
            return definitionTagsExist
                ? $"({DefinitionTags![0]}) {Definitions[0]}"
                : Definitions[0];
        }

        bool newlines = options.NewlineBetweenDefinitions!.Value;
        string separator = newlines
            ? "\n"
            : "; ";

        StringBuilder defResult = new();
        for (int i = 0; i < Definitions.Length; i++)
        {
            if (newlines)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({i + 1}) ");
            }

            if (definitionTagsExist && DefinitionTags!.Length > i)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({DefinitionTags[i]}) ");
            }

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
        return obj is EpwingYomichanRecord epwingYomichanRecord
               && PrimarySpelling == epwingYomichanRecord.PrimarySpelling
               && Reading == epwingYomichanRecord.Reading
               && epwingYomichanRecord.Definitions.SequenceEqual(Definitions);
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
