using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal sealed class EpwingYomichanRecord : IEpwingRecord, IGetFrequency, IEquatable<EpwingYomichanRecord>
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    public string[]? Definitions { get; }
    public string[]? WordClasses { get; }
    public string[]? DefinitionTags { get; }
    public string[]? ImagePaths { get; }
    //public int Score { get; }
    //public int Sequence { get; }
    //public string[]? TermTags { get; }

    public EpwingYomichanRecord(string primarySpelling, string? reading, string[]? definitions, string[]? wordClasses, string[]? definitionTags, string[]? imagePaths)
    {
        PrimarySpelling = primarySpelling;
        Reading = reading;
        Definitions = definitions;
        WordClasses = wordClasses;
        DefinitionTags = definitionTags;
        ImagePaths = imagePaths;
    }

    public string? BuildFormattedDefinition(DictOptions options)
    {
        if (Definitions is null)
        {
            return null;
        }

        Debug.Assert(options.NewlineBetweenDefinitions is not null);
        bool newline = options.NewlineBetweenDefinitions.Value;

        if (Definitions.Length is 1)
        {
            return DefinitionTags is not null
                ? DefinitionTags.Length is 1
                    ? $"[{DefinitionTags[0]}] {Definitions[0]}"
                    : $"[{string.Join(", ", DefinitionTags)}]{(newline ? '\n' : ' ')}{Definitions[0]}"
                : Definitions[0];
        }

        char defSeparator = newline
            ? '\n'
            : '；';

        StringBuilder defBuilder = ObjectPoolManager.StringBuilderPool.Get();
        if (DefinitionTags is not null)
        {
            _ = defBuilder.Append('[').AppendJoin(", ", DefinitionTags).Append(']').Append(newline ? '\n' : ' ');
        }

        string[] definitions = Definitions;
        for (int i = 0; i < definitions.Length; i++)
        {
            int sequence = i + 1;
            _ = defBuilder.Append(CultureInfo.InvariantCulture, $"{sequence}. {definitions[i]}");
            if (sequence != definitions.Length)
            {
                _ = defBuilder.Append(defSeparator);
            }
        }

        string def = defBuilder.ToString();
        ObjectPoolManager.StringBuilderPool.Return(defBuilder);
        return def;
    }

    public int GetFrequency(IDictionary<string, IList<FrequencyRecord>> freqDict)
    {
        if (freqDict.TryGetValue(JapaneseUtils.NormalizeText(PrimarySpelling), out IList<FrequencyRecord>? freqResults))
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

        else if (Reading is not null && freqDict.TryGetValue(JapaneseUtils.NormalizeText(Reading), out IList<FrequencyRecord>? readingFreqResults))
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

        return int.MaxValue;
    }

    public int GetFrequency(Dictionary<string, List<FrequencyRecord>> freqDict)
    {
        bool readingExists = Reading is not null;
        if (freqDict.TryGetValue(JapaneseUtils.NormalizeText(PrimarySpelling), out List<FrequencyRecord>? freqResults))
        {
            foreach (ref readonly FrequencyRecord freqResult in freqResults.AsReadOnlySpan())
            {
                if (freqResult.Spelling == PrimarySpelling || freqResult.Spelling == Reading)
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (readingExists)
        {
            Debug.Assert(Reading is not null);
            if (freqDict.TryGetValue(JapaneseUtils.NormalizeText(Reading), out List<FrequencyRecord>? readingFreqResults))
            {
                foreach (ref readonly FrequencyRecord readingFreqResult in readingFreqResults.AsReadOnlySpan())
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

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is EpwingYomichanRecord other
               && (ReferenceEquals(this, other) || (PrimarySpelling == other.PrimarySpelling
               && Reading == other.Reading
               && other.Definitions.AsReadOnlySpan().SequenceEqual(Definitions)
               && other.ImagePaths.AsReadOnlySpan().SequenceEqual(ImagePaths)));
    }

    public bool Equals([NotNullWhen(true)] EpwingYomichanRecord? other)
    {
        return other is not null
               && (ReferenceEquals(this, other) || (PrimarySpelling == other.PrimarySpelling
               && Reading == other.Reading
               && other.Definitions.AsReadOnlySpan().SequenceEqual(Definitions)
               && other.ImagePaths.AsReadOnlySpan().SequenceEqual(ImagePaths)));
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + PrimarySpelling.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + (Reading?.GetHashCode(StringComparison.Ordinal) ?? 37);

            if (Definitions is not null)
            {
                foreach (string definition in Definitions)
                {
                    hash = (hash * 37) + definition.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            if (ImagePaths is not null)
            {
                foreach (string imagePath in ImagePaths)
                {
                    hash = (hash * 37) + imagePath.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            return hash;
        }
    }

    public static bool operator ==(EpwingYomichanRecord? left, EpwingYomichanRecord? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(EpwingYomichanRecord? left, EpwingYomichanRecord? right) => !(left == right);
}
