using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JapaneseLookup.Anki
{
    public class Note
    {
        [JsonPropertyName("deckName")] public string DeckName { get; set; }

        [JsonPropertyName("modelName")]  public string ModelName { get; set; }

        [JsonPropertyName("fields")] public Dictionary<string, object> Fields { get; set; }

        [JsonPropertyName("options")] public Dictionary<string, object> Options { get; set; }

        [JsonPropertyName("tags")]  public string[] Tags { get; set; }

        [JsonPropertyName("audio")] public Dictionary<string, object>[] Audio { get; set; }

        [JsonPropertyName("video")] public Dictionary<string, object>[] Video { get; set; }

        [JsonPropertyName("picture")] public Dictionary<string, object>[] Picture { get; set; }

        public Note(
            string deckName,
            string modelName,
            Dictionary<string, object> fields,
            Dictionary<string, object> options,
            string[] tags,
            Dictionary<string, object>[] audio,
            Dictionary<string, object>[] video,
            Dictionary<string, object>[] picture
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
}