namespace JL.Core.Lookup;

public sealed class LookupFrequencyResult
{
    public string Name { get; }
    public int Freq { get; internal set; }

    internal LookupFrequencyResult(string name, int freq)
    {
        Name = name;
        Freq = freq;
    }
}
