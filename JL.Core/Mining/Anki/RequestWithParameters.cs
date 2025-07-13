using System.Text.Json.Serialization;

namespace JL.Core.Mining.Anki;

// ReSharper disable UnusedMember.Global
internal sealed class RequestWithParameters<T>(string action, int version, Dictionary<string, T> @params) : Request(action, version) where T : notnull
{
    [JsonPropertyName("params")] public Dictionary<string, T> Params { get; } = @params;
}
