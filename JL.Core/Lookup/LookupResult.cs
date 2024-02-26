using JL.Core.Dicts;

namespace JL.Core.Lookup;

public sealed class LookupResult
{
    // common (required for sorting)
    public string PrimarySpelling { get; }
    public string[]? Readings { get; }
    public string? FormattedDefinitions { get; }
    public Dict Dict { get; }
    public string MatchedText { get; }
    public List<LookupFrequencyResult>? Frequencies { get; }

    // JMdict, JMnedict, KANJIDIC2
    internal int EdictId { get; }

    // Word dictionaries
    internal string DeconjugatedMatchedText { get; }
    public string? DeconjugationProcess { get; }
    // JMdict, Nazeka EPWING
    public string[]? AlternativeSpellings { get; }
    public string[]? PrimarySpellingOrthographyInfoList { get; }
    public string[]?[]? ReadingsOrthographyInfoList { get; }
    public string[]?[]? AlternativeSpellingsOrthographyInfoList { get; }
    internal string[]?[]? MiscList { get; }

    // Kanji
    public string[]? OnReadings { get; }
    public string[]? KunReadings { get; }
    public string? KanjiComposition { get; }
    public string? KanjiStats { get; }
    // KANJIDIC2
    public string[]? NanoriReadings { get; }
    public string[]? RadicalNames { get; }
    public byte StrokeCount { get; }
    public byte KanjiGrade { get; }
    public Dictionary<string, IList<IDictRecord>>? PitchAccentDict { get; }

    internal LookupResult(
        string primarySpelling,
        string matchedText,
        string deconjugatedMatchedText,
        Dict dict,
        string[]? readings,
        List<LookupFrequencyResult>? frequencies = null,
        string[]? alternativeSpellings = null,
        string[]? primarySpellingOrthographyInfoList = null,
        string[]?[]? readingsOrthographyInfoList = null,
        string[]?[]? alternativeSpellingsOrthographyInfoList = null,
        string[]?[]? miscList = null,
        string[]? onReadings = null,
        string[]? kunReadings = null,
        string[]? nanoriReadings = null,
        string[]? radicalNames = null,
        string? formattedDefinitions = null,
        string? deconjugationProcess = null,
        string? kanjiComposition = null,
        string? kanjiStats = null,
        int edictId = 0,
        byte strokeCount = 0,
        byte kanjiGrade = byte.MaxValue,
        Dictionary<string, IList<IDictRecord>>? pitchAccentDict = null
    )
    {
        MatchedText = matchedText;
        DeconjugatedMatchedText = deconjugatedMatchedText;
        Frequencies = frequencies;
        Dict = dict;
        PrimarySpelling = primarySpelling;
        Readings = readings;
        FormattedDefinitions = formattedDefinitions;
        EdictId = edictId;
        AlternativeSpellings = alternativeSpellings;
        DeconjugationProcess = deconjugationProcess;
        PrimarySpellingOrthographyInfoList = primarySpellingOrthographyInfoList;
        ReadingsOrthographyInfoList = readingsOrthographyInfoList;
        AlternativeSpellingsOrthographyInfoList = alternativeSpellingsOrthographyInfoList;
        MiscList = miscList;
        OnReadings = onReadings;
        KunReadings = kunReadings;
        NanoriReadings = nanoriReadings;
        StrokeCount = strokeCount;
        KanjiComposition = kanjiComposition;
        KanjiStats = kanjiStats;
        KanjiGrade = kanjiGrade;
        RadicalNames = radicalNames;
        PitchAccentDict = pitchAccentDict;
    }
}
