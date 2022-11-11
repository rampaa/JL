using JL.Core.Freqs;

namespace JL.Core.Dicts;

public interface IDictRecordWithGetFrequency : IDictRecord
{
    int GetFrequency(Freq frequency);
}
