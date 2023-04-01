using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;

namespace JL.Core.Dicts.EPWING.EpwingYomichan;

internal sealed class EpwingYomichanRecord : IEpwingRecord, IDictRecordWithGetFrequency
{
    public List<string>? Definitions { get; set; }
    public string? Reading { get; }
    public List<string>? WordClasses { get; }
    public string PrimarySpelling { get; }
    private List<string>? DefinitionTags { get; }
    //public int Score { get; init; }
    //public int Sequence { get; init; }
    //public string TermTags { get; init; }

    public EpwingYomichanRecord(IReadOnlyList<JsonElement> jsonElement)
    {
        PrimarySpelling = jsonElement[0].ToString();
        Reading = jsonElement[1].ToString();

        if (Reading is "" || Reading == PrimarySpelling)
        {
            Reading = null;
        }

        JsonElement definitionTagsElement = jsonElement[2];
        if (definitionTagsElement.ValueKind is JsonValueKind.String)
        {
            DefinitionTags = new List<string>();

            DefinitionTags = definitionTagsElement.ToString().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

            if (DefinitionTags.Count is 0)
            {
                DefinitionTags = null;
            }
        }

        WordClasses = jsonElement[3].ToString().Split(' ').ToList();
        if (WordClasses.Count is 0)
        {
            WordClasses = null;
        }

        //jsonElement[4].TryGetInt32(out int score);
        //Score = score;

        Definitions = GetDefinitionsFromJsonArray(jsonElement[5]);
        if (Definitions.Count is 0)
        {
            Definitions = null;
        }

        //jsonElement[6].TryGetInt32(out int sequence);
        //Sequence = sequence;

        //TermTags = jsonElement[7].ToString();
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
            if (DefinitionTags?.Count > i)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({DefinitionTags[i]}) ");
            }

            _ = defResult.Append(Definitions[i] + separator);
        }

        return defResult.ToString().TrimEnd(' ', '\n');
    }

    public int GetFrequency(Freq freq)
    {
        int frequency = int.MaxValue;

        if (freq.Contents.TryGetValue(Kana.KatakanaToHiragana(PrimarySpelling),
                out List<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];

                if (Reading == freqResult.Spelling || PrimarySpelling == freqResult.Spelling)
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }
        }

        else if (!string.IsNullOrEmpty(Reading)
                 && freq.Contents.TryGetValue(Kana.KatakanaToHiragana(Reading),
                     out List<FrequencyRecord>? readingFreqResults))
        {
            int readingFreqResultsCount = readingFreqResults.Count;
            for (int i = 0; i < readingFreqResultsCount; i++)
            {
                FrequencyRecord readingFreqResult = readingFreqResults[i];

                if (Reading == readingFreqResult.Spelling && Kana.IsKatakana(Reading))
                {
                    if (frequency > readingFreqResult.Frequency)
                    {
                        frequency = readingFreqResult.Frequency;
                    }
                }
            }
        }

        return frequency;
    }

    private static List<string>? GetDefinitionsFromJsonElement(JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.Array => GetDefinitionsFromJsonArray(jsonElement),
            JsonValueKind.Object => GetDefinitionsFromJsonObject(jsonElement),
            JsonValueKind.String => GetDefinitionsFromJsonString(jsonElement),
            JsonValueKind.Number => null,
            JsonValueKind.Undefined => null,
            JsonValueKind.True => null,
            JsonValueKind.False => null,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    private static List<string> GetDefinitionsFromJsonArray(JsonElement jsonElement)
    {
        List<string> definitions = new();

        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            List<string>? defs = GetDefinitionsFromJsonElement(definitionElement);
            if (defs is not null)
            {
                definitions.AddRange(defs);
            }
        }

        return definitions;
    }

    private static List<string>? GetDefinitionsFromJsonObject(JsonElement jsonElement)
    {
        if (jsonElement.TryGetProperty("content", out JsonElement contentElement))
        {
            return GetDefinitionsFromJsonElement(contentElement);
        }

        return null;
    }

    private static List<string> GetDefinitionsFromJsonString(JsonElement jsonElement)
    {
        return jsonElement.ToString()
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(static s => s.Replace("\\\"", "\""))
            .ToList();
    }
}
