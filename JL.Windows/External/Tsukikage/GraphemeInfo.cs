using System.Text.Json.Serialization;

namespace JL.Windows.External.Tsukikage;

#pragma warning disable CA1812 // Internal class that is apparently never instantiated
internal sealed class GraphemeInfo(int graphemeStartIndex, string text, bool isVertical)
#pragma warning restore CA1812 // Internal class that is apparently never instantiated
{
    [JsonPropertyName("i")] public int GraphemeStartIndex { get; } = graphemeStartIndex;
    [JsonPropertyName("t")] public string Text { get; } = text;
    [JsonPropertyName("v")] public bool IsVertical { get; } = isVertical;
}
