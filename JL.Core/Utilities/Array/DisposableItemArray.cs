namespace JL.Core.Utilities.Array;

internal readonly ref struct DisposableItemArray<T> : IDisposable where T : IDisposable
{
    public T[]? Items { get; }

    public DisposableItemArray(int length)
    {
        Items = new T[length];
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
}
