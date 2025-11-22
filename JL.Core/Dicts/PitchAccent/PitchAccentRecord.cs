using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

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

    public PitchAccentRecord(JsonElement[] jsonElements)
    {
        Position = byte.MaxValue;
        Spelling = jsonElements[0].GetString()!.GetPooledString();

        JsonElement thirdJsonElement = jsonElements[2];
        Reading = thirdJsonElement.GetProperty("reading").GetString();

        JsonElement pitchesArray = thirdJsonElement.GetProperty("pitches");
        foreach (JsonElement pitchElement in pitchesArray.EnumerateArray())
        {
            JsonElement positionProperty = pitchElement.GetProperty("position");
            if (positionProperty.ValueKind is JsonValueKind.Number)
            {
                if (positionProperty.TryGetByte(out byte position))
                {
                    Position = position;
                    break;
                }
            }
            else if (positionProperty.ValueKind is JsonValueKind.String)
            {
                Position = 0;

                string? positionStr = positionProperty.GetString();
                Debug.Assert(positionStr is not null);
                Debug.Assert(positionStr.Length <= byte.MaxValue);

                bool foundHighPitch = false;
                byte pitchStringLength = (byte)positionStr.Length;
                for (byte i = 0; i < pitchStringLength; i++)
                {
                    if (foundHighPitch)
                    {
                        if (positionStr[i] is 'L')
                        {
                            Position = i;
                            break;
                        }
                    }
                    else if (positionStr[i] is 'H')
                    {
                        foundHighPitch = true;
                    }
                }

                break;
            }
        }

        Reading = Spelling == Reading
            ? null
            : Reading!.GetPooledString();

        if (string.IsNullOrWhiteSpace(Spelling) && !string.IsNullOrWhiteSpace(Reading))
        {
            Spelling = Reading;
            Reading = null;
        }
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

    public static bool operator ==(PitchAccentRecord? left, PitchAccentRecord? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(PitchAccentRecord? left, PitchAccentRecord? right) => !(left == right);
}
