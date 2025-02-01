using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

internal readonly struct Rule(string type, string[] decEnd, string[] conEnd, string detail, string? contextRule = null, string[]? decTag = null, string[]? conTag = null)
{
    [JsonPropertyName("type")] public string Type { get; } = type.GetPooledString();
    [JsonPropertyName("dec_end")] public string[] DecEnd { get; } = decEnd;
    [JsonPropertyName("con_end")] public string[] ConEnd { get; } = conEnd;
    [JsonPropertyName("detail")] public string Detail { get; } = detail.GetPooledString();
    [JsonPropertyName("contextrule")] public string? ContextRule { get; } = contextRule?.GetPooledString();
    [JsonPropertyName("dec_tag")] public string[]? DecTag { get; } = decTag;
    [JsonPropertyName("con_tag")] public string[]? ConTag { get; } = conTag;
}
