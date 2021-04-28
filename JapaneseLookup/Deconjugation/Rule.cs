using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JapaneseLookup.Deconjugation
{
    internal class Rule
    {
        public Rule(string type, List<string> decEnd, List<string> conEnd, List<string> decTag,
            List<string> conTag, string detail)
        {
            Type = type;
            DecEnd = decEnd;
            ConEnd = conEnd;
            DecTag = decTag;
            ConTag = conTag;
            Detail = detail;
        }

        [JsonPropertyName("type")] public string Type { get; }
        [JsonPropertyName("dec_end")] public List<string> DecEnd { get; }
        [JsonPropertyName("con_end")] public List<string> ConEnd { get; }
        [JsonPropertyName("dec_tag")] public List<string> DecTag { get; }
        [JsonPropertyName("con_tag")] public List<string> ConTag { get; }
        [JsonPropertyName("detail")] public string Detail { get; }
    }
}