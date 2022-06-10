using System.Text.Json;

namespace JL.Core.Frequency.FreqYomichan;

public class FrequencyYomichanLoader
{
    public static async Task Load(Freq freq)
    {
        if (!Directory.Exists(freq.Path) && !File.Exists(freq.Path))
            return;

        Dictionary<string, List<FrequencyRecord>> freqDict = freq.Contents;
        List<List<JsonElement>>? frequencyJson;

        string[] jsonFiles = Directory.EnumerateFiles(freq.Path, "*_bank_*.json", SearchOption.TopDirectoryOnly)
            .Where(s => s.Contains("term") || s.Contains("kanji"))
            .ToArray();

        foreach (string jsonFile in jsonFiles)
        {
            FileStream openStream = File.OpenRead(jsonFile);
            await using (openStream.ConfigureAwait(false))
            {
                frequencyJson = await JsonSerializer.DeserializeAsync<List<List<JsonElement>>>(openStream)
                    .ConfigureAwait(false);
            }

            foreach (List<JsonElement> value in frequencyJson!)
            {
                string spellling = value[0].ToString();
                string spellingInHiragana = Kana.KatakanaToHiraganaConverter(spellling);
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
                    if (freqElement.ValueKind == JsonValueKind.Number)
                    {
                        frequency = freqElement.GetInt32();
                    }

                    else
                    {
                        frequency = thirdElement.GetProperty("frequency").GetProperty("value").GetInt32();
                    }
                }

                else if (thirdElement.TryGetProperty("value", out JsonElement freqValue))
                {
                    frequency = freqValue.GetInt32();
                }

                else if (thirdElement.ValueKind == JsonValueKind.Array)
                {
                    reading = thirdElement[0].ToString();
                    frequency = thirdElement[1].GetInt32();
                }

                else if (thirdElement.ValueKind == JsonValueKind.Number)
                {
                    frequency = thirdElement.GetInt32();
                }

                if (frequency != int.MaxValue)
                {
                    if (reading == "")
                    {
                        if (freqDict.TryGetValue(spellingInHiragana, out List<FrequencyRecord>? spellingFreqResult))
                        {
                            spellingFreqResult.Add(new(spellling, frequency));
                        }

                        else
                        {
                            freqDict.Add(spellingInHiragana, new() { new(spellling, frequency) });
                        }
                    }

                    else
                    {
                        string readingInHiragana = Kana.KatakanaToHiraganaConverter(reading);
                        if (freqDict.TryGetValue(readingInHiragana, out List<FrequencyRecord>? readingFreqResult))
                        {
                            readingFreqResult.Add(new(spellling, frequency));
                        }

                        else
                        {
                            freqDict.Add(readingInHiragana, new() { new(spellling, frequency) });
                        }

                        if (reading != spellling)
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
