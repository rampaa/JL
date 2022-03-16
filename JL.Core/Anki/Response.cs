using System.Text.Json.Serialization;

namespace JL.Core.Anki
{
    public class Response
    {
        // result can be: 
        //   a number
        //   an array of strings
        //   an array of (JSON) objects
        //   an array of booleans
        // /shrug
        [JsonPropertyName("result")] public object Result { get; set; }

        [JsonPropertyName("error")] public object Error { get; set; }
    }
}
