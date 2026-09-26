namespace JL.Core.Dicts.EPWING.Nazeka;

internal readonly record struct EpwingNazekaPreparedRecord(
    string PrimarySpelling,
    string? Reading,
    byte[]? AlternativeSpellings,
    byte[] Definitions,
    byte[]? ImageInfo,
    bool IsFirstInEntry,
    int SearchKeyOffset,
    int SearchKeyCount);
