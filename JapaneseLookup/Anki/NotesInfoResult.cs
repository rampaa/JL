using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JapaneseLookup.Anki
{
    internal class NotesInfoResult
    {
        [JsonPropertyName("fields")] public Dictionary<string, Dictionary<string, object>> Fields { get; set; }
    }
}