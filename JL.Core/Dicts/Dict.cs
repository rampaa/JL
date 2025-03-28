using System.Collections.Frozen;
using System.Text.Json.Serialization;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts;

public sealed class Dict(DictType type, string name, string path, bool active, int priority, int size, DictOptions options) : IEquatable<Dict>
{
    public DictType Type { get; } = type;
    public string Name { get; } = name;
    public string Path { get; set; } = path;
    public bool Active { get; set; } = active;
    public int Priority { get; set; } = priority;

    // ReSharper disable once MemberCanBeInternal
    public int Size { get; internal set; } = size;
    public DictOptions Options { get; set; } = options;
    [JsonIgnore] public bool Ready { get; set; } // = false;

#pragma warning disable CA2227 // Collection properties should be read only
    [JsonIgnore] public IDictionary<string, IList<IDictRecord>> Contents { get; set; } = FrozenDictionary<string, IList<IDictRecord>>.Empty;
#pragma warning restore CA2227 // Collection properties should be read only

    public override int GetHashCode()
    {
        return Name.GetHashCode(StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is Dict dict && Name == dict.Name;
    }

    public bool Equals(Dict? other)
    {
        return other is not null && Name == other.Name;
    }

    public static bool operator ==(Dict? left, Dict? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(Dict? left, Dict? right) => !left?.Equals(right) ?? right is not null;
}
