namespace JL.Core.Dicts.Interfaces;

internal interface IDictRecordWithSingleReading : IDictRecord
{
    public string? Reading { get; }
}
