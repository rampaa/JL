using System.Text.Json.Serialization;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;

namespace JL.Core.Dicts;

public class Dict
{
    public DictType Type { get; set; }
    public string Name { get; set; }

    public string Path { get; set; }

    public bool Active { get; set; }

    public int Priority { get; set; }

    [JsonIgnore] public Dictionary<string, List<IResult>> Contents { get; set; } = new();

    public DictOptions? Options { get; set; } // can be null for dicts.json files generated before version 1.10

    public Dict(DictType type, string? name, string path, bool active, int priority, DictOptions options)
    {
        Type = type;
        Name = name ?? type.GetDescription() ?? type.ToString();
        Path = path;
        Active = active;
        Priority = priority;
        Options = options;
    }
}
