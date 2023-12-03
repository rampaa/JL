using System.Text.Json.Serialization;

namespace JL.Core.Freqs.Options;
public readonly record struct UseDBOption(bool Value)
{
    [JsonIgnore] public static readonly FreqType[] ValidFreqTypes = FreqUtils.s_freqTypesWithDBSupport;
}
