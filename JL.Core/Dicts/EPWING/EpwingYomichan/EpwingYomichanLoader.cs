using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.EpwingYomichan;

internal static class EpwingYomichanLoader
{
    public static async Task Load(Dict dict)
    {
        if (!Directory.Exists(dict.Path))
        {
            return;
        }

        List<string> jsonFiles = Directory.EnumerateFiles(dict.Path, "*_bank_*.json", SearchOption.TopDirectoryOnly)
            .Where(static s => s.Contains("term", StringComparison.Ordinal) || s.Contains("kanji", StringComparison.Ordinal))
            .ToList();

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

        foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents.TrimExcess();
    }

    private static void AddToDictionary(IEpwingRecord yomichanRecord, Dict dict)
    {
        if (!EpwingUtils.IsValidEpwingResultForDictType(yomichanRecord, dict))
        {
            return;
        }

        string hiraganaExpression = JapaneseUtils.KatakanaToHiragana(yomichanRecord.PrimarySpelling);

        IList<IDictRecord>? records;

        if (!string.IsNullOrEmpty(yomichanRecord.Reading))
        {
            string hiraganaReading = JapaneseUtils.KatakanaToHiragana(yomichanRecord.Reading);

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
