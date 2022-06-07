using System.Text.Json;
using System.Text.RegularExpressions;
using JL.Core.Dicts;

namespace JL.Core.Pitch;

public class PitchResult : IResult
{
    public string Spelling { get; set; }
    public string? Reading { get; set; }
    public int Position { get; set; }

    public PitchResult(string spelling, string? reading, int position)
    {
        Spelling = spelling;
        Reading = reading;
        Position = position;
    }

    public PitchResult(List<JsonElement> jsonObject)
    {
        Spelling = jsonObject[0].ToString();

        if (jsonObject[2].ValueKind == JsonValueKind.Object)
        {
            Reading = jsonObject[2].GetProperty("reading").ToString();

            Position = jsonObject[2].GetProperty("pitches")[0].GetProperty("position").GetInt32();
        }

        else
        {
            Reading = jsonObject[1].ToString();

            if (int.TryParse(Regex.Match(jsonObject[5][0].ToString(), @"\[(\d)\]").Groups[1].Value, out int position))
            {
                Position = position;
            }

            else
            {
                Position = -1;
            }
        }

        if (Spelling == Reading)
            Reading = null;
    }
}
