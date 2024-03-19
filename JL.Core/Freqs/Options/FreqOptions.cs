namespace JL.Core.Freqs.Options;
public sealed class FreqOptions(UseDBOption? useDB = null, HigherValueMeansHigherFrequencyOption? higherValueMeansHigherFrequency = null)
{
    public UseDBOption? UseDB { get; } = useDB;
    public HigherValueMeansHigherFrequencyOption? HigherValueMeansHigherFrequency { get; } = higherValueMeansHigherFrequency;
}
