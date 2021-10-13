using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JapaneseLookup.Dicts;
using JapaneseLookup.EDICT.JMdict;

namespace JapaneseLookup.Frequency
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

                    if (ConfigManager.Dicts[DictType.JMdict].Contents.TryGetValue(
                        Kana.KatakanaToHiraganaConverter(exactSpelling),
                        out List<IResult> jMDictResults))
                    {
                        foreach (var result1 in jMDictResults)
                        {
                            var result = (JMdictResult) result1;
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
                                    if (!ConfigManager.Dicts[DictType.JMdict].Contents
                                        .TryGetValue(aspelling, out var edictResults))
                                        continue;

                                    foreach (var aresult1 in edictResults)
                                    {
                                        var aresult = (JMdictResult) aresult1;
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
                        !ConfigManager.Dicts[DictType.JMdict].Contents.TryGetValue(reading, out jMDictResults))
                        continue;

                    foreach (var result1 in jMDictResults)
                    {
                        var result = (JMdictResult) result1;
                        if (result.PrimarySpelling == exactSpelling
                            || (result.AlternativeSpellings != null &&
                                result.AlternativeSpellings.Contains(exactSpelling))
                            || (result.KanaSpellings != null && result.KanaSpellings.Contains(exactSpelling)))
                        {
                            foreach (var rreading in result.Readings)
                            {
                                if (!ConfigManager.Dicts[DictType.JMdict].Contents.TryGetValue(
                                    Kana.KatakanaToHiraganaConverter(rreading), out var rjMDictResults))
                                    continue;

                                foreach (var rresult1 in rjMDictResults)
                                {
                                    var rresult = (JMdictResult) rresult1;
                                    if (rresult.PrimarySpelling == exactSpelling
                                        || rresult.PrimarySpelling == result.PrimarySpelling
                                        || (rresult.AlternativeSpellings != null &&
                                            rresult.AlternativeSpellings.Contains(exactSpelling))
                                        || (rresult.KanaSpellings != null &&
                                            rresult.KanaSpellings.Contains(exactSpelling)))
                                    {
                                        if (rresult.Readings.Any())
                                        {
                                            if (ConfigManager.Dicts[DictType.JMdict].Contents.TryGetValue(
                                                Kana.KatakanaToHiraganaConverter(rresult.PrimarySpelling),
                                                out var pDictResults))
                                            {
                                                foreach (var pDictResult1 in pDictResults)
                                                {
                                                    var pDictResult = (JMdictResult) pDictResult1;
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