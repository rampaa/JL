using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JapaneseLookup.PoS
{
    public class JmdictWC
    {
        [JsonPropertyName("S")]
        public string Spelling { get; set; }

        [JsonPropertyName("R")]
        public List<string> Readings { get; set; }

        [JsonPropertyName("C")]
        public List<string> WordClasses { get; set; }
        public JmdictWC() : this(null, new List<string>(), new List<string>()) { }
        public JmdictWC(string spelling, List<string> readings, List<string> wordClasses)
        {
            Spelling = spelling;
            Readings = readings;
            WordClasses = wordClasses;
        }
    }
}
