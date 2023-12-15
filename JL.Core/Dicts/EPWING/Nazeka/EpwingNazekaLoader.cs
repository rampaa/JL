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

        Dictionary<string, IList<IDictRecord>> nazekaEpwingDict = dict.Contents;

        foreach (JsonElement jsonObj in jsonObjects!.Skip(1))
        {
            string reading = jsonObj.GetProperty("r").GetString()!.GetPooledString();

            List<string>? spellingList = new();
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

            List<string> definitionList = new();
            foreach (JsonElement definitionJsonElement in jsonObj.GetProperty("l").EnumerateArray())
            {
                string? definition = definitionJsonElement.GetString();
                if (!string.IsNullOrWhiteSpace(definition))
                {
                    if (dict.Type is DictType.Kenkyuusha)
                    {
                        definition = definition.Replace("┏", "", StringComparison.Ordinal);
                    }

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
                string[]? alternativeSpellings = spellingList.RemoveAtToArray(0);

                if (!EpwingUtils.IsValidEpwingResultForDictType(primarySpelling, reading, definitions, dict))
                {
                    continue;
                }

                EpwingNazekaRecord record = new(primarySpelling, reading, alternativeSpellings, definitions);
                AddRecordToDictionary(primarySpelling, record, nazekaEpwingDict);
                if (dict.Type is not DictType.NonspecificNameNazeka and not DictType.NonspecificKanjiNazeka)
                {
                    AddRecordToDictionary(reading, record, nazekaEpwingDict);
                }

                for (int i = 1; i < spellingList.Count; i++)
                {
                    primarySpelling = spellingList[i];
                    alternativeSpellings = spellingList.RemoveAtToArray(i);

                    if (!EpwingUtils.IsValidEpwingResultForDictType(primarySpelling, reading, definitions, dict))
                    {
                        continue;
                    }

                    AddRecordToDictionary(primarySpelling, new EpwingNazekaRecord(primarySpelling, reading, alternativeSpellings, definitions), nazekaEpwingDict);
                }
            }

            else
            {
                if (!EpwingUtils.IsValidEpwingResultForDictType(reading, null, definitions, dict))
                {
                    continue;
                }

                EpwingNazekaRecord record = new(reading, null, null, definitions);
                AddRecordToDictionary(reading, record, nazekaEpwingDict);
            }
        }

        foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents.TrimExcess();
    }

    private static void AddRecordToDictionary(string key, IDictRecord record, Dictionary<string, IList<IDictRecord>> dictionary)
    {
        string keyInHiragana = JapaneseUtils.KatakanaToHiragana(key).GetPooledString();
        if (dictionary.TryGetValue(keyInHiragana, out IList<IDictRecord>? result))
        {
            result.Add(record);
        }
        else
        {
            dictionary[keyInHiragana] = new List<IDictRecord> { record };
        }
    }
}
