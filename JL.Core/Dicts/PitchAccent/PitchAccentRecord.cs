using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JL.Core.Dicts.Interfaces;

namespace JL.Core.Dicts.PitchAccent;

internal sealed class PitchAccentRecord : IDictRecord, IEquatable<PitchAccentRecord>
{
    public string Spelling { get; }
    public string? Reading { get; }
    public byte Position { get; }

    public PitchAccentRecord(string spelling, string? reading, byte position)
    {
        Spelling = spelling;
        Reading = reading;
        Position = position;
    }

    internal static byte GetPositionFromPitchString(string positionStr)
    {
        Debug.Assert(positionStr.Length <= byte.MaxValue);

        bool foundHighPitch = false;
        byte pitchStringLength = (byte)positionStr.Length;
        for (byte i = 0; i < pitchStringLength; i++)
        {
            if (foundHighPitch)
            {
                if (positionStr[i] is 'L')
                {
                    return i;
                }
            }
            else if (positionStr[i] is 'H')
            {
                foundHighPitch = true;
            }
        }

        return 0;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Spelling.GetHashCode(StringComparison.Ordinal), Reading?.GetHashCode(StringComparison.Ordinal) ?? 0);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is PitchAccentRecord pitchAccentRecord
            && Spelling == pitchAccentRecord.Spelling
            && Reading == pitchAccentRecord.Reading;
    }

    public bool Equals([NotNullWhen(true)] PitchAccentRecord? other)
    {
        return other is not null
            && Spelling == other.Spelling
            && Reading == other.Reading;
    }

    public bool Equals([NotNullWhen(true)] IDictRecord? other)
    {
        return other is PitchAccentRecord pitchAccentRecord && Equals(pitchAccentRecord);
    }

    public static bool operator ==(PitchAccentRecord? left, PitchAccentRecord? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(PitchAccentRecord? left, PitchAccentRecord? right) => !(left == right);
}
