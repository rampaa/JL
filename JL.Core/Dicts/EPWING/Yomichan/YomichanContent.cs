namespace JL.Core.Dicts.EPWING.Yomichan;
internal readonly struct YomichanContent
{
    public string? Tag { get; }
    public string? Content { get; }

    public YomichanContent(string? tag, string? content)
    {
        Tag = tag;
        Content = content;
    }
}
