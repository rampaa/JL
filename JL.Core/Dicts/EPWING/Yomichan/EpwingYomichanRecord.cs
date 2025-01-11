using System.Globalization;
using System.Text;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal sealed class EpwingYomichanRecord : IEpwingRecord, IGetFrequency, IEquatable<EpwingYomichanRecord>
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
        char separator = newlines
            ? '\n'
            : '；';

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

    public int GetFrequency(IDictionary<string, IList<FrequencyRecord>> freqDict)
    {
        int frequency = int.MaxValue;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out IList<FrequencyRecord>? freqResults))
        {
            int freqResultCount = freqResults.Count;
            for (int i = 0; i < freqResultCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];
                if (freqResult.Spelling == PrimarySpelling || freqResult.Spelling == Reading)
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (Reading is not null && freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading), out IList<FrequencyRecord>? readingFreqResults))
        {
            int readingFreqResultCount = readingFreqResults.Count;
            for (int j = 0; j < readingFreqResultCount; j++)
            {
                FrequencyRecord readingFreqResult = readingFreqResults[j];
                if (readingFreqResult.Spelling == PrimarySpelling
                    || (readingFreqResult.Spelling == Reading && JapaneseUtils.IsKatakana(Reading[0])))
                {
                    return readingFreqResult.Frequency;
                }
            }
        }

        return frequency;
    }

    public int GetFrequency(Dictionary<string, List<FrequencyRecord>> freqDict)
    {
        bool readingExists = Reading is not null;
        int frequency = int.MaxValue;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out List<FrequencyRecord>? freqResults))
        {
            int freqResultCount = freqResults.Count;
            for (int i = 0; i < freqResultCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];
                if (freqResult.Spelling == PrimarySpelling || freqResult.Spelling == Reading)
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (readingExists && freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading!), out List<FrequencyRecord>? readingFreqResults))
        {
            int readingFreqResultCount = readingFreqResults.Count;
            for (int j = 0; j < readingFreqResultCount; j++)
            {
                FrequencyRecord readingFreqResult = readingFreqResults[j];
                if (readingFreqResult.Spelling == PrimarySpelling
                    || (readingFreqResult.Spelling == Reading && JapaneseUtils.IsKatakana(Reading[0])))
                {
                    return readingFreqResult.Frequency;
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

    public bool Equals(EpwingYomichanRecord? other)
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
}
