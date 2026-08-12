using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
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

        GenerateMazegakiVariantsOption? generateMazegakiOption = freq.Options.GenerateMazegakiVariants;
        Debug.Assert(nonKanjiDict || generateMazegakiOption is not null);
        bool generateMazegaki = nonKanjiDict
            // ReSharper disable once NullableWarningSuppressionIsUsed
            && generateMazegakiOption!.Value;

        GenerateFusejiVariantsOption? generateFusejiVariantsOption = freq.Options.GenerateFusejiVariants;
        Debug.Assert(!nonKanjiDict || generateFusejiVariantsOption is not null);
        bool generateFusejiVariants = nonKanjiDict
                                // ReSharper disable once NullableWarningSuppressionIsUsed
                                && generateFusejiVariantsOption!.Value;

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

        Debug.Assert(freq.Contents is Dictionary<string, IList<FrequencyRecord>>);
        Dictionary<string, IList<FrequencyRecord>> dictionary = (Dictionary<string, IList<FrequencyRecord>>)freq.Contents;

        // TODO: When migrating to .NET 10 again, use CompareOptions.NumericOrdering to order JSON files
        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, freq.Type is FreqType.Yomichan ? "term_meta_bank_*.json" : "kanji_meta_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
            FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
            await using (fileStream.ConfigureAwait(false))
            {
                await foreach (JsonElement[]? jsonElements in JsonSerializer.DeserializeAsyncEnumerable<JsonElement[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                {
                    Debug.Assert(jsonElements is not null);

                    string primarySpelling = jsonElements[0]
                        // ReSharper disable once NullableWarningSuppressionIsUsed
                        .GetString()!.GetPooledString();

                    string primarySpellingInHiragana = JapaneseUtils.NormalizeText(primarySpelling).GetPooledString();
                    string? reading = null;
                    int frequency = -1;
                    ref readonly JsonElement thirdElement = ref jsonElements[2];

                    if (thirdElement.ValueKind is JsonValueKind.Number)
                    {
                        frequency = thirdElement.GetInt32();
                    }
                    else if (thirdElement.ValueKind is JsonValueKind.Object)
                    {
                        if (thirdElement.TryGetProperty("value", out JsonElement freqValue))
                        {
                            frequency = freqValue.GetInt32();
                            if (frequency <= 0 && thirdElement.TryGetProperty("displayValue", out JsonElement displayValue))
                            {
                                frequency = TextUtils.ExtractFirstInt(displayValue.GetString());
                            }
                        }
                        else if (thirdElement.TryGetProperty("reading", out JsonElement readingValue))
                        {
                            reading = readingValue
                                // ReSharper disable once NullableWarningSuppressionIsUsed
                                .GetString()!.GetPooledString();
                            JsonElement frequencyElement = thirdElement.GetProperty("frequency");

                            if (frequencyElement.ValueKind is JsonValueKind.Number)
                            {
                                frequency = frequencyElement.GetInt32();
                            }
                            else if (frequencyElement.ValueKind is JsonValueKind.Object)
                            {
                                frequency = frequencyElement.GetProperty("value").GetInt32();
                                if (frequency <= 0 && frequencyElement.TryGetProperty("displayValue", out JsonElement displayValue))
                                {
                                    frequency = TextUtils.ExtractFirstInt(displayValue.GetString());
                                }
                            }
                            else // if (frequencyElement.ValueKind is JsonValueKind.String)
                            {
                                frequency = TextUtils.ExtractFirstInt(frequencyElement.GetString());
                            }
                        }
                    }
                    else // if (thirdElement.ValueKind is JsonValueKind.String)
                    {
                        string? freqStr = thirdElement.GetString();
                        Debug.Assert(freqStr is not null);

                        frequency = TextUtils.ExtractFirstInt(freqStr);
                    }

                    if (frequency <= 0)
                    {
                        continue;
                    }

                    if (frequency > freq.MaxValue)
                    {
                        freq.MaxValue = frequency;
                    }

                    if (primarySpelling == reading)
                    {
                        reading = null;
                    }

                    FrequencyRecord frequencyRecordWithPrimarySpelling = new(primarySpelling, frequency);
                    if (reading is null)
                    {
                        if (FreqUtils.AddOrUpdate(dictionary, primarySpellingInHiragana, frequencyRecordWithPrimarySpelling) && generateFusejiVariants)
                        {
                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                            {
                                _ = FreqUtils.AddOrUpdate(dictionary, fusejiVariant, frequencyRecordWithPrimarySpelling);
                            }
                        }
                    }
                    else
                    {
                        string readingInHiragana = JapaneseUtils.NormalizeText(reading).GetPooledString();
                        if (FreqUtils.AddOrUpdate(dictionary, readingInHiragana, frequencyRecordWithPrimarySpelling) && generateFusejiVariants)
                        {
                            foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(readingInHiragana, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                            {
                                _ = FreqUtils.AddOrUpdate(dictionary, fusejiVariant, frequencyRecordWithPrimarySpelling);
                            }
                        }

                        FrequencyRecord frequencyRecordWithReading = new(reading, frequency);
                        if (FreqUtils.AddOrUpdate(dictionary, primarySpellingInHiragana, frequencyRecordWithReading))
                        {
                            if (generateFusejiVariants)
                            {
                                foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(primarySpellingInHiragana, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                {
                                    _ = FreqUtils.AddOrUpdate(dictionary, fusejiVariant, frequencyRecordWithReading);
                                }
                            }

                            if (generateMazegaki)
                            {
                                foreach (string mazegakiVariant in MazegakiVariantGenerator.GenerateMazegakiVariants(primarySpellingInHiragana, reading))
                                {
                                    if (FreqUtils.AddOrUpdate(dictionary, mazegakiVariant, frequencyRecordWithReading) && generateFusejiVariants)
                                    {
                                        foreach (string fusejiVariant in FusejiUtils.CreateFusejiVariants(mazegakiVariant, maxTotalFuseji, maxConsecutiveFuseji, maxSearchKeyLengthForFusejiGeneration))
                                        {
                                            _ = FreqUtils.AddOrUpdate(dictionary, fusejiVariant, frequencyRecordWithReading);
                                        }
                                    }
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
