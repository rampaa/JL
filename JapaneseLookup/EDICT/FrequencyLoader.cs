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

                    if (EdictLoader.jMdictDictionary.TryGetValue(exactSpelling, out List<EdictResult> jMDictResults))
                    {
                        foreach (EdictResult result in jMDictResults)
                        {
                            if (result.PrimarySpelling == reading
                                || (reading != exactSpelling 
                                && result.Readings.Contains(reading)))
                            {
                                if (result.FrequencyDict != null && result.FrequencyDict.TryGetValue(freqListName, out var frequency))
                                {
                                    if (frequency.FrequencyRank > frequencyRank)
                                    {
                                        frequency.FrequencyRank = frequencyRank;
                                        frequency.FrequencyPPM = frequencyPPM;
                                    }
                                }

                                else if (result.FrequencyDict == null)
                                {
                                    result.FrequencyDict = new();
                                    result.FrequencyDict.Add(freqListName, new Frequency(frequencyRank, frequencyPPM));
                                }

                                else
                                {
                                    result.FrequencyDict.Add(freqListName, new Frequency(frequencyRank, frequencyPPM));
                                }

                                if (result.AlternativeSpellings != null)
                                {
                                    foreach (var aspelling in result.AlternativeSpellings)
                                    {
                                        if (EdictLoader.jMdictDictionary.TryGetValue(aspelling, out var edictResults))
                                        {
                                            foreach (var aresult in edictResults)
                                            {
                                                if (aresult.PrimarySpelling == reading
                                                    || (reading != exactSpelling 
                                                    && aresult.Readings.Contains(reading)))

                                                    if (aresult.FrequencyDict != null && aresult.FrequencyDict.TryGetValue(freqListName, out var afrequency))
                                                    {
                                                        if (afrequency.FrequencyRank > frequencyRank)
                                                        {
                                                            afrequency.FrequencyRank = frequencyRank;
                                                            afrequency.FrequencyPPM = frequencyPPM;
                                                        }
                                                    }

                                                    else if (aresult.FrequencyDict == null)
                                                    {
                                                        aresult.FrequencyDict = new();
                                                        aresult.FrequencyDict.Add(freqListName, new Frequency(frequencyRank, frequencyPPM));
                                                    }

                                                    else
                                                    {
                                                        aresult.FrequencyDict.Add(freqListName, new Frequency(frequencyRank, frequencyPPM));
                                                    }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (reading != exactSpelling && EdictLoader.jMdictDictionary.TryGetValue(reading, out jMDictResults))
                    {
                        foreach (EdictResult result in jMDictResults)
                        {
                            if (result.PrimarySpelling == exactSpelling
                                || (result.AlternativeSpellings != null && result.AlternativeSpellings.Contains(exactSpelling))
                                || (result.KanaSpellings != null && result.KanaSpellings.Contains(exactSpelling)))
                            {
                                foreach (var rreading in result.Readings)
                                {
                                    if (EdictLoader.jMdictDictionary.TryGetValue(rreading, out var rjMDictResults))
                                    {
                                        foreach (var rresult in rjMDictResults)
                                        {
                                            if (rresult.PrimarySpelling == exactSpelling
                                                || (rresult.AlternativeSpellings != null && rresult.AlternativeSpellings.Contains(exactSpelling))
                                                || (rresult.KanaSpellings != null && rresult.KanaSpellings.Contains(exactSpelling)))
                                            {
                                                if (rresult.FrequencyDict != null && rresult.FrequencyDict.TryGetValue(freqListName, out var frequency))
                                                {
                                                    if (frequency.FrequencyRank > frequencyRank)
                                                    {
                                                        frequency.FrequencyRank = frequencyRank;
                                                        frequency.FrequencyPPM = frequencyPPM;
                                                    }
                                                }

                                                else if (rresult.FrequencyDict == null)
                                                {
                                                    rresult.FrequencyDict = new();
                                                    rresult.FrequencyDict.Add(freqListName, new Frequency(frequencyRank, frequencyPPM));
                                                }

                                                else
                                                {
                                                    rresult.FrequencyDict.Add(freqListName, new Frequency(frequencyRank, frequencyPPM));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}