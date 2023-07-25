using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.YomichanKanji;

internal sealed class YomichanKanjiRecord : IDictRecord
{
    public string[]? OnReadings { get; }
    public string[]? KunReadings { get; }
    //public string[]? Tags { get; }
    private List<string>? Definitions { get; }
    private List<string>? Stats { get; }

    public YomichanKanjiRecord(IReadOnlyList<JsonElement> jsonElement)
    {
        OnReadings = jsonElement[1].ToString().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();
        if (OnReadings.Length is 0)
        {
            OnReadings = null;
        }

        KunReadings = jsonElement[2].ToString().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();
        if (KunReadings.Length is 0)
        {
            KunReadings = null;
        }

        //Tags = jsonElement[3].ToString().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        //if (Tags.Length is 0)
        //{
        //    Tags = null;
        //}

        JsonElement definitionsArray = jsonElement[4];
        Definitions = new List<string>();
        foreach (JsonElement definition in definitionsArray.EnumerateArray())
        {
            Definitions.Add(definition.ToString());
        }

        if (Definitions.Count is 0)
        {
            Definitions = null;
        }

        JsonElement statsElement = jsonElement[5];
        Stats = new List<string>();
        foreach (JsonProperty stat in statsElement.EnumerateObject())
        {
            Stats.Add(string.Create(CultureInfo.InvariantCulture, $"{stat.Name}: {stat.Value}"));
        }

        if (Stats.Count is 0)
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

        for (int i = 0; i < Stats.Count; i++)
        {
            _ = statResult.Append(CultureInfo.InvariantCulture, $"{Stats[i]}\n");
        }

        return statResult.Remove(statResult.Length - 1, 1).ToString();
    }
}
