namespace JL.Core.Dicts.KANJIDIC;

internal sealed class KanjidicRecord : IDictRecord
{
    public string[]? Definitions { get; }
    public string[]? OnReadings { get; }
    public string[]? KunReadings { get; }
    public string[]? NanoriReadings { get; }
    public string[]? RadicalNames { get; }
    public byte StrokeCount { get; }
    public byte Grade { get; }
    public int Frequency { get; }

    public KanjidicRecord(string[]? definitions,
        string[]? onReadings,
        string[]? kunReadings,
        string[]? nanoriReadings,
        string[]? radicalNames,
        byte strokeCount,
        byte grade,
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
