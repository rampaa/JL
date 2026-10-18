namespace JL.Core.WordClass;

internal sealed class WordClassCandidate(string? normalizedPrimarySpelling, string[]? readings, string[]? normalizedReadings, string[] wordClasses)
{
    public string? NormalizedPrimarySpelling { get; } = normalizedPrimarySpelling;
    public string[]? Readings { get; } = readings;
    public string[]? NormalizedReadings { get; } = normalizedReadings;
    public string[] WordClasses { get; } = wordClasses;
}
