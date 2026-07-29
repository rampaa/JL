namespace JL.Core.WordClass;

internal class WordClassCandidate(string primarySpelling, string? normalizedPrimarySpelling, string[]? readings, string[]? normalizedReadings, string[] wordClasses)
{
    public string PrimarySpelling { get; } = primarySpelling;
    public string? NormalizedPrimarySpelling { get; } = normalizedPrimarySpelling;
    public string[]? Readings { get; } = readings;
    public string[]? NormalizedReadings { get; } = normalizedReadings;
    public string[] WordClasses { get; } = wordClasses;
}
