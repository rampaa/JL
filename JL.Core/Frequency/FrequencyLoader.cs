using System.Text.Json;

namespace JL.Core.Frequency;

public static class FrequencyLoader
{
    public static async Task<Dictionary<string, List<List<JsonElement>>>?> LoadJson(string path)
    {
        await using FileStream openStream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Dictionary<string, List<List<JsonElement>>>>(openStream)
            .ConfigureAwait(false);
    }

    public static void BuildFreqDict(Dictionary<string, List<List<JsonElement>>> frequencyDict)
    {
        Storage.FreqDicts.TryGetValue(Storage.Frontend.CoreConfig.FrequencyListName, out Dictionary<string, List<FrequencyEntry>>? freqDict);

        if (freqDict == null || frequencyDict == null)
        {
            Utilities.Utils.Logger.Error("Couldn't load frequency");
            throw new InvalidOperationException();
        }

        foreach ((string reading, List<List<JsonElement>> value) in frequencyDict)
        {
            int valueCount = value.Count;
            for (int i = 0; i < valueCount; i++)
            {
                List<JsonElement> elementList = value[i];

                string exactSpelling = elementList[0].ToString();
                elementList[1].TryGetInt32(out int frequencyRank);

                if (freqDict.TryGetValue(reading, out List<FrequencyEntry>? readingFreqResult))
                {
                    readingFreqResult.Add(new FrequencyEntry(exactSpelling, frequencyRank));
                }

                else
                {
                    freqDict.Add(reading,
                        new List<FrequencyEntry> { new(exactSpelling, frequencyRank) });
                }

                string exactSpellingInHiragana = Kana.KatakanaToHiraganaConverter(exactSpelling);

                if (exactSpellingInHiragana != reading)
                {
                    if (freqDict.TryGetValue(exactSpellingInHiragana, out List<FrequencyEntry>? exactSpellingFreqResult))
                    {
                        exactSpellingFreqResult.Add(new FrequencyEntry(reading, frequencyRank));
                    }

                    else
                    {
                        freqDict.Add(exactSpellingInHiragana,
                            new List<FrequencyEntry> { new(reading, frequencyRank) });
                    }
                }
            }
        }

        freqDict.TrimExcess();
    }
}
