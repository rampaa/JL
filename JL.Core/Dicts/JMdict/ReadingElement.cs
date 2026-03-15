using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMdict;

[method: JsonConstructor]
internal readonly struct ReadingElement(string reb, List<string>? reRestrList, string[]? reInfArray) : IEquatable<ReadingElement>
{
    public string Reb { get; } = reb; // Reading in kana. e.g. むすめ
    public List<string>? ReRestrList { get; } = reRestrList; // ReRestrList = Keb. The reading is only valid for this specific keb.
    public string[]? ReInfArray { get; } = reInfArray; // e.g. gikun
    // public bool ReNokanji { get; } // Is kana insufficient to notate the right spelling?
    // public List<string>? RePriList { get; } = rePriList; // e.g. ichi1

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Reb.GetHashCode(StringComparison.Ordinal);

            if (ReRestrList is not null)
            {
                foreach (string reRestr in ReRestrList.AsReadOnlySpan())
                {
                    hash = (hash * 37) + reRestr.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            if (ReInfArray is not null)
            {
                foreach (string reInf in ReInfArray)
                {
                    hash = (hash * 37) + reInf.GetHashCode(StringComparison.Ordinal);
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
        return obj is ReadingElement readingElement && Equals(readingElement);
    }

    public bool Equals(ReadingElement other)
    {
        return Reb == other.Reb
            && (ReRestrList is null
                ? other.ReRestrList is null
                : other.ReRestrList is not null && ReRestrList.AsReadOnlySpan().SequenceEqual(other.ReRestrList.AsReadOnlySpan()))
            && (ReInfArray is null
                ? other.ReInfArray is null
                : other.ReInfArray is not null && ReInfArray.SequenceEqual(other.ReInfArray));
    }

    public static bool operator ==(in ReadingElement left, in ReadingElement right) => left.Equals(right);
    public static bool operator !=(in ReadingElement left, in ReadingElement right) => !left.Equals(right);
}
