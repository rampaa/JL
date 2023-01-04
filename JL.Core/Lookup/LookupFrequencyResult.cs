namespace JL.Core.Lookup;

public class LookupFrequencyResult
{
    public string Name { get; }
    public int Freq { get; set; }

    public LookupFrequencyResult(string name, int freq)
    {
        Name = name;
        Freq = freq;
    }
}
