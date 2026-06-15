using System.Buffers;
using System.Diagnostics;

namespace JL.Core.Utilities.ObjectPool;

internal sealed class RentedArrayBuffer<T>(int capacity) : IEquatable<RentedArrayBuffer<T>>, IDisposable
{
    public T[] Array { get; } = ArrayPool<T>.Shared.Rent(capacity);
    private int Count { get; set; }

    public void Add(T item)
    {
        Debug.Assert(Array.Length > Count);
        Array[Count] = item;
        ++Count;
    }

    public Span<T> AsSpan() => Array.AsSpan(0, Count);

    public ReadOnlySpan<T> AsReadOnlySpan() => Array.AsSpan(0, Count);

    public override bool Equals(object? obj)
    {
        return obj is RentedArrayBuffer<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 23) + Count.GetHashCode();
            for (int i = 0; i < Count; i++)
            {
                hash *= 23 + (Array[i]?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    public static bool operator ==(RentedArrayBuffer<T> left, RentedArrayBuffer<T> right) => left.Equals(right);
    public static bool operator !=(RentedArrayBuffer<T> left, RentedArrayBuffer<T> right) => !(left == right);

    public bool Equals(RentedArrayBuffer<T>? other)
    {
        return other is not null &&
            Count == other.Count
            && Array.AsSpan(0, Count).SequenceEqual(other.Array.AsSpan(0, other.Count));
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(Array);
    }
}
