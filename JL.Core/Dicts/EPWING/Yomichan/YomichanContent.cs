using System.Text.Json.Serialization;

namespace JL.Core.Dicts.EPWING.Yomichan;

[method: JsonConstructor]
internal readonly ref struct YomichanContent(string? tag, string? content, bool appendWhitespace)
{
    public string? Tag { get; } = tag;
    public string? Content { get; } = content;
    public bool AppendWhitespace { get; } = appendWhitespace;
}
