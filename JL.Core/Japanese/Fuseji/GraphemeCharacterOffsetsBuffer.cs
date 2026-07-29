using System.Runtime.CompilerServices;

namespace JL.Core.Japanese.Fuseji;

[InlineArray(11)]
internal struct GraphemeCharacterOffsetsBuffer : IEquatable<GraphemeCharacterOffsetsBuffer>
{
    private byte _firstElement;

    public readonly bool Equals(GraphemeCharacterOffsetsBuffer other)
    {
        return _firstElement == other._firstElement;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is GraphemeCharacterOffsetsBuffer other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return _firstElement;
    }

    public static bool operator ==(GraphemeCharacterOffsetsBuffer left, GraphemeCharacterOffsetsBuffer right) => left.Equals(right);
    public static bool operator !=(GraphemeCharacterOffsetsBuffer left, GraphemeCharacterOffsetsBuffer right) => !(left == right);
}
