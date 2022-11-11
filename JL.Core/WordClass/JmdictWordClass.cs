using System.Text.Json.Serialization;

namespace JL.Core.WordClass;

public class JmdictWordClass
{
    [JsonPropertyName("S")] public string Spelling { get; }

    [JsonPropertyName("R")] public List<string>? Readings { get; }

    [JsonPropertyName("C")] public List<string> WordClasses { get; }

    public JmdictWordClass(string spelling, List<string>? readings, List<string> wordClasses)
    {
        Spelling = spelling;
        Readings = readings;
        WordClasses = wordClasses;
    }
}
