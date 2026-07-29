using System.Runtime.CompilerServices;

namespace JL.Core.Japanese.Fuseji;

[InlineArray(10)]
internal struct CombinationGraphemeIndicesBuffer : IEquatable<CombinationGraphemeIndicesBuffer>
{
    private byte _firstElement;

    public readonly bool Equals(CombinationGraphemeIndicesBuffer other)
    {
        return _firstElement == other._firstElement;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is CombinationGraphemeIndicesBuffer other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return _firstElement;
    }

    public static bool operator ==(CombinationGraphemeIndicesBuffer left, CombinationGraphemeIndicesBuffer right) => left.Equals(right);

    public static bool operator !=(CombinationGraphemeIndicesBuffer left, CombinationGraphemeIndicesBuffer right) => !(left == right);
}
