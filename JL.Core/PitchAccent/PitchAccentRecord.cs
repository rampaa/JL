using System.Text.Json;
using System.Text.RegularExpressions;
using JL.Core.Dicts;

namespace JL.Core.PitchAccent;

public class PitchAccentRecord : IDictRecord
{
    public string Spelling { get; }
    public string? Reading { get; }
    public int Position { get; }

    private static readonly Regex s_positionRegex = new("@\"(\\[|［)(\\d)(］|\\])", RegexOptions.Compiled);

    public PitchAccentRecord(List<JsonElement> jsonObject)
    {
        Spelling = jsonObject[0].ToString();

        if (jsonObject[2].ValueKind is JsonValueKind.Object)
        {
            Reading = jsonObject[2].GetProperty("reading").ToString();

            Position = jsonObject[2].GetProperty("pitches")[0].GetProperty("position").GetInt32();
        }

        else
        {
            Reading = jsonObject[1].ToString();

            Position = int.TryParse(s_positionRegex.Match(jsonObject[5][0].ToString()).Groups[2].Value, out int position)
                ? position
                : -1;
        }

        if (Spelling == Reading)
        {
            Reading = null;
        }
    }
}
