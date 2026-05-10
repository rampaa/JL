using System.Diagnostics.CodeAnalysis;

namespace JL.Core.Deconjugation;

internal readonly struct Form(
    string text,
    string originalText,
    string lastTag,
    ProcessNode? process) : IEquatable<Form>
{
    public string Text { get; } = text;
    public string OriginalText { get; } = originalText;
    public string LastTag { get; } = lastTag;
    public ProcessNode? Process { get; } = process;

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Form other && Equals(other);
    }

    public bool Equals(Form other)
    {
        return Text == other.Text
               && OriginalText == other.OriginalText
               && LastTag == other.LastTag
               && Process == other.Process;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Text.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + OriginalText.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + LastTag.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + (Process?.GetHashCode() ?? 0);
            return hash;
        }
    }

    public static bool operator ==(Form? left, Form? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(Form? left, Form? right) => !(left == right);
}
