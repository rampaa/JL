using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using JL.Core.Utilities.Japanese;
using JL.Core.Utilities.Japanese.Mazegaki;

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

                    PitchAccentRecord record = new(jsonObject);
                    if (record.Position is byte.MaxValue || string.IsNullOrWhiteSpace(record.Spelling))
                    {
                        continue;
                    }

                    string spellingInHiragana = JapaneseUtils.NormalizeText(record.Spelling).GetPooledString();
                    if (DictUtils.AddRecordToDictionary(spellingInHiragana, record, pitchDict))
                    {
                        if (record.Reading is not null)
                        {
                            string readingInHiragana = JapaneseUtils.NormalizeText(record.Reading).GetPooledString();
                            if (spellingInHiragana != readingInHiragana)
                            {
                                _ = DictUtils.AddRecordToDictionary(readingInHiragana, record, pitchDict);
                            }

                            foreach (string variant in MazegakiVariantGenerator.GenerateMixedVariants(spellingInHiragana, readingInHiragana))
                            {
                                _ = DictUtils.AddRecordToDictionary(variant, record, pitchDict);
                            }
                        }
                    }
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
