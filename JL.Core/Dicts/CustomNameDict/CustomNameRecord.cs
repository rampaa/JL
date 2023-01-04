using System.Globalization;

namespace JL.Core.Dicts.CustomNameDict;

public class CustomNameRecord : IDictRecord
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

    public string BuildFormattedDefinition()
    {
        return $"({NameType.ToLower(CultureInfo.InvariantCulture)}) {Reading}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        CustomNameRecord customNameRecordObj = (obj as CustomNameRecord)!;

        return PrimarySpelling == customNameRecordObj.PrimarySpelling
               && Reading == customNameRecordObj.Reading
               && NameType == customNameRecordObj.NameType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PrimarySpelling, Reading, NameType);
    }
}
