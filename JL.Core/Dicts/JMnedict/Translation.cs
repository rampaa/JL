using System.Text.Json.Serialization;

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
            foreach (string nameType in NameTypeList)
            {
                hash = (hash * 37) + nameType.GetHashCode(StringComparison.Ordinal);
            }

            foreach (string transDet in TransDetList)
            {
                hash = (hash * 37) + transDet.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is Translation other
            && NameTypeList.SequenceEqual(other.NameTypeList)
            && TransDetList.SequenceEqual(other.TransDetList);
    }

    public bool Equals(Translation other)
    {
        return NameTypeList.SequenceEqual(other.NameTypeList) && TransDetList.SequenceEqual(other.TransDetList);
    }

    public static bool operator ==(Translation left, Translation right) => left.Equals(right);
    public static bool operator !=(Translation left, Translation right) => !left.Equals(right);
}
