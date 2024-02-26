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

    internal PitchAccentRecord(List<JsonElement> jsonObject)
    {
        Spelling = jsonObject[0].GetString()!.GetPooledString();

        if (jsonObject[2].ValueKind is JsonValueKind.Object)
        {
            Reading = jsonObject[2].GetProperty("reading").GetString();
            Position = jsonObject[2].GetProperty("pitches")[0].GetProperty("position").GetByte();
        }

        else
        {
            Reading = jsonObject[1].GetString();

            string? positionStr = jsonObject[5][0].GetString();
            if (positionStr is not null)
            {
                Match match = Utils.s_numberRegex.Match(positionStr);
                if (match.Success)
                {
                    Position = byte.TryParse(match.ValueSpan, out byte position)
                        ? position
                        : byte.MaxValue;
                }
            }
        }

        Reading = Spelling == Reading
            ? null
            : Reading!.GetPooledString();
    }
}
