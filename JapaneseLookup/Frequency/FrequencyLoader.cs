using System.Collections.Generic;
using System.Diagnostics;
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
            FreqDicts.TryGetValue(ConfigManager.FrequencyList, out var freqDict);

            foreach ((string reading, var value) in frequencyDict)
            {
                foreach (List<JsonElement> element in value)
                {
                    string exactSpelling = element[0].ToString();
                    element[1].TryGetInt32(out int frequencyRank);
                    Debug.Assert(freqDict != null, nameof(freqDict) + " != null");
                    if (freqDict.TryGetValue(reading, out var readingFreqResult))
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
                        if (freqDict.TryGetValue(exactSpellingInHiragana, out var exactSpellingFreqResult))
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
        }
    }
}