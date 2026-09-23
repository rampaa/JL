namespace JL.Core.Dicts.EPWING.Yomichan;

internal readonly record struct ImportOptions(bool NonKanjiDict, bool NonNameDict, bool GenerateMazegaki,
    bool GenerateFusejiVariants, int MaxSearchKeyLengthForFusejiGeneration, int MaxTotalFuseji);
