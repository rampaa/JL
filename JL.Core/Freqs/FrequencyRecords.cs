namespace JL.Core.Freqs;

internal record struct FrequencyRecords
{
    private FrequencyRecord _firstRecord;
    private List<FrequencyRecord>? _additionalRecords;

    internal FrequencyRecords(FrequencyRecord record)
    {
        _firstRecord = record;
        _additionalRecords = null;
    }

    internal void Add(FrequencyRecord record)
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

    internal bool AddOrUpdate(FrequencyRecord record, bool higherValueMeansHigherFrequency)
    {
        if (_firstRecord == record)
        {
            if (higherValueMeansHigherFrequency ? _firstRecord.Frequency < record.Frequency : _firstRecord.Frequency > record.Frequency)
            {
                _firstRecord = record;
                return true;
            }

            return false;
        }

        if (_additionalRecords is null)
        {
            _additionalRecords = [record];
            return true;
        }

        int index = _additionalRecords.IndexOf(record);
        if (index < 0)
        {
            _additionalRecords.Add(record);
            return true;
        }

        if (higherValueMeansHigherFrequency ? _additionalRecords[index].Frequency < record.Frequency : _additionalRecords[index].Frequency > record.Frequency)
        {
            _additionalRecords[index] = record;
            return true;
        }

        return false;
    }

    internal readonly FrequencyRecord[] ToArray()
    {
        if (_additionalRecords is null)
        {
            return [_firstRecord];
        }

        FrequencyRecord[] records = new FrequencyRecord[_additionalRecords.Count + 1];
        records[0] = _firstRecord;
        _additionalRecords.CopyTo(records, 1);
        return records;
    }
}
