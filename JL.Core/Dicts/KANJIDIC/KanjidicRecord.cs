namespace JL.Core.Dicts.KANJIDIC;

internal sealed class KanjidicRecord(
    string[]? definitions,
    string[]? onReadings,
    string[]? kunReadings,
    string[]? nanoriReadings,
    string[]? radicalNames,
    byte strokeCount,
    byte grade,
    int frequency) : IDictRecord
{
    public string[]? Definitions { get; } = definitions;
    public string[]? OnReadings { get; } = onReadings;
    public string[]? KunReadings { get; } = kunReadings;
    public string[]? NanoriReadings { get; } = nanoriReadings;
    public string[]? RadicalNames { get; } = radicalNames;
    public byte StrokeCount { get; } = strokeCount;
    public byte Grade { get; } = grade;
    public int Frequency { get; } = frequency;

    public string? BuildFormattedDefinition()
    {
        return Definitions is null
            ? null
            : string.Join(", ", Definitions);
    }
}
