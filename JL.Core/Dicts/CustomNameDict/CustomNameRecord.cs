namespace JL.Core.Dicts.CustomNameDict;

internal sealed record class CustomNameRecord : IDictRecord
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
        string extraInfo = ExtraInfo is not null
            ? $"\n{ExtraInfo}"
            : "";

#pragma warning disable CA1308 // Normalize strings to uppercase
        return $"({NameType.ToLowerInvariant()}) {Reading ?? PrimarySpelling}{extraInfo}";
#pragma warning restore CA1308 // Normalize strings to uppercase
    }
}
