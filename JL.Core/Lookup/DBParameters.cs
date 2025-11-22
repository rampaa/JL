using JL.Core.Utilities;

namespace JL.Core.Lookup;

internal sealed class DBParameters(List<string>? allTextWithoutLongVowelMark,
    string? parameter,
    string? verbParameter,
    string? yomichanWordQuery,
    string? yomichanVerbQuery,
    string? nazekaWordQuery,
    string? nazekaVerbQuery,
    string? nazekaTextWithoutLongVowelMarkQuery,
    string? yomichanTextWithoutLongVowelMarkQuery,
    string? jmdictTextWithoutLongVowelMarkParameter) : IEquatable<DBParameters>
{
    public List<string>? AllTextWithoutLongVowelMark { get; } = allTextWithoutLongVowelMark;
    public string? Parameter { get; } = parameter;
    public string? VerbParameter { get; } = verbParameter;
    public string? YomichanWordQuery { get; } = yomichanWordQuery;
    public string? YomichanVerbQuery { get; } = yomichanVerbQuery;
    public string? NazekaWordQuery { get; } = nazekaWordQuery;
    public string? NazekaVerbQuery { get; } = nazekaVerbQuery;
    public string? NazekaTextWithoutLongVowelMarkQuery { get; } = nazekaTextWithoutLongVowelMarkQuery;
    public string? YomichanTextWithoutLongVowelMarkQuery { get; } = yomichanTextWithoutLongVowelMarkQuery;
    public string? JmdictTextWithoutLongVowelMarkParameter { get; } = jmdictTextWithoutLongVowelMarkParameter;

    public bool Equals(DBParameters? other)
    {
        return other is not null && other.AllTextWithoutLongVowelMark.AsReadOnlySpan().SequenceEqual(AllTextWithoutLongVowelMark.AsReadOnlySpan());
    }

    public override bool Equals(object? obj)
    {
        return obj is DBParameters other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            if (AllTextWithoutLongVowelMark is not null)
            {
                foreach (string text in AllTextWithoutLongVowelMark.AsReadOnlySpan())
                {
                    hash = (hash * 37) + text.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            return hash;
        }
    }

    public static bool operator ==(DBParameters? left, DBParameters? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(DBParameters left, DBParameters right) => !(left == right);
}
