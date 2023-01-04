using System.Text.Json;

namespace JL.Core.Freqs.FrequencyYomichan;

public class FrequencyYomichanLoader
{
    public static async Task Load(Freq freq)
    {
        if (!Directory.Exists(freq.Path) && !File.Exists(freq.Path))
        {
            return;
        }

        Dictionary<string, List<FrequencyRecord>> freqDict = freq.Contents;

        string[] jsonFiles = Directory.EnumerateFiles(freq.Path, "*_bank_*.json", SearchOption.TopDirectoryOnly)
            .Where(s => s.Contains("term") || s.Contains("kanji"))
            .ToArray();

        foreach (string jsonFile in jsonFiles)
        {
            FileStream openStream = File.OpenRead(jsonFile);
            List<List<JsonElement>>? frequencyJson;
            await using (openStream.ConfigureAwait(false))
            {
                frequencyJson = await JsonSerializer.DeserializeAsync<List<List<JsonElement>>>(openStream)
                    .ConfigureAwait(false);
            }

            foreach (List<JsonElement> value in frequencyJson!)
            {
                string spelling = value[0].ToString();
                string spellingInHiragana = Kana.KatakanaToHiragana(spelling);
                string reading = "";
                int frequency = int.MaxValue;
                JsonElement thirdElement = value[2];

                if (int.TryParse(value[2].ToString().Split('/').First(), out int parsedFreq))
                {
                    frequency = parsedFreq;
                }

                else if (thirdElement.TryGetProperty("reading", out JsonElement readingValue))
                {
                    reading = readingValue.ToString();
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
                    reading = thirdElement[0].ToString();
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
                        if (freqDict.TryGetValue(spellingInHiragana, out List<FrequencyRecord>? spellingFreqResult))
                        {
                            spellingFreqResult.Add(new(spelling, frequency));
                        }

                        else
                        {
                            freqDict.Add(spellingInHiragana, new() { new(spelling, frequency) });
                        }
                    }

                    else
                    {
                        string readingInHiragana = Kana.KatakanaToHiragana(reading);
                        if (freqDict.TryGetValue(readingInHiragana, out List<FrequencyRecord>? readingFreqResult))
                        {
                            readingFreqResult.Add(new(spelling, frequency));
                        }

                        else
                        {
                            freqDict.Add(readingInHiragana, new() { new(spelling, frequency) });
                        }

                        if (reading != spelling)
                        {
                            if (freqDict.TryGetValue(spellingInHiragana, out List<FrequencyRecord>? spellingFreqResult))
                            {
                                spellingFreqResult.Add(new(reading, frequency));
                            }

                            else
                            {
                                freqDict.Add(spellingInHiragana, new() { new(reading, frequency) });
                            }
                        }
                    }
                }
            }

            freqDict.TrimExcess();
        }
    }
}
