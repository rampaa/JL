using System.Text.Json.Serialization;

namespace JL.Core.Deconjugation;

internal sealed class Rule(string type, string[] decEnd, string[] conEnd, string detail, string? contextRule = null, string[]? decTag = null, string[]? conTag = null)
{
    [JsonPropertyName("type")] public string Type { get; set; } = type;
    [JsonPropertyName("dec_end")] public string[] DecEnd { get; } = decEnd;
    [JsonPropertyName("con_end")] public string[] ConEnd { get; } = conEnd;
    [JsonPropertyName("detail")] public string Detail { get; set; } = detail;
    [JsonPropertyName("contextrule")] public string? ContextRule { get; set; } = contextRule;
    [JsonPropertyName("dec_tag")] public string[]? DecTag { get; } = decTag;
    [JsonPropertyName("con_tag")] public string[]? ConTag { get; } = conTag;
}
