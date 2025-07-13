using System.Text.Json;
using System.Text.Json.Serialization;

namespace JL.Core.Mining.Anki;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class Response
{
    // result can be:
    //   null
    //   a number
    //   a boolean
    //   a string
    //   an array of strings
    //   an array of (JSON) objects
    //   an array of booleans
    // /shrug
    [JsonPropertyName("result")] public JsonElement Result { get; init; }

    [JsonPropertyName("error")] public string? Error { get; init; }
}
