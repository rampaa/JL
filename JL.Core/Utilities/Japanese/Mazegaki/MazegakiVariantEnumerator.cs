using System.Buffers;
using System.Runtime.CompilerServices;

namespace JL.Core.Utilities.Japanese.Mazegaki;

internal ref struct MazegakiVariantEnumerator
{
    private readonly string _expression;
    private readonly string _reading;
    private MazegakiSegment[]? _segments;
    private readonly int _segmentCount;
    private ulong _mask;
    private readonly ulong _maxMask;

    public MazegakiVariantEnumerator(string expression, string reading)
    {
        _expression = expression;
        _reading = reading;
        _segments = null;
        _segmentCount = 0;
        _mask = 0;
        Current = "";
        _segments = ArrayPool<MazegakiSegment>.Shared.Rent(expression.Length * 2);

        if (!MazegakiVariantGenerator.TryGetUniqueSegmentation(_expression, _reading, _segments, out _segmentCount, out int kanjiCount)
            || kanjiCount is < 2 or > 63)
        {
            Dispose();
            return;
        }

        _maxMask = CreateLowBitsMask(kanjiCount);
    }

    public string Current { get; private set; }

    public bool MoveNext()
    {
        if (_segments is null)
        {
            return false;
        }

        if (_mask + 1 >= _maxMask)
        {
            Dispose();
            return false;
        }

        ++_mask;
        Current = MazegakiVariantGenerator.Assemble(_expression, _reading, _segments, _segmentCount, _mask);
        return true;
    }

    private void Dispose()
    {
        if (_segments is not null)
        {
            ArrayPool<MazegakiSegment>.Shared.Return(_segments);
            _segments = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CreateLowBitsMask(int bitCount)
    {
        return (1UL << bitCount) - 1;
    }
}
