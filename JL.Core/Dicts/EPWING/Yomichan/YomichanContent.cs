namespace JL.Core.Dicts.EPWING.Yomichan;

internal readonly struct YomichanContent(string? tag, string? content)
{
    public string? Tag { get; } = tag;
    public string? Content { get; } = content;
}
