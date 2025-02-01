using System.Text.Json.Serialization;

namespace JL.Core.Deconjugation;

[method: JsonConstructor]
internal readonly ref struct VirtualRule(string decEnd, string conEnd, string decTag, string conTag, string detail)
{
    public string DecEnd { get; } = decEnd;
    public string ConEnd { get; } = conEnd;
    public string DecTag { get; } = decTag;
    public string ConTag { get; } = conTag;
    public string Detail { get; } = detail;
}
