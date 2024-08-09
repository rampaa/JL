using System.Collections.Frozen;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "*_bank_*.json", SearchOption.TopDirectoryOnly)
            .Where(static s => s.Contains("term", StringComparison.Ordinal) || s.Contains("kanji", StringComparison.Ordinal));

        foreach (string jsonFile in jsonFiles)
        {
            List<List<JsonElement>>? jsonElementLists;

            FileStream fileStream = File.OpenRead(jsonFile);
            await using (fileStream.ConfigureAwait(false))
            {
                jsonElementLists = await JsonSerializer
                    .DeserializeAsync<List<List<JsonElement>>>(fileStream)
                    .ConfigureAwait(false);
            }

            if (jsonElementLists is null)
            {
                continue;
            }

            foreach (List<JsonElement> jsonElements in jsonElementLists)
            {
                EpwingYomichanRecord? record = GetEpwingYomichanRecord(jsonElements, dict);
                if (record is not null)
                {
                    AddToDictionary(record, dict);
                }
            }
        }

        foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static EpwingYomichanRecord? GetEpwingYomichanRecord(List<JsonElement> jsonElements, Dict dict)
    {
        string primarySpelling = jsonElements[0].GetString()!.GetPooledString();
        string? reading = jsonElements[1].GetString();
        reading = string.IsNullOrEmpty(reading) || reading == primarySpelling
            ? null
            : reading.GetPooledString();

        string[]? definitions = EpwingYomichanUtils.GetDefinitions(jsonElements[5]);
        definitions?.DeduplicateStringsInArray();

        if (definitions is null
            || !EpwingUtils.IsValidEpwingResultForDictType(primarySpelling, reading, definitions, dict))
        {
            return null;
        }

        string[]? definitionTags = null;
        JsonElement definitionTagsElement = jsonElements[2];
        if (definitionTagsElement.ValueKind is JsonValueKind.String)
        {
            definitionTags = definitionTagsElement.GetString()!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (definitionTags.Length is 0)
            {
                definitionTags = null;
            }
            else
            {
                definitionTags.DeduplicateStringsInArray();
            }
        }

        string[]? wordClasses = jsonElements[3].GetString()!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (wordClasses.Length is 0)
        {
            wordClasses = null;
        }
        else
        {
            wordClasses.DeduplicateStringsInArray();
        }

        //jsonElements[4].TryGetInt32(out int score);
        //jsonElements[6].TryGetInt32(out int sequence);
        //string[] termTags = jsonElements[7].ToString();

        return new EpwingYomichanRecord(primarySpelling, reading, definitions, wordClasses, definitionTags);
    }

    private static void AddToDictionary(EpwingYomichanRecord yomichanRecord, Dict dict)
    {
        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiWithWordSchemaYomichan;
        string primarySpellingInHiragana = nonKanjiDict
            ? JapaneseUtils.KatakanaToHiragana(yomichanRecord.PrimarySpelling).GetPooledString()
            : yomichanRecord.PrimarySpelling.GetPooledString();

        if (dict.Contents.TryGetValue(primarySpellingInHiragana, out IList<IDictRecord>? records))
        {
            records.Add(yomichanRecord);
        }
        else
        {
            dict.Contents[primarySpellingInHiragana] = [yomichanRecord];
        }

        if (nonKanjiDict && dict.Type is not DictType.NonspecificNameYomichan && !string.IsNullOrEmpty(yomichanRecord.Reading))
        {
            string readingInHiragana = JapaneseUtils.KatakanaToHiragana(yomichanRecord.Reading).GetPooledString();
            if (primarySpellingInHiragana != readingInHiragana)
            {
                if (dict.Contents.TryGetValue(readingInHiragana, out records))
                {
                    records.Add(yomichanRecord);
                }
                else
                {
                    dict.Contents[readingInHiragana] = [yomichanRecord];
                }
            }
        }
    }
}
