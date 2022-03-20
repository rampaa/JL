using System.Diagnostics;
using System.Text.Json;

namespace JL.Core.Dicts.EPWING
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
                List<List<JsonElement>> jsonObjects = await JsonSerializer.DeserializeAsync<List<List<JsonElement>>>(openStream)
                    .ConfigureAwait(false);

                Debug.Assert(jsonObjects != null, nameof(jsonObjects) + " != null");
                foreach (List<JsonElement> jsonObj in jsonObjects)
                {
                    DictionaryBuilder(new EpwingResult(jsonObj), Storage.Dicts[dictType].Contents, dictType);
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
            string[] badCharacters = { "�", "(", "=", "＝", "［", "〔", "「", "『", "（", "【", "[" };

            foreach (string badCharacter in badCharacters)
            {
                if (result.PrimarySpelling.Contains(badCharacter))
                    return false;
            }

            if (!Storage.JapaneseRegex.IsMatch(result.PrimarySpelling))
                return false;

            switch (dictType)
            {
                case DictType.Kenkyuusha:

                    // TODO: Make this configurable?
                    // Remove all example sentences
                    if (result.Definitions.Count > 2)
                    {
                        for (int i = 2; i < result.Definitions.Count; i++)
                        {
                            if (!char.IsDigit(result.Definitions[i][0]))
                            {
                                result.Definitions.RemoveAt(i);
                                --i;
                            }
                        }
                    }

                    // Only keep one example sentence per definition
                    //if (result.Definitions.Count > 2)
                    //{
                    //    bool isMainExample = true;

                    //    for (int i = 2; i < result.Definitions.Count; i++)
                    //    {
                    //        if (char.IsDigit(result.Definitions[i][0]))
                    //        {
                    //            isMainExample = true;
                    //        }

                    //        else
                    //        {
                    //            if (!isMainExample)
                    //            {
                    //                result.Definitions.RemoveAt(i);
                    //                --i;
                    //            }

                    //            isMainExample = false;
                    //        }
                    //    }
                    //}

                    // Filter duplicate entries.
                    // If an entry has reading info while others don't, keep the one with the reading info.
                    if (Storage.Dicts[DictType.Kenkyuusha].Contents.TryGetValue(
                        Kana.KatakanaToHiraganaConverter(result.PrimarySpelling), out List<IResult> kenkyuushaResults))
                    {
                        for (int i = 0; i < kenkyuushaResults.Count; i++)
                        {
                            var kenkyuushaResult = (EpwingResult)kenkyuushaResults[i];

                            if (kenkyuushaResult.Definitions.SequenceEqual(result.Definitions))
                            {
                                if (string.IsNullOrEmpty(kenkyuushaResult.Reading) &&
                                    !string.IsNullOrEmpty(result.Reading))
                                {
                                    kenkyuushaResults.RemoveAt(i);
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
                    if (!result.Definitions.Any(def => Storage.JapaneseRegex.IsMatch(def)))
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
