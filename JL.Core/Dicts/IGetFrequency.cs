using JL.Core.Freqs;

namespace JL.Core.Dicts;

internal interface IGetFrequency
{
    int GetFrequency(Freq freq);

    int GetFrequencyFromDB(IDictionary<string, List<FrequencyRecord>> freqDict);
}
