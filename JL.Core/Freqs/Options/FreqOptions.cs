namespace JL.Core.Freqs.Options;
public sealed class FreqOptions(UseDBOption? useDB = null, HigherValueMeansHigherFrequencyOption? higherValueMeansHigherFrequency = null)
{
    public UseDBOption? UseDB { get; internal set; } = useDB;
    public HigherValueMeansHigherFrequencyOption? HigherValueMeansHigherFrequency { get; internal set; } = higherValueMeansHigherFrequency;
}
