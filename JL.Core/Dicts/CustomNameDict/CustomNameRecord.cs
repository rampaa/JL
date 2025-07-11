using JL.Core.Dicts.Interfaces;

namespace JL.Core.Dicts.CustomNameDict;

internal sealed class CustomNameRecord : IDictRecordWithSingleReading, IEquatable<CustomNameRecord>
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    private string NameType { get; }
    private string? ExtraInfo { get; }

    public CustomNameRecord(string primarySpelling, string? reading, string nameType, string? extraInfo)
    {
        PrimarySpelling = primarySpelling;
        Reading = reading;
        NameType = nameType;
        ExtraInfo = extraInfo;
    }

    public string BuildFormattedDefinition()
    {
        return $"[{NameType}] {Reading ?? PrimarySpelling}{(ExtraInfo is not null ? $"\n{ExtraInfo}" : "")}";
    }

    public override bool Equals(object? obj)
    {
        return obj is CustomNameRecord customNameRecord
               && PrimarySpelling == customNameRecord.PrimarySpelling
               && Reading == customNameRecord.Reading
               && NameType == customNameRecord.NameType
               && ExtraInfo == customNameRecord.ExtraInfo;
    }

    public bool Equals(CustomNameRecord? other)
    {
        return other is not null
               && PrimarySpelling == other.PrimarySpelling
               && Reading == other.Reading
               && NameType == other.NameType
               && ExtraInfo == other.ExtraInfo;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PrimarySpelling.GetHashCode(StringComparison.Ordinal),
            Reading?.GetHashCode(StringComparison.Ordinal) ?? 37,
            NameType.GetHashCode(StringComparison.Ordinal),
            ExtraInfo?.GetHashCode(StringComparison.Ordinal) ?? 37);
    }

    public static bool operator ==(CustomNameRecord? left, CustomNameRecord? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(CustomNameRecord? left, CustomNameRecord? right) => !left?.Equals(right) ?? right is not null;
}
