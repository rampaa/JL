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
        return $"({NameType}) {Reading ?? PrimarySpelling}\n{ExtraInfo ?? ""}";
    }
}
