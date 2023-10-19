namespace JL.Core.Dicts.EPWING.EpwingYomichan;
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
