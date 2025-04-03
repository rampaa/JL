using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMnedict;

[method: JsonConstructor]
internal readonly struct Translation(List<string> nameTypeList, List<string> transDetList) : IEquatable<Translation>
{
    public List<string> NameTypeList { get; } = nameTypeList;
    public List<string> TransDetList { get; } = transDetList;
    //public List<string> XRefList { get; }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17 * 37;
            foreach (ref readonly string nameType in NameTypeList.AsSpan())
            {
                hash = (hash * 37) + nameType.GetHashCode(StringComparison.Ordinal);
            }

            foreach (ref readonly string transDet in TransDetList.AsSpan())
            {
                hash = (hash * 37) + transDet.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is Translation translation && Equals(translation);
    }

    public bool Equals(Translation other)
    {
        return NameTypeList.AsSpan().SequenceEqual(other.NameTypeList.AsSpan()) && TransDetList.AsSpan().SequenceEqual(other.TransDetList.AsSpan());
    }

    public static bool operator ==(Translation left, Translation right) => left.Equals(right);
    public static bool operator !=(Translation left, Translation right) => !left.Equals(right);
}
