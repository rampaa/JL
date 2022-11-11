using System.Text.Json;
using System.Text.RegularExpressions;
using JL.Core.Dicts;

namespace JL.Core.PitchAccent;

public class PitchAccentRecord : IDictRecord
{
    public string Spelling { get; }
    public string? Reading { get; }
    public int Position { get; }

    public PitchAccentRecord(List<JsonElement> jsonObject)
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

            if (int.TryParse(Regex.Match(jsonObject[5][0].ToString(), @"(\[|［)(\d)(］|\])").Groups[2].Value, out int position))
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
