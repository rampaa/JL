using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using JL.Core.Freqs.Options;

namespace JL.Core.Freqs;

public sealed class Freq(FreqType type, string name, string path, bool active, int priority, int size, int maxValue, FreqOptions options, bool autoUpdatable = false, Uri? url = null, string? revision = null) : IEquatable<Freq>
{
    public FreqType Type { get; } = type;
    public string Name { get; } = name;
    public string Path { get; set; } = path;
    public bool Active { get; set; } = active;
    public int Priority { get; set; } = priority;

    // ReSharper disable once MemberCanBeInternal
    public int Size { get; internal set; } = size;

    // ReSharper disable once MemberCanBeInternal
    public int MaxValue { get; internal set; } = maxValue;

    public FreqOptions Options { get; set; } = options;
    public bool AutoUpdatable { get; set; } = autoUpdatable;
    public Uri? Url { get; set; } = url;
    public string? Revision { get; set; } = revision;

    [JsonIgnore] public bool Ready { get; set; } // = false;
    [JsonIgnore] public bool Updating { get; internal set; } // = false;

#pragma warning disable CA2227 // Collection properties should be read only
    [JsonIgnore] public IDictionary<string, IList<FrequencyRecord>> Contents { get; set; } = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
#pragma warning restore CA2227 // Collection properties should be read only

    public override int GetHashCode()
    {
        return Name.GetHashCode(StringComparison.Ordinal);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Freq freq && Name == freq.Name;
    }

    public bool Equals([NotNullWhen(true)] Freq? other)
    {
        return other is not null && Name == other.Name;
    }

    public static bool operator ==(Freq? left, Freq? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(Freq? left, Freq? right) => !(left == right);
}
