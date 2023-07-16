using System.Globalization;

namespace JL.Core.Dicts.CustomNameDict;

internal sealed record class CustomNameRecord : IDictRecord
{
    public string PrimarySpelling { get; }
    public string Reading { get; }
    private string NameType { get; }

    public CustomNameRecord(string primarySpelling, string reading, string nameType)
    {
        PrimarySpelling = primarySpelling;
        Reading = reading;
        NameType = nameType;
    }

#pragma warning disable CA1308
    public string BuildFormattedDefinition()
    {
        return string.Create(CultureInfo.InvariantCulture, $"({NameType.ToLowerInvariant()}) {Reading}");
    }
#pragma warning restore CA1308

}
