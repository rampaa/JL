using System.Diagnostics.CodeAnalysis;
using JL.Core.Dicts.Interfaces;
using JL.Core.Frontend;

namespace JL.Core.Dicts.CustomNameDict;

internal sealed class CustomNameRecord : IDictRecord, IEquatable<CustomNameRecord>
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    private string NameType { get; }
    private string? ExtraInfo { get; }
    public ImageInfo? ImageInfo { get; }

    public CustomNameRecord(string primarySpelling, string? reading, string nameType, string? extraInfo, ImageInfo? imageInfo)
    {
        PrimarySpelling = primarySpelling;
        Reading = reading;
        NameType = nameType;
        ExtraInfo = extraInfo;
        ImageInfo = imageInfo;
    }

    public string BuildFormattedDefinition()
    {
        return $"[{NameType}] {Reading ?? PrimarySpelling}{(ExtraInfo is not null ? $"\n{ExtraInfo}" : "")}";
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is CustomNameRecord other
               && PrimarySpelling == other.PrimarySpelling
               && Reading == other.Reading
               && NameType == other.NameType
               && ExtraInfo == other.ExtraInfo
               && ImageInfo == other.ImageInfo;
    }

    public bool Equals([NotNullWhen(true)] CustomNameRecord? other)
    {
        return other is not null
               && PrimarySpelling == other.PrimarySpelling
               && Reading == other.Reading
               && NameType == other.NameType
               && ExtraInfo == other.ExtraInfo
               && ImageInfo == other.ImageInfo;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PrimarySpelling.GetHashCode(StringComparison.Ordinal),
            Reading?.GetHashCode(StringComparison.Ordinal) ?? 37,
            NameType.GetHashCode(StringComparison.Ordinal),
            ExtraInfo?.GetHashCode(StringComparison.Ordinal) ?? 37,
            ImageInfo?.GetHashCode() ?? 37);
    }

    public static bool operator ==(CustomNameRecord? left, CustomNameRecord? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(CustomNameRecord? left, CustomNameRecord? right) => !(left == right);
}
