using JL.Core.Utilities.ObjectPool;

namespace JL.Core.Utilities.Array;

internal readonly ref struct DisposableItemArrayRefStruct<T> : IDisposable where T : notnull, IDisposable
{
    public RentedArrayBuffer<T?>? Items { get; }

    public DisposableItemArrayRefStruct(int length)
    {
        Items = new RentedArrayBuffer<T?>(length);
    }

    public void Dispose()
    {
        if (Items is not null)
        {
            foreach (T? item in Items.Array)
            {
                item?.Dispose();
            }
        }
    }
}
