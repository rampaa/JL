using System.Text.Json.Serialization;

namespace JL.Core.Mining.Anki;

// ReSharper disable UnusedMember.Global
internal class Request(string action, int version)
{
    [JsonPropertyName("action")] public string Action { get; } = action;
    [JsonPropertyName("version")] public int Version { get; } = version;
}
