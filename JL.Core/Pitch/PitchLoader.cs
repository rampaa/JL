using System.Text.Json;
using JL.Core.Dicts;

namespace JL.Core.Pitch;

public static class PitchLoader
{
    public static async Task Load(Dict dict)
    {
        Dictionary<string, List<IResult>> pitchDict = dict.Contents;

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

            if (jsonObjects == null)
                continue;

            foreach (List<JsonElement> jsonObject in jsonObjects)
            {
                PitchResult newEntry = new(jsonObject);

                if (newEntry.Position == -1)
                    continue;

                string spellingInHiragana = Kana.KatakanaToHiraganaConverter(newEntry.Spelling);

                if (pitchDict.TryGetValue(spellingInHiragana, out List<IResult>? result))
                {
                    result.Add(newEntry);
                }

                else
                {
                    pitchDict[spellingInHiragana] = new List<IResult> { newEntry };
                }

                if (!string.IsNullOrEmpty(newEntry.Reading))
                {
                    string readingInHiragana = Kana.KatakanaToHiraganaConverter(newEntry.Reading);

                    if (pitchDict.TryGetValue(readingInHiragana, out List<IResult>? readingResult))
                    {
                        readingResult.Add(newEntry);
                    }

                    else
                    {
                        pitchDict[readingInHiragana] = new List<IResult> { newEntry };
                    }
                }
            }
        }

        pitchDict.TrimExcess();
    }
}
