using System.Text.Json;

namespace JL.Core.Dicts.Kanjium
{
    public class KanjiumResult : IResult
    {
        public string Spelling { get; set; }
        public string? Reading { get; set; }
        public int Position { get; set; }

        public KanjiumResult(string spelling, string? reading, int position)
        {
            Spelling = spelling;
            Reading = reading;
            Position = position;
        }

        public KanjiumResult(List<JsonElement> jsonObject)
        {
            Spelling = jsonObject[0].ToString();

            JsonElement jO = jsonObject[2];

            Reading = jO.GetProperty("reading").ToString();

            if (Spelling == Reading)
                Reading = null;

            Position = jO.GetProperty("pitches")[0].GetProperty("position").GetInt32();
        }
    }
}
