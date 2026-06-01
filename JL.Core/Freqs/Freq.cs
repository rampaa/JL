using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using JL.Core.Freqs.Options;
using JL.Core.Utilities.Database;

namespace JL.Core.Freqs;

public sealed class Freq : IEquatable<Freq>
{
    public FreqType Type { get; }
    public string Name { get; }
    public string Path { get; set; }
    public bool Active { get; set; }
    public int Priority { get; set; }

    // ReSharper disable once MemberCanBeInternal
    public int Size { get; internal set; }

    // ReSharper disable once MemberCanBeInternal
    public int MaxValue { get; internal set; }

    public FreqOptions Options { get; set; }
    public bool AutoUpdatable { get; set; }
    public Uri? Url { get; set; }
    public string? Revision { get; set; }

    [JsonIgnore] public bool Ready { get; set; } // = false;
    [JsonIgnore] public bool Updating { get; internal set; } // = false;

    [JsonIgnore] public string DBPath { get; private set; }
    [JsonIgnore] public string ReadOnlyConnectionString { get; private set; }

#pragma warning disable CA2227 // Collection properties should be read only
    [JsonIgnore] public IDictionary<string, IList<FrequencyRecord>> Contents { get; set; } = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
#pragma warning restore CA2227 // Collection properties should be read only

    public Freq(FreqType type, string name, string path, bool active, int priority, int size, int maxValue, FreqOptions options, bool autoUpdatable = false, Uri? url = null, string? revision = null)
    {
        Type = type;
        Name = name;
        Path = path;
        Active = active;
        Priority = priority;
        Size = size;
        MaxValue = maxValue;
        Options = options;
        AutoUpdatable = autoUpdatable;
        Url = url;
        Revision = revision;
        DBPath = DBUtils.GetDBPathForFreqDict(name);
        ReadOnlyConnectionString = DBUtils.GetReadOnlyConnectionString(DBPath);
        Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
    }

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
