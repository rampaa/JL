using System.Text.Json;
using JL.Core.Dicts;

namespace JL.Core.PitchAccent;

public static class PitchAccentLoader
{
    public static async Task Load(Dict dict)
    {
        Dictionary<string, List<IDictRecord>> pitchDict = dict.Contents;

        string[] jsonFiles = Directory.GetFiles(dict.Path, "term*bank_*.json");

        foreach (string jsonFile in jsonFiles)
        {
            List<List<JsonElement>>? jsonObjects;

            FileStream openStream = File.OpenRead(jsonFile);
            await using (openStream.ConfigureAwait(false))
            {
                jsonObjects = await JsonSerializer.DeserializeAsync<List<List<JsonElement>>>(openStream)
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

                string spellingInHiragana = Kana.KatakanaToHiragana(newEntry.Spelling);

                if (pitchDict.TryGetValue(spellingInHiragana, out List<IDictRecord>? result))
                {
                    result.Add(newEntry);
                }

                else
                {
                    pitchDict[spellingInHiragana] = new List<IDictRecord> { newEntry };
                }

                if (!string.IsNullOrEmpty(newEntry.Reading))
                {
                    string readingInHiragana = Kana.KatakanaToHiragana(newEntry.Reading);

                    if (pitchDict.TryGetValue(readingInHiragana, out List<IDictRecord>? readingResult))
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

        pitchDict.TrimExcess();
    }
}
