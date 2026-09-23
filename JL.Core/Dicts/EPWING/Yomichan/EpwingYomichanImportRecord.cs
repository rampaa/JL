namespace JL.Core.Dicts.EPWING.Yomichan;

internal readonly record struct EpwingYomichanImportRecord(string PrimarySpelling, string? Reading, double PopularityScore,
    byte[] Definitions, byte[]? WordClasses, byte[]? DefinitionTags, byte[]? ImageInfos,
    string SearchKey, string? AdditionalSearchKey);
