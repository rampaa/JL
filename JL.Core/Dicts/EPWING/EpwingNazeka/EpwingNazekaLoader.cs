using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.EpwingNazeka;

internal static class EpwingNazekaLoader
{
    public static async Task Load(Dict dict)
    {
        if (!File.Exists(dict.Path))
        {
            return;
        }

        // TODO: Utf8JsonReader?
        List<JsonElement>? jsonObjects;

        FileStream fileStream = File.OpenRead(dict.Path);
        await using (fileStream.ConfigureAwait(false))
        {
            jsonObjects = await JsonSerializer.DeserializeAsync<List<JsonElement>>(fileStream)
                .ConfigureAwait(false);
        }

        Dictionary<string, IList<IDictRecord>> nazekaEpwingDict = dict.Contents;

        foreach (JsonElement jsonObj in jsonObjects!.Skip(1))
        {
            string reading = jsonObj.GetProperty("r").ToString().GetPooledString();

            List<string>? spellingList = new();
            foreach (JsonElement spellingJsonElement in jsonObj.GetProperty("s").EnumerateArray())
            {
                string spelling = spellingJsonElement.ToString();

                if (!string.IsNullOrWhiteSpace(spelling))
                {
                    spellingList.Add(spelling.GetPooledString());
                }
            }

            if (spellingList.Count is 0)
            {
                spellingList = null;
            }

            List<string>? definitionList = new();
            foreach (JsonElement definitionJsonElement in jsonObj.GetProperty("l").EnumerateArray())
            {
                string definition = definitionJsonElement.ToString();

                if (!string.IsNullOrWhiteSpace(definition))
                {
                    definitionList.Add(definition.GetPooledString());
                }
            }

            if (definitionList.Count is 0)
            {
                definitionList = null;
            }

            string[]? definitions = definitionList?.ToArray();

            if (spellingList is not null)
            {
                string primarySpelling = spellingList[0];

                string[]? alternativeSpellings = spellingList.RemoveAtToArray(0);

                string key = JapaneseUtils.KatakanaToHiragana(reading).GetPooledString();

                EpwingNazekaRecord tempRecord = new(primarySpelling, reading, alternativeSpellings, definitions);

                if (nazekaEpwingDict.TryGetValue(key, out IList<IDictRecord>? result))
                {
                    result.Add(tempRecord);
                }
                else
                {
                    nazekaEpwingDict.Add(key, new List<IDictRecord> { tempRecord });
                }

                for (int i = 0; i < spellingList.Count; i++)
                {
                    primarySpelling = spellingList[i];

                    alternativeSpellings = spellingList.RemoveAtToArray(i);

                    key = JapaneseUtils.KatakanaToHiragana(primarySpelling).GetPooledString();

                    tempRecord = new EpwingNazekaRecord(primarySpelling, reading, alternativeSpellings, definitions);

                    if (!EpwingUtils.IsValidEpwingResultForDictType(tempRecord, dict))
                    {
                        continue;
                    }

                    if (nazekaEpwingDict.TryGetValue(key, out result))
                    {
                        result.Add(tempRecord);
                    }
                    else
                    {
                        nazekaEpwingDict.Add(key, new List<IDictRecord> { tempRecord });
                    }
                }
            }

            else
            {
                string key = JapaneseUtils.KatakanaToHiragana(reading).GetPooledString();

                EpwingNazekaRecord tempRecord = new(reading, null, null, definitions);

                if (!EpwingUtils.IsValidEpwingResultForDictType(tempRecord, dict))
                {
                    continue;
                }

                if (nazekaEpwingDict.TryGetValue(key, out IList<IDictRecord>? result))
                {
                    result.Add(tempRecord);
                }
                else
                {
                    nazekaEpwingDict.Add(key, new List<IDictRecord> { tempRecord });
                }
            }
        }

        foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents.TrimExcess();
    }
}
