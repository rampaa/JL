using JL.Core.Freqs;

namespace JL.Core.Dicts;

internal interface IGetFrequency
{
    int GetFrequency(Freq freq);

    int GetFrequencyFromDB(Freq freq);
}
