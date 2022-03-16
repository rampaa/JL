using System.Text.Json.Serialization;

namespace JL.Core.Anki
{
    internal class NotesInfoResult
    {
        [JsonPropertyName("fields")] public Dictionary<string, Dictionary<string, object>> Fields { get; set; }
    }
}
