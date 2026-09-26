namespace JL.Core.Dicts.EPWING.Nazeka;

internal readonly record struct EpwingNazekaVariantSource(
    long RowId,
    long EntryRowId,
    string PrimarySpelling,
    string? Reading);
