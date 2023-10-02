using System.Globalization;

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

#pragma warning disable CA1308
    public string BuildFormattedDefinition()
    {
        string extraInfo = ExtraInfo is not null
            ? string.Create(CultureInfo.InvariantCulture, $"\n{ExtraInfo}")
            : "";

        return string.Create(CultureInfo.InvariantCulture, $"({NameType.ToLowerInvariant()}) {Reading ?? PrimarySpelling}{extraInfo}");
    }
#pragma warning restore CA1308

}
