using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Diagnostics;
using JapaneseLookup.Abstract;
using JapaneseLookup.Dicts;

namespace JapaneseLookup.EPWING
{
    public static class EpwingJsonLoader
    {
        public static async Task Loader(DictType dictType, string dictPath)
        {
            if (!(Directory.Exists(dictPath) || File.Exists(dictPath)))
                return;

            List<EpwingEntry> epwingEntryList = new();

            string[] jsonFiles = Directory.GetFiles(dictPath, "*_bank_*.json");
            // string[] jsonFiles = Directory.GetFiles(dictPath, "*.json");

            foreach (string jsonFile in jsonFiles)
            {
                await using FileStream openStream = File.OpenRead(jsonFile);
                var jsonObject = await JsonSerializer.DeserializeAsync<List<List<JsonElement>>>(openStream);

                Debug.Assert(jsonObject != null, nameof(jsonObject) + " != null");
                foreach (var obj in jsonObject)
                {
                    epwingEntryList.Add(new EpwingEntry(obj));
                }
            }

            DictionaryBuilder(epwingEntryList, ConfigManager.Dicts[dictType].Contents);
            ConfigManager.Dicts[dictType].Contents.TrimExcess();
        }

        public static void DictionaryBuilder(List<EpwingEntry> epwingEntryList,
            Dictionary<string, List<IResult>> epwingDictionary)
        {
            foreach (var entry in epwingEntryList)
            {
                var result = new EpwingResult
                {
                    Definitions = entry.Glosssary,
                    Reading = entry.Reading,
                    PrimarySpelling = entry.Expression,
                    WordClasses = entry.Rules
                };
                
                string hiraganaExpression = Kana.KatakanaToHiraganaConverter(entry.Expression);
                if (hiraganaExpression != entry.Expression && string.IsNullOrEmpty(entry.Reading))
                    result.KanaSpelling = entry.Expression;

                if (!string.IsNullOrEmpty(entry.Reading))
                {
                    string hiraganaReading = Kana.KatakanaToHiraganaConverter(entry.Reading);
                    if (hiraganaReading != entry.Reading)
                        result.KanaSpelling = entry.Reading;

                    if (epwingDictionary.TryGetValue(hiraganaReading, out List<IResult> tempList2))
                        tempList2.Add(result);
                    else
                        epwingDictionary.TryAdd(entry.Reading, new List<IResult> { result });
                }

                if (epwingDictionary.TryGetValue(hiraganaExpression, out List<IResult> tempList))
                    tempList.Add(result);
                else
                    epwingDictionary.TryAdd(hiraganaExpression, new List<IResult> { result });

            }
        }
    }
}