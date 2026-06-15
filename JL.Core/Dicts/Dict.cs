using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Utilities.Database;

namespace JL.Core.Dicts;

public sealed class Dict : IEquatable<Dict>
{
    public DictType Type { get; }
    public string Name { get; }
    public string Path { get; set; }
    public bool Active { get; set; }
    public int Priority { get; set; }

    // ReSharper disable once MemberCanBeInternal
    public int Size { get; internal set; }

    public DictOptions Options { get; set; }
    public bool AutoUpdatable { get; set; }
    public Uri? Url { get; set; }
    public string? Revision { get; set; }

    [JsonIgnore] public bool Ready { get; set; } // = false;
    [JsonIgnore] public bool Updating { get; internal set; } // = false;

    [JsonIgnore] public string DBPath { get; }
    [JsonIgnore] internal string ReadOnlyConnectionString { get; private set; }

#pragma warning disable CA2227 // Collection properties should be read only
    [JsonIgnore] public IDictionary<string, IList<IDictRecord>> Contents { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    public Dict(DictType type, string name, string path, bool active, int priority, int size, DictOptions options, bool autoUpdatable = false, Uri? url = null, string? revision = null)
    {
        Type = type;
        Name = name;
        Path = path;
        Active = active;
        Priority = priority;
        Size = size;
        Options = options;
        AutoUpdatable = autoUpdatable;
        Url = url;
        Revision = revision;
        DBPath = DBUtils.GetDBPathForDict(name);
        ReadOnlyConnectionString = DBUtils.GetReadOnlyConnectionString(DBPath);
        Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode(StringComparison.Ordinal);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Dict dict && Name == dict.Name;
    }

    public bool Equals([NotNullWhen(true)] Dict? other)
    {
        return other is not null && Name == other.Name;
    }

    public static bool operator ==(Dict? left, Dict? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(Dict? left, Dict? right) => !(left == right);
}
