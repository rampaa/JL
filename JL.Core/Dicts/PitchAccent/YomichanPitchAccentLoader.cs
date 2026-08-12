using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;

namespace JL.Core.Dicts.PitchAccent;

internal static class YomichanPitchAccentLoader
{
    public const int Size = 434991;
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        IDictionary<string, IList<IDictRecord>> pitchDict = dict.Contents;
        GenerateMazegakiVariantsOption? generateMazegakiOption = dict.Options.GenerateMazegakiVariants;
        Debug.Assert(generateMazegakiOption is not null);
        bool generateMazegaki = generateMazegakiOption.Value;

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = dict.Options.GenerateFusejiVariants;
        Debug.Assert(generateFusejiVariantsOption is not null);
        bool generateFusejiVariants = generateFusejiVariantsOption.Value;

        int maxSearchKeyLengthForFusejiGeneration;
        int maxTotalFuseji;
        int maxConsecutiveFuseji;
        if (generateFusejiVariants)
        {
            Debug.Assert(dict.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
            maxSearchKeyLengthForFusejiGeneration = dict.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

            Debug.Assert(dict.Options.MaxTotalFusejiCount is not null);
            maxTotalFuseji = dict.Options.MaxTotalFusejiCount.Value;

            Debug.Assert(dict.Options.MaxConsecutiveFusejiCount is not null);
            maxConsecutiveFuseji = dict.Options.MaxConsecutiveFusejiCount.Value;
        }
        else
        {
            maxSearchKeyLengthForFusejiGeneration = 0;
            maxTotalFuseji = 0;
            maxConsecutiveFuseji = 0;
        }

        // TODO: When migrating to .NET 10 again, use CompareOptions.NumericOrdering to order JSON files
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
                    if (DictUtils.AddRecordToDictionary(spellingInHiragana, record, dict))
                    {
                        if (generateFusejiVariants)
                        {
                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(spellingInHiragana, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                            {
                                _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, dict);
                            }
                        }

                        if (record.Reading is not null)
                        {
                            string readingInHiragana = JapaneseUtils.NormalizeText(record.Reading).GetPooledString();
                            if (spellingInHiragana != readingInHiragana)
                            {
                                if (DictUtils.AddRecordToDictionary(readingInHiragana, record, dict))
                                {
                                    if (generateFusejiVariants)
                                    {
                                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                        {
                                            _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, dict);
                                        }
                                    }

                                    if (generateMazegaki)
                                    {
                                        foreach (string mazegaki in MazegakiVariantGenerator.GenerateMazegakiVariants(spellingInHiragana, readingInHiragana))
                                        {
                                            if (DictUtils.AddRecordToDictionary(mazegaki, record, dict))
                                            {
                                                if (generateFusejiVariants)
                                                {
                                                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegaki, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                                    {
                                                        _ = DictUtils.AddRecordToDictionary(fusejiVariant, record, dict);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
