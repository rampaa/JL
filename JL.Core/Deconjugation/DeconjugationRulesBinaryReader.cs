using System.Buffers.Binary;
using System.Text.Unicode;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

internal ref struct DeconjugationRulesBinaryReader(ReadOnlySpan<byte> data)
{
    private ReadOnlySpan<byte> _remaining = data;

    internal readonly bool IsComplete => _remaining.IsEmpty;

    internal bool TryReadInt32(out int value)
    {
        if (!BinaryPrimitives.TryReadInt32LittleEndian(_remaining, out value))
        {
            return false;
        }

        _remaining = _remaining[sizeof(int)..];
        return true;
    }

    internal bool TryReadCount(int minimumEntryLength, out int count)
    {
        return TryReadInt32(out count) && count >= 0 && count <= _remaining.Length / minimumEntryLength;
    }

    internal bool TryReadIndex(int count, out int index)
    {
        return TryReadInt32(out index) && index >= 0 && index < count;
    }

    internal bool TryReadUInt16(out ushort value)
    {
        if (!BinaryPrimitives.TryReadUInt16LittleEndian(_remaining, out value))
        {
            return false;
        }

        _remaining = _remaining[sizeof(ushort)..];
        return true;
    }

    internal bool TryReadRuleType(out RuleType type)
    {
        if (_remaining.IsEmpty || _remaining[0] > (byte)RuleType.NeverFinal)
        {
            type = default;
            return false;
        }

        type = (RuleType)_remaining[0];
        _remaining = _remaining[1..];
        return true;
    }

    internal bool TryReadString(out string value)
    {
        if (!TryReadCount(1, out int byteLength) || !Utf8.IsValid(_remaining[..byteLength]))
        {
            value = "";
            return false;
        }

        value = TextUtils.Utf8NoBom.GetString(_remaining[..byteLength]);
        _remaining = _remaining[byteLength..];
        return true;
    }
}
