using JL.Core.Dicts;

namespace JL.Core.Lookup;

public class LookupResult
{
    // common (required for sorting)
    public string MatchedText { get; }
    public string DeconjugatedMatchedText { get; init; }
    public List<LookupFrequencyResult>? Frequencies { get; init; }
    public Dict Dict { get; init; }
    public string PrimarySpelling { get; init; }

    public List<string>? Readings { get; init; }
    public string? FormattedDefinitions { get; init; }
    public int EdictId { get; init; }
    public List<string>? AlternativeSpellings { get; init; }
    public string? Process { get; init; }
    public List<string>? POrthographyInfoList { get; init; }
    public List<string>? ROrthographyInfoList { get; init; }
    public List<string>? AOrthographyInfoList { get; init; }

    // Kanji
    public List<string>? OnReadings { get; init; }
    public List<string>? KunReadings { get; init; }
    public List<string>? NanoriReadings { get; init; }
    public int StrokeCount { get; init; }
    public string? KanjiComposition { get; init; }
    public int KanjiGrade { get; init; }
    public string? KanjiStats { get; init; }

    public LookupResult(
        string primarySpelling,
        string matchedText,
        string deconjugatedMatchedText,
        Dict dict,
        List<string>? readings,
        List<LookupFrequencyResult>? frequencies = null,
        List<string>? alternativeSpellings = null,
        List<string>? pOrthographyInfoList = null,
        List<string>? rOrthographyInfoList = null,
        List<string>? aOrthographyInfoList = null,
        List<string>? onReadings = null,
        List<string>? kunReadings = null,
        List<string>? nanoriReadings = null,
        string? formattedDefinitions = null,
        string? process = null,
        string? kanjiComposition = null,
        string? kanjiStats = null,
        int edictId = 0,
        int strokeCount = 0,
        int kanjiGrade = 0
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
        Process = process;
        POrthographyInfoList = pOrthographyInfoList;
        ROrthographyInfoList = rOrthographyInfoList;
        AOrthographyInfoList = aOrthographyInfoList;
        OnReadings = onReadings;
        KunReadings = kunReadings;
        NanoriReadings = nanoriReadings;
        StrokeCount = strokeCount;
        KanjiComposition = kanjiComposition;
        KanjiStats = kanjiStats;
        KanjiGrade = kanjiGrade;
    }
}
