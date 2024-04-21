using System.Collections.Frozen;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Freqs.FrequencyNazeka;

internal static class FrequencyNazekaLoader
{
    public static async Task Load(Freq freq)
    {
        string fullPath = Path.GetFullPath(freq.Path, Utils.ApplicationPath);
        if (!File.Exists(fullPath))
        {
            return;
        }

        Dictionary<string, List<List<JsonElement>>>? frequencyJson;

        FileStream fileStream = File.OpenRead(fullPath);
        await using (fileStream.ConfigureAwait(false))
        {
            frequencyJson = await JsonSerializer.DeserializeAsync<Dictionary<string, List<List<JsonElement>>>>(fileStream)
                .ConfigureAwait(false);
        }

        foreach ((string reading, List<List<JsonElement>> value) in frequencyJson!)
        {
            int valueCount = value.Count;
            for (int i = 0; i < valueCount; i++)
            {
                List<JsonElement> elementList = value[i];

                string exactSpelling = elementList[0].GetString()!.GetPooledString();
                int frequencyRank = elementList[1].GetInt32();

                if (frequencyRank > freq.MaxValue)
                {
                    freq.MaxValue = frequencyRank;
                }

                if (freq.Contents.TryGetValue(reading, out IList<FrequencyRecord>? readingFreqResult))
                {
                    readingFreqResult.Add(new FrequencyRecord(exactSpelling, frequencyRank));
                }
                else
                {
                    freq.Contents[reading.GetPooledString()] = [new FrequencyRecord(exactSpelling, frequencyRank)];
                }

                string exactSpellingInHiragana = JapaneseUtils.KatakanaToHiragana(exactSpelling).GetPooledString();
                if (exactSpellingInHiragana != reading)
                {
                    if (freq.Contents.TryGetValue(exactSpellingInHiragana, out IList<FrequencyRecord>? exactSpellingFreqResult))
                    {
                        exactSpellingFreqResult.Add(new FrequencyRecord(reading, frequencyRank));
                    }

                    else
                    {
                        freq.Contents[exactSpellingInHiragana] = [new FrequencyRecord(reading, frequencyRank)];
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
