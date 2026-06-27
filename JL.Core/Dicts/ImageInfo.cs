namespace JL.Core.Dicts;

public sealed class ImageInfo(string path, int width, int height) : IEquatable<ImageInfo>
{
    public string Path { get; } = path;
    public int Width { get; } = width;
    public int Height { get; } = height;

    public bool Equals(ImageInfo? other)
    {
        return other is not null
            && Path == other.Path
            && Width == other.Width
            && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is ImageInfo other
            && Path == other.Path
            && Width == other.Width
            && Height == other.Height;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Path.GetHashCode(StringComparison.Ordinal), Width, Height);
    }

    public static bool operator ==(ImageInfo? left, ImageInfo? right) => left is not null ? left.Equals(right) : right is null;

    public static bool operator !=(ImageInfo? left, ImageInfo? right) => !(left == right);
}
