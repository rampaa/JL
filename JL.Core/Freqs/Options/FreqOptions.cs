namespace JL.Core.Freqs.Options;

public sealed class FreqOptions(UseDBOption useDB, HigherValueMeansHigherFrequencyOption higherValueMeansHigherFrequency)
{
    // ReSharper disable once MemberCanBeInternal
    public FreqOptions() : this(new UseDBOption(true), new HigherValueMeansHigherFrequencyOption(false)) { }

    public UseDBOption UseDB { get; } = useDB;
    public HigherValueMeansHigherFrequencyOption HigherValueMeansHigherFrequency { get; } = higherValueMeansHigherFrequency;
}
