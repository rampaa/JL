using System.Text.Json.Serialization;
using JL.Core.Freqs.Options;

namespace JL.Core.Freqs;

public sealed class Freq
{
    public FreqType Type { get; }
    public string Name { get; set; }
    public string Path { get; set; }
    public bool Active { get; set; }
    public int Priority { get; set; }
    public int Size { get; set; }
    public FreqOptions? Options { get; set; } // can be null for dicts.json files generated before version 1.25.0
    [JsonIgnore] public bool Ready { get; set; }

    [JsonIgnore] public Dictionary<string, IList<FrequencyRecord>> Contents { get; internal set; } = new();

    public Freq(FreqType type, string name, string path, bool active, int priority, int size, bool ready, FreqOptions options)
    {
        Type = type;
        Name = name;
        Path = path;
        Active = active;
        Priority = priority;
        Size = size;
        Ready = ready;
        Options = options;
    }
}
