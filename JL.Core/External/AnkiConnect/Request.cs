using System.Text.Json.Serialization;

namespace JL.Core.External.AnkiConnect;

// ReSharper disable UnusedMember.Global
internal class Request(string action, int version, string? key)
{
    [JsonPropertyName("action")] public string Action { get; } = action;
    [JsonPropertyName("version")] public int Version { get; } = version;
    [JsonPropertyName("key")] public string? Key { get; } = key;
}
