using System.Diagnostics.CodeAnalysis;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

internal sealed class Form(
    string text,
    string originalText,
    string lastTag,
    List<string> process) : IEquatable<Form>
{
    public string Text { get; } = text;
    public string OriginalText { get; } = originalText;
    public string LastTag { get; } = lastTag;
    public List<string> Process { get; } = process;

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Form other
               && (ReferenceEquals(this, other) || (Text == other.Text
               && OriginalText == other.OriginalText
               && LastTag == other.LastTag
               && Process.AsReadOnlySpan().SequenceEqual(other.Process.AsReadOnlySpan())));
    }

    public bool Equals([NotNullWhen(true)] Form? other)
    {
        return other is not null
               && (ReferenceEquals(this, other) || (Text == other.Text
               && OriginalText == other.OriginalText
               && LastTag == other.LastTag
               && Process.AsReadOnlySpan().SequenceEqual(other.Process.AsReadOnlySpan())));
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Text.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + OriginalText.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + LastTag.GetHashCode(StringComparison.Ordinal);
            return hash;
        }
    }

    public static bool operator ==(Form? left, Form? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(Form? left, Form? right) => !(left == right);
}
