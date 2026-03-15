using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JL.Core.Dicts.JMnedict;

[method: JsonConstructor]
internal readonly struct Translation(string[] transDetArray, string[]? nameTypeArray) : IEquatable<Translation>
{
    public string[] TransDetArray { get; } = transDetArray;
    public string[]? NameTypeArray { get; } = nameTypeArray;
    //public List<string> XRefList { get; } = xRefList;

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17 * 37;
            foreach (string transDet in TransDetArray)
            {
                hash = (hash * 37) + transDet.GetHashCode(StringComparison.Ordinal);
            }

            if (NameTypeArray is not null)
            {
                foreach (string nameType in NameTypeArray)
                {
                    hash = (hash * 37) + nameType.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            return hash;
        }
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Translation translation && Equals(translation);
    }

    public bool Equals(Translation other)
    {
        return TransDetArray.SequenceEqual(other.TransDetArray)
            && (NameTypeArray is null
                ? other.NameTypeArray is null
                : other.NameTypeArray is not null && NameTypeArray.SequenceEqual(other.NameTypeArray));
    }

    public static bool operator ==(Translation left, Translation right) => left.Equals(right);
    public static bool operator !=(Translation left, Translation right) => !left.Equals(right);
}
