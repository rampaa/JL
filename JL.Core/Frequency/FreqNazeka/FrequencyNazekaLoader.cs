using System.Text.Json;

namespace JL.Core.Frequency.FreqNazeka;

public static class FrequencyNazekaLoader
{
    public static async Task Load(Freq freq)
    {
        Dictionary<string, List<FrequencyRecord>> freqDict = freq.Contents;
        Dictionary<string, List<List<JsonElement>>>? frequencyJson;

        FileStream openStream = File.OpenRead(freq.Path);
        await using (openStream.ConfigureAwait(false))
        {
            frequencyJson = await JsonSerializer.DeserializeAsync<Dictionary<string, List<List<JsonElement>>>>(openStream)
                .ConfigureAwait(false);
        }

        foreach ((string reading, List<List<JsonElement>> value) in frequencyJson!)
        {
            int valueCount = value.Count;
            for (int i = 0; i < valueCount; i++)
            {
                List<JsonElement> elementList = value[i];

                string exactSpelling = elementList[0].ToString();
                elementList[1].TryGetInt32(out int frequencyRank);

                if (freqDict.TryGetValue(reading, out List<FrequencyRecord>? readingFreqResult))
                {
                    readingFreqResult.Add(new FrequencyRecord(exactSpelling, frequencyRank));
                }

                else
                {
                    freqDict.Add(reading,
                        new List<FrequencyRecord> { new(exactSpelling, frequencyRank) });
                }

                string exactSpellingInHiragana = Kana.KatakanaToHiraganaConverter(exactSpelling);

                if (exactSpellingInHiragana != reading)
                {
                    if (freqDict.TryGetValue(exactSpellingInHiragana, out List<FrequencyRecord>? exactSpellingFreqResult))
                    {
                        exactSpellingFreqResult.Add(new(reading, frequencyRank));
                    }

                    else
                    {
                        freqDict.Add(exactSpellingInHiragana,
                            new List<FrequencyRecord> { new(reading, frequencyRank) });
                    }
                }
            }
        }

        freqDict.TrimExcess();
    }
}
