using System.Text.Json;

namespace JL.Core.Dicts.EPWING.EpwingNazeka;

internal static class EpwingNazekaLoader
{
    public static async Task Load(Dict dict)
    {
        List<JsonElement>? jsonObjects;

        FileStream openStream = File.OpenRead(dict.Path);
        await using (openStream.ConfigureAwait(false))
        {
            jsonObjects = await JsonSerializer.DeserializeAsync<List<JsonElement>>(openStream)
                .ConfigureAwait(false);
        }

        Dictionary<string, List<IDictRecord>> nazekaEpwingDict = dict.Contents;

        foreach (JsonElement jsonObj in jsonObjects!.Skip(1))
        {
            string reading = jsonObj.GetProperty("r").ToString();

            List<string>? spellings = jsonObj.GetProperty("s").ToString().TrimStart('[').TrimEnd(']')
                .Split("\",", StringSplitOptions.RemoveEmptyEntries)
                .Select(select => select.Trim('\n', ' ', '"')).ToList();

            List<string>? definitions = jsonObj.GetProperty("l").ToString().TrimStart('[').TrimEnd(']')
                .Split("\",", StringSplitOptions.RemoveEmptyEntries)
                .Select(select => select.Trim('\n', ' ', '"')).ToList();

            if (!definitions.Any())
            {
                definitions = null;
            }

            if (spellings is [""])
            {
                spellings = null;
            }

            if (spellings != null)
            {
                string primarySpelling = spellings[0];

                List<string>? alternativeSpellings = spellings.ToList();
                alternativeSpellings.RemoveAt(0);

                string key = Kana.KatakanaToHiraganaConverter(reading);

                EpwingNazekaRecord tempRecord = new(primarySpelling, reading, alternativeSpellings,
                    definitions);

                if (nazekaEpwingDict.TryGetValue(key, out List<IDictRecord>? result))
                {
                    result.Add(tempRecord);
                }
                else
                {
                    nazekaEpwingDict.Add(key, new List<IDictRecord> { tempRecord });
                }

                for (int i = 0; i < spellings.Count; i++)
                {
                    primarySpelling = spellings[i];

                    alternativeSpellings = spellings.ToList();
                    alternativeSpellings.RemoveAt(i);

                    if (!alternativeSpellings.Any())
                        alternativeSpellings = null;

                    key = Kana.KatakanaToHiraganaConverter(primarySpelling);

                    tempRecord = new(primarySpelling, reading, alternativeSpellings, definitions);

                    if (!EpwingUtils.IsValidEpwingResultForDictType(tempRecord, dict))
                        continue;

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
                string primarySpelling = reading;
                string key = Kana.KatakanaToHiraganaConverter(primarySpelling);

                EpwingNazekaRecord tempRecord = new(primarySpelling, null, null, definitions);

                if (!EpwingUtils.IsValidEpwingResultForDictType(tempRecord, dict))
                    continue;

                if (nazekaEpwingDict.TryGetValue(key, out List<IDictRecord>? result))
                {
                    result.Add(tempRecord);
                }
                else
                {
                    nazekaEpwingDict.Add(key, new List<IDictRecord> { tempRecord });
                }
            }
        }
        nazekaEpwingDict.TrimExcess();
    }
}
