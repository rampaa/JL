using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.PitchAccent;

internal static class YomichanPitchAccentLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        IDictionary<string, IList<IDictRecord>> pitchDict = dict.Contents;

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "term_meta_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
            FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
            await using (fileStream.ConfigureAwait(false))
            {
                await foreach (JsonElement[]? jsonObject in JsonSerializer.DeserializeAsyncEnumerable<JsonElement[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                {
                    Debug.Assert(jsonObject is not null);

                    PitchAccentRecord newEntry = new(jsonObject);
                    if (newEntry.Position is byte.MaxValue || string.IsNullOrWhiteSpace(newEntry.Spelling))
                    {
                        continue;
                    }

                    string spellingInHiragana = JapaneseUtils.NormalizeText(newEntry.Spelling).GetPooledString();
                    if (pitchDict.TryGetValue(spellingInHiragana, out IList<IDictRecord>? result))
                    {
                        if (!result.Contains(newEntry))
                        {
                            result.Add(newEntry);
                        }
                    }
                    else
                    {
                        pitchDict[spellingInHiragana] = [newEntry];
                    }

                    if (newEntry.Reading is not null)
                    {
                        string readingInHiragana = JapaneseUtils.NormalizeText(newEntry.Reading).GetPooledString();
                        if (spellingInHiragana != readingInHiragana)
                        {
                            if (pitchDict.TryGetValue(readingInHiragana, out IList<IDictRecord>? readingResult))
                            {
                                if (!readingResult.Contains(newEntry))
                                {
                                    readingResult.Add(newEntry);
                                }
                            }
                            else
                            {
                                pitchDict[readingInHiragana] = [newEntry];
                            }
                        }
                    }
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
