namespace JL.Core.Utilities.Japanese.Okurigana;

internal readonly struct ExpressionRun(int start, int length, bool isKanji) : IEquatable<ExpressionRun>
{
    public int Start { get; } = start;
    public int Length { get; } = length;
    public bool IsKanji { get; } = isKanji;

    public bool Equals(ExpressionRun other)
    {
        return Start == other.Start
            && Length == other.Length
            && IsKanji == other.IsKanji;
    }

    public override bool Equals(object? obj) => obj is ExpressionRun other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Start, Length, IsKanji);

    public static bool operator ==(ExpressionRun left, ExpressionRun right) => left.Equals(right);

    public static bool operator !=(ExpressionRun left, ExpressionRun right) => !left.Equals(right);
}
