using System.Text.Json;

namespace JL.Core.Dicts.EPWING.EpwingYomichan;

public static class EpwingYomichanLoader
{
    public static async Task Load(Dict dict)
    {
        if (!Directory.Exists(dict.Path) && !File.Exists(dict.Path))
            return;

        string[] jsonFiles = Directory.EnumerateFiles(dict.Path, "*_bank_*.json", SearchOption.TopDirectoryOnly)
            .Where(s => s.Contains("term") || s.Contains("kanji"))
            .ToArray();

        foreach (string jsonFile in jsonFiles)
        {
            List<List<JsonElement>>? jsonObjects;

            FileStream openStream = File.OpenRead(jsonFile);
            await using (openStream.ConfigureAwait(false))
            {
                jsonObjects = await JsonSerializer
                    .DeserializeAsync<List<List<JsonElement>>>(openStream)
                    .ConfigureAwait(false);
            }

            if (jsonObjects == null)
                continue;

            foreach (List<JsonElement> jsonObj in jsonObjects)
            {
                DictionaryBuilder(new EpwingYomichanResult(jsonObj), dict);
            }
        }

        dict.Contents.TrimExcess();
    }

    private static void DictionaryBuilder(EpwingYomichanResult yomichanResult, Dict dict)
    {
        if (!EpwingUtils.IsValidEpwingResultForDictType(yomichanResult, dict))
            return;

        string hiraganaExpression = Kana.KatakanaToHiraganaConverter(yomichanResult.PrimarySpelling);

        if (!string.IsNullOrEmpty(yomichanResult.Reading))
        {
            string hiraganaReading = Kana.KatakanaToHiraganaConverter(yomichanResult.Reading);

            if (dict.Contents.TryGetValue(hiraganaReading, out List<IResult>? tempList2))
                tempList2.Add(yomichanResult);
            else
                dict.Contents.Add(hiraganaReading, new List<IResult> { yomichanResult });
        }

        if (dict.Contents.TryGetValue(hiraganaExpression, out List<IResult>? tempList))
            tempList.Add(yomichanResult);
        else
            dict.Contents.Add(hiraganaExpression, new List<IResult> { yomichanResult });
    }
}
