using System.Collections.Frozen;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.PitchAccent;

internal static class YomichanPitchAccentLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        IDictionary<string, IList<IDictRecord>> pitchDict = dict.Contents;

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "term*bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
            List<List<JsonElement>>? jsonObjects;

            FileStream fileStream = File.OpenRead(jsonFile);
            await using (fileStream.ConfigureAwait(false))
            {
                jsonObjects = await JsonSerializer
                    .DeserializeAsync<List<List<JsonElement>>>(fileStream, Utils.s_jsoNotIgnoringNull)
                    .ConfigureAwait(false);
            }

            if (jsonObjects is null)
            {
                continue;
            }

            foreach (List<JsonElement> jsonObject in jsonObjects)
            {
                PitchAccentRecord newEntry = new(jsonObject);

                if (newEntry.Position is byte.MaxValue)
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
                    pitchDict[spellingInHiragana] = [newEntry];
                }

                if (!string.IsNullOrEmpty(newEntry.Reading))
                {
                    string readingInHiragana = JapaneseUtils.KatakanaToHiragana(newEntry.Reading).GetPooledString();
                    if (spellingInHiragana != readingInHiragana)
                    {
                        if (pitchDict.TryGetValue(readingInHiragana, out IList<IDictRecord>? readingResult))
                        {
                            readingResult.Add(newEntry);
                        }
                        else
                        {
                            pitchDict[readingInHiragana] = [newEntry];
                        }
                    }
                }
            }
        }

        foreach ((string key, IList<IDictRecord> recordList) in pitchDict)
        {
            pitchDict[key] = recordList.ToArray();
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
