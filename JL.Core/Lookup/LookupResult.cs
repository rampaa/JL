using JL.Core.Dicts;

namespace JL.Core.Lookup;

public sealed class LookupResult
    (
        string primarySpelling,
        string matchedText,
        Dict dict,
        string[]? readings,
        List<LookupFrequencyResult>? frequencies = null,
        string[]? alternativeSpellings = null,
        string? formattedDefinitions = null,
        string? deconjugatedMatchedText = null,
        string? deconjugationProcess = null,
        int entryId = 0,
        byte[]? pitchPositions = null,
        string[]? wordClasses = null,
        JmdictLookupResult? jmdictLookupResult = null,
        KanjiLookupResult? kanjiLookupResult = null
    ) : IEquatable<LookupResult>
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

            if (Readings is not null)
            {
                for (int i = 0; i < Readings.Length; i++)
                {
                    hash = (hash * 37) + Readings[i].GetHashCode(StringComparison.Ordinal);
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
                ? Readings?.SequenceEqual(other.Readings) ?? false
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
                ? Readings?.SequenceEqual(other.Readings) ?? false
                : Readings is null;
    }

    public static bool operator ==(LookupResult? left, LookupResult? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(LookupResult? left, LookupResult? right) => !left?.Equals(right) ?? right is not null;
}
