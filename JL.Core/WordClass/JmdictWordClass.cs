using System.Text.Json.Serialization;

namespace JL.Core.WordClass;

internal sealed class JmdictWordClass(string spelling, string[]? readings, string[] wordClasses)
{
    [JsonPropertyName("S")] public string Spelling { get; set; } = spelling;

    [JsonPropertyName("R")] public string[]? Readings { get; } = readings;

    [JsonPropertyName("C")] public string[] WordClasses { get; } = wordClasses;
}
