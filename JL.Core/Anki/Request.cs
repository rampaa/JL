using System.Text.Json.Serialization;

namespace JL.Core.Anki;

public class Request
{
    [JsonPropertyName("action")] public string Action { get; }

    [JsonPropertyName("version")] public int Version { get; }

    [JsonPropertyName("params")] public Dictionary<string, object>? Params { get; }

    public Request(string action, int version, Dictionary<string, object>? @params = null)
    {
        Action = action;
        Version = version;
        Params = @params;
    }
}
