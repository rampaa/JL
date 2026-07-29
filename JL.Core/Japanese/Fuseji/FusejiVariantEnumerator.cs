using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace JL.Core.Japanese.Fuseji;

internal ref struct FusejiVariantEnumerator
{
    private readonly ReadOnlySpan<char> _originalText;
    private readonly AvailableGraphemeIndicesBuffer _availableGraphemeIndicesBuffer;
    private readonly int _totalGraphemeCount;
    private readonly int _availableGraphemeCount;
    private readonly int _maxAllowedConsecutiveMasks;
    private readonly int _maxMasksWeCanInsert;
    private readonly ulong _originalMaskBitmask;

    private GraphemeCharacterOffsetsBuffer _graphemeCharacterOffsetsBuffer;
    private CombinationGraphemeIndicesBuffer _combinationGraphemeIndicesBuffer;
    private bool _hasNoValidVariants;
    private bool _isFirstCombinationOfCurrentSize;
    private int _currentCombinationSize;
    private ulong _currentCombinationBitmask;

    public FusejiVariantEnumerator(ReadOnlySpan<char> text, int maxTotalFuseji, int maxConsecutiveFuseji, int maxSearchKeyLengthForFusejiGeneration)
    {
        Current = "";
        if (text.Length > maxSearchKeyLengthForFusejiGeneration)
        {
            _hasNoValidVariants = true;
            _totalGraphemeCount = 0;
            _availableGraphemeCount = 0;
            _currentCombinationSize = 0;
            _maxMasksWeCanInsert = 0;
            return;
        }

        _graphemeCharacterOffsetsBuffer = default;
        Span<byte> graphemeCharacterOffsets = _graphemeCharacterOffsetsBuffer;
        _totalGraphemeCount = PopulateGraphemeCharacterOffsets(text, graphemeCharacterOffsets);
        if (_totalGraphemeCount < 2)
        {
            _hasNoValidVariants = true;
            _totalGraphemeCount = 0;
            _availableGraphemeCount = 0;
            _currentCombinationSize = 0;
            _maxMasksWeCanInsert = 0;
            return;
        }

        _originalText = text;
        _hasNoValidVariants = false;
        _originalMaskBitmask = 0;
        _currentCombinationBitmask = 0;
        _availableGraphemeIndicesBuffer = default;
        _combinationGraphemeIndicesBuffer = default;

        int originalMaskedGraphemeCount = CountAndMarkOriginalMaskedGraphemes(
            text,
            _totalGraphemeCount,
            graphemeCharacterOffsets,
            maxTotalFuseji,
            out _originalMaskBitmask);

        int maxAcceptableGraphemeCount = Math.Max(_totalGraphemeCount - 2, 1);
        int maxAllowedTotalMasks = Math.Min(maxAcceptableGraphemeCount, maxTotalFuseji);
        _maxAllowedConsecutiveMasks = Math.Min(maxAcceptableGraphemeCount, maxConsecutiveFuseji);

        bool tooManyOriginalMasks = originalMaskedGraphemeCount > maxAllowedTotalMasks;
        bool tooManyConsecutiveOriginalMasks = HasTooManyConsecutiveMaskedGraphemes(_originalMaskBitmask, _maxAllowedConsecutiveMasks);
        if (tooManyOriginalMasks || tooManyConsecutiveOriginalMasks)
        {
            _hasNoValidVariants = true;
            return;
        }

        Span<byte> availableGraphemeIndices = _availableGraphemeIndicesBuffer;
        _availableGraphemeCount = PopulateAvailableGraphemeIndices(_totalGraphemeCount, _originalMaskBitmask, availableGraphemeIndices);

        _maxMasksWeCanInsert = maxAllowedTotalMasks - originalMaskedGraphemeCount;
        if (_maxMasksWeCanInsert < 1 || _availableGraphemeCount < 1)
        {
            _hasNoValidVariants = true;
            return;
        }

        _currentCombinationSize = 1;
        _isFirstCombinationOfCurrentSize = true;
    }

    public string Current { get; private set; }

    public readonly FusejiVariantEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        if (_hasNoValidVariants)
        {
            return false;
        }

        while (true)
        {
            if (!TryGenerateNextCombination())
            {
                _hasNoValidVariants = true;
                return false;
            }

            if (IsValidCombination(out ulong combinationBitmask))
            {
                _currentCombinationBitmask = combinationBitmask;
                Current = BuildVariantString();
                return true;
            }
        }
    }

    // Generates combinations of grapheme indices in increasing size order (1, 2, 3, ...),
    // and within a given size, in lexicographic order. Returns false once every size up to
    // _maxMasksWeCanInsert has been exhausted.
    private bool TryGenerateNextCombination()
    {
        Span<byte> combinationIndices = _combinationGraphemeIndicesBuffer;

        while (true)
        {
            if (_isFirstCombinationOfCurrentSize)
            {
                bool combinationSizeFitsAvailableGraphemes = _currentCombinationSize <= _availableGraphemeCount
                    && _currentCombinationSize <= _maxMasksWeCanInsert;

                if (!combinationSizeFitsAvailableGraphemes)
                {
                    return false;
                }

                for (int index = 0; index < _currentCombinationSize; index++)
                {
                    combinationIndices[index] = (byte)index;
                }

                _isFirstCombinationOfCurrentSize = false;
                return true;
            }

            int indexToIncrement = FindRightmostIncrementableIndex(combinationIndices, _currentCombinationSize, _availableGraphemeCount);
            if (indexToIncrement >= 0)
            {
                IncrementCombinationFrom(combinationIndices, indexToIncrement, _currentCombinationSize);
                return true;
            }

            _currentCombinationSize++;
            if (_currentCombinationSize > _maxMasksWeCanInsert)
            {
                return false;
            }

            _isFirstCombinationOfCurrentSize = true;
        }
    }

    private readonly bool IsValidCombination(out ulong combinationBitmask)
    {
        ulong bitmask = _originalMaskBitmask;
        ReadOnlySpan<byte> combinationIndices = _combinationGraphemeIndicesBuffer;
        ReadOnlySpan<byte> availableGraphemeIndices = _availableGraphemeIndicesBuffer;

        for (int index = 0; index < _currentCombinationSize; index++)
        {
            int graphemeIndex = availableGraphemeIndices[combinationIndices[index]];
            bitmask = SetBit(bitmask, graphemeIndex);
        }

        if (HasTooManyConsecutiveMaskedGraphemes(bitmask, _maxAllowedConsecutiveMasks))
        {
            combinationBitmask = 0;
            return false;
        }
        combinationBitmask = bitmask;
        return true;
    }

    private string BuildVariantString(
)
    {
        int variantCharacterLength = ComputeVariantCharacterLength(_originalText.Length, _currentCombinationBitmask, _graphemeCharacterOffsetsBuffer);
        FusejiVariantStringCreationState creationState = new(_originalText, _currentCombinationBitmask, ref _graphemeCharacterOffsetsBuffer, _totalGraphemeCount);
        return string.Create(variantCharacterLength, creationState, WriteVariantCharacters);
    }

    private static int FindRightmostIncrementableIndex(ReadOnlySpan<byte> combinationIndices, int combinationSize, int availableGraphemeCount)
    {
        for (int index = combinationSize - 1; index >= 0; index--)
        {
            int maxValueAtThisPosition = availableGraphemeCount - combinationSize + index;
            if (combinationIndices[index] < maxValueAtThisPosition)
            {
                return index;
            }
        }

        return -1;
    }

    private static void IncrementCombinationFrom(Span<byte> combinationIndices, int indexToIncrement, int combinationSize)
    {
        byte incrementedValue = (byte)(combinationIndices[indexToIncrement] + 1);
        combinationIndices[indexToIncrement] = incrementedValue;

        for (int index = indexToIncrement + 1; index < combinationSize; index++)
        {
            combinationIndices[index] = (byte)(incrementedValue + index - indexToIncrement);
        }
    }

    private static int PopulateGraphemeCharacterOffsets(ReadOnlySpan<char> text, Span<byte> graphemeCharacterOffsets)
    {
        graphemeCharacterOffsets[0] = 0;

        int graphemeCount = 0;
        int characterIndex = 0;
        ReadOnlySpan<char> remainingText = text;

        while (!remainingText.IsEmpty)
        {
            int graphemeLength = StringInfo.GetNextTextElementLength(remainingText);
            if (graphemeLength is 0)
            {
                break;
            }

            characterIndex += graphemeLength;
            graphemeCount++;
            graphemeCharacterOffsets[graphemeCount] = (byte)characterIndex;
            remainingText = remainingText[graphemeLength..];
        }

        return graphemeCount;
    }

    private static int CountAndMarkOriginalMaskedGraphemes(
        ReadOnlySpan<char> text,
        int graphemeCount,
        ReadOnlySpan<byte> graphemeCharacterOffsets,
        int maxTotalFuseji,
        out ulong originalMaskBitmask)
    {
        ulong bitmask = 0;
        int maskedGraphemeCount = 0;

        for (int graphemeIndex = 0; graphemeIndex < graphemeCount; graphemeIndex++)
        {
            int graphemeStart = graphemeCharacterOffsets[graphemeIndex];
            int graphemeLength = graphemeCharacterOffsets[graphemeIndex + 1] - graphemeStart;
            bool isMaskCharacter = graphemeLength is 1 && text[graphemeStart] is JapaneseUtils.NormalizedFuseji;

            if (isMaskCharacter)
            {
                if (maskedGraphemeCount < maxTotalFuseji)
                {
                    bitmask = SetBit(bitmask, graphemeIndex);
                }

                maskedGraphemeCount++;
            }
        }

        originalMaskBitmask = bitmask;
        return maskedGraphemeCount;
    }

    private static int PopulateAvailableGraphemeIndices(int graphemeCount, ulong originalMaskBitmask, Span<byte> availableGraphemeIndices)
    {
        int availableCount = 0;

        for (int graphemeIndex = 0; graphemeIndex < graphemeCount; graphemeIndex++)
        {
            if (!IsBitSet(originalMaskBitmask, graphemeIndex))
            {
                availableGraphemeIndices[availableCount] = (byte)graphemeIndex;
                availableCount++;
            }
        }

        return availableCount;
    }

    private static int ComputeVariantCharacterLength(int originalTextLength, ulong combinationBitmask, ReadOnlySpan<byte> graphemeCharacterOffsets)
    {
        int variantCharacterLength = originalTextLength;
        ulong remainingMaskBits = combinationBitmask;

        while (remainingMaskBits is not 0)
        {
            int graphemeIndex = GetLowestSetBitIndex(remainingMaskBits);
            int graphemeLength = graphemeCharacterOffsets[graphemeIndex + 1] - graphemeCharacterOffsets[graphemeIndex];
            variantCharacterLength -= graphemeLength - 1;
            remainingMaskBits = ClearLowestSetBit(remainingMaskBits);
        }

        return variantCharacterLength;
    }

    private static void WriteVariantCharacters(Span<char> destination, FusejiVariantStringCreationState state)
    {
        ReadOnlySpan<byte> graphemeCharacterOffsets = state.GraphemeCharacterOffsets;
        int destinationIndex = 0;

        for (int graphemeIndex = 0; graphemeIndex < state.TotalGraphemeCount; graphemeIndex++)
        {
            if (IsBitSet(state.CombinationBitmask, graphemeIndex))
            {
                destination[destinationIndex] = JapaneseUtils.NormalizedFuseji;
                destinationIndex++;
                continue;
            }

            int sourceStart = graphemeCharacterOffsets[graphemeIndex];
            int sourceLength = graphemeCharacterOffsets[graphemeIndex + 1] - sourceStart;
            CopyGraphemeCharacters(state.OriginalText, sourceStart, sourceLength, destination, ref destinationIndex);
        }
    }

    private static void CopyGraphemeCharacters(ReadOnlySpan<char> originalText, int sourceStart, int sourceLength, Span<char> destination, ref int destinationIndex)
    {
        switch (sourceLength)
        {
            case 1:
                destination[destinationIndex] = originalText[sourceStart];
                destinationIndex += 1;
                break;

            case 2:
                destination[destinationIndex] = originalText[sourceStart];
                destination[destinationIndex + 1] = originalText[sourceStart + 1];
                destinationIndex += 2;
                break;

            default:
                originalText.Slice(sourceStart, sourceLength).CopyTo(destination[destinationIndex..]);
                destinationIndex += sourceLength;
                break;
        }
    }

    // A grapheme's bitmask bit is set when that grapheme is (or would be) rendered as a '○' mask.
    // These are one-line leaf operations called from the hottest loop (one candidate combination
    // is checked at a time), so forcing inlining here is worth it. Everything above is either
    // cold (runs once per constructed enumerator or once per successfully matched string) or
    // large enough that the JIT's own heuristics already make the right call.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong SetBit(ulong bitmask, int bitIndex)
    {
        return bitmask | (1UL << bitIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBitSet(ulong bitmask, int bitIndex)
    {
        return (bitmask & (1UL << bitIndex)) is not 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetLowestSetBitIndex(ulong bitmask)
    {
        return BitOperations.TrailingZeroCount(bitmask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ClearLowestSetBit(ulong bitmask)
    {
        return bitmask & (bitmask - 1);
    }

    // Two masked graphemes are "consecutive" when their bits are adjacent. Shifting the
    // bitmask by 1..maxAllowedConsecutiveMasks and AND-ing with itself leaves a nonzero
    // result only if a run longer than maxAllowedConsecutiveMasks exists.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasTooManyConsecutiveMaskedGraphemes(ulong maskBitmask, int maxAllowedConsecutiveMasks)
    {
        ulong overlapBitmask = maskBitmask;
        for (int shiftAmount = 1; shiftAmount <= maxAllowedConsecutiveMasks; shiftAmount++)
        {
            overlapBitmask &= maskBitmask >> shiftAmount;
        }

        return overlapBitmask is not 0;
    }
}
