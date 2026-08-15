namespace JL.Core.Japanese.Fuseji;

internal static class FusejiUtils
{
    public static FusejiVariantEnumerator CreateFusejiVariants(ReadOnlySpan<char> text, int maxTotalFuseji, int maxSearchKeyLengthForFusejiGeneration)
    {
        return new FusejiVariantEnumerator(text, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration);
    }
}
