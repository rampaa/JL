namespace JL.Core.Lookup;

public sealed class JmdictLookupResult(
    string[]? primarySpellingOrthographyInfoList,
    string[]?[]? readingsOrthographyInfoList,
    string[]?[]? alternativeSpellingsOrthographyInfoList,
    string[]? miscSharedByAllSensesList,
    string[]?[]? miscList,
    string[]?[]? wordClassesForSenses)
{
    public string[]? PrimarySpellingOrthographyInfoList { get; } = primarySpellingOrthographyInfoList;
    public string[]?[]? ReadingsOrthographyInfoList { get; } = readingsOrthographyInfoList;
    public string[]?[]? AlternativeSpellingsOrthographyInfoList { get; } = alternativeSpellingsOrthographyInfoList;
    internal string[]? MiscSharedByAllSenses { get; } = miscSharedByAllSensesList;
    internal string[]?[]? MiscList { get; } = miscList;
    internal string[]?[]? WordClassesForSenses { get; } = wordClassesForSenses;
}
