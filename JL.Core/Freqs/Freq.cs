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

    public Freq(FreqType type, string name, string path, bool active, int priority, int size)
    {
        Type = type;
        Name = name;
        Path = path;
        Active = active;
        Priority = priority;
        Size = size;
    }
}
