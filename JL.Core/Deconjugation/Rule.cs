using System.Text.Json.Serialization;

namespace JL.Core.Deconjugation;

internal sealed class Rule
{
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("contextrule")] public string? ContextRule { get; set; }
    [JsonPropertyName("dec_end")] public string[] DecEnd { get; }
    [JsonPropertyName("con_end")] public string[] ConEnd { get; }
    [JsonPropertyName("dec_tag")] public string[]? DecTag { get; }
    [JsonPropertyName("con_tag")] public string[]? ConTag { get; }
    [JsonPropertyName("detail")] public string Detail { get; set; }

    public Rule(string type, string? contextRule, string[] decEnd, string[] conEnd, string[]? decTag,
        string[]? conTag, string detail)
    {
        Type = type;
        ContextRule = contextRule;
        DecEnd = decEnd;
        ConEnd = conEnd;
        DecTag = decTag;
        ConTag = conTag;
        Detail = detail;
    }
}
