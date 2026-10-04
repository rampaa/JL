using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts;

internal record struct DictRecords<TRecord> where TRecord : class, IDictRecord, IEquatable<TRecord>
{
    private readonly TRecord _firstRecord;
    private List<TRecord>? _additionalRecords;

    internal DictRecords(TRecord record)
    {
        _firstRecord = record;
        _additionalRecords = null;
    }

    internal void Add(TRecord record)
    {
        if (_additionalRecords is null)
        {
            _additionalRecords = [record];
        }
        else
        {
            _additionalRecords.Add(record);
        }
    }

    internal bool AddIfNotExists(TRecord record)
    {
        if (_firstRecord.Equals(record))
        {
            return false;
        }

        if (_additionalRecords is null)
        {
            _additionalRecords = [record];
            return true;
        }

        if (_additionalRecords.AsReadOnlySpan().Contains(record))
        {
            return false;
        }

        _additionalRecords.Add(record);
        return true;
    }

    internal readonly IList<IDictRecord> ToArray()
    {
        if (_additionalRecords is null)
        {
            IDictRecord[] singleRecord = [_firstRecord];
            return singleRecord;
        }

        IDictRecord[] records = new IDictRecord[_additionalRecords.Count + 1];
        records[0] = _firstRecord;
        ReadOnlySpan<TRecord> additionalRecords = _additionalRecords.AsReadOnlySpan();
        for (int i = 0; i < additionalRecords.Length; i++)
        {
            records[i + 1] = additionalRecords[i];
        }

        return records;
    }
}
