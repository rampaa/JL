using System.Text.Json.Serialization;

namespace JL.Core.Deconjugation;

internal sealed class Rule(string type, string? contextRule, string[] decEnd, string[] conEnd, string[]? decTag, string[]? conTag, string detail)
{
    [JsonPropertyName("type")] public string Type { get; set; } = type;
    [JsonPropertyName("contextrule")] public string? ContextRule { get; set; } = contextRule;
    [JsonPropertyName("dec_end")] public string[] DecEnd { get; } = decEnd;
    [JsonPropertyName("con_end")] public string[] ConEnd { get; } = conEnd;
    [JsonPropertyName("dec_tag")] public string[]? DecTag { get; } = decTag;
    [JsonPropertyName("con_tag")] public string[]? ConTag { get; } = conTag;
    [JsonPropertyName("detail")] public string Detail { get; set; } = detail;
}
