using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
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

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "term_bank_*.json", SearchOption.TopDirectoryOnly);

        bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiWithWordSchemaYomichan;
        bool nonNameDict = dict.Type is not DictType.NonspecificNameYomichan;

        foreach (string jsonFile in jsonFiles)
        {
            ReadOnlyMemory<ReadOnlyMemory<JsonElement>> jsonElementLists;

            FileStream fileStream = File.OpenRead(jsonFile);
            await using (fileStream.ConfigureAwait(false))
            {
                jsonElementLists = await JsonSerializer
                    .DeserializeAsync<ReadOnlyMemory<ReadOnlyMemory<JsonElement>>>(fileStream)
                    .ConfigureAwait(false);
            }

            foreach (ref readonly ReadOnlyMemory<JsonElement> jsonElements in jsonElementLists.Span)
            {
                EpwingYomichanRecord? record = GetEpwingYomichanRecord(jsonElements.Span, dict);
                if (record is not null)
                {
                    AddToDictionary(record, dict, nonKanjiDict, nonNameDict);
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static EpwingYomichanRecord? GetEpwingYomichanRecord(ReadOnlySpan<JsonElement> jsonElements, Dict dict)
    {
        string primarySpelling = jsonElements[0].GetString()!.GetPooledString();
        string? reading = jsonElements[1].GetString();
        reading = string.IsNullOrWhiteSpace(reading) || reading == primarySpelling
            ? null
            : reading.GetPooledString();

        if (string.IsNullOrWhiteSpace(primarySpelling))
        {
            if (reading is null)
            {
                return null;
            }

            primarySpelling = reading;
            reading = null;
        }

        string[]? definitionTags = null;
        ref readonly JsonElement definitionTagsElement = ref jsonElements[2];
        if (definitionTagsElement.ValueKind is JsonValueKind.String)
        {
            string? definitionTagsStr = definitionTagsElement.GetString();
            Debug.Assert(definitionTagsStr is not null);

            definitionTags = definitionTagsStr.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (definitionTags.Length is 0)
            {
                definitionTags = null;
            }
            else
            {
                definitionTags.DeduplicateStringsInArray();
            }
        }

        if (definitionTags?.Length is 1 && definitionTags[0] is "子" or "句")
        {
            return null;
        }

        List<string> imagePaths = [];
        string[]? definitions = EpwingYomichanUtils.GetDefinitions(jsonElements[5], dict, imagePaths);
        definitions?.DeduplicateStringsInArray();

        if (definitions is null
            ? imagePaths.Count is 0
            : !EpwingUtils.IsValidEpwingResultForDictType(primarySpelling, reading, definitions, dict))
        {
            return null;
        }

        string? wordClassesStr = jsonElements[3].GetString();
        Debug.Assert(wordClassesStr is not null);
        string[]? wordClasses = wordClassesStr.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
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

        return new EpwingYomichanRecord(primarySpelling, reading, definitions, wordClasses, definitionTags, imagePaths.TrimToArray());
    }

    private static void AddToDictionary(EpwingYomichanRecord yomichanRecord, Dict dict, bool nonKanjiDict, bool nonNameDict)
    {
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

        if (nonKanjiDict && nonNameDict && yomichanRecord.Reading is not null)
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
