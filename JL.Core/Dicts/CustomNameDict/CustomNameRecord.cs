using JL.Core.Dicts.Interfaces;

namespace JL.Core.Dicts.CustomNameDict;

public sealed record class CustomNameRecord : IDictRecordWithSingleReading
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
}
