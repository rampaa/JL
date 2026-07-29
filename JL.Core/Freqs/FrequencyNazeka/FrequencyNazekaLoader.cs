using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using JL.Core.Freqs.Options;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;

namespace JL.Core.Freqs.FrequencyNazeka;

internal static class FrequencyNazekaLoader
{
    public static async Task Load(Freq freq)
    {
        string fullPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        GenerateMazegakiVariantsOption? generateMazegakiOption = freq.Options.GenerateMazegakiVariants;
        Debug.Assert(generateMazegakiOption is not null);
        bool generateMazegaki = generateMazegakiOption.Value;

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = freq.Options.GenerateFusejiVariants;
        Debug.Assert(generateFusejiVariantsOption is not null);
        bool generateFusejiVariants = generateFusejiVariantsOption.Value;

        int maxSearchKeyLengthForFusejiGeneration;
        int maxTotalFuseji;
        int maxConsecutiveFuseji;
        if (generateFusejiVariants)
        {
            Debug.Assert(freq.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
            maxSearchKeyLengthForFusejiGeneration = freq.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

            Debug.Assert(freq.Options.MaxTotalFusejiCount is not null);
            maxTotalFuseji = freq.Options.MaxTotalFusejiCount.Value;

            Debug.Assert(freq.Options.MaxConsecutiveFusejiCount is not null);
            maxConsecutiveFuseji = freq.Options.MaxConsecutiveFusejiCount.Value;
        }
        else
        {
            maxSearchKeyLengthForFusejiGeneration = 0;
            maxTotalFuseji = 0;
            maxConsecutiveFuseji = 0;
        }

        Dictionary<string, JsonElement[][]>? frequencyJson;
        FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
        await using (fileStream.ConfigureAwait(false))
        {
            frequencyJson = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement[][]>>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false);
            Debug.Assert(frequencyJson is not null);
        }

        foreach ((string reading, JsonElement[][] value) in frequencyJson)
        {
            foreach (JsonElement[] elementList in value)
            {
                int frequencyRank = elementList[1].GetInt32();
                string exactSpelling = elementList[0]
                    // ReSharper disable once NullableWarningSuppressionIsUsed
                    .GetString()!.GetPooledString();

                if (frequencyRank > freq.MaxValue)
                {
                    freq.MaxValue = frequencyRank;
                }

                FrequencyRecord frequencyRecordWithExactSpelling = new(exactSpelling, frequencyRank);
                FreqUtils.AddOrUpdate(freq.Contents, reading, frequencyRecordWithExactSpelling);
                if (generateFusejiVariants)
                {
                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(reading, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                    {
                        FreqUtils.AddOrUpdate(freq.Contents, fusejiVariant, frequencyRecordWithExactSpelling);
                    }
                }

                string exactSpellingInHiragana = JapaneseUtils.NormalizeText(exactSpelling).GetPooledString();
                if (exactSpellingInHiragana != reading)
                {
                    FrequencyRecord frequencyRecordWithReading = new(reading, frequencyRank);
                    FreqUtils.AddOrUpdate(freq.Contents, exactSpellingInHiragana, frequencyRecordWithReading);
                    if (generateFusejiVariants)
                    {
                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(exactSpellingInHiragana, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                        {
                            FreqUtils.AddOrUpdate(freq.Contents, fusejiVariant, frequencyRecordWithReading);
                        }
                    }

                    if (generateMazegaki)
                    {
                        foreach (string mazegakiVariant in MazegakiVariantGenerator.GenerateMazegakiVariants(exactSpellingInHiragana, reading))
                        {
                            FreqUtils.AddOrUpdate(freq.Contents, mazegakiVariant, frequencyRecordWithReading);
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegakiVariant, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    FreqUtils.AddOrUpdate(freq.Contents, fusejiVariant, frequencyRecordWithReading);
                                }
                            }
                        }
                    }
                }
            }
        }

        freq.Contents = freq.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<FrequencyRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
