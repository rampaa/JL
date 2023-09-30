using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.EpwingYomichan;

internal sealed class EpwingYomichanRecord : IEpwingRecord, IGetFrequency
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    public string[] Definitions { get; set; }
    public string[]? WordClasses { get; }
    private string[]? DefinitionTags { get; }
    //public int Score { get; init; }
    //public int Sequence { get; init; }
    //public string TermTags { get; init; }

    public EpwingYomichanRecord(List<JsonElement> jsonElement)
    {
        PrimarySpelling = jsonElement[0].ToString().GetPooledString();
        Reading = jsonElement[1].ToString();

        if (Reading is "" || Reading == PrimarySpelling)
        {
            Reading = null;
        }

        else
        {
            Reading = Reading.GetPooledString();
        }

        JsonElement definitionTagsElement = jsonElement[2];
        if (definitionTagsElement.ValueKind is JsonValueKind.String)
        {
            DefinitionTags = definitionTagsElement.ToString().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (DefinitionTags.Length is 0)
            {
                DefinitionTags = null;
            }

            else
            {
                DefinitionTags.DeduplicateStringsInArray();
            }
        }
        else
        {
            DefinitionTags = null;
        }

        WordClasses = jsonElement[3].ToString().Split(' ');
        if (WordClasses.Length is 0)
        {
            WordClasses = null;
        }

        else
        {
            WordClasses.DeduplicateStringsInArray();
        }

        //jsonElement[4].TryGetInt32(out int score);
        //Score = score;

        List<string> definitionList = GetDefinitionsFromJsonArray(jsonElement[5]);
        Definitions = definitionList.TrimStringListToStringArray() ?? Array.Empty<string>();
        Definitions.DeduplicateStringsInArray();

        //jsonElement[6].TryGetInt32(out int sequence);
        //Sequence = sequence;

        //TermTags = jsonElement[7].ToString();
    }

    public string BuildFormattedDefinition(DictOptions? options)
    {
        StringBuilder defResult = new();

        string separator = options is { NewlineBetweenDefinitions.Value: false }
            ? ""
            : "\n";

        for (int i = 0; i < Definitions.Length; i++)
        {
            if (DefinitionTags?.Length > i)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"({DefinitionTags[i]}) ");
            }

            _ = defResult.Append(CultureInfo.InvariantCulture, $"{Definitions[i]}{separator}");
        }

        return defResult.Remove(defResult.Length - separator.Length, separator.Length).ToString();
    }

    public int GetFrequency(Freq freq)
    {
        int frequency = int.MaxValue;

        if (freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling),
                out IList<FrequencyRecord>? freqResults))
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
                 && freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading),
                     out IList<FrequencyRecord>? readingFreqResults))
        {
            int readingFreqResultsCount = readingFreqResults.Count;
            for (int i = 0; i < readingFreqResultsCount; i++)
            {
                FrequencyRecord readingFreqResult = readingFreqResults[i];

                if (Reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(Reading))
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
        return jsonElement.TryGetProperty("content", out JsonElement contentElement)
            ? GetDefinitionsFromJsonElement(contentElement)
            : null;
    }

    private static List<string> GetDefinitionsFromJsonString(JsonElement jsonElement)
    {
        return jsonElement.ToString()
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(static s => s.Replace("\\\"", "\"", StringComparison.Ordinal))
            .ToList();
    }
}
