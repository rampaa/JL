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
    public static class FrequencyLoader
    {
        public static async Task<Dictionary<string, List<List<JsonElement>>>> LoadJson(string path)
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

                    if (JMdictLoader.jMdictDictionary.TryGetValue(Kana.KatakanaToHiraganaConverter(exactSpelling),
                        out List<JMdictResult> jMDictResults))
                    {
                        foreach (JMdictResult result in jMDictResults)
                        {
                            if (Kana.KatakanaToHiraganaConverter(result.PrimarySpelling) == reading
                                || (reading != exactSpelling && (result.Readings.Contains(reading) ||
                                                                 result.Readings.Contains(
                                                                     Kana.HiraganaToKatakanaConverter(reading)))))
                            {
                                if (result.FrequencyDict != null &&
                                    result.FrequencyDict.TryGetValue(freqListName, out var frequency))
                                {
                                    if (frequency > frequencyRank)
                                    {
                                        result.FrequencyDict[freqListName] = frequencyRank;
                                    }
                                }

                                else if (result.FrequencyDict == null)
                                {
                                    result.FrequencyDict = new();
                                    result.FrequencyDict.Add(freqListName, frequencyRank);
                                }

                                else
                                {
                                    result.FrequencyDict.Add(freqListName, frequencyRank);
                                }

                                if (result.AlternativeSpellings == null)
                                    continue;

                                foreach (var aspelling in result.AlternativeSpellings)
                                {
                                    if (!JMdictLoader.jMdictDictionary.TryGetValue(aspelling, out var edictResults))
                                        continue;

                                    foreach (var aresult in edictResults)
                                    {
                                        if (aresult.PrimarySpelling == reading
                                            || (reading != exactSpelling
                                                && aresult.Readings.Contains(reading)))

                                            if (aresult.FrequencyDict != null &&
                                                aresult.FrequencyDict.TryGetValue(freqListName, out var afrequency))
                                            {
                                                if (afrequency > frequencyRank)
                                                {
                                                    aresult.FrequencyDict[freqListName] = frequencyRank;
                                                }
                                            }

                                            else if (aresult.FrequencyDict == null)
                                            {
                                                aresult.FrequencyDict = new();
                                                aresult.FrequencyDict.Add(freqListName, frequencyRank);
                                            }

                                            else
                                            {
                                                aresult.FrequencyDict.Add(freqListName, frequencyRank);
                                            }
                                    }
                                }
                            }
                        }
                    }

                    if (reading == exactSpelling ||
                        !JMdictLoader.jMdictDictionary.TryGetValue(reading, out jMDictResults))
                        continue;

                    foreach (JMdictResult result in jMDictResults)
                    {
                        if (result.PrimarySpelling == exactSpelling
                            || (result.AlternativeSpellings != null &&
                                result.AlternativeSpellings.Contains(exactSpelling))
                            || (result.KanaSpellings != null && result.KanaSpellings.Contains(exactSpelling)))
                        {
                            foreach (var rreading in result.Readings)
                            {
                                if (!JMdictLoader.jMdictDictionary.TryGetValue(
                                    Kana.KatakanaToHiraganaConverter(rreading), out var rjMDictResults))
                                    continue;

                                foreach (var rresult in rjMDictResults)
                                {
                                    if (rresult.PrimarySpelling == exactSpelling
                                        || rresult.PrimarySpelling == result.PrimarySpelling
                                        || (rresult.AlternativeSpellings != null &&
                                            rresult.AlternativeSpellings.Contains(exactSpelling))
                                        || (rresult.KanaSpellings != null &&
                                            rresult.KanaSpellings.Contains(exactSpelling)))
                                    {
                                        if (rresult.Readings.Any())
                                        {
                                            if (JMdictLoader.jMdictDictionary.TryGetValue(
                                                Kana.KatakanaToHiraganaConverter(rresult.PrimarySpelling),
                                                out var pDictResults))
                                            {
                                                foreach (var pDictResult in pDictResults)
                                                {
                                                    if (pDictResult.PrimarySpelling == result.PrimarySpelling
                                                        && (pDictResult.Readings.Contains(reading)
                                                            || pDictResult.Readings.Contains(
                                                                Kana.HiraganaToKatakanaConverter(reading))))
                                                    {
                                                        if (pDictResult.FrequencyDict != null &&
                                                            pDictResult.FrequencyDict.TryGetValue(freqListName,
                                                                out var pFrequency))
                                                        {
                                                            if (pFrequency > frequencyRank)
                                                            {
                                                                pDictResult.FrequencyDict[freqListName] = frequencyRank;
                                                            }
                                                        }

                                                        else if (pDictResult.FrequencyDict == null)
                                                        {
                                                            pDictResult.FrequencyDict = new();
                                                            pDictResult.FrequencyDict.Add(freqListName,
                                                                frequencyRank);
                                                        }

                                                        else
                                                        {
                                                            pDictResult.FrequencyDict.Add(freqListName,
                                                                frequencyRank);
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (rresult.FrequencyDict != null &&
                                            rresult.FrequencyDict.TryGetValue(freqListName, out var frequency))
                                        {
                                            if (frequency > frequencyRank)
                                            {
                                                rresult.FrequencyDict[freqListName] = frequencyRank;
                                            }
                                        }

                                        else if (rresult.FrequencyDict == null)
                                        {
                                            rresult.FrequencyDict = new();
                                            rresult.FrequencyDict.Add(freqListName, frequencyRank);
                                        }

                                        else
                                        {
                                            rresult.FrequencyDict.Add(freqListName, frequencyRank);
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