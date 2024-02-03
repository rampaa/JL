using System.Text.Json.Serialization;

namespace JL.Core.Mining.Anki;

internal sealed class Note
{
    [JsonPropertyName("deckName")] public string DeckName { get; }

    [JsonPropertyName("modelName")] public string ModelName { get; }

    [JsonPropertyName("fields")] public Dictionary<string, object> Fields { get; }

    [JsonPropertyName("options")] public Dictionary<string, object> Options { get; }

    [JsonPropertyName("tags")] public string[] Tags { get; }

    [JsonPropertyName("audio")] public Dictionary<string, object>? Audio { get; }

    [JsonPropertyName("video")] public Dictionary<string, object>? Video { get; }

    [JsonPropertyName("picture")] public Dictionary<string, object>? Picture { get; }

    public Note(
        string deckName,
        string modelName,
        Dictionary<string, object> fields,
        Dictionary<string, object> options,
        string[] tags,
        Dictionary<string, object>? audio,
        Dictionary<string, object>? video,
        Dictionary<string, object>? picture
    )
    {
        DeckName = deckName;
        ModelName = modelName;
        Fields = fields;
        Options = options;
        Tags = tags;
        Audio = audio;
        Video = video;
        Picture = picture;
    }
}
