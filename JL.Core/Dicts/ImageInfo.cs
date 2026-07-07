using System.Text.Json.Serialization;
using MessagePack;

namespace JL.Core.Dicts;

[MessagePackObject]
[method: JsonConstructor]
public sealed record class ImageInfo([property: Key(0)] string Path, [property: Key(1)] int PixelWidth, [property: Key(2)] int PixelHeight, [property: Key(3)] double Width, [property: Key(4)] double Height);
