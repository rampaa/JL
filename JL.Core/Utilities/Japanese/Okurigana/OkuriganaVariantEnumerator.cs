using System.Buffers;
using System.Runtime.CompilerServices;

namespace JL.Core.Utilities.Japanese.Okurigana;

internal ref struct OkuriganaVariantEnumerator
{
    private readonly string _expression;
    private readonly string _reading;
    private OkuriganaSegment[]? _segments;
    private readonly int _segmentCount;
    private readonly int _kanjiCount;
    private ulong _mask;
    private readonly ulong _maxMask;

    public OkuriganaVariantEnumerator(string expression, string reading)
    {
        _expression = expression;
        _reading = reading;
        _segments = null;
        _segmentCount = 0;
        _kanjiCount = 0;
        _mask = 0;
        Current = "";
        _segments = ArrayPool<OkuriganaSegment>.Shared.Rent(expression.Length * 2);

        if (!OkuriganaVariantGenerator.TryGetUniqueSegmentation(_expression, _reading, _segments, out _segmentCount, out _kanjiCount))
        {
            Dispose();
            return;
        }

        if (_kanjiCount is < 2 or > 63)
        {
            Dispose();
            return;
        }

        _maxMask = CreateLowBitsMask(_kanjiCount);
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
        Current = OkuriganaVariantGenerator.Assemble(_expression, _reading, _segments, _segmentCount, _mask);
        return true;
    }

    public void Dispose()
    {
        if (_segments is not null)
        {
            ArrayPool<OkuriganaSegment>.Shared.Return(_segments);
            _segments = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CreateLowBitsMask(int bitCount)
    {
        return (1UL << bitCount) - 1;
    }
}
