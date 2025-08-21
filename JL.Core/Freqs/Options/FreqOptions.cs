namespace JL.Core.Freqs.Options;

public sealed class FreqOptions(UseDBOption useDB,
    HigherValueMeansHigherFrequencyOption higherValueMeansHigherFrequency,
    AutoUpdateAfterNDaysOption? autoUpdateAfterNDays = null)
{
    public UseDBOption UseDB { get; } = useDB;
    public HigherValueMeansHigherFrequencyOption HigherValueMeansHigherFrequency { get; internal set; } = higherValueMeansHigherFrequency;
    public AutoUpdateAfterNDaysOption? AutoUpdateAfterNDays { get; internal set; } = autoUpdateAfterNDays;
}
