using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JapaneseLookup.Abstract;
using JapaneseLookup.Dicts;
using JapaneseLookup.EDICT.JMdict;

namespace JapaneseLookup.Frequency
{
    public static class FrequencyLoader
    {
        public static Dictionary<string, Dictionary<string, List<FrequencyEntry>>> FreqDicts { get; set; } = new();
        public static async Task<Dictionary<string, List<List<JsonElement>>>> LoadJson(string path)
        {
            await using FileStream openStream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<Dictionary<string, List<List<JsonElement>>>>(openStream);
        }

        public static void BuildFreqDict(Dictionary<string, List<List<JsonElement>>> frequencyDict)
        {
            FreqDicts.TryGetValue(ConfigManager.FrequencyList, out var FreqDict);

            foreach (KeyValuePair<string, List<List<JsonElement>>> dictEntry in frequencyDict)
            {
                string reading = dictEntry.Key;
                foreach (List<JsonElement> element in dictEntry.Value)
                {
                    string exactSpelling = element[0].ToString();
                    element[1].TryGetInt32(out int frequencyRank);
                    if (FreqDict.TryGetValue(reading, out var readingFreqResult))
                    {
                        readingFreqResult.Add(new FrequencyEntry(exactSpelling, frequencyRank));
                    }
                    else
                    {
                        FreqDict.Add(reading, new List<FrequencyEntry> { new FrequencyEntry(exactSpelling, frequencyRank) });
                    }

                    string exactSpellingInHiragana = Kana.KatakanaToHiraganaConverter(exactSpelling);

                    if (exactSpellingInHiragana != reading)
                    {
                        if (FreqDict.TryGetValue(exactSpellingInHiragana, out var exacSpellingFreqResult))
                        {
                            exacSpellingFreqResult.Add(new FrequencyEntry(reading, frequencyRank));
                        }
                        else
                        {
                            FreqDict.Add(exactSpellingInHiragana, new List<FrequencyEntry> { new FrequencyEntry(reading, frequencyRank) });
                        }
                    }
                }
            }
        }
    }
}