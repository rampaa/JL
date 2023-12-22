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
    public int EdictId { get; }

    // Word dictionaries
    public string DeconjugatedMatchedText { get; }
    public string? DeconjugationProcess { get; }
    // JMdict, Nazeka EPWING
    public string[]? AlternativeSpellings { get; }
    public string[]? PrimarySpellingOrthographyInfoList { get; }
    public string[]?[]? ReadingsOrthographyInfoList { get; }
    public string[]?[]? AlternativeSpellingsOrthographyInfoList { get; }

    // Kanji
    public string[]? OnReadings { get; }
    public string[]? KunReadings { get; }
    public string? KanjiComposition { get; }
    public string? KanjiStats { get; }
    // KANJIDIC2
    public string[]? NanoriReadings { get; }
    public string[]? RadicalNames { get; }
    public int StrokeCount { get; }
    public int KanjiGrade { get; }
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
        string[]? onReadings = null,
        string[]? kunReadings = null,
        string[]? nanoriReadings = null,
        string[]? radicalNames = null,
        string? formattedDefinitions = null,
        string? deconjugationProcess = null,
        string? kanjiComposition = null,
        string? kanjiStats = null,
        int edictId = 0,
        int strokeCount = 0,
        int kanjiGrade = -1,
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
