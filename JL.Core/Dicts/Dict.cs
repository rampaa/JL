using System.Text.Json.Serialization;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts;

public sealed class Dict
{
    public DictType Type { get; }
    public string Name { get; set; }
    public string Path { get; set; }
    public bool Active { get; set; }
    public int Priority { get; set; }
    public int Size { get; set; }
    [JsonIgnore] public bool Ready { get; internal set; }

    [JsonIgnore] public Dictionary<string, IList<IDictRecord>> Contents { get; set; } = new();

    public DictOptions? Options { get; set; } // can be null for dicts.json files generated before version 1.10

    public Dict(DictType type, string name, string path, bool active, int priority, int size, bool ready, DictOptions options)
    {
        Type = type;
        Name = name;
        Path = path;
        Active = active;
        Priority = priority;
        Size = size;
        Options = options;
        Ready = ready;
    }
}
