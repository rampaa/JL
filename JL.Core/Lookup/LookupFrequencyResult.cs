namespace JL.Core.Lookup;

public readonly record struct LookupFrequencyResult
{
    internal string Name { get; }
    public int Freq { get; }

    internal bool HigherValueMeansHigherFrequency { get; }

    internal LookupFrequencyResult(string name, int freq, bool higherValueMeansHigherFrequency)
    {
        Name = name;
        Freq = freq;
        HigherValueMeansHigherFrequency = higherValueMeansHigherFrequency;
    }
}
