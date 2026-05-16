using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JL.Core.Dicts;

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
        int minDeconjugationProcessStepCount = 0,
        int entryId = 0,
        byte[]? pitchPositions = null,
        string[]? wordClasses = null,
        string[]? imagePaths = null,
        JmdictLookupResult? jmdictLookupResult = null,
        KanjiLookupResult? kanjiLookupResult = null
    ) : IEquatable<LookupResult>, IComparable<LookupResult>, IComparable
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
    private int MinDeconjugationProcessStepCount { get; } = minDeconjugationProcessStepCount;
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
            hash = (hash * 37) + Dict.GetHashCode();
            hash = (hash * 37) + (FormattedDefinitions?.GetHashCode(StringComparison.Ordinal) ?? 37);

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

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is LookupResult other
            && PrimarySpelling == other.PrimarySpelling
            && Dict.Name == other.Dict.Name
            && FormattedDefinitions == other.FormattedDefinitions
            && (other.Readings is not null
                ? Readings?.SequenceEqual(other.Readings) ?? false
                : Readings is null);
    }

    public bool Equals([NotNullWhen(true)] LookupResult? other)
    {
        return other is not null
            && PrimarySpelling == other.PrimarySpelling
            && Dict.Name == other.Dict.Name
            && FormattedDefinitions == other.FormattedDefinitions
            && (other.Readings is not null
                ? Readings?.SequenceEqual(other.Readings) ?? false
                : Readings is null);
    }

    public static bool operator ==(LookupResult? left, LookupResult? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(LookupResult? left, LookupResult? right) => !(left == right);

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
        int readingIndexOfMatchedText = Readings?.IndexOf(MatchedText) ?? -1;
        int otherReadingIndexOfMatchedText = other.Readings?.IndexOf(otherMatchedText) ?? -1;

        bool readingsContainMatchedText = readingIndexOfMatchedText >= 0;
        bool otherReadingsContainMatchedText = otherReadingIndexOfMatchedText >= 0;
        cmpResult = otherReadingsContainMatchedText.CompareTo(readingsContainMatchedText);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 4. ThenBy the minimum deconjugation process step count
        cmpResult = MinDeconjugationProcessStepCount.CompareTo(other.MinDeconjugationProcessStepCount);
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

        int primarySpellingOrthographyScore = jmdictLookupResultExists && matchedPrimarySpelling
            ? GetPrimarySpellingOrthographyScore(jmdictLookupResult!)
            : int.MaxValue;

        int otherPrimarySpellingOrthographyScore = otherJmdictLookupResultExists && otherMatchedPrimarySpelling
            ? GetPrimarySpellingOrthographyScore(otherJmdictLookupResult!)
            : int.MaxValue;

        cmpResult = primarySpellingOrthographyScore.CompareTo(otherPrimarySpellingOrthographyScore);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 7. ThenBy: Reading orthography info check (uk, ok, ik, rk)
        int readingOrthographyScore = jmdictLookupResultExists && readingsContainMatchedText
            ? GetReadingOrthographyScore(jmdictLookupResult, readingIndexOfMatchedText)
            : int.MaxValue;

        int otherReadingOrthographyScore = otherJmdictLookupResultExists && otherReadingsContainMatchedText
            ? GetReadingOrthographyScore(otherJmdictLookupResult, otherReadingIndexOfMatchedText)
            : int.MaxValue;

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
        int readingIndexOfMatchedTextScore = readingsContainMatchedText
            ? readingIndexOfMatchedText
            : int.MaxValue;

        int otherReadingIndexOfMatchedTextScore = otherReadingsContainMatchedText
            ? otherReadingIndexOfMatchedText
            : int.MaxValue;

        cmpResult = readingIndexOfMatchedTextScore.CompareTo(otherReadingIndexOfMatchedTextScore);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 10. ThenBy: EntryId
        int idScore = EntryId > 0
            ? EntryId
            : int.MaxValue;

        int otherIdScore = other.EntryId > 0
            ? other.EntryId
            : int.MaxValue;

        cmpResult = idScore.CompareTo(otherIdScore);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 11. ThenBy: Primary spelling
        cmpResult = PrimarySpelling.CompareTo(otherPrimarySpelling, StringComparison.Ordinal);
        if (cmpResult is not 0)
        {
            return cmpResult;
        }

        // 12. ThenByDescending: FormattedDefinitions length
        cmpResult = (other.FormattedDefinitions?.Length ?? 0).CompareTo(FormattedDefinitions?.Length ?? 0);
        return cmpResult is not 0
            ? cmpResult
            // 13. ThenBy: FormattedDefinitions
            : FormattedDefinitions.CompareTo(other.FormattedDefinitions, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        return obj is LookupResult lookupResult
            ? CompareTo(lookupResult)
            : -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetPrimarySpellingOrthographyScore(JmdictLookupResult jmdictResult)
    {
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

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetReadingOrthographyScore(JmdictLookupResult? jmdictLookupResult, int readingIndexOfMatchedText)
    {
        Debug.Assert(jmdictLookupResult is not null);
        string[]? readingsOrthographyInfo = jmdictLookupResult.ReadingsOrthographyInfoList?[readingIndexOfMatchedText];
        if (readingsOrthographyInfo is not null)
        {
            foreach (string readingsOrthographyInfoItem in readingsOrthographyInfo)
            {
                if (readingsOrthographyInfoItem is "ok" or "ik" or "rk")
                {
                    return 2;
                }
            }
        }

        string[]? miscSharedByAllSenses = jmdictLookupResult.MiscSharedByAllSenses;
        if (miscSharedByAllSenses is not null && miscSharedByAllSenses.Contains("uk"))
        {
            return 0;
        }

        string[]?[]? miscList = jmdictLookupResult.MiscList;
        if (miscList is not null)
        {
            foreach (string[]? misc in miscList)
            {
                if (misc is not null && misc.Contains("uk"))
                {
                    return 0;
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
