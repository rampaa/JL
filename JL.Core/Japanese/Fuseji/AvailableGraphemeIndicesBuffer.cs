using System.Runtime.CompilerServices;

namespace JL.Core.Japanese.Fuseji;

[InlineArray(10)]
internal struct AvailableGraphemeIndicesBuffer : IEquatable<AvailableGraphemeIndicesBuffer>
{
    private byte _firstElement;

    public readonly bool Equals(AvailableGraphemeIndicesBuffer other)
    {
        return _firstElement == other._firstElement;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is AvailableGraphemeIndicesBuffer other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return _firstElement;
    }

    public static bool operator ==(AvailableGraphemeIndicesBuffer left, AvailableGraphemeIndicesBuffer right) => left.Equals(right);

    public static bool operator !=(AvailableGraphemeIndicesBuffer left, AvailableGraphemeIndicesBuffer right) => !(left == right);
}
