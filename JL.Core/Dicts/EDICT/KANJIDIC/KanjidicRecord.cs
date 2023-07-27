namespace JL.Core.Dicts.EDICT.KANJIDIC;

internal sealed class KanjidicRecord : IDictRecord
{
    private string[]? Definitions { get; }
    public string[]? OnReadings { get; }
    public string[]? KunReadings { get; }
    public string[]? NanoriReadings { get; }
    public int StrokeCount { get; }
    public int Grade { get; }
    public int Frequency { get; }

    public KanjidicRecord(string[]? definitions,
        string[]? onReadings,
        string[]? kunReadings,
        string[]? nanoriReadings,
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
    }

    public string? BuildFormattedDefinition()
    {
        return Definitions is null
            ? null
            : string.Join(", ", Definitions);
    }
}
