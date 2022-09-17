using JL.Core.Frequency;

namespace JL.Core.Dicts;

public interface IHasGetFrequency : IResult
{
    int GetFrequency(Freq frequency);
}
