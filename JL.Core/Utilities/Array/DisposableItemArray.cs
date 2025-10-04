namespace JL.Core.Utilities.Array;

public readonly struct DisposableItemArray<T> : IDisposable, IEquatable<DisposableItemArray<T>> where T : IDisposable
{
    public T[]? Items { get; }

    public DisposableItemArray(params ReadOnlySpan<T> items)
    {
        Items = new T[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            Items[i] = items[i];
        }
    }

    public void Dispose()
    {
        if (Items is not null)
        {
            foreach (T item in Items)
            {
                item.Dispose();
            }
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is DisposableItemArray<T> other
            && other.Items is not null
                ? Items?.SequenceEqual(other.Items) ?? false
                : Items is null;
    }

    public override int GetHashCode()
    {
        int hash = 17;

        T[]? items = Items;
        if (items is not null)
        {
            foreach (T item in items)
            {
                hash = (hash * 37) + item.GetHashCode();
            }
        }
        else
        {
            hash *= 37;
        }

        return hash;
    }

    public static bool operator ==(DisposableItemArray<T> left, DisposableItemArray<T> right) => left.Equals(right);

    public static bool operator !=(DisposableItemArray<T> left, DisposableItemArray<T> right) => !left.Equals(right);

    public bool Equals(DisposableItemArray<T> other)
    {
        return other.Items is not null
                ? Items?.SequenceEqual(other.Items) ?? false
                : Items is null;
    }
}
