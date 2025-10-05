using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using JL.Core.Dicts;
using JL.Core.Freqs;
using Microsoft.Extensions.ObjectPool;

namespace JL.Core.Utilities;

public static class ObjectPoolManager
{
    public static readonly ObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool(1024, 1024 * 4);
    internal static readonly StringPool s_stringPoolInstance = StringPool.Shared;

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
