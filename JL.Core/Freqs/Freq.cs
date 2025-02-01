using System.Collections.Frozen;
using System.Text.Json.Serialization;
using JL.Core.Freqs.Options;
namespace JL.Core.Freqs;

public sealed class Freq(FreqType type, string name, string path, bool active, int priority, int size, int maxValue, FreqOptions options) : IEquatable<Freq>
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


    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
            hash = (hash * 37) + Path.GetHashCode(StringComparison.OrdinalIgnoreCase);
            hash = (hash * 37) + Type.GetHashCode();
            return hash;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is Freq other
            && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
            && Type == other.Type;
    }

    public bool Equals(Freq? other)
    {
        return other is not null
            && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
            && Type == other.Type;
    }
}
