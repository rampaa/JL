namespace JL.Core.Freqs.Options;
public sealed class FreqOptions
{
    public UseDBOption? UseDB { get; }
    public HigherValueMeansHigherFrequencyOption? HigherValueMeansHigherFrequency { get; }

    public FreqOptions(UseDBOption? useDB = null,HigherValueMeansHigherFrequencyOption? higherValueMeansHigherFrequency = null)
    {
        UseDB = useDB;
        HigherValueMeansHigherFrequency = higherValueMeansHigherFrequency;
    }
}
