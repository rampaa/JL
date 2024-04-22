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

        List<string> jsonFiles = Directory.EnumerateFiles(fullPath, "*_bank_*.json", SearchOption.TopDirectoryOnly)
            .Where(static s => s.Contains("term", StringComparison.Ordinal) || s.Contains("kanji", StringComparison.Ordinal))
            .ToList();

        foreach (string jsonFile in jsonFiles)
        {
            List<List<JsonElement>>? frequencyJson;
            FileStream fileStream = File.OpenRead(jsonFile);
            await using (fileStream.ConfigureAwait(false))
            {
                frequencyJson = await JsonSerializer.DeserializeAsync<List<List<JsonElement>>>(fileStream)
                    .ConfigureAwait(false);
            }

            foreach (List<JsonElement> value in frequencyJson!)
            {
                string spelling = value[0].GetString()!.GetPooledString();
                string spellingInHiragana = JapaneseUtils.KatakanaToHiragana(spelling).GetPooledString();
                string? reading = null;
                int frequency = int.MaxValue;
                JsonElement thirdElement = value[2];

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
                        Match match = Utils.NumberRegex().Match(freqStr);
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

                    default:
                        break;
                }

                if (frequency is not int.MaxValue)
                {
                    if (frequency > freq.MaxValue)
                    {
                        freq.MaxValue = frequency;
                    }

                    if (reading is null)
                    {
                        if (freq.Contents.TryGetValue(spellingInHiragana, out IList<FrequencyRecord>? spellingFreqResult))
                        {
                            spellingFreqResult.Add(new FrequencyRecord(spelling, frequency));
                        }

                        else
                        {
                            freq.Contents[spellingInHiragana] = [new FrequencyRecord(spelling, frequency)];
                        }
                    }

                    else
                    {
                        string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading).GetPooledString();
                        if (freq.Contents.TryGetValue(readingInHiragana, out IList<FrequencyRecord>? readingFreqResult))
                        {
                            readingFreqResult.Add(new FrequencyRecord(spelling, frequency));
                        }

                        else
                        {
                            freq.Contents[readingInHiragana] = [new FrequencyRecord(spelling, frequency)];
                        }

                        if (reading != spelling)
                        {
                            if (freq.Contents.TryGetValue(spellingInHiragana, out IList<FrequencyRecord>? spellingFreqResult))
                            {
                                spellingFreqResult.Add(new FrequencyRecord(reading, frequency));
                            }

                            else
                            {
                                freq.Contents[spellingInHiragana] = [new FrequencyRecord(reading, frequency)];
                            }
                        }
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
