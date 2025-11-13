using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
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

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "*_meta_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
            FileStream fileStream = new(jsonFile, FileStreamOptionsPresets.s_asyncRead64KBufferFso);
            await using (fileStream.ConfigureAwait(false))
            {
                await foreach (JsonElement[]? jsonElements in JsonSerializer.DeserializeAsyncEnumerable<JsonElement[]>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false))
                {
                    Debug.Assert(jsonElements is not null);

                    string primarySpelling = jsonElements[0].GetString()!.GetPooledString();
                    string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(primarySpelling).GetPooledString();
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
                            reading = readingValue.GetString()!.GetPooledString();
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
                        FreqUtils.AddOrUpdate(freq.Contents, primarySpellingInHiragana, frequencyRecordWithPrimarySpelling);
                    }
                    else
                    {
                        string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading).GetPooledString();
                        FreqUtils.AddOrUpdate(freq.Contents, readingInHiragana, frequencyRecordWithPrimarySpelling);

                        FrequencyRecord frequencyRecordWithReading = new(reading, frequency);
                        FreqUtils.AddOrUpdate(freq.Contents, primarySpellingInHiragana, frequencyRecordWithReading);
                    }
                }
            }
        }

        freq.Contents = freq.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<FrequencyRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }
}
