using System.Text.Json.Serialization;

namespace JL.Core.Lookup;

public readonly record struct LookupFrequencyResult
{
    internal string Name { get; }
    public int Freq { get; }

    internal bool HigherValueMeansHigherFrequency { get; }

    [JsonConstructor]
    internal LookupFrequencyResult(string name, int freq, bool higherValueMeansHigherFrequency)
    {
        Name = name;
        Freq = freq;
        HigherValueMeansHigherFrequency = higherValueMeansHigherFrequency;
    }
}
