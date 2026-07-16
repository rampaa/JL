using System.Diagnostics.CodeAnalysis;
using JL.Core.Dicts.Interfaces;

namespace JL.Core.Dicts.KANJIDIC;

internal sealed class KanjidicRecord(
    string[]? definitions,
    string[]? onReadings,
    string[]? kunReadings,
    string[]? nanoriReadings,
    string[]? radicalNames,
    byte strokeCount,
    byte grade,
    int frequency) : IDictRecord, IEquatable<KanjidicRecord>
{
    public string[]? Definitions { get; } = definitions;
    public string[]? OnReadings { get; } = onReadings;
    public string[]? KunReadings { get; } = kunReadings;
    public string[]? NanoriReadings { get; } = nanoriReadings;
    public string[]? RadicalNames { get; } = radicalNames;
    public byte StrokeCount { get; } = strokeCount;
    public byte Grade { get; } = grade;
    public int Frequency { get; } = frequency;

    public string? BuildFormattedDefinition()
    {
        return Definitions is null
            ? null
            : string.Join("; ", Definitions);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is KanjidicRecord other && Equals(other);
    }

    public bool Equals([NotNullWhen(true)] IDictRecord? other)
    {
        return other is KanjidicRecord kanjidicRecord && Equals(kanjidicRecord);
    }

    public bool Equals([NotNullWhen(true)] KanjidicRecord? other)
    {
        return other is not null
            && Frequency == other.Frequency
            && StrokeCount == other.StrokeCount
            && Grade == other.Grade
            && Definitions.SequenceEqual(other.Definitions)
            && KunReadings.SequenceEqual(other.KunReadings)
            && OnReadings.SequenceEqual(other.OnReadings)
            && NanoriReadings.SequenceEqual(other.NanoriReadings)
            && RadicalNames.SequenceEqual(other.RadicalNames);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = HashCode.Combine(Frequency, StrokeCount, Grade);
            if (Definitions is not null)
            {
                foreach (string definition in Definitions)
                {
                    hash = (hash * 37) + definition.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            if (KunReadings is not null)
            {
                foreach (string kunReading in KunReadings)
                {
                    hash = (hash * 37) + kunReading.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            if (OnReadings is not null)
            {
                foreach (string onReading in OnReadings)
                {
                    hash = (hash * 37) + onReading.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            if (NanoriReadings is not null)
            {
                foreach (string nanoriReading in NanoriReadings)
                {
                    hash = (hash * 37) + nanoriReading.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            if (RadicalNames is not null)
            {
                foreach (string radicalName in RadicalNames)
                {
                    hash = (hash * 37) + radicalName.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            return hash;
        }
    }

    public static bool operator ==(KanjidicRecord? left, KanjidicRecord? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(KanjidicRecord? left, KanjidicRecord? right) => !(left == right);
}
