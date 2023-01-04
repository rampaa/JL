using System.Text.Json;

namespace JL.Core.Dicts.EPWING.EpwingYomichan;

public static class EpwingYomichanLoader
{
    public static async Task Load(Dict dict)
    {
        if (!Directory.Exists(dict.Path) && !File.Exists(dict.Path))
        {
            return;
        }

        string[] jsonFiles = Directory.EnumerateFiles(dict.Path, "*_bank_*.json", SearchOption.TopDirectoryOnly)
            .Where(s => s.Contains("term") || s.Contains("kanji"))
            .ToArray();

        foreach (string jsonFile in jsonFiles)
        {
            List<List<JsonElement>>? jsonObjects;

            FileStream fileStream = File.OpenRead(jsonFile);
            await using (fileStream.ConfigureAwait(false))
            {
                jsonObjects = await JsonSerializer
                    .DeserializeAsync<List<List<JsonElement>>>(fileStream)
                    .ConfigureAwait(false);
            }

            if (jsonObjects is null)
            {
                continue;
            }

            foreach (List<JsonElement> jsonObj in jsonObjects)
            {
                AddToDictionary(new EpwingYomichanRecord(jsonObj), dict);
            }
        }

        dict.Contents.TrimExcess();
    }

    private static void AddToDictionary(EpwingYomichanRecord yomichanRecord, Dict dict)
    {
        if (!EpwingUtils.IsValidEpwingResultForDictType(yomichanRecord, dict))
        {
            return;
        }

        string hiraganaExpression = Kana.KatakanaToHiragana(yomichanRecord.PrimarySpelling);

        List<IDictRecord>? records;

        if (!string.IsNullOrEmpty(yomichanRecord.Reading))
        {
            string hiraganaReading = Kana.KatakanaToHiragana(yomichanRecord.Reading);

            if (dict.Contents.TryGetValue(hiraganaReading, out records))
            {
                records.Add(yomichanRecord);
            }
            else
            {
                dict.Contents.Add(hiraganaReading, new List<IDictRecord> { yomichanRecord });
            }
        }

        if (dict.Contents.TryGetValue(hiraganaExpression, out records))
        {
            records.Add(yomichanRecord);
        }
        else
        {
            dict.Contents.Add(hiraganaExpression, new List<IDictRecord> { yomichanRecord });
        }
    }
}
