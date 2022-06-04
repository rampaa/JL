using System.Text.Json.Serialization;

namespace JL.Core.Frequency;

public class Freq
{
    public FreqType Type { get; set; }
    public string Name { get; set; }

    public string Path { get; set; }

    public bool Active { get; set; }

    public int Priority { get; set; }

    [JsonIgnore] public Dictionary<string, List<FrequencyRecord>> Contents { get; set; } = new();

    //public DictOptions? Options { get; set; } // can be null for dicts.json files generated before version 1.10

    public Freq(FreqType type, string name, string path, bool active, int priority)
    {
        Type = type;
        Name = name; /*?? type.GetDescription() ?? type.ToString();*/
        Path = path;
        Active = active;
        Priority = priority;
        //Options = options;
    }
}
