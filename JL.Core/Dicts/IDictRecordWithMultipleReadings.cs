namespace JL.Core.Dicts;
internal interface IDictRecordWithMultipleReadings : IDictRecord
{
    public string PrimarySpelling { get; }
    public string[]? Readings { get; }
}
