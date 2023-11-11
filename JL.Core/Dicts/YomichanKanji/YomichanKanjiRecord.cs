using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;

namespace JL.Core.Dicts.YomichanKanji;

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

        List<string> definitionList = new();
        JsonElement definitionsArray = jsonElement[4];
        foreach (JsonElement definition in definitionsArray.EnumerateArray())
        {
            definitionList.Add(definition.GetString()!);
        }

        Definitions = definitionList.TrimStringListToStringArray();

        List<string> statList = new();
        JsonElement statsElement = jsonElement[5];
        foreach (JsonProperty stat in statsElement.EnumerateObject())
        {
            statList.Add(string.Create(CultureInfo.InvariantCulture, $"{stat.Name}: {stat.Value}"));
        }

        Stats = statList.Count > 0 ? statList.ToArray() : null;
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

        for (int i = 0; i < Definitions.Length; i++)
        {
            _ = defResult.Append(CultureInfo.InvariantCulture, $"{Definitions[i]}{separator}");
        }

        return defResult.Remove(defResult.Length - separator.Length, separator.Length).ToString();
    }

    public string? BuildFormattedStats()
    {
        if (Stats is null)
        {
            return null;
        }

        StringBuilder statResult = new();

        for (int i = 0; i < Stats.Length; i++)
        {
            _ = statResult.Append(CultureInfo.InvariantCulture, $"{Stats[i]}\n");
        }

        return statResult.Remove(statResult.Length - 1, 1).ToString();
    }
}
