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
        public static async Task Load(DictType dictType, string dictPath)
        {
            if (!Directory.Exists(dictPath) && !File.Exists(dictPath))
                return;

            string[] jsonFiles = Directory.GetFiles(dictPath, "*_bank_*.json");

            foreach (string jsonFile in jsonFiles)
            {
                await using FileStream openStream = File.OpenRead(jsonFile);
                var jsonObjects = await JsonSerializer.DeserializeAsync<List<List<JsonElement>>>(openStream)
                    .ConfigureAwait(false);

                Debug.Assert(jsonObjects != null, nameof(jsonObjects) + " != null");
                foreach (var obj in jsonObjects)
                {
                    DictionaryBuilder(new EpwingResult(obj), Storage.Dicts[dictType].Contents, dictType);
                }
            }

            Storage.Dicts[dictType].Contents.TrimExcess();
        }

        private static void DictionaryBuilder(EpwingResult result,
            Dictionary<string, List<IResult>> epwingDictionary, DictType dictType)
        {
            if (!IsValidEpwingResultForDictType(result, dictType))
                return;

            string hiraganaExpression = Kana.KatakanaToHiraganaConverter(result.PrimarySpelling);

            //if (hiraganaExpression != entry.Expression && string.IsNullOrEmpty(entry.Reading))
            //    result.KanaSpelling = entry.Expression;

            if (!string.IsNullOrEmpty(result.Reading))
            {
                string hiraganaReading = Kana.KatakanaToHiraganaConverter(result.Reading);
                //if (hiraganaReading != entry.Reading)
                //    result.KanaSpelling = entry.Reading;

                if (epwingDictionary.TryGetValue(hiraganaReading, out List<IResult> tempList2))
                    tempList2.Add(result);
                else
                    epwingDictionary.Add(hiraganaReading, new List<IResult> { result });
            }

            if (epwingDictionary.TryGetValue(hiraganaExpression, out List<IResult> tempList))
                tempList.Add(result);
            else
                epwingDictionary.Add(hiraganaExpression, new List<IResult> { result });
        }

        private static bool IsValidEpwingResultForDictType(EpwingResult result, DictType dictType)
        {
            string[] badCharacters = { "�", "(", "=", "＝", "［", "〔", "「", "『", "（", "【" };

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
                    if (Storage.Dicts[DictType.Kenkyuusha].Contents.TryGetValue(
                        Kana.KatakanaToHiraganaConverter(result.PrimarySpelling), out var kenkyuushaResults))
                    {
                        foreach (IResult result1 in kenkyuushaResults.ToList())
                        {
                            var kenkyuushaResult = (EpwingResult)result1;
                            if (kenkyuushaResult.Definitions.SequenceEqual(result.Definitions))
                            {
                                if (string.IsNullOrEmpty(kenkyuushaResult.Reading) &&
                                    !string.IsNullOrEmpty(result.Reading))
                                {
                                    kenkyuushaResults.Remove(kenkyuushaResult);
                                    break;
                                }

                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }

                    break;
                case DictType.Daijirin:
                    // english definitions
                    if (result.Definitions.Any(def => def.Contains("→英和") || def.Contains("\\u003")))
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

                case DictType.Gakken:
                    break;

                case DictType.Kotowaza:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dictType), dictType, null);
            }

            return true;
        }
    }
}