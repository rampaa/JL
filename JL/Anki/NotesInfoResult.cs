using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JL.Anki
{
    internal class NotesInfoResult
    {
        [JsonPropertyName("fields")] public Dictionary<string, Dictionary<string, object>> Fields { get; set; }
    }
}
