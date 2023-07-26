namespace JL.Core.Dicts.EDICT.KANJIDIC;

internal sealed class KanjidicRecord : IDictRecord
{
    public string[]? Definitions { get; set; }
    public string[]? OnReadings { get; set; }
    public string[]? KunReadings { get; set; }
    public string[]? NanoriReadings { get; set; }
    public int StrokeCount { get; set; }
    public int Grade { get; set; }
    public int Frequency { get; set; }

    public KanjidicRecord(string[]? definitions, string[]? onReadings, string[]? kunReadings, string[]? nanoriReadings, int strokeCount, int grade, int frequency)
    {
        Definitions = definitions;
        OnReadings = onReadings;
        KunReadings = kunReadings;
        NanoriReadings = nanoriReadings;
        StrokeCount = strokeCount;
        Grade = grade;
        Frequency = frequency;
    }

    public string? BuildFormattedDefinition()
    {
        return Definitions is null
            ? null
            : string.Join(", ", Definitions);
    }
}
