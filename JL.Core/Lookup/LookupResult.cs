namespace JL.Core.Lookup;

public class LookupResult
{
    // common (required for sorting)
    public List<string> FoundForm { get; set; } // todo rename foundform to foundtext
    public List<string> Frequency { get; set; }
    public List<string> DictType { get; set; }
    public List<string> FoundSpelling { get; set; }

    public List<string> Readings { get; set; }
    public List<string> Definitions { get; set; }
    public List<string> EdictID { get; set; }
    public List<string> AlternativeSpellings { get; set; }
    public List<string> Process { get; set; }
    public List<string> POrthographyInfoList { get; set; }
    public List<string> ROrthographyInfoList { get; set; }
    public List<string> AOrthographyInfoList { get; set; }

    // KANJIDIC
    public List<string> OnReadings { get; set; }
    public List<string> KunReadings { get; set; }
    public List<string> Nanori { get; set; }
    public List<string> StrokeCount { get; set; }
    public List<string> Composition { get; set; }
    public List<string> Grade { get; set; }
}
