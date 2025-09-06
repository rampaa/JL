using System.Collections.Frozen;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
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
            ReadOnlyMemory<ReadOnlyMemory<JsonElement>> jsonObjects;

            FileStream fileStream = File.OpenRead(jsonFile);
            await using (fileStream.ConfigureAwait(false))
            {
                jsonObjects = await JsonSerializer
                    .DeserializeAsync<ReadOnlyMemory<ReadOnlyMemory<JsonElement>>>(fileStream, JsonOptions.DefaultJso)
                    .ConfigureAwait(false);
            }

            foreach (ref readonly ReadOnlyMemory<JsonElement> jsonObjectMemory in jsonObjects.Span)
            {
                PitchAccentRecord newEntry = new(jsonObjectMemory.Span);
                if (newEntry.Position is byte.MaxValue || string.IsNullOrWhiteSpace(newEntry.Spelling))
                {
                    continue;
                }

                string spellingInHiragana = JapaneseUtils.KatakanaToHiragana(newEntry.Spelling).GetPooledString();
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
                    string readingInHiragana = JapaneseUtils.KatakanaToHiragana(newEntry.Reading).GetPooledString();
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

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
