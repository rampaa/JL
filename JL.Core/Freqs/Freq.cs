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
    [JsonIgnore] public bool Ready { get; internal set; }
    [JsonIgnore] public Dictionary<string, IList<FrequencyRecord>> Contents { get; internal set; } = new();
    public FreqOptions? Options { get; set; } // can be null for dicts.json files generated before version 1.25.0

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
