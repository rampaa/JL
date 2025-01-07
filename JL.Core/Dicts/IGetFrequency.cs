using JL.Core.Freqs;

namespace JL.Core.Dicts;

internal interface IGetFrequency
{
    public int GetFrequency(IDictionary<string, IList<FrequencyRecord>> freqDict);

    public int GetFrequency(Dictionary<string, List<FrequencyRecord>> freqDict);
}
