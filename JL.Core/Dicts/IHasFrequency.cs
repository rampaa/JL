using JL.Core.Frequency;

namespace JL.Core.Dicts;

public interface IHasFrequency : IResult
{
    int GetFrequency(Freq frequency);
}
