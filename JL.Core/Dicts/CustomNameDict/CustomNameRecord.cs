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

        return $"({NameType}) {Reading ?? PrimarySpelling}{extraInfo}";
    }
}
