using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.YomichanKanji;

public class YomichanKanjiRecord : IDictRecord
{
    public List<string>? OnReadings { get; }
    public List<string>? KunReadings { get; }
    //public List<string>? Tags { get; }
    private List<string>? Definitions { get; }
    private List<string>? Stats { get; }

    public YomichanKanjiRecord(List<JsonElement> jsonElement)
    {
        OnReadings = jsonElement[1].ToString().Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        if (!OnReadings.Any())
        {
            OnReadings = null;
        }

        KunReadings = jsonElement[2].ToString().Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        if (!KunReadings.Any())
        {
            KunReadings = null;
        }

        //Tags = jsonElement[3].ToString().Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        //if (!Tags.Any())
        //{
        //    Tags = null;
        //}

        JsonElement definitionsArray = jsonElement[4];
        if (definitionsArray.ValueKind is JsonValueKind.Array)
        {
            Definitions = new List<string>();
            foreach (JsonElement definition in definitionsArray.EnumerateArray())
            {
                Definitions.Add(definition.ToString());
            }

            if (!Definitions.Any())
            {
                Definitions = null;
            }
        }

        JsonElement statsElement = jsonElement[5];
        Stats = new List<string>();
        if (statsElement.ValueKind is JsonValueKind.Array)
        {
            foreach (JsonElement stat in statsElement.EnumerateArray())
            {
                Stats.Add(stat.ToString());
            }
        }

        else if (statsElement.ValueKind is JsonValueKind.Object)
        {
            foreach (JsonProperty stat in statsElement.EnumerateObject())
            {
                Stats.Add(stat.Name + ": " + stat.Value);
            }
        }

        if (!Stats.Any())
        {
            Stats = null;
        }
    }
    public string? BuildFormattedDefinition(DictOptions? options)
    {
        if (Definitions is null)
        {
            return null;
        }

        StringBuilder defResult = new();

        string separator = options?.NewlineBetweenDefinitions?.Value ?? true
            ? "\n"
            : "; ";

        for (int i = 0; i < Definitions.Count; i++)
        {
            _ = defResult.Append(Definitions[i] + separator);
        }

        return defResult.ToString().TrimEnd(' ', '\n');
    }

    public string? BuildFormattedStats()
    {
        if (Stats is null)
        {
            return null;
        }

        StringBuilder statResult = new();

        for (int i = 0; i < Stats.Count; i++)
        {
            _ = statResult.Append(Stats[i] + "\n");
        }

        return statResult.ToString().TrimEnd('\n');
    }
}
