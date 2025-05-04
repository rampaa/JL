using System.Text.Json.Serialization;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts;
public abstract class DictBase(DictType type, string name, string path, bool active, int priority, int size, DictOptions options) : IEquatable<DictBase>
{
    public DictType Type { get; } = type;
    public string Name { get; } = name;
    public string Path { get; set; } = path;
    public bool Active { get; set; } = active;
    public int Priority { get; set; } = priority;
    public int Size { get; internal set; } = size;
    public DictOptions Options { get; set; } = options;
    [JsonIgnore] public bool Ready { get; set; } // = false;

    public override int GetHashCode()
    {
        return Name.GetHashCode(StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is DictBase dict && Name == dict.Name;
    }

    public bool Equals(DictBase? other)
    {
        return other is not null && Name == other.Name;
    }

    public static bool operator ==(DictBase? left, DictBase? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(DictBase? left, DictBase? right) => !left?.Equals(right) ?? right is not null;
}
