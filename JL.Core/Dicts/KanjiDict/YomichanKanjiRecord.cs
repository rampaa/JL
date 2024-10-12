using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;

namespace JL.Core.Dicts.KanjiDict;

internal sealed class YomichanKanjiRecord : IDictRecord
{
    public string[]? OnReadings { get; }
    public string[]? KunReadings { get; }
    //public string[]? Tags { get; }
    public string[]? Definitions { get; }
    public string[]? Stats { get; }

    public YomichanKanjiRecord(string[]? onReadings, string[]? kunReadings, string[]? definitions, string[]? stats)
    {
        OnReadings = onReadings;
        KunReadings = kunReadings;
        Definitions = definitions;
        Stats = stats;
    }

    public YomichanKanjiRecord(List<JsonElement> jsonElement)
    {
        OnReadings = jsonElement[1].GetString()!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (OnReadings.Length is 0)
        {
            OnReadings = null;
        }

        else
        {
            OnReadings.DeduplicateStringsInArray();
        }

        KunReadings = jsonElement[2].GetString()!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (KunReadings.Length is 0)
        {
            KunReadings = null;
        }

        else
        {
            KunReadings.DeduplicateStringsInArray();
        }

        //Tags = jsonElement[3].GetString()!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        //if (Tags.Length is 0)
        //{
        //    Tags = null;
        //}

        List<string> definitionList = [];
        JsonElement definitionsArray = jsonElement[4];
        foreach (JsonElement definitionElement in definitionsArray.EnumerateArray())
        {
            string? definition = definitionElement.GetString();
            if (!string.IsNullOrWhiteSpace(definition))
            {
                definitionList.Add(definition);
            }
        }

        Definitions = definitionList.TrimListToArray();

        List<string> statList = [];
        JsonElement statsElement = jsonElement[5];
        foreach (JsonProperty stat in statsElement.EnumerateObject())
        {
            statList.Add(string.Create(CultureInfo.InvariantCulture, $"{stat.Name}: {stat.Value}"));
        }

        Stats = statList.TrimListToArray();
    }

    public string? BuildFormattedDefinition(DictOptions options)
    {
        if (Definitions is null)
        {
            return null;
        }

        if (Definitions.Length is 1)
        {
            return Definitions[0];
        }

        StringBuilder defResult = new();
        string separator = options.NewlineBetweenDefinitions!.Value
            ? "\n"
            : "; ";

        for (int i = 0; i < Definitions.Length; i++)
        {
            _ = defResult.Append(Definitions[i]);
            if (i + 1 != Definitions.Length)
            {
                _ = defResult.Append(separator);
            }
        }

        return defResult.ToString();
    }

    public string? BuildFormattedStats()
    {
        if (Stats is null)
        {
            return null;
        }

        if (Stats.Length is 1)
        {
            return Stats[0];
        }

        StringBuilder statResult = new();
        for (int i = 0; i < Stats.Length; i++)
        {
            _ = statResult.Append(Stats[i]);
            if (i + 1 != Stats.Length)
            {
                _ = statResult.Append('\n');
            }
        }

        return statResult.ToString();
    }
}
