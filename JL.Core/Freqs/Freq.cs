using System.Collections.Frozen;
using System.Text.Json.Serialization;
using JL.Core.Freqs.Options;

namespace JL.Core.Freqs;

public sealed class Freq(FreqType type, string name, string path, bool active, int priority, int size, int maxValue, FreqOptions options)
{
    public FreqType Type { get; } = type;
    public string Name { get; set; } = name;
    public string Path { get; set; } = path;
    public bool Active { get; set; } = active;
    public int Priority { get; set; } = priority;

    // ReSharper disable once MemberCanBeInternal
    public int Size { get; internal set; } = size;

    // ReSharper disable once MemberCanBeInternal
    public int MaxValue { get; internal set; } = maxValue;
    [JsonIgnore] public bool Ready { get; set; } // = false;
    public FreqOptions Options { get; set; } = options;

#pragma warning disable CA2227
    [JsonIgnore] public IDictionary<string, IList<FrequencyRecord>> Contents { get; set; } = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
#pragma warning restore CA2227

}
