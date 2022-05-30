using JL.Core.Dicts;

namespace JL.Core.Lookup;

public class LookupResult
{
    // common (required for sorting)
    public string FoundForm { get; init; } // todo rename foundform to foundtext
    public int Frequency { get; init; }
    public Dict? Dict { get; init; }
    public string FoundSpelling { get; init; }

    public List<string>? Readings { get; init; }
    public string? FormattedDefinitions { get; init; }
    public string? EdictId { get; init; }
    public List<string>? AlternativeSpellings { get; init; }
    public string? Process { get; init; }
    public List<string>? POrthographyInfoList { get; init; }
    public List<string>? ROrthographyInfoList { get; init; }
    public List<string>? AOrthographyInfoList { get; init; }

    // KANJIDIC
    public List<string>? OnReadings { get; init; }
    public List<string>? KunReadings { get; init; }
    public List<string>? Nanori { get; init; }
    public int StrokeCount { get; init; }
    public string? Composition { get; init; }
    public int Grade { get; init; }

    public LookupResult()
    {
        Frequency = int.MaxValue;
        StrokeCount = 0;
        Grade = 0;
        FoundForm = string.Empty;
        FoundSpelling = string.Empty;
    }
}
