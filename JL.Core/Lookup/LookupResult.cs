using JL.Core.Dicts;

namespace JL.Core.Lookup;

public class LookupResult
{
    // common (required for sorting)
    public string FoundForm { get; set; } // todo rename foundform to foundtext
    public int Frequency { get; set; }
    public Dict? Dict { get; set; }
    public string FoundSpelling { get; set; }

    public List<string>? Readings { get; set; }
    public string? FormattedDefinitions { get; set; }
    public string? EdictId { get; set; }
    public List<string>? AlternativeSpellings { get; set; }
    public string? Process { get; set; }
    public List<string>? POrthographyInfoList { get; set; }
    public List<string>? ROrthographyInfoList { get; set; }
    public List<string>? AOrthographyInfoList { get; set; }

    // KANJIDIC
    public List<string>? OnReadings { get; set; }
    public List<string>? KunReadings { get; set; }
    public List<string>? Nanori { get; set; }
    public int StrokeCount { get; set; }
    public string? Composition { get; set; }
    public int Grade { get; set; }

    public LookupResult()
    {
        Frequency = int.MaxValue;
        StrokeCount = 0;
        Grade = 0;
        FoundForm = string.Empty;
        FoundSpelling = string.Empty;
    }
}
