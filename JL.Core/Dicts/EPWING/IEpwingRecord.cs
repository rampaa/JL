namespace JL.Core.Dicts.EPWING;

internal interface IEpwingRecord : IDictRecord
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    public string[]? Definitions { get; set; }
}
