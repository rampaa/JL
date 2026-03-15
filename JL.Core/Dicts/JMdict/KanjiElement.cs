using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JL.Core.Dicts.JMdict;

[method: JsonConstructor]
internal readonly struct KanjiElement(string keb, string[]? keInfArray) : IEquatable<KanjiElement>
{
    public string Keb { get; } = keb; // e.g. 娘
    public string[]? KeInfArray { get; } = keInfArray; // e.g. Ateji.
    // public List<string>? KePriList { get; } = kePriList; // e.g. gai1

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Keb.GetHashCode(StringComparison.Ordinal);
            if (KeInfArray is not null)
            {
                foreach (string keInf in KeInfArray)
                {
                    hash = (hash * 37) + keInf.GetHashCode(StringComparison.Ordinal);
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
        return obj is KanjiElement kanjiElement && Equals(kanjiElement);
    }

    public bool Equals(KanjiElement other)
    {
        return Keb == other.Keb
            && (KeInfArray is null
                ? other.KeInfArray is null
                : other.KeInfArray is not null && KeInfArray.SequenceEqual(other.KeInfArray));
    }

    public static bool operator ==(KanjiElement left, KanjiElement right) => left.Equals(right);
    public static bool operator !=(KanjiElement left, KanjiElement right) => !left.Equals(right);
}
