namespace JL.Core.Deconjugation;

internal sealed class Form
{
    public string Text { get; }
    public string OriginalText { get; }
    public List<string> Tags { get; }
    public HashSet<string> SeenText { get; }
    public List<string> Process { get; }
    public Form(string text, string originalText, List<string> tags, HashSet<string> seenText,
        List<string> process)
    {
        Text = text;
        OriginalText = originalText;
        Tags = tags;
        SeenText = seenText;
        Process = process;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        Form form = (Form)obj;

        return Text == form.Text
               && OriginalText == form.OriginalText
               && Tags.SequenceEqual(form.Tags)
               && SeenText.SetEquals(form.SeenText)
               && Process.SequenceEqual(form.Process);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            hash = (hash * 37) + Text.GetHashCode(StringComparison.Ordinal);
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
