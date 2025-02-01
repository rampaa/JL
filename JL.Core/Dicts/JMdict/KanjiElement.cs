using System.Text.Json.Serialization;

namespace JL.Core.Dicts.JMdict;

[method: JsonConstructor]
internal readonly struct KanjiElement(string keb, List<string> keInfList) : IEquatable<KanjiElement>
{
    public string Keb { get; } = keb; // e.g. 娘
    public List<string> KeInfList { get; } = keInfList; // e.g. Ateji.
    // public List<string> KePriList { get; } // e.g. gai1

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Keb.GetHashCode(StringComparison.Ordinal);
            foreach (string keInf in KeInfList)
            {
                hash = (hash * 37) + keInf.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is KanjiElement other
            && Keb == other.Keb
            && KeInfList.SequenceEqual(other.KeInfList);
    }

    public bool Equals(KanjiElement other)
    {
        return Keb == other.Keb && KeInfList.SequenceEqual(other.KeInfList);
    }

    public static bool operator ==(KanjiElement left, KanjiElement right) => left.Equals(right);
    public static bool operator !=(KanjiElement left, KanjiElement right) => !left.Equals(right);
}
