using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace JapaneseLookup.EDICT
{
    class FrequencyLoader
    {
        public static async Task<Dictionary<string, List<List<JsonElement>>>> LoadJSON(string path)
        {
            await using FileStream openStream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<Dictionary<string, List<List<JsonElement>>>>(openStream);
        }

        public static void AddToJMdict(string freqListName, Dictionary<string, List<List<JsonElement>>> frequencyDict)
        {
            foreach (KeyValuePair<string, List<List<JsonElement>>> dictEntry in frequencyDict)
            {
                string reading = dictEntry.Key;
                foreach (List<JsonElement> element in dictEntry.Value)
                {
                    string exactSpelling = element[0].ToString();
                    element[1].TryGetInt32(out int frequencyRank);
                    element[2].TryGetDouble(out double frequencyPPM);
                    if (JMdictLoader.jMdictDictionary.TryGetValue(exactSpelling, out List<Results> jMDictResults))
                    {
                        foreach (Results result in jMDictResults)
                        {
                            if ((!result.KanaSpellings.Any()) || result.Readings.Contains(reading))
                            {
                                if (result.FrequencyDict.TryGetValue(freqListName, out var frequency))
                                {
                                    if (frequency.FrequencyRank > frequencyRank)
                                    {
                                        frequency.FrequencyRank = frequencyRank;
                                        frequency.FrequencyPPM = frequencyPPM;
                                    }
                                }
                                else
                                {
                                    result.FrequencyDict.Add(freqListName, new Frequency(frequencyRank, frequencyPPM));
                                }
                            }
                        }
                    }

                    if (reading != exactSpelling && JMdictLoader.jMdictDictionary.TryGetValue(reading, out jMDictResults))
                    {
                        foreach (Results result in jMDictResults)
                        {
                            if (result.PrimarySpelling == exactSpelling 
                                || result.AlternativeSpellings.Contains(exactSpelling) 
                                || result.KanaSpellings.Contains(exactSpelling))
                            {
                                if (result.FrequencyDict.TryGetValue(freqListName, out var frequency))
                                {
                                    if (frequency.FrequencyRank > frequencyRank)
                                    {
                                        frequency.FrequencyRank = frequencyRank;
                                        frequency.FrequencyPPM = frequencyPPM;
                                    }
                                }
                                else
                                {
                                    result.FrequencyDict.Add(freqListName, new Frequency(frequencyRank, frequencyPPM));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}