using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public sealed class Dict(DictType type, string name, string path, bool active, int priority, int size, bool ready, DictOptions options)
{
    public DictType Type { get; } = type;
    public string Name { get; set; } = name;
    public string Path { get; set; } = path;
    public bool Active { get; set; } = active;
    public int Priority { get; set; } = priority;
    public int Size { get; set; } = size;
    [JsonIgnore] public bool Ready { get; set; } = ready;

#pragma warning disable CA2227
    [JsonIgnore] public Dictionary<string, IList<IDictRecord>> Contents { get; set; } = [];
#pragma warning restore CA2227

    public DictOptions? Options { get; set; } = options; // can be null for dicts.json files generated before version 1.10
}
