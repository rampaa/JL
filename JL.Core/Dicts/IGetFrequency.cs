using JL.Core.Freqs;

namespace JL.Core.Dicts;

internal interface IGetFrequency
{
    public int GetFrequency(Freq freq);

    public int GetFrequencyFromDB(Dictionary<string, List<FrequencyRecord>> freqDict);
}
