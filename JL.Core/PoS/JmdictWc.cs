using System.Text.Json.Serialization;

namespace JL.Core.PoS
{
    public class JmdictWc
    {
        [JsonPropertyName("S")] public string Spelling { get; set; }

        [JsonPropertyName("R")] public List<string> Readings { get; set; }

        [JsonPropertyName("C")] public List<string> WordClasses { get; set; }

        public JmdictWc(string spelling, List<string> readings, List<string> wordClasses)
        {
            Spelling = spelling;
            Readings = readings;
            WordClasses = wordClasses;
        }
    }
}
