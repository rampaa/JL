using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;

namespace JL.Core.Dicts.EPWING.EpwingYomichan;

public class EpwingYomichanRecord : IEpwingRecord, IDictRecordWithGetFrequency
{
    public List<string>? Definitions { get; set; }
    public string? Reading { get; }
    public List<string>? WordClasses { get; }
    public string PrimarySpelling { get; }
    private List<string>? DefinitionTags { get; }
    //public int Score { get; init; }
    //public int Sequence { get; init; }
    //public string TermTags { get; init; }

    public EpwingYomichanRecord(List<JsonElement> jsonElement)
    {
        PrimarySpelling = jsonElement[0].ToString();
        Reading = jsonElement[1].ToString();

        if (Reading is "" || Reading == PrimarySpelling)
        {
            Reading = null;
        }

        DefinitionTags = new();

        JsonElement definitionTagsElement = jsonElement[2];
        if (definitionTagsElement.ValueKind is JsonValueKind.Array)
        {
            foreach (JsonElement definitionTag in definitionTagsElement.EnumerateArray())
            {
                DefinitionTags.Add(definitionTag.ToString());
            }
        }

        else //if (definitionTagsElement.ValueKind is JsonValueKind.String)
        {
            DefinitionTags = definitionTagsElement.ToString()
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        if (DefinitionTags.Count is 0)
        {
            DefinitionTags = null;
        }

        WordClasses = jsonElement[3].ToString().Split(" ").ToList();

        if (WordClasses.Count is 0)
        {
            WordClasses = null;
        }

        //jsonElement[4].TryGetInt32(out int score);
        //Score = score;

        Definitions = new List<string>();

        JsonElement definitionsArray = jsonElement[5];
        if (definitionsArray.GetArrayLength() > 1)
        {
            foreach (JsonElement definitionElement in jsonElement[5].EnumerateArray())
            {
                Definitions.Add(definitionElement.ToString());
            }
        }

        else
        {
            JsonElement definitionElement = jsonElement[5][0];

            if (definitionElement.ValueKind is JsonValueKind.Object)
            {
                definitionElement = definitionElement.GetProperty("content")[0];
            }

            Definitions = definitionElement.ToString()
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Replace("\\\"", "\""))
                .ToList();
        }

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
}
