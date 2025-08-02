using System.Text.Json.Serialization;

namespace JL.Core.Mining.Anki;

public sealed class AnkiConfig(string deckName, string modelName, OrderedDictionary<string, JLField> fields, string[]? tags = null)
{
    [JsonPropertyName("deckName")] public string DeckName { get; } = deckName;
    [JsonPropertyName("modelName")] public string ModelName { get; } = modelName;
    [JsonPropertyName("fields")] public OrderedDictionary<string, JLField> Fields { get; internal set; } = fields;
    [JsonPropertyName("tags")] public string[]? Tags { get; } = tags;
}
