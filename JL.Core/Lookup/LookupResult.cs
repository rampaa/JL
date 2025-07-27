using System.Diagnostics;
using System.Runtime.CompilerServices;
using JL.Core.Dicts;
using JL.Core.Utilities;

namespace JL.Core.Lookup;

public sealed class LookupResult
    (
        string primarySpelling,
        string matchedText,
        Dict dict,
        string[]? readings,
        string? formattedDefinitions,
        List<LookupFrequencyResult>? frequencies = null,
        string[]? alternativeSpellings = null,
        string? deconjugatedMatchedText = null,
        string? deconjugationProcess = null,
        int entryId = 0,
        byte[]? pitchPositions = null,
        string[]? wordClasses = null,
        string[]? imagePaths = null,
        JmdictLookupResult? jmdictLookupResult = null,
        KanjiLookupResult? kanjiLookupResult = null
    ) : IEquatable<LookupResult>, IComparable<LookupResult>
{
    // common (required for sorting)
    public string PrimarySpelling { get; } = primarySpelling;
    public string[]? Readings { get; } = readings;
    public string? FormattedDefinitions { get; } = formattedDefinitions;
    public Dict Dict { get; } = dict;
    public string MatchedText { get; } = matchedText;
    public List<LookupFrequencyResult>? Frequencies { get; } = frequencies;

    // JMdict, JMnedict
    internal int EntryId { get; } = entryId;

    // Word dictionaries
    public string? DeconjugatedMatchedText { get; } = deconjugatedMatchedText;
    public string? DeconjugationProcess { get; } = deconjugationProcess;
    internal string[]? WordClasses { get; } = wordClasses;

    // Yomichan dictionaries
    public string[]? ImagePaths { get; } = imagePaths;

    // JMdict, Nazeka EPWING
    public string[]? AlternativeSpellings { get; } = alternativeSpellings;

    // JMdict
    public JmdictLookupResult? JmdictLookupResult { get; } = jmdictLookupResult;

    // Kanji
    public KanjiLookupResult? KanjiLookupResult { get; } = kanjiLookupResult;

    // Pitch Dictionary
    public byte[]? PitchPositions { get; } = pitchPositions;

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + PrimarySpelling.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + MatchedText.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + Dict.GetHashCode();
            hash = (hash * 37) + FormattedDefinitions?.GetHashCode(StringComparison.Ordinal) ?? 37;

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
                hash = (hash * 37) + 37;
            }

            return hash;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is LookupResult other
            && PrimarySpelling == other.PrimarySpelling
            && MatchedText == other.MatchedText
            && Dict == other.Dict
            && FormattedDefinitions == other.FormattedDefinitions
            && other.Readings is not null
                ? Readings?.AsReadOnlySpan().SequenceEqual(other.Readings) ?? false
                : Readings is null;
    }

    public bool Equals(LookupResult? other)
    {
        return other is not null
            && PrimarySpelling == other.PrimarySpelling
            && MatchedText == other.MatchedText
            && Dict == other.Dict
            && FormattedDefinitions == other.FormattedDefinitions
            && other.Readings is not null
                ? Readings?.AsReadOnlySpan().SequenceEqual(other.Readings) ?? false
                : Readings is null;
    }

    public static bool operator ==(LookupResult? left, LookupResult? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(LookupResult? left, LookupResult? right) => !left?.Equals(right) ?? right is not null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(LookupResult? other)
    {
        if (other is null)
        {
            return -1;
        }

        string otherMatchedText = other.MatchedText;

        // 1. Order by MatchedText.Length descending
        int cmpResult = otherMatchedText.Length.CompareTo(MatchedText.Length);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 2. ThenByDescending: PrimarySpelling == MatchedText
        string otherPrimarySpelling = other.PrimarySpelling;

        bool matchedPrimarySpelling = PrimarySpelling == MatchedText;
        bool otherMatchedPrimarySpelling = otherPrimarySpelling == otherMatchedText;
        cmpResult = otherMatchedPrimarySpelling.CompareTo(matchedPrimarySpelling);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 3. ThenByDescending: Readings contains MatchedText
        int readingIndexOfMatchedText = Readings?.AsReadOnlySpan().IndexOf(MatchedText) ?? -1;
        int otherReadingIndexOfMatchedText = other.Readings?.AsReadOnlySpan().IndexOf(otherMatchedText) ?? -1;

        bool readingsContainMatchedText = readingIndexOfMatchedText >= 0;
        bool otherReadingsContainMatchedText = otherReadingIndexOfMatchedText >= 0;
        cmpResult = otherReadingsContainMatchedText.CompareTo(readingsContainMatchedText);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 4. ThenBy the length of the deconjugation process
        int deconjugationScore = DeconjugationProcess?.Length ?? 0;
        int otherDeconjugationScore = other.DeconjugationProcess?.Length ?? 0;
        cmpResult = deconjugationScore.CompareTo(otherDeconjugationScore);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 5. ThenBy: Dict.Priority ascending
        cmpResult = Dict.Priority.CompareTo(other.Dict.Priority);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 6. ThenBy: Primary spelling orthography info check (oK, iK, rK)
        JmdictLookupResult? jmdictLookupResult = JmdictLookupResult;
        JmdictLookupResult? otherJmdictLookupResult = other.JmdictLookupResult;
        bool jmdictLookupResultExists = jmdictLookupResult is not null;
        bool otherJmdictLookupResultExists = otherJmdictLookupResult is not null;

        int primarySpellingOrthographyScore = GetPrimarySpellingOrthographyScore(jmdictLookupResult, jmdictLookupResultExists, matchedPrimarySpelling);
        int otherPrimarySpellingOrthographyScore = GetPrimarySpellingOrthographyScore(otherJmdictLookupResult, otherJmdictLookupResultExists, otherMatchedPrimarySpelling);
        cmpResult = primarySpellingOrthographyScore.CompareTo(otherPrimarySpellingOrthographyScore);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 7. ThenBy: Reading orthography info check (uk, ok, ik, rk)
        int readingOrthographyScore = GetReadingOrthographyScore(jmdictLookupResult, jmdictLookupResultExists, readingsContainMatchedText, readingIndexOfMatchedText);
        int otherReadingOrthographyScore = GetReadingOrthographyScore(otherJmdictLookupResult, otherJmdictLookupResultExists, otherReadingsContainMatchedText, otherReadingIndexOfMatchedText);
        cmpResult = readingOrthographyScore.CompareTo(otherReadingOrthographyScore);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 8. ThenBy: Frequency score
        int frequencyScore = GetFrequencyScore(Frequencies);
        int otherFrequencyScore = GetFrequencyScore(other.Frequencies);
        cmpResult = frequencyScore.CompareTo(otherFrequencyScore);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 9. ThenBy: Index in Readings
        return (readingIndexOfMatchedText >= 0 ? readingIndexOfMatchedText : int.MaxValue)
            .CompareTo(otherReadingIndexOfMatchedText >= 0 ? otherReadingIndexOfMatchedText : int.MaxValue);

        // 10. ThenBy: EntryId
        // EntryId.CompareTo(other.EntryId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetPrimarySpellingOrthographyScore(JmdictLookupResult? jmdictResult, bool jmdictLookupResultExists, bool matchedPrimarySpelling)
    {
        Debug.Assert(jmdictResult is not null == jmdictLookupResultExists);

        if (matchedPrimarySpelling && jmdictLookupResultExists)
        {
            Debug.Assert(jmdictResult is not null);

            string[]? primarySpellingOrthographyInfoList = jmdictResult.PrimarySpellingOrthographyInfoList;
            if (primarySpellingOrthographyInfoList is not null)
            {
                foreach (string primarySpellingOrthographyInfo in primarySpellingOrthographyInfoList)
                {
                    if (primarySpellingOrthographyInfo is "oK" or "iK" or "rK")
                    {
                        return 1;
                    }
                }
            }
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetReadingOrthographyScore(JmdictLookupResult? jmdictLookupResult, bool jmdictLookupResultExists, bool readingsContainMatchedText, int readingIndexOfMatchedText)
    {
        if (!readingsContainMatchedText)
        {
            return 2;
        }

        Debug.Assert(jmdictLookupResult is not null == jmdictLookupResultExists);
        if (jmdictLookupResultExists)
        {
            Debug.Assert(jmdictLookupResult is not null);

            string[]? miscSharedByAllSenses = jmdictLookupResult.MiscSharedByAllSenses;
            if (miscSharedByAllSenses is not null && miscSharedByAllSenses.AsReadOnlySpan().Contains("uk"))
            {
                return 0;
            }

            string[]?[]? miscList = jmdictLookupResult.MiscList;
            if (miscList is not null)
            {
                foreach (string[]? misc in miscList)
                {
                    if (misc is not null && misc.AsReadOnlySpan().Contains("uk"))
                    {
                        return 0;
                    }
                }
            }

            string[]? readingsOrthographyInfo = jmdictLookupResult.ReadingsOrthographyInfoList?[readingIndexOfMatchedText];
            if (readingsOrthographyInfo is not null)
            {
                foreach (string readingsOrthographyInfoItem in readingsOrthographyInfo)
                {
                    if (readingsOrthographyInfoItem is "ok" or "ik" or "rk")
                    {
                        return 3;
                    }
                }
            }
        }

        return 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetFrequencyScore(List<LookupFrequencyResult>? frequencies)
    {
        if (frequencies is null)
        {
            return int.MaxValue;
        }

        Debug.Assert(frequencies.Count > 0);
        LookupFrequencyResult freqResult = frequencies[0];
        return !freqResult.HigherValueMeansHigherFrequency
            ? freqResult.Freq
            : freqResult.Freq is int.MaxValue
                ? int.MaxValue
                : int.MaxValue - freqResult.Freq;
    }

    public static bool operator <(LookupResult left, LookupResult right) => left.CompareTo(right) < 0;
    public static bool operator <=(LookupResult left, LookupResult right) => left.CompareTo(right) <= 0;
    public static bool operator >(LookupResult left, LookupResult right) => left.CompareTo(right) > 0;
    public static bool operator >=(LookupResult left, LookupResult right) => left.CompareTo(right) >= 0;
}
