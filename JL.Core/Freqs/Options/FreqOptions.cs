namespace JL.Core.Freqs.Options;

public sealed class FreqOptions(UseDBOption useDB,
    HigherValueMeansHigherFrequencyOption higherValueMeansHigherFrequency,
    AutoUpdateAfterNDaysOption? autoUpdateAfterNDays = null,
    GenerateMazegakiVariantsOption? generateMazegakiVariants = null,
    GenerateFusejiVariantsOption? generateFusejiVariants = null,
    MaxSearchKeyLengthForFusejiGenerationOption? maxSearchKeyLengthForFusejiGeneration = null,
    MaxTotalFusejiCountOption? maxTotalFusejiCount = null)
{
    public UseDBOption UseDB { get; } = useDB;
    public HigherValueMeansHigherFrequencyOption HigherValueMeansHigherFrequency { get; } = higherValueMeansHigherFrequency;

    // ReSharper disable once MemberCanBeInternal
    public AutoUpdateAfterNDaysOption? AutoUpdateAfterNDays { get; internal set; } = autoUpdateAfterNDays;

    public GenerateMazegakiVariantsOption? GenerateMazegakiVariants { get; internal set; } = generateMazegakiVariants;
    public GenerateFusejiVariantsOption? GenerateFusejiVariants { get; internal set; } = generateFusejiVariants;
    public MaxSearchKeyLengthForFusejiGenerationOption? MaxSearchKeyLengthForFusejiGeneration { get; internal set; } = maxSearchKeyLengthForFusejiGeneration;
    public MaxTotalFusejiCountOption? MaxTotalFusejiCount { get; internal set; } = maxTotalFusejiCount;
}
