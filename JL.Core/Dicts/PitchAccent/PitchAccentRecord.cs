using System.Text.Json;
using System.Text.RegularExpressions;
using JL.Core.Utilities;

namespace JL.Core.Dicts.PitchAccent;

public sealed record class PitchAccentRecord : IDictRecord
{
    public string Spelling { get; }
    public string? Reading { get; }
    public byte Position { get; }

    internal PitchAccentRecord(string spelling, string? reading, byte position)
    {
        Spelling = spelling;
        Reading = reading;
        Position = position;
    }

    internal PitchAccentRecord(List<JsonElement> jsonElements)
    {
        Spelling = jsonElements[0].GetString()!.GetPooledString();

        JsonElement thirdJsonElement = jsonElements[2];
        if (thirdJsonElement.ValueKind is JsonValueKind.Object)
        {
            Reading = thirdJsonElement.GetProperty("reading").GetString();
            Position = thirdJsonElement.GetProperty("pitches")[0].GetProperty("position").TryGetByte(out byte position)
                ? position
                : byte.MaxValue;
        }

        else
        {
            Reading = jsonElements[1].GetString();

            string? positionStr = jsonElements[5][0].GetString();
            if (positionStr is not null)
            {
                Match match = Utils.s_numberRegex.Match(positionStr);
                Position = match.Success && byte.TryParse(match.ValueSpan, out byte position)
                    ? position
                    : byte.MaxValue;
            }
            else
            {
                Position = byte.MaxValue;
            }
        }

        Reading = Spelling == Reading
            ? null
            : Reading!.GetPooledString();
    }
}
