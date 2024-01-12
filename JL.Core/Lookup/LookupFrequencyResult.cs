namespace JL.Core.Lookup;

public sealed class LookupFrequencyResult
{
    public string Name { get; }
    public int Freq { get; }

    public bool HigherValueMeansHigherFrequency { get; }

    internal LookupFrequencyResult(string name, int freq, bool higherValueMeansHigherFrequency)
    {
        Name = name;
        Freq = freq;
        HigherValueMeansHigherFrequency = higherValueMeansHigherFrequency;
    }
}
