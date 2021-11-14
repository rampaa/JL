using JapaneseLookup.Abstract;
using JapaneseLookup.Dicts;
using JapaneseLookup.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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

            DictionaryBuilder(epwingEntryList, ConfigManager.Dicts[dictType].Contents, dictType);
            ConfigManager.Dicts[dictType].Contents.TrimExcess();
        }

        private static void DictionaryBuilder(List<EpwingEntry> epwingEntryList,
            Dictionary<string, List<IResult>> epwingDictionary, DictType dictType)
        {
            foreach (EpwingEntry entry in epwingEntryList)
            {
                var result = new EpwingResult
                {
                    Definitions = entry.Glossary,
                    Reading = entry.Reading,
                    PrimarySpelling = entry.Expression,
                    WordClasses = entry.Rules
                };

                if (!result.WordClasses.Any())
                    result.WordClasses = null;

                if (!result.Definitions.Any())
                    result.Definitions = null;

                if (!IsValidEpwingResultForDictType(result, dictType))
                    continue;

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

        private static bool IsValidEpwingResultForDictType(EpwingResult result, DictType dictType)
        {
            string[] badCharacters = { "�", "(", "=", "＝", "［", "〔", "「", "『", "（" };

            foreach (string badCharacter in badCharacters)
            {
                if (result.PrimarySpelling.Contains(badCharacter))
                    return false;
            }

            if (!MainWindowUtilities.JapaneseRegex.IsMatch(result.PrimarySpelling))
                return false;

            switch (dictType)
            {
                case DictType.Kenkyuusha:
                    break;
                case DictType.Daijirin:
                    // english definitions
                    if (result.Definitions.Any(def => def.Contains("→英和")))
                        return false;

                    // english definitions
                    if (!result.Definitions.Any(def => MainWindowUtilities.JapaneseRegex.IsMatch(def)))
                        return false;

                    break;
                case DictType.Daijisen:
                    // kanji definitions
                    if (result.Definitions.Any(def => def.Contains("［音］")))
                        return false;

                    break;
                case DictType.Koujien:
                    break;
                case DictType.Meikyou:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dictType), dictType, null);
            }

            return true;
        }
    }
}