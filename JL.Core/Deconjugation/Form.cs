using JL.Core.Utilities;

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
               && Tags.AsSpan().SequenceEqual(form.Tags.AsSpan())
               && Process.AsSpan().SequenceEqual(form.Process.AsSpan());
    }

    public bool Equals(Form? other)
    {
        return other is not null
               && Text == other.Text
               && OriginalText == other.OriginalText
               && Tags.AsSpan().SequenceEqual(other.Tags.AsSpan())
               && Process.AsSpan().SequenceEqual(other.Process.AsSpan());
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Text.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + OriginalText.GetHashCode(StringComparison.Ordinal);

            foreach (ref readonly string tag in Tags.AsSpan())
            {
                hash = (hash * 37) + tag.GetHashCode(StringComparison.Ordinal);
            }

            foreach (ref readonly string process in Process.AsSpan())
            {
                hash = (hash * 37) + process.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }

    public static bool operator ==(Form? left, Form? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(Form? left, Form? right) => !left?.Equals(right) ?? right is not null;
}
