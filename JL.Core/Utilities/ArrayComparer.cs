using System.Diagnostics.CodeAnalysis;

namespace JL.Core.Utilities;

public class ArrayComparer<T> : IEqualityComparer<T[]?> where T : IEquatable<T>
{
    public static readonly ArrayComparer<T> Instance = new();

    private ArrayComparer()
    {
    }

    public bool Equals(T[]? x, T[]? y)
    {
        return x.AsReadOnlySpan().SequenceEqual(y);
    }

    public int GetHashCode([DisallowNull] T[]? obj)
    {
        HashCode hash = new();
        foreach (T element in obj)
        {
            hash.Add(element);
        }

        return hash.ToHashCode();
    }
}
