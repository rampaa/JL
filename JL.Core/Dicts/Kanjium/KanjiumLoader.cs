using System.Text.Json;

namespace JL.Core.Dicts.Kanjium
{
    public class KanjiumLoader
    {
        public static async Task Load(DictType dictType, string dictPath)
        {
            if (!Directory.Exists(dictPath) && !File.Exists(dictPath))
                return;

            Dictionary<string, List<IResult>> kanjiumDict = Storage.Dicts[dictType].Contents;

            string[] jsonFiles = Directory.GetFiles(dictPath, "term_meta_bank_*.json");

            foreach (string jsonFile in jsonFiles)
            {
                await using FileStream openStream = File.OpenRead(jsonFile);
                List<List<JsonElement>> jsonObjects = await JsonSerializer.DeserializeAsync<List<List<JsonElement>>>(openStream)
                    .ConfigureAwait(false);

                foreach (List<JsonElement> jsonObject in jsonObjects)
                {
                    KanjiumResult newEntry = new(jsonObject);

                    if (kanjiumDict.TryGetValue(newEntry.Spelling, out List<IResult> result))
                    {
                        result.Add(newEntry);
                    }

                    else
                    {
                        kanjiumDict[newEntry.Spelling] = new List<IResult> { newEntry };
                    }

                    if (!string.IsNullOrEmpty(newEntry.Reading))
                    {
                        if (kanjiumDict.TryGetValue(newEntry.Reading, out List<IResult> readingResult))
                        {
                            readingResult.Add(newEntry);
                        }

                        else
                        {
                            kanjiumDict[newEntry.Reading] = new List<IResult> { newEntry };
                        }
                    }
                }
            }

            kanjiumDict.TrimExcess();
        }
    }
}
