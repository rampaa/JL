using System.Collections.Frozen;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.KanjiDict;

internal static class YomichanKanjiLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "kanji_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
            List<List<JsonElement>>? jsonObjects;

            FileStream fileStream = File.OpenRead(jsonFile);
            await using (fileStream.ConfigureAwait(false))
            {
                jsonObjects = await JsonSerializer
                    .DeserializeAsync<List<List<JsonElement>>>(fileStream, Utils.s_jso)
                    .ConfigureAwait(false);
            }

            if (jsonObjects is null)
            {
                continue;
            }

            foreach (List<JsonElement> jsonObj in jsonObjects)
            {
                YomichanKanjiRecord yomichanKanjiRecord = new(jsonObj);
                string kanji = jsonObj[0].GetString()!.GetPooledString();

                if (dict.Contents.TryGetValue(kanji, out IList<IDictRecord>? kanjiResult))
                {
                    kanjiResult.Add(yomichanKanjiRecord);
                }
                else
                {
                    dict.Contents[kanji] = [yomichanKanjiRecord];
                }
            }
        }

        foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
