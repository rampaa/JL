using System.Text.Json.Serialization;

namespace JL.Core.Mining.Anki;

internal sealed class Note(
    string deckName,
    string modelName,
    Dictionary<string, object> fields,
    Dictionary<string, object> options,
    string[] tags,
    Dictionary<string, object>? audio,
    Dictionary<string, object>? video,
    Dictionary<string, object>? picture)
{
    [JsonPropertyName("deckName")] public string DeckName { get; } = deckName;

    [JsonPropertyName("modelName")] public string ModelName { get; } = modelName;

    [JsonPropertyName("fields")] public Dictionary<string, object> Fields { get; } = fields;

    [JsonPropertyName("options")] public Dictionary<string, object> Options { get; } = options;

    [JsonPropertyName("tags")] public string[] Tags { get; } = tags;

    [JsonPropertyName("audio")] public Dictionary<string, object>? Audio { get; } = audio;

    [JsonPropertyName("video")] public Dictionary<string, object>? Video { get; } = video;

    [JsonPropertyName("picture")] public Dictionary<string, object>? Picture { get; } = picture;
}
