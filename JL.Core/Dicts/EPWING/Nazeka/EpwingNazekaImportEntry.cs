namespace JL.Core.Dicts.EPWING.Nazeka;

internal readonly record struct EpwingNazekaImportEntry(
    string Reading,
    List<string>? Spellings,
    List<string> Definitions,
    string? ImagePath);
