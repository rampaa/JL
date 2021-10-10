using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JapaneseLookup.Anki
{
    public class Request
    {
        [JsonPropertyName("action")] public string Action { get; set; }

        [JsonPropertyName("version")] public int Version { get; set; }

        [JsonPropertyName("params")] public Dictionary<string, object> Params { get; set; }

        public Request(string action, int version, Dictionary<string, object> @params = null)
        {
            Action = action;
            Version = version;
            Params = @params;
        }
    }
}