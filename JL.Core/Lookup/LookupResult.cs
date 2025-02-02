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
        string[]? primarySpellingOrthographyInfoList = null,
        string[]?[]? readingsOrthographyInfoList = null,
        string[]?[]? alternativeSpellingsOrthographyInfoList = null,
        string[]?[]? miscList = null,
        string[]? miscSharedByAllSensesList = null,
        string[]? onReadings = null,
        string[]? kunReadings = null,
        string[]? nanoriReadings = null,
        string[]? radicalNames = null,
        string? formattedDefinitions = null,
        string? deconjugatedMatchedText = null,
        string? deconjugationProcess = null,
        string? kanjiComposition = null,
        string? kanjiStats = null,
        int entryId = 0,
        byte strokeCount = 0,
        byte kanjiGrade = byte.MaxValue,
        byte[]? pitchPositions = null
    ) : IEquatable<LookupResult>
{
    // common (required for sorting)
    public string PrimarySpelling { get; } = primarySpelling;
    public string[]? Readings { get; } = readings;
    public string? FormattedDefinitions { get; } = formattedDefinitions;
    public Dict Dict { get; } = dict;
    public string MatchedText { get; } = matchedText;
    public List<LookupFrequencyResult>? Frequencies { get; } = frequencies;

    // JMdict, JMnedict, KANJIDIC2
    internal int EntryId { get; } = entryId;

    // Word dictionaries
    public string? DeconjugatedMatchedText { get; } = deconjugatedMatchedText;
    public string? DeconjugationProcess { get; } = deconjugationProcess;
    // JMdict, Nazeka EPWING
    public string[]? AlternativeSpellings { get; } = alternativeSpellings;
    public string[]? PrimarySpellingOrthographyInfoList { get; } = primarySpellingOrthographyInfoList;
    public string[]?[]? ReadingsOrthographyInfoList { get; } = readingsOrthographyInfoList;
    public string[]?[]? AlternativeSpellingsOrthographyInfoList { get; } = alternativeSpellingsOrthographyInfoList;
    internal string[]?[]? MiscList { get; } = miscList;
    internal string[]? MiscSharedByAllSenses { get; } = miscSharedByAllSensesList;
    // Kanji
    public string[]? OnReadings { get; } = onReadings;
    public string[]? KunReadings { get; } = kunReadings;
    public string? KanjiComposition { get; } = kanjiComposition;
    public string? KanjiStats { get; } = kanjiStats;
    // KANJIDIC2
    public string[]? NanoriReadings { get; } = nanoriReadings;
    public string[]? RadicalNames { get; } = radicalNames;
    public byte StrokeCount { get; } = strokeCount;
    public byte KanjiGrade { get; } = kanjiGrade;
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
            return hash;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is LookupResult other
            && PrimarySpelling == other.PrimarySpelling
            && MatchedText == other.MatchedText
            && Dict == other.Dict
            && FormattedDefinitions == other.FormattedDefinitions;
    }

    public bool Equals(LookupResult? other)
    {
        return other is not null
            && PrimarySpelling == other.PrimarySpelling
            && MatchedText == other.MatchedText
            && Dict == other.Dict
            && FormattedDefinitions == other.FormattedDefinitions;
    }

    public static bool operator ==(LookupResult? left, LookupResult? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(LookupResult? left, LookupResult? right) => !left?.Equals(right) ?? right is not null;
}
