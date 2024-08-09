using System.Collections.Frozen;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal static class EpwingNazekaLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        // TODO: Utf8JsonReader?
        List<JsonElement>? jsonObjects;

        FileStream fileStream = File.OpenRead(fullPath);
        await using (fileStream.ConfigureAwait(false))
        {
            jsonObjects = await JsonSerializer.DeserializeAsync<List<JsonElement>>(fileStream)
                .ConfigureAwait(false);
        }

        IDictionary<string, IList<IDictRecord>> nazekaEpwingDict = dict.Contents;

        foreach (JsonElement jsonObj in jsonObjects!.Skip(1))
        {
            string reading = jsonObj.GetProperty("r").GetString()!.GetPooledString();

            List<string>? spellingList = [];
            foreach (JsonElement spellingJsonElement in jsonObj.GetProperty("s").EnumerateArray())
            {
                string? spelling = spellingJsonElement.GetString();

                if (!string.IsNullOrWhiteSpace(spelling))
                {
                    spellingList.Add(spelling.GetPooledString());
                }
            }

            if (spellingList.Count is 0)
            {
                spellingList = null;
            }

            List<string> definitionList = [];
            foreach (JsonElement definitionJsonElement in jsonObj.GetProperty("l").EnumerateArray())
            {
                string? definition = definitionJsonElement.GetString();
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    definitionList.Add(definition.GetPooledString());
                }
            }

            if (definitionList.Count is 0)
            {
                continue;
            }

            string[] definitions = definitionList.ToArray();
            definitions.DeduplicateStringsInArray();

            if (spellingList is not null)
            {
                string primarySpelling = spellingList[0];
                if (!EpwingUtils.IsValidEpwingResultForDictType(primarySpelling, reading, definitions, dict))
                {
                    continue;
                }

                bool nonKanjiDict = dict.Type is not DictType.NonspecificKanjiNazeka;

                string primarySpellingInHiragana = nonKanjiDict
                    ? JapaneseUtils.KatakanaToHiragana(primarySpelling).GetPooledString()
                    : primarySpelling.GetPooledString();

                EpwingNazekaRecord record = new(primarySpelling, reading, spellingList.RemoveAtToArray(0), definitions);
                AddRecordToDictionary(primarySpellingInHiragana, record, nazekaEpwingDict);
                if (nonKanjiDict && dict.Type is not DictType.NonspecificNameNazeka)
                {
                    string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading).GetPooledString();
                    if (primarySpellingInHiragana != readingInHiragana)
                    {
                        AddRecordToDictionary(readingInHiragana, record, nazekaEpwingDict);
                    }
                }

                int spellingListCount = spellingList.Count;
                for (int i = 1; i < spellingListCount; i++)
                {
                    string alternativeSpelling = spellingList[i];
                    if (!EpwingUtils.IsValidEpwingResultForDictType(alternativeSpelling, reading, definitions, dict))
                    {
                        continue;
                    }

                    string alternativeSpellingInHiragana = nonKanjiDict
                        ? JapaneseUtils.KatakanaToHiragana(alternativeSpelling).GetPooledString()
                        : alternativeSpelling.GetPooledString();

                    if (primarySpellingInHiragana != alternativeSpellingInHiragana)
                    {
                        AddRecordToDictionary(alternativeSpellingInHiragana, new EpwingNazekaRecord(alternativeSpelling, reading, spellingList.RemoveAtToArray(i), definitions), nazekaEpwingDict);
                    }
                }
            }

            else
            {
                if (!EpwingUtils.IsValidEpwingResultForDictType(reading, null, definitions, dict))
                {
                    continue;
                }

                EpwingNazekaRecord record = new(reading, null, null, definitions);
                AddRecordToDictionary(JapaneseUtils.KatakanaToHiragana(reading).GetPooledString(), record, nazekaEpwingDict);
            }
        }

        foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static void AddRecordToDictionary(string keyInHiragana, IDictRecord record, IDictionary<string, IList<IDictRecord>> dictionary)
    {
        if (dictionary.TryGetValue(keyInHiragana, out IList<IDictRecord>? result))
        {
            result.Add(record);
        }
        else
        {
            dictionary[keyInHiragana] = [record];
        }
    }
}
