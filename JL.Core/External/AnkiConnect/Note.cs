using System.Text.Json.Serialization;

namespace JL.Core.External.AnkiConnect;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
internal sealed class Note(
    string deckName,
    string modelName,
    Dictionary<string, string> fields,
    Dictionary<string, object?>? options,
    string[]? tags = null,
    Dictionary<string, object>[]? audios = null,
    Dictionary<string, object>[]? pictures = null)
{
    [JsonPropertyName("deckName")] public string DeckName { get; } = deckName;
    [JsonPropertyName("modelName")] public string ModelName { get; } = modelName;
    [JsonPropertyName("fields")] public Dictionary<string, string> Fields { get; } = fields;
    [JsonPropertyName("options")] public Dictionary<string, object?>? Options { get; set; } = options;
    [JsonPropertyName("tags")] public string[]? Tags { get; set; } = tags;
    [JsonPropertyName("audio")] public Dictionary<string, object>[]? Audios { get; set; } = audios;
    [JsonPropertyName("picture")] public Dictionary<string, object>[]? Pictures { get; set; } = pictures;
    // [JsonPropertyName("video")] public Dictionary<string, object>[]? Videos { get; } = videos;
}
