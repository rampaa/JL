using CommunityToolkit.HighPerformance.Buffers;
using JL.Core.Dicts;
using JL.Core.Freqs;

namespace JL.Core.Utilities;

public static class StringPoolUtils
{
    public static readonly StringPool StringPoolInstance = StringPool.Shared;

    public static void ClearStringPoolIfDictsAreReady()
    {
        if (DictUtils.DictsReady
            && FreqUtils.FreqsReady
            && DictUtils.Dicts.Values.ToArray().All(static dict => !dict.Updating)
            && FreqUtils.FreqDicts.Values.ToArray().All(static freq => !freq.Updating))
        {
            StringPoolInstance.Reset();
        }
    }
}
