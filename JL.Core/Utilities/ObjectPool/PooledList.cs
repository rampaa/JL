using System.Buffers;
using System.Runtime.CompilerServices;

namespace JL.Core.Utilities.ObjectPool;

internal sealed class PooledList<T>(int initialCapacity) : IEquatable<PooledList<T>>, IDisposable
{
    public T[] Array { get; private set; } = ArrayPool<T>.Shared.Rent(initialCapacity);
    public int Count { get; private set; }

    public void Add(T item)
    {
        if (Array.Length > Count)
        {
            Array[Count] = item;
            ++Count;
        }
        else
        {
            int newCapacity = Array.Length * 2;
            T[] newArray = ArrayPool<T>.Shared.Rent(newCapacity);
            Span<T> arraySpan = Array.AsSpan(0, Count);
            arraySpan.CopyTo(newArray);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                arraySpan.Clear();
            }
            ArrayPool<T>.Shared.Return(Array);

            newArray[Count] = item;
            ++Count;
            Array = newArray;
        }
    }

    public Span<T> AsSpan() => Array.AsSpan(0, Count);

    public ReadOnlySpan<T> AsReadOnlySpan() => Array.AsSpan(0, Count);

    public override bool Equals(object? obj)
    {
        return obj is PooledList<T> other && Equals(other);
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

    public static bool operator ==(PooledList<T> left, PooledList<T> right) => left.Equals(right);
    public static bool operator !=(PooledList<T> left, PooledList<T> right) => !(left == right);

    public bool Equals(PooledList<T>? other)
    {
        return other is not null &&
            Count == other.Count
            && Array.AsSpan(0, Count).SequenceEqual(other.Array.AsSpan(0, other.Count));
    }

    public void Dispose()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.AsSpan(0, Count).Clear();
        }

        ArrayPool<T>.Shared.Return(Array);
    }

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.AsSpan(0, Count).Clear();
        }

        Count = 0;
    }

    public T this[int index]
    {
        get => Array[index];
        set => Array[index] = value;
    }

    public void RemoveLastItem()
    {
        --Count;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array[Count] = default!;
        }
    }
}
