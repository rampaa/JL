using System.Text.Json;

namespace JL.Core.Dicts.EPWING.EpwingYomichan;

public class EpwingYomichanResult : IResult
{
    public List<string>? Definitions { get; set; }
    public string? Reading { get; }
    public List<string>? WordClasses { get; }
    public string PrimarySpelling { get; }

    //public string DefinitionTags { get; init; }
    //public int Score { get; init; }
    //public int Sequence { get; init; }
    //public string TermTags { get; init; }

    public EpwingYomichanResult(List<JsonElement> jsonElement)
    {
        PrimarySpelling = jsonElement[0].ToString();
        Reading = jsonElement[1].ToString();

        if (Reading == "" || Reading == PrimarySpelling)
            Reading = null;

        //DefinitionTags = jsonElement[2].ToString();

        WordClasses = jsonElement[3].ToString().Split(" ").ToList();

        if (!WordClasses.Any())
            WordClasses = null;

        //jsonElement[4].TryGetInt32(out int score);
        //Score = score;

        JsonElement definitionElement = jsonElement[5][0];

        if (definitionElement.ValueKind == JsonValueKind.Object)
        {
            definitionElement = definitionElement.GetProperty("content")[0];
        }

        Definitions = definitionElement.ToString()
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Replace("\\\"", "\""))
            .ToList();

        if (!Definitions.Any())
            Definitions = null;

        //jsonElement[6].TryGetInt32(out int sequence);
        //Sequence = sequence;

        //TermTags = jsonElement[7].ToString();
    }

    public EpwingYomichanResult(string primarySpelling, string reading, List<string> definitions, List<string> wordClasses)
    {
        Definitions = definitions;
        Reading = reading;
        WordClasses = wordClasses;
        PrimarySpelling = primarySpelling;
    }
}
