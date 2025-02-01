using System.Collections.Frozen;
using System.Text.Json.Serialization;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts;

public sealed class Dict(DictType type, string name, string path, bool active, int priority, int size, DictOptions options) : IEquatable<Dict>
{
    public DictType Type { get; } = type;
    public string Name { get; set; } = name;
    public string Path { get; set; } = path;
    public bool Active { get; set; } = active;
    public int Priority { get; set; } = priority;

    // ReSharper disable once MemberCanBeInternal
    public int Size { get; internal set; } = size;
    public DictOptions Options { get; set; } = options;
    [JsonIgnore] public bool Ready { get; set; } // = false;

#pragma warning disable CA2227
    [JsonIgnore] public IDictionary<string, IList<IDictRecord>> Contents { get; set; } = FrozenDictionary<string, IList<IDictRecord>>.Empty;
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
        return obj is Dict other
            && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
            && Type == other.Type;
    }

    public bool Equals(Dict? other)
    {
        return other is not null
            && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
            && Type == other.Type;
    }
}
