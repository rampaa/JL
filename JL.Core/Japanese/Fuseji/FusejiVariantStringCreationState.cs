namespace JL.Core.Japanese.Fuseji;

internal readonly ref struct FusejiVariantStringCreationState
{
    public ReadOnlySpan<char> OriginalText { get; }
    public ulong CombinationBitmask { get; }

    public readonly ref readonly GraphemeCharacterOffsetsBuffer GraphemeCharacterOffsets;
    public int TotalGraphemeCount { get; }

    public FusejiVariantStringCreationState(
        ReadOnlySpan<char> originalText,
        ulong combinationBitmask,
        ref readonly GraphemeCharacterOffsetsBuffer graphemeCharacterOffsets,
        int totalGraphemeCount)
    {
        OriginalText = originalText;
        CombinationBitmask = combinationBitmask;
        GraphemeCharacterOffsets = ref graphemeCharacterOffsets;
        TotalGraphemeCount = totalGraphemeCount;
    }
}
