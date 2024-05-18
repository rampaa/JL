using System.Text.Json.Serialization;

namespace JL.Core.Mining.Anki;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
internal sealed class Note(
    string deckName,
    string modelName,
    Dictionary<string, object> fields,
    string[] tags,
    Dictionary<string, object>? options,
    Dictionary<string, object>? audio,
    Dictionary<string, object>? picture,
    Dictionary<string, object>? video)
{
    [JsonPropertyName("deckName")] public string DeckName { get; } = deckName;
    [JsonPropertyName("modelName")] public string ModelName { get; } = modelName;
    [JsonPropertyName("fields")] public Dictionary<string, object> Fields { get; } = fields;
    [JsonPropertyName("tags")] public string[] Tags { get; } = tags;
    [JsonPropertyName("options")] public Dictionary<string, object>? Options { get; set; } = options;
    [JsonPropertyName("audio")] public Dictionary<string, object>? Audio { get; set; } = audio;
    [JsonPropertyName("picture")] public Dictionary<string, object>? Picture { get; set; } = picture;
    [JsonPropertyName("video")] public Dictionary<string, object>? Video { get; } = video;
}
