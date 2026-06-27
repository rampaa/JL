using System.Text.Json.Serialization;
using MessagePack;

namespace JL.Core.Dicts;

[method: JsonConstructor]
public sealed record class ImageInfo([property: Key(0)] string Path, [property: Key(1)] int Width, [property: Key(2)] int Height);
