namespace JL.Core.Dicts.EPWING;

internal interface IEpwingRecord : IDictRecord
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    public List<string>? Definitions { get; set; }
}
