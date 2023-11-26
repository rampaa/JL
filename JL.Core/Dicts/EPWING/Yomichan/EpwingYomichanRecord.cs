using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal sealed class EpwingYomichanRecord : IEpwingRecord, IGetFrequency
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    public string[] Definitions { get; set; }
    public string[]? WordClasses { get; }
    public string[]? DefinitionTags { get; }
    //public int Score { get; }
    //public int Sequence { get; }
    //public string TermTags { get; }

    public EpwingYomichanRecord(string primarySpelling, string? reading, string[] definitions, string[]? wordClasses, string[]? definitionTags)
    {
        PrimarySpelling = primarySpelling;
        Reading = reading;
        Definitions = definitions;
        WordClasses = wordClasses;
        DefinitionTags = definitionTags;
    }

    public EpwingYomichanRecord(List<JsonElement> jsonElement)
    {
        PrimarySpelling = jsonElement[0].GetString()!.GetPooledString();
        Reading = jsonElement[1].GetString();

        if (string.IsNullOrEmpty(Reading) || Reading == PrimarySpelling)
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
            DefinitionTags = definitionTagsElement.GetString()!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

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

        WordClasses = jsonElement[3].GetString()!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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

        Definitions = GetDefinitions(jsonElement[5]) ?? Array.Empty<string>();
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
            for (int i = 0; i < freqResults.Count; i++)
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

        else if (Reading is not null
                 && freq.Contents.TryGetValue(JapaneseUtils.KatakanaToHiragana(Reading),
                     out IList<FrequencyRecord>? readingFreqResults))
        {
            for (int i = 0; i < readingFreqResults.Count; i++)
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

    public int GetFrequencyFromDB(Freq freq)
    {
        int frequency = int.MaxValue;
        List<FrequencyRecord> freqResults = FreqDBManager.GetRecordsFromDB(freq.Name, JapaneseUtils.KatakanaToHiragana(PrimarySpelling));
        if (freqResults.Count > 0)
        {
            for (int i = 0; i < freqResults.Count; i++)
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

        else if (Reading is not null)
        {
            List<FrequencyRecord> readingFreqResults = FreqDBManager.GetRecordsFromDB(freq.Name, JapaneseUtils.KatakanaToHiragana(Reading));
            if (readingFreqResults.Count > 0)
            {
                for (int i = 0; i < readingFreqResults.Count; i++)
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
        }

        return frequency;
    }

    private static string[]? GetDefinitions(JsonElement jsonElement)
    {
        List<string> definitions = new();
        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            string? definition = null;
            if (definitionElement.ValueKind is JsonValueKind.String)
            {
                definition = definitionElement.GetString()!.Trim();
            }

            else if (definitionElement.ValueKind is JsonValueKind.Array)
            {
                definition = GetDefinitionsFromJsonArray(definitionElement);
            }

            else if (definitionElement.ValueKind is JsonValueKind.Object)
            {
                definition = GetDefinitionsFromJsonObject(definitionElement).Content;
            }

            if (definition is not null)
            {
                definitions.Add(definition.GetPooledString());
            }
        }

        return definitions.TrimStringListToStringArray();
    }

    private static string? GetDefinitionsFromJsonArray(JsonElement jsonElement, string? parentTag = null)
    {
        StringBuilder stringBuilder = new();

        bool first = true;
        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            if (definitionElement.ValueKind is JsonValueKind.String)
            {
                _ = stringBuilder.Append(definitionElement.GetString());
            }

            else if (definitionElement.ValueKind is JsonValueKind.Array)
            {
                _ = stringBuilder.Append(GetDefinitionsFromJsonArray(definitionElement));
            }

            else if (definitionElement.ValueKind is JsonValueKind.Object)
            {
                if (first)
                {
                    first = false;
                    parentTag = null;
                }

                YomichanContent contentResult = GetDefinitionsFromJsonObject(definitionElement, parentTag);
                if (contentResult.Content is not null)
                {
                    if (contentResult.Tag is null or "span")
                    {
                        _ = stringBuilder.Append(contentResult.Content);
                    }

                    else if (contentResult.Tag is "ruby" or "rt")
                    {
                        _ = stringBuilder.Append(CultureInfo.InvariantCulture, $" ({contentResult.Content}) ");
                    }

                    else //if (contentResult.Tag is "div" or "a" or "li" or "ul" or "ol" or "p" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6")
                    {
                        _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{contentResult.Content.TrimStart('\n')}");
                    }
                }
            }
        }

        return stringBuilder.Length > 0
            ? stringBuilder.ToString().Trim()
            : null;
    }

    private static YomichanContent GetDefinitionsFromJsonObject(JsonElement jsonElement, string? parentTag = null)
    {
        if (jsonElement.TryGetProperty("content", out JsonElement contentElement))
        {
            string? tag = null;
            if (jsonElement.TryGetProperty("tag", out JsonElement tagElement))
            {
                tag = tagElement.GetString();
            }

            if (contentElement.ValueKind is JsonValueKind.String)
            {
                return new YomichanContent(parentTag ?? tag, contentElement.GetString()!.Trim());
            }

            if (contentElement.ValueKind is JsonValueKind.Array)
            {
                return new YomichanContent(parentTag ?? tag, GetDefinitionsFromJsonArray(contentElement, tag));
            }

            if (contentElement.ValueKind is JsonValueKind.Object)
            {
                return GetDefinitionsFromJsonObject(contentElement, parentTag ?? tag);
            }
        }

        return default;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        EpwingYomichanRecord epwingYomichanRecordObj = (EpwingYomichanRecord)obj;
        return PrimarySpelling == epwingYomichanRecordObj.PrimarySpelling
               && Reading == epwingYomichanRecordObj.Reading
               && epwingYomichanRecordObj.Definitions.SequenceEqual(Definitions);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 37) + PrimarySpelling.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + Reading?.GetHashCode(StringComparison.Ordinal) ?? 37;

            foreach (string definition in Definitions)
            {
                hash = (hash * 37) + definition.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }
}
