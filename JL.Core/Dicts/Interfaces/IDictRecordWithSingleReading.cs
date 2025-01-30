namespace JL.Core.Dicts.Interfaces;

internal interface IDictRecordWithSingleReading : IDictRecord
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
}
