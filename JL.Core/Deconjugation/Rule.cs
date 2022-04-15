using System.Text.Json.Serialization;

namespace JL.Core.Deconjugation
{
    public class Rule
    {
        [JsonPropertyName("type")] public string Type { get; }
        [JsonPropertyName("contextrule")] public string? Contextrule { get; }
        [JsonPropertyName("dec_end")] public List<string> DecEnd { get; }
        [JsonPropertyName("con_end")] public List<string> ConEnd { get; }
        [JsonPropertyName("dec_tag")] public List<string>? DecTag { get; }
        [JsonPropertyName("con_tag")] public List<string>? ConTag { get; }
        [JsonPropertyName("detail")] public string Detail { get; }

        public Rule(string type, string? contextrule, List<string> decEnd, List<string> conEnd, List<string>? decTag,
            List<string>? conTag, string detail)
        {
            Type = type;
            Contextrule = contextrule;
            DecEnd = decEnd;
            ConEnd = conEnd;
            DecTag = decTag;
            ConTag = conTag;
            Detail = detail;
        }
    }
}
