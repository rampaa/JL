using System.Text.Json.Serialization;

namespace JL.Core.External.AnkiConnect;

// ReSharper disable UnusedMember.Global
internal sealed class RequestWithParameters<T>(string action, int version, string? key, Dictionary<string, T> parameters) : Request(action, version, key) where T : notnull
{
    [JsonPropertyName("params")] public Dictionary<string, T> Params { get; } = parameters;
}
