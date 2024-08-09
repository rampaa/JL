using System.Collections.Frozen;
using System.Text.Json.Serialization;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts;

public sealed class Dict(DictType type, string name, string path, bool active, int priority, int size, bool ready, DictOptions? options)
{
    public DictType Type { get; internal set; } = type;
    public string Name { get; set; } = name;
    public string Path { get; set; } = path;
    public bool Active { get; set; } = active;
    public int Priority { get; set; } = priority;

    // ReSharper disable once MemberCanBeInternal
    public int Size { get; set; } = size;

    [JsonIgnore] public bool Ready { get; set; } = ready;

#pragma warning disable CA2227
    [JsonIgnore] public IDictionary<string, IList<IDictRecord>> Contents { get; set; } = FrozenDictionary<string, IList<IDictRecord>>.Empty;
#pragma warning restore CA2227

    public DictOptions Options { get; set; } = options ?? new DictOptions();
}
