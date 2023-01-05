using System.Text.Json.Serialization;

namespace JL.Core.Freqs;

public sealed class Freq
{
    public FreqType Type { get; }
    public string Name { get; set; }
    public string Path { get; set; }
    public bool Active { get; set; }
    public int Priority { get; set; }
    public int Size { get; set; }

    [JsonIgnore] public Dictionary<string, List<FrequencyRecord>> Contents { get; internal set; } = new();

    //public DictOptions? Options { get; set; } // can be null for dicts.json files generated before version 1.10

    public Freq(FreqType type, string name, string path, bool active, int priority, int size)
    {
        Type = type;
        Name = name; //?? type.GetDescription() ?? type.ToString();
        Path = path;
        Active = active;
        Priority = priority;
        Size = size;
        //Options = options;
    }
}
