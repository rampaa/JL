using JL.Core.Lookup;
using Microsoft.Extensions.ObjectPool;

namespace JL.Core.Utilities.ObjectPool;

internal sealed class IntermediaryResultDictionaryPolicy : PooledObjectPolicy<Dictionary<string, IntermediaryResult>>
{
    public override Dictionary<string, IntermediaryResult> Create() => new(StringComparer.Ordinal);

    public override bool Return(Dictionary<string, IntermediaryResult> obj)
    {
        obj.Clear();
        return true;
    }
}
