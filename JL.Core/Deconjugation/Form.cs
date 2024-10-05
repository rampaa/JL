namespace JL.Core.Deconjugation;

internal sealed class Form(
    string text,
    string originalText,
    List<string> tags,
    HashSet<string> seenText,
    List<string> process)
{
    public string Text { get; } = text;
    public string OriginalText { get; } = originalText;
    public List<string> Tags { get; } = tags;
    public HashSet<string> SeenText { get; } = seenText;
    public List<string> Process { get; } = process;

    public override bool Equals(object? obj)
    {
        return obj is Form form
               && Text == form.Text
               && OriginalText == form.OriginalText
               && Tags.SequenceEqual(form.Tags)
               && SeenText.SetEquals(form.SeenText)
               && Process.SequenceEqual(form.Process);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Text.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + OriginalText.GetHashCode(StringComparison.Ordinal);

            foreach (string tag in Tags)
            {
                hash = (hash * 37) + tag.GetHashCode(StringComparison.Ordinal);
            }

            foreach (string process in Process)
            {
                hash = (hash * 37) + process.GetHashCode(StringComparison.Ordinal);
            }

            foreach (string seenText in SeenText)
            {
                hash = (hash * 37) + seenText.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }
}
