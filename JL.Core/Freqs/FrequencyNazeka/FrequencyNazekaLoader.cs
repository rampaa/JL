using System.Collections.Frozen;
using System.Diagnostics;
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
        if (generateFusejiVariants)
        {
            Debug.Assert(freq.Options.MaxSearchKeyLengthForFusejiGeneration is not null);
            maxSearchKeyLengthForFusejiGeneration = freq.Options.MaxSearchKeyLengthForFusejiGeneration.Value;

            Debug.Assert(freq.Options.MaxTotalFusejiCount is not null);
            maxTotalFuseji = freq.Options.MaxTotalFusejiCount.Value;
        }
        else
        {
            maxSearchKeyLengthForFusejiGeneration = 0;
            maxTotalFuseji = 0;
        }

        bool higherValueMeansHigherFrequency = freq.Options.HigherValueMeansHigherFrequency.Value;
        Dictionary<string, FrequencyRecords> dictionary = new(freq.Size > 0 ? freq.Size : 0, StringComparer.Ordinal);
        await foreach (FrequencyNazekaRecordBatch batch in FrequencyNazekaReader.ReadRecordBatches(fullPath, parseInParallel: true).ConfigureAwait(false))
        {
            for (int recordIndex = 0; recordIndex < batch.Count; recordIndex++)
            {
                ref readonly FrequencyNazekaRecord record = ref batch.Records[recordIndex];
                string reading = record.Reading;
                int frequencyRank = record.Frequency;
                string exactSpelling = record.Spelling.GetPooledString();

                if (frequencyRank > freq.MaxValue)
                {
                    freq.MaxValue = frequencyRank;
                }

                FrequencyRecord frequencyRecordWithExactSpelling = new(exactSpelling, frequencyRank);
                if (FreqUtils.AddOrUpdate(dictionary, reading, frequencyRecordWithExactSpelling, higherValueMeansHigherFrequency) && generateFusejiVariants)
                {
                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(reading, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                    {
                        _ = FreqUtils.AddOrUpdate(dictionary, fusejiVariant, frequencyRecordWithExactSpelling, higherValueMeansHigherFrequency);
                    }
                }

                string exactSpellingInHiragana = JapaneseUtils.NormalizeText(exactSpelling);
                if (exactSpellingInHiragana != reading)
                {
                    exactSpellingInHiragana = exactSpellingInHiragana.GetPooledString();
                    FrequencyRecord frequencyRecordWithReading = new(reading, frequencyRank);
                    if (FreqUtils.AddOrUpdate(dictionary, exactSpellingInHiragana, frequencyRecordWithReading, higherValueMeansHigherFrequency))
                    {
                        if (generateFusejiVariants)
                        {
                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(exactSpellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                            {
                                _ = FreqUtils.AddOrUpdate(dictionary, fusejiVariant, frequencyRecordWithReading, higherValueMeansHigherFrequency);
                            }
                        }

                        if (generateMazegaki)
                        {
                            foreach (string mazegakiVariant in MazegakiVariantGenerator.GenerateMazegakiVariants(exactSpellingInHiragana, reading))
                            {
                                if (FreqUtils.AddOrUpdate(dictionary, mazegakiVariant, frequencyRecordWithReading, higherValueMeansHigherFrequency) && generateFusejiVariants)
                                {
                                    foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegakiVariant, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                    {
                                        _ = FreqUtils.AddOrUpdate(dictionary, fusejiVariant, frequencyRecordWithReading, higherValueMeansHigherFrequency);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        freq.Contents = dictionary.ToFrozenDictionary(static entry => entry.Key, static IList<FrequencyRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
