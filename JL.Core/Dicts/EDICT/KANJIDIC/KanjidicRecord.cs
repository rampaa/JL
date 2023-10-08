namespace JL.Core.Dicts.EDICT.KANJIDIC;

internal sealed class KanjidicRecord : IDictRecord
{
    private string[]? Definitions { get; }
    public string[]? OnReadings { get; }
    public string[]? KunReadings { get; }
    public string[]? NanoriReadings { get; }
    public string[]? RadicalNames { get; }
    public int StrokeCount { get; }
    public int Grade { get; }
    public int Frequency { get; }

    public KanjidicRecord(string[]? definitions,
        string[]? onReadings,
        string[]? kunReadings,
        string[]? nanoriReadings,
        string[]? radicalNames,
        int strokeCount,
        int grade,
        int frequency)
    {
        Definitions = definitions;
        OnReadings = onReadings;
        KunReadings = kunReadings;
        NanoriReadings = nanoriReadings;
        StrokeCount = strokeCount;
        Grade = grade;
        Frequency = frequency;
        RadicalNames = radicalNames;
    }

    public string? BuildFormattedDefinition()
    {
        return Definitions is null
            ? null
            : string.Join(", ", Definitions);
    }
}
