namespace JL.Core.Deconjugation;

internal sealed class Form(
    string text,
    string originalText,
    List<string> tags,
    List<string> process) : IEquatable<Form>
{
    public string Text { get; } = text;
    public string OriginalText { get; } = originalText;
    public List<string> Tags { get; } = tags;
    public List<string> Process { get; } = process;

    public override bool Equals(object? obj)
    {
        return obj is Form form
               && Text == form.Text
               && OriginalText == form.OriginalText
               && Tags.SequenceEqual(form.Tags)
               && Process.SequenceEqual(form.Process);
    }

    public bool Equals(Form? other)
    {
        return other is not null
               && Text == other.Text
               && OriginalText == other.OriginalText
               && Tags.SequenceEqual(other.Tags)
               && Process.SequenceEqual(other.Process);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Text.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + OriginalText.GetHashCode(StringComparison.Ordinal);

            for (int i = 0; i < Tags.Count; i++)
            {
                hash = (hash * 37) + Tags[i].GetHashCode(StringComparison.Ordinal);
            }

            for (int i = 0; i < Process.Count; i++)
            {
                hash = (hash * 37) + Process[i].GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }

    public static bool operator ==(Form? left, Form? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(Form? left, Form? right) => !left?.Equals(right) ?? right is not null;
}
