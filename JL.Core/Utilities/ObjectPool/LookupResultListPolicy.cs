using JL.Core.Lookup;
using Microsoft.Extensions.ObjectPool;

namespace JL.Core.Utilities.ObjectPool;

internal sealed class LookupResultListPolicy : PooledObjectPolicy<List<LookupResult>>
{
    public override List<LookupResult> Create() => [];

    public override bool Return(List<LookupResult> obj)
    {
        obj.Clear();
        return true;
    }
}
