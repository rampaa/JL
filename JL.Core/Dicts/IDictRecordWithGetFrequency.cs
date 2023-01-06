using JL.Core.Freqs;

namespace JL.Core.Dicts;

internal interface IDictRecordWithGetFrequency : IDictRecord
{
    int GetFrequency(Freq freq);
}
