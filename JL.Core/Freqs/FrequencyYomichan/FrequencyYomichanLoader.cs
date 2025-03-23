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
            List<List<JsonElement>>? frequencyJson;
            FileStream fileStream = File.OpenRead(jsonFile);
            await using (fileStream.ConfigureAwait(false))
            {
                frequencyJson = await JsonSerializer
                    .DeserializeAsync<List<List<JsonElement>>>(fileStream, Utils.s_jso)
                    .ConfigureAwait(false);
            }

            for (int i = 0; i < frequencyJson!.Count; i++)
            {
                List<JsonElement> value = frequencyJson[i];
                string primarySpelling = value[0].GetString()!.GetPooledString();
                string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(primarySpelling).GetPooledString();
                string? reading = null;
                int frequency = int.MaxValue;
                JsonElement thirdElement = value[2];

#pragma warning disable IDE0010 // Add missing cases to switch statement
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (thirdElement.ValueKind)
                {
                    case JsonValueKind.Number:
                        frequency = thirdElement.GetInt32();
                        break;
                    case JsonValueKind.Object when thirdElement.TryGetProperty("value", out JsonElement freqValue):
                        frequency = freqValue.GetInt32();
                        break;

                    case JsonValueKind.Object:
                    {
                        if (thirdElement.TryGetProperty("reading", out JsonElement readingValue))
                        {
                            reading = readingValue.GetString()!.GetPooledString();
                            JsonElement frequencyElement = thirdElement.GetProperty("frequency");
                            frequency = frequencyElement.ValueKind is JsonValueKind.Number
                                ? frequencyElement.GetInt32()
                                : frequencyElement.GetProperty("value").GetInt32();
                        }

                        break;
                    }

                    case JsonValueKind.String:
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

                        break;
                    }

                    // Check if there is any frequency dictionary with this format
                    case JsonValueKind.Array:
                        reading = thirdElement[0].GetString()!.GetPooledString();
                        frequency = thirdElement[1].GetInt32();
                        break;
                }
#pragma warning restore IDE0010 // Add missing cases to switch statement

                if (frequency is int.MaxValue)
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
