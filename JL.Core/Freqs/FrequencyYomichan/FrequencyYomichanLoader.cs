using System.Collections.Frozen;
using System.Text.Json;
using System.Text.RegularExpressions;
using JL.Core.Utilities;

namespace JL.Core.Freqs.FrequencyYomichan;

internal static class FrequencyYomichanLoader
{
    public static async Task Load(Freq freq)
    {
        string fullPath = Path.GetFullPath(freq.Path, Utils.ApplicationPath);
        if (!Directory.Exists(fullPath))
        {
            return;
        }

        IEnumerable<string> jsonFiles = Directory.EnumerateFiles(fullPath, "*_meta_bank_*.json", SearchOption.TopDirectoryOnly);
        foreach (string jsonFile in jsonFiles)
        {
            ReadOnlyMemory<ReadOnlyMemory<JsonElement>> frequencyJson;
            FileStream fileStream = File.OpenRead(jsonFile);
            await using (fileStream.ConfigureAwait(false))
            {
                frequencyJson = await JsonSerializer
                    .DeserializeAsync<ReadOnlyMemory<ReadOnlyMemory<JsonElement>>>(fileStream, Utils.s_jso)
                    .ConfigureAwait(false);
            }

            foreach (ref readonly ReadOnlyMemory<JsonElement> valueMemory in frequencyJson.Span)
            {
                ReadOnlySpan<JsonElement> value = valueMemory.Span;
                string primarySpelling = value[0].GetString()!.GetPooledString();
                string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(primarySpelling).GetPooledString();
                string? reading = null;
                int frequency = int.MaxValue;
                ref readonly JsonElement thirdElement = ref value[2];

                if (thirdElement.ValueKind is JsonValueKind.Number)
                {
                    frequency = thirdElement.GetInt32();
                }

                else if (thirdElement.ValueKind is JsonValueKind.Object)
                {
                    if (thirdElement.TryGetProperty("value", out JsonElement freqValue))
                    {
                        frequency = freqValue.GetInt32();
                    }
                    else if (thirdElement.TryGetProperty("reading", out JsonElement readingValue))
                    {
                        reading = readingValue.GetString()!.GetPooledString();
                        JsonElement frequencyElement = thirdElement.GetProperty("frequency");
                        frequency = frequencyElement.ValueKind is JsonValueKind.Number
                            ? frequencyElement.GetInt32()
                            : frequencyElement.GetProperty("value").GetInt32();
                    }
                }

                else if (thirdElement.ValueKind is JsonValueKind.String)
                {
                    string freqStr = thirdElement.GetString()!;
                    Match match = Utils.NumberRegex.Match(freqStr);
                    if (match.Success)
                    {
                        if (int.TryParse(match.ValueSpan, out int parsedFreq))
                        {
                            frequency = parsedFreq;
                        }
                    }
                }

                else if (thirdElement.ValueKind is JsonValueKind.Array)
                {
                    reading = thirdElement[0].GetString()!.GetPooledString();
                    frequency = thirdElement[1].GetInt32();
                }

                else
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
                    if (freq.Contents.TryGetValue(primarySpellingInHiragana, out IList<FrequencyRecord>? spellingFreqResult))
                    {
                        int index = spellingFreqResult.IndexOf(frequencyRecordWithPrimarySpelling);

                        if (index < 0)
                        {
                            spellingFreqResult.Add(frequencyRecordWithPrimarySpelling);
                        }
                        else
                        {
                            FrequencyRecord record = spellingFreqResult[index];
                            if (record.Frequency > frequency)
                            {
                                spellingFreqResult[index] = frequencyRecordWithPrimarySpelling;
                            }
                        }
                    }
                    else
                    {
                        freq.Contents[primarySpellingInHiragana] = [frequencyRecordWithPrimarySpelling];
                    }
                }

                else
                {
                    string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading).GetPooledString();
                    if (freq.Contents.TryGetValue(readingInHiragana, out IList<FrequencyRecord>? readingFreqResult))
                    {
                        int index = readingFreqResult.IndexOf(frequencyRecordWithPrimarySpelling);
                        if (index < 0)
                        {
                            readingFreqResult.Add(frequencyRecordWithPrimarySpelling);
                        }
                        else
                        {
                            FrequencyRecord record = readingFreqResult[index];
                            if (record.Frequency > frequency)
                            {
                                readingFreqResult[index] = frequencyRecordWithPrimarySpelling;
                            }
                        }
                    }
                    else
                    {
                        freq.Contents[readingInHiragana] = [frequencyRecordWithPrimarySpelling];
                    }

                    FrequencyRecord frequencyRecordWithReading = new(reading, frequency);
                    if (freq.Contents.TryGetValue(primarySpellingInHiragana, out IList<FrequencyRecord>? spellingFreqResult))
                    {
                        int index = spellingFreqResult.IndexOf(frequencyRecordWithReading);
                        if (index < 0)
                        {
                            spellingFreqResult.Add(frequencyRecordWithReading);
                        }
                        else
                        {
                            FrequencyRecord record = spellingFreqResult[index];
                            if (record.Frequency > frequency)
                            {
                                spellingFreqResult[index] = frequencyRecordWithReading;
                            }
                        }
                    }
                    else
                    {
                        freq.Contents[primarySpellingInHiragana] = [frequencyRecordWithReading];
                    }
                }
            }
        }

        foreach ((string key, IList<FrequencyRecord> recordList) in freq.Contents)
        {
            freq.Contents[key] = recordList.ToArray();
        }

        freq.Contents = freq.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
