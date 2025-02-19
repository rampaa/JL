using System.Text.Json.Serialization;

namespace JL.Core.Mining.Anki;

// ReSharper disable UnusedMember.Global
internal sealed class Request(string action, int version, Dictionary<string, object>? @params = null)
{
    [JsonPropertyName("action")] public string Action { get; } = action;
    [JsonPropertyName("version")] public int Version { get; } = version;
    [JsonPropertyName("params")] public Dictionary<string, object>? Params { get; } = @params;
}
