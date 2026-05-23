using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Lookup;
using Microsoft.Extensions.ObjectPool;

namespace JL.Core.Utilities.ObjectPool;

public static class ObjectPoolManager
{
    public static readonly ObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool(1024, 1024 * 4);
    internal static readonly StringPool s_stringPoolInstance = StringPool.Shared;

    internal static readonly ObjectPool<Dictionary<string, IntermediaryResult>> s_intermediaryResultPool = new DefaultObjectPoolProvider().Create(new IntermediaryResultDictionaryPolicy());
    internal static readonly ObjectPool<List<LookupResult>> s_lookupResultListPool = new DefaultObjectPoolProvider().Create(new ListPolicy<LookupResult>());

    public static void ClearStringPoolIfDictsAreReady()
    {
        if (DictUtils.DictsReady
            && FreqUtils.FreqsReady
            && DictUtils.Dicts.Values.ToArray().All(static dict => !dict.Updating)
            && FreqUtils.FreqDicts.Values.ToArray().All(static freq => !freq.Updating))
        {
            s_stringPoolInstance.Reset();
        }
    }
}
