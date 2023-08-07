using System.Text.Json;
using JL.Core.Dicts;
using JL.Core.Utilities;

namespace JL.Core.PitchAccent;

internal static class PitchAccentLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        Dictionary<string, IList<IDictRecord>> pitchDict = dict.Contents;

        string[] jsonFiles = Directory.GetFiles(fullPath, "term*bank_*.json");

        foreach (string jsonFile in jsonFiles)
        {
            List<List<JsonElement>>? jsonObjects;

            FileStream fileStream = File.OpenRead(jsonFile);
            await using (fileStream.ConfigureAwait(false))
            {
                jsonObjects = await JsonSerializer.DeserializeAsync<List<List<JsonElement>>>(fileStream)
                    .ConfigureAwait(false);
            }

            if (jsonObjects is null)
            {
                continue;
            }

            foreach (List<JsonElement> jsonObject in jsonObjects)
            {
                PitchAccentRecord newEntry = new(jsonObject);

                if (newEntry.Position is -1)
                {
                    continue;
                }

                string spellingInHiragana = JapaneseUtils.KatakanaToHiragana(newEntry.Spelling).GetPooledString();

                if (pitchDict.TryGetValue(spellingInHiragana, out IList<IDictRecord>? result))
                {
                    result.Add(newEntry);
                }

                else
                {
                    pitchDict[spellingInHiragana] = new List<IDictRecord> { newEntry };
                }

                if (!string.IsNullOrEmpty(newEntry.Reading))
                {
                    string readingInHiragana = JapaneseUtils.KatakanaToHiragana(newEntry.Reading).GetPooledString();

                    if (pitchDict.TryGetValue(readingInHiragana, out IList<IDictRecord>? readingResult))
                    {
                        readingResult.Add(newEntry);
                    }

                    else
                    {
                        pitchDict[readingInHiragana] = new List<IDictRecord> { newEntry };
                    }
                }
            }
        }

        foreach ((string key, IList<IDictRecord> recordList) in pitchDict)
        {
            pitchDict[key] = recordList.ToArray();
        }

        pitchDict.TrimExcess();
    }
}
