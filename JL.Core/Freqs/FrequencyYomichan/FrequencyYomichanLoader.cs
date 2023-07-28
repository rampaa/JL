using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Freqs.FrequencyYomichan;

internal static class FrequencyYomichanLoader
{
    public static async Task Load(Freq freq)
    {
        if (!Directory.Exists(freq.Path))
        {
            return;
        }

        Dictionary<string, IList<FrequencyRecord>> freqDict = freq.Contents;

        List<string> jsonFiles = Directory.EnumerateFiles(freq.Path, "*_bank_*.json", SearchOption.TopDirectoryOnly)
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
                string spelling = value[0].ToString().GetPooledString();
                string spellingInHiragana = JapaneseUtils.KatakanaToHiragana(spelling).GetPooledString();
                string reading = "";
                int frequency = int.MaxValue;
                JsonElement thirdElement = value[2];

                if (int.TryParse(value[2].ToString().Split('/')[0], out int parsedFreq))
                {
                    frequency = parsedFreq;
                }

                else if (thirdElement.TryGetProperty("reading", out JsonElement readingValue))
                {
                    reading = readingValue.ToString().GetPooledString();
                    JsonElement freqElement = thirdElement.GetProperty("frequency");
                    frequency = freqElement.ValueKind is JsonValueKind.Number
                        ? freqElement.GetInt32()
                        : thirdElement.GetProperty("frequency").GetProperty("value").GetInt32();
                }

                else if (thirdElement.TryGetProperty("value", out JsonElement freqValue))
                {
                    frequency = freqValue.GetInt32();
                }

                else if (thirdElement.ValueKind is JsonValueKind.Array)
                {
                    reading = thirdElement[0].ToString().GetPooledString();
                    frequency = thirdElement[1].GetInt32();
                }

                else if (thirdElement.ValueKind is JsonValueKind.Number)
                {
                    frequency = thirdElement.GetInt32();
                }

                if (frequency is not int.MaxValue)
                {
                    if (reading is "")
                    {
                        if (freqDict.TryGetValue(spellingInHiragana, out IList<FrequencyRecord>? spellingFreqResult))
                        {
                            spellingFreqResult.Add(new FrequencyRecord(spelling, frequency));
                        }

                        else
                        {
                            freqDict.Add(spellingInHiragana, new List<FrequencyRecord> { new(spelling, frequency) });
                        }
                    }

                    else
                    {
                        string readingInHiragana = JapaneseUtils.KatakanaToHiragana(reading).GetPooledString();
                        if (freqDict.TryGetValue(readingInHiragana, out IList<FrequencyRecord>? readingFreqResult))
                        {
                            readingFreqResult.Add(new FrequencyRecord(spelling, frequency));
                        }

                        else
                        {
                            freqDict.Add(readingInHiragana, new List<FrequencyRecord> { new(spelling, frequency) });
                        }

                        if (reading != spelling)
                        {
                            if (freqDict.TryGetValue(spellingInHiragana, out IList<FrequencyRecord>? spellingFreqResult))
                            {
                                spellingFreqResult.Add(new FrequencyRecord(reading, frequency));
                            }

                            else
                            {
                                freqDict.Add(spellingInHiragana, new List<FrequencyRecord> { new(reading, frequency) });
                            }
                        }
                    }
                }
            }

            foreach ((string key, IList<FrequencyRecord> recordList) in freq.Contents)
            {
                freq.Contents[key] = recordList.ToArray();
            }

            freqDict.TrimExcess();
        }
    }
}
