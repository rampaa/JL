using System.Text.Json;
using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.EPWING.EpwingYomichan;

public static class EpwingYomichanLoader
{
    public static async Task Load(Dict dict)
    {
        if (!Directory.Exists(dict.Path) && !File.Exists(dict.Path))
            return;

        string[] jsonFiles = Directory.GetFiles(dict.Path, "*_bank_*.json");

        foreach (string jsonFile in jsonFiles)
        {
            List<List<JsonElement>>? jsonObjects;

            FileStream openStream = File.OpenRead(jsonFile);
            await using (openStream.ConfigureAwait(false))
            {
                jsonObjects = await JsonSerializer
                    .DeserializeAsync<List<List<JsonElement>>>(openStream)
                    .ConfigureAwait(false);
            }

            if (jsonObjects == null)
                continue;

            foreach (List<JsonElement> jsonObj in jsonObjects)
            {
                DictionaryBuilder(new EpwingYomichanResult(jsonObj), dict);
            }
        }

        dict.Contents.TrimExcess();
    }

    private static void DictionaryBuilder(EpwingYomichanResult yomichanResult, Dict dict)
    {
        if (!IsValidEpwingResultForDictType(yomichanResult, dict))
            return;

        string hiraganaExpression = Kana.KatakanaToHiraganaConverter(yomichanResult.PrimarySpelling);

        if (!string.IsNullOrEmpty(yomichanResult.Reading))
        {
            string hiraganaReading = Kana.KatakanaToHiraganaConverter(yomichanResult.Reading);

            if (dict.Contents.TryGetValue(hiraganaReading, out List<IResult>? tempList2))
                tempList2.Add(yomichanResult);
            else
                dict.Contents.Add(hiraganaReading, new List<IResult> { yomichanResult });
        }

        if (dict.Contents.TryGetValue(hiraganaExpression, out List<IResult>? tempList))
            tempList.Add(yomichanResult);
        else
            dict.Contents.Add(hiraganaExpression, new List<IResult> { yomichanResult });
    }

    private static bool IsValidEpwingResultForDictType(EpwingYomichanResult yomichanResult, Dict dict)
    {
        string[] badCharacters = { "�", "(", "=", "＝", "［", "〔", "「", "『", "（", "【", "[" };

        foreach (string badCharacter in badCharacters)
        {
            if (yomichanResult.PrimarySpelling.Contains(badCharacter))
                return false;
        }

        if (!Storage.JapaneseRegex.IsMatch(yomichanResult.PrimarySpelling))
            return false;

        switch (dict.Type)
        {
            case DictType.Kenkyuusha:
                if ((dict.Options?.Examples?.Value ?? ExamplesOptionValue.None) == ExamplesOptionValue.None)
                {
                    if (yomichanResult.Definitions?.Count > 2)
                    {
                        for (int i = 2; i < yomichanResult.Definitions.Count; i++)
                        {
                            if (!char.IsDigit(yomichanResult.Definitions[i][0]))
                            {
                                yomichanResult.Definitions.RemoveAt(i);
                                --i;
                            }
                        }
                    }
                }
                else if (dict.Options is { Examples.Value: ExamplesOptionValue.One })
                {
                    if (yomichanResult.Definitions?.Count > 2)
                    {
                        bool isMainExample = true;

                        for (int i = 2; i < yomichanResult.Definitions.Count; i++)
                        {
                            if (char.IsDigit(yomichanResult.Definitions[i][0]))
                            {
                                isMainExample = true;
                            }

                            else
                            {
                                if (!isMainExample)
                                {
                                    yomichanResult.Definitions.RemoveAt(i);
                                    --i;
                                }

                                isMainExample = false;
                            }
                        }
                    }
                }

                // Filter duplicate entries.
                // If an entry has reading info while others don't, keep the one with the reading info.
                if (dict.Contents.TryGetValue(
                        Kana.KatakanaToHiraganaConverter(yomichanResult.PrimarySpelling),
                        out List<IResult>? kenkyuushaResults))
                {
                    for (int i = 0; i < kenkyuushaResults.Count; i++)
                    {
                        var kenkyuushaResult = (EpwingYomichanResult)kenkyuushaResults[i];

                        if (kenkyuushaResult.Definitions?.SequenceEqual(yomichanResult.Definitions ?? new()) ?? false)
                        {
                            if (string.IsNullOrEmpty(kenkyuushaResult.Reading) &&
                                !string.IsNullOrEmpty(yomichanResult.Reading))
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
                if (yomichanResult.Definitions != null)
                {
                    // english definitions
                    if (yomichanResult.Definitions.Any(def => def.Contains("→英和") || def.Contains("\\u003")))
                        return false;

                    // english definitions
                    if (!yomichanResult.Definitions.Any(def => Storage.JapaneseRegex.IsMatch(def)))
                        return false;
                }

                break;

            case DictType.Daijisen:
                // kanji definitions
                if (yomichanResult.Definitions?.Any(def => def.Contains("［音］")) ?? false)
                    return false;
                break;

            case DictType.Koujien:
            case DictType.Meikyou:
            case DictType.Gakken:
            case DictType.Kotowaza:
            case DictType.IwanamiYomichan:
            case DictType.JitsuyouYomichan:
            case DictType.ShinmeikaiYomichan:
            case DictType.NikkokuYomichan:
            case DictType.ShinjirinYomichan:
            case DictType.OubunshaYomichan:
            case DictType.ZokugoYomichan:
            case DictType.WeblioKogoYomichan:
            case DictType.GakkenYojijukugoYomichan:
            case DictType.ShinmeikaiYojijukugoYomichan:
            case DictType.NonspecificYomichan:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(dict), dict.Name, null);
        }

        return true;
    }
}
