using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.KanjiDict;

internal static class YomichanKanjiLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "kanji_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
            FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
            await using (fileStream.ConfigureAwait(false))
            {
                await foreach (JsonElement[]? jsonObj in JsonSerializer.DeserializeAsyncEnumerable<JsonElement[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                {
                    Debug.Assert(jsonObj is not null);

                    YomichanKanjiRecord yomichanKanjiRecord = new(jsonObj);
                    string kanji = jsonObj[0].GetString()!.GetPooledString();
                    if (string.IsNullOrWhiteSpace(kanji))
                    {
                        continue;
                    }

                    if (dict.Contents.TryGetValue(kanji, out IList<IDictRecord>? kanjiResult))
                    {
                        if (!kanjiResult.Contains(yomichanKanjiRecord))
                        {
                            kanjiResult.Add(yomichanKanjiRecord);
                        }
                    }
                    else
                    {
                        dict.Contents[kanji] = [yomichanKanjiRecord];
                    }
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
