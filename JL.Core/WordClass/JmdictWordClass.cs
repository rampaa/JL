using System.Text.Json.Serialization;

namespace JL.Core.WordClass;

internal sealed class JmdictWordClass(string spelling, string[] wordClasses, string[]? readings = null)
{
    [JsonPropertyName("S")] public string Spelling { get; set; } = spelling;
    [JsonPropertyName("C")] public string[] WordClasses { get; } = wordClasses;
    [JsonPropertyName("R")] public string[]? Readings { get; } = readings;
}
