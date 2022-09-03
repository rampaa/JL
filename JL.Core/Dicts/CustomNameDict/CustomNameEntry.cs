namespace JL.Core.Dicts.CustomNameDict;

public class CustomNameEntry : IResult
{
    public string PrimarySpelling { get; }
    public string Reading { get; }
    private string NameType { get; }

    public CustomNameEntry(string primarySpelling, string reading, string nameType)
    {
        PrimarySpelling = primarySpelling;
        Reading = reading;
        NameType = nameType;
    }

    public string BuildFormattedDefinition()
    {
        return $"({NameType.ToLower()}) {Reading}";
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;

        CustomNameEntry customNameEntryObj = (obj as CustomNameEntry)!;

        return PrimarySpelling == customNameEntryObj.PrimarySpelling
               && Reading == customNameEntryObj.Reading
               && NameType == customNameEntryObj.NameType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PrimarySpelling, Reading, NameType);
    }
}
