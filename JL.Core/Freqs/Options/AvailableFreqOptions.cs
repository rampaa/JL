using System.Text.Json.Serialization;

namespace JL.Core.Freqs.Options;

public sealed class UseDBOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly FreqType[] ValidFreqTypes = FreqUtils.s_allFreqDicts;
}

public sealed class HigherValueMeansHigherFrequencyOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly FreqType[] ValidFreqTypes = FreqUtils.s_allFreqDicts;
}

public sealed class AutoUpdateAfterNDaysOption(int value)
{
    public int Value { get; } = value;

    [JsonIgnore]
    public static readonly FreqType[] ValidFreqTypes = [FreqType.Yomichan, FreqType.YomichanKanji];
}
