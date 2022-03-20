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

                    string spellingInHiragana = Kana.KatakanaToHiraganaConverter(newEntry.Spelling);

                    if (kanjiumDict.TryGetValue(spellingInHiragana, out List<IResult> result))
                    {
                        result.Add(newEntry);
                    }

                    else
                    {
                        kanjiumDict[spellingInHiragana] = new List<IResult> { newEntry };
                    }

                    if (!string.IsNullOrEmpty(newEntry.Reading))
                    {
                        string readingInHiragana = Kana.KatakanaToHiraganaConverter(newEntry.Reading);

                        if (kanjiumDict.TryGetValue(readingInHiragana, out List<IResult> readingResult))
                        {
                            readingResult.Add(newEntry);
                        }

                        else
                        {
                            kanjiumDict[readingInHiragana] = new List<IResult> { newEntry };
                        }
                    }
                }
            }

            kanjiumDict.TrimExcess();
        }
    }
}
