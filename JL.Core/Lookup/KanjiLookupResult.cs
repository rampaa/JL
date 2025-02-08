namespace JL.Core.Lookup;

public sealed class KanjiLookupResult(
    string[]? onReadings,
    string[]? kunReadings,
    string? kanjiComposition,
    string[]? nanoriReadings = null,
    string[]? radicalNames = null,
    byte strokeCount = 0,
    byte kanjiGrade = byte.MaxValue,
    string? kanjiStats = null)
{
    public string[]? OnReadings { get; } = onReadings;
    public string[]? KunReadings { get; } = kunReadings;
    public string? KanjiComposition { get; } = kanjiComposition;
    public string? KanjiStats { get; } = kanjiStats;

    // KANJIDIC2
    public string[]? NanoriReadings { get; } = nanoriReadings;
    public string[]? RadicalNames { get; } = radicalNames;
    public byte StrokeCount { get; } = strokeCount;
    public byte KanjiGrade { get; } = kanjiGrade;
}
