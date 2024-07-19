namespace JL.Core.Dicts.EPWING;

internal interface IEpwingRecord : IDictRecord
{
    public string? Reading { get; }
    public string[] Definitions { get; }
}
