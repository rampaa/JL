using System.Collections.Frozen;
using System.Diagnostics;
using JL.Core.Freqs.Options;
using JL.Core.Japanese;
using JL.Core.Japanese.Fuseji;
using JL.Core.Japanese.Mazegaki;
using JL.Core.Utilities;

namespace JL.Core.Freqs.FrequencyYomichan;

internal static class FrequencyYomichanLoader
{
    public static async Task Load(Freq freq)
    {
        string fullPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        bool nonKanjiDict = freq.Type is not FreqType.YomichanKanji;

        bool generateMazegaki = false;
        bool generateFusejiVariants = false;
        if (nonKanjiDict)
        {
            GenerateMazegakiVariantsOption? generateMazegakiOption = freq.Options.GenerateMazegakiVariants;
            Debug.Assert(generateMazegakiOption is not null);
            generateMazegaki = generateMazegakiOption.Value;

            GenerateFusejiVariantsOption? generateFusejiVariantsOption = freq.Options.GenerateFusejiVariants;
            Debug.Assert(generateFusejiVariantsOption is not null);
            generateFusejiVariants = generateFusejiVariantsOption.Value;
        }

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

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, freq.Type is FreqType.Yomichan ? "term_meta_bank_*.json" : "kanji_meta_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
            await foreach (FrequencyYomichanRecordBatch batch in FrequencyYomichanReader.ReadRecordBatches(jsonFile).ConfigureAwait(false))
            {
                for (int recordIndex = 0; recordIndex < batch.Count; recordIndex++)
                {
                    ref readonly FrequencyYomichanRecord record = ref batch.Records[recordIndex];
                    string? reading = record.Reading;
                    string primarySpelling = record.Spelling.GetPooledString();
                    string primarySpellingInHiragana = JapaneseUtils.NormalizeText(primarySpelling).GetPooledString();
                    int frequency = record.Frequency;

                    if (frequency > freq.MaxValue)
                    {
                        freq.MaxValue = frequency;
                    }

                    reading = primarySpelling == reading
                        ? null
                        : reading?.GetPooledString();

                    FrequencyRecord frequencyRecordWithPrimarySpelling = new(primarySpelling, frequency);
                    if (reading is null)
                    {
                        if (FreqUtils.AddOrUpdate(dictionary, primarySpellingInHiragana, frequencyRecordWithPrimarySpelling, higherValueMeansHigherFrequency) && generateFusejiVariants)
                        {
                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                            {
                                _ = FreqUtils.AddOrUpdate(dictionary, fusejiVariant, frequencyRecordWithPrimarySpelling, higherValueMeansHigherFrequency);
                            }
                        }
                    }
                    else
                    {
                        string readingInHiragana = JapaneseUtils.NormalizeText(reading).GetPooledString();
                        if (FreqUtils.AddOrUpdate(dictionary, readingInHiragana, frequencyRecordWithPrimarySpelling, higherValueMeansHigherFrequency) && generateFusejiVariants)
                        {
                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                            {
                                _ = FreqUtils.AddOrUpdate(dictionary, fusejiVariant, frequencyRecordWithPrimarySpelling, higherValueMeansHigherFrequency);
                            }
                        }

                        FrequencyRecord frequencyRecordWithReading = new(reading, frequency);
                        if (FreqUtils.AddOrUpdate(dictionary, primarySpellingInHiragana, frequencyRecordWithReading, higherValueMeansHigherFrequency))
                        {
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana, maxTotalFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    _ = FreqUtils.AddOrUpdate(dictionary, fusejiVariant, frequencyRecordWithReading, higherValueMeansHigherFrequency);
                                }
                            }

                            if (generateMazegaki)
                            {
                                foreach (string mazegakiVariant in MazegakiVariantGenerator.GenerateMazegakiVariants(primarySpellingInHiragana, reading))
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
        }

        freq.Contents = dictionary.ToFrozenDictionary(static entry => entry.Key, static IList<FrequencyRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
