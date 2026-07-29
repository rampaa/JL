using System.Buffers;
using System.Diagnostics;

namespace JL.Core.Utilities.ObjectPool;

internal sealed class RentedArrayBuffer<T>(int capacity) : IDisposable
{
    private T[] Array { get; } = ArrayPool<T>.Shared.Rent(capacity);
    private int Count { get; set; }

    public void Add(T item)
    {
        Debug.Assert(Array.Length > Count);
        Array[Count] = item;
        ++Count;
    }

    // public Span<T> AsSpan() => Array.AsSpan(0, Count);

    public ReadOnlySpan<T> AsReadOnlySpan() => Array.AsSpan(0, Count);

    public T this[int index]
    {
        get => Array[index];
        // ReSharper disable once UnusedMember.Global
        set => Array[index] = value;
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(Array);
    }
}
