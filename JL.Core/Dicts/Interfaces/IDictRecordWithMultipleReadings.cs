namespace JL.Core.Dicts.Interfaces;
internal interface IDictRecordWithMultipleReadings : IDictRecord
{
    public string PrimarySpelling { get; }
    public string[]? Readings { get; }
}
