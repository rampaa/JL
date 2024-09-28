namespace JL.Core.Freqs.Options;

public sealed class FreqOptions(UseDBOption useDB, HigherValueMeansHigherFrequencyOption higherValueMeansHigherFrequency)
{
    public UseDBOption UseDB { get; } = useDB;
    public HigherValueMeansHigherFrequencyOption HigherValueMeansHigherFrequency { get; } = higherValueMeansHigherFrequency;
}
