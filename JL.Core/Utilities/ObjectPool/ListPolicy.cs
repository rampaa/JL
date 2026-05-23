using Microsoft.Extensions.ObjectPool;

namespace JL.Core.Utilities.ObjectPool;

internal sealed class ListPolicy<T> : PooledObjectPolicy<List<T>>
{
    public override List<T> Create() => [];

    public override bool Return(List<T> obj)
    {
        obj.Clear();
        return true;
    }
}
