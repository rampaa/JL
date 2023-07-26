using System.Text.Json.Serialization;

namespace JL.Core.WordClass;

internal sealed class JmdictWordClass
{
    [JsonPropertyName("S")] public string Spelling { get; }

    [JsonPropertyName("R")] public string[]? Readings { get; }

    [JsonPropertyName("C")] public string[] WordClasses { get; }

    public JmdictWordClass(string spelling, string[]? readings, string[] wordClasses)
    {
        Spelling = spelling;
        Readings = readings;
        WordClasses = wordClasses;
    }
}
